using System;
using System.Collections.Generic;
using System.Data;
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
