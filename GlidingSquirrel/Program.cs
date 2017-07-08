using System;

namespace SBRL.GlidingSquirrel
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			FileHttpServer server = new FileHttpServer(60606);
			server.Start().Wait();
		}
	}
}
