using System;
using System.Collections.Generic;

using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;

namespace Clifton.Core.ServiceInterfaces
{
	public class Email
	{
		/// <summary>
		/// If null, will get the from address from App.config
		/// </summary>
		public string From { get; set; }

		public List<string> To { get; set; }
		public string Subject { get; set; }
		public string Body { get; set; }

		public List<string> Cc { get; set; }
		public List<string> Bcc { get; set; }
	}

	public interface IEmailService : IService
	{
		void Send(Email email);
	}
}
