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

namespace Clifton.WebFileResponseService
{
	public class WebFileResponseReceptor : IReceptor
	{
		public void Process(ISemanticProcessor proc, IMembrane membrane, UnhandledContext unhandledContext)
		{
			bool handled = proc.ServiceManager.Get<IWebFileResponse>().ProcessFileRequest(unhandledContext.Context);

			if (!handled)
			{
				RouteNotFoundResponse(proc, unhandledContext.Context);
			}
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, HtmlPageRoute context)
		{
			bool handled = proc.ServiceManager.Get<IWebFileResponse>().ProcessFileRequest(context.Context);

			if (!handled)
			{
				RouteNotFoundResponse(proc, context.Context);
			}
		}

		protected virtual void RouteNotFoundResponse(ISemanticProcessor proc, HttpListenerContext context)
		{
			proc.ProcessInstance<WebServerMembrane, StringResponse>(r =>
			{
				r.Context = context;
				r.Message = "Route not found";
				r.StatusCode = 500;
			});
		}
	}
}
