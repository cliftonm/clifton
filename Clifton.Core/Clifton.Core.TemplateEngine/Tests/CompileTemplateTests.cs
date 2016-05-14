using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Clifton.Core.TemplateEngine;

namespace Tests
{
	[TestClass]
	public class CompileTemplateTests
	{
		[TestMethod]
		public void CompileBasicTemplate()
		{
			string code = ParseTemplate();
			string assyCode = @"
using System;
using System.Text;
using Clifton.Core.TemplateEngine;

public class RuntimeCompiled : IRuntimeAssembly
{
  public string GetTemplate(params object[] paramList)
  {
";

			assyCode += code;
			assyCode += @"
    return sb.ToString();
  }
}";

			Assembly assy = CreateAssembly(assyCode);
			IRuntimeAssembly t = (IRuntimeAssembly)assy.CreateInstance("RuntimeCompiled");
			string ret = t.GetTemplate();
			Assert.AreEqual("  Literal\r\nA line with \" + str.ToString() + \" and \" + i.ToString() + \" with @ignore me\r\n", ret);
		}

		/// <summary>
		/// Parse a template.
		/// </summary>
		private string ParseTemplate()
		{
			string template = @"
@{
  string str = ""Hello World"";
  int i = 10;
  @:Literal
}
A line with @str and @i with @@ignore me
";
			string parsed = Parser.Parse(template);
			Assert.AreEqual("StringBuilder sb = new StringBuilder();\r\n  string str = \"Hello World\";\r\n  int i = 10;\r\nsb.Append(\"  Literal\\r\\n\");\r\nsb.Append(\"A line with \\\" + str.ToString() + \\\" and \\\" + i.ToString() + \\\" with @ignore me\\r\\n\");\r\n", parsed);

			return parsed;
		}

		[TestMethod]
		public void FriendlyCompileTemplate()
		{
			string template = @"
@{
  string str = ""Hello World"";
  int i = 10;
  @:Literal
}
A line with @str and @i with @@ignore me
";

			TemplateEngine eng = new TemplateEngine();
			string ret = eng.Parse(template);
			Assert.AreEqual("  Literal\r\nA line with \" + str.ToString() + \" and \" + i.ToString() + \" with @ignore me\r\n", ret);
		}

		[TestMethod]
		public void ParameterPassing()
		{
			string template = "A line with @str and @i with @@ignore me";

			TemplateEngine eng = new TemplateEngine();
			string ret = eng.Parse(template, new List<ParamTypeInfo>()
			{
				new ParamTypeInfo() {ParamName="str", ParamType="string", ParamValue = "Hello World"},
				new ParamTypeInfo() {ParamName="i", ParamType="int", ParamValue = 10},
			});

			Assert.AreEqual("A line with \" + str.ToString() + \" and \" + i.ToString() + \" with @ignore me\r\n", ret);
		}

		public class Model : ModelInterface.IModel
		{
			public string Str { get; set; }
			public int I { get; set; }
		}

		[TestMethod]
		public void NonNativePassing()
		{
			string template = "A line with @model.Str and @model.I with @@ignore me";
			Model model = new Model() { Str = "Howdy", I = 15 };

			TemplateEngine eng = new TemplateEngine();
			eng.Usings.Add("using ModelInterface;");
			eng.References.Add("ModelInterface.dll");
			string ret = eng.Parse(template, new List<ParamTypeInfo>()
			{
				new ParamTypeInfo() {ParamName="model", ParamType="IModel", ParamValue = model},
			});

			Assert.AreEqual("A line with \" + model.Str.ToString() + \" and \" + model.I.ToString() + \" with @ignore me\r\n", ret);
		}

		[TestMethod]
		public void DynamicParameterTypePassing()
		{
			string template = "A line with @model.Str and @model.I with @@ignore me";
			Model model = new Model() { Str = "Howdy", I = 15 };

			TemplateEngine eng = new TemplateEngine();
			// Removed: eng.Usings.Add("using ModelInterface;");
			// Removed: eng.References.Add("ModelInterface.dll");
			eng.References.Add("Microsoft.CSharp.dll");
			eng.References.Add(typeof(System.Runtime.CompilerServices.DynamicAttribute).Assembly.Location);

			string ret = eng.Parse(template, new List<ParamTypeInfo>()
			{
				// new ParamTypeInfo() {ParamName="model", ParamType="IModel", ParamValue = model},
				// changed to:
				new ParamTypeInfo() {ParamName="model", ParamType="dynamic", ParamValue = model},
			});

			Assert.AreEqual("A line with \" + model.Str.ToString() + \" and \" + model.I.ToString() + \" with @ignore me\r\n", ret);
		}

		[TestMethod]
		public void SimplerParameterPassing()
		{
			string template = "A line with @model.Str and @i with @@ignore me";
			Model model = new Model() { Str = "Howdy" };

			TemplateEngine eng = new TemplateEngine();
			eng.Usings.Add("using ModelInterface;");
			eng.References.Add("ModelInterface.dll");
			
			// An example of non-native and native type passing.
			string ret = eng.Parse(template, new string[] {"model", "i"}, model, 15);

			Assert.AreEqual("A line with Howdy and 15 with @ignore me\r\n", ret);
		}

		public class Model2
		{
			public int I { get; set; }
		}

		[TestMethod]
		public void DynamicNonNativeTypeOnlyParameterPassing()
		{
			string template = "A line with @model.Str and @model2.I with @@ignore me";
			Model model = new Model() { Str = "I'm Dynamic!", I=20 };
			Model2 model2 = new Model2() { I = 20 };

			TemplateEngine eng = new TemplateEngine();
			eng.UsesDynamic();
			string ret = eng.Parse(template, model, model2);

			Assert.AreEqual("A line with I'm Dynamic! and 20 with @ignore me\r\n", ret);
		}

		[TestMethod]
		public void CacheTest()
		{
			string template = "A line with @model.Str and @model2.I with @@ignore me";
			Model model = new Model() { Str = "I'm Dynamic!", I = 20 };
			Model2 model2 = new Model2() { I = 20 };

			TemplateEngine eng = new TemplateEngine();
			eng.UsesDynamic();
			string ret = eng.Parse(template, model, model2);

			Assert.AreEqual("A line with I'm Dynamic! and 20 with @ignore me\r\n", ret);
			Assert.IsTrue(eng.IsCached(template));

			model.Str = "Cached!";
			model2.I = 25;
			ret = eng.Parse(template, model, model2);
			Assert.AreEqual("A line with Cached! and 25 with @ignore me\r\n", ret);
		}

		[TestMethod]
		public void VarReplacementInLiteral()
		{
			string template = @"
@{
@:alert(""<@model.Str@>"")
}";			

			Model model = new Model() { Str = "Welcome!" };
			TemplateEngine eng = new TemplateEngine();
			eng.UsesDynamic();
			string ret = eng.Parse(template, model);
			Assert.AreEqual("alert(\"\" + model.Str.ToString() + \"\r\n", ret);
		}

		/// <summary>
		/// Create an in-memory assembly.
		/// </summary>
		private Assembly CreateAssembly(string code)
		{
			List<string> errors;
			Assembly assy = Compiler.Compile(code, out errors);
			Assert.IsNotNull(assy);

			return assy;
		}
	}
}
