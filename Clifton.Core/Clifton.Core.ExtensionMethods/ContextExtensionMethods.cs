/* The MIT License (MIT)
* 
* Copyright (c) 2015 Marc Clifton
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

// While posted in 2/5/08, this is still a great read:
// https://weblog.west-wind.com/posts/2008/Feb/05/Linq-to-SQL-DataContext-Lifetime-Management

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

using Clifton.Core.Assertions;
using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModelTableManagement;

namespace Clifton.Core.ExtensionMethods
{
	public class EntityProperty
	{
		public PropertyInfo Property { get; set; }
	}

	// From ideas here: http://stackoverflow.com/questions/637117/how-to-get-the-tsql-query-from-linq-datacontext-submitchanges
	public class SqlLogWriter : TextWriter
	{
		public override Encoding Encoding { get { return Encoding.Default; } }

		protected Action<string> logger;

		public SqlLogWriter(Action<string> logger)
		{
			this.logger = logger;
		}

		public override void Write(char[] buffer, int index, int count)
		{
			Write(new string(buffer, index, count));
		}

		public override void Write(char value)
		{
			Write(value.ToString());
		}

		public override void Write(string value)
		{
			logger.IfNotNull(l => l(value));
		}
	}

	// !!! Note that a new SqlConnection must always be created so that we're not using the state of any existing context. !!!
	// TODO: Probably should be wrapped in a "using" statement so it gets disposed immediately.

	// A very interesting point here: http://stackoverflow.com/questions/4605638/multi-threading-with-linq-to-sql
	// You shouldn't share a DataContext across threads. It is inherently unsafe. 
	// Additionally, DataContexts are meant to be used one per unit of work (i.e., one per conversation). 
	// Each request should be considered a different conversation and should be answered with a unique DataContext.

	// The reason for all these extension methods, besides getting Linq2Sql to work in a more general purpose way, is 
	// for the reason stated above, especially since a lot of what happens, whether handling RabbitMQ messages from
	// a Beaglebone or implementing a web server, occurs on different threads, and certainly on threads that did not
	// create the original DataContext.

	public static class ContextExtensionMethods
	{
		public static Action<Exception> ExceptionHandler = null;
		public static Action<string> SqlLogger = null;

        /*
		/// <summary>
		/// Clone the entity so we it loses its association with any existing data context.
		/// </summary>
		public static T CloneEntity<T>(this T originalEntity)
		{
			Type entityType = typeof(T);
			DataContractSerializer ser = new DataContractSerializer(entityType);

			using (MemoryStream ms = new MemoryStream())
			{
				ser.WriteObject(ms, originalEntity);
				ms.Position = 0;
				return (T)ser.ReadObject(ms);
			}
		}

		public static T CloneEntityOfConcreteType<T>(this T originalEntity)
		{
			Type entityType = typeof(T);
			DataContractSerializer ser = new DataContractSerializer(entityType, new Type[] { originalEntity.GetType() });

			using (MemoryStream ms = new MemoryStream())
			{
				ser.WriteObject(ms, originalEntity);
				ms.Position = 0;
				return (T)ser.ReadObject(ms);
			}
		}
        */

        /* !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        We create a new context from an EXISTING, NON-DISPOSED context.  If we don't create a new context,
        calls to GetTable<T> return the previously loaded data rather than actually hitting the DB for new data.

        FURTHERMORE, when EntityRef or EntitySet is used, we can't dispose of the context anyways!!!

            The model that derives from DataContext should be implemented as:

	            public class ModelDataContext : DataContext
	            {
                    public static ModelDataContext Context { get { return context; } }

                    public static ModelDataContext Factory(DbConnection conn)
                    {
                        ModelDataContext ret;

                        if (Context == null)
                        {
                            ret = new ModelDataContext(conn);
                        }
                        else
                        {
                            ret = context;
                        }

                        return ret;
                    }

                    private static ModelDataContext context;
                    private ModelDataContext() : base(Context.Connection) { }

		            private ModelDataContext(DbConnection conn) : base(conn)
		            {
			            context = this;
		            }

                    public Table<ATable> ATables => GetTable<ATable>();
                    ... etc ...

           !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! */

        /*
        public static void CreateNewContext(DataContext context, out SqlConnection connection, out DataContext newContext)
        {
            connection = new SqlConnection(context.Connection.ConnectionString);
            Type t = context.GetType();
            ConstructorInfo ci = t.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { }, null);
            newContext = (DataContext)ci.Invoke(new object[] { });
            newContext.Log = new SqlLogWriter(SqlLogger);
        }
        */

        public static void CreateNewContext(DataContext context, out SqlConnection connection, out DataContext newContext)
        {
            connection = new SqlConnection(context.Connection.ConnectionString);
            newContext = (DataContext)Activator.CreateInstance(context.GetType(), new object[] { connection });
            newContext.Log = new SqlLogWriter(SqlLogger);
        }

        public static List<T> Query<T>(this DataContext context, Expression<Func<T, bool>> whereClause = null) where T : class, IEntity
		{
            SqlConnection connection;
            DataContext newContext;
            CreateNewContext(context, out connection, out newContext);
            List<T> data = null;

            try
            {
                if (whereClause == null)
                {
                    data = newContext.GetTable<T>().ToList();
                }
                else
                {
                    data = newContext.GetTable<T>().Where(whereClause).ToList();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
            //finally
            //{
            //    newContext.Dispose();
            //}

			return data;
		}

		public static T Single<T>(this DataContext context, Expression<Func<T, bool>> whereClause = null) where T : class, IEntity
		{
            SqlConnection connection;
            DataContext newContext;
            CreateNewContext(context, out connection, out newContext);
            List<T> data = null;
			T ret = null;

			try
			{
				if (whereClause == null)
				{
					data = newContext.GetTable<T>().ToList();
				}
				else
				{
					data = newContext.GetTable<T>().Where(whereClause).ToList();
				}

                if (data.Count == 0)
                {
                    throw new ApplicationException("Now rows were returned querying Single for " + typeof(T).Name);
                }
                else if (data.Count > 1)
                {
                    throw new ApplicationException("More than one row was returned querying Single for " + typeof(T).Name);
                }

                ret = data[0];
			}
			catch (Exception ex)
			{
				LogException(ex);
				throw;
			}
            //finally
            //{
            //    newContext.Dispose();
            //}

            return ret;
		}

		public static T SingleOrDefault<T>(this DataContext context, Expression<Func<T, bool>> whereClause = null) where T : class, IEntity
		{
			SqlConnection connection;
			DataContext newContext;
            CreateNewContext(context, out connection, out newContext);
			List<T> data = null;
			T ret = null;

			try
			{
				if (whereClause == null)
				{
					data = newContext.GetTable<T>().ToList();
				}
				else
				{
					data = newContext.GetTable<T>().Where(whereClause).ToList();
				}

                if (data.Count == 1)
                {
                    ret = data[0];
                }
                else if (data.Count > 0)
                {
                    throw new ApplicationException("More than one row was returned querying SingleOrDefault for " + typeof(T).Name);
                }
			}
			catch (Exception ex)
			{
				LogException(ex);
				throw;
			}
            //finally
            //{
            //    newContext.Dispose();
            //}

            return ret;
		}

        // Ex: var cio = ModelDataContext.Context.LastOrDefault<StaffCheckInCheckOut, DateTime>(s => s.When);
        public static T FirstOrDefault<T, TOrderBy>(
            this DataContext context, 
            Expression<Func<T, TOrderBy>> orderBy, 
            Expression<Func<T, bool>> whereClause = null) where T : class, IEntity
        {
            SqlConnection connection;
            DataContext newContext;
            CreateNewContext(context, out connection, out newContext);
            T ret = null;

            try
            {
                if (whereClause == null)
                {
                    ret = newContext.GetTable<T>().OrderBy(orderBy).FirstOrDefault();
                }
                else
                {
                    ret = newContext.GetTable<T>().OrderBy(orderBy).Where(whereClause).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
            //finally
            //{
            //    newContext.Dispose();
            //}

            return ret;
        }

        // Example: var cio = ModelDataContext.Context.LastOrDefault<StaffCheckInCheckOut, DateTime>(s => s.When);
        public static T LastOrDefault<T, TOrderBy>(
            this DataContext context, 
            Expression<Func<T, TOrderBy>> orderBy, 
            Expression<Func<T, bool>> whereClause = null) where T : class, IEntity
        {
            SqlConnection connection;
            DataContext newContext;
            CreateNewContext(context, out connection, out newContext);
            T ret = null;

            try
            {
                if (whereClause == null)
                {
                    ret = newContext.GetTable<T>().OrderByDescending(orderBy).FirstOrDefault();
                }
                else
                {
                    ret = newContext.GetTable<T>().OrderByDescending(orderBy).Where(whereClause).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
            //finally
            //{
            //    newContext.Dispose();
            //}

            return ret;
        }

        // Version without expression, so the where clause is not passed on to SQL Server.
        // Use this when the expression cannot be translated to a SQL Server expression.
        public static T SingleOrDefault2<T>(this DataContext context, Func<T, bool> whereClause = null) where T : class, IEntity
        {
            SqlConnection connection;
            DataContext newContext;
            CreateNewContext(context, out connection, out newContext);
            List<T> data = null;
            T ret = null;

            try
            {
                if (whereClause == null)
                {
                    data = newContext.GetTable<T>().ToList();
                }
                else
                {
                    data = newContext.GetTable<T>().Where(whereClause).ToList();
                }

                if (data.Count == 1)
                {
                    ret = data[0];
                }
                else if (data.Count > 0)
                {
                    throw new ApplicationException("More than one row was returned querying SingleOrDefault for " + typeof(T).Name);
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
            //finally
            //{
            //    newContext.Dispose();
            //}

            return ret;
        }

        public static int Count<T>(this DataContext context, Expression<Func<T, bool>> whereClause = null) where T : class, IEntity
		{
            SqlConnection connection;
            DataContext newContext;
            CreateNewContext(context, out connection, out newContext);
            int count = 0;

			try
			{
				if (whereClause == null)
				{
					count = newContext.GetTable<T>().Count();
				}
				else
				{
					count = newContext.GetTable<T>().Where(whereClause).Count();
				}
			}
			catch (Exception ex)
			{
				LogException(ex);
				throw;
			}
            //finally
            //{
            //    newContext.Dispose();
            //}

            return count;
		}

        public static bool Exists<T>(this DataContext context, Expression<Func<T, bool>> whereClause) where T : class, IEntity
        {
            SqlConnection connection;
            DataContext newContext;
            CreateNewContext(context, out connection, out newContext);
            bool exists = false;

            try
            {
                exists = newContext.GetTable<T>().Any(whereClause);
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
            //finally
            //{
            //    newContext.Dispose();
            //}

            return exists;
        }

        public static List<MappedRecord> GetTable(this DataContext context, string tableName)
		{
			EntityProperty model = GetEntityProperty(context, tableName);
			ITable records = (ITable)model.Property.GetValue(context, null);

			return records.Cast<MappedRecord>().ToList();
		}

		private static EntityProperty GetEntityProperty(DataContext context, IEntity entity)
		{
			return GetEntityProperty(context, entity.GetType().Name);
		}

		private static EntityProperty GetEntityProperty(DataContext context, string tableName)
		{
			EntityProperty property = null;

			try
			{
				property = (from prop in context.GetType().GetProperties()
							where prop.GetMethod.ReturnType.Name.BeginsWith("Table`")		// look for Table<> return types.
							&& prop.GetMethod.ReturnType.GenericTypeArguments[0].Name == tableName
							select new EntityProperty
							{
								Property = prop,
							}).Single();
			}
			catch
			{
				throw new ApplicationException("Can't acquire Entity from Context.  Does the entity " + tableName + " have a getter entry in the Context class?");
			}

			return property;
		}

		public static List<T> QueryOfConreteType<T>(this DataContext context, IEntity entity, Expression<Func<T, bool>> whereClause = null) where T : class, IEntity
		{
			return context.QueryOfConreteType(entity.GetType().Name, whereClause);
		}

		public static List<T> QueryOfConreteType<T>(this DataContext context, string tableName, Expression<Func<T, bool>> whereClause = null) where T : class, IEntity
		{
			SqlConnection connection;
			DataContext newContext;
			CreateNewContext(context, out connection, out newContext);
	
			// TODO: What is this? newContext.Mapping;
			List<T> data = new List<T>();

			try
			{
				EntityProperty model = GetEntityProperty(newContext, tableName);

				if (whereClause == null)
				{
					var records = model.Property.GetValue(context, null);
					data = ((ITable)records).Cast<T>().ToList();
				}
				else
				{
					// var records = newContext.GetType().GetProperty(collectionName).GetValue(context, null);
					var records = model.Property.GetValue(context, null);
					data = ((ITable)records).Cast<T>().Where(whereClause).ToList();
					//int count = records.Count();
				}
			}
			catch (Exception ex)
			{
				LogException(ex);
				throw;
			}
            //finally
            //{
            //    newContext.Dispose();
            //}

            return data;
		}

		public static int CountOfConcreteType<T>(this DataContext context, IEntity entity, Expression<Func<T, bool>> whereClause = null) where T : class, IEntity
		{
			SqlConnection connection;
			DataContext newContext;
			CreateNewContext(context, out connection, out newContext);
			// TODO: What is this? newContext.Mapping;
			int count = 0;

			try
			{
				EntityProperty model = GetEntityProperty(newContext, entity);

				if (whereClause == null)
				{
					var records = model.Property.GetValue(context, null);
					count = ((ITable)records).Cast<T>().Count();
				}
				else
				{
					// var records = newContext.GetType().GetProperty(collectionName).GetValue(context, null);
					var records = model.Property.GetValue(context, null);
					count = ((ITable)records).Cast<T>().Count();
				}
			}
			catch (Exception ex)
			{
				LogException(ex);
				throw;
			}
            //finally
            //{
            //    newContext.Dispose();
            //}

            return count;
		}

		/// <summary>
		/// Create an extension method Insert on the context that auto-populates the Id.
		/// The record is immediately inserted as well.
		/// </summary>
		public static int Insert<T>(this DataContext context, T data) where T : class, IEntity
		{
			SqlConnection connection;
			DataContext newContext;
			CreateNewContext(context, out connection, out newContext);

			try
			{
				newContext.GetTable<T>().InsertOnSubmit(data);
				newContext.SubmitChanges();
				SetCreatedOnFieldValue(newContext, data, newContext.Mapping.GetTable(typeof(T)).TableName);
			}
			catch (Exception ex)
			{
				LogException(ex);
				throw;
			}
            //finally
            //{
            //    newContext.Dispose();
            //}

            return (int)data.Id;
		}

        /// <summary>
        /// Insert the items in the collection.
        /// </summary>
        public static void InsertRange<T>(this DataContext context, IEnumerable<T> dataItems) where T : class, IEntity
        {
            SqlConnection connection;
            DataContext newContext;
            CreateNewContext(context, out connection, out newContext);

            try
            {
                newContext.GetTable<T>().InsertAllOnSubmit(dataItems);
                dataItems.ForEach(data => SetCreatedOnFieldValue(newContext, data, newContext.Mapping.GetTable(typeof(T)).TableName));
                newContext.SubmitChanges();
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
            //finally
            //{
            //    newContext.Dispose();
            //}
        }

        private static void SetCreatedOnFieldValue<T>(DataContext context, T data, string tableName) where T : class, IEntity
		{
			if (data is ICreateUpdate)
			{
				string idFieldName = GetPkFieldName(data);		// Because some DB's may not use "Id" as the PK field name.  TODO: Support multiple PK fields?
				DateTime dt = context.ExecuteQuery<DateTime>("update " + tableName + " set CreatedOn = SYSDATETIME(), UpdatedOn = SYSDATETIME()  OUTPUT INSERTED.CreatedOn where " + idFieldName + " = " + ((int)data.Id).ToString()).Single();
				((ICreateUpdate)data).CreatedOn = dt;
				((ICreateUpdate)data).UpdatedOn = dt;
			}
		}

		private static void SetUpdatedOnFieldValue<T>(DataContext context, T data, string tableName) where T : class, IEntity
		{
			if (data is ICreateUpdate)
			{
				string idFieldName = GetPkFieldName(data);		// Because some DB's may not use "Id" as the PK field name.  TODO: Support multiple PK fields?
				DateTime dt = context.ExecuteQuery<DateTime>("update " + tableName + " set UpdatedOn = SYSDATETIME()  OUTPUT INSERTED.UpdatedOn where " + idFieldName + " = " + ((int)data.Id).ToString()).Single();
				((ICreateUpdate)data).UpdatedOn = dt;
			}
		}

		private static string GetPkFieldName<T>(T data)
		{
			Type t = data.GetType();

			var props = (from prop in t.GetProperties()
						where Attribute.IsDefined(prop, typeof(ColumnAttribute))
						select new 
						{
							PkFieldName = Attribute.IsDefined(prop, typeof(ColumnAttribute)) ? ((ColumnAttribute)prop.GetCustomAttribute(typeof(ColumnAttribute))).Name ?? prop.Name : prop.Name,
							IsPrimaryKey = Attribute.IsDefined(prop, typeof(ColumnAttribute)) ? ((ColumnAttribute)prop.GetCustomAttribute(typeof(ColumnAttribute))).IsPrimaryKey : false,
						}).Where(p=>p.IsPrimaryKey);

			// TODO: Support multiple PK fields?
			Assert.That(props.Count() == 1, "Expected one and only one primary key field to be defined.");

			return props.ToList()[0].PkFieldName;
		}

		public static int InsertOfConcreteType<T>(this DataContext context, T data) where T : class, IEntity
		{
			SqlConnection connection;
			DataContext newContext;
			CreateNewContext(context, out connection, out newContext);

			try
			{
				// T cloned = CloneEntityOfConcreteType(data);
				EntityProperty model = GetEntityProperty(newContext, data);
				var records = model.Property.GetValue(newContext, null);
				((ITable)records).InsertOnSubmit(data);
				newContext.SubmitChanges();
				SetCreatedOnFieldValue(newContext, data, newContext.Mapping.GetTable(records.GetType().GenericTypeArguments[0]).TableName);
				// data.Id = cloned.Id;
			}
			catch (Exception ex)
			{
				LogException(ex);
				throw;
			}
            //finally
            //{
            //    newContext.Dispose();
            //}

            return (int)data.Id;
		}

        public static void DeleteAll<T>(this DataContext context) where T : class, IEntity
        {
            SqlConnection connection;
            DataContext newContext;
            CreateNewContext(context, out connection, out newContext);

            try
            {
                // T cloned = CloneEntity(data);		    		    // Disconnect from any other context.
                var records = newContext.GetTable<T>();                 // Get IEnumerable for delete.
                newContext.GetTable<T>().DeleteAllOnSubmit(records);    // We know it's only one record.
                newContext.SubmitChanges();
            }
            catch (Exception ex)
            {
                LogException(ex);
                throw;
            }
            //finally
            //{
            //    newContext.Dispose();
            //}
        }

        public static void Delete<T>(this DataContext context, T data) where T : class, IEntity
		{
			SqlConnection connection;
			DataContext newContext;
			CreateNewContext(context, out connection, out newContext);

			try
			{
				// T cloned = CloneEntity(data);													// Disconnect from any other context.
				var records = newContext.GetTable<T>().Where(t => (int)t.Id == data.Id);		// Get IEnumerable for delete.
				newContext.GetTable<T>().DeleteAllOnSubmit(records);							// We know it's only one record.
				newContext.SubmitChanges();
			}
			catch (Exception ex)
			{
				LogException(ex);
				throw;
			}
            //finally
            //{
            //    newContext.Dispose();
            //}
        }

        /// <summary>
        /// Must provide a where clause when deleting.
        /// </summary>
        public static void Delete<T>(this DataContext context, Expression<Func<T, bool>> whereClause) where T : class, IEntity
		{
			SqlConnection connection;
			DataContext newContext;
			CreateNewContext(context, out connection, out newContext);

			try
			{
				List<T> records = QueryWithContext<T>(newContext, whereClause);
				newContext.GetTable<T>().DeleteAllOnSubmit<T>(records);
				newContext.SubmitChanges();
			}
			catch (Exception ex)
			{
				LogException(ex);
				throw;
			}
            //finally
            //{
            //    newContext.Dispose();
            //}
        }

        public static void DeleteOfConcreteType<T>(this DataContext context, T data) where T : class, IEntity
		{
			SqlConnection connection;
			DataContext newContext;
			CreateNewContext(context, out connection, out newContext);

			try
			{
				// T cloned = CloneEntityOfConcreteType(data);										// Disconnect from any other context.
				EntityProperty model = GetEntityProperty(newContext, data);
				var records = model.Property.GetValue(newContext, null);
				var recordsToDelete = ((ITable)records).Cast<T>().Where(t => (int)t.Id == data.Id);	 // Cast to (int) is required because there's no mapping for int?
				((ITable)records).DeleteAllOnSubmit(recordsToDelete);						// We know it's only one record.
				newContext.SubmitChanges();
			}
			catch (Exception ex)
			{
				LogException(ex);
				throw;
			}
            //finally
            //{
            //    newContext.Dispose();
            //}
        }

        public static void Update<T>(this DataContext context, T data) where T : class, IEntity
		{
			// We have to query the record, because contexts are transactional, then copy in the changes, which marks fields in this context as changed,
			// so that the update then updates only the fields changed.
			SqlConnection connection;
			DataContext newContext;
			CreateNewContext(context, out connection, out newContext);

			try
			{
                // Gets the current record values from the DB before we update the record with any new values.
				T record = newContext.GetTable<T>().Where(t => (int)t.Id == data.Id).Single();	 // Cast to (int) is required because there's no mapping for int?
				record.CopyFrom(data);
				newContext.SubmitChanges();
				SetUpdatedOnFieldValue(newContext, data, newContext.Mapping.GetTable(typeof(T)).TableName);
			}
			catch (Exception ex)
			{
				LogException(ex);
				throw;
			}
            //finally
            //{
            //    newContext.Dispose();
            //}
        }

        public static void UpdateOfConcreteType<T>(this DataContext context, T data) where T : class, IEntity
		{
			// We have to query the record, because contexts are transactional, then copy in the changes, which marks fields in this context as changed,
			// so that the update then updates only the fields changed.
			SqlConnection connection;
			DataContext newContext;
			CreateNewContext(context, out connection, out newContext);

			try
			{
				EntityProperty model = GetEntityProperty(newContext, data);
				var records = model.Property.GetValue(newContext, null);
				T record = ((ITable)records).Cast<T>().Where(t => (int)t.Id == data.Id).Single();	 // Cast to (int) is required because there's no mapping for int?
				record.CopyFrom(data);
				newContext.SubmitChanges();
				SetUpdatedOnFieldValue(newContext, data, newContext.Mapping.GetTable(records.GetType().GenericTypeArguments[0]).TableName);
			}
			catch (Exception ex)
			{
				LogException(ex);
				throw;
			}
            //finally
            //{
            //    newContext.Dispose();
            //}
        }

        private static List<T> QueryWithContext<T>(DataContext context, Expression<Func<T, bool>> whereClause = null) where T : class, IEntity
		{
			SqlConnection connection = new SqlConnection(context.Connection.ConnectionString);
			List<T> data = null;

			try
			{
				if (whereClause == null)
				{
					data = context.GetTable<T>().ToList();
				}
				else
				{
					data = context.GetTable<T>().Where(whereClause).ToList();
				}
			}
			catch (Exception ex)
			{
				LogException(ex);
				throw;
			}

			return data;
		}

		/// <summary>
		/// Copy all properties decorated with Column attribute except the PK column.
		/// </summary>
		private static void CopyFrom<T>(this T dest, T src) where T : IEntity
		{
			Type type = src.GetType(); // typeof(T);
			var props = from prop in type.GetProperties()
						where Attribute.IsDefined(prop, typeof(ColumnAttribute))
						select new
						{
							property = prop,
							isPrimaryKey = ((ColumnAttribute)prop.GetCustomAttribute(typeof(ColumnAttribute))).IsPrimaryKey,
						};

			// TODO: Do we still need to check for PK?
			// TODO: Do we need to check ourselves for field value changes?  Is Linq2Sql updating the entire record?
			props.Where(p => !p.isPrimaryKey).ForEach(p =>
			{
				p.property.SetValue(dest, p.property.GetValue(src));
			});
		}

		public static void CreateTableIfNotExists<T>(this DataContext context) where T : IEntity
		{
			Type type = typeof(T);
			context.CreateTableIfNotExists(type);
		}

		public static void CreateTableIfNotExists(this DataContext context, Type type)
		{
			var props = from prop in type.GetProperties()
						where Attribute.IsDefined(prop, typeof(ColumnAttribute))
						select new
						{
							name = prop.Name,
							type = prop.PropertyType,
							isPrimaryKey = ((ColumnAttribute)prop.GetCustomAttribute(typeof(ColumnAttribute))).IsPrimaryKey,
							isRequired = Attribute.IsDefined(prop, typeof(RequiredAttribute)),
							isUnique = Attribute.IsDefined(prop, typeof(UniqueAttribute)),
						};

			StringBuilder sb = new StringBuilder("if not exists (select * from sys.tables t where t.name = '" + type.Name + "') create table ");
			sb.Append(type.Name);
			sb.Append("(");
			List<string> fields = new List<string>();

			// Note leading spaces in the type names.
			Dictionary<Type, string> typeMap = new Dictionary<Type, string>()
			{
				{typeof(string), " NVARCHAR(MAX)"},			// NVARCHAR supports Unicode.  TEXT is deprecated.  VARCHAR is just 8 bit chars.
				{typeof(int), " INTEGER"},
				{typeof(int?), " INTEGER"},
				{typeof(long), " INTEGER"},
				{typeof(float), " FLOAT"},
				{typeof(double), " FLOAT"},
				{typeof(bool), " BIT"},
				{typeof(bool?), " BIT"},
				{typeof(DateTime), " datetime2"},
				{typeof(DateTime?), " datetime2"},
				{typeof(byte[]), " BLOB"},
				{typeof(Guid), " UNIQUEIDENTIFIER"},
			};

			props.ForEach(p =>
			{
				StringBuilder sbField = new StringBuilder(p.name);

				if (!p.isPrimaryKey) sbField.Append(typeMap[p.type]);
				if (p.isPrimaryKey) sbField.Append(" INTEGER PRIMARY KEY IDENTITY(1, 1)");
				if (p.isRequired && !p.isPrimaryKey) sbField.Append(" NOT NULL");
				if (p.isUnique && !p.isPrimaryKey) sbField.Append(" UNIQUE");
				fields.Add(sbField.ToString());
			});

			sb.Append(String.Join(", ", fields));
			sb.Append(")");

			context.ExecuteCommand(sb.ToString());
		}

		private static void LogException(Exception ex)
		{
			ExceptionHandler.IfNotNull(e => e(ex));
		}
	}
}
