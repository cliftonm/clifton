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
