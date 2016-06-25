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
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.Semantics;

namespace Clifton.Core.TemplateEngine
{
    public class TemplateEngine
    {
		public List<string> Usings { get; protected set; }
		public List<string> References { get; protected set; }

		protected Dictionary<Guid, IRuntimeAssembly> cachedAssemblies;
		protected bool useDynamic;
		protected ISemanticProcessor proc;

		public TemplateEngine(ISemanticProcessor proc = null)
		{
			Usings = new List<string>();
			References = new List<string>();
			cachedAssemblies = new Dictionary<Guid, IRuntimeAssembly>();
			AddCoreUsings();
		}

		protected virtual void AddCoreUsings()
		{
			Usings.Add("using System;");
			Usings.Add("using System.Text;");
			Usings.Add("using Clifton.Core.TemplateEngine;");
		}

		public void UsesDynamic()
		{
			useDynamic = true;
			References.Add("Microsoft.CSharp.dll");
			References.Add(typeof(System.Runtime.CompilerServices.DynamicAttribute).Assembly.Location);
		}

		public string Parse(string template)
		{
			string ret;
			IRuntimeAssembly t;

			if (!IsCached(template, out t))
			{
				string code = new Parser().Parse(template);
				StringBuilder sb = new StringBuilder(String.Join("\r\n", Usings));
				sb.Append(GetClassBoilerplate());
				sb.Append(code);
				sb.Append(GetFinisherBoilerplate());
				t = GetAssembly(sb.ToString());
				cachedAssemblies[GetHash(template)] = t;
			}

			ret = t.GetTemplate();

			return ret;
		}

		public string Parse(string template, List<ParamTypeInfo> parms)
		{
			string ret;
			IRuntimeAssembly t;

			if (!IsCached(template, out t))
			{
				string code = new Parser().Parse(template);
				StringBuilder sb = new StringBuilder(String.Join("\r\n", Usings));
				sb.Append(GetClassBoilerplate());
				InitializeParameters(sb, parms);
				sb.Append(code);
				sb.Append(GetFinisherBoilerplate());
				t = GetAssembly(sb.ToString());
				cachedAssemblies[GetHash(template)] = t;
			}

			object[] objParms = parms.Select(p => p.ParamValue).ToArray();
			ret = t.GetTemplate(objParms);

			return ret;
		}

		public string Parse(string template, string[] names, params object[] parms)
		{
			string ret;
			IRuntimeAssembly t;

			if (!IsCached(template, out t))
			{
				string code = new Parser().Parse(template);
				StringBuilder sb = new StringBuilder(String.Join("\r\n", Usings));
				sb.Append(GetClassBoilerplate());
				InitializeParameters(sb, names, parms);
				sb.Append(code);
				sb.Append(GetFinisherBoilerplate());
				t = GetAssembly(sb.ToString());
				cachedAssemblies[GetHash(template)] = t;
			}

			ret = t.GetTemplate(parms);

			return ret;
		}

		public string Parse(string template, params object[] parms)
		{
			string ret;
			IRuntimeAssembly t;

			if (!IsCached(template, out t))
			{
				string code = new Parser().Parse(template);
				StringBuilder sb = new StringBuilder(String.Join("\r\n", Usings));
				sb.Append(GetClassBoilerplate());
				InitializeParameters(sb, parms);
				sb.Append(code);
				sb.Append(GetFinisherBoilerplate());
				t = GetAssembly(sb.ToString());
				cachedAssemblies[GetHash(template)] = t;
			}

			ret = t.GetTemplate(parms);

			return ret;
		}

		//public bool IsCached(string template)
		//{
		//	return cachedAssemblies.ContainsKey(GetHash(template));
		//}

		public bool IsCached(string template, out IRuntimeAssembly t)
		{
			return cachedAssemblies.TryGetValue(GetHash(template), out t);
		}

		public virtual Guid GetHash(string template)
		{
			using (MD5 md5 = MD5.Create())
			{
				byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(template));

				return new Guid(hash);
			}
		}

		private string GetClassBoilerplate()
		{
			return @"
public class RuntimeCompiled : IRuntimeAssembly
{
  public string GetTemplate(object[] paramList)
  {
";
		}

		private string GetFinisherBoilerplate()
		{
			return @"
    return sb.ToString();
  }
}";
		}

		private IRuntimeAssembly GetAssembly(string assyCode)
		{
			IRuntimeAssembly t = null;
			List<string> errors;
			Assembly assy = Compiler.Compile(assyCode, out errors, References);

			if (assy == null)
			{
				proc.IfNotNull((p) => errors.ForEach(errMsg => p.ProcessInstance<LoggerMembrane, ST_CompilerError>(err => err.Error = errMsg)));
				throw new TemplateEngineException(errors);
			}
			else
			{
				t = (IRuntimeAssembly)assy.CreateInstance("RuntimeCompiled");
			}

			return t;
		}

		/// <summary>
		/// Using a fully specified parameter list: value, name, and type.
		/// </summary>
		private void InitializeParameters(StringBuilder sb, List<ParamTypeInfo> parms)
		{
			parms.ForEachWithIndex((pti, idx) =>
				{
					sb.Append(pti.ParamType + " " + pti.ParamName + " = (" + pti.ParamType+")paramList[" + idx + "];\r\n");
				});
		}

		/// <summary>
		/// Making assumptions about class types and native types, still requiring variable names.
		/// </summary>
		private void InitializeParameters(StringBuilder sb, string[] names, object[] parms)
		{
			parms.ForEachWithIndex((parm, idx) =>
			{
				if (useDynamic)
				{
					sb.Append("dynamic " + names[idx] + " = paramList[" + idx + "];\r\n");
				}
				else
				{
					Type t = parm.GetType();
					string typeName = t.IsClass ? "I" + t.Name : t.Name;
					sb.Append(typeName + " " + names[idx] + " = (" + typeName + ")paramList[" + idx + "];\r\n");
				}
			});
		}

		/// <summary>
		/// Only dynamic is supported.  Non-class types are not supported because we can't determine their names.
		/// Class types must be distinct.
		/// </summary>
		private void InitializeParameters(StringBuilder sb, object[] parms)
		{
			List<string> typeNames = new List<string>();

			parms.ForEachWithIndex((parm, idx) =>
			{
				Type t = parm.GetType();
				string typeName = t.Name.CamelCase();

				if (!t.IsClass)
				{
					throw new TemplateEngineException("Automatic parameter passing does not support native types.  Wrap the type in a class.");
				}

				if (typeNames.Contains(typeName))
				{
					throw new TemplateEngineException("Type names must be distinct.");
				}

				typeNames.Add(typeName);
				sb.Append("dynamic " + typeName + " = paramList[" + idx + "];\r\n");
			});
		}
	}
}
