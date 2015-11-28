using System.Net;

namespace Clifton.WebInterfaces
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