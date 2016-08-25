using Clifton.Core.ServiceManagement;

namespace ServiceInterfaces
{
    public interface ILoggerService : IService
    {
        void Log(string msg);
    }

    public interface IConsoleLoggerService : ILoggerService { }
}
