using System.Collections.Generic;
using System.Data;

using Clifton.Core.Db;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;

namespace Clifton.Core.ServiceInterfaces
{
	public interface IDatabaseServices : IService
	{
		void SetConnectionString(ConnectionString connectionString);
		UserId Login(UserName username, PlainTextPassword password);
		uint GetRole(UserId id);

		bool Exists(ViewName viewName, Dictionary<string, object> parms, WhereClause where);
		int Insert(ViewName viewName, Dictionary<string, object> parms);
		T QueryScalar<T>(ViewName viewName, string fieldName, Dictionary<string, object> parms, WhereClause where);
		void Update(ViewName viewName, Dictionary<string, object> parms);
		void Delete(ViewName viewName, Dictionary<string, object> parms);
		void Delete(ViewName viewName, Dictionary<string, object> parms, WhereClause where);
		DataTable Query(ViewName viewName);
		DataTable Query(ViewName viewName, Dictionary<string, object> parms, WhereClause where);
		void FixupLookups(ViewName viewName, Dictionary<string, object> parms);
	}
}
