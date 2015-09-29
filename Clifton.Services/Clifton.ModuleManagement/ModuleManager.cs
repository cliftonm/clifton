using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

using Clifton.Assertions;
using Clifton.CoreSemanticTypes;
using Clifton.ExtensionMethods;
using Clifton.Semantics;
using Clifton.ServiceInterfaces;

namespace Clifton.ModuleManagement
{
    public class ModuleManager : ServiceBase, IModuleManager
    {
		public ModuleManager()
		{
		}

		/// <summary>
		/// Register all modules specified in the XML filename so that the application
		/// can gain access to the services provided in those modules.
		/// </summary>
		public virtual void RegisterModules(XmlFileName filename)
		{
			List<AssemblyFileName> moduleFilenames = GetModuleList(filename);
			List<Assembly> modules = LoadModules(moduleFilenames);
			List<IModule> registrants = InstantiateRegistrants(modules);
			InitializeRegistrants(registrants);
		}

		/// <summary>
		/// Return the list of assembly names specified in the XML file so that
		/// we know what assemblies are considered modules as part of the application.
		/// </summary>
		protected virtual List<AssemblyFileName> GetModuleList(XmlFileName filename)
		{
			Assert.That(File.Exists(filename.Value), "Module definition file " + filename.Value + " does not exist.");
			XDocument xdoc = XDocument.Load(filename.Value);
			return GetModuleList(xdoc);
		}

		/// <summary>
		/// Load the assemblies and return the list of loaded assemblies.  In order to register
		/// services that the module implements, we have to load the assembly.
		/// </summary>
		protected virtual List<Assembly> LoadModules(List<AssemblyFileName> moduleFilenames)
		{
			List<Assembly> modules = new List<Assembly>();

			moduleFilenames.ForEach(a =>
				{
					Assembly assembly = LoadAssembly(a);
					modules.Add(assembly);
				});

			return modules;
		}

		/// <summary>
		/// Load and return an assembly given the assembly filename so we can proceed with
		/// instantiating the module and so the module can register its services.
		/// </summary>
		protected virtual Assembly LoadAssembly(AssemblyFileName assyName)
		{
			FullPath fullPath = GetFullPath(assyName);
			Assembly assembly = Assembly.LoadFile(fullPath.Value);

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
			List<IModule> registrants = new List<IModule>();
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
			registrants.ForEach(r => r.InitializeServices(serviceManager));
		}

		/// <summary>
		/// Return the full path of the executing application (here we assume that ModuleManager.dll is in that path) and concatenate the assembly name of the module.
		/// .NET requires the the full path in order to load the associated assembly.
		/// </summary>
		protected virtual FullPath GetFullPath(AssemblyFileName assemblyName)
		{
			string appLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string fullPath = Path.Combine(appLocation, assemblyName.Value);

			return FullPath.Create(fullPath);
		}
    }
}
