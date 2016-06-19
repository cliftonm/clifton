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
		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			ServiceManager.Get<ISemanticProcessor>().Register<LoggerMembrane, LoggerReceptor>();
		}

		public virtual void Log(string msg)
		{
			Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss : ") + msg);
		}

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
			while (ex != null)
			{
				Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss EXCEPTION : ") + ex.Message);
				Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss EXCEPTION : ") + ex.StackTrace);
				ex = ex.InnerException;

				if (ex != null)
				{
					Console.WriteLine("Inner Exception:");
				}
			}
		}
	}

	public class LoggerReceptor : IReceptor
	{
		public void Process(ISemanticProcessor proc, IMembrane membrane, ST_Log msg)
		{
			proc.ServiceManager.Get<IConsoleLoggerService>().Log(msg.Message);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, ST_Exception exception)
		{
			proc.ServiceManager.Get<IConsoleLoggerService>().Log(exception.Exception);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, ST_ExceptionObject exception)
		{
			proc.ServiceManager.Get<IConsoleLoggerService>().Log(exception.ExceptionMessage.Value);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, ST_CompilerError error)
		{
			proc.ServiceManager.Get<IConsoleLoggerService>().Log(error.Error);
		}
	}
}
