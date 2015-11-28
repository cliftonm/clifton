using System;
using System.Net;

using Clifton.Core.ExtensionMethods;

namespace Clifton.WebInterfaces
{
	public static class WebServerExtensionMethods
	{
		/// <summary>
		/// Return the URL path.
		/// </summary>
		public static UriPath Path(this HttpListenerContext context)
		{
			return UriPath.Create(context.Request.RawUrl.LeftOf("?").RightOf("/").ToLower());
		}

		/// <summary>
		/// Returns the verb of the request: GET, POST, PUT, DELETE, and so forth.
		/// </summary>
		public static HttpVerb Verb(this HttpListenerContext context)
		{
			return HttpVerb.Create(context.Request.HttpMethod.ToUpper());
		}

		/// <summary>
		/// Return the extension for the URL path's page.
		/// </summary>
		public static UriExtension Extension(this HttpListenerContext context)
		{
			return UriExtension.Create(context.Path().Value.RightOfRightmostOf('.').ToLower());
		}

		/// <summary>
		/// Return the remote endpoint IP address.
		/// </summary>
		public static IPAddress EndpointAddress(this HttpListenerContext context)
		{
			return context.Request.RemoteEndPoint.Address;
		}
	}
}
