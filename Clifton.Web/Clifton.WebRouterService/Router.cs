using System;
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
	public class RouteInfo
	{
		public Type ReceptorSemanticType { get; protected set; }
		public RouteType RouteType { get; protected set; }
		public uint RoleMask { get; protected set; }

		/// <summary>
		/// By default, the role mask is 0: no role.
		/// The application determines how the uint bits determine role permissions.
		/// Any bits that are set with a binary "and" of the route's role mask and the current role passes the authorization test.
		/// </summary>
		public RouteInfo(Type receptorSemanticType, RouteType routeType, uint roleMask = 0)
		{
			ReceptorSemanticType = receptorSemanticType;
			RouteType = routeType;
			RoleMask = roleMask;
		}
	}

	public class WebRouterModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IRouterService, WebRouter>();
		}
	}

	public class WebRouter : ServiceBase, IRouterService
	{
		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			ISemanticProcessor semProc = ServiceManager.Get<ISemanticProcessor>();
			semProc.Register<WebServerMembrane, WebRouterReceptor>();
		}

		public void RegisterSemanticRoute(string path, Type receptorSemanticType, RouteType routeType = RouteType.PublicRoute, uint roleMask = 0)
		{
			Console.WriteLine("Registering path: " + path);
			WebRouterReceptor.routes[path] = new RouteInfo(receptorSemanticType, routeType, roleMask);
		}
	}
}
