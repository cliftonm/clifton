using System;

namespace Clifton.Core.ServiceManagement
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
		/// Associate a concrete instance provided by the caller with its specific interface.
		/// </summary>
		void RegisterSingleton<I>(IService svc)
			where I : IService;

		/// <summary>
		/// Returns true if the service exists.  Useful for testing whether
		/// a service has been included in the application.
		/// </summary>
		bool Exists<T>() where T : IService;

		bool IfExists<T>(Action<T> action) where T : IService;

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
