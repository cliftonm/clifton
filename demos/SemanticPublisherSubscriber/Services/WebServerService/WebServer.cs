using System.IO;
using System.Net;
using System.Threading.Tasks;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;

using Semantics;
using ServiceInterfaces;

namespace WebServerService
{
    public class WebServerModule : IModule
    {
        public void InitializeServices(IServiceManager serviceManager)
        {
            serviceManager.RegisterSingleton<IWebServerService, WebServer>();
        }
    }

    public class WebServer : ServiceBase, IWebServerService
    {
        protected HttpListener listener;
        protected ILoggerService logger;
        protected ISemanticProcessor semProc;
        protected bool httpOnly;

        public virtual void Start(string ip, int port)
        {
            logger = ServiceManager.Get<ILoggerService>();
            semProc = ServiceManager.Get<ISemanticProcessor>();
            listener = new HttpListener();
            string url = IpWithPort(ip, port);
            logger.Log("Listening on " + ip + ":" + port);
            listener.Prefixes.Add(url);

            listener.Start();
            Task.Run(() => WaitForConnection(listener));
        }

        protected virtual void WaitForConnection(object objListener)
        {
            HttpListener listener = (HttpListener)objListener;

            while (true)
            {
                // Wait for a connection.  Return to caller while we wait.
                HttpListenerContext context = listener.GetContext();
                string verb = context.Request.HttpMethod;
                string path = context.Request.RawUrl.LeftOf("?").RightOf("/");
                logger.Log(verb + ": " + path);

                string data = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding).ReadToEnd();
                ServiceManager.Get<ISemanticProcessor>().ProcessInstance<WebServerMembrane, ST_HttpRequest>(r =>
                {
                    r.Context = context;
                    r.Verb = verb;
                    r.Path = path;
                    r.Data = data;
                });
            }
        }

        /// <summary>
        /// Returns the url appended with a / for port 80, otherwise, the [url]:[port]/ if the port is not 80.
        /// </summary>
        protected string IpWithPort(string ip, int port)
        {
            string ret;

            if (port == 80)
            {
                ret = "http://" + ip + "/";
            }
            else
            {
                ret = "http://" + ip + ":" + port.ToString() + "/";
            }

            return ret;
        }
    }
}