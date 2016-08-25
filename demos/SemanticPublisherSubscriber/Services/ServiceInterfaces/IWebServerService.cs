using Clifton.Core.ServiceManagement;

namespace ServiceInterfaces
{
    public interface IWebServerService : IService
    {
        void Start(string ip, int port);
    }
}
