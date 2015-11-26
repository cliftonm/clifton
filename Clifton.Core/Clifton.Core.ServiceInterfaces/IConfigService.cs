using Clifton.Core.ServiceManagement;

namespace Clifton.Core.ServiceInterfaces
{
	public interface IConfigService : IService
	{
		string GetConnectionString(string key);
		string GetValue(string key);
	}
}
