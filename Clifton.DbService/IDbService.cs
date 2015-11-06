using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.DbServices
{
	public interface IDbService
	{
		void LoadViewDefinitions();
		bool Exists(IViewInfo view, Dictionary<string, object> parms, string where = null);
		void Update(IViewInfo view, Dictionary<string, object> fields);
		void Update(IViewInfo view, string where, Dictionary<string, object> fields, Dictionary<string, object> parms);
		decimal Insert(IViewInfo view, Dictionary<string, object> parms);
		T QueryScalar<T>(IViewInfo view, string fieldName, Dictionary<string, object> parms, string where = null);
		DataTable Query(string dataTableName, string sql, Dictionary<string, object> parms = null);
		DataTable Query(IViewInfo view, int top = 0, string where = null, string orderBy = null, Dictionary<string, object> parms = null);
		IViewInfo GetView(string viewName);
		List<T> Populate<T>(DataTable dt) where T : new();
	}
}
