using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace Clifton.WebInterfaces
{
	public class HttpListenerRequestWrapper : IRequest
	{
		public NameValueCollection QueryString { get { return request.QueryString; } }

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

		public void Close()
		{
			response.Close();
		}
	}

	public class HttpListenerContextWrapper : IContext
	{
		public IRequest Request { get { return request; } }
		public IResponse Response { get { return response; } }

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
