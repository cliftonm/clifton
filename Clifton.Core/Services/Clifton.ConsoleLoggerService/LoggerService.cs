using System;

using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;

namespace Clifton.ConsoleLoggerService
{
	public class LoggerModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IConsoleLoggerService, LoggerService>();
		}
	}

	public class LoggerService : ServiceBase, IConsoleLoggerService
	{
		public virtual void Log(LogMessage msg)
		{
			Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss : ") + msg.Value);
		}

		public virtual void Log(ExceptionMessage msg)
		{
			Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss EXCEPTION : ") + msg.Value);
		}

		public virtual void Log(Exception ex)
		{
			Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss EXCEPTION : ") + ex.Message);
			Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss EXCEPTION : ") + ex.StackTrace);
		}
	}
}
