﻿/* The MIT License (MIT)
* 
* Copyright (c) 2017 Marc Clifton
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
using System.Web;
using System.Web.Script.Serialization;

using Clifton.Core.Assertions;
using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;
using Clifton.WebInterfaces;

namespace Clifton.IISService
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
			throw new Exception("Please use Clifton.WebServerService if you want to handle the listener context directly.");
		}

		public virtual void ProcessRequest(HttpContext context)
		{
			// IIS will fire this event twice (or more?)  The HeadersWritten flag is false only on the first entry.
			// TODO: This is such a fucking kludge to handle bizarre behavior on IIS.
			// if (context.Response.HeadersWritten) return;

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
				// TODO: The removal of the password when logging is really kludgy.
				string parms = String.IsNullOrEmpty(data) ? nvcSerialized : data.LeftOf("Password").LeftOf("password");
				logger.Log(LogMessage.Create(context.Request.UserHostAddress + " - [" + context.Request.HttpMethod + ": " + context.Request.FilePath + "] Parameters: " + parms));

				IContext contextWrapper = new WebInterfaces.HttpContextWrapper(context);

				// If the pre-router lets us continue, the route the request.
				if (ServiceManager.Exists<IWebWorkflowService>())
				{
					if (ServiceManager.Get<IWebWorkflowService>().PreRouter(contextWrapper))
					{
						ProcessRoute(contextWrapper, data);
					}
					else
					{
						// Otherwise just close the response.
						context.Response.Close();
					}
				}
				else
				{
					ProcessRoute(contextWrapper, data);
				}
			}
		}

		protected void ProcessRoute(IContext context, string data)
		{
			// Must be processed on the IIS caller thread, otherwise we exit the EndRequest handler too soon.
			semProc.ProcessInstance<WebServerMembrane, Route>(r =>
			{
				r.Context = context;
				r.Data = data;
			}, true);
		}
	}
}
