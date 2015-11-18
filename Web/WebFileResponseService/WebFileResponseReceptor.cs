using System;
using System.Net;
using System.Text;

using Clifton.ExtensionMethods;
using Clifton.SemanticProcessorInterfaces;
using Clifton.ServiceInterfaces;

using WebServerInterfaces;
using WebServerSemantics;

namespace WebFileResponseService
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

		/// <summary>
		/// A specific HtmlPageRoute -- these exist because some pages require that the user be authenticated, so the route is explicitly
		/// instantiated with the HtmlPageRoute semantic type.  For example, in the router initialization:
		/// router.RegisterSemanticRoute("GET:foo/foo", typeof(HtmlPageRoute), RouteType.AuthenticatedRoute);
		/// </summary>
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
