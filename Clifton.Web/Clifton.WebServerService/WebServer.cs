/* The MIT License (MIT)
* 
* Copyright (c) 2015 Marc Clifton
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

using Clifton.Core.Assertions;
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
		protected bool httpOnly;

		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			logger = ServiceManager.Get<ILoggerService>();
			semProc = ServiceManager.Get<ISemanticProcessor>();
            Assert.SilentTry(() => httpOnly = ServiceManager.Get<IAppConfigService>().GetValue("httpOnly").to_b());
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

		public virtual void Start(string ip, int[] ports)
		{
			listener = new HttpListener();

			foreach (int port in ports)
			{
				string url = IpWithPort(ip, port);
				logger.Log(LogMessage.Create("Listening on " + ip + " (port " + port + ")"));
				listener.Prefixes.Add(url);
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

				// Redirect to HTTPS if not local and not secure.
				if (!context.Request.IsLocal && !context.Request.IsSecureConnection && !httpOnly)
				{
					logger.Log(LogMessage.Create("Redirecting to HTTPS"));
					string redirectUrl = context.Request.Url.ToString().Replace("http:", "https:");
					context.Response.Redirect(redirectUrl);
					context.Response.Close();
				}
				else
				{
					string data = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding).ReadToEnd();
					NameValueCollection nvc = context.Request.QueryString;
					string nvcSerialized = new JavaScriptSerializer().Serialize(nvc.AllKeys.ToDictionary(k => k, k => nvc[k]));
					string parms = String.IsNullOrEmpty(data) ? nvcSerialized : data.LeftOf("Password");
					logger.Log(LogMessage.Create(context.Request.RemoteEndPoint.ToString() + " - [" + context.Verb().Value + ": " + context.Path().Value + "] Parameters: " + parms));

					// If the pre-router lets us continue, the route the request.
					if (ServiceManager.Exists<IWebWorkflowService>())
					{
						if (ServiceManager.Get<IWebWorkflowService>().PreRouter(context))
						{
							ProcessRoute(context, data);
						}
						else
						{
							// Otherwise just close the response.
							context.Response.Close();
						}
					}
					else
					{
						ProcessRoute(context, data);
					}
				}
			}
		}

		protected void ProcessRoute(HttpListenerContext context, string data)
		{
			semProc.ProcessInstance<WebServerMembrane, Route>(r =>
			{
				r.Context = context;
				r.Data = data;
			});
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
            //else if ((ip == "localhost") || (ip == "127.0.0.1"))
            //{
            //    ret = "http://" + ip + ":" + port.ToString() + "/";
            //}
            else
            {
                ret = "https://" + ip + ":" + port.ToString() + "/";
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
