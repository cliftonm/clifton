using System;
using System.Collections.Generic;

namespace Clifton.Core.TemplateEngine
{
	public class TemplateEngineException : ApplicationException
	{
		public List<string> CompilerErrors { get; protected set; }

		public TemplateEngineException(List<string> errors)
			: base()
		{
			CompilerErrors = errors;
		}

		public TemplateEngineException(string msg)
			: base(msg)
		{
		}
	}
}
