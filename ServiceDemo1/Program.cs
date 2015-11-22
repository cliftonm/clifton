using System;
using System.Reflection;

using Clifton.CoreSemanticTypes;
using Clifton.ModuleManagement;
using Clifton.ServiceInterfaces;
using Clifton.ServiceManagement;

namespace ServiceDemo1
{
	class Program
	{
		static IServiceManager serviceManager;

		static void Main(string[] args)
		{
			Bootstrap();
			ILoggerService logger = serviceManager.Get<ILoggerService>();
			IAppConfigService config = serviceManager.Get<IAppConfigService>();
			logger.Log(LogMessage.Create(config.GetValue("Message")));
			// throw new ApplicationException("Here's a global exception being handled!");
			Console.ReadLine();
		}

		static void Bootstrap()
		{
			serviceManager = new ServiceManager();
			serviceManager.RegisterSingleton<IModuleManager, ModuleManager>();

			try
			{
				IModuleManager moduleMgr = serviceManager.Get<IModuleManager>();
				moduleMgr.RegisterModules(XmlFileName.Create("modules.xml"));
				serviceManager.FinishedInitialization();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);

				if (ex is ReflectionTypeLoadException)
				{
					ReflectionTypeLoadException exl = ex as ReflectionTypeLoadException;
					Exception[] exceptions = exl.LoaderExceptions;

					foreach (Exception exle in exceptions)
					{
						Console.WriteLine(exle.Message);
					}
				}

				Environment.Exit(1);
			}
		}
	}
}
