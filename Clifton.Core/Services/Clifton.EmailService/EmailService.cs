using System;
using System.Net;
using System.Net.Mail;

using Clifton.Core.Assertions;
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
        public bool Success { get; protected set; }

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
				// message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(email.Body, null, MediaTypeNames.Text.Html));

				email.To.ForEach(e => message.To.Add(e));
				email.Cc.IfNotNull(ccs => ccs.ForEach(cc => message.CC.Add(cc)));
				email.Bcc.IfNotNull(bccs => bccs.ForEach(bcc => message.Bcc.Add(bcc)));

				smtpClient.Send(message);
                Success = true;
			}
			catch (Exception ex)
			{
                Success = false;
                // If an exception occurs when emailing, log through whatever logging mechanism in place, rather than handling by trying to email another exception!
                Assert.SilentTry(() =>
                    ServiceManager.Get<ISemanticProcessor>().ProcessInstance<LoggerMembrane, ST_Exception>(ex2 => ex2.Exception = ex, true));
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
