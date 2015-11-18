using System;
using System.Net;
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
			serviceManager.RegisterSingleton<IWebServerService, WebServer>();
		}
	}

    public class WebServer : ServiceBase, IWebServerService
    {
		protected HttpListener listener;
		protected ILoggerService logger;
		protected ISemanticProcessor semProc;

		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			logger = ServiceManager.Get<ILoggerService>();
			semProc = ServiceManager.Get<ISemanticProcessor>();
		}

		public virtual void Start()
		{
			IConfigService cfg = ServiceManager.Get<IConfigService>();
			listener = new HttpListener();
			string ip = cfg.GetValue("IP");
			int port = cfg.GetValue("Port").to_i();
			string url = UrlWithPort(ip, port);
			logger.Log(LogMessage.Create("Listening on " + ip + " (port " + port + ")"));
			listener.Prefixes.Add(url);

			if ( (port != 80) && (cfg.GetValue("ServeWebPages").to_b()) )
			{
				// For an Amazon EC2 instance, the listening port cannot be localhost, it has to be the non-elastic IP!
				// Firewalled on EC2, port 80 is open only for localhost testing on dev box.
				string url80 = UrlWithPort(ip, 80);
				logger.Log(LogMessage.Create("Listening on " + ip + " (port 80)"));
				listener.Prefixes.Add(url80);

				// Enable HTTPS listener
				string url443 = UrlWithPort(ip, 443);
				logger.Log(LogMessage.Create("Listening on " + ip + " (port 443)"));
				listener.Prefixes.Add(url443);

				//logger.Log(LogMessage.Create("Listening on " + ip + "  (port 80)"));
				//listener.Prefixes.Add("http://localhost/");
			}

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
				logger.Log(LogMessage.Create(context.Verb().Value + ": " + context.Path().Value));

				// If the pre-router lets us continue, the route the request.
				if (ServiceManager.Get<IWebWorkflowService>().PreRouter(context))
				{
					semProc.ProcessInstance<WebServerMembrane, Route>(r => r.Context = context);
				}
				else
				{
					// Otherwise just close the response.
					context.Response.Close();
				}
			}
		}

		/// <summary>
		/// Returns the url appended with a / for port 80, otherwise, the [url]:[port]/ if the port is not 80.
		/// </summary>
		protected string UrlWithPort(string url, int port)
		{
			string ret = url + "/";

			if (port != 80)
			{
				ret = url + ":" + port.ToString() + "/";
			}

			return ret;
		}
    }
}
