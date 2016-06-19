using System;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;

namespace Clifton.Core.PaperTrailAppLoggerService
{
	public class LoggerModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IPaperTrailAppLoggerService, LoggerService>();
		}
	}

	public class LoggerService : ServiceBase, IPaperTrailAppLoggerService
	{
		protected string key;

		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			ServiceManager.Get<ISemanticProcessor>().Register<LoggerMembrane, LoggerReceptor>();
			IAppConfigService config = ServiceManager.Get<IAppConfigService>();
			key = config.GetValue("PaperTrailAppKey");
			UdpPaperTrail.IP = config.GetValue("PaperTrailAppIP");
			UdpPaperTrail.Port = config.GetValue("PaperTrailAppPort").to_i();
		}

		public virtual void Log(string msg)
		{
			UdpPaperTrail.SendUdpMessage("<22>" + DateTime.Now.ToString("MMM d H:mm:ss") + key.Spaced() + ": " + msg);
		}

		public void Log(LogMessage msg)
		{
			UdpPaperTrail.SendUdpMessage("<22>" + DateTime.Now.ToString("MMM d H:mm:ss") + key.Spaced() + ": " + msg.Value);
		}

		public void Log(ExceptionMessage msg)
		{
			UdpPaperTrail.SendUdpMessage("<22>" + DateTime.Now.ToString("MMM d H:mm:ss") + key.Spaced() + ": " + msg.Value);
		}

		public virtual void Log(Exception ex)
		{
			while (ex != null)
			{
				UdpPaperTrail.SendUdpMessage("<22>" + DateTime.Now.ToString("MMM d H:mm:ss") + key.Spaced() + ": " + ex.Message);
				UdpPaperTrail.SendUdpMessage("<22>" + DateTime.Now.ToString("MMM d H:mm:ss") + key.Spaced() + ": " + ex.StackTrace);
				ex = ex.InnerException;

				if (ex != null)
				{
					Console.WriteLine("Inner Exception:");
				}
			}
		}

		public void Log(string tid, string msg)
		{
			UdpPaperTrail.SendUdpMessage("<22>" + DateTime.Now.ToString("MMM d H:mm:ss") + key.Spaced() + tid + ": " + msg);
		}

		public void Log(string tid, Exception ex)
		{
			while (ex != null)
			{
				UdpPaperTrail.SendUdpMessage("<22>" + DateTime.Now.ToString("MMM d H:mm:ss") + key.Spaced() + tid + ": " + ex.Message);
				UdpPaperTrail.SendUdpMessage("<22>" + DateTime.Now.ToString("MMM d H:mm:ss") + key.Spaced() + tid + ": " + ex.StackTrace);
				ex = ex.InnerException;

				if (ex != null)
				{
					Console.WriteLine("Inner Exception:");
				}
			}
		}
	}

	/// <summary>
	/// The only two semantic types we listen for.
	/// </summary>
	public class LoggerReceptor : IReceptor
	{
		public void Process(ISemanticProcessor proc, IMembrane membrane, ST_Log msg)
		{
			proc.ServiceManager.Get<IPaperTrailAppLoggerService>().Log(msg.Tid, msg.Message);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, ST_Exception exception)
		{
			proc.ServiceManager.Get<IPaperTrailAppLoggerService>().Log(exception.Tid, exception.Exception);
		}
	}
}
