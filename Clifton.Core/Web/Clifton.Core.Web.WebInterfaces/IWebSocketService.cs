using Clifton.Core.ServiceManagement;

namespace Clifton.Core.Web.WebInterfaces
{
    public interface IWebSocketServerService : IService
    {
		void Start(string ipAddress, int port, string path);
    }

	public interface IWebSocketClientService : IService
	{
		void Start(string ipAddress, int port, string path);
	}
}
