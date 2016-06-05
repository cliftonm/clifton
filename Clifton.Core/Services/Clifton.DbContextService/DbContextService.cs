using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModelTableManagement;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;

namespace Clifton.DbContextService
{
	public class DbContextModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IDbContextService, DbContextService>();
		}
	}

	public class DbContextService : ServiceBase, IDbContextService
	{
		protected DataContext context;

		public DataContext Context { get { return context; } }

		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			ISemanticProcessor semProc = ServiceManager.Get<ISemanticProcessor>();
			semProc.Register<EmailClientMembrane, DbContextReceptor>();
		}

		public void InitializeContext(DataContext context)
		{
			this.context = context;
		}

		public bool CreateDatabaseAndTablesIfNotExists()
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

			// TODO: Determine whether tables exist!
			// TODO: Even better, figure out the migrations based on the current schema vs. the model schema!
			return false;
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

			switch(records.Count())
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

	public class DbContextReceptor : IReceptor
	{
	}
}
