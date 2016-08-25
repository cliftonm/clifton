using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Clifton.Core.Assertions;
using Clifton.Core.ExtensionMethods;
using Clifton.Core.Semantics;
using Clifton.Core.ModuleManagement;
using Clifton.Core.ServiceManagement;

namespace HuntTheWumpus
{
    static partial class Program
    {
        public static ServiceManager serviceManager;

        public static void InitializeBootstrap()
        {
            serviceManager = new ServiceManager();
            serviceManager.RegisterSingleton<IServiceModuleManager, ServiceModuleManager>();
        }

        public static void Bootstrap(Action<Exception> onBootstrapException)
        {
            try
            {
                IModuleManager moduleMgr = (IModuleManager)serviceManager.Get<IServiceModuleManager>();
                List<AssemblyFileName> modules = GetModuleList(XmlFileName.Create("modules.xml"));
                moduleMgr.RegisterModules(modules);
                serviceManager.FinishedInitialization();
                serviceManager.FinishedInitialization2();
            }
            catch (Exception ex)
            {
                onBootstrapException(ex);
            }
        }

        /// <summary>
        /// Return the list of assembly names specified in the XML file so that
        /// we know what assemblies are considered modules as part of the application.
        /// </summary>
        private static List<AssemblyFileName> GetModuleList(XmlFileName filename)
        {
            Assert.That(File.Exists(filename.Value), "Module definition file " + filename.Value + " does not exist.");
            XDocument xdoc = XDocument.Load(filename.Value);

            return GetModuleList(xdoc);
        }

        /// <summary>
        /// Returns the list of modules specified in the XML document so we know what
        /// modules to instantiate.
        /// </summary>
        private static List<AssemblyFileName> GetModuleList(XDocument xdoc)
        {
            List<AssemblyFileName> assemblies = new List<AssemblyFileName>();
            (from module in xdoc.Element("Modules").Elements("Module")
             select module.Attribute("AssemblyName").Value).ForEach(s => assemblies.Add(AssemblyFileName.Create(s)));

            return assemblies;
        }
    }
}
