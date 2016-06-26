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

using Clifton.Core.ServiceInterfaces;

namespace Clifton.Core.Semantics
{
	public class ST_Exception : ISemanticType
	{
		public Exception Exception { get; set; }
		public string Tid { get; set; }			// used for PaperTrailApp.

		public ST_Exception()
		{
		}

		public ST_Exception(Exception ex)
		{
			Exception = ex;
		}
	}

	public class ST_ExceptionObject : ISemanticType
	{
		public ExceptionMessage ExceptionMessage { get; set; }

		public ST_ExceptionObject()
		{
		}

		public ST_ExceptionObject(ExceptionMessage ex)
		{
			ExceptionMessage = ex;
		}
	}

	/// <summary>
	/// Used by TemplateEngine and other runtime compiler services.
	/// </summary>
	public class ST_CompilerError : ISemanticType
	{
		public string Error {get; set;}

		public ST_CompilerError()
		{
		}

		public ST_CompilerError(string err)
		{
			Error = err;
		}
	}

	public class ST_Log : ISemanticType
	{
		public string Message { get; set; }
		public string Tid { get; set; }			// used for PaperTrailApp.

		public ST_Log()
		{
		}
	}

	public class XmlFileName : ImmutableSemanticType<XmlFileName, string> { };
	public class OptionalPath : ImmutableSemanticType<OptionalPath, string> { };
	public class FullPath : ImmutableSemanticType<FullPath, string> { };
	public class AssemblyFileName : ImmutableSemanticType<AssemblyFileName, string> { }

	public class ConnectionString : ImmutableSemanticType<ConnectionString, string> { }
	public class UserName : ImmutableSemanticType<UserName, string> { }
	public class HashedPassword : ImmutableSemanticType<HashedPassword, string> { }
	public class PlainTextPassword : ImmutableSemanticType<PlainTextPassword, string> { }
	public class UserId : ImmutableSemanticType<UserId, int> { }
	public class ViewName : ImmutableSemanticType<ViewName, string> { }
	public class WhereClause : ImmutableSemanticType<WhereClause, string> { }
}
