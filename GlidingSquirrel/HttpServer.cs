using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SBRL.GlidingSquirrel
{ 
	public class HttpServer
	{
		public IPAddress BindAddress { get; private set; }
		public int Port { get; private set; }

		protected TcpListener server;

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
			server = new TcpListener(new IPEndPoint(BindAddress, Port));

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
			StreamWriter destination = new StreamWriter(client.GetStream());

			HttpRequest request = await HttpRequest.FromStream(source);
		}
	}
}
