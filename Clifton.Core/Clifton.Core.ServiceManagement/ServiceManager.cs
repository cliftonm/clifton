/* The MIT License (MIT)
* 
* Copyright (c) 2015 Marc Clifton
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Clifton.Core.Assertions;
using Clifton.Core.ExtensionMethods;

namespace Clifton.Core.ServiceManagement
{
	public class ServiceManager : ServiceBase, IServiceManager
	{
        /// <summary>
        /// Normally we have only one service manager for the entire application.
        /// If this is NOT the case, DO NOT use the singleton Instance property!
        /// </summary>
        public static IServiceManager Instance { get; protected set; }

		protected ConcurrentDictionary<Type, Type> interfaceServiceMap;
		protected ConcurrentDictionary<Type, IService> singletons;
		protected ConcurrentDictionary<Type, ConstructionOption> constructionOption;

		protected object locker = new object();

		public ServiceManager()
		{
			interfaceServiceMap = new ConcurrentDictionary<Type, Type>();
			singletons = new ConcurrentDictionary<Type, IService>();
			constructionOption = new ConcurrentDictionary<Type, ConstructionOption>();
            Instance = this;
		}

		/// <summary>
		/// Register a service S that can be instantiated as a singleton or multiple instance that implements interface I.
		/// Both S and I must implement IService.
		/// </summary>
		public virtual void Register<I, S>()
			where I : IService
			where S : IService
		{
			Type interfaceType = typeof(I);
			Type serviceType = typeof(S);
			Assert.Not(interfaceServiceMap.ContainsKey(interfaceType), "The service " + GetName<S>() + " has already been registered.");
			interfaceServiceMap[interfaceType] = serviceType;
			constructionOption[interfaceType] = ConstructionOption.SingletonOrInstance;
		}

		public virtual void RegisterInstanceOnly<I, S>()
			where I : IService
			where S : IService
		{
			Type interfaceType = typeof(I);
			Type serviceType = typeof(S);
			Assert.Not(interfaceServiceMap.ContainsKey(interfaceType), "The service " + GetName<S>() + " has already been registered.");
			interfaceServiceMap[interfaceType] = serviceType;
			constructionOption[interfaceType] = ConstructionOption.AlwaysInstance;
		}

		public virtual void RegisterSingleton<I, S>(Action<I> initializer = null)
			where I : IService
			where S : IService
		{
			Type interfaceType = typeof(I);
			Type serviceType = typeof(S);
			Assert.Not(interfaceServiceMap.ContainsKey(interfaceType), "The service " + GetName<S>() + " has already been registered.");
			interfaceServiceMap[interfaceType] = serviceType;
			constructionOption[interfaceType] = ConstructionOption.AlwaysSingleton;
			RegisterSingletonBaseInterfaces(interfaceType, serviceType);

			// Singletons are always instantiated immediately so that they can be initialized
			// for global behaviors.  A good example is the global exception handler services.
			CreateAndRegisterSingleton(initializer);
		}

		/// <summary>
		/// Associate a concrete instance provided by the caller with its specific interface.
		/// </summary>
		public virtual void RegisterSingleton<I>(IService svc)
			where I : IService
		{
			Type t = typeof(I);
			singletons[t] = svc;
			interfaceServiceMap[t] = svc.GetType();
			constructionOption[t] = ConstructionOption.AlwaysSingleton;
		}

		public virtual List<Exception> FinishSingletonInitialization()
		{
            List<Exception> exceptions = new List<Exception>();

            singletons.ForEach(kvp =>
            {
                try
                {
                    kvp.Value.FinishedInitialization();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            return exceptions;
		}

		public virtual bool Exists<T>() where T : IService
		{
			return interfaceServiceMap.ContainsKey(typeof(T));
		}

        public virtual bool IfExists<T>(Action<T> action) where T : IService
		{
			bool ret = Exists<T>();

			if (ret)
			{
				T service = Get<T>(null);
				action(service);
			}

			return ret;
		}

		public virtual T Get<T>(Action<T> initializer = null)
			where T : IService
		{
			IService instance = null;
			VerifyRegistered<T>();
			Type interfaceType = typeof(T);

			switch (constructionOption[interfaceType])
			{
				case ConstructionOption.AlwaysInstance:
					instance = CreateInstance<T>(initializer);
                    instance.Initialize(this);
					break;

				case ConstructionOption.AlwaysSingleton:
					instance = CreateOrGetSingleton(initializer);
					break;

				default:
					throw new ApplicationException("Cannot determine whether the service " + GetName<T>() + " should be created as a unique instance or as a singleton.");
			}

			return (T)instance;
		}

        public virtual T Get<T>(Type interfaceType, Action<T> initializer = null)
            where T : IService
        {
            IService instance = null;
            VerifyRegistered<T>();

            switch (constructionOption[interfaceType])
            {
                case ConstructionOption.AlwaysInstance:
                    instance = CreateInstance(interfaceType, initializer);
                    instance.Initialize(this);
                    break;

                case ConstructionOption.AlwaysSingleton:
                    instance = CreateOrGetSingleton(interfaceType, initializer);
                    break;

                default:
                    throw new ApplicationException("Cannot determine whether the service " + GetName<T>() + " should be created as a unique instance or as a singleton.");
            }

            return (T)instance;
        }

        /// <summary>
        /// If allowed, returns a new instance of the service implement interface T.
        /// </summary>
        public virtual T GetInstance<T>(Action<T> initializer = null)
			where T : IService
		{
			VerifyRegistered<T>();
			VerifyInstanceOption<T>();
			IService instance = CreateInstance<T>(initializer);
			instance.Initialize(this);

			return (T)instance;
		}

		/// <summary>
		/// If allowed, creates and registers or returns an existing service that implements interface T.
		/// </summary>
		public virtual T GetSingleton<T>(Action<T> initializer = null)
			where T : IService
		{
			VerifyRegistered<T>();
			VerifySingletonOption<T>();
			IService instance = CreateOrGetSingleton(initializer);

			return (T)instance;
		}

		/// <summary>
		/// Return a registered singleton or create it and register it if it isn't registered.
		/// </summary>
		protected IService CreateOrGetSingleton<T>(Action<T> initializer)
			where T : IService
		{
			Type t = typeof(T);
			IService instance;

			lock (locker)
			{
				if (!singletons.TryGetValue(t, out instance))
				{
					instance = CreateAndRegisterSingleton(initializer);
                }
            }

			return instance;
		}

        protected IService CreateOrGetSingleton<T>(Type t, Action<T> initializer)
            where T : IService
        {
            IService instance;

            lock (locker)
            {
                if (!singletons.TryGetValue(t, out instance))
                {
                    instance = CreateAndRegisterSingleton(initializer);
                }
            }

            return instance;
        }

        /// <summary>
        /// Create and register a singleton.
        /// </summary>
        protected virtual IService CreateAndRegisterSingleton<T>(Action<T> initializer = null)
			where T : IService
		{
			IService instance = CreateInstance(initializer);
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
				interfaceServiceMap[itype] = serviceType;
				constructionOption[itype] = ConstructionOption.AlwaysSingleton;
			}
		}

		/// <summary>
		/// Interface T must be registered.
		/// </summary>
		protected void VerifyRegistered<T>()
			where T : IService
		{
			Assert.That(Exists<T>(), "The service " + GetName<T>() + " has not been registered.");
		}

        /// <summary>
        /// Create and return the concrete instance that implements service interface T.
        /// </summary>
        protected IService CreateInstance<T>(Action<T> initializer)
			where T : IService
		{
			Type t = typeof(T);
			IService instance = (IService)Activator.CreateInstance(interfaceServiceMap[t]);
            initializer.IfNotNull((i) => i((T)instance));

            return instance;
		}

        protected IService CreateInstance<T>(Type t, Action<T> initializer)
            where T : IService
        {
            IService instance = (IService)Activator.CreateInstance(interfaceServiceMap[t]);
            initializer.IfNotNull((i) => i((T)instance));

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
