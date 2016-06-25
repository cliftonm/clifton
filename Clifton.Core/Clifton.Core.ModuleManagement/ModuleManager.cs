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
using System.Xml;
using System.Xml.Linq;

using Clifton.Core.Assertions;
using Clifton.Core.ExtensionMethods;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;

namespace Clifton.Core.ModuleManagement
{
	public class ModuleManager : ServiceBase, IModuleManager
	{
		protected List<IModule> registrants;

		public ReadOnlyCollection<IModule> Modules { get { return registrants.AsReadOnly(); } }

		public ModuleManager()
		{
		}

		/// <summary>
		/// Register all modules specified in the XML filename so that the application
		/// can gain access to the services provided in those modules.
		/// </summary>
		public virtual void RegisterModules(XmlFileName filename, string optionalFolder = null, Func<string, Assembly> resourceAssemblyResolver = null)
		{
			List<AssemblyFileName> moduleFilenames = GetModuleList(filename);
			List<Assembly> modules = LoadModules(moduleFilenames, optionalFolder, resourceAssemblyResolver);
			List<IModule> registrants = InstantiateRegistrants(modules);
			InitializeRegistrants(registrants);
		}

		public virtual void RegisterModules(List<AssemblyFileName> moduleFilenames, string optionalFolder = null, Func<string, Assembly> resourceAssemblyResolver = null)
		{
			List<Assembly> modules = LoadModules(moduleFilenames, optionalFolder, resourceAssemblyResolver);
			List<IModule> registrants = InstantiateRegistrants(modules);
			InitializeRegistrants(registrants);
		}

		/// <summary>
		/// Return the list of assembly names specified in the XML file so that
		/// we know what assemblies are considered modules as part of the application.
		/// </summary>
		public virtual List<AssemblyFileName> GetModuleList(XmlFileName filename)
		{
			Assert.That(File.Exists(filename.Value), "Module definition file " + filename.Value + " does not exist.");
			XDocument xdoc = XDocument.Load(filename.Value);
			return GetModuleList(xdoc);
		}

		/// <summary>
		/// Load the assemblies and return the list of loaded assemblies.  In order to register
		/// services that the module implements, we have to load the assembly.
		/// </summary>
		protected virtual List<Assembly> LoadModules(List<AssemblyFileName> moduleFilenames, string optionalFolder, Func<string, Assembly> resourceAssemblyResolver)
		{
			List<Assembly> modules = new List<Assembly>();

			moduleFilenames.ForEach(a =>
			{
				Assembly assembly = LoadAssembly(a, optionalFolder, resourceAssemblyResolver);
				modules.Add(assembly);
			});

			return modules;
		}

		/// <summary>
		/// Load and return an assembly given the assembly filename so we can proceed with
		/// instantiating the module and so the module can register its services.
		/// </summary>
		protected virtual Assembly LoadAssembly(AssemblyFileName assyName, string optionalFolder, Func<string, Assembly> resourceAssemblyResolver)
		{
			FullPath fullPath = GetFullPath(assyName, optionalFolder);
			Assembly assembly = null;

			if (!File.Exists(fullPath.Value))
			{
				Assert.Not(resourceAssemblyResolver == null, "resourceAssemblyResolver must be defined when attempting to load modules from the application's resources.");
				assembly = resourceAssemblyResolver(assyName.Value);
			}
			else
			{
				try
				{
					assembly = Assembly.LoadFile(fullPath.Value);
				}
				catch (Exception ex)
				{
					throw new ApplicationException("Unable to load module " + assyName.Value + ": " + ex.Message);
				}
			}

			return assembly;
		}

		/// <summary>
		/// Returns the list of modules specified in the XML document so we know what
		/// modules to instantiate.
		/// </summary>
		protected virtual List<AssemblyFileName> GetModuleList(XDocument xdoc)
		{
			List<AssemblyFileName> assemblies = new List<AssemblyFileName>();
			(from module in xdoc.Element("Modules").Elements("Module")
			 select module.Attribute("AssemblyName").Value).ForEach(s => assemblies.Add(AssemblyFileName.Create(s)));

			return assemblies;
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
		/// Initialize each registrant by passing in the service manager.  This allows the module
		/// to register the services it provides.
		/// </summary>
		protected virtual void InitializeRegistrants(List<IModule> registrants)
		{
			registrants.ForEach(r => r.InitializeServices(ServiceManager));
		}

		/// <summary>
		/// Return the full path of the executing application (here we assume that ModuleManager.dll is in that path) and concatenate the assembly name of the module.
		/// .NET requires the the full path in order to load the associated assembly.
		/// </summary>
		protected virtual FullPath GetFullPath(AssemblyFileName assemblyName, string optionalFolder)
		{
			string appLocation;
			string assyLocation = Assembly.GetExecutingAssembly().Location;

			if (assyLocation == "")
			{
				Assert.Not(optionalFolder == null, "Assemblies embedded as resources require that the optionalFolder parameter specify the path to resolve assemblies.");
				appLocation = optionalFolder;       // Must be specified!
			}
			else
			{
				appLocation = Path.GetDirectoryName(assyLocation);
				appLocation = appLocation + "\\" + (optionalFolder ?? "");
			}
			string fullPath = Path.Combine(appLocation, assemblyName.Value);

			return FullPath.Create(fullPath);
		}
	}
}
