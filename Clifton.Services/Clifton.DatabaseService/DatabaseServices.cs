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
	}
}
