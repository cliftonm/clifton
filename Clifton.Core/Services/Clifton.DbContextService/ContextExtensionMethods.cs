using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data.SQLite;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ServiceInterfaces;

namespace Clifton.DbContextService.ExtensionMethods
{
	[Table]
	public class sqlite_sequence
	{
		[Column, Required]
		public string name { get; set; }
		[Column, Required]
		public int seq { get; set; }
	}

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
		public static List<T> Query<T>(this DataContext context, Expression<Func<T, bool>> whereClause = null) where T : class, IEntity
		{
			DataContext newContext = (DataContext)Activator.CreateInstance(context.GetType(), new object[] { context.Connection });
			List<T> data;

			if (whereClause == null)
			{
				data = newContext.GetTable<T>().ToList();
			}
			else
			{
				data = newContext.GetTable<T>().Where(whereClause).ToList();
			}

			return data;
		}

		/// <summary>
		/// Create an extension method Insert on the context that auto-populates the Id.
		/// The record is immediately inserted as well.
		/// Because of the nightmare of auto-incrementing PK's in SQLite, this all has to be done
		/// in a separate data context.  Also, data contexts are supposed to be transactional, so how TF are you supposed to actually use Linq2Sql???
		/// </summary>
		public static int Insert<T>(this DataContext context, T data) where T : class, IEntity
		{
			DataContext newContext = (DataContext)Activator.CreateInstance(context.GetType(), new object[] { context.Connection });
			T cloned = CloneEntity(data);
			// context.GetTable<T>().Attach(cloned);
			newContext.GetTable<T>().InsertOnSubmit(cloned);
			newContext.SubmitChanges();
			// select seq from sqlite_sequence where name="table_name"
			int id = Convert.ToInt32((from s in newContext.GetTable<sqlite_sequence>() where s.name == typeof(T).Name select s.seq).Single());
			data.Id = id;

			return id;
		}

		public static void Delete<T>(this DataContext context, T data) where T : class, IEntity
		{
			DataContext newContext = (DataContext)Activator.CreateInstance(context.GetType(), new object[] { context.Connection });
			T cloned = CloneEntity(data);													// Disconnect from any other context.
			var records = newContext.GetTable<T>().Where(t => (int)t.Id == data.Id);		// Get IEnumerable for delete.
			newContext.GetTable<T>().DeleteAllOnSubmit(records);							// We know it's only one record.
			newContext.SubmitChanges();
		}

		/// <summary>
		/// WTF?  We have to query the record, because contexts are transactional (another WTF!!!) and because when we insert a record (above),
		/// we have to set the Id ourselves because the Linq2SQLite doesn't do this for us, and that creates an Update transaction, and we 
		/// can't have that because you can't update PK's in Linq2Sql, and Linq2Sql is such a fucking kludge as a result.
		/// </summary>
		public static void Update<T>(this DataContext context, T data) where T : class, IEntity
		{
			DataContext newContext = (DataContext)Activator.CreateInstance(context.GetType(), new object[] { context.Connection });
			T record = newContext.GetTable<T>().Where(t => (int)t.Id == data.Id).Single();	 // Cast to (int) is required because there's no mapping for int?
			record.CopyFrom(data);
			newContext.SubmitChanges();
		}

		/// <summary>
		/// Copy all properties decorated with Column attribute except the PK column.
		/// </summary>
		public static void CopyFrom<T>(this T dest, T src) where T : IEntity
		{
			Type type = typeof(T);
			var props = from prop in type.GetProperties()
						where Attribute.IsDefined(prop, typeof(ColumnAttribute))
						select new
						{
							property = prop,
							isPrimaryKey = ((ColumnAttribute)prop.GetCustomAttribute(typeof(ColumnAttribute))).IsPrimaryKey,
						};

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

			StringBuilder sb = new StringBuilder("CREATE TABLE IF NOT EXISTS ");
			sb.Append(type.Name);
			sb.Append("(");
			List<string> fields = new List<string>();

			// Note leading spaces in the type names.
			Dictionary<Type, string> typeMap = new Dictionary<Type, string>()
			{
				{typeof(string), " TEXT"},
				{typeof(int), " INTEGER"},
				{typeof(int?), " INTEGER"},
				{typeof(long), " INTEGER"},
				{typeof(float), " REAL"},
				{typeof(double), " REAL"},
				{typeof(bool), " INTEGER"},
				{typeof(DateTime), " NUMERIC"},
				{typeof(byte[]), " BLOB"},
				{typeof(Guid), " BLOB"},
			};

			props.ForEach(p =>
			{
				StringBuilder sbField = new StringBuilder(p.name);

				if (p.isPrimaryKey) sbField.Append(" INTEGER PRIMARY KEY ASC AUTOINCREMENT");
				if (!p.isPrimaryKey) sbField.Append(typeMap[p.type]);
				if (p.isRequired && !p.isPrimaryKey) sbField.Append(" NOT NULL");
				if (p.isUnique && !p.isPrimaryKey) sbField.Append(" UNIQUE");
				fields.Add(sbField.ToString());
			});

			sb.Append(String.Join(", ", fields));
			sb.Append(")");

			context.ExecuteCommand(sb.ToString());
		}
	}
}
