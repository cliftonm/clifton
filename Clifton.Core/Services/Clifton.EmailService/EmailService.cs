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
using System.Net;
using System.Net.Mail;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;

namespace Clifton.EmailService
{
	public class EmailModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IEmailService, EmailService>();
		}
	}

	public class EmailService : ServiceBase, IEmailService
	{
		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			ISemanticProcessor semProc = ServiceManager.Get<ISemanticProcessor>();
			semProc.Register<EmailClientMembrane, EmailClientReceptor>();
		}

		public void Send(Email email)
		{
			IConfigService config = ServiceManager.Get<IConfigService>();

			try
			{
				// TODO: Until we figure out why we can't send emails from bmbfc.org, we're using our poloron server.
				string username = config.GetValue("emailusername");
				string password = config.GetValue("emailpassword");
				string fromEmail = email.From ?? config.GetValue("emailfrom"); //  "postmaster@bmbfc.org";
				string host = config.GetValue("emailhost");

				SmtpClient smtpClient = new SmtpClient();
				NetworkCredential basicCredential = new NetworkCredential(username, password);
				MailMessage message = new MailMessage();
				MailAddress fromAddress = new MailAddress(fromEmail);

				smtpClient.Host = host;
				smtpClient.Port = 587;          // Note the port setting!
				smtpClient.UseDefaultCredentials = false;
				smtpClient.Credentials = basicCredential;

				message.From = fromAddress;
				message.Subject = email.Subject;
				message.IsBodyHtml = true;
				message.Body = email.Body;
				message.IsBodyHtml = true;

				email.To.ForEach(e => message.To.Add(e));
				email.Cc.IfNotNull(ccs => ccs.ForEach(cc => message.CC.Add(cc)));
				email.Bcc.IfNotNull(bccs => bccs.ForEach(bcc => message.Bcc.Add(bcc)));

				smtpClient.Send(message);
			}
			catch (Exception ex)
			{
				// ServiceManager.Get<ILoggerService>().Log(ex);
				ServiceManager.Get<ISemanticProcessor>().ProcessInstance<LoggerMembrane, ST_Exception>(ex2 => ex2.Exception = ex);
			}
		}
	}

	/// <summary>
	/// Support for semantic routing.
	/// </summary>
	public class EmailClientReceptor : IReceptor
	{
		public void Process(ISemanticProcessor proc, IMembrane membrane, Email email)
		{
			proc.ServiceManager.Get<IEmailService>().Send(email);
		}
	}
}
