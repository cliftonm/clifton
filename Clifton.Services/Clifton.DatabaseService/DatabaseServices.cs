using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Clifton.CoreSemanticTypes;
using Clifton.DbServices;
using Clifton.Semantics;
using Clifton.ServiceInterfaces;
using Clifton.Utils;

namespace Clifton.DatabaseService
{
	public class DatabaseServicesModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IDatabaseServices, DatabaseServices>();
		}
	}

	public class DatabaseServices : ServiceBase, IDatabaseServices
	{
		protected DbService db;

		public DatabaseServices()
		{
			db = new DbService();
		}

		public void SetConnectionString(ConnectionString connectionString)
		{
			db.SetConnectionString(connectionString.Value);
			db.LoadViewDefinitions();
		}

		// TODO: This is actually a web-server user management functions.  Move them somewhere else!

		/// <summary>
		/// Verifies the username/password with the entry in the table SiteUser.
		/// Acquires the role from the table Role and sets it in the session manager.
		/// </summary>
		/// <returns>Null if user not found.</returns>
		public UserId Login(UserName username, PlainTextPassword password)
		{
			DataTable dt = db.Query(db.GetView("SiteUser"), "username=@username or email=@username", new Dictionary<string, object>()
				 {
					 {"@username", username.Value},

				 });

			List<DataRow> match = (from user in dt.AsEnumerable() where PasswordHash.ValidatePassword(password.Value, user.Field<string>("PasswordHash")) select user).ToList();

			return match.Count == 1 ? UserId.Create(match[0].Field<int>("Id")) : null;
		}

		// TODO: This is actually a web-server user management functions.  Move them somewhere else!

		public uint GetRole(UserId id)
		{
			uint mask = db.QueryScalar<uint>(db.GetView("User"), "ActivityMask", new Dictionary<string, object>() { { "userid", id.Value } }, "SiteUser.Id = @userid");

			return mask;
		}

		// Generic DB functions.

		public bool Exists(ViewName viewName, Dictionary<string, object> parms, WhereClause where)
		{
			bool exists = db.Exists(db.GetView(viewName.Value), parms, where.Value);

			return exists;
		}

		public int Insert(ViewName viewName, Dictionary<string, object> parms)
		{
			int id = db.Insert(db.GetView(viewName.Value), parms);

			return id;
		}

		public T QueryScalar<T>(ViewName viewName, string fieldName, Dictionary<string, object> parms, WhereClause where)
		{
			T ret = db.QueryScalar<T>(db.GetView(viewName.Value), fieldName, parms, where.Value);

			return ret;
		}

		public void Update(ViewName viewName, Dictionary<string, object> fields)
		{
			db.Update(db.GetView(viewName.Value), fields);
		}

		public DataTable Query(ViewName viewName)
		{
			DataTable dt = db.Query(db.GetView(viewName.Value), null);

			return dt;
		}

		public DataTable Query(ViewName viewName, Dictionary<string, object> parms, WhereClause where)
		{
			DataTable dt = db.Query(db.GetView(viewName.Value), where.Value, parms);

			return dt;
		}

		public void Delete(ViewName viewName, Dictionary<string, object> parms)
		{
			db.Delete(db.GetView(viewName.Value), parms);
		}

		public void Delete(ViewName viewName, Dictionary<string, object> parms, WhereClause where)
		{
			db.Delete(db.GetView(viewName.Value), where.Value, parms);
		}
	}
}
