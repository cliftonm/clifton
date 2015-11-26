using System;

namespace Clifton.Core.ServiceManagement
{
	/// <summary>
	/// A useful base class for a default implementation of IService methods.
	/// </summary>
	public abstract class ServiceBase : IService
	{
		public IServiceManager ServiceManager { get; set; }

		public virtual void Initialize(IServiceManager svcMgr)
		{
			ServiceManager = svcMgr;
		}

		public virtual void FinishedInitialization()
		{
		}
	}
}
