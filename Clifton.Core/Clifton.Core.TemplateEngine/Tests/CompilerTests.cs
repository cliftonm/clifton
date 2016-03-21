using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Core.TemplateEngine;

namespace Tests
{
	[TestClass]
	public class CompilerTests
	{
		[TestMethod]
		public void BasicCompilation()
		{
			List<string> errors;
			Assembly assy = Compiler.Compile(@"
using System;
using Clifton.Core.TemplateEngine;

public class RuntimeCompiled : IRuntimeAssembly
{
	public string HelloWorld()
	{
		return ""Hello World"";
	}

    public string Print(string something)
	{
		return ""This is something: "" + something;
	}
}
", out errors);

			if (assy == null)
			{
				errors.ForEach(err => Console.WriteLine(err));
			}
			else
			{
				ITestRuntimeAssembly t = (ITestRuntimeAssembly)assy.CreateInstance("RuntimeCompiled");
				string ret = t.HelloWorld();
				Assert.AreEqual("Hello World", ret);
			}
		}
	}
}
