using System;

namespace Clifton.ServiceInterfaces
{
	/// <summary>
	/// A useful base class for a default implementation of IService methods.
	/// </summary>
	public abstract class ServiceBase : IService
	{
		protected IServiceManager serviceManager;

		public virtual void Initialize(IServiceManager svcMgr)
		{
			serviceManager = svcMgr;
		}

		public virtual void FinishedInitialization()
		{
		}
	}
}
