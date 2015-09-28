using System;

using Clifton.CoreSemanticTypes;

namespace Clifton.ServiceInterfaces
{
	public interface IModuleManager : IService
	{
		void RegisterModules(XmlFileName filename);
	}
}
