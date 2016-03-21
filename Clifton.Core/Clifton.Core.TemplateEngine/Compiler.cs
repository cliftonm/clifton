/*
    Copyright 2016 Marc Clifton

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

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
			cp.ReferencedAssemblies.Add("Clifton.Core.TemplateEngine.dll");
			
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

