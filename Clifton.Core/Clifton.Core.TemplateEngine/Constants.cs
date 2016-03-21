using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.Core.TemplateEngine
{
	public static class Constants
	{
		public const string START_CODE_BLOCK = "@{";
		public const string END_CODE_BLOCK = "}";
		public const string EOL = ";" + Constants.CRLF;
		public const string CRLF = "\\r\\n";
		public const string LITERAL = "@:";
	}
}
