using System;

using Clifton.Core.ServiceManagement;

namespace Clifton.Core.ServiceInterfaces
{
	public interface IEmailExceptionLoggerService : IService 
	{
		void Log(string msg);
		void Log(ExceptionMessage msg);
		void Log(Exception ex);
	}
}
