using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Clifton.Core.Assertions;
using Clifton.Core.ExtensionMethods;
using Clifton.Core.Utils;

namespace Clifton.Core.Db
{
	public struct FkPk
	{
		public ViewTableGraph FK { get; set; }
		public ViewTableGraph PK { get; set; }
	}

	public class DbService : IDbService
	{
		public enum TableRelationship
		{
			None,
			Parent,
			Child,
		}

		// protected List<string> tableAliases = Enumerable.Range('a', 26).Select(x => ((char)x).ToString()).ToList();
		protected string connectionString;

		public DbService()
		{
		}

		public void SetConnectionString(string connectionString)
		{
			this.connectionString = connectionString;
		}

		/// <summary>
		/// Initialize the connection string with the name found in the connectionStrings section of App.config.
		/// </summary>
		public DbService(string connectionString)
		{
		}

		public void LoadViewDefinitions()
		{
			// CoreViews = new ConcurrentDictionary<string, ViewInfo>();
			SqlConnection conn = OpenConnection() as SqlConnection;
			DataTable dtTables = conn.GetSchema("Tables");
			List<PrimaryKey> primaryKeys = GetPrimaryKeysForAllTables();
			List<ForeignKey> foreignKeys = GetForeignKeysForAllTables();

			// Initialize type and default value to populate the non-nullable columns.
			Dictionary<string, Tuple<Type, object>> lookupType = new Dictionary<string, Tuple<Type, object>>()
			{
				{"varbinary", new Tuple<Type, object>(typeof(byte[]), new byte[] {}) },
				{"binary", new Tuple<Type, object>(typeof(byte[]), new byte[] {}) },
				{"smallint", new Tuple<Type, object>(typeof(Int16), 0) },
				{"int", new Tuple<Type, object>(typeof(int), 0) },
				{"tinyint", new Tuple<Type, object>(typeof(byte), 0) },
				{"money", new Tuple<Type, object>(typeof(decimal), 0) },
				{"decimal", new Tuple<Type, object>(typeof(decimal), 0) },
				{"float", new Tuple<Type, object>(typeof(double), 0) },
				{"real", new Tuple<Type, object>(typeof(Single), 0) },
				{"bigint", new Tuple<Type, object>(typeof(long), 0) },
				{"numeric", new Tuple<Type, object>(typeof(decimal), 0) },
				{"char", new Tuple<Type, object>(typeof(char), "") },
				{"nchar", new Tuple<Type, object>(typeof(char), "") },
				{"nvarchar", new Tuple<Type, object>(typeof(string), "") },
				{"varchar", new Tuple<Type, object>(typeof(string), "") },
				{"ntext", new Tuple<Type, object>(typeof(string), "") },
				{"text", new Tuple<Type, object>(typeof(string), "") },
				{"bit", new Tuple<Type, object>(typeof(bool), false) },
				{"date", new Tuple<Type, object>(typeof(DateTime), new DateTime(2001, 1, 1)) },
				{"datetime", new Tuple<Type, object>(typeof(DateTime), new DateTime(2001, 1, 1)) },
				{"smalldatetime", new Tuple<Type, object>(typeof(DateTime), new DateTime(2001, 1, 1)) },
			};

			foreach (DataRow row in dtTables.Rows)
			{
				string tableName = row["TABLE_NAME"].ToString();
				DataTable dtColumns = conn.GetSchema("Columns", new[] { conn.Database, null, tableName });

				TableInfo table = new TableInfo(tableName);

				// Set FK information.
				// Group by foreign key name to get clustered fk's.
				List<ForeignKey> fkGroups = foreignKeys.Where(fk2 => fk2.TableName == tableName).DistinctBy(fk => fk.Name).ToList();

				foreach (ForeignKey fkGroup in fkGroups)
				{
					TableForeignKey tfk = new TableForeignKey();
					tfk.PkTable = fkGroup.ReferencedTableName;

					foreach (ForeignKey fk in fkGroups.Where(fk2 => fk2.TableName == tableName && fk2.Name == fkGroup.Name))
					{
						tfk.FkPkMap[fk.ColumnName] = fk.ReferencedColumnName;  // referenced column name is the PK (usually) in the referenced table.
					}

					table.References.Add(tfk);
				}

				ViewTableInfo vti = new ViewTableInfo()
				{
					TableInfo = table,
					// no alias.
				};

				// Initialize the view with the table reference.
				ViewInfo view = new ViewInfo(tableName);
				view.ViewTables.Add(vti);

				// For each table we register a TableInfo instance and a default ViewInfo view.
				ViewSchema.Schema.RegisterTable(table);
				ViewSchema.Schema.RegisterView(view);

				// Populate the table and view fields.
				foreach (DataRow col in dtColumns.Rows)
				{
					string fieldName = col.Field<string>("COLUMN_NAME");
					bool isPK = primaryKeys.Exists(k => k.TableName == tableName && k.ColumnName == fieldName);
					bool isNullable = col.Field<string>("IS_NULLABLE") == "YES";
					string dataType = col.Field<string>("DATA_TYPE");

					Assert.That(lookupType.ContainsKey(dataType), "Missing " + dataType + " from the list of known types.");

					TableFieldInfo tfi = new TableFieldInfo()
					{
						FieldName = fieldName,
						Caption = fieldName.SplitCamelCase(),
						IsPK = isPK,
						IsNullable = isNullable,
						DataType = lookupType[dataType].Item1,
						IsVisible = fieldName != "id",
						TableInfo = table,
					};

					table.Fields.Add(tfi);

					view.AddField(table.Name, new ViewFieldInfo()
					{
						FieldName = fieldName,
						TableFieldInfo = tfi,
					});
				}
			}
		}

		/// <summary>
		/// The caller provides the actual connection string.
		/// </summary>
		public void ForceConnectionString(string connectionString)
		{
			this.connectionString = connectionString;
		}

		public DataTable Query(IViewInfo iview, string whereClause, Dictionary<string, object> parms = null)
		{
			return Query(iview, 0, whereClause, null, parms);
		}

		/// <summary>
		/// Return the collection of records in the specified view.
		/// </summary>
		public DataTable Query(IViewInfo iview, int top = 0, string whereClause = null, string orderBy = null, Dictionary<string, object> parms = null)
		{
			// TODO: Implement interfaces for all ViewInfo properties.
			ViewInfo view = (ViewInfo)iview;
			DataTable dt = new DataTable();
			IDbConnection conn = OpenConnection();
			IDbCommand cmd = conn.CreateCommand();
			StringBuilder sb = GetQuery(view, top, whereClause, orderBy, parms);
			parms.IfNotNull(p => p.ForEach(parm => cmd.Parameters.Add(new SqlParameter(parm.Key, parm.Value))));

			// Get the data.
			cmd.CommandText = sb.ToString();
			SqlDataAdapter da = new SqlDataAdapter((SqlCommand)cmd);

			try
			{
				da.Fill(dt);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(sb.ToString());
				throw;
			}

			CloseConnection(conn);

			return dt;
		}

		/// <summary>
		/// Return a DataTable populated from the specified SQL query string.
		/// </summary>
		public DataTable Query(string dataTableName, string sql, Dictionary<string, object> parms = null)
		{
			// The connection cannot be persisted as everything in these calls must be thread safe.
			DataTable dt = new DataTable(dataTableName);
			IDbConnection conn = OpenConnection();
			IDbCommand cmd = conn.CreateCommand();
			cmd.CommandText = sql;
			// cmd.Transaction = externalTransaction;
			parms.IfNotNull((p) => p.ForEach(kvp => cmd.Parameters.Add(new SqlParameter(kvp.Key, kvp.Value))));
			// parms.Dump();
			SqlDataAdapter da = new SqlDataAdapter((SqlCommand)cmd);

			try
			{
				da.Fill(dt);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(sql);
				throw;
			}

			CloseConnection(conn);

			return dt;
		}

		/// <summary>
		/// Return the single field (or first field) specified in the query, as a string.
		/// DBNull is returned as null
		/// </summary>
		public object QueryScalar(string sql, Dictionary<string, object> parms = null)
		{
			// The connection cannot be persisted as everything in these calls must be thread safe.
			IDbConnection conn = OpenConnection();
			IDbCommand cmd = conn.CreateCommand();
			cmd.CommandText = sql;
			PopulateParameters(cmd, parms);
			object obj;

			try
			{
				obj = cmd.ExecuteScalar();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(sql);
				throw;
			}

			object ret = (obj == DBNull.Value ? null : obj);
			CloseConnection(conn);

			return ret;
		}

		/// <summary>
		/// We want to have the return be converted to the generic type specified in the call.
		/// </summary>
		public T QueryScalar<T>(string sql, Dictionary<string, object> parms = null)
		{
			IDbConnection conn = OpenConnection();
			IDbCommand cmd = conn.CreateCommand();
			cmd.CommandText = sql;
			PopulateParameters(cmd, parms);
			object obj;

			try
			{
				obj = cmd.ExecuteScalar();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(sql);
				throw;
			}

			T ret = default(T);

			if (obj != null)
			{
				ret = (T)Converter.Convert(obj, typeof(T));
			}

			return ret;
		}

		/// <summary>
		/// Query the view.  The field name is the aliased field name.  The qualifier parameters are the aliased parameter names.
		/// </summary>
		public T QueryScalar<T>(IViewInfo view, string fieldName, Dictionary<string, object> parms, string where)
		{
			Assert.That(parms.Count > 0, "At least one parameter must be specified for QueryScalar<T>");

			// TODO: Deal with aliased field names.
			// TODO: We need to do a full join here of all tables in the view.
			// string tableName = ((ViewInfo)view).ViewTables[0].TableInfo.Name;
			// T ret = QueryScalar<T>("select " + fieldName + " from " + tableName.Brackets() + " where " + AndOptionalWhere(where) + GetWhereClause(parms), parms);

			DataTable dt = Query(view, where, parms);
			T ret = default(T);

			if (dt.Rows.Count == 1)
			{
				ret = (T)Converter.Convert(dt.Rows[0][fieldName], typeof(T));
			}
			else
			{
				throw new ApplicationException("Expected 1 and only 1 row returned for QueryScalar");
			}

			return ret;
		}

		/// <summary>
		/// Execute the sql statement with the optional parameters.
		/// </summary>
		public void Execute(string sql, Dictionary<string, object> parms = null)
		{
			IDbConnection conn = OpenConnection();
			IDbCommand cmd = conn.CreateCommand();
			cmd.CommandText = sql;
			parms.IfNotNull((p) => p.ForEach(kvp => cmd.Parameters.Add(new SqlParameter(kvp.Key, kvp.Value))));
			cmd.ExecuteNonQuery();
			CloseConnection(conn);
		}

		public IViewInfo GetView(string viewName)
		{
			return ViewSchema.Schema.GetView(viewName);
		}

		public bool TryGetView(string viewName, out IViewInfo iview)
		{
			ViewInfo view;
			bool ret = ViewSchema.Schema.TryGetView(viewName, out view);
			iview = view;

			return ret;
		}

		/// <summary>
		/// Returns true if the record exists.
		/// </summary>
		public bool Exists(IViewInfo view, Dictionary<string, object> parms, string where)
		{
			DataTable dt = Query(view, where, parms);

			return dt.Rows.Count != 0;
		}

		/// <summary>
		/// Insert records in all tables in the view.
		/// </summary>
		public int Insert(IViewInfo iview, Dictionary<string, object> parms)
		{
			// TODO: Implement multi-table insert.  This needs to be smart, preventing joined table inserts when they're just FK helpers.  Needs to be configurable in the ViewInfo for the table collection.
			// TODO: If implementing true multi-table inserts, need to figure out how to return the ID's of all affected table records.
			
			// TODO: Implement full interfaces for the View classes so we don't have to do this casting.
			ViewInfo view = (ViewInfo)iview;

			string tableName = view.ViewTables[0].TableInfo.Name;
			Dictionary<string, object> dealiasedParms = GetFieldNames(view, tableName, parms);
			int ret = Insert(tableName, dealiasedParms);

			return ret;
		}

		/// <summary>
		/// Handles updating a multi-table view.  
		/// This is a shorthand update for schemas that use "Id" as the primary key in all tables.
		/// </summary>
		public void Update(IViewInfo iview, Dictionary<string, object> parms)
		{
			// TODO: Implement interfaces for properties.
			ViewInfo view = (ViewInfo)iview;
			// TODO: Only the master table is currently updated.  Need full implementation to update all tables in the view.
			string tableName = view.ViewTables[0].TableInfo.Name;
			Update(view, tableName, parms);
		}

		/// <summary>
		/// Update a record given the row qualifier and separate collections for the fields to update vs. the params in the qualifer.
		/// Fields are aliased, parms are not, as we assume that the caller knows the de-aliased field names.  TODO: Bad assumption???
		/// </summary>
		public void Update(IViewInfo iview, string where, Dictionary<string, object> fields, Dictionary<string, object> parms)
		{
			// TODO: Eliminate cast by implementing interfaces to all components of ViewInfo.
			ViewInfo view = (ViewInfo)iview;

			// TODO: Only the master table is currently updated.  Need full implementation to update all tables in the view.
			string tableName = view.ViewTables[0].TableInfo.Name;
			Dictionary<string, object> dealiasedFields = GetFieldNames(view, tableName, fields);

			StringBuilder sb = new StringBuilder("update");
			sb.Append(tableName.Brackets().Spaced());
			sb.Append("set ");
			sb.Append(GetCommaDelimitedSetFields(dealiasedFields));
			sb.Append(" where ");
			sb.Append(GetWhereClause(parms));
			
			IDbConnection conn = OpenConnection();
			IDbCommand cmd = conn.CreateCommand();
			PopulateParameters(cmd, dealiasedFields);
			PopulateParameters(cmd, parms);

			cmd.CommandText = sb.ToString();

			try
			{
			cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(sb.ToString());
				throw;
			}

			CloseConnection(conn);
		}

		public void Delete(IViewInfo iview, Dictionary<string, object> parms)
		{
			ViewInfo view = (ViewInfo)iview;										
			// TODO: Only the master table record is currently deleted.  Need full implementation to delete records across all tables in the view.
			// However, this also needs to be smart -- the view needs to specify whether joined tables are read-only.
			string tableName = view.ViewTables[0].TableInfo.Name;
			parms = GetFieldNames(view, tableName, parms);
			Delete(tableName, parms);
		}

		public void Delete(IViewInfo iview, string whereClause, Dictionary<string, object> parms)
		{
			ViewInfo view = (ViewInfo)iview;
			// TODO: Only the master table record is currently deleted.  Need full implementation to delete records across all tables in the view.
			// However, this also needs to be smart -- the view needs to specify whether joined tables are read-only.
			string tableName = view.ViewTables[0].TableInfo.Name;
			parms = GetFieldNames(view, tableName, parms);
			string sql = "delete from " + tableName + " where " + GetWhereClause(parms);

			IDbConnection conn = OpenConnection();
			IDbCommand cmd = conn.CreateCommand();
			PopulateParameters(cmd, parms);

			cmd.CommandText = sql;

			try
			{
				cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(sql);
				throw;
			}

			CloseConnection(conn);
		}

		public void DeleteAll(string tableName)
		{
			// Get the connection and create the command:
			IDbConnection conn = OpenConnection();
			IDbCommand cmd = conn.CreateCommand();

			cmd.CommandText = "delete from " + tableName.Brackets();

			try
			{
			cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(cmd.CommandText);
				throw;
			}

			CloseConnection(conn);
		}

		/// <summary>
		/// Using reflection, populate a list of the specified generic type from the DataTable.
		/// </summary>
		/// <typeparam name="T">The type to populate.</typeparam>
		/// <param name="dt">The source data table.</param>
		/// <returns>A list of instances of type T.</returns>
		public List<T> Populate<T>(DataTable dt) where T : new()
		{
			List<T> items = new List<T>();
			Dictionary<string, PropertyInfo> objectProperties = GetObjectFields(typeof(T));

			foreach (DataRow row in dt.Rows)
			{
				T item = new T();

				objectProperties.ForEach(kvp =>
				{
					object sourceValue = row[kvp.Key];
					object targetValue = Converter.Convert(sourceValue, kvp.Value.PropertyType);
					kvp.Value.SetValue(item, targetValue);
				});

				items.Add(item);
			}

			// Here's a more direct way of doing this in Linq
			// var data = from row in dt.AsEnumerable() select new WebServer.Models.Performer() { ID = Convert.ToInt32(row["id"]), Name = row["name"].ToString(), StageName = row["stagename"].ToString() };
			// return data.OfType<T>().ToList();

			return items;
		}

		/// <summary>
		/// Returns the number of records qualified by the optional where clause (leave off the "where" when calling) and parameters.
		/// </summary>
		public int Count(string tableName, string whereClause = null, Dictionary<string, object> parms = null)
		{
			string sql = "select count(*) from " + tableName;

			if (!String.IsNullOrEmpty(whereClause))
			{
				sql += " where " + whereClause;
			}

			int count = QueryScalar<int>(sql, parms);

			return count;
		}

		/// <summary>
		/// Get all the fields in the model decorated with the DbFieldName attribute and their associated PropertyInfo objects.
		/// We call this once, rather than computing this every single time for each model instance that we need to populate from the row.
		/// </summary>
		protected Dictionary<string, PropertyInfo> GetObjectFields(Type t)
		{
			Dictionary<string, PropertyInfo> props = new Dictionary<string, PropertyInfo>();

			t.GetProperties().ForEach(prop =>
			{
				prop.GetCustomAttribute(typeof(DbFieldNameAttribute)).IfNotNull(attr =>
				{
					string fieldName = ((DbFieldNameAttribute)attr).FieldName;
					props[fieldName] = prop;
				});
			});

			return props;
		}

		protected IDbConnection OpenConnection()
		{
			IDbConnection conn;

			conn = new SqlConnection(connectionString);
			conn.Open();

			return conn;
		}

		protected void CloseConnection(IDbConnection conn)
		{
			conn.Close();
		}

		protected Dictionary<string, object> GetPK(string tableName, Dictionary<string, object> parms)
		{
			TableInfo ti = ViewSchema.Schema.Tables[tableName];
			Dictionary<string, object> pks = new Dictionary<string,object>();

			// TODO: This assumes that the PK fields passed in as JSON parameters is an EXACT case match for the PK field names.
			ti.Fields.Where(f => f.IsPK).ForEach(f => pks[f.FieldName] = parms[f.FieldName]);

			return pks;
		}

		protected string GetWhereClause(Dictionary<string, object> parms)
		{
			List<string> parmList=new List<string>();
			// foo = @foo and ...
			parms.ForEach(kvp=>parmList.Add(kvp.Key.Brackets() + " = @" + kvp.Key));
			string joinedParms = String.Join(" and ", parmList);

			return joinedParms;
		}

		protected string GetCommaDelimitedParameters(Dictionary<string, object> parms)
		{
			List<string> parmList = new List<string>();
			// foo = @foo
			parms.ForEach(kvp => parmList.Add("@" + kvp.Key));
			string joinedParms = String.Join(", ", parmList);

			return joinedParms;
		}

		protected string GetCommaDelimitedSetFields(Dictionary<string, object> parms)
		{
			List<string> parmList = new List<string>();
			// foo = @foo, ...
			parms.ForEach(kvp => parmList.Add(kvp.Key.Brackets() + " = @" + kvp.Key));
			string joinedParms = String.Join(", ", parmList);

			return joinedParms;
		}

		/// <summary>
		/// Add parameters in the parameter dictionary to the SqlCommand.
		/// </summary>
		protected void PopulateParameters(IDbCommand cmd, Dictionary<string, object> parms)
		{
			parms.IfNotNull((p) => p.ForEach(kvp => cmd.Parameters.Add(new SqlParameter("@" + kvp.Key, kvp.Value ?? DBNull.Value))));
		}

		/// <summary>
		/// Returns a collection of field-value pairs for the specified table.  The field names are non-aliased.
		/// </summary>
		protected Dictionary<string, object> GetFieldNames(ViewInfo view, string tableName, Dictionary<string, object> parms)
		{
			Dictionary<string, object> ret = new Dictionary<string, object>();

			// For each field in the view...
			view.ViewTables.Single(vt => vt.TableInfo.Name == tableName).Fields.ForEach(vfi =>
			{
				// If the alias in the incoming params matches a view in the specified table...
				// (We have to deal with lowercase because the JSON response might be all in lowercase)
				// Then we add that field (un-aliased) and its value from the params to the returned collection.
				parms.SingleOrDefault(kvp => kvp.Key == vfi.Alias).IfTrue(kvp => !String.IsNullOrEmpty(kvp.Key), kvp2 =>
					{
						if (vfi.TableFieldInfo.DataType == typeof(DateTime))
						{
							// Parse whatever we get into an actual DateTime instance.
							if ((kvp2.Value != null) && (kvp2.Value.ToString() != ""))
							{
								try
								{
									ret[vfi.TableFieldInfo.FieldName] = DateTime.Parse(kvp2.Value.ToString());
								}
								catch
								{
									Console.WriteLine("Input string that failed date conversion: " + kvp2.Value.ToString());
									throw;
								}
							}
							else
							{
								ret[vfi.TableFieldInfo.FieldName] = "";
							}
						}
						else
						{
							ret[vfi.TableFieldInfo.FieldName] = kvp2.Value;
						}
					});
			});

			// ret.Dump();

			return ret;
		}

		protected string AndOptionalWhere(string where)
		{
			return String.IsNullOrEmpty(where) ? "" : where + " and ";
		}

		protected List<PrimaryKey> GetPrimaryKeysForAllTables()
		{
			DataTable dt = Query("TablePrimaryKeys", "SELECT i.name AS IndexName, OBJECT_NAME(ic.OBJECT_ID) AS TableName, COL_NAME(ic.OBJECT_ID,ic.column_id) AS ColumnName FROM sys.indexes AS i INNER JOIN sys.index_columns AS ic ON i.OBJECT_ID = ic.OBJECT_ID AND i.index_id = ic.index_id WHERE i.is_primary_key = 1 order by TableName, ColumnName");
			List<PrimaryKey> primaryKeys = Populate<PrimaryKey>(dt);

			return primaryKeys;
		}

		protected List<ForeignKey> GetForeignKeysForAllTables()
		{
			DataTable dt = Query("TablePrimaryKeys", 
				@"SELECT 
				f.parent_object_id, 
				f.name as IndexName,
				SCHEMA_NAME(f.schema_id) SchemaName,
				OBJECT_NAME(f.parent_object_id) TableName,
				COL_NAME(fc.parent_object_id,fc.parent_column_id) ColName,
				SCHEMA_NAME(ref.schema_id) ReferencedSchemaName,
				OBJECT_NAME(f.referenced_object_id) ReferencedTableName,
				COL_NAME(fc.referenced_object_id, fc.referenced_column_id) ReferencedColumnName
				FROM sys.foreign_keys AS f
				INNER JOIN sys.foreign_key_columns AS fc ON f.OBJECT_ID = fc.constraint_object_id
				INNER JOIN sys.tables t ON t.OBJECT_ID = fc.referenced_object_id
				INNER JOIN sys.tables ref ON ref.object_id = f.referenced_object_id
				order by IndexName, TableName, ColName, ReferencedTableName, ReferencedColumnName");

			List<ForeignKey> foreignKeys = Populate<ForeignKey>(dt);

			return foreignKeys;
		}

		/// <summary>
		/// Return true if the record, identified by the unique key field and value, exists.
		/// </summary>
		protected bool Exists(string tableName, string uniqueKey, object ukValue)
		{
			bool ret = QueryScalar<bool>("select top 1 " + uniqueKey + " from " + tableName.Brackets() + " where " + uniqueKey + "=@" + uniqueKey, new Dictionary<string, object>() { { "@" + uniqueKey, ukValue } });

			return ret;
		}

		/// <summary>
		/// Get the list of fields specified in the view, with field aliases, excluding fields that shouldn't be part of the query, using a SQL format specifier if supplied.
		/// </summary>
		protected List<string> GetViewFields(ViewInfo view)
		{
			List<string> allFields = new List<string>();
			// TODO: Replace view.IndexOf with the ViewTableInfo's Alias property (the alias for the table.)
			foreach (ViewTableInfo vti in view.ViewTables)
			{
				List<string> fields = vti.Fields.Where(
					f => !f.IsExcludedFromQuery).Select(
					f => f.TableFieldInfo.SqlFormat == null ? vti.Alias.Brackets() + "." + f.TableFieldInfo.FieldName + " as " + f.Alias : f.TableFieldInfo.SqlFormat + " as " + f.Alias).ToList();
				allFields.AddRange(fields);
			}

			return allFields;
		}

		protected StringBuilder GetQuery(ViewInfo view, int top = 0, string whereClause = null, string orderBy = null, Dictionary<string, object> parms = null)
		{
			// Build the query:
			StringBuilder sb = new StringBuilder("select ");
			List<string> tableJoins = new List<string>();
			tableJoins.Add(view.ViewTables[0].TableInfo.Name);
			List<string> fields = GetViewFields(view);

			// TODO: Move this into the view definition.
			if (top != 0)
			{
				sb.Append("top" + top.ToString().Spaced());
			}

			if (view.IsDistinct)
			{
				// TODO: SQL generator for selecting top n and distinct has not been tested!
				if (top != 0)
				{
					// put the select into an inner "from" so that we can then get the top n from the inner selection.
					// specify the fields so an order by can be placed outside of the inner select.
					sb.Append(String.Join(", ", fields));
					sb.Append(" from(distinct select ");
				}
				else
				{
					sb.Append("distinct ");
				}
			}

			sb.Append(String.Join(", ", fields));
			sb.Append(" from ");

			if (view.ViewTables.Count == 1)
			{
				// Simple one table query.
				sb.Append(view.ViewTables[0].TableInfo.Name.Brackets());
			}
			else
			{
				// Multi-table joins.
				string joins = GetJoins(view);
				sb.Append(joins.Spaced());
			}

			// Append any where clause
			if (!String.IsNullOrEmpty(whereClause))
			{
				sb.Append(("where " + whereClause).Spaced());
			}

			// close inner select if distinct with top n rows requested.
			if (view.IsDistinct && top != 0)
			{
				sb.Append(")");
			}

			// If no orderby override, use the view's orderby.  TODO: Should we really allow for this override?
			if (String.IsNullOrEmpty(orderBy) && !String.IsNullOrEmpty(view.OrderBy))
			{
				sb.Append(" order by ");
				sb.Append(view.OrderBy);
			}
			else
			{
				orderBy.IfNotNull(o => sb.Append(" order by" + o.Spaced()));
			}

			return sb;
		}

		/// <summary>
		/// From an object graph of the tables involved in the schema, return the "from" and "joins" that create the view.
		/// </summary>
		protected string GetJoins(ViewInfo view)
		{
			List<ViewTableGraph> graph = BuildTableGraph(view);
			ViewTableGraph root = GetUnreferencedChildTable(graph);
			StringBuilder sb = new StringBuilder();
			sb.Append(root.ViewTable.TableInfo.Name.Brackets() + " as " + root.ViewTable.Alias.Brackets().Spaced());

			List<FkPk> processedViews = new List<FkPk>();
			CreateJoins(sb, root, processedViews);

			return sb.ToString();
		}

		/// <summary>
		/// Traverse the graph recusrively, building the joins to parent and child tables.
		/// </summary>
		protected void CreateJoins(StringBuilder sb, ViewTableGraph node, List<FkPk> processed)
		{
			// Process parent relationships.
			foreach (ViewTableGraph parent in node.ParentTables)
			{
				// We're testing the specific fk - pk relationship.
				FkPk fkpk = new FkPk() { FK = node, PK = parent };

				if (!processed.Contains(fkpk))
				{
					sb.Append(" left join " + parent.ViewTable.TableInfo.Name.Brackets() + " as " + parent.ViewTable.Alias.Brackets() + " on ");
					string joinFields = node.ViewTable.TableInfo.GetJoinFields(parent.ViewTable.TableInfo);
					sb.Append(joinFields);
					processed.Add(fkpk);
					CreateJoins(sb, parent, processed);
				}
			}

			// Process child relationships.
			foreach (ViewTableGraph child in node.ChildTables)
			{
				// We're testing the specific fk - pk relationship.
				// Note the reversal here because the child is the FK.
				FkPk fkpk = new FkPk() { FK = child, PK = node};

				if (!processed.Contains(fkpk))
				{
					// TODO: WE NEED A TEST CASE FOR THIS SCENARIO!
					sb.Append(" left join " + child.ViewTable.TableInfo.Name.Brackets() + " as " + child.ViewTable.Alias.Brackets() + " on ");
					string joinFields = child.ViewTable.TableInfo.GetJoinFields(node.ViewTable.TableInfo);
					sb.Append(joinFields);
					processed.Add(fkpk);
					CreateJoins(sb, child, processed);
				}
			}
		}

		/// <summary>
		/// Build a list of all relationships between tables in the view.
		/// </summary>
		protected List<ViewTableGraph> BuildTableGraph(ViewInfo view)
		{
			// Start with the first view table.
			List<ViewTableGraph> graph = new List<ViewTableGraph>();

			// Initialize all graph objects.
			view.ViewTables.ForEach(vt => graph.Add(new ViewTableGraph(vt)));

			// For all the tables in the graph list...
			foreach (ViewTableGraph vtgSrc in graph)
			{
				// Test against other tables except ourselves.
				foreach (ViewTableGraph vtgTest in graph)
				{
					if (vtgSrc != vtgTest)
					{
						TableRelationship relationship = GetRelationship(vtgSrc.ViewTable, vtgTest.ViewTable);

						// We do not add the complementary parent/child because these will be discovered as we iterate through the all the tables.

						switch (relationship)
						{
							case TableRelationship.Child:
								// The src table is a child to the table under test.
								vtgSrc.ParentTables.Add(graph.Single(g=>g.ViewTable == vtgTest.ViewTable));
								break;

							case TableRelationship.Parent:
								// The table under test has a child to the soource table.
								vtgSrc.ChildTables.Add(graph.Single(g=>g.ViewTable == vtgTest.ViewTable));
								break;
						}
					}
				}
			}

			// Ensure that all tables are wired up.  We can't have a table that doesn't reference any other tables.
			foreach (ViewTableGraph item in graph)
			{
				Assert.That(item.ParentTables.Count > 0 || item.ChildTables.Count > 0, "The table " + item.ViewTable.TableInfo.Name + " is part of the view but does not have any relationships to the other tables in the view.");
			}

			return graph;
		}

		/// <summary>
		/// Determine whether the relationship between the two tables exists and whether it is child->parent or parent->child.
		/// </summary>
		protected TableRelationship GetRelationship(ViewTableInfo vtiSrc, ViewTableInfo vtiUnderTest)
		{
			TableInfo src = vtiSrc.TableInfo;
			TableInfo underTest = vtiUnderTest.TableInfo;
			TableRelationship ret = TableRelationship.None;

			// If the source contains a reference to the table under test, this in an FK->PK relationship (child->parent)
			// The source view must also contain a field referencing the under test view.
			if (src.References.Exists(tfk => tfk.PkTable == underTest.Name))
			{
				foreach (ViewFieldInfo vfi in vtiSrc.Fields)
				{
					foreach (TableForeignKey tfk in vfi.References)
					{
						if (tfk.PkTable == underTest.Name)
						{
							return TableRelationship.Child;
						}
					}
				}
			}

			// If the table under test contains a reference to the source table, this is a PK->FK relationship (parent -> child)
			if (underTest.References.Exists(tfk => tfk.PkTable == src.Name))
			{
				// The view under test must also contain a field referencing the source view.
				foreach (ViewFieldInfo vfi in vtiUnderTest.Fields)
				{
					foreach (TableForeignKey tfk in vfi.References)
					{
						if (tfk.PkTable == src.Name)
						{
							ret = TableRelationship.Parent;
						}
					}
				}
			}

			return ret;
		}

		/// <summary>
		/// Return the first table that isn't referenced by any other tables as a child.
		/// </summary>
		protected ViewTableGraph GetUnreferencedChildTable(List<ViewTableGraph> graph)
		{
			ViewTableGraph ret;

			var roots = graph.Where(vtg => vtg.ChildTables.Count == 0);

			// Assert.That(roots.Count() > 0, "Circular references are not supported.");

			if (roots.Count() == 0)
			{
				// This is a circular reference, so we'll start with the first table in the view.
				ret = graph[0];
			}
			else
			{
				ret = roots.First();
			}
	
			return ret;
		}

		/// <summary>
		/// Return a hash of column name and value for the one and only row. 
		/// Returns null if the row doesn't exist.
		/// </summary>
		protected Dictionary<string, string> QuerySingleRow(string tableName, string sql, Dictionary<string, object> parms = null)
		{
			Dictionary<string, string> ret = null;
			DataTable dt = Query(tableName, sql, parms);

			if (dt.Rows.Count == 1)
			{
				ret = new Dictionary<string, string>();
				foreach (DataColumn dc in dt.Columns)
				{
					ret[dc.ColumnName] = dt.Rows[0][dc].ToString();
				}
			}
			else if (dt.Rows.Count > 1)
			{
				Console.WriteLine(sql);
				throw new ApplicationException("Returned more than one row.");
			}

			return ret;
		}
/*
		/// <summary>
		/// If the UK field value exists, we do an update, otherwise an insert.
		/// </summary>
		protected decimal InsertOrUpdate(IViewInfo view, Dictionary<string, object> parms, bool identityInsert = false)
		{
			decimal ret = -1;

			string pkField = idField;

			// Get the primary key field and value.  We do a case-insensitve search but return the exact matching pkfield name.
			// Let the caller handle any exception.
			KeyValuePair<string, object> pkKvp = parms.Where(kvp => kvp.Key == pkField).SingleOrDefault();

			if (Exists(tableName, pkField, pkKvp.Value))
			{
				Update(tableName, parms);
			}
			else
			{
				ret = Insert(tableName, parms, identityInsert);
			}

			return ret;
		}
*/
		protected int Insert(string tableName, Dictionary<string, object> parms, bool identityInsert = false)
		{
			// Get the connection and create the command:
			IDbConnection conn = OpenConnection();
			IDbCommand cmd = conn.CreateCommand();

			// Build the update statement:
			StringBuilder sql = new StringBuilder("insert into");
			sql.Append(tableName.Brackets().Spaced());
			sql.Append("(");
			sql.Append(String.Join(",", parms.Keys));
			sql.Append(") values (");
			sql.Append(GetCommaDelimitedParameters(parms));
			sql.Append("); select SCOPE_IDENTITY()");	// See here: http://stackoverflow.com/questions/5228780/how-to-get-last-inserted-id

			if (identityInsert)
			{
				sql.Insert(0, "set IDENTITY_INSERT " + tableName.Brackets() + " ON; ");
				sql.Append("set IDENTITY_INSERT " + tableName.Brackets() + " OFF; ");
			}

			PopulateParameters(cmd, parms);
			cmd.CommandText = sql.ToString();
			object oid = null;

			try
			{
			oid = cmd.ExecuteScalar();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(sql.ToString());
				throw;
			}

			int id = -1;

			if (oid != DBNull.Value)
			{
				id = Convert.ToInt32(oid);
			}

			// We don't have an auto-increment ID field!  Bad DB design!
			if (id == -1)
			{
				// Get the PK value that the user set when creating the record.
				// We only assume a single PK in this case, and it must be a number.
				id = Convert.ToInt32(GetPK(tableName, parms).First().Value);
			}

			CloseConnection(conn);

			return id;
		}

		/// <summary>
		/// Update a record given the parameters and ID field.
		/// The ID field will be removed from the updated field set.
		/// The parms must include the ID field.
		/// </summary>
		protected void Update(ViewInfo view, string tableName, Dictionary<string, object> parms)
		{
			Dictionary<string, object> dealiasedFields = GetFieldNames(view, tableName, parms);
			Dictionary<string, object> pks = GetPK(tableName, dealiasedFields);

			// Remove pk fields, as these will not be part of the "set" values.
			pks.ForEach(pk => dealiasedFields.Remove(pk.Key));

			// Get the connection and create the command:
			IDbConnection conn = OpenConnection();
			IDbCommand cmd = conn.CreateCommand();

			// Build the update statement:
			StringBuilder sql = new StringBuilder("update");
			sql.Append(tableName.Brackets().Spaced());
			sql.Append("set ");
			sql.Append(GetCommaDelimitedSetFields(dealiasedFields));
			sql.Append(" where ");
			sql.Append(GetWhereClause(pks));
			cmd.CommandText = sql.ToString();
			PopulateParameters(cmd, dealiasedFields);
			PopulateParameters(cmd, pks);

			try
			{
			cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(cmd.CommandText);
				throw;
			}

			CloseConnection(conn);
		}

		/// <summary>
		/// Deletes a record for the given PK value in the parms collection.
		/// We use the collection because it's convenient to pass in the entire set of record fields.
		/// </summary>
		protected void Delete(string tableName, Dictionary<string, object> parms)
		{
			Dictionary<string, object> pks = GetPK(tableName, parms);

			// Get the connection and create the command:
			IDbConnection conn = OpenConnection();
			IDbCommand cmd = conn.CreateCommand();

			cmd.CommandText = "delete from " + tableName.Brackets() + " where " + GetWhereClause(pks);
			PopulateParameters(cmd, pks);

			Console.WriteLine(cmd.CommandText);

			cmd.ExecuteNonQuery();
			CloseConnection(conn);
		}
	}
}
