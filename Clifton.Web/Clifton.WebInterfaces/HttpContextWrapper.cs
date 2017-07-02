using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace Clifton.WebInterfaces
{
	public class HttpRequestWrapper : IRequest
	{
		public NameValueCollection QueryString { get { return request.QueryString; } }

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
			response.Close();
		}
	}

	public class HttpContextWrapper : IContext
	{
		public IRequest Request { get { return request; } }
		public IResponse Response { get { return response; } }

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
