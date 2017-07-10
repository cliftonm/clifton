/* The MIT License (MIT)
* 
* Copyright (c) 2017 Marc Clifton
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
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.SessionState;

using Clifton.Core.ExtensionMethods;

namespace Clifton.WebInterfaces
{
	public class HttpRequestWrapper : IRequest
	{
		public NameValueCollection QueryString { get { return request.QueryString; } }
		public Uri Url { get { return request.Url; } }
		public Stream InputStream { get { return request.InputStream; } }
		public Encoding ContentEncoding { get { return request.ContentEncoding; } }
		public IPEndPoint RemoteEndPoint { get
			{
				string ip = request.UserHostAddress;

				if (request.UserHostAddress == "::1")
				{
					ip = "127.0.0.1";
				}

				return new IPEndPoint(new IPAddress(ip.Split('.').Select(s => Convert.ToByte(s)).ToArray()), 0);
			}
		}

		protected HttpRequest request;

		public HttpRequestWrapper(HttpRequest request)
		{
			this.request = request;
		}
	}

	public class HttpResponseWrapper : IResponse
	{
		public int StatusCode { get { return response.StatusCode; } set { response.StatusCode = value; } }
		public string ContentType { get { return response.ContentType; } set { response.ContentType = value; } }
		public Encoding ContentEncoding { get { return response.ContentEncoding; } set { response.ContentEncoding = value; } }
		public long ContentLength64 { get { return contentLength; } set { contentLength = value; } }
		public Stream OutputStream { get { return response.OutputStream; } }

		protected HttpResponse response;
		protected long contentLength;

		public HttpResponseWrapper(HttpResponse response)
		{
			this.response = response;
		}

		public void Close()
		{
			// Never close the response from IIS.
			// response.End();
		}

		public void Write(string data, string contentType = "text/text", int statusCode = 200)
		{
			// IIS will fire the EndRequest event twice, the second time, setting header information will throw an exception.
			// This is handled by the IISService, testing for context.Response.HeadersWritten to prevent processing of the second
			// request.
			StatusCode = statusCode;
			ContentType = contentType;
			ContentEncoding = Encoding.UTF8;
			byte[] byteData = data.to_Utf8();
			response.OutputStream.Write(byteData, 0, byteData.Length);
			Close();
		}

		public void Write(byte[] data, string contentType = "text/text", int statusCode = 200)
		{
			StatusCode = statusCode;
			ContentType = contentType;
			ContentEncoding = Encoding.UTF8;
			response.OutputStream.Write(data, 0, data.Length);
			Close();
		}
	}

	public class HttpContextWrapper : IContext
	{
		public IRequest Request { get { return request; } }
		public IResponse Response { get { return response; } }
		public HttpSessionState Session { get { return context.Session; } }
		public bool IsLocal { get { return context.Request.IsLocal; } }
		public bool IsSecureConnection { get { return context.Request.IsSecureConnection; } }

		protected HttpRequestWrapper request;
		protected HttpContext context;
		protected IResponse response;

		public HttpContextWrapper(HttpContext context)
		{
			this.context = context;
			response = new HttpResponseWrapper(context.Response);
			request = new HttpRequestWrapper(context.Request);
		}

		public IPAddress EndpointAddress()
		{
			return context.EndpointAddress();
		}

		public HttpVerb Verb()
		{
			return context.Verb();
		}

		public UriPath Path()
		{
			return context.Path();
		}

		public UriExtension Extension()
		{
			return context.Extension();
		}

		public void Redirect(string url)
		{
			context.Redirect(url);
		}
	}
}
