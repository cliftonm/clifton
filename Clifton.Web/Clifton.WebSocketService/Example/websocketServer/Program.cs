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
        public static WebSocketServerService wsServer;

        static void Main(string[] args)
        {
            ServiceManager sm = new ServiceManager();

            SemanticProcessor semproc = new SemanticProcessor();
            semproc.Initialize(sm);
            sm.RegisterSingleton<ISemanticProcessor>(semproc);

            wsServer = new WebSocketServerService();
            wsServer.Initialize(sm);
            sm.RegisterSingleton<IWebSocketServerService>(wsServer);

            semproc.Register<SocketMembrane, ServerSocketReceiver>();
            wsServer.Start("127.0.0.1", 1000, "/wstest");

            Console.WriteLine("WS listening on 127.0.0.1:1000");
            Console.ReadLine();
            wsServer.Stop();
        }
    }

    public class ServerSocketReceiver : IReceptor
    {
        public void Process(ISemanticProcessor pool, IMembrane membrane, ServerSocketMessage msg)
        {
            Console.WriteLine("Received " + msg.Text);
            msg.Session.Reply("How are you?");
        }
    }
}
