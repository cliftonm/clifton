using System;

using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;

namespace Clifton.Core.Services.ConsoleCriticalExceptionService
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
				if (e.ExceptionObject is Exception)
				{
					ServiceManager.Get<ISemanticProcessor>().ProcessInstance<LoggerMembrane, ST_Exception>(ex2 => ex2.Exception = ((Exception)e.ExceptionObject));
				}
				else
				{
					ServiceManager.Get<ISemanticProcessor>().ProcessInstance<LoggerMembrane, ST_ExceptionObject>(em=> em.ExceptionMessage = ExceptionMessage.Create(e.ExceptionObject.GetType().Name));
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
