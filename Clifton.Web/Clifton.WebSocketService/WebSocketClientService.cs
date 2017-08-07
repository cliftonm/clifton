using System;
using System.Linq;
using System.Net;

using WebSocketSharp;
using WebSocketSharp.Server;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;
using Clifton.WebInterfaces;

namespace Clifton.Core.Web.WebSocketService
{
	public class WebSocketClientService : ServiceBase, IWebSocketClientService
	{
		protected WebSocket wsc;

		public void Start(string address, int port, string path)
		{
            // IPAddress ipaddr = new IPAddress(address.Split('.').Select(a => (byte)a.to_i()).ToArray());
            // WebSocketServer wss = new WebSocketServer(ipaddr, port, this);
            wsc = new WebSocket(address + ":" + port + path, this);
			wsc.OnMessage += OnMessage;
            wsc.OnError += OnError;
            wsc.OnClose += OnClose;
            wsc.Log.Level = LogLevel.NoLogging;
            wsc.Connect();
		}

		public void Send(string msg)
		{
			wsc.Send(msg);
		}

		protected void OnMessage(object sender, MessageEventArgs e)
		{
			IService service = e.CallerContext as IService;
			service.ServiceManager.Get<ISemanticProcessor>().ProcessInstance<SocketMembrane, ClientSocketMessage>(msg =>
				{
					msg.Text = e.Data;
				});
		}

        protected void OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine("Error: " + e.Message);
        }

        protected void OnClose(object sender, CloseEventArgs e)
        {
            Console.WriteLine("Close: " + e.Reason);
        }
    }
}
