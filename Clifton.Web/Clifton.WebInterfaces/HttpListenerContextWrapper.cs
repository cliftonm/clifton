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
using System.Net;
using System.Text;
using System.Web.SessionState;

using Clifton.Core.ExtensionMethods;

namespace Clifton.WebInterfaces
{
	public class HttpListenerRequestWrapper : IRequest
	{
		public NameValueCollection QueryString { get { return request.QueryString; } }
		public Uri Url {get { return request.Url; } }
		public Stream InputStream { get { return request.InputStream; } }
		public Encoding ContentEncoding { get { return request.ContentEncoding; } }
		public IPEndPoint RemoteEndPoint { get { return request.RemoteEndPoint; } }
        public bool AcceptEncoding { get { return request.Headers["Accept-Encoding"] != null; } }

        protected HttpListenerRequest request;

		public HttpListenerRequestWrapper(HttpListenerRequest request)
		{
			this.request = request;
		}
	}

	public class HttpListenerResponseWrapper : IResponse
	{
		public int StatusCode { get { return response.StatusCode; } set { response.StatusCode = value; } }
		public string ContentType { get { return response.ContentType; } set { response.ContentType = value; } }
		public Encoding ContentEncoding { get { return response.ContentEncoding; } set { response.ContentEncoding = value; } }
		public long ContentLength64 { get { return response.ContentLength64; } set { response.ContentLength64 = value; } }
		public Stream OutputStream { get { return response.OutputStream; } }

		protected HttpListenerResponse response;

		public HttpListenerResponseWrapper(HttpListenerResponse response)
		{
			this.response = response;
		}

		public void Write(string data, string contentType = "text/text", int statusCode = 200)
		{
			StatusCode = statusCode;
			ContentType = contentType;
			ContentEncoding = Encoding.UTF8;
			byte[] byteData = data.to_Utf8();
			// https://stackoverflow.com/a/35442761/2276361
			// ContentLength64 = byteData.Length;
			OutputStream.Write(byteData, 0, byteData.Length);
			Close();
		}

        public void WriteCompressed(string data, string contentType = "text/text", int statusCode = 200)
        {
            StatusCode = statusCode;
            ContentType = contentType;
            response.AddHeader("Content-Encoding", "gzip");
            byte[] byteData = data.GZip();
            ContentLength64 = byteData.Length;
            response.OutputStream.Write(byteData, 0, byteData.Length);
            Close();
        }

        public void Write(byte[] data, string contentType = "text/text", int statusCode = 200)
		{
			StatusCode = statusCode;
			ContentType = contentType;
			ContentEncoding = Encoding.UTF8;
			// https://stackoverflow.com/a/35442761/2276361
			// ContentLength64 = data.Length;
			OutputStream.Write(data, 0, data.Length);
			Close();
		}

		public void Close()
		{
			response.Close();
		}
	}

	public class HttpListenerContextWrapper : IContext
	{
		public HttpSessionState Session { get { throw new ApplicationException("Please use IWebSessionService."); } }
		public IRequest Request { get { return request; } }
		public IResponse Response { get { return response; } }
		public bool IsLocal { get { return context.Request.IsLocal; } }
		public bool IsSecureConnection { get { return context.Request.IsSecureConnection; } }

		protected HttpListenerRequestWrapper request;
		protected HttpListenerResponseWrapper response;
		protected HttpListenerContext context;

		public HttpListenerContextWrapper(HttpListenerContext context)
		{
			this.context = context;
			response = new HttpListenerResponseWrapper(context.Response);
			request = new HttpListenerRequestWrapper(context.Request);
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
