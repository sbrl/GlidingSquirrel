using System;
using System.Collections.Generic;
using System.Linq;

using SBRL.GlidingSquirrel.Http;

namespace SBRL.GlidingSquirrel
{
    enum OperationMode
    {
        FileHttp,
        EchoWebsocket
    }
	class MainClass
	{
		public static void Main(string[] args)
		{
            OperationMode mode = OperationMode.FileHttp;
			string webrootPath = ".";
			int port = 60606;

			List<string> extraArgs = new List<string>();
			for(int i = 0; i < args.Length; i++)
			{
				if(!args[i].StartsWith("-"))
				{
					extraArgs.Add(args[i]);
					continue;
				}

				switch(args[i])
				{
					case "-p":
					case "--port":
						port = int.Parse(args[++i]);
						break;

                    case "-m":
                    case "--mode":
                        mode = (OperationMode)Enum.Parse(typeof(OperationMode), args[++i]);
                        break;
					
					case "-h":
					case "--help":
						Console.WriteLine("GlidingSquirrel v{0}", HttpServer.Version);
						Console.WriteLine();
						Console.WriteLine("A HTTP 1.0 server, written by Starbeamrainbowlabs");
						Console.WriteLine();
						Console.WriteLine("Usage:");
						Console.WriteLine("GlidingSquirrel [options] [webroot]");
						Console.WriteLine();
						Console.WriteLine("Options:");
						Console.WriteLine("    --help                Shows this help message");
						Console.WriteLine("    --version             Display the version of GlidingSquirrel and then exit");
						Console.WriteLine("    --port {port-number}  Sets the port number to listen on");
                        Console.WriteLine("    --mode {mode}         Sets the operating mode. Possible values: FileHttp, EchoWebsocket");
                        Console.WriteLine();
						return;
					
					case "-v":
					case "--version":
						Console.WriteLine("GlidingSquirrel v{0}", HttpServer.Version);
						Console.WriteLine();
						Console.WriteLine("By Starbeamrainbowlabs");
						Console.WriteLine("Licensed under the Mozilla Public License 2.0.");
						Console.WriteLine("You can review a copy of this license here: https://git.starbeamrainbowlabs.com/sbrl/GlidingSquirrel/src/master/LICENSE");
						Console.WriteLine();
						Console.WriteLine("The name comes from FlyingSquirrel, my earlier attempt at" +
						                  "building a http server in Node.JS. That version even supported" +
						                  "PHP via CGI - which may make it into GlidingSquirrel eventually," +
						                  "though at this point it's unlikely.");
						return;
				}
			}

			if(extraArgs.Count > 0)
				webrootPath = args.First();

            switch(mode)
            {
				case OperationMode.FileHttp:
					FileHttpServer server = new FileHttpServer(port, webrootPath);
					server.Start().Wait();
                    break;

                case OperationMode.EchoWebsocket:

                    break;
            }
		}
	}
}
