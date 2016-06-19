using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ServiceInterfaces;

namespace Clifton.Core.ModelTableManagement
{
	/// <summary>
	/// Views do not transact with the database, so the insert, delete, and update methods do nothing.
	/// </summary>
	public class ModelView<T> : ModelTable<T> where T : MappedRecord, IEntity, new()
	{
		public ModelView(ModelMgr modelMgr, IDbContextService db, DataTable backingTable, List<IEntity> modelCollection) :
			base(modelMgr, db, backingTable, modelCollection)
		{
		}

		protected override void Insert(T newInstance)
		{
		}

		protected override void Delete(IEntity item)
		{
		}

		protected override void Update(IEntity instance)
		{
		}
	}
}