using System;
using System.Text;

using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;

namespace Clifton.ConsoleLoggerService
{
	public class EmailExceptionModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IEmailExceptionLoggerService, EmailExceptionLoggerService>();
		}
	}

	public class EmailExceptionLoggerService : ServiceBase, IEmailExceptionLoggerService
	{
		private Email CreateEmail()
		{
			Email email = new Email();
			IAppConfigService config = ServiceManager.Get<IAppConfigService>();
			email.AddTo(config.GetValue("emailExceptionTo"));
			email.Subject = config.GetValue("emailExceptionSubject");

			return email;
		}

		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			ServiceManager.Get<ISemanticProcessor>().Register<LoggerMembrane, EmailExceptionLoggerReceptor>();
		}

		public virtual void Log(string msg)
		{
			Email email = CreateEmail();
			email.Body = msg;
			ServiceManager.Get<IEmailService>().Send(email);
		}

		public virtual void Log(ExceptionMessage msg)
		{
			Email email = CreateEmail();
			email.Body = msg.Value;
			ServiceManager.Get<IEmailService>().Send(email);
		}

		public virtual void Log(Exception ex)
		{
			StringBuilder sb = new StringBuilder();
			Email email = CreateEmail();

			while (ex != null)
			{
				sb.AppendLine(ex.Message);
				sb.AppendLine(ex.StackTrace);

				ex = ex.InnerException;

				if (ex != null)
				{
					sb.AppendLine("========= Inner Exception =========");
				}
			}

			email.Body = sb.ToString();
			ServiceManager.Get<IEmailService>().Send(email);
		}
	}

	public class EmailExceptionLoggerReceptor : IReceptor
	{
		public void Process(ISemanticProcessor proc, IMembrane membrane, ST_Exception exception)
		{
			proc.ServiceManager.Get<IEmailExceptionLoggerService>().Log(exception.Exception);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, ST_ExceptionObject exception)
		{
			proc.ServiceManager.Get<IEmailExceptionLoggerService>().Log(exception.ExceptionMessage.Value);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, ST_CompilerError error)
		{
			proc.ServiceManager.Get<IConsoleLoggerService>().Log(error.Error);
		}
	}
}
