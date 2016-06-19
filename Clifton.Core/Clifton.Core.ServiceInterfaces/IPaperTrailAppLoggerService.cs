using System;
using Clifton.Core.ServiceManagement;

namespace Clifton.Core.ServiceInterfaces
{
	public interface IPaperTrailAppLoggerService : ILoggerService 
	{
		void Log(string tid, string msg);
		void Log(string tid, Exception ex);
	}
}
