using System;

using Clifton.Core.Semantics;
using Clifton.Core.Services.SemanticProcessorService;
using Clifton.Core.ServiceManagement;
using Clifton.Core.Web.WebSocketService;
using Clifton.WebInterfaces;

namespace websocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceManager sm = new ServiceManager();

            SemanticProcessor semproc = new SemanticProcessor();
            semproc.Initialize(sm);
            sm.RegisterSingleton<ISemanticProcessor>(semproc);

            var wsClient = new WebSocketClientService();
            wsClient.Initialize(sm);
            sm.RegisterSingleton<IWebSocketClientService>(wsClient);

            semproc.Register<SocketMembrane, ServerSocketReceiver>();

            // wsClient.Start("ws://127.0.0.1", 1000, "/wstest");
			wsClient.Start("ws://24.105.201.179", 31415, "/wstest");
			// wsClient.Start("ws://emsserver.club", 443, "/wstest");


			// wsClient.Start("wss://24.105.201.179", 443, "/wstest");
			// wsClient.Start("wss://emsserver.club", 443, "/wstest");

			// Console.WriteLine("WS client connected to 127.0.0.1:1000");
			wsClient.Send("Hello World!");
            Console.ReadLine();
        }
    }

    public class ServerSocketReceiver : IReceptor
    {
        public void Process(ISemanticProcessor pool, IMembrane membrane, ClientSocketMessage msg)
        {
            Console.WriteLine("Received " + msg.Text);
        }
    }
}
