using System.Text;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;

using Semantics;

namespace WebResponseService
{
    // Here we create a placeholder, because this service is not actually exposed.
    // All activities are handled as a subscriber.
    public interface IWebResponseService : IService { }

    public class WebResponseModule : IModule
    {
        public void InitializeServices(IServiceManager serviceManager)
        {
            serviceManager.RegisterSingleton<IWebResponseService, WebResponseService>();
        }
    }

    public class WebResponseService : ServiceBase, IWebResponseService
    {
        public override void FinishedInitialization()
        {
            base.FinishedInitialization2();
            ServiceManager.Get<ISemanticProcessor>().Register<WebServerMembrane, WebResponder>();
        }
    }

    public class WebResponder : IReceptor
    {
        public void Process(ISemanticProcessor proc, IMembrane membrane, ST_JsonResponse resp)
        {
            resp.Context.Response.StatusCode = resp.StatusCode;
            resp.Context.Response.ContentType = "text/json";
            resp.Context.Response.ContentEncoding = Encoding.UTF8;
            byte[] byteData = resp.Json.to_Utf8();
            resp.Context.Response.ContentLength64 = byteData.Length;
            resp.Context.Response.OutputStream.Write(byteData, 0, byteData.Length);
            resp.Context.Response.Close();
        }

        public void Process(ISemanticProcessor proc, IMembrane membrane, ST_HtmlResponse resp)
        {
            byte[] utf8data = resp.Html.to_Utf8();
            resp.Context.Response.ContentType = "text/html";
            resp.Context.Response.ContentEncoding = Encoding.UTF8;
            resp.Context.Response.ContentLength64 = utf8data.Length;
            resp.Context.Response.OutputStream.Write(utf8data, 0, utf8data.Length);
            resp.Context.Response.Close();
        }

        public void Process(ISemanticProcessor proc, IMembrane membrane, ST_CssResponse resp)
        {
            byte[] utf8data = resp.Css.to_Utf8();
            resp.Context.Response.ContentType = "text/css";
            resp.Context.Response.ContentEncoding = Encoding.UTF8;
            resp.Context.Response.ContentLength64 = utf8data.Length;
            resp.Context.Response.OutputStream.Write(utf8data, 0, utf8data.Length);
            resp.Context.Response.Close();
        }

        public void Process(ISemanticProcessor proc, IMembrane membrane, ST_JavascriptResponse resp)
        {
            byte[] utf8data = resp.Javascript.to_Utf8();
            resp.Context.Response.ContentType = "text/javascript";
            resp.Context.Response.ContentEncoding = Encoding.UTF8;
            resp.Context.Response.ContentLength64 = utf8data.Length;
            resp.Context.Response.OutputStream.Write(utf8data, 0, utf8data.Length);
            resp.Context.Response.Close();
        }

        public void Process(ISemanticProcessor proc, IMembrane membrane, ST_RouteNotFound resp)
        {
            resp.Context.Response.StatusCode = 404;
            resp.Context.Response.Close();
        }
    }
}
