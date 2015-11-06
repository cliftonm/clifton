using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Clifton.CoreSemanticTypes;

namespace Clifton.ServiceInterfaces
{
	public interface IDatabaseServices : IService
	{
		void SetConnectionString(ConnectionString connectionString);
		UserId Login(UserName username, PlainTextPassword password);
		uint GetRole(UserId id);
	}
}
