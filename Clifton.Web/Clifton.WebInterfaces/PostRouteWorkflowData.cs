using System;
using System.Net;

using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;

namespace Clifton.WebInterfaces
{
	public class PostRouteWorkflowData
	{
		public IServiceManager ServiceManager { get; protected set; }
		public HttpListenerContext Context { get; protected set; }
		public HtmlResponse HtmlResponse { get; protected set; }

		public PostRouteWorkflowData(IServiceManager serviceManager, HttpListenerContext context, HtmlResponse htmlResponse)
		{
			ServiceManager = serviceManager;
			Context = context;
			HtmlResponse = htmlResponse;
		}
	}
}
