/* The MIT License (MIT)
* 
* Copyright (c) 2015 Marc Clifton
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ServiceInterfaces;

namespace Clifton.Core.ModelTableManagement
{
	public class RowDeletedEventArgs : EventArgs
	{
		public IEntity Entity { get; set; }
	}

	/// <summary>
	/// Wires up table change events so that the underlying model collection and individual model instances can be kept in sync.
	/// </summary>
	public class ModelTable<T> where T : MappedRecord, IEntity, new()
	{
		public event EventHandler<RowDeletedEventArgs> RowDeleted;
		protected DataTable dt;
		protected T newInstance;
		protected List<IEntity> items;
		protected ModelMgr modelMgr;
		protected IDbContextService db;

		public ModelTable(ModelMgr modelMgr, IDbContextService db, DataTable backingTable, List<IEntity> modelCollection)
		{
			this.modelMgr = modelMgr;
			this.db = db;
			dt = backingTable;
			items = modelCollection;
			WireUpEvents(dt);
		}

		public void ResetItems(List<IEntity> modelCollection)
		{
			items = modelCollection;
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
					Insert(newInstance);
					break;

				// We don't do this here because the Table_ColumnChanged event handles persisting the change.
				//case DataRowAction.Change:
				//	{
				//		// Any change to a grid view column that is mapped to a table column causes this event to fire,
				//		// which results in an immediate update of the database.
				//		IEntity item = items.SingleOrDefault(record => ((MappedRecord)record).Row == e.Row);
				//		db.Context.UpdateOfConcreteType(item);
				//		break;
				//	}

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
				Delete(item);
				RowDeleted.Fire(this, new RowDeletedEventArgs() { Entity = item });
			}
		}

		/// <summary>
		/// ModelView overrides this method.
		/// </summary>
		protected virtual void Insert(T newInstance)
		{
			db.Context.InsertOfConcreteType(newInstance);
		}

		/// <summary>
		/// ModelView overrides this method.
		/// </summary>
		protected virtual void Delete(IEntity item)
		{
			db.Context.DeleteOfConcreteType(item);
		}

		/// <summary>
		/// ModelView overrides this method.
		/// </summary>
		protected virtual void Update(IEntity instance)
		{
			db.Context.UpdateOfConcreteType(instance);
		}

		protected virtual void Table_ColumnChanged(object sender, DataColumnChangeEventArgs e)
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

			// Comboboxes do not fire a DataRowAction.Change RowChanged event when closing the dialog, 
			// these fire only when the use changes the selected row, so we persist the change now if not
			// a detached record (as in, it must exist in the database.)
			if (e.Row.RowState != DataRowState.Detached)
			{
				PropertyInfo pi = instance.GetType().GetProperty(e.Column.ColumnName);
				object oldVal = pi.GetValue(instance);

				// Prevents infinite recursion by updating the model only when the field has changed.
				// Otherwise, programmatically setting a field calls UpdateRowField, which changes the table's field,
				// which fires the ModelTable.Table_ColumnChanged event.  This then calls back here, creating an infinite loop.
				if (((oldVal == null) && (e.ProposedValue != DBNull.Value)) ||
					 ((oldVal != null) && (!oldVal.Equals(e.ProposedValue))))
				{
					modelMgr.UpdateRecordField(instance, e.Column.ColumnName, e.ProposedValue);
					// If it's actually a column in the table, then persist the change.
					ExtDataColumn edc = (ExtDataColumn)e.Column;
					if (edc.IsDbColumn)
					{
						Update(instance);
					}
				}
			}
			else
			{
				// If detached (not a real record in the DB yet) we want to update the model, but wait to persist (insert) the record
				// when the row editing is complete (Table_RowChange action is DataRowAction.Add).
				// Does changing a combobox for a detached row work?
				PropertyInfo pi = instance.GetType().GetProperty(e.Column.ColumnName);
				object oldVal = pi.GetValue(instance);

				// Prevents infinite recursion by updating the model only when the field has changed.
				// Otherwise, programmatically setting a field calls UpdateRowField, which changes the table's field,
				// which fires the ModelTable.Table_ColumnChanged event.  This then calls back here, creating an infinite loop.
				if (((oldVal == null) && (e.ProposedValue != DBNull.Value)) ||
					 ((oldVal != null) && (!oldVal.Equals(e.ProposedValue))))
				{
					modelMgr.UpdateRecordField(instance, e.Column.ColumnName, e.ProposedValue);
				}
			}
		}
	}
}

