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
        bool Success { get; }
		void Send(Email email);
	}
}
