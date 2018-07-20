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
using System.Data.Linq;
using System.Linq;
using System.Reflection;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.ServiceInterfaces;

namespace Clifton.Core.ModelTableManagement
{
	public class RowDeletedEventArgs : EventArgs
	{
		public IEntity Entity { get; set; }

        /// <summary>
        /// Set to true if the RowDeleted event handler handles "deleting" the row.
        /// If true, the row is not actually deleted from the DB.
        /// </summary>
        public bool Handled { get; set; }
	}

    public class RowAddingEventArgs : EventArgs
    {
        public IEntity Entity { get; set; }
    }

    public class RowChangedEventArgs : EventArgs
    {
        public IEntity Entity { get; set; }
        public string ColumnName { get; set; }

        /// <summary>
        /// Set to true if the RowDeleted event handler handles "deleting" the row.
        /// If true, the row is not actually deleted from the DB.
        /// </summary>
        public bool Handled { get; set; }
    }

    public class RowChangeFinalizedEventArgs : EventArgs
    {
        public IEntity Entity { get; set; }
        public string ColumnName { get; set; }
    }

    public class RowAddFinalizedEventArgs : EventArgs
    {
        public IEntity Entity { get; set; }
    }

    public class RowDeleteFinalizedEventArgs : EventArgs
    {
        public IEntity Entity { get; set; }
    }

    public class ColumnChangingEventArgs : EventArgs
    {
        public IEntity Entity { get; set; }
        public string ColumnName { get; set; }
        public object ProposedValue { get; set; }
    }

    public class RowChangingEventArgs : EventArgs
    {
        public IEntity Entity { get; set; }
        public string ColumnName { get; set; }

        /// <summary>
        /// Set to true if the RowDeleted event handler handles "deleting" the row.
        /// If true, the row is not actually deleted from the DB.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// If true, update the NewInstance.  This is used when archiving an existing instance
        /// but we want the new instance to be updated.
        /// </summary>
        public bool ReplaceInstance { get; set; }
        public IEntity NewInstance { get; set; }
    }

    public interface IModelTable
	{
		void BeginProgrammaticUpdate();
		void EndProgrammaticUpdate();
        void Replace(IEntity oldEntity, IEntity withEntity);
	}

	/// <summary>
	/// Wires up table change events so that the underlying model collection and individual model instances can be kept in sync.
	/// </summary>
	public class ModelTable<T> : IModelTable, IDisposable  where T : MappedRecord, IEntity, new()
	{
        public DataTable Table { get; }

        public const string PK_FIELD = "Id";
        public event EventHandler<ColumnChangingEventArgs> ColumnChanging;   // Fires from DataTable.ColumnChanging event, contrast with RowChanged/RowChanging.
        public event EventHandler<RowDeletedEventArgs> RowDeleted;
        public event EventHandler<RowAddingEventArgs> RowAdding;
        public event EventHandler<RowChangedEventArgs> RowChanged;
        public event EventHandler<RowChangingEventArgs> RowChanging;
        public event EventHandler<RowChangeFinalizedEventArgs> RowChangeFinalized;
        public event EventHandler<RowAddFinalizedEventArgs> RowAddFinalized;
        public event EventHandler<RowDeleteFinalizedEventArgs> RowDeleteFinalized;
        protected DataTable dt;
		protected T newInstance;
		protected List<IEntity> items;
		protected ModelMgr modelMgr;
		protected DataContext context;
		protected bool programmaticUpdate;

		public ModelTable(ModelMgr modelMgr, DataContext context, DataTable backingTable, List<IEntity> modelCollection)
		{
			this.modelMgr = modelMgr;
			this.context = context;
			dt = backingTable;
			items = modelCollection;
			WireUpEvents(dt);
			RegisterWithModelManager();
		}

		public void Dispose()
		{
            dt.ColumnChanging -= Table_ColumnChanging;
			dt.ColumnChanged -= Table_ColumnChanged;
			dt.RowDeleted -= Table_RowDeleted;
			dt.TableNewRow -= Table_TableNewRow;
			dt.RowChanged -= Table_RowChanged;
			UnregisterWithModelManager();
		}

        public void Replace(IEntity oldEntity, IEntity newEntity)
        {
            int idx = items.IndexOf(record => ((MappedRecord)record).Row == ((MappedRecord)oldEntity).Row);

            if (idx != -1)
            {
                items[idx] = newEntity;
            }
        }

		public void ResetItems(List<IEntity> modelCollection)
		{
			items = modelCollection;
		}

		/// <summary>
		/// Ignore table change events, which effectively means that the model manager is not notified of changes.
		/// </summary>
		public void BeginProgrammaticUpdate()
		{
			programmaticUpdate = true;
		}

		/// <summary>
		/// Re-enable notifying the model manager of changes made by the user interacting through the UI, and
		/// other changes being made to the DataTable.
		/// </summary>
		public void EndProgrammaticUpdate()
		{
			programmaticUpdate = false;
		}

		protected void WireUpEvents(DataTable dt)
		{
            dt.ColumnChanging += Table_ColumnChanging;
            dt.ColumnChanged += Table_ColumnChanged;
			dt.RowDeleted += Table_RowDeleted;
			dt.TableNewRow += Table_TableNewRow;
			dt.RowChanged += Table_RowChanged;
		}

		protected void RegisterWithModelManager()
		{
			modelMgr.Register<T>(this);
		}

		protected void UnregisterWithModelManager()
		{
			modelMgr.Unregister<T>(this);
		}

		protected void Table_RowChanged(object sender, DataRowChangeEventArgs e)
		{
			if (!programmaticUpdate)
			{
				switch (e.Action)
				{
					case DataRowAction.Add:
                        RowAdding.Fire(this, new RowAddingEventArgs() { Entity = newInstance });
                        items.Add(newInstance);
						Insert(newInstance);

						// After an insert, we need to set the the ID in the DataView, otherwise combobox controls in the grid whose
						// field is this ID won't find the combobox record.
						programmaticUpdate = true;
						e.Row[PK_FIELD] = newInstance.Id;
						programmaticUpdate = false;
                        RowAddFinalized.Fire(this, new RowAddFinalizedEventArgs() { Entity = newInstance });

                        break;

					// We don't do this here because the Table_ColumnChanged event handles persisting the change.
					//case DataRowAction.Change:
					//	{
					//		// Any change to a grid view column that is mapped to a table column causes this event to fire,
					//		// which results in an immediate update of the database.
					//		IEntity item = items.SingleOrDefault(record => ((MappedRecord)record).Row == e.Row);
					//		context.UpdateOfConcreteType(item);
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
		}

		protected void Table_TableNewRow(object sender, DataTableNewRowEventArgs e)
		{
			if (!programmaticUpdate)
			{
				newInstance = new T();
				newInstance.Row = e.Row;
			}
		}

        protected void Table_RowDeleted(object sender, DataRowChangeEventArgs e)
        {
            if (!programmaticUpdate)
            {
                IEntity item = items.SingleOrDefault(record => ((MappedRecord)record).Row == e.Row);

                if (item != null)
                {
                    RowDeletedEventArgs args = new RowDeletedEventArgs() { Entity = item };
                    RowDeleted.Fire(this, args);

                    if (!args.Handled)
                    {
                        items.Remove(item);
                        Delete(item);
                        RowDeleteFinalized.Fire(this, new RowDeleteFinalizedEventArgs() { Entity = item });
                    }
                }
            }
        }

		/// <summary>
		/// ModelView overrides this method.
		/// </summary>
		protected virtual void Insert(T newInstance)
		{
			context.InsertOfConcreteType(newInstance);
		}

		/// <summary>
		/// ModelView overrides this method.
		/// </summary>
		protected virtual void Delete(IEntity item)
		{
			context.DeleteOfConcreteType(item);
		}

		/// <summary>
		/// ModelView overrides this method.
		/// </summary>
		protected virtual void Update(IEntity instance)
		{
			context.UpdateOfConcreteType(instance);
		}

        protected virtual void Table_ColumnChanging(object sender, DataColumnChangeEventArgs e)
        {
            if (!programmaticUpdate)
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

                ColumnChangingEventArgs rowChangingArgs = new ColumnChangingEventArgs()
                {
                    Entity = instance,
                    ColumnName = e.Column.ColumnName,
                    ProposedValue = e.ProposedValue
                };

                ColumnChanging.Fire(this, rowChangingArgs);
            }
        }

        protected virtual void Table_ColumnChanged(object sender, DataColumnChangeEventArgs e)
		{
            // Debugging
            //if (e.Row.Table.TableName == "EmsStationType")
            //{
            //}
			if (!programmaticUpdate)
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
                    // Use the column name that has changed, not the potential mapped column name.
					PropertyInfo pi = instance.GetType().GetProperty(e.Column.ColumnName);
					object oldVal = pi.GetValue(instance);

					// Prevents infinite recursion by updating the model only when the field has changed.
					// Otherwise, programmatically setting a field calls UpdateRowField, which changes the table's field,
					// which fires the ModelTable.Table_ColumnChanged event.  This then calls back here, creating an infinite loop.
					if (((oldVal == null) && (e.ProposedValue != DBNull.Value)) ||
						 ((oldVal != null) && (!oldVal.Equals(e.ProposedValue))))
					{
                        // Always update the actual column, as the setter, for a mapped column, will probably update the 
                        // value for the mapped column.
                        RowChangingEventArgs rowChangingArgs = new RowChangingEventArgs() { Entity = instance, ColumnName = e.Column.ColumnName };
                        RowChanging.Fire(this, rowChangingArgs);

                        if (!rowChangingArgs.Handled)
                        {
                            if (rowChangingArgs.ReplaceInstance)
                            {
                                instance = rowChangingArgs.NewInstance;
                            }

                            modelMgr.UpdateRecordField(instance, e.Column.ColumnName, e.ProposedValue);
                        }

                        // If mapped, then update again, using the mapped column name.
                        //if (((ExtDataColumn)e.Column).MappedColumn != null)
                        //{
                        //    PropertyInfo piMapped = instance.GetType().GetProperty(((ExtDataColumn)e.Column).MappedColumn);
                        //    object mappedColumnValue = piMapped.GetValue(instance);
                        //    modelMgr.UpdateRecordField(instance, ((ExtDataColumn)e.Column).MappedColumn, mappedColumnValue);
                        //}

						// If it's actually a column in the table, then persist the change.
						ExtDataColumn edc = (ExtDataColumn)e.Column;

						if (edc.IsDbColumn || edc.MappedColumn != null)
						{
                            RowChangedEventArgs rowChangedArgs = new RowChangedEventArgs() { Entity = instance, ColumnName = e.Column.ColumnName };
                            RowChanged.Fire(this, rowChangedArgs);

                            if (!rowChangedArgs.Handled)
                            {
                                Update(instance);
                                RowChangeFinalized.Fire(this, new RowChangeFinalizedEventArgs() { Entity = instance, ColumnName = e.Column.ColumnName });
                            }
                        }
					}
				}
				else
				{
					// TODO: CAN PROBABLY BE REMOVED NOW THAT WE HAVE THE MODEL MANAGER SETTING THE PROGRAMMATIC FLAG.

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
}

