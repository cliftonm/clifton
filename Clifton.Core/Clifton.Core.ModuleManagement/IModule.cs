using System;

using Clifton.Core.ServiceManagement;

namespace Clifton.Core.ModuleManagement
{
	public interface IModule
	{
		void InitializeServices(IServiceManager serviceManager);
	}
}
