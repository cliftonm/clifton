using System;
using System.IO;
using System.Net;
using System.Text;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;

using Clifton.WebInterfaces;

namespace Clifton.WebFileResponseService
{
	public class WebFileResponseModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IWebFileResponse, WebFileResponse>();
		}
	}

	public class WebFileResponse : ServiceBase, IWebFileResponse
	{
		protected string websitePath;

		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			websitePath = ServiceManager.Get<IAppConfigService>().GetValue("WebsitePath");
			ISemanticProcessor semProc = ServiceManager.Get<ISemanticProcessor>();
			semProc.Register<WebServerMembrane, WebFileResponseReceptor>();
		}

		public bool ProcessFileRequest(HttpListenerContext context)
		{
			bool handled = false;
			string path = context.Path().Value.Replace('/', '\\').LeftOfRightmostOf('.');	// without extension
			string ext = context.Extension().Value;

			if (String.IsNullOrEmpty(path))
			{
				path = "index";
			}

			if (String.IsNullOrEmpty(ext))
			{
				ext = "html";
			}

			path = path + "." + ext;
			path = Path.Combine(websitePath, path);

			if (File.Exists(path))
			{
				switch (ext)
				{
					case "html":
					case "spa":
						ServiceManager.Get<ISemanticProcessor>().ProcessInstance<WebServerMembrane, HtmlResponse>(r =>
						{
							r.Context = context;
							r.Html = ReadTextFile(path);
						});
						break;

					case "js":
						ServiceManager.Get<ISemanticProcessor>().ProcessInstance<WebServerMembrane, JavascriptResponse>(r =>
						{
							r.Context = context;
							r.Script = ReadTextFile(path);
						});
						break;

					case "css":
						ServiceManager.Get<ISemanticProcessor>().ProcessInstance<WebServerMembrane, CssResponse>(r =>
						{
							r.Context = context;
							r.Script = ReadTextFile(path);
						});
						break;

					case "jpg":
					case "ico":
					case "png":
					case "bmp":
					case "gif":
						ServiceManager.Get<ISemanticProcessor>().ProcessInstance<WebServerMembrane, ImageResponse>(r =>
						{
							r.Context = context;
							r.ContentType = "image/" + ext;
							r.BinaryData = ReadBinaryFile(path);
						});
						break;

					default:
						ServiceManager.Get<ISemanticProcessor>().ProcessInstance<WebServerMembrane, StringResponse>(r =>
						{
							r.Context = context;
							r.Message = ReadTextFile(path);
							r.StatusCode = 200;
						});
						break;
				}

				handled = true;
			}

			return handled;
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

