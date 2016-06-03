using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;

using Clifton.Core.ExtensionMethods;

namespace Clifton.Core.ModelTableManagement
{
	public class Field
	{
		public string Name { get; set; }
		public string DisplayName { get; set; }
		public Type Type { get; set; }
		public bool ReadOnly { get; set; }
	}

	public class ModelPropertyChangedEventArgs : EventArgs
	{
		public string FieldName { get; set; }
		public object Value { get; set; }
	}

	/// <summary>
	/// Provides services to populate a model collection and to track model collections by their model type.
	/// </summary>
	public class ModelMgr
	{
		/// <summary>
		/// Fires when a model field has been updated with a programmatic call to UpdateRecordField or when the UI changes the associated field in the DataTable.
		/// </summary>
		public event EventHandler<ModelPropertyChangedEventArgs> PropertyChanged;

		protected Dictionary<Type, List<IEntity>> mappedRecords;
		protected DbContextService db;

		public ModelMgr(DbContextService db)
		{
			this.db = db;
			mappedRecords = new Dictionary<Type, List<IEntity>>();
		}

		/// <summary>
		/// Loads all the records for the model type into the DataView and our underlying model collection for that model type.
		/// </summary>
		public void LoadRecords<T>(DataView dv) where T : MappedRecord, IEntity
		{
			Type recType = typeof(T);
			if (!mappedRecords.ContainsKey(recType)) mappedRecords[recType] = new List<IEntity>();
			(from rec in db.Context.GetTable<T>() select rec).ForEach(m => mappedRecords[recType].Add(m));
			mappedRecords[recType].ForEach(m => AddRow(dv, (T)m));		// The cast to (T) is critical here so that the type is T rather than MappedRecord.
		}

		/// <summary>
		/// Returns a collection of type IEntity.  This method is used to initialize a ModelTable instance.
		/// </summary>
		public List<IEntity> GetEntityRecordCollection<T>() where T : MappedRecord, IEntity, new()
		{
			Type recType = typeof(T);

			return mappedRecords[recType];
		}

		/// <summary>
		/// Returns a collection cast to the specified model type, which can by used by the application to get a concrete collection of the model type, as opposed
		/// the the GetEntityRecordCollection, which returns a collection of IEntity.
		/// </summary>
		public List<T> GetRecordCollection<T>() where T : MappedRecord, IEntity, new()
		{
			Type recType = typeof(T);

			return mappedRecords[recType].Cast<T>().ToList();
		}

		/// <summary>
		/// Returns a view by inspecting, via reflection, the model's properties.
		/// Those properties decorated with the DisplayField attribute are added, in order, to the underlying DataTable column collection.
		/// Optionally, properties with the DisplayName attribute set the caption for the column.
		/// </summary>
		public DataView CreateView<T>()
		{
			DataTable dt = new DataTable();
			List<Field> fields = GetFields<T>();

			foreach (Field field in fields)
			{
				DataColumn dc;

				if (field.Type.Name == "Nullable`1")
				{
					dc = new DataColumn(field.Name, field.Type.UnderlyingSystemType.GenericTypeArguments[0]);
				}
				else
				{
					dc = new DataColumn(field.Name, field.Type);
				}

				dc.ReadOnly = field.ReadOnly;
				dc.Caption = field.DisplayName;
				dt.Columns.Add(dc);
			}

			return new DataView(dt);
		}

		/// <summary>
		/// Appends a DataRow from the fields in the model.
		/// </summary>
		public void AddRow<T>(DataView view, T model) where T : MappedRecord
		{
			DataRow row = NewRow(view, model);
			view.Table.Rows.Add(row);
		}

		/// <summary>
		/// Inserts a DataRow from the fields in the model.
		/// </summary>
		public void InsertRow<T>(DataView view, T model) where T : MappedRecord
		{
			DataRow row = NewRow(view, model);
			view.Table.Rows.InsertAt(row, 0);
		}

		/// <summary>
		/// Updates the backing DataRow with changes made to the model fields.
		/// </summary>
		public void UpdateRow<T>(T model) where T : MappedRecord
		{
			DataRow row = model.Row;
			List<Field> fields = GetFields<T>();

			foreach (Field field in fields)
			{
				Type modelType = typeof(T);
				object val = model.GetType().GetProperty(field.Name).GetValue(model);
				UpdateTableRowField(row, field.Name, val);
			}
		}

		// TODO: Get rid of this for programmatic calls, instead the model's property setter should fire the property change event.
		/// <summary>
		/// Called from the ModelTable.Table_ColumnChanged event handler.
		/// Also called when the model field is programmatically changed via the model's call to UpdateTableRowField.
		/// The programmatic implementation is a workaround to get the change event to fire when the model field is changed.  
		/// </summary>
		public void UpdateRecordField(IEntity record, string columnName, object val)
		{
			PropertyInfo pi = record.GetType().GetProperty(columnName);
			object oldVal = pi.GetValue(record);

			// Prevents infinite recursion by updating the model only when the field has changed.
			// Otherwise, programmatically setting a field calls UpdateRowField, which changes the table's field,
			// which fires the ModelTable.Table_ColumnChanged event.  This then calls back here, creating an infinite loop.
			if (oldVal != val)
			{
				pi.SetValue(record, DbNullConverter(val));
			}

			// We always want this event to fire, whether the change was done in the DataGridView or programmatically.
			// TODO: Should the event fire only when the value hasn't changed?
			PropertyChanged.Fire(record, new ModelPropertyChangedEventArgs() { FieldName = columnName, Value = val });
		}

		/// <summary>
		/// Sets the out record to the record found in the specified collection.
		/// </summary>
		public bool TryGetRow<T>(List<T> items, Func<T, bool> predicate, out T record) where T : MappedRecord
		{
			record = null;

			T item = items.SingleOrDefault(predicate);

			if (item != null)
			{
				record = item;
			}

			return record != null;
		}

		/// <summary>
		/// Sets the out record to the record found in the collection of the model type.
		/// </summary>
		public bool TryGetRow<T>(Func<T, bool> predicate, out T record) where T : MappedRecord
		{
			record = null;
			List<IEntity> items = mappedRecords[typeof(T)];

			T item = items.SingleOrDefault(predicate);

			if (item != null)
			{
				record = item;
			}

			return record != null;
		}

		/// <summary>
		/// Returns the record, or null if not found, in the collection of the model type.
		/// </summary>
		public T GetRow<T>(Func<T, bool> predicate) where T : MappedRecord
		{
			T record = null;
			List<IEntity> items = mappedRecords[typeof(T)];

			T item = items.SingleOrDefault(predicate);

			if (item != null)
			{
				record = item;
			}

			return record;
		}

		protected object DbNullConverter(object val)
		{
			return (val == DBNull.Value ? null : val);
		}

		/// <summary>
		/// Read only columns have to be set back to read-writeable in order to update them.
		/// Ironically, this is not the case when adding a row to the grid's view's table.
		/// </summary>
		public void UpdateTableRowField(DataRow row, string fieldName, object val)
		{
			if (row != null)
			{
				bool lastState = row.Table.Columns[fieldName].ReadOnly;
				row.Table.Columns[fieldName].ReadOnly = false;
				row[fieldName] = val ?? DBNull.Value;
				row.Table.Columns[fieldName].ReadOnly = lastState;
			}
		}

		protected DataRow NewRow<T>(DataView view, T model) where T : MappedRecord
		{
			List<Field> fields = GetFields<T>();
			DataRow row = view.Table.NewRow();

			foreach (Field field in fields)
			{
				Type modelType = typeof(T);
				object val = model.GetType().GetProperty(field.Name).GetValue(model);
				row[field.Name] = val ?? DBNull.Value;
			}

			model.Row = row;

			return row;
		}

		// TODO: Get cached field lists for models.
		protected List<Field> GetFields<T>()
		{
			Type modelType = typeof(T);
			var props = from prop in modelType.GetProperties()
						where Attribute.IsDefined(prop, typeof(DisplayFieldAttribute))
						select new Field()
						{
							Name = prop.Name,
							DisplayName = Attribute.IsDefined(prop, typeof(DisplayNameAttribute)) ? ((DisplayNameAttribute)prop.GetCustomAttribute(typeof(DisplayNameAttribute))).DisplayName : prop.Name,
							Type = prop.PropertyType,
							ReadOnly = Attribute.IsDefined(prop, typeof(ReadOnlyAttribute)),
						};

			return props.ToList();
		}
	}
}
