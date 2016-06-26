using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Clifton.Core.Assertions;
using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;

namespace ModuleManagerDemo
{
	class Program
	{
		static void Main(string[] args)
		{
			IModuleManager mgr = new ModuleManager();
			List<AssemblyFileName> moduleNames = GetModuleList(XmlFileName.Create("modules.xml"));
			mgr.RegisterModules(moduleNames, OptionalPath.Create("dll"));

			// The one and only module that is being loaded.
			IModule module = mgr.Modules[0];
			module.Say("Hello World.");
		}

		/// <summary>
		/// Return the list of assembly names specified in the XML file so that
		/// we know what assemblies are considered modules as part of the application.
		/// </summary>
		static private List<AssemblyFileName> GetModuleList(XmlFileName filename)
		{
			Assert.That(File.Exists(filename.Value), "Module definition file " + filename.Value + " does not exist.");
			XDocument xdoc = XDocument.Load(filename.Value);

			return GetModuleList(xdoc);
		}

		/// <summary>
		/// Returns the list of modules specified in the XML document so we know what
		/// modules to instantiate.
		/// </summary>
		static private List<AssemblyFileName> GetModuleList(XDocument xdoc)
		{
			List<AssemblyFileName> assemblies = new List<AssemblyFileName>();
			(from module in xdoc.Element("Modules").Elements("Module")
			 select module.Attribute("AssemblyName").Value).ForEach(s => assemblies.Add(AssemblyFileName.Create(s)));

			return assemblies;
		}
	}
}
