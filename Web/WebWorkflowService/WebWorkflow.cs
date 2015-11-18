using System;
using System.IO;
using System.Net;

using Clifton.ExtensionMethods;
using Clifton.SemanticProcessorInterfaces;
using Clifton.ServiceInterfaces;
using Clifton.Workflow;

using WebServerInterfaces;
using WebServerSemantics;

namespace WebWorkflowService
{
	public class WebWorkflowModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IWebWorkflowService, WebWorkflow>();
		}
	}

	public class WebWorkflow : ServiceBase, IWebWorkflowService
	{
		protected Workflow<PreRouteWorkflowData> preRouteWorkflow;
		protected Workflow<PostRouteWorkflowData> postRouteWorkflow;

		public WebWorkflow()
		{
			preRouteWorkflow = new Workflow<PreRouteWorkflowData>();
			postRouteWorkflow = new Workflow<PostRouteWorkflowData>();
		}

		public void RegisterPreRouterWorkflow(WorkflowItem<PreRouteWorkflowData> item)
		{
			preRouteWorkflow.AddItem(item);
		}

		public void RegisterPostRouterWorkflow(WorkflowItem<PostRouteWorkflowData> item)
		{
			postRouteWorkflow.AddItem(item);
		}

		public bool PreRouter(HttpListenerContext context)
		{
			WorkflowState state = preRouteWorkflow.Execute(new PreRouteWorkflowData(context));

			return state == WorkflowState.Done;
		}

		public bool PostRouter(HttpListenerContext context, HtmlResponse response)
		{
			WorkflowState state = postRouteWorkflow.Execute(new PostRouteWorkflowData(ServiceManager, context, response));

			return state == WorkflowState.Done;
		}
	}
}

