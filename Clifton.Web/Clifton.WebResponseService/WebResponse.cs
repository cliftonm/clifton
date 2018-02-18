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

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
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
            resp.Context.Response.ContentLength64 = resp.Exception.Message.Length;
            resp.Context.Response.Write(resp.Exception.Message, "text/text", 500);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, StringResponse resp)
		{
            resp.Context.Response.ContentLength64 = resp.Message.Length;
            resp.Context.Response.Write(resp.Message, "text/text", resp.StatusCode);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, JsonResponse resp)
		{
            resp.Context.Response.ContentLength64 = resp.Json.Length;
            resp.Context.Response.Write(resp.Json, "text/json", resp.StatusCode);
		}

        public void Process(ISemanticProcessor proc, IMembrane membrane, BinaryResponse resp)
        {
            resp.Context.Response.ContentLength64 = resp.BinaryData.Length;
            resp.Context.Response.Write(resp.BinaryData, resp.ContentType);
        }

        public void Process(ISemanticProcessor proc, IMembrane membrane, ImageResponse resp)
		{
            resp.Context.Response.ContentLength64 = resp.BinaryData.Length;
            resp.Context.Response.Write(resp.BinaryData, resp.ContentType);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, FontResponse resp)
		{
            resp.Context.Response.ContentLength64 = resp.BinaryData.Length;
            resp.Context.Response.Write(resp.BinaryData, resp.ContentType);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, DataResponse resp)
		{
            string data64 = resp.Data.ToBase64();
            resp.Context.Response.ContentLength64 = data64.Length;
			resp.Context.Response.Write(data64, "text/text", resp.StatusCode);
		}

		/// <summary>
		/// The HtmlResponse process is the only process that invokes the workflow post router for final
		/// processing of the HTML.
		/// </summary>
		public void Process(ISemanticProcessor proc, IMembrane membrane, HtmlResponse resp)
		{
			proc.ServiceManager.IfExists<IWebWorkflowService>(wws => wws.PostRouter(resp.Context, resp));
            // resp.Context.Response.ContentLength64 = resp.Html.Length;
            resp.Context.Response.Write(resp.Html, "text/html");
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, JavascriptResponse resp)
		{
            // resp.Context.Response.ContentLength64 = resp.Script.Length;
            resp.Context.Response.Write(resp.Script, "text/javascript");
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, CssResponse resp)
		{
            // resp.Context.Response.ContentLength64 = resp.Script.Length;
            resp.Context.Response.Write(resp.Script, "text/css");
		}
	}
}
