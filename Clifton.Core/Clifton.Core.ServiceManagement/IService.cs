using System;

namespace Clifton.Core.ServiceManagement
{
	public interface IService
	{
		IServiceManager ServiceManager { get; }
		void Initialize(IServiceManager srvMgr);
		void FinishedInitialization();
	}
}
