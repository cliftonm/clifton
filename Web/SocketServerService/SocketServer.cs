using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Clifton.ExtensionMethods;
using Clifton.SemanticProcessorInterfaces;
using Clifton.ServiceInterfaces;

using WebServerInterfaces;
using WebServerSemantics;


namespace WebServerService
{
	public class WebServerModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<ISocketServerService, SocketServer>();
		}
	}

    public class SocketServer : ServiceBase, ISocketServerService
    {
		public bool Terminate { get; set; }

		protected ILoggerService logger;
		protected ISemanticProcessor semProc;
		protected Socket listener;

		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			logger = ServiceManager.Get<ILoggerService>();
			semProc = ServiceManager.Get<ISemanticProcessor>();
		}

		public virtual void Start()
		{
			IConfigService cfg = ServiceManager.Get<IConfigService>();
			string ip = cfg.GetValue("SocketIP");
			int port = cfg.GetValue("SocketPort").to_i();
			byte[] bip = ip.Split('.').Select(s => (byte)s.to_i()).ToArray();
			string url = ip+":"+port+"/";
			logger.Log(LogMessage.Create("Listening on " + ip + " (port " + port + ")"));
			listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			listener.Bind(new IPEndPoint(new IPAddress(bip), port));
			listener.Listen(10);

			Task.Run(() =>
				{
					while (!Terminate)
					{
						Socket handler = listener.Accept();
						Thread thread = new Thread(new ParameterizedThreadStart(SocketListener));
						thread.IsBackground = true;
						thread.Start(handler);
					}
				});
		}

		protected void SocketListener(object obj)
		{
			Socket handler = (Socket)obj;

			try
			{
				while (true)
				{
					byte[] nextPacket = null;
					byte[] packet = GetPacket(handler, ref nextPacket);
				}
			}
			catch
			{
				// Done with thread, as connection has been lost.
			}
		}

		protected byte[] GetPacket(Socket handler, ref byte[] nextPacket)
		{
			int bytesReceived = 0;
			int packetLength = 0;
			byte[] data = new byte[256];
			int idx = 2;

			if (nextPacket != null)
			{
				bytesReceived = nextPacket.Length;

				if (bytesReceived == 1)
				{
					data = new byte[256];
					bytesReceived = handler.Receive(data);
					packetLength = nextPacket[0] + (data[0] << 8);
					bytesReceived -= 1;
					idx = 1;
				}
				else
				{
					data = new byte[nextPacket.Length];
					Array.Copy(nextPacket, data, nextPacket.Length);
					packetLength = nextPacket[0] + (nextPacket[0] << 8);
					bytesReceived -= 2;
					idx = 2;
				}
			}
			else
			{
				nextPacket = null;
				data = new byte[256];
				bytesReceived = handler.Receive(data);
				packetLength = data[0] + (data[1] << 8);
				bytesReceived -= 2;
			}

			byte[] packet = new byte[packetLength];
			Array.Copy(data, idx, packet, 0, bytesReceived);

			while (bytesReceived < packetLength)
			{
				int segment = handler.Receive(data);
				int totalBytesReceived = bytesReceived + segment;

				if (totalBytesReceived == packetLength)
				{
					Array.Copy(data, 0, packet, bytesReceived, segment);
				}
				else if (totalBytesReceived > packetLength)
				{
					// Copy overflow as start of next packet.
					int overflow = totalBytesReceived - packetLength;
					nextPacket = new byte[overflow];
					Array.Copy(data, segment - overflow, nextPacket, 0, overflow);

					// Fill up current packet.
					Array.Copy(data, 0, packet, bytesReceived, packetLength - bytesReceived);
				}

				bytesReceived = totalBytesReceived;
			}

			return packet;
		}
	}
}
										 /*

			// Start listening for connections.
			while (true)
			{
				Console.WriteLine("Waiting for a connection...");
				// Program is suspended while waiting for an incoming connection.
				Socket handler = listener.Accept();
				data = null;

				// An incoming connection needs to be processed.
				while (true)
				{
					bytes = new byte[1024];
					int bytesRec = handler.Receive(bytes);
					data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
					if (data.IndexOf("<EOF>") > -1)
					{
						break;
					}
				}

				// Show the data on the console.
				Console.WriteLine("Text received : {0}", data);

				// Echo the data back to the client.
				byte[] msg = Encoding.ASCII.GetBytes(data);

				handler.Send(msg);
				handler.Shutdown(SocketShutdown.Both);
				handler.Close();
			}

		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
		}

		Console.WriteLine("\nPress ENTER to continue...");
		Console.Read();

	}

	public static int Main(String[] args)
	{
		StartListening();
		return 0;
	}
}
*/