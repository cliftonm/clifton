using System;
using System.Net;

using Clifton.SemanticProcessorInterfaces;
using Clifton.ServiceInterfaces;

namespace WebServerSemantics
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
