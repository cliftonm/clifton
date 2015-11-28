using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;
using Clifton.WebInterfaces;

namespace Clifton.WebServerService
{
	public class WebServerModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IWebServerService, WebServer>();
		}
	}

	/// <summary>
	/// Required service dependencies:
	///   ILoggerService
	///   ISemanticProcessor
	/// Optional:
	///   IWebWorkflowService, for setting up a workflow for pre/post processing of route/responses.
	/// </summary>
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

		/// <summary>
		/// Returns list of IP addresses assigned to localhost network devices, such as hardwired ethernet, wireless, etc.
		/// </summary>
		public List<IPAddress> GetLocalHostIPs()
		{
			IPHostEntry host;
			host = Dns.GetHostEntry(Dns.GetHostName());
			List<IPAddress> ret = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToList();

			return ret;
		}

		public virtual void Start(string ip, int port)
		{
			listener = new HttpListener();
			string url = UrlWithPort(ip, port);
			logger.Log(LogMessage.Create("Listening on " + ip + " (port " + port + ")"));
			listener.Prefixes.Add(url);

			/*
			if (useLocalIP)
			{
				List<IPAddress> localHostIPs = GetLocalHostIPs();
				localHostIPs.ForEach(localip =>
				{
					url = UrlWithPort("http://" + localip.ToString(), port);
					logger.Log(LogMessage.Create("Listening on http://" + localip + " (port " + port + ")"));
					listener.Prefixes.Add(url);
				});
			}
			*/

			// SSL Support.  TODO: Figure out a better way to do this than the convoluted approach commented out here.
			/*
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
			*/

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
				if (ServiceManager.Exists<IWebWorkflowService>())
				{
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
				else
				{
					semProc.ProcessInstance<WebServerMembrane, Route>(r => r.Context = context);
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

		protected string GetExternalIP()
		{
			string externalIP;
			externalIP = (new WebClient()).DownloadString("http://checkip.dyndns.org/");
			externalIP = (new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")).Matches(externalIP)[0].ToString();

			return externalIP;
		}
    }
}
