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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;

using Clifton.Core.Assertions;
using Clifton.Core.Exceptions;
using Clifton.Core.ExtensionMethods;
using Clifton.Core.Semantics;

namespace Clifton.Core.ModuleManagement
{
	public class ModuleManager : IModuleManager
	{
		protected List<IModule> registrants;

		public ReadOnlyCollection<IModule> Modules { get { return registrants.AsReadOnly(); } }

		public ModuleManager()
		{
		}

		/// <summary>
		/// Register modules specified in a list of assembly filenames.
		/// </summary>
		public virtual void RegisterModules(List<AssemblyFileName> moduleFilenames, OptionalPath optionalPath = null, Func<string, Assembly> assemblyResolver = null)
		{
			List<Assembly> modules = LoadModules(moduleFilenames, optionalPath, assemblyResolver);
			List<IModule> registrants = InstantiateRegistrants(modules);
			InitializeRegistrants(registrants);
		}

		public virtual void RegisterModulesFrom(List<AssemblyFileName> moduleFilenames, string path, Func<string, Assembly> assemblyResolver = null)
		{
			List<Assembly> modules = LoadModulesFrom(moduleFilenames, path, assemblyResolver);
			List<IModule> registrants = InstantiateRegistrants(modules);
			InitializeRegistrants(registrants);
		}

		/// <summary>
		/// Load the assemblies and return the list of loaded assemblies.  In order to register
		/// services that the module implements, we have to load the assembly.
		/// </summary>
		protected virtual List<Assembly> LoadModules(List<AssemblyFileName> moduleFilenames, OptionalPath optionalPath, Func<string, Assembly> assemblyResolver)
		{
			List<Assembly> modules = new List<Assembly>();

			moduleFilenames.ForEach(a =>
			{
				Assembly assembly = LoadAssembly(a, optionalPath, assemblyResolver);
				modules.Add(assembly);
			});

			return modules;
		}

		protected virtual List<Assembly> LoadModulesFrom(List<AssemblyFileName> moduleFilenames, string path, Func<string, Assembly> assemblyResolver = null)
		{
			List<Assembly> modules = new List<Assembly>();

			moduleFilenames.ForEach(a =>
			{
				Assembly assembly = LoadAssemblyFrom(a, path, assemblyResolver);
				modules.Add(assembly);
			});

			return modules;
		}

		/// <summary>
		/// Load and return an assembly given the assembly filename so we can proceed with
		/// instantiating the module and so the module can register its services.
		/// </summary>
		protected virtual Assembly LoadAssembly(AssemblyFileName assyName, OptionalPath optionalPath, Func<string, Assembly> assemblyResolver)
		{
			FullPath fullPath = GetFullPath(assyName, optionalPath);
			Assembly assembly = null;

			if (!File.Exists(fullPath.Value))
			{
				Assert.Not(assemblyResolver == null, "Module " + fullPath.Value + " not found.\r\n.  An assemblyResolver must be defined when attempting to load modules from the application's resources or specify the optionalPath to locate the assembly.");
				assembly = assemblyResolver(assyName.Value);
			}
			else
			{
				try
				{
					assembly = Assembly.LoadFile(fullPath.Value);
				}
				catch (Exception ex)
				{
					throw new ModuleManagerException("Unable to load module " + assyName.Value + ": " + ex.Message);
				}
			}

			return assembly;
		}

		protected virtual Assembly LoadAssemblyFrom(AssemblyFileName assyName, string path, Func<string, Assembly> assemblyResolver = null)
		{
			string fullPath = Path.Combine(path, assyName.Value);
			Assembly assembly = null;

			if (!File.Exists(fullPath))
			{
				throw new ApplicationException( "Module " + fullPath + " not found.\r\n.");
				// Assert.Not(assemblyResolver == null, "Module " + fullPath + " not found.\r\n.  An assemblyResolver must be defined when attempting to load modules from the application's resources or specify the optionalPath to locate the assembly.");
				// assembly = assemblyResolver(assyName.Value);
			}
			else
			{
				try
				{
					assembly = Assembly.LoadFile(fullPath);
				}
				catch (Exception ex)
				{
					throw new ModuleManagerException("Unable to load module " + assyName.Value + ": " + ex.Message);
				}
			}

			return assembly;
		}

		/// <summary>
		/// Instantiate and return the list of registratants -- assemblies with classes that implement IModule.
		/// The registrants is one and only one class in the module that implements IModule, which we can then
		/// use to call the Initialize method so the module can register its services.
		/// </summary>
		protected virtual List<IModule> InstantiateRegistrants(List<Assembly> modules)
		{
			registrants = new List<IModule>();
			modules.ForEach(m =>
			{
				IModule registrant = InstantiateRegistrant(m);
				registrants.Add(registrant);
			});

			return registrants;
		}

		/// <summary>
		/// Instantiate a registrant.  A registrant must have one and only one class that implements IModule.
		/// The registrant is one and only one class in the module that implements IModule, which we can then
		/// use to call the Initialize method so the module can register its services.
		/// </summary>
		protected virtual IModule InstantiateRegistrant(Assembly module)
		{
			var classesImplementingInterface = module.GetTypes().
					Where(t => t.IsClass).
					Where(c => c.GetInterfaces().Where(i => i.Name == "IModule").Count() > 0);

			Assert.That(classesImplementingInterface.Count() <= 1, "Module can only have one class that implements IModule");
			Assert.That(classesImplementingInterface.Count() != 0, "Module does not have any classes that implement IModule");

			Type implementor = classesImplementingInterface.Single();
			IModule instance = Activator.CreateInstance(implementor) as IModule;

			return instance;
		}

		/// <summary>
		/// Initialize each registrant.  This method should be overridden by your application needs.
		/// </summary>
		protected virtual void InitializeRegistrants(List<IModule> registrants)
		{
		}

		/// <summary>
		/// Return the full path of the executing application (here we assume that ModuleManager.dll is in that path) and concatenate the assembly name of the module.
		/// .NET requires the the full path in order to load the associated assembly.
		/// </summary>
		protected virtual FullPath GetFullPath(AssemblyFileName assemblyName, OptionalPath optionalPath)
		{
			string appLocation;
			string assyLocation = Assembly.GetExecutingAssembly().Location;

			// An assembly that is loaded as a resource will have its assembly location as "".
			if (assyLocation == "")
			{
				Assert.Not(optionalPath == null, "Assemblies embedded as resources require that the optionalPath parameter specify the path to resolve assemblies.");
				appLocation = optionalPath.Value;       // Must be specified!  Here the optional path is the full path?  This gives two different meanings to how optional path is used!
			}
			else
			{
				appLocation = Path.GetDirectoryName(assyLocation);

				if (optionalPath != null)
				{
					appLocation = Path.Combine(appLocation, optionalPath.Value);
				}
			}

			string fullPath = Path.Combine(appLocation, assemblyName.Value);

			return FullPath.Create(fullPath);
		}
	}
}
