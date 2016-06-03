using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using Clifton.Core.ExtensionMethods;

namespace Clifton.Core.ModelTableManagement
{
	/// <summary>
	/// Wires up table change events so that the underlying model collection and individual model instances can be kept in sync.
	/// </summary>
	public class ModelTable<T> where T : MappedRecord, IEntity, new()
	{
		protected DataTable dt;
		protected T newInstance;
		protected List<IEntity> items;
		protected ModelMgr modelMgr;
		protected DbContextService db;

		public ModelTable(ModelMgr modelMgr, DbContextService db, DataTable backingTable, List<IEntity> modelCollection)
		{
			this.modelMgr = modelMgr;
			this.db = db;
			dt = backingTable;
			items = modelCollection;
			WireUpEvents(dt);
		}

		protected void WireUpEvents(DataTable dt)
		{
			dt.ColumnChanged += Table_ColumnChanged;
			dt.RowDeleted += Table_RowDeleted;
			dt.TableNewRow += Table_TableNewRow;
			dt.RowChanged += Table_RowChanged;
		}

		protected void Table_RowChanged(object sender, DataRowChangeEventArgs e)
		{
			switch (e.Action)
			{
				case DataRowAction.Add:
					items.Add(newInstance);
					db.Context.InsertOfConcreteType(newInstance);
					break;

				case DataRowAction.Change:
					{
						// Any change to a grid view column that is mapped to a table column causes this event to fire,
						// which results in an immediate update of the database.
						IEntity item = items.SingleOrDefault(record => ((MappedRecord)record).Row == e.Row);
						db.Context.UpdateOfConcreteType(item);
						break;
					}

				// This never happens when connected to a DataGridView.  Not sure why not.
				//case DataRowAction.Delete:
				//	{
				//		T item = items.SingleOrDefault(record => record.Row == e.Row);
				//		Globals.context.Delete(item);
				//		break;
				//	}
			}
		}

		protected void Table_TableNewRow(object sender, DataTableNewRowEventArgs e)
		{
			newInstance = new T();
			newInstance.Row = e.Row;
		}

		protected void Table_RowDeleted(object sender, DataRowChangeEventArgs e)
		{
			IEntity item = items.SingleOrDefault(record => ((MappedRecord)record).Row == e.Row);

			if (item != null)
			{
				items.Remove(item);
				db.Context.DeleteOfConcreteType(item);
			}
		}

		protected void Table_ColumnChanged(object sender, DataColumnChangeEventArgs e)
		{
			IEntity instance;

			if (e.Row.RowState == DataRowState.Detached)
			{
				instance = newInstance;
			}
			else
			{
				instance = items.SingleOrDefault(record => ((MappedRecord)record).Row == e.Row);
			}

			modelMgr.UpdateRecordField(instance, e.Column.ColumnName, e.ProposedValue);
		}
	}
}
