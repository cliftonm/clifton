using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq.Expressions;

using System.Xml.Serialization;

using Clifton.Core.ModelTableManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;

namespace Clifton.Core.ServiceInterfaces
{
	public interface IDbContextService : IService
	{
		void InitializeContext(DataContext context);

		/// <summary>
		/// Returns true if tables were created.
		/// </summary>
		bool CreateDatabaseAndTablesIfNotExists();

		bool RecordExists<T>(Func<T, bool> whereClause) where T : class, IEntity;
		bool RecordExists<T>(Func<T, bool> whereClause, out int id) where T : class, IEntity;
		bool RecordOfTypeExists<T>(IEntity entity, Func<T, bool> whereClause, out int id) where T : class, IEntity;
		DataContext Context { get; }
	}

	public class DbContextMembrane : Membrane { }
}
