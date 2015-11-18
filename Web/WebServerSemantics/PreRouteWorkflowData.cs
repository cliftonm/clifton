using System;
using System.Net;

namespace WebServerSemantics
{
	public class PreRouteWorkflowData
	{
		public HttpListenerContext Context { get; protected set; }

		public PreRouteWorkflowData(HttpListenerContext context)
		{
			Context = context;
		}
	}
}
