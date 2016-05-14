using System;
using System.Net;
using System.Text;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;

using Clifton.WebInterfaces;

namespace Clifton.WebResponseService
{
	public class WebResponseModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IWebResponder, WebResponder>();
		}
	}

	public class WebResponder : ServiceBase, IWebResponder
	{
		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			ISemanticProcessor semProc = ServiceManager.Get<ISemanticProcessor>();
			semProc.Register<WebServerMembrane, ResponderReceptor>();
		}
	}

	public class ResponderReceptor : IReceptor
	{
		public void Process(ISemanticProcessor proc, IMembrane membrane, ExceptionResponse resp)
		{
			proc.ServiceManager.Get<ILoggerService>().Log(LogMessage.Create(resp.Exception.Message));
			resp.Context.Response.StatusCode = 500;
			resp.Context.Response.ContentType = "text/text";
			resp.Context.Response.ContentEncoding = Encoding.UTF8;
			resp.Context.Response.ContentLength64 = resp.Exception.Message.Length;
			resp.Context.Response.OutputStream.Write(resp.Exception.Message.to_Utf8(), 0, resp.Exception.Message.Length);
			resp.Context.Response.Close();
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, StringResponse resp)
		{
			resp.Context.Response.StatusCode = resp.StatusCode;
			resp.Context.Response.ContentType = "text/text";
			resp.Context.Response.ContentEncoding = Encoding.UTF8;
			resp.Context.Response.ContentLength64 = resp.Message.Length;
			resp.Context.Response.OutputStream.Write(resp.Message.to_Utf8(), 0, resp.Message.Length);
			resp.Context.Response.Close();
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, JsonResponse resp)
		{
			resp.Context.Response.StatusCode = resp.StatusCode;
			resp.Context.Response.ContentType = "text/json";
			resp.Context.Response.ContentEncoding = Encoding.UTF8;
			resp.Context.Response.ContentLength64 = resp.Json.Length;
			resp.Context.Response.OutputStream.Write(resp.Json.to_Utf8(), 0, resp.Json.Length);
			resp.Context.Response.Close();
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, ImageResponse resp)
		{
			resp.Context.Response.ContentType = resp.ContentType;
			resp.Context.Response.ContentEncoding = Encoding.UTF8;
			resp.Context.Response.ContentLength64 = resp.BinaryData.Length;
			resp.Context.Response.OutputStream.Write(resp.BinaryData, 0, resp.BinaryData.Length);
			resp.Context.Response.Close();
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, FontResponse resp)
		{
			resp.Context.Response.ContentType = resp.ContentType;
			resp.Context.Response.ContentEncoding = Encoding.UTF8;
			resp.Context.Response.ContentLength64 = resp.BinaryData.Length;
			resp.Context.Response.OutputStream.Write(resp.BinaryData, 0, resp.BinaryData.Length);
			resp.Context.Response.Close();
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, DataResponse resp)
		{
			byte[] utf8data = resp.Data.ToBase64().to_Utf8();
			resp.Context.Response.StatusCode = resp.StatusCode;
			resp.Context.Response.ContentEncoding = Encoding.UTF8;
			resp.Context.Response.ContentLength64 = utf8data.Length;
			resp.Context.Response.OutputStream.Write(utf8data, 0, utf8data.Length);
			resp.Context.Response.Close();
		}

		/// <summary>
		/// The HtmlResponse process is the only process that invokes the workflow post router for final
		/// processing of the HTML.
		/// </summary>
		public void Process(ISemanticProcessor proc, IMembrane membrane, HtmlResponse resp)
		{
			proc.ServiceManager.IfExists<IWebWorkflowService>(wws => wws.PostRouter(resp.Context, resp));
			byte[] utf8data = resp.Html.to_Utf8();
			resp.Context.Response.ContentType = "text/html";
			resp.Context.Response.ContentEncoding = Encoding.UTF8;
			resp.Context.Response.ContentLength64 = utf8data.Length;
			resp.Context.Response.OutputStream.Write(utf8data, 0, utf8data.Length);
			resp.Context.Response.Close();
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, JavascriptResponse resp)
		{
			byte[] utf8data = resp.Script.to_Utf8();
			resp.Context.Response.ContentType = "text/javascript";
			resp.Context.Response.ContentEncoding = Encoding.UTF8;
			resp.Context.Response.ContentLength64 = utf8data.Length;
			resp.Context.Response.OutputStream.Write(utf8data, 0, utf8data.Length);
			resp.Context.Response.Close();
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, CssResponse resp)
		{
			byte[] utf8data = resp.Script.to_Utf8();
			resp.Context.Response.ContentType = "text/css";
			resp.Context.Response.ContentEncoding = Encoding.UTF8;
			resp.Context.Response.ContentLength64 = utf8data.Length;
			resp.Context.Response.OutputStream.Write(utf8data, 0, utf8data.Length);
			resp.Context.Response.Close();
		}
	}
}
