using System;
using System.Reflection;

using Clifton.CoreSemanticTypes;
using Clifton.ModuleManagement;
using Clifton.ServiceInterfaces;
using Clifton.ServiceManagement;


namespace PoloronGameServer
{
	static partial class Program
	{
		public static ServiceManager serviceManager;

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
				Environment.Exit(1);
			}
		}
	}
}
