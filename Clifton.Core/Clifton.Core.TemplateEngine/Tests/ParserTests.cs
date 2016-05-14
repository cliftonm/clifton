using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Core.TemplateEngine;

namespace Tests
{
	[TestClass]
	public class ParserTests
	{
		[TestMethod]
		public void ParserTest()
		{
			string textToParse=@"
@{
  string str = ""Hello World"";
  int i = 10;
  @:Literal
}
A line with @str and @i with @@ignore me
";
			string parsed = Parser.Parse(textToParse);
			Assert.AreEqual("StringBuilder sb = new StringBuilder();\r\n  string str = \"Hello World\";\r\n  int i = 10;\r\nsb.Append(\"  Literal\\r\\n\");\r\nsb.Append(\"A line with \\\" + str.ToString() + \\\" and \\\" + i.ToString() + \\\" with @ignore me\\r\\n\");\r\n", parsed);
		}
	}
}
