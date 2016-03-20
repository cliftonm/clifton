using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;
using Clifton.Core.Workflow;

using Clifton.WebInterfaces;

namespace Clifton.WebDefaultWorkflowService
{
	// We don't expose this interface because this service doesn't do anything other than initialize the web workflow.
	public interface IWebDefaultWorkflowService : IService { }

	public class WebWorkflowModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IWebDefaultWorkflowService, WebDefaultWorkflow>();
		}
	}

	public class WebDefaultWorkflow : ServiceBase, IWebDefaultWorkflowService
	{
		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			ServiceManager.Get<IWebWorkflowService>().RegisterPreRouterWorkflow(new WorkflowItem<PreRouteWorkflowData>(PreRouter));
			
			// Only called for HTML responses:
			ServiceManager.Get<IWebWorkflowService>().RegisterPostRouterWorkflow(new WorkflowItem<PostRouteWorkflowData>(PostRouterInjectLayout));
			ServiceManager.Get<IWebWorkflowService>().RegisterPostRouterWorkflow(new WorkflowItem<PostRouteWorkflowData>(PostRouterRendering));
			ServiceManager.Get<IWebWorkflowService>().RegisterPostRouterWorkflow(new WorkflowItem<PostRouteWorkflowData>(PostRouterProcessTokens));
		}

		protected WorkflowState PreRouter(WorkflowContinuation<PreRouteWorkflowData> wc, PreRouteWorkflowData data)
		{
			return WorkflowState.Continue;
		}

		protected WorkflowState PostRouterRendering(WorkflowContinuation<PostRouteWorkflowData> wc, PostRouteWorkflowData data)
		{
			string render = data.HtmlResponse.Html.Between("<!-- Render:", "-->");

			if (!String.IsNullOrEmpty(render))
			{
				// Remove the render metadata code.
				data.HtmlResponse.Html = data.HtmlResponse.Html.LeftOf("<!-- Render:") + data.HtmlResponse.Html.RightOf("<!-- Render:").RightOf("-->");
				List<string> renderCmds = render.Split("\r\n".ToCharArray()).Where(s => !String.IsNullOrEmpty(s.Trim())).Select(s => s.Trim()).ToList();
				data.HtmlResponse.Html = ProcessRenderingCommands(data.Context, data.HtmlResponse.Html, renderCmds);
			}

			return WorkflowState.Continue;
		}

		// Very bare bones for now!
		protected string ProcessRenderingCommands(HttpListenerContext context, string html, List<string> renderCmds)
		{
			string ret = html;

			foreach (string cmdLine in renderCmds)
			{
				string cmd = cmdLine.LeftOf("(");
				string args = cmdLine.RightOf("(").LeftOfRightmostOf(")");
				string id = args.LeftOf(",").Trim();
				string fncWithParms = args.RightOf(",").Trim();
				string fnc = fncWithParms.LeftOf("(");
				string fncParms = fncWithParms.Between("(", ")");
				bool notResult = fnc[0]=='!';

				if (notResult)
				{
					fnc=fnc.Substring(1);
				}

				bool fncRet = false;

				switch (fnc.ToLower())
				{
					case "role":
						fncRet = false; // ServiceManager.Get<IWebSessionService>().IsAuthenticated(context);
						break;

					case "authenticated":
						fncRet = ServiceManager.Get<IWebSessionService>().IsAuthenticated(context);
						break;
				}

				if (notResult)
				{
					fncRet = !fncRet;
				}

				if (fncRet)
				{
					switch (cmd.ToLower())
					{
						case "remove":
							ret = RemoveElementWithId(ret, id);
							break;
					}
				}
			}

			return ret;
		}

		// Very bare bones for now!
		protected string RemoveElementWithId(string html, string id)
		{
			int idx = html.IndexOf(id);

			if (idx != -1)
			{
				// scan left until we encounter <
				while (html[idx] != '<') --idx;

				++idx;		// ignore <
				int startTagIdx = idx;

				// scan right until we encounter ' ' (we know the tag doesn't end in > because it has an id attribute)
				while (html[idx] != ' ') ++idx;

				string tag = html.Substring(startTagIdx, idx - startTagIdx);

				// For dealing with nesting.
				string startTagOption1 = "<" + tag + " ";
				string startTagOption2 = "<" + tag + ">";

				// TODO: no support for tags that don't end in this format.
				// Is nesting implemented correctly?
				string endTag = "</" + tag + ">";
				int endTagIdx = startTagIdx + tag.Length;
				int nestLevel = 0;
				string remainder;

				// Nested tag support.
				do
				{
					remainder = html.Substring(endTagIdx);

					if (remainder.BeginsWith(startTagOption1) || remainder.BeginsWith(startTagOption2))
					{
						++nestLevel;
					}
					else if (remainder.BeginsWith(endTag))
					{
						--nestLevel;
					}

					++endTagIdx;

				} while ( (!remainder.StartsWith(endTag)) || (nestLevel >= 0) );
				
				string htmlLeft = html.Substring(0, startTagIdx - 1);
				string htmlRight = html.Substring(endTagIdx + endTag.Length - 1);

				html = htmlLeft + htmlRight;
			}

			return html;
		}

		protected WorkflowState PostRouterInjectLayout(WorkflowContinuation<PostRouteWorkflowData> wc, PostRouteWorkflowData data)
		{
			string websitePath = ServiceManager.Get<IAppConfigService>().GetValue("WebsitePath");
			string text = File.ReadAllText(Path.Combine(websitePath, "Layout\\_layout.html"));
			data.HtmlResponse.Html = text.Replace("<% content %>", data.HtmlResponse.Html);

			return WorkflowState.Continue;
		}

		protected WorkflowState PostRouterProcessTokens(WorkflowContinuation<PostRouteWorkflowData> wc, PostRouteWorkflowData data)
		{
			data.HtmlResponse.Html = ProcessTokens(data.Context, data.HtmlResponse.Html);

			return WorkflowState.Continue;
		}

		protected string ProcessTokens(HttpListenerContext context, string html)
		{
			string newhtml = html;

			while (newhtml.Contains("@Session."))
			{
				string sessionToken = newhtml.Between("@Session.", "@");
				string defaultValue = String.Empty;

				// Does it have a default value?
				if (sessionToken.Contains("("))
				{
					defaultValue = sessionToken.Between('(', ')');
					sessionToken = sessionToken.LeftOf("(");
				}

				if (String.IsNullOrEmpty(sessionToken))
				{
					ServiceManager.Get<ILoggerService>().Log(ExceptionMessage.Create("Missing session token or bad format"));
					break;
				}
				else
				{
					string newVal = ServiceManager.Get<IWebSessionService>().GetSessionObject(context, sessionToken);
					newVal.IfNull(() => newVal = defaultValue);
					newhtml = newhtml.LeftOf('@') + newVal + newhtml.RightOf('@').RightOf('@');
				}
			}

			newhtml = Replace(newhtml, "@CurrentYear@", (src, token) => src.Replace(token, DateTime.Now.Year.ToString()));

			// TODO: Get from application config.
			newhtml = Replace(newhtml, "@AppName@", (src, token) => src.Replace(token, "ByteStruck"));

			return newhtml;
		}

		protected string Replace(string html, string token, Func<string, string, string> replaceWith)
		{
			string newhtml = html;

			while (newhtml.Contains(token))
			{
				newhtml = replaceWith(newhtml, token);
			}

			return newhtml;
		}
	}
}
