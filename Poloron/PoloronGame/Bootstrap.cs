using System;
using System.Reflection;
using System.Windows.Forms;

using Clifton.CoreSemanticTypes;
using Clifton.ModuleManagement;
using Clifton.ServiceInterfaces;
using Clifton.ServiceManagement;


namespace PoloronGame
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
				MessageBox.Show(ex.Message, "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Environment.Exit(1);
			}
		}
	}
}
