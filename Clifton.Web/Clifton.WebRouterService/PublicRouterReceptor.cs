using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;
using Clifton.WebInterfaces;

namespace Clifton.WebRouterService
{
	/// <summary>
	/// Maps a route to the semantic type that processes that route.
	/// The route key is the verb and path, the route dictionary value is the semantic type to instantiate
	/// for that route.  We assume here that the semantic type will be populated from deserialized JSON data
	/// that is in the request input stream.
	/// If the route key is not found in the web service route dictionary, then the UnhandledContext semantic
	/// type is instantiated on the WebServerMembrane bus.  This is (default) handled in the WebFileResponseService module,
	/// by the UnhandledContext receptor.
	/// </summary>
	public class PublicRouterReceptor : IReceptor
	{
		public void Process(ISemanticProcessor proc, IMembrane membrane, Route route)
		{
			IPublicRouterService routerService = proc.ServiceManager.Get<IPublicRouterService>();
			HttpListenerContext context = route.Context;
			HttpVerb verb = context.Verb();
			UriPath path = context.Path();
			string searchRoute = GetSearchRoute(verb, path);
			RouteInfo routeInfo;

			// Semantic routes can be either public or authenticated.
			if (routerService.Routes.TryGetValue(searchRoute, out routeInfo))
			{
				string data = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding).ReadToEnd();
				Type receptorSemanticType = routeInfo.ReceptorSemanticType;
				SemanticRoute semanticRoute = (SemanticRoute)Activator.CreateInstance(receptorSemanticType);
				semanticRoute.PostData = data;

				if (!String.IsNullOrEmpty(data))
				{
					// Is it JSON?
					if (data[0] == '{')
					{
						JsonConvert.PopulateObject(data, semanticRoute);
					}
					else
					{
						// Example: "username=sdfsf&password=sdfsdf&LoginButton=Login"
						string[] parms = data.Split('&');

						foreach (string parm in parms)
						{
							string[] keyVal = parm.Split('=');
							PropertyInfo pi = receptorSemanticType.GetProperty(keyVal[0], BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

							if (pi != null)
							{
								// TODO: Convert to property type.
								// TODO: value needs to be re-encoded to handle special characters.
								pi.SetValue(semanticRoute, keyVal[1]);
							}
						}
					}
				}
				else if (verb.Value == "GET")
				{
					// Parse parameters
					NameValueCollection nvc = context.Request.QueryString;

					foreach(string key in nvc.AllKeys)
					{
						PropertyInfo pi = receptorSemanticType.GetProperty(key, BindingFlags.Public | BindingFlags.Instance);

						if (pi != null)
						{
							// TODO: Convert to property type.
							// TODO: value needs to be re-encoded to handle special characters.
							pi.SetValue(semanticRoute, nvc[key]);
						}
					}
				}

				// Must be done AFTER populating the object -- sometimes the json converter nulls the base class!
				semanticRoute.Context = context;
				proc.ProcessInstance<WebServerMembrane>(semanticRoute, true);		// TODO: Why execute on this thread?
			}
			else if (verb.Value == "GET")
			{
				// Only issue the UnhandledContext if this is not an authenticated route.
				if (!proc.ServiceManager.Get<IAuthenticatingRouterService>().IsAuthenticatedRoute(searchRoute))
				{
					// Put the context on the bus for some service to pick up.
					// All unhandled context are assumed to be public routes.
					proc.ProcessInstance<WebServerMembrane, UnhandledContext>(c => c.Context = context);
				}
			}
			else
			{
				proc.ProcessInstance<WebServerMembrane, ExceptionResponse>(e =>
					{
						e.Context = context;
						e.Exception = new Exception("Route " + searchRoute + " not defined.");
					});
			}
		}

		protected string GetSearchRoute(HttpVerb verb, UriPath path)
		{
			return verb.Value + ":" + path.Value;
		}
	}
}
