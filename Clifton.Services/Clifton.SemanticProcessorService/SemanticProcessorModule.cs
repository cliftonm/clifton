using System;

using Clifton.SemanticProcessorInterfaces;
using Clifton.ServiceInterfaces;

namespace Clifton.SemanticProcessorService
{
	public class SemanticProcessorModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<ISemanticProcessor, SemanticProcessor>();
		}
	}
}
