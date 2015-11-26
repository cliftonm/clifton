using System;

using Clifton.Core.ModuleManagement;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;

namespace Clifton.Core.ConsoleCriticalExceptionService
{
	public class ConsoleCriticalExceptionModule : IModule
	{
		public virtual void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IConsoleCriticalExceptionService, ConsoleCriticalException>();
		}
	}

	public class ConsoleCriticalException : ServiceBase, IConsoleCriticalExceptionService
	{
		public override void Initialize(IServiceManager svcMgr)
		{
			base.Initialize(svcMgr);
			AppDomain.CurrentDomain.UnhandledException += GlobalExceptionHandler;
		}

		protected virtual void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs e)
		{
			try
			{
				ILoggerService logger = ServiceManager.Get<ILoggerService>();

				if (e.ExceptionObject is Exception)
				{
					logger.Log((Exception)e.ExceptionObject);
				}
				else
				{
					logger.Log(ExceptionMessage.Create(e.ExceptionObject.GetType().Name));
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
			}

			Environment.Exit(1);
		}
	}
}
