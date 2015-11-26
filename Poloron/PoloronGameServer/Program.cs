using System;

using Clifton.ServiceInterfaces;
using Clifton.SemanticProcessorInterfaces;
using Clifton.Semantics;

using WebServerInterfaces;
using WebServerSemantics;

namespace PoloronGameServer
{
	partial class Program
	{
		static void Main(string[] args)
		{
			Bootstrap();
			InitializePacketHandler();
			ISocketServerService socketService = serviceManager.Get<ISocketServerService>();
			socketService.Start();
			Console.WriteLine("Press a key to exit.");
			Console.ReadLine();
		}

		static void InitializePacketHandler()
		{
			ISemanticProcessor semProc = serviceManager.Get<ISemanticProcessor>();
			semProc.Register<SocketMembrane, PacketReceptor>();
		}
	}

	public class PacketReceptor : IReceptor
	{
		public void Process(ISemanticProcessor proc, IMembrane membrane, SocketPacket packet)
		{
		}
	}
}
