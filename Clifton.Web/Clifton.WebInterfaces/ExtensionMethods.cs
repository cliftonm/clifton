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

		/// <summary>
		/// Redirect to the designated page.
		/// </summary>
		public static void Redirect(this HttpListenerContext context, string url)
		{
			// URL should not begin with a /, as we inject it here.
			// This check is made here so that if the programmer accidentally puts in a leading slash, we catch it here.
			if (url.BeginsWith("/"))
			{
				url = url.Substring(1);
			}

			url = url.Replace('\\', '/');
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;
			response.StatusCode = (int)HttpStatusCode.Redirect;
			string redirectUrl = request.Url.Scheme + "://" + request.Url.Host + "/" + url;
			response.Redirect(redirectUrl);
			response.Close();
		}
	}
}
