using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq.Expressions;

using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;

namespace Clifton.Core.ServiceInterfaces
{
	public interface ITable { }

	public interface IEntity : ITable
	{
		int? Id { get; set; }
	}

	public interface IDbContextService : IService
	{
		void InitializeContext(DataContext context);
		void CreateDatabaseAndTablesIfNotExists();
		bool RecordExists<T>(Expression<Func<T, bool>> whereClause) where T : class, IEntity;
	}

	public class UniqueAttribute : Attribute { }
	public class DbContextMembrane : Membrane { }
}
