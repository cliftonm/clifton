using System;
using System.Collections.Generic;
using System.Data;
using System.Net;

using Clifton.ServiceInterfaces;
using Clifton.Workflow;

using WebServerInterfaces;
using WebServerSemantics;

namespace WebServerInterfaces
{
	public interface ILoginService : IService
	{
	}

	public interface IViewPersistenceService : IService
	{
		string GetViewName(string url);
		void PersistView(string prefix, string viewName, RouteType routeType = RouteType.PublicRoute, uint roleMask = 0,
			Action<Dictionary<string, object>> customInsert = null,
			Action<Dictionary<string, object>> customUpdate = null,
			Action<DataTable> customQuery = null
		);
		Action<Dictionary<string, object>> GetCustomInsertHandler(string url);
		Action<Dictionary<string, object>> GetCustomUpdateHandler(string url);
		Action<DataTable> GetCustomQueryHandler(string url);
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
