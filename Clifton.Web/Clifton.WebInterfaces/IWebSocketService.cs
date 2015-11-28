using Clifton.Core.ServiceManagement;

namespace Clifton.WebInterfaces
{
    public interface IWebSocketServerService : IService
    {
		void Start(string ipAddress, int port, string path);
    }

	public interface IWebSocketSession
	{
		void Reply(string msg);
	}

	public interface IWebSocketClientService : IService
	{
		void Start(string ipAddress, int port, string path);
		void Send(string msg);
	}
}
