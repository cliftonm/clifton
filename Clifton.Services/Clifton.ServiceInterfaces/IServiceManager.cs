using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.ServiceInterfaces
{
	public enum ConstructionOption
	{
		SingletonOrInstance,
		AlwaysInstance,
		AlwaysSingleton,
	}

	public interface IServiceManager : IService
	{
		void Register<I, S>(ConstructionOption option = ConstructionOption.SingletonOrInstance)
			where I : IService
			where S : IService;

		T GetInstance<T>()
			where T : IService;
				
		T GetSingleton<T>() 
			where T : IService;
	}
}
