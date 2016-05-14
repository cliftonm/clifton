using System;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;

using Clifton.DbContextService.ExtensionMethods;

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

		public bool RecordExists<T>(Expression<Func<T, bool>> whereClause) where T : class, IEntity
		{
			return context.Query<T>(whereClause).Count() != 0;
		}
}

	public class DbContextReceptor : IReceptor
	{
	}
}
