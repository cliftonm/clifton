using System;

using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;

namespace Clifton.Core.ModuleManagement
{
	public interface IModuleManager : IService
	{
		void RegisterModules(XmlFileName filename);
	}
}
