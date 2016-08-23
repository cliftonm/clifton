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
		void RegisterSingleton<I, S>(Action<I> initializer = null)
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
		T Get<T>(Action<T> initializer = null) where T : IService;

		/// <summary>
		/// Instantiate the service as a unique instance, if the service allows instance instantiation.
		/// </summary>
		T GetInstance<T>(Action<T> initializer = null)
			where T : IService;
				
		/// <summary>
		/// Get the singleton service, if the service can behave like a singleton.
		/// </summary>
		T GetSingleton<T>(Action<T> initializer = null) 
			where T : IService;
	}
}
