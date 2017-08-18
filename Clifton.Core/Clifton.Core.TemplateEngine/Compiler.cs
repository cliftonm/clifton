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
using System.Reflection;

using System.CodeDom.Compiler;
using Microsoft.CSharp;

using Clifton.Core.ExtensionMethods;

namespace Clifton.Core.TemplateEngine
{
	public static class Compiler
	{
		public static string TemplateEnginePath = null;

		public static Assembly Compile(string code, out List<string> errors, List<string> references = null)
		{
			Assembly assy = null;
			errors = null;
			CodeDomProvider provider = null;
			provider = CodeDomProvider.CreateProvider("CSharp");
			CompilerParameters cp = new CompilerParameters();

			// Generate a class library in memory.
			cp.GenerateExecutable = false;
			cp.GenerateInMemory = true;
			cp.TreatWarningsAsErrors = false;
			cp.ReferencedAssemblies.Add("System.dll");
			//cp.ReferencedAssemblies.Add(@"c:\websites\projourn\bin\Clifton.Core.TemplateEngine.dll");
			cp.ReferencedAssemblies.Add(TemplateEnginePath ?? "Clifton.Core.TemplateEngine.dll");

			// to support the "dynamic" keyword, add references for:
			// "Microsoft.CSharp.dll"
			// typeof(System.Runtime.CompilerServices.DynamicAttribute).Assembly.Location

			references.IfNotNull(refs => refs.ForEach(r => cp.ReferencedAssemblies.Add(r)));

			// Invoke compilation of the source file.
			CompilerResults cr = provider.CompileAssemblyFromSource(cp, code);

			if (cr.Errors.Count > 0)
			{
				errors = new List<string>();

				foreach (var err in cr.Errors)
				{
					errors.Add(err.ToString());
				}
			}
			else
			{
				assy = cr.CompiledAssembly;
			}

			return assy;
		}
	}
}

