using System;
using System.Linq;
using System.Net;

using WebSocketSharp;
using WebSocketSharp.Server;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;
using Clifton.Core.Web.WebInterfaces;

namespace Clifton.Core.Web.WebSocketService
{
	public class WebSocketClientService : ServiceBase, IWebSocketClientService
	{
		protected WebSocket wsc;

		public void Start(string address, int port, string path)
		{
			// IPAddress ipaddr = new IPAddress(address.Split('.').Select(a => (byte)a.to_i()).ToArray());
			// WebSocketServer wss = new WebSocketServer(ipaddr, port, this);
			wsc = new WebSocket(address + ":"+port+path, this);
			wsc.OnMessage += OnMessage;
			wsc.Connect();
		}

		public void Send(string msg)
		{
			wsc.Send(msg);
		}

		private void OnMessage(object sender, MessageEventArgs e)
		{
			IService service = e.CallerContext as IService;
		}
	}
}
