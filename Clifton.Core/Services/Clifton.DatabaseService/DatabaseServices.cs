using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Clifton.Core.Assertions;
using Clifton.Core.Db;
using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;
using Clifton.Core.Utils;

namespace Clifton.Core.Services.DatabaseService
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
			UserId id = null;

			if (match.Count == 1)
			{
				id = UserId.Create(match[0].Field<int>("Id"));
				db.Update(db.GetView("SiteUser"), new Dictionary<string, object>() { { "Id", id.Value }, { "LastSignOn", DateTime.UtcNow } });
			}

			return id;
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

		// Because of how jqxDataTable works:
		// In a data table with a DropDownList, the selected *display name* associated with the *lookup field name* is returned.
		// We need to convert this to the non-aliased master table FK ID and lookup the value from the FK table given the name.
		// ANNOYINGLY, THIS MEANS THAT THE NAME MUST ALSO BE UNIQUE!  This should be acceptable, if not even good practice, but it's still an annoying constraint.
		public void FixupLookups(ViewName viewName, Dictionary<string, object> kvParams)
		{
			ViewInfo view = (ViewInfo)db.GetView(viewName.Value);
			// Find all lookup fields:
			view.AllFields.Where(f => f.IsFK).ForEach(f =>
			{
				object val;
				TableForeignKey tfk = f.References[0];				// assume only one FK involvement for this field.
				// assume a non-clustered fk.
				string fkFieldName = tfk.FkPkMap.First().Key;		// fk field name
				string pkFieldName = tfk.FkPkMap.First().Value;		// pk field name of referenced table.
				Assert.That(tfk.ValueFields.Count > 0, "View: " + view.Name + " -> To resolve foreign keys, at least one display field must be defined in the foreign key relationship for " + f.TableFieldInfo.TableInfo.Name + "." + fkFieldName + " -> " + tfk.PkTable + "." + pkFieldName);
				string aliasedPkValueFieldName = tfk.ValueFields[0];

				// Not all FK's in the view may be defined, especially when there are sub-classes of the GenericTableController that 
				// populate the FK values based on session parameters, for example.
				if (view.Exists(aliasedPkValueFieldName))
				{
					string dealiasedPkValueFieldName = view.GetField(aliasedPkValueFieldName).TableFieldInfo.FieldName;

					// Aliases can be null for table joins where we're just getting an ID or other fields that don't map to a lookup that needs to be resolved.
					// TODO: What was this issue really?

					if (kvParams.TryGetValue(aliasedPkValueFieldName, out val))
					{
						// Remove the *value field* name
						kvParams.Remove(aliasedPkValueFieldName);

						// "Please Choose" is text that the jqx dropdown might return, but it seems to return an empty field.
						if ((val != null) && (!String.IsNullOrEmpty(val.ToString())) && (!val.ToString().ToLower().BeginsWith("please choose")))
						{
							// Replace with the non-aliased *FK field* name.
							// TODO: This doesn't handle multiple columns referencing the same FK table.
							// Now, lookup the value in the FK table.  Sigh, this requires a database query.  If the jqxDataTable / jqxDropDownList worked correctly with ID's, this wouldn't be necessary!
							// Assume one value field.
							object id = db.QueryScalar("select ID from " + tfk.PkTable + " where " + dealiasedPkValueFieldName + " = @val", new Dictionary<string, object>() { { "val", val } });
							Assert.That(id != null, "Expected to resolve the foreign key ID for primary key table " + tfk.PkTable + " with display value of " + val.ToString().SingleQuote());

							// The update will throw an exception if a null FK is not permitted but the lookup returned a null for id.
							kvParams[fkFieldName] = id;
						}
						else
						{
							Assert.That(f.TableFieldInfo.IsNullable, "The FK " + fkFieldName + " in the view " + view.Name + " is not marked as nullable.  A value is required.");
							kvParams[fkFieldName] = null;
						}
					}
				}
			});
		}
	}
}

