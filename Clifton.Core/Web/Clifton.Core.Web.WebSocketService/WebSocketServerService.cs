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
	public class WebSocketServerService : ServiceBase, IWebSocketServerService
	{
		public void Start(string address, int port, string path)
		{
			IPAddress ipaddr = new IPAddress(address.Split('.').Select(a => (byte)a.to_i()).ToArray());
			WebSocketServer wss = new WebSocketServer(ipaddr, port, this);
			wss.AddWebSocketService<ServerReceiver>(path);
			wss.Start();
		}
	}

	public class ServerReceiver : WebSocketBehavior
	{
		protected override void OnMessage(MessageEventArgs e)
		{
			Console.WriteLine(e.Data);

			IService service = e.CallerContext as IService;
			Send("--->" + e.Data);
		}
	}
}
