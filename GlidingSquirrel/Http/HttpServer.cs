using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
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

	public abstract class HttpServer
	{
		public static readonly string Version = "0.4-alpha";

		public readonly IPAddress BindAddress;
		public readonly int Port;

		public string BindEndpoint {
			get {
				string result = BindAddress.ToString();
				if(result.Contains(":"))
					result = $"[{result}]";
				result += $":{Port}";
				return result;
			}
		}

		protected TcpListener server;

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
		public Dictionary<string, string> MimeTypeOverrides = new Dictionary<string, string>() {
			[".html"] = "text/html"
		};

		public HttpServer(IPAddress inBindAddress, int inPort)
		{
			BindAddress = inBindAddress;
			Port = inPort;
		}
		public HttpServer(int inPort) : this(IPAddress.IPv6Any, inPort)
		{
		}

		public async Task Start()
		{
			Log.WriteLine($"GlidingSquirrel v{Version}");
			Log.Write("Starting server - ");

			server = new TcpListener(new IPEndPoint(BindAddress, Port));
			server.Start();

			Console.WriteLine("done");
			Log.WriteLine($"Listening for requests on http://{BindEndpoint}");

			await setup();

			while(true)
			{
				TcpClient nextClient = await server.AcceptTcpClientAsync();
				ThreadPool.QueueUserWorkItem(new WaitCallback(HandleClientThreadRoot), nextClient);
			}
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
				Console.WriteLine(error);
			}
			finally
			{
				if(finalAction != HttpConnectionAction.LeaveAlone)
					client.Close();
			}
		}

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
						"{0} Removing rogue connection header (value: {1}) from request",
						request.ClientAddress,
						request.Headers["connection"]
					);
					request.Headers.Remove("connection");
				}

				nextAction = await doHandleRequest(request, response);

				if(nextAction == HttpConnectionAction.LeaveAlone ||
				   nextAction == HttpConnectionAction.KillConnection)
					break;

				Log.WriteLine(
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

		public async Task<HttpConnectionAction> doHandleRequest(HttpRequest request, HttpResponse response)
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
