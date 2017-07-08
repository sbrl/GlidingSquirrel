using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MimeSharp;

namespace SBRL.GlidingSquirrel
{ 
	public abstract class HttpServer
	{
		public static readonly string Version = "0.1-alpha";

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

			response.Headers.Add("server", $"GlidingSquirrel/{Version}");

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

			Log.WriteLine(
				"{0} [{1}] [{2}] {3}",
				request.ClientAddress,
				request.Method.ToString(),
				response.ResponseCode,
				request.Url
			);

			await response.SendTo(destination);
			client.Close();
		}

		protected abstract Task setup();

		public abstract Task HandleRequest(HttpRequest request, HttpResponse response);
	}
}
