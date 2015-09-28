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
		/// <summary>
		/// Register a service that can be instantiated as a singleton or an instance.
		/// </summary>
		void Register<I, S>()
			where I : IService
			where S : IService;

		/// <summary>
		/// Register a service that can only be instantiated as an instance.
		/// </summary>
		void RegisterInstanceOnly<I, S>()
			where I : IService
			where S : IService;

		/// <summary>
		/// Register a service that is instantiated once as a singleton.
		/// </summary>
		void RegisterSingleton<I, S>()
			where I : IService
			where S : IService;

		/// <summary>
		/// If the service is specifically instance-only or singleton-only, we can use this method.
		/// </summary>
		T Get<T>() where T : IService;

		/// <summary>
		/// Instantiate the service as a unique instance, if the service allows instance instantiation.
		/// </summary>
		T GetInstance<T>()
			where T : IService;
				
		/// <summary>
		/// Get the singleton service, if the service can behave like a singleton.
		/// </summary>
		T GetSingleton<T>() 
			where T : IService;
	}
}
