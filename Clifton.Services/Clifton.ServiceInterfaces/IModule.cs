using System;

using Clifton.ServiceInterfaces;

namespace Clifton.ServiceInterfaces
{
	public interface IModule
	{
		void InitializeServices(IServiceManager serviceManager);
	}
}
