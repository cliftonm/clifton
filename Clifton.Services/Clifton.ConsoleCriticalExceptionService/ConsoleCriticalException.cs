using System;

using Clifton.ServiceInterfaces;

namespace Clifton.ConsoleCriticalExceptionService
{
	public class ConsoleCriticalExceptionModule : IModule
	{
		public virtual void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IConsoleCriticalExceptionService, ConsoleCriticalException>();
		}
	}

    public class ConsoleCriticalException : IConsoleCriticalExceptionService
    {
		protected IServiceManager serviceManager;

		public void Initialize(IServiceManager svcMgr)
		{
			serviceManager = svcMgr;
			AppDomain.CurrentDomain.UnhandledException += GlobalExceptionHandler;
		}

		void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs e)
		{
			try
			{
				ILoggerService logger = serviceManager.GetSingleton<ILoggerService>();

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
