using System;
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

		protected Mime mimeLookup = new Mime();

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


			while(true)
			{
				TcpClient nextClient = await server.AcceptTcpClientAsync();
				ThreadPool.QueueUserWorkItem(new WaitCallback(HandleClientThreadRoot), nextClient);
			}
		}

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

		public abstract Task HandleRequest(HttpRequest request, HttpResponse response);
	}
}
