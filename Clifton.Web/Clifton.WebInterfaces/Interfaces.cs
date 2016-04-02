using System;
using System.Collections.Generic;
using System.Net;

using Clifton.Core.Semantics;
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

	//public interface IPublicRouterService : IService
	//{
	//	Dictionary<string, RouteInfo> Routes { get; }
	//	void RegisterSemanticRoute<T>(string path) where T : SemanticRoute;
	//}

	public interface IAuthenticatingRouterService : IService
	{
		Dictionary<string, RouteInfo> Routes { get; }
		bool IsAuthenticatedRoute(string path);
		void RegisterSemanticRoute<T>(string path, RouteType routeType = RouteType.AuthenticatedRoute, uint roleMask = 0) where T : SemanticRoute;
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
		List<IPAddress> GetLocalHostIPs();
		void Start(string ip, int[] ports);
	}

	public interface IWebWorkflowService : IService
	{
		void RegisterPreRouterWorkflow(WorkflowItem<PreRouteWorkflowData> item);
		void RegisterPostRouterWorkflow(WorkflowItem<PostRouteWorkflowData> item);
		bool PreRouter(HttpListenerContext context);
		bool PostRouter(HttpListenerContext context, HtmlResponse response);
	}
}
