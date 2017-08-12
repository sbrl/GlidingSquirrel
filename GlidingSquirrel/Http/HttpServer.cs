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
	public abstract class HttpServer
	{
		public static readonly string Version = "0.2-alpha";

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

		public int MaximumUrlLength = 1024 * 16; // Default: 16kb

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

			try
			{
				await HandleClient(client);
			}
			catch(Exception error)
			{
				Console.WriteLine(error);
			}
			finally
			{
				client.Close();
			}
		}

		public async Task HandleClient(TcpClient client)
		{
			StreamReader source = new StreamReader(client.GetStream());
			StreamWriter destination = new StreamWriter(client.GetStream()) { AutoFlush = true };

			HttpRequest request = await HttpRequest.FromStream(source);
			request.ClientAddress = client.Client.RemoteEndPoint as IPEndPoint;
			HttpResponse response = new HttpResponse();

			// Respond with the same protocol version that the request asked for
			response.HttpVersion = request.HttpVersion;
			// Tell everyone what version of the gliding squirrel we are running
			response.Headers.Add("server", $"GlidingSquirrel/{Version}");
			// Add the date header
			response.Headers.Add("date", DateTime.Now.ToString("R"));
			// We don't support keep-alive just yet
			response.Headers.Add("connection", Connection.Close);

			await doHandleRequest(request, response);

			Log.WriteLine(
				"{0} [HTTP {1} {2}] [{3}] {4}",
				request.ClientAddress,
				request.HttpVersion.ToString("0.0"),
				request.Method.ToString(),
				response.ResponseCode,
				request.Url
			);

			await response.SendTo(destination);
			client.Close();
		}

		public async Task doHandleRequest(HttpRequest request, HttpResponse response)
		{
			// Check the http version of the request
			if(request.HttpVersion < 1.0f || request.HttpVersion >= 2.0f)
			{
				response.ResponseCode = HttpResponseCode.RequestUrlTooLong;
				response.ContentType = "text/plain";
				await response.SetBody($"Error: HTTP version {request.HttpVersion} isn't supportedby this server.\r\n" +
					"Supported versions: 1.0, 1.1");
				return;
			}
			// Check the length of the url
			if(request.Url.Length > MaximumUrlLength)
			{
				response.ResponseCode = HttpResponseCode.RequestUrlTooLong;
				response.ContentType = "text/plain";
				await response.SetBody($"Error: That request url was too long (this " +
					$"server's limit is {MaximumUrlLength} characters)");
				return;
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
				return;
			}

			try
			{
				await HandleRequest(request, response);
			}
			catch(Exception error)
			{
				response.ResponseCode = new HttpResponseCode(503, "Server Error Occurred");
				await response.SetBody(
					$"An error ocurred whilst serving your request to '{request.Url}'. Details:\n\n" +
					$"{error.ToString()}"
				);
			}

			if(request.Accepts.Length > 0 && !request.WillAccept(response.ContentType))
			{
				response.ResponseCode = HttpResponseCode.NotAcceptable;
				await response.SetBody($"Error: The content available (with the mime type {response.ContentType})" +
					$"does not appear to be acceptable according to your accepts" +
					$"header ({request.GetHeaderValue("accepts", "")})");
				response.ContentType = "text/plain";
			}
		}

		protected abstract Task setup();

		public abstract Task HandleRequest(HttpRequest request, HttpResponse response);
	}
}
