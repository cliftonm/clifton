using System;
using System.Linq;
using System.Net;

using WebSocketSharp;
using WebSocketSharp.Server;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;
using Clifton.WebInterfaces;

namespace Clifton.Core.Web.WebSocketService
{
	public class WebSocketServerService : ServiceBase, IWebSocketServerService
	{
        protected WebSocketServer wss;

        public void Start(string address, int port, string path)
		{
			IPAddress ipaddr = new IPAddress(address.Split('.').Select(a => (byte)a.to_i()).ToArray());
			wss = new WebSocketServer(ipaddr, port, this);
			wss.AddWebSocketService<ServerReceiver>(path);
            wss.Log.Level = LogLevel.NoLogging;
			wss.Start();
		}

        public void Stop()
        {
            wss.Stop();
        }
	}

	public class ServerReceiver : WebSocketBehavior, Clifton.WebInterfaces.IWebSocketSession
	{
		public void Reply(string msg)
		{
			Send(msg);
		}

        protected override void OnOpen()
        {
            Console.WriteLine("Open");
        }

        protected override void OnMessage(MessageEventArgs e)
		{
			IService service = e.CallerContext as IService;
			service.ServiceManager.Get<ISemanticProcessor>().ProcessInstance<SocketMembrane, ServerSocketMessage>(msg => 
				{
					msg.Text = e.Data;
					msg.Session = this;
				});
		}

        protected override void OnError(object sender, ErrorEventArgs e)
        {
			// Console.WriteLine("Error: " + e.Message);
			IService service = ((WebSocket)sender).CallerContext as IService;
			service.ServiceManager.Get<ISemanticProcessor>().ProcessInstance<SocketMembrane, ServerSocketError>(msg => msg.Session = this);
		}

        protected override void OnClose(object sender, CloseEventArgs e)
        {
			// Console.WriteLine("Close: " + e.Reason);
			IService service = ((WebSocket)sender).CallerContext as IService;
			service.ServiceManager.Get<ISemanticProcessor>().ProcessInstance<SocketMembrane, ServerSocketClosed>(msg => msg.Session = this);
		}
	}
}
