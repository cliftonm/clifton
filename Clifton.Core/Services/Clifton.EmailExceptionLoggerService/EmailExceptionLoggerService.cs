/* The MIT License (MIT)
* 
* Copyright (c) 2015 Marc Clifton
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

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

			email.Body = sb.ToString().Replace("\r", "<br/>").Replace("\n", "<br/>");
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
