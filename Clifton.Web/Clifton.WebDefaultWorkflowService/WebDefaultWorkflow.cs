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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;
using Clifton.Core.TemplateEngine;
using Clifton.Core.Workflow;

using Clifton.WebInterfaces;

namespace Clifton.WebDefaultWorkflowService
{
	public class WebWorkflowModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IWebDefaultWorkflowService, WebDefaultWorkflow>();
		}
	}

	public class WebDefaultWorkflow : ServiceBase, IWebDefaultWorkflowService
	{
		protected TemplateEngine templateEngine;
		protected Dictionary<string, object> appTemplateObjects;

		public WebDefaultWorkflow()
		{
			appTemplateObjects = new Dictionary<string, object>();
		}

		public void RegisterAppTemplateObject(string name, object obj)
		{
			appTemplateObjects[name] = obj;
		}

		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			InitializeTemplateEngine();

			ServiceManager.Get<IWebWorkflowService>().RegisterPreRouterWorkflow(new WorkflowItem<PreRouteWorkflowData>(PreRouter));
			
			// Only called for HTML responses:
			ServiceManager.Get<IWebWorkflowService>().RegisterPostRouterWorkflow(new WorkflowItem<PostRouteWorkflowData>(PostRouterInjectLayout));
			ServiceManager.Get<IWebWorkflowService>().RegisterPostRouterWorkflow(new WorkflowItem<PostRouteWorkflowData>(PostRouterRendering));
		}

		protected void InitializeTemplateEngine()
		{
			templateEngine = new TemplateEngine(ServiceManager.Get<ISemanticProcessor>());
			templateEngine.UsesDynamic();
		}

		protected WorkflowState PreRouter(WorkflowContinuation<PreRouteWorkflowData> wc, PreRouteWorkflowData data)
		{
			return WorkflowState.Continue;
		}

		protected WorkflowState PostRouterRendering(WorkflowContinuation<PostRouteWorkflowData> wc, PostRouteWorkflowData data)
		{
			string template = data.HtmlResponse.Html;
			IWebSessionService sessionSvc = ServiceManager.Get<IWebSessionService>();
			List<string> objectNames = new List<string>() { "session", "context", "cfg" };
			List<object> objects = new List<object>() { sessionSvc, data.Context, ServiceManager.Get<IAppConfigService>() };
			objectNames.AddRange(appTemplateObjects.Keys);
			objects.AddRange(appTemplateObjects.Values);
			try
			{
				string newHtml = templateEngine.Parse(template, objectNames.ToArray(), objects.ToArray());
				data.HtmlResponse.Html = newHtml;
			}
			catch (Exception ex)
			{
				// ServiceManager.Get<ILoggerService>().Log(ex);
				ServiceManager.Get<ISemanticProcessor>().ProcessInstance<LoggerMembrane, ST_Exception>(ex2 => ex2.Exception = ex);
			}

			return WorkflowState.Continue;
		}

		protected WorkflowState PostRouterInjectLayout(WorkflowContinuation<PostRouteWorkflowData> wc, PostRouteWorkflowData data)
		{
			string websitePath = ServiceManager.Get<IAppConfigService>().GetValue("WebsitePath");
			string text = File.ReadAllText(Path.Combine(websitePath, "Layout\\_layout.html"));
			data.HtmlResponse.Html = text.Replace("<% content %>", data.HtmlResponse.Html);

			return WorkflowState.Continue;
		}

		protected string Replace(string html, string token, Func<string, string, string> replaceWith)
		{
			string newhtml = html;

			while (newhtml.Contains(token))
			{
				newhtml = replaceWith(newhtml, token);
			}

			return newhtml;
		}
	}
}
