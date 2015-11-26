using System;

using WebServerInterfaces;

namespace PoloronGameServer
{
	partial class Program
	{
		static void Main(string[] args)
		{
			Bootstrap();
			ISocketServerService socketService = serviceManager.Get<ISocketServerService>();
			socketService.Start();
			Console.WriteLine("Press a key to exit.");
			Console.ReadLine();
		}
	}
}
