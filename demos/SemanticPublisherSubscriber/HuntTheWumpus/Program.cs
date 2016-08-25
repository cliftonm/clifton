using System;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.Semantics;

using Semantics;
using ServiceInterfaces;

namespace HuntTheWumpus
{
    /*
    public class ST_Foobar : SemanticRoute
    {
        public string Test
        {
            get { return Test; }
            set
            {
                Program.serviceManager.Get<ILoggerService>().Log("test parameter set to: " + value.Quote());
            }
        }
    }
    */

    static partial class Program
    {
        static void Main(string[] args)
        {
            InitializeBootstrap();
            Bootstrap((e) => Console.WriteLine(e.Message));

            serviceManager.Get<IWebServerService>().Start("127.0.0.1", 80);

            ISemanticWebRouterService router = serviceManager.Get<ISemanticWebRouterService>();
            router.Register<ST_FileResponse>("get", "foobar");

            Console.WriteLine("Press ENTER to exit the server.");
            Console.ReadLine();
        }
    }
}
