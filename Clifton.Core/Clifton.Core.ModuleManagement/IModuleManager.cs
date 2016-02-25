using System;
using System.Collections.Generic;
using System.Reflection;

using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;

namespace Clifton.Core.ModuleManagement
{
	public interface IModuleManager : IService
	{
		void RegisterModules(XmlFileName filename, string optionalFolder = null, Func<string, Assembly> resourceAssemblyResolver = null);
		void RegisterModules(List<AssemblyFileName> moduleFilenames, string optionalFolder = null, Func<string, Assembly> resourceAssemblyResolver = null);
		List<AssemblyFileName> GetModuleList(XmlFileName filename);
	}
}
