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
	public class LoggerModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IEmailService, EmailService>();
		}
	}

	public class EmailService : ServiceBase, IEmailService
	{
		public virtual void Send(Email email)
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
				ServiceManager.Get<ILoggerService>().Log(ex);
			}
		}
	}
}
