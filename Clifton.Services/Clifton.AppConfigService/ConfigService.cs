using System;
using System.Configuration;

using Clifton.ServiceInterfaces;

namespace Clifton.AppConfigService
{
	public class AppConfigModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.Register<IAppConfigService, ConfigService>(ConstructionOption.AlwaysSingleton);
		}
	}

    public class ConfigService : IAppConfigService
    {
		public void Initialize(IServiceManager svcMgr) { }

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
