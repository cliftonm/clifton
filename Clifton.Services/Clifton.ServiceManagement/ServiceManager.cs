using System;
using System.Collections.Concurrent;

using Clifton.Assertions;
using Clifton.ExtensionMethods;
using Clifton.ServiceInterfaces;

namespace Clifton.ServiceManagement
{
	public class ServiceManager : IServiceManager
	{
		protected ConcurrentDictionary<Type, Type> interfaceServiceMap;
		protected ConcurrentDictionary<Type, IService> singletons;
		protected ConcurrentDictionary<Type, ConstructionOption> constructionOption;

		protected object locker = new object();

		public ServiceManager()
		{
			interfaceServiceMap = new ConcurrentDictionary<Type, Type>();
			singletons = new ConcurrentDictionary<Type, IService>();
			constructionOption = new ConcurrentDictionary<Type, ConstructionOption>();
		}

		// Strange stuff, haha.
		public void Initialize(IServiceManager svcMgr) { }

		/// <summary>
		/// Register a service S that implements interface I.
		/// Both S and I must implement IService.
		/// </summary>
		public virtual void Register<I, S>(ConstructionOption option = ConstructionOption.SingletonOrInstance)
			where I : IService
			where S : IService
		{
			Type interfaceType = typeof(I);
			Type serviceType = typeof(S);
			Assert.Not(interfaceServiceMap.ContainsKey(interfaceType), "The service " + GetName<S>() + " has already been registered.");
			interfaceServiceMap[interfaceType] = serviceType;
			constructionOption[interfaceType] = option;

			if (option == ConstructionOption.AlwaysSingleton)
			{
				RegisterSingletonBaseInterfaces(interfaceType, serviceType);

				// Singletons are always instantiated immediately so that they can be initialized
				// for global behaviors.  A good example is the global exception handler services.
				CreateAndRegister<I>();				
			}
		}

		/// <summary>
		/// If allowed, returns a new instance of the service implement interface T.
		/// </summary>
		public virtual T GetInstance<T>()
			where T : IService
		{
			VerifyRegistered<T>();
			VerifyInstanceOption<T>();
			IService instance = CreateInstance<T>();
			instance.Initialize(this);

			return (T)instance;
		}

		/// <summary>
		/// If allowed, creates and registers or returns an existing service that implements interface T.
		/// </summary>
		public virtual T GetSingleton<T>()
			where T : IService
		{
			VerifyRegistered<T>();
			VerifySingletonOption<T>();
			Type t = typeof(T);
			IService instance;

			lock (locker)
			{
				if (!singletons.TryGetValue(t, out instance))
				{
					instance = CreateAndRegister<T>();
				}
			}

			return (T)instance;
		}

		protected virtual IService CreateAndRegister<T>()
			where T : IService
		{
			IService instance = CreateInstance<T>();
			Register<T>(instance);
			instance.Initialize(this);

			return instance;
		}

		/// <summary>
		/// Singletons are allowed to also register their base type so that applications can reference singleton services by the common type
		/// rather than their instance specific interface type.
		/// </summary>
		protected virtual void RegisterSingletonBaseInterfaces(Type interfaceType, Type serviceType)
		{
			Type[] itypes = interfaceType.GetInterfaces();

			foreach (Type itype in itypes)
			{
				if (itype.Name != "IService")
				{
					interfaceServiceMap[itype] = serviceType;
					constructionOption[itype] = ConstructionOption.AlwaysSingleton;
				}
			}
		}

		/// <summary>
		/// Interface T must be registered.
		/// </summary>
		protected void VerifyRegistered<T>()
			where T : IService
		{
			Type t = typeof(T);
			Assert.That(interfaceServiceMap.ContainsKey(t), "The service " + GetName<T>() + " has not been registered.");
		}

		/// <summary>
		/// Create and return the concrete instance that implements service interface T.
		/// </summary>
		protected IService CreateInstance<T>()
			where T : IService
		{
			Type t = typeof(T);
			IService instance = (IService)Activator.CreateInstance(interfaceServiceMap[t]);

			return instance;
		}

		/// <summary>
		/// Register the service instance that implements the service interface T into the singleton dictionary.
		/// </summary>
		protected void Register<T>(IService instance)
			where T : IService
		{
			Type t = typeof(T);
			singletons[t] = instance;
		}

		protected void VerifyInstanceOption<T>()
			where T : IService
		{
			Type t = typeof(T);
			ConstructionOption opt = constructionOption[t];
			Assert.That(opt == ConstructionOption.AlwaysInstance || opt == ConstructionOption.SingletonOrInstance, "The service " + GetName<T>() + " does not support instance creation.");
		}

		protected void VerifySingletonOption<T>()
			where T : IService
		{
			Type t = typeof(T);
			ConstructionOption opt = constructionOption[t];
			Assert.That(opt == ConstructionOption.AlwaysSingleton || opt == ConstructionOption.SingletonOrInstance, "The service " + GetName<T>() + " does not support singleton creation.");
		}

		protected string GetName<T>()
		{
			return typeof(T).Name;
		}
	}
}
