using System;
using System.IO;
using System.Net;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;

using Semantics;
using ServiceInterfaces;

namespace WebFileResponseService
{
    // Here we create a placeholder, because this service is not actually exposed.
    // All activities are handled as a subscriber.
    public interface IFileResponseService : IService { }

    public class FileResponseModule : IModule
    {
        public void InitializeServices(IServiceManager serviceManager)
        {
            serviceManager.RegisterSingleton<FileResponseService, FileResponseService>();
        }
    }

    public class FileResponseService : ServiceBase, IFileResponseService
    {
        public override void FinishedInitialization()
        {
            base.FinishedInitialization2();
            ServiceManager.Get<ISemanticProcessor>().Register<WebServerMembrane, FileResponder>();
        }
    }

    public class FileResponder : IReceptor
    {
        public void Process(ISemanticProcessor proc, IMembrane membrane, ST_FileResponse resp)
        {
            ProcessFileRequest(proc, resp.Context);
        }

        protected void ProcessFileRequest(ISemanticProcessor semProc, HttpListenerContext context)
        {
            bool handled = false;
            string path = context.Request.RawUrl.LeftOf("?").RightOf("/").LeftOfRightmostOf('.');
            string ext = context.Request.RawUrl.RightOfRightmostOf('.');

            if (String.IsNullOrEmpty(path))
            {
                path = "index";
            }

            if (String.IsNullOrEmpty(ext))
            {
                ext = "html";
            }

            path = path + "." + ext;
            // Hardcoded folder path for the website!
            path = Path.Combine("Website", path);

            if (File.Exists(path))
            {
                switch (ext)
                {
                    case "html":
                        semProc.ProcessInstance<WebServerMembrane, ST_HtmlResponse>(r =>
                        {
                            r.Context = context;
                            r.Html = ReadTextFile(path);
                        });
                        break;

                    case "js":
                        semProc.ProcessInstance<WebServerMembrane, ST_JavascriptResponse>(r =>
                        {
                            r.Context = context;
                            r.Javascript = ReadTextFile(path);
                        });
                        break;

                    case "css":
                        semProc.ProcessInstance<WebServerMembrane, ST_CssResponse>(r =>
                        {
                            r.Context = context;
                            r.Css = ReadTextFile(path);
                        });
                        break;
                }

                handled = true;
            }

            if (!handled)
            {
                semProc.ServiceManager.Get<ILoggerService>().Log("Route not found.");
                semProc.ProcessInstance<WebServerMembrane, ST_RouteNotFound>(r => r.Context = context);
            }
        }

        protected string ReadTextFile(string fn)
        {
            string text = File.ReadAllText(fn);

            return text;
        }

        protected byte[] ReadBinaryFile(string fn)
        {
            FileStream fStream = new FileStream(fn, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fStream);
            byte[] data = br.ReadBytes((int)fStream.Length);
            br.Close();
            fStream.Close();

            return data;
        }
    }
}
