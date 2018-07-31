using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using MimeSharp;

namespace SBRL.GlidingSquirrel.Http
{
	/// <summary>
	/// Describes what Gliding Squirrel should do with a connection once your HandleRequest() method has
	/// finished building a response.
	/// </summary>
	public enum HttpConnectionAction
	{
		/// <summary>
		/// Continues as normal by sending the response to the client,
		/// and then handling keep-alives as normal.
		/// </summary>
		Continue,
		/// <summary>
		/// Sends the response to the client, and then kills the connection.
		/// </summary>
		SendAndKillConnection,
		/// <summary>
		/// Kills the connection without sending the response.
		/// </summary>
		KillConnection,
		/// <summary>
		/// Assumes that the client connection has been handed off to another connection manager and
		/// leaves it alone.
		/// </summary>
		LeaveAlone
	}

	/// <summary>
	/// From https://stackoverflow.com/a/7857844/1460422
	/// </summary>
	public class TcpListenerExtended : TcpListener
	{
		public TcpListenerExtended(IPEndPoint endpoint) : base(endpoint) {  }
		public TcpListenerExtended(IPAddress bindAddress, int port) : base(bindAddress, port) {  }

		/// <summary>
		/// Whether the TcpListener is currently listening for connections.
		/// </summary>
		/// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
		public new bool Active {
			get { return base.Active; }
		}
	}

	/// <summary>
	/// The delegate for the OnShutdown event on the HttpServer.
	/// </summary>
	public delegate void OnServerShutdown();

	/// <summary>
	/// The main HTTP Server implementation class. Inherit from this class to build your own HTTP server!
	/// Please note that for WebSockets support you'll need to inherit from the seperate <see cref="Websocket.WebsocketServer" />
	/// class instead.
	/// </summary>
	public abstract class HttpServer
	{
		/// <summary>
		/// The current version of GlidingSquirrel
		/// </summary>
		public static string Version {
			get {
				using (StreamReader resourceReader = new StreamReader(Assembly.GetCallingAssembly().GetManifestResourceStream("SBRL.GlidingSquirrel.release-version.txt")))
				{
					return resourceReader.ReadToEnd().Trim();
				}
			}
		}

		/// <summary>
		/// The address the server will bind to.
		/// </summary>
		public readonly IPAddress BindAddress;
		/// <summary>
		/// The port the server will listen on.
		/// </summary>
		public readonly int Port;

		/// <summary>
		/// The endpoint the server will listen on. Useful for logging.
		/// You can change this value via the BindAddress and Port properties.
		/// </summary>
		public string BindEndpoint {
			get {
				string result = BindAddress.ToString();
				if(result.Contains(":"))
					result = $"[{result}]";
				result += $":{Port}";
				return result;
			}
		}

		/// <summary>
		/// The underlying tcp listener.
		/// </summary>
		protected TcpListenerExtended server;

		/// <summary>
		/// The maximum allowed length for urls.
		/// </summary>
		public int MaximumUrlLength = 1024 * 16; // Default: 16kb
		/// <summary>
		/// The maximum allowed time to wait for data from the client, in milliseconds.
		/// After this time has elapsed the connection will be closed.
		/// Note that this doesn't affect a connection once a websocket has been fully initialised.
		/// </summary>
		public int IdleTimeout = 1000 * 60;

		private Mime mimeLookup = new Mime();
		/// <summary>
		/// Override MIME type mappings. Values specified here will be sued in place of any found in MimeSharp's database.
		/// </summary>
		public Dictionary<string, string> MimeTypeOverrides = new Dictionary<string, string>() {
			[".html"] = "text/html"
		};

		/// <summary>
		/// Fired when the server shuts down.
		/// </summary>
		public event OnServerShutdown OnShutdown;

		/// <summary>
		/// The cancellation token we use to stop the server and all it's threads
		/// </summary>
		protected CancellationTokenSource canceller = new CancellationTokenSource();
		/// <summary>
		/// Shortcut property to get the cancellation token directly.
		/// </summary>
		protected CancellationToken cancellationToken {
			get {
				return canceller.Token;
			}
		}

		/// <summary>
		/// Initialises a new HttpServer.
		/// </summary>
		/// <param name="inBindAddress">The IP address to bind to.</param>
		/// <param name="inPort">The port to listen on.</param>
		public HttpServer(IPAddress inBindAddress, int inPort)
		{
			BindAddress = inBindAddress;
			Port = inPort;
		}
		/// <summary>
		/// Initializes a new HttpServer that listens for connections from all IPv6 addresses.
		/// </summary>
		/// <param name="inPort">The port to listen on.</param>
		public HttpServer(int inPort) : this(IPAddress.IPv6Any, inPort)
		{
		}

		/// <summary>
		/// Starts listening for requests.
		/// </summary>
		public async Task Start()
		{
			Log.WriteLine(LogLevel.System, $"GlidingSquirrel v{Version}");
			Log.Write(LogLevel.System, "Starting server - ");

			server = new TcpListenerExtended(new IPEndPoint(BindAddress, Port));
			cancellationToken.Register(server.Stop); // Stop the server when we cancel out
			server.Start();

			Log.WriteLine(LogLevel.System, "done");
			Log.WriteLine(LogLevel.System, $"Listening for requests on http://{BindEndpoint}");

			await setup();

			while(server.Active)
			{
				TcpClient nextClient;
				try {
					nextClient = await server.AcceptTcpClientAsync();
				} catch (ObjectDisposedException) {
					break; // break out - the server must have been shutdown
				}
				ThreadPool.QueueUserWorkItem(new WaitCallback(HandleClientThreadRoot), nextClient);
			}

			OnShutdown?.Invoke();
		}

		/// <summary>
		/// Stops the server. If overriding, please remember to call this method!
		/// </summary>
		public void Stop()
		{
			canceller.Cancel();
		}

		/// <summary>
		/// Fetches the mime type for a given file path.
		/// This method applies  
		/// </summary>
		/// <returns>The mime type for the specified file path.</returns>
		/// <param name="filePath">The file path to lookup.</param>
		public string LookupMimeType(string filePath)
		{
			string fileExtension = Path.GetExtension(filePath);
			if(MimeTypeOverrides.ContainsKey(fileExtension))
				return MimeTypeOverrides[fileExtension];
			
			return mimeLookup.Lookup(filePath);
		}

		/// <summary>
		/// Handles requests from a specified client.
		/// This is the root method that is called when spawning a new thread to
		/// handle the client.
		/// </summary>
		/// <param name="transferredClient">The client to handle.</param>
		protected async void HandleClientThreadRoot(object transferredClient)
		{
			TcpClient client = transferredClient as TcpClient;

			HttpConnectionAction finalAction = HttpConnectionAction.KillConnection;
			try
			{
				finalAction = await HandleClient(client);
			}
			catch(Exception error)
			{
				Log.WriteLine(LogLevel.Error, error.ToString());
			}
			finally
			{
				if(finalAction != HttpConnectionAction.LeaveAlone)
					client.Close();
			}
		}

		/// <summary>
		/// Handles a single HTTP client connection. Usually you don't want to call this
		/// directly - Start() will for you :-)
		/// </summary>
		/// <param name="client">The client to handle.</param>
		/// <returns>What should be done with the underlying connection.</returns>
		public async Task<HttpConnectionAction> HandleClient(TcpClient client)
		{
			client.ReceiveTimeout = IdleTimeout;
			StreamReader source = new StreamReader(client.GetStream());
			StreamWriter destination = new StreamWriter(client.GetStream()) { AutoFlush = true };

			HttpConnectionAction nextAction = HttpConnectionAction.Continue;
			int requestsMade = 0;

			while(true)
			{
				requestsMade++;

				HttpRequest request = await HttpRequest.FromStream(source);
				// If the request is null, then something went wrong! Well, the connection was probably closed by the client.
				if(request == null)
					break;

				request.ClientConnection = client;
				request.ClientAddress = client.Client.RemoteEndPoint as IPEndPoint;

				HttpResponse response = new HttpResponse();

				// Respond with the same protocol version that the request asked for
				response.HttpVersion = request.HttpVersion;
				// Tell everyone what version of the gliding squirrel we are running
				response.Headers.Add("Server", $"GlidingSquirrel/{Version}");
				// Add the date header
				response.Headers.Add("Date", DateTime.Now.ToString("R"));
				// We don't support keep-alive just yet
				response.Headers.Add("Connection", Connection.KeepAlive);
				// Make sure compression works as expected
				//response.Headers.Add("vary", "accept-encoding");

				// Delete the connection header if we're running http 1.0 - ref RFC2616 sec 14.10, final paragraph
				if(request.HttpVersion <= 1.0f && request.Headers.ContainsKey("connection"))
				{
					Log.WriteLine(
						LogLevel.Warning,
						"{0} Removing rogue connection header (value: {1}) from request",
						request.ClientAddress,
						request.Headers["connection"]
					);
					request.Headers.Remove("connection");
				}

				nextAction = await DoHandleRequest(request, response);

				if(nextAction == HttpConnectionAction.LeaveAlone ||
				   nextAction == HttpConnectionAction.KillConnection)
					break;

				Log.WriteLine(
					LogLevel.Info,
					"{0} [{1}:HTTP {2} {3}] [{4}] {5}",
					request.ClientAddress,
					requestsMade,
					request.HttpVersion.ToString("0.0"),
					request.Method.ToString(),
					response.ResponseCode,
					request.Url
				);

				await response.SendTo(destination);

				if(nextAction == HttpConnectionAction.SendAndKillConnection)
					break;

				if(
					request.HttpVersion <= 1.0f ||
					(
						request.Headers.ContainsKey("connection") &&
						request.Headers["connection"] == "close"
					)
				)
					break;
			}

			if(nextAction != HttpConnectionAction.LeaveAlone)
				client.Close();

			return nextAction;
		}

		/// <summary>
		/// Handles a single request from a client.
		/// </summary>
		/// <param name="request">The request to handle.</param>
		/// <param name="response">The response to send back to the client.</param>
		/// <returns>What should be done with the underlying connection.</returns>
		public async Task<HttpConnectionAction> DoHandleRequest(HttpRequest request, HttpResponse response)
		{
			// Check the http version of the request
			if(request.HttpVersion < 1.0f || request.HttpVersion >= 2.0f)
			{
				response.ResponseCode = HttpResponseCode.RequestUrlTooLong;
				response.ContentType = "text/plain";
				await response.SetBody($"Error: HTTP version {request.HttpVersion} isn't supportedby this server.\r\n" +
					"Supported versions: 1.0, 1.1");
				return HttpConnectionAction.SendAndKillConnection;
			}
			// Check the length of the url
			if(request.Url.Length > MaximumUrlLength)
			{
				response.ResponseCode = HttpResponseCode.RequestUrlTooLong;
				response.ContentType = "text/plain";
				await response.SetBody($"Error: That request url was too long (this " +
					$"server's limit is {MaximumUrlLength} characters)");
				return HttpConnectionAction.SendAndKillConnection;
			}

			// Make sure that the content-length header is specified if it's needed
			if(request.ContentLength == -1 &&
			   request.Headers.ContainsKey("content-type") &&
			   request.Method != HttpMethod.GET &&
			   request.Method != HttpMethod.HEAD)
			{
				response.ResponseCode = HttpResponseCode.LengthRequired;
				response.ContentType = "text/plain";
				await response.SetBody("Error: You appear to be uploading something, but didn't " +
					"specify the content-length header.");
				return HttpConnectionAction.SendAndKillConnection;
			}

			HttpConnectionAction nextAction = HttpConnectionAction.Continue;
			try
			{
				nextAction = await HandleRequest(request, response);
			}
			catch(Exception error)
			{
				response.ResponseCode = new HttpResponseCode(503, "Server Error Occurred");
				response.Headers.Add("content-type", "text/plain");
				await response.SetBody(
					$"An error ocurred whilst serving your request to '{request.Url}'. Details:\n\n" +
					$"{error.ToString()}"
				);
			}

			if(nextAction != HttpConnectionAction.LeaveAlone && request.Accepts.Length > 0 && !request.WillAccept(response.ContentType))
			{
				response.ResponseCode = HttpResponseCode.NotAcceptable;
				await response.SetBody($"Error: The content available (with the mime type {response.ContentType})" +
					$"does not appear to be acceptable according to your accepts" +
					$"header ({request.GetHeaderValue("accepts", "")})");
				response.ContentType = "text/plain";
			}

			return nextAction;
		}

		/// <summary>
		/// Called once the server is listening for requests, but before the first request is accepted.
		/// Use to perform setup logic, obviously :P
		/// </summary>
		/// <returns>The setup.</returns>
		protected abstract Task setup();

		/// <summary>
		/// Used to handle a request. Called in a separate thread. If an exception is thrown
		/// here, it's caught and sent back to the user - which may not be what you want.
		/// todo: Upgrades to that part of the system are pending.
		/// </summary>
		/// <param name="request">The request made by the client.</param>
		/// <param name="response">The response to send to the client.</param>
		/// <returns>What to do with the connection.</returns>
		public abstract Task<HttpConnectionAction> HandleRequest(HttpRequest request, HttpResponse response);
	}
}
