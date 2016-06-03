using System;
using System.Data.Linq;
using System.Linq;
using System.Reflection;

using Clifton.Core.ExtensionMethods;

namespace Clifton.Core.ModelTableManagement
{
	public class DbContextService
	{
		protected DataContext context;

		public DataContext Context { get { return context; } }

		public DbContextService(DataContext context)
		{
			this.context = context;
		}

		public void InitializeContext(DataContext context)
		{
			this.context = context;
		}

		public void CreateDatabaseAndTablesIfNotExists()
		{
			var models = from prop in context.GetType().GetProperties()
						 where prop.GetMethod.ReturnType.Name.BeginsWith("Table`")		// look for Table<> return types.
						 select new
						 {
							 Property = prop,
						 };

			// Get the generic type returned by the method in the context and create the table if missing.
			models.ForEach(m =>
			{
				PropertyInfo p = m.Property;
				Type t = p.GetMethod.ReturnType;
				Type gt = t.GenericTypeArguments[0].UnderlyingSystemType;
				context.CreateTableIfNotExists(gt);
			});
		}

		public bool RecordExists<T>(Func<T, bool> whereClause) where T : class, IEntity
		{
			return context.Count<T>(whereClause) != 0;
		}

		public bool RecordExists<T>(Func<T, bool> whereClause, out int id) where T : class, IEntity
		{
			id = -1;
			var records = context.Query<T>(whereClause);
			bool exists = false;

			if (records.Count() == 1)
			{
				id = (int)records[0].Id;
				exists = true;
			}

			return exists;
		}

		public bool RecordOfTypeExists<T>(IEntity entity, Func<T, bool> whereClause, out int id) where T : class, IEntity
		{
			id = -1;
			var records = context.QueryOfConreteType<T>(entity, whereClause);
			bool exists = false;

			switch (records.Count())
			{
				case 0:
					break;

				case 1:
					id = (int)records[0].Id;
					exists = true;
					break;

				default:
					throw new ApplicationException("Not a unique key.");
			}

			return exists;
		}
	}
}