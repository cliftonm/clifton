using System;
using System.Net;

using Clifton.Core.ServiceManagement;
using Clifton.Core.Workflow;

namespace Clifton.WebInterfaces
{
    public interface IWebSocketServerService : IService
    {
		void Start(string ipAddress, int port, string path);
    }

	public interface IWebSocketSession
	{
		void Reply(string msg);
	}

	public interface IWebSocketClientService : IService
	{
		void Start(string ipAddress, int port, string path);
		void Send(string msg);
	}

	public interface ILoginService : IService
	{
	}

	public interface IRouterService : IService
	{
		void RegisterSemanticRoute(string path, Type t, RouteType routeType = RouteType.PublicRoute, uint roleMask = 0);
	}

	public interface IWebSessionService : IService
	{
		void UpdateState(HttpListenerContext context);
		void Authenticate(HttpListenerContext context);
		void Logout(HttpListenerContext context);
		string GetSessionObject(HttpListenerContext context, string objectName);
		T GetSessionObject<T>(HttpListenerContext context, string objectName);
		void SetSessionObject(HttpListenerContext context, string objectName, object val);
		SessionState GetState(HttpListenerContext context);
		bool IsAuthenticated(HttpListenerContext context);
		bool IsExpired(HttpListenerContext context);
	}

	public interface IWebResponder : IService { }

	public interface IWebFileResponse : IService
	{
		bool ProcessFileRequest(HttpListenerContext context);
	}

	public interface IWebServerService : IService
	{
		void Start();
	}

	public interface IWebWorkflowService : IService
	{
		void RegisterPreRouterWorkflow(WorkflowItem<PreRouteWorkflowData> item);
		void RegisterPostRouterWorkflow(WorkflowItem<PostRouteWorkflowData> item);
		bool PreRouter(HttpListenerContext context);
		bool PostRouter(HttpListenerContext context, HtmlResponse response);
	}
}
