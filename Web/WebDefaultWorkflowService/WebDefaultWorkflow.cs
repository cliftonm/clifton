using System;
using System.IO;
using System.Net;

using Clifton.ExtensionMethods;
using Clifton.SemanticProcessorInterfaces;
using Clifton.ServiceInterfaces;
using Clifton.Workflow;

using WebServerInterfaces;
using WebServerSemantics;

namespace WebDefaultWorkflowService
{
	// We don't expose this interface because this service doesn't do anything other than initialize the web workflow.
	public interface IWebDefaultWorkflowService : IService { }

	public class WebWorkflowModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IWebDefaultWorkflowService, WebDefaultWorkflow>();
		}
	}

	public class WebDefaultWorkflow : ServiceBase, IWebDefaultWorkflowService
    {
		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			ServiceManager.Get<IWebWorkflowService>().RegisterPreRouterWorkflow(new WorkflowItem<PreRouteWorkflowData>(PreRouter));
			ServiceManager.Get<IWebWorkflowService>().RegisterPostRouterWorkflow(new WorkflowItem<PostRouteWorkflowData>(PostRouterInjectLayout));
			ServiceManager.Get<IWebWorkflowService>().RegisterPostRouterWorkflow(new WorkflowItem<PostRouteWorkflowData>(PostRouterProcessTokens));
		}

		protected WorkflowState PreRouter(WorkflowContinuation<PreRouteWorkflowData> wc, PreRouteWorkflowData data)
		{
			return WorkflowState.Continue;
		}

		protected WorkflowState PostRouterInjectLayout(WorkflowContinuation<PostRouteWorkflowData> wc, PostRouteWorkflowData data)
		{
			if ( (data.Context.Extension().Value.Contains(".htm")) || (String.IsNullOrEmpty(data.Context.Extension().Value)) )
			{
				string websitePath = ServiceManager.Get<IAppConfigService>().GetValue("WebsitePath");
				string text = File.ReadAllText(Path.Combine(websitePath, "_layout.html"));
				data.HtmlResponse.Html = text.Replace("<% content %>", data.HtmlResponse.Html);
			}

			return WorkflowState.Continue;
		}

		protected WorkflowState PostRouterProcessTokens(WorkflowContinuation<PostRouteWorkflowData> wc, PostRouteWorkflowData data)
		{
			data.HtmlResponse.Html = ProcessTokens(data.Context, data.HtmlResponse.Html);

			return WorkflowState.Continue;
		}

		protected string ProcessTokens(HttpListenerContext context, string html)
		{
			string newhtml = html;

			while (newhtml.Contains("@Session."))
			{
				string sessionToken = newhtml.Between("@Session.", "@");
				string defaultValue = String.Empty;

				// Does it have a default value?
				if (sessionToken.Contains("("))
				{
					defaultValue = sessionToken.Between('(', ')');
					sessionToken = sessionToken.LeftOf("(");
				}

				if (String.IsNullOrEmpty(sessionToken))
				{
					ServiceManager.Get<ILoggerService>().Log(ExceptionMessage.Create("Missing session token or bad format"));
					break;
				}
				else
				{
					string newVal = ServiceManager.Get<IWebSessionService>().GetSessionObject(context, sessionToken);
					newVal.IfNull(() => newVal = defaultValue);
					newhtml = newhtml.LeftOf('@') + newVal + newhtml.RightOf('@').RightOf('@');
				}
			}

			return newhtml;
		}

    }
}
