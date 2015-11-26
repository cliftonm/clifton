using System;
using System.Configuration;

using Clifton.Core.ModuleManagement;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;

namespace Clifton.Cores.Services.AppConfigService
{
	public class AppConfigModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IAppConfigService, ConfigService>();
		}
	}

	public class ConfigService : ServiceBase, IAppConfigService
	{
		public virtual string GetConnectionString(string key)
		{
			return ConfigurationManager.ConnectionStrings[key].ConnectionString;
		}

		public virtual string GetValue(string key)
		{
			return ConfigurationManager.AppSettings[key];
		}
	}
}
