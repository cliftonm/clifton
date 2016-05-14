using System;
using System.Collections.Generic;

using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;

namespace Clifton.Core.ServiceInterfaces
{
	public class EmailClientMembrane : Membrane { }

	public class Email : ISemanticType
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

		public Email()
		{
			To = new List<string>();
			Cc = new List<string>();
			Bcc = new List<string>();
		}

		public void AddTo(string to)
		{
			To.Add(to);
		}

		public void AddCc(string cc)
		{
			Cc.Add(cc);
		}

		public void AddBcc(string bcc)
		{
			Bcc.Add(bcc);
		}
	}

	public interface IEmailService : IService
	{
		void Send(Email email);
	}
}
