using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;

using Clifton.WebInterfaces;

namespace Clifton.WebRouterService
{
	public class WebRouterModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			// serviceManager.RegisterSingleton<IPublicRouterService, PublicWebRouter>();
			serviceManager.RegisterSingleton<IAuthenticatingRouterService, AuthenticatingWebRouter>();
		}
	}

	public abstract class RouterBase : ServiceBase
	{
		public Dictionary<string, RouteInfo> Routes { get { return routes; } }

		protected Dictionary<string, RouteInfo> routes = new Dictionary<string, RouteInfo>();
	}

	/*
	public class PublicWebRouter : RouterBase, IPublicRouterService
	{
		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			ISemanticProcessor semProc = ServiceManager.Get<ISemanticProcessor>();
			semProc.Register<WebServerMembrane, PublicRouterReceptor>();
		}

		public void RegisterSemanticRoute<T>(string path) where T : SemanticRoute
		{
			routes[path] = new RouteInfo(typeof(T));
		}
	}
	*/

	public class AuthenticatingWebRouter : RouterBase, IAuthenticatingRouterService
	{
		public bool IsAuthenticatedRoute(string path)
		{
			return routes.ContainsKey(path);
		}

		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			ISemanticProcessor semProc = ServiceManager.Get<ISemanticProcessor>();
			semProc.Register<WebServerMembrane, AuthenticatingRouterReceptor>();
		}

		public void RegisterSemanticRoute<T>(string path, RouteType routeType = RouteType.PublicRoute, Role roleMask = Role.None) where T : SemanticRoute
		{
			// TODO: we set the path part to lowercase.  Kludgy.
			routes[path.LeftOf(":") + ":" + path.RightOf(":").ToLower()] = new RouteInfo(typeof(T), routeType, (uint)roleMask);
		}
	}
}
