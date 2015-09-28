using System;

using Clifton.Semantics;

namespace Clifton.ServiceInterfaces
{
	public class LogMessage : ImmutableSemanticType<LogMessage, string> { };
	public class ExceptionMessage : ImmutableSemanticType<ExceptionMessage, string> { };

	public interface ILoggerService : IService
	{
		void Log(LogMessage msg);
		void Log(ExceptionMessage msg);
		void Log(Exception ex);
	}
}
