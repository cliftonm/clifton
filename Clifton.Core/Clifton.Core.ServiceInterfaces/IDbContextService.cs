using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq.Expressions;

using System.Xml.Serialization;

using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;

namespace Clifton.Core.ServiceInterfaces
{
	public interface IEntity
	{
		int? Id { get; set; }
	}

	public interface INamedEntity : IEntity
	{
		string Name { get; set; }
	}

	[Table]
	public abstract class NamedEntity : IEntity
	{
		[XmlIgnore]
		public abstract int? Id { get; set; }
		[XmlIgnore]
		public abstract string Name { get; set; }
	}

	//[Table]
	//public abstract class NamedEntity
	//{
	//}

	public interface IDbContextService : IService
	{
		void InitializeContext(DataContext context);
		void CreateDatabaseAndTablesIfNotExists();
		bool RecordExists<T>(Func<T, bool> whereClause) where T : class, IEntity;
		bool RecordExists<T>(Func<T, bool> whereClause, out int id) where T : class, IEntity;
		bool RecordOfTypeExists<T>(IEntity entity, Func<T, bool> whereClause, out int id) where T : class, IEntity;
		DataContext Context { get; }
	}

	public class UniqueAttribute : Attribute { }
	public class DbContextMembrane : Membrane { }
}
