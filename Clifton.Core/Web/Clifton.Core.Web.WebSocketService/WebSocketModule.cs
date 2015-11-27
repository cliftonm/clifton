using System;

using Clifton.Core.ModuleManagement;
using Clifton.Core.ServiceManagement;
using Clifton.Core.Web.WebInterfaces;

namespace Clifton.Core.Web.WebSocketService
{
	public class WebSocketModule : IModule
	{
		public virtual void InitializeServices(IServiceManager serviceManager)
		{

			serviceManager.RegisterSingleton<IWebSocketServerService, WebSocketServerService>();
			serviceManager.RegisterSingleton<IWebSocketClientService, WebSocketClientService>();
		}
	}
}
