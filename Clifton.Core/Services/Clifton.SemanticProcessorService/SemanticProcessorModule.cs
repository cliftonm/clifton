using System;

using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;

namespace Clifton.Core.Services.SemanticProcessorService
{
	public class SemanticProcessorModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<ISemanticProcessor, SemanticProcessor>();
		}
	}
}
