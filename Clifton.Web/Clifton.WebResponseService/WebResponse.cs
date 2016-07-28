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
			// proc.ServiceManager.Get<ILoggerService>().Log(LogMessage.Create(resp.Exception.Message));
			proc.ServiceManager.Get<ISemanticProcessor>().ProcessInstance<LoggerMembrane, ST_Exception>(ex => ex.Exception = resp.Exception);
			resp.Context.Response.StatusCode = 500;
			resp.Context.Response.ContentType = "text/text";
			resp.Context.Response.ContentEncoding = Encoding.UTF8;
            byte[] byteData = resp.Exception.Message.to_Utf8();
            resp.Context.Response.ContentLength64 = byteData.Length;
			resp.Context.Response.OutputStream.Write(byteData, 0, byteData.Length);
			resp.Context.Response.Close();
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, StringResponse resp)
		{
			resp.Context.Response.StatusCode = resp.StatusCode;
			resp.Context.Response.ContentType = "text/text";
			resp.Context.Response.ContentEncoding = Encoding.UTF8;
            byte[] byteData = resp.Message.to_Utf8();
            resp.Context.Response.ContentLength64 = byteData.Length;
            resp.Context.Response.OutputStream.Write(byteData, 0, byteData.Length);
			resp.Context.Response.Close();
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, JsonResponse resp)
		{
			resp.Context.Response.StatusCode = resp.StatusCode;
			resp.Context.Response.ContentType = "text/json";
			resp.Context.Response.ContentEncoding = Encoding.UTF8;
            byte[] byteData = resp.Json.to_Utf8();
            resp.Context.Response.ContentLength64 = byteData.Length;
			resp.Context.Response.OutputStream.Write(byteData, 0, byteData.Length);
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
