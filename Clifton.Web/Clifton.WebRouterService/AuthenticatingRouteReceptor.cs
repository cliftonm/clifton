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
using System.Reflection;

using Newtonsoft.Json;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.Semantics;
using Clifton.Core.Utils;
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
	public class AuthenticatingRouterReceptor : IReceptor
	{
		public void Process(ISemanticProcessor proc, IMembrane membrane, Route route)
		{
			IAuthenticatingRouterService routerService = proc.ServiceManager.Get<IAuthenticatingRouterService>();
			IContext context = route.Context;
			HttpVerb verb = context.Verb();
			UriPath path = context.Path();

			string searchRoute = GetSearchRoute(verb, path);
			string data = route.Data;
			RouteInfo routeInfo;

			IPAddress addr = context.Request.RemoteEndPoint.Address;
			string ip = addr.ToString();

			// Handle localhost format.
			if (ip == "::1")
			{
				addr = new IPAddress(new byte[] { 127, 0, 0, 1 });
			}

			Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss tt ") + "IP: " + addr.ToString() + "    URL: " + route.Context.Request.Url);

			// TODO: Session manager may not exist.  How do we handle services that are missing?
			IWebSessionService session = proc.ServiceManager.Get<IWebSessionService>();

			// Semantic routes can be either public or authenticated.
			if (routerService.Routes.TryGetValue(searchRoute, out routeInfo))
			{
				// Public routes always authenticate.
				bool authenticatedRoute = true;
				bool authorizedRoute = true;

				if (routeInfo.RouteType == RouteType.AuthenticatedRoute)
				{
					session.UpdateState(context);
					authenticatedRoute = session.IsAuthenticated(context);
				}

				if (routeInfo.RouteType == RouteType.RoleRoute)
				{
					session.UpdateState(context);
					authenticatedRoute = session.IsAuthenticated(context);

					// User must be authenticated and have the correct role setting.
					if (authenticatedRoute)
					{
						// Any bits that are set with a binary "and" of the route's role mask and the current role passes the authorization test.	
						uint mask = session.GetSessionObject<uint>(context, "RoleMask");
						authorizedRoute = (mask & routeInfo.RoleMask) != 0;
					}
				}

				if (authenticatedRoute)     // user is authenticated
				{
					session.UpdateLastTransaction(context);
				}

				if (authenticatedRoute && authorizedRoute)
				{
					Type receptorSemanticType = routeInfo.ReceptorSemanticType;
					SemanticRoute semanticRoute = (SemanticRoute)Activator.CreateInstance(receptorSemanticType);
					semanticRoute.PostData = data;

					if (!String.IsNullOrEmpty(data))
					{
                        // Is it JSON?
                        // NOTE: "JSON" is passed in as a string, not object.  So this is what it looks like in the Javascript:
                        // $.post("/geeks/createProfile", '{ "profileName": "foobar" }'
                        // Note the surrounding ' marks
                        if (data[0] == '{')
                        {
                            JsonConvert.PopulateObject(data, semanticRoute);
                            SetUrlParameters(context.Request.Url.ToString(), semanticRoute, receptorSemanticType);
                        }
                        else if (MultiPartParser.IsMultiPart(data))
                        {
                            MultiPartParser.ContentType ct = MultiPartParser.GetContentType(data);
                            string content = MultiPartParser.GetContent(data);

                            if (!(semanticRoute is IFileUpload))
                            {
                                throw new RouterException("Semantic route class must implement IFileUpload");
                            }

                            ((IFileUpload)semanticRoute).Content = content;
                        }
                        else
                        {
                            // Instead here, the data is passed in as an object, which comes in as params.  The Javascript for this looks like:
                            // $.post("/geeks/createProfile", { "profileName": profileName }
                            // Note the lack of surrounding ' around the { }
                            // Example: "username=sdfsf&password=sdfsdf&LoginButton=Login"
                            // Use $.post(url, JSON.stringify(data) to convert to JSON
                            string[] parms = data.Split('&');

							foreach (string parm in parms)
							{
								string[] keyVal = parm.Split('=');
								PropertyInfo pi = receptorSemanticType.GetProperty(keyVal[0], BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

								if (pi != null)
								{
									// TODO: Should handling of "+" be before or after the UnescapedDataString call?
									object valOfType = Converter.Convert(Uri.UnescapeDataString(keyVal[1].Replace('+', ' ')), pi.PropertyType);
									pi.SetValue(semanticRoute, valOfType);
								}
							}
						}
					}
					else if (verb.Value == "GET")
					{
						SetUrlParameters(context.Request.Url.ToString(), semanticRoute, receptorSemanticType);
					}

					// Must be done AFTER populating the object -- sometimes the json converter nulls the base class!
					semanticRoute.Context = context;
					// TODO: Why are we doing this on the caller thread, except for debugging???
					proc.ProcessInstance<WebServerMembrane>(semanticRoute, true);
				}
				else
				{
					// Deal with expired or requires authentication.
					switch (session.GetState(context))
					{
						case SessionState.New:
							// TODO: Oh man, this is application specific!!!
							session.SetSessionObject(context, "OneTimeBadAlert", "Please Sign In");
							context.Redirect("/account/login");
							//proc.ProcessInstance<WebServerMembrane, StringResponse>(r =>
							//{
							//	r.Context = context;
							//	r.Message = "authenticationRequired";		// used in clifton.spa.js to handle SPA error responses
							//	r.StatusCode = 403;
							//});
							break;

						case SessionState.Authenticated:
							proc.ProcessInstance<WebServerMembrane, StringResponse>(r =>
							{
								r.Context = context;
								r.Message = "notAuthorized";				// used in clifton.spa.js to handle SPA error responses
								r.StatusCode = 401;
							});
							break;

						case SessionState.Expired:
							session.SetSessionObject(context, "OneTimeBadAlert", "Session expired.  Please sign in again.");
							context.Redirect("/account/login");
							//proc.ProcessInstance<WebServerMembrane, StringResponse>(r =>
							//{
							//	r.Context = context;
							//	r.Message = "sessionExpired";				// used in clifton.spa.js to handle SPA error responses
							//	r.StatusCode = 401;
							//});
							break;
					}
				}
			}
			else
			{
				// proc.ProcessInstance<LoggerMembrane, ST_Log>(msg => msg.Message = "Using default handler: " + verb.Value + ": " + path.Value);
				// Put the context on the bus for some service to pick up.
				// All unhandled context are assumed to be public routes.
				proc.ProcessInstance<WebServerMembrane, UnhandledContext>(c => c.Context = context);
			}
		}

		protected string GetSearchRoute(HttpVerb verb, UriPath path)
		{
			return verb.Value + ":" + path.Value;
		}

		protected void SetUrlParameters(string url, SemanticRoute semanticRoute, Type receptorSemanticType)
		{
			// Parse parameters
			// NameValueCollection nvc = context.Request.QueryString;
			// We process the parameters ourselves because we may have to convert URL formatting, like %3D, to their original characters, like '='
			Dictionary<string, string> keyValues = new Dictionary<string, string>();
			string urlParams = Uri.UnescapeDataString(url.RightOf('?')).Replace('+', ' ');
			string[] kv = urlParams.Split('&');         // split by params

			foreach (string kvparam in kv)
			{
				string[] kv2 = kvparam.Split('=');

				if (kv2.Length == 2 && !String.IsNullOrEmpty(kv2[0]))
				{
					string key = kv2[0];
					string val = kv2[1];
					PropertyInfo pi = null;

					try
					{
						pi = receptorSemanticType.GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
					}
					catch (Exception ex)
					{

					}

					if (pi != null)
					{
						// pi.SetValue(semanticRoute, Uri.UnescapeDataString(nvc[key].Replace('+', ' ')));
						// TODO: Should handling of "+" be before or after the UnescapedDataString call?
						object valOfType = Converter.Convert(val, pi.PropertyType);
						pi.SetValue(semanticRoute, valOfType);
					}
				}
			}
		}
	}
}
