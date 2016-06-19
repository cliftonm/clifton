using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.Core.PaperTrailAppLoggerService
{
	public static class UdpPaperTrail
	{
		public static string IP;
		public static int Port;
		private static IPAddress addr = null;

		public static void SendUdpMessage(string message)
		{
			try
			{
				if (addr == null)
				{
					IPAddress[] addresses = Dns.GetHostAddresses(IP);
					addr = addresses[0];
				}

				SendUdpMessage(addr, Port, message);
			}
			catch
			{
				// silent catch, we don't care if the network has died.
			}
		}

		private static void SendUdpMessage(IPAddress address, int port, string message)
		{
			try
			{
				Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				IPEndPoint endPoint = new IPEndPoint(address, port);
				byte[] buffer = Encoding.ASCII.GetBytes(message);
				socket.SendTo(buffer, endPoint);
				socket.Close();
			}
			catch
			{
				// Silent exception handler, we don't want logging to kill the app.
			}
		}
	}
}
