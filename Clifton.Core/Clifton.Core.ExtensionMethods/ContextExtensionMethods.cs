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

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModelTableManagement;

namespace Clifton.Core.ExtensionMethods
{
	public class EntityProperty
	{
		public PropertyInfo Property { get; set; }
	}

	// !!! Note that a new SqlConnection must always be created so that we're not using the state of any existing context. !!!
	// TODO: Probably should be wrapped in a "using" statement so it gets disposed immediately.

	public static class ContextExtensionMethods
	{
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

		public static List<T> Query<T>(this DataContext context, Func<T, bool> whereClause = null) where T : class, IEntity
		{
			SqlConnection connection = new SqlConnection(context.Connection.ConnectionString);
			DataContext newContext = (DataContext)Activator.CreateInstance(context.GetType(), new object[] { connection });
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
			}

			return data;
		}

		public static T Single<T>(this DataContext context, Func<T, bool> whereClause = null) where T : class, IEntity
		{
			SqlConnection connection = new SqlConnection(context.Connection.ConnectionString);
			DataContext newContext = (DataContext)Activator.CreateInstance(context.GetType(), new object[] { connection });
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

				ret = data[0];
			}
			catch (Exception ex)
			{
				LogException(ex);
			}

			return ret;
		}

		public static int Count<T>(this DataContext context, Func<T, bool> whereClause = null) where T : class, IEntity
		{
			SqlConnection connection = new SqlConnection(context.Connection.ConnectionString);
			DataContext newContext = (DataContext)Activator.CreateInstance(context.GetType(), new object[] { connection });
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
			}

			return count;
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

		public static List<T> QueryOfConreteType<T>(this DataContext context, IEntity entity, Func<T, bool> whereClause = null) where T : class, IEntity
		{
			return context.QueryOfConreteType(entity.GetType().Name, whereClause);
		}

		public static List<T> QueryOfConreteType<T>(this DataContext context, string tableName, Func<T, bool> whereClause = null) where T : class, IEntity
		{
			SqlConnection connection = new SqlConnection(context.Connection.ConnectionString);
			DataContext newContext = (DataContext)Activator.CreateInstance(context.GetType(), new object[] { connection });
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
			}

			return data;
		}

		public static int CountOfConcreteType<T>(this DataContext context, IEntity entity, Func<T, bool> whereClause = null) where T : class, IEntity
		{
			SqlConnection connection = new SqlConnection(context.Connection.ConnectionString);
			DataContext newContext = (DataContext)Activator.CreateInstance(context.GetType(), new object[] { connection });
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
			}

			return count;
		}

		/// <summary>
		/// Create an extension method Insert on the context that auto-populates the Id.
		/// The record is immediately inserted as well.
		/// </summary>
		public static int Insert<T>(this DataContext context, T data) where T : class, IEntity
		{
			SqlConnection connection = new SqlConnection(context.Connection.ConnectionString);
			DataContext newContext = (DataContext)Activator.CreateInstance(context.GetType(), new object[] { connection });
			try
			{
				T cloned = CloneEntity(data);
				newContext.GetTable<T>().InsertOnSubmit(cloned);
				newContext.Log = Console.Out;
				newContext.SubmitChanges();
				data.Id = cloned.Id;
			}
			catch (Exception ex)
			{
				LogException(ex);
			}

			return (int)data.Id;
		}

		public static int InsertOfConcreteType<T>(this DataContext context, T data) where T : class, IEntity
		{
			SqlConnection connection = new SqlConnection(context.Connection.ConnectionString);
			DataContext newContext = (DataContext)Activator.CreateInstance(context.GetType(), new object[] { connection });
			try
			{
				T cloned = CloneEntityOfConcreteType(data);
				EntityProperty model = GetEntityProperty(newContext, cloned);
				var records = model.Property.GetValue(newContext, null);
				((ITable)records).InsertOnSubmit(cloned);
				newContext.Log = Console.Out;
				newContext.SubmitChanges();
				data.Id = cloned.Id;
			}
			catch (Exception ex)
			{
				LogException(ex);
			}

			return (int)data.Id;
		}

		public static void Delete<T>(this DataContext context, T data) where T : class, IEntity
		{
			SqlConnection connection = new SqlConnection(context.Connection.ConnectionString);
			DataContext newContext = (DataContext)Activator.CreateInstance(context.GetType(), new object[] { connection });
			try
			{
				T cloned = CloneEntity(data);													// Disconnect from any other context.
				var records = newContext.GetTable<T>().Where(t => (int)t.Id == data.Id);		// Get IEnumerable for delete.
				newContext.GetTable<T>().DeleteAllOnSubmit(records);							// We know it's only one record.
				newContext.Log = Console.Out;
				newContext.SubmitChanges();
			}
			catch (Exception ex)
			{
				LogException(ex);
			}
		}

		public static void DeleteOfConcreteType<T>(this DataContext context, T data) where T : class, IEntity
		{
			SqlConnection connection = new SqlConnection(context.Connection.ConnectionString);
			DataContext newContext = (DataContext)Activator.CreateInstance(context.GetType(), new object[] { connection });
			try
			{
				T cloned = CloneEntityOfConcreteType(data);										// Disconnect from any other context.
				EntityProperty model = GetEntityProperty(newContext, data);
				var records = model.Property.GetValue(newContext, null);
				var recordsToDelete = ((ITable)records).Cast<T>().Where(t => (int)t.Id == data.Id);	 // Cast to (int) is required because there's no mapping for int?
				((ITable)records).DeleteAllOnSubmit(recordsToDelete);						// We know it's only one record.
				newContext.Log = Console.Out;
				newContext.SubmitChanges();
			}
			catch (Exception ex)
			{
				LogException(ex);
			}
		}

		public static void Update<T>(this DataContext context, T data) where T : class, IEntity
		{
			// We have to query the record, because contexts are transactional, then copy in the changes, which marks fields in this context as changed,
			// so that the update then updates only the fields changed.
			SqlConnection connection = new SqlConnection(context.Connection.ConnectionString);
			DataContext newContext = (DataContext)Activator.CreateInstance(context.GetType(), new object[] { connection });
			try
			{
				newContext.Log = Console.Out;
				T record = newContext.GetTable<T>().Where(t => (int)t.Id == data.Id).Single();	 // Cast to (int) is required because there's no mapping for int?
				record.CopyFrom(data);
				newContext.SubmitChanges();
			}
			catch (Exception ex)
			{
				LogException(ex);
			}
		}

		public static void UpdateOfConcreteType<T>(this DataContext context, T data) where T : class, IEntity
		{
			// We have to query the record, because contexts are transactional, then copy in the changes, which marks fields in this context as changed,
			// so that the update then updates only the fields changed.
			SqlConnection connection = new SqlConnection(context.Connection.ConnectionString);
			DataContext newContext = (DataContext)Activator.CreateInstance(context.GetType(), new object[] { connection });
			try
			{
				EntityProperty model = GetEntityProperty(newContext, data);
				var records = model.Property.GetValue(newContext, null);
				T record = ((ITable)records).Cast<T>().Where(t => (int)t.Id == data.Id).Single();	 // Cast to (int) is required because there's no mapping for int?
				record.CopyFrom(data);
				newContext.Log = Console.Out;
				newContext.SubmitChanges();
			}
			catch (Exception ex)
			{
				LogException(ex);
			}
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
			System.Windows.Forms.MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace, "ContextExtensionMethod Exception", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
		}
	}
}
