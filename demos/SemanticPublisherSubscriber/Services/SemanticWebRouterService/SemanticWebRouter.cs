using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;

using Newtonsoft.Json;

using Clifton.Core.Utils;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;

using Semantics;
using ServiceInterfaces;

namespace SemanticWebRouterService
{
    // Struct, so it's key-able.
    public struct Route
    {
        public string Verb { get; set; }
        public string Path { get; set; }
    }

    public class SemanticWebRouterModule : IModule
    {
        public void InitializeServices(IServiceManager serviceManager)
        {
            serviceManager.RegisterSingleton<ISemanticWebRouterService, WebRouterService>();
        }
    }

    public class WebRouterService : ServiceBase, ISemanticWebRouterService
    {
        protected Dictionary<Route, Type> semanticRoutes;

        public WebRouterService()
        {
            semanticRoutes = new Dictionary<Route, Type>();
        }

        public override void FinishedInitialization()
        {
            base.FinishedInitialization2();
            ServiceManager.Get<ISemanticProcessor>().Register<WebServerMembrane, RouteProcessor>();
        }

        public void Register<T>(string verb, string path) where T : ISemanticRoute
        {
            semanticRoutes[new Route() { Verb = verb.ToLower(), Path = path.ToLower() }] = typeof(T);
        }

        public void RouteRequest(ST_HttpRequest req)
        {
            Route route = new Route() { Verb = req.Verb.ToLower(), Path = req.Path.ToLower() };
            Type routeHandler;
            bool found = semanticRoutes.TryGetValue(route, out routeHandler);
            ISemanticRoute semanticRoute = null;

            if (found)
            {
                semanticRoute = InstantiateRouteHandler(routeHandler, req);
                semanticRoute.Context = req.Context;
                ServiceManager.Get<ISemanticProcessor>().ProcessInstance<WebServerMembrane>(semanticRoute);
            }
            else
            {
                ServiceManager.Get<ILoggerService>().Log("Route not found.");
                ServiceManager.Get<ISemanticProcessor>().ProcessInstance<WebServerMembrane, ST_RouteNotFound>(r=>r.Context=req.Context);
            }
        }

        protected ISemanticRoute InstantiateRouteHandler(Type routeHandler, ST_HttpRequest req)
        {
            ISemanticRoute semanticRoute = (ISemanticRoute)Activator.CreateInstance(routeHandler);

            if (!string.IsNullOrEmpty(req.Data))
            {
                // We assume data will be in JSON format.
                JsonConvert.PopulateObject(req.Data, semanticRoute);
            }
            else if (req.Verb.ToLower() == "get")
            {
                PopulateFromQueryString(req, semanticRoute);
            }

            return semanticRoute;
        }

        protected void PopulateFromQueryString(ST_HttpRequest req, ISemanticRoute semanticRoute)
        {
            NameValueCollection nvc = req.Context.Request.QueryString;

            foreach (string key in nvc.AllKeys)
            {
                PropertyInfo pi = semanticRoute.GetType().GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (pi != null)
                {
                    object valOfType = Converter.Convert(Uri.UnescapeDataString(nvc[key].Replace('+', ' ')), pi.PropertyType);
                    pi.SetValue(semanticRoute, valOfType);
                }
            }
        }
    }

    public class RouteProcessor : IReceptor
    {
        public void Process(ISemanticProcessor semProc, IMembrane membrane, ST_HttpRequest req)
        {
            semProc.ServiceManager.Get<ISemanticWebRouterService>().RouteRequest(req);
        }
    }
}