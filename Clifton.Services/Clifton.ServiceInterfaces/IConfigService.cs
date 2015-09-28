using System;

namespace Clifton.ServiceInterfaces
{
	public interface IConfigService : IService
	{
		string GetConnectionString(string key);
		string GetValue(string key);
	}
}
