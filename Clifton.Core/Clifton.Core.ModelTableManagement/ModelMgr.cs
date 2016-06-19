using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;

using Clifton.Core.Assertions;
using Clifton.Core.ExtensionMethods;
using Clifton.Core.ServiceInterfaces;

namespace Clifton.Core.ModelTableManagement
{
	public class ExtDataColumn : DataColumn
	{
		public bool Visible { get; set; }
		public bool IsDbColumn { get; set; }

		public ExtDataColumn(string colName, Type colType, bool visible, bool isDbColumn)
			: base(colName, colType)
		{
			Visible = visible;
			IsDbColumn = isDbColumn;
		}
	}

	public class Field
	{
		public string Name { get; set; }
		public string DisplayName { get; set; }
		public Type Type { get; set; }
		public bool ReadOnly { get; set; }
		public bool Visible { get; set; }
		public bool IsColumn { get; set; }
		public bool IsDisplayField { get; set; }

		public bool IsTableField { get { return IsColumn || IsDisplayField; } }
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

		public IDbContextService DbContextService { get { return db; } }

		protected Dictionary<Type, List<IEntity>> mappedRecords;
		protected IDbContextService db;

		public ModelMgr(IDbContextService db)
		{
			this.db = db;
			mappedRecords = new Dictionary<Type, List<IEntity>>();
		}

		public void Register<T>() where T : MappedRecord, IEntity
		{
			Type recType = typeof(T);
			mappedRecords[recType] = new List<IEntity>();
		}

		public void Clear<T>() where T : MappedRecord, IEntity
		{
			Type recType = typeof(T);
			mappedRecords[recType] = new List<IEntity>();
		}

		/// <summary>
		/// Loads all the records for the model type into the DataView and our underlying model collection for that model type.
		/// </summary>
		public List<IEntity> LoadRecords<T>(DataView dv) where T : MappedRecord, IEntity
		{
			Clear<T>();
			Type recType = typeof(T);
			(from rec in db.Context.GetTable<T>() select rec).ForEach(m => AddRow(dv, (T)m));			// The cast to (T) is critical here so that the type is T rather than MappedRecord.

			return mappedRecords[recType];
		}

		/// <summary>
		/// Adds all the records for the model type into the DataView and our underlying EXISTING model collection for that model type.
		/// </summary>
		public void AddRecords<T>(DataView dv) where T : MappedRecord, IEntity
		{
			Assert.That(mappedRecords.ContainsKey(typeof(T)), "Model Manager does not know about " + typeof(T).Name + ".\r\nCreate an instance of ModuleMgr with this record collection.");

			Type recType = typeof(T);
			mappedRecords[recType].ForEach(m =>
				{
					DataRow row = NewRow(dv, (T)m);		// The cast to (T) is critical here so that the type is T rather than MappedRecord.
					dv.Table.Rows.Add(row);
				});
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

			foreach (Field field in fields.Where(f=>f.IsTableField))
			{
				// Only create columns in the underlying data table for those properties in the model that have Column or DisplayField attributes.
				// Otherwise, we start tracking things like EntityRef properties, etc, that we don't want to track!
				ExtDataColumn dc;

				// Handle nullable types by creating the column type as the underlying, non-nullable, type.
				if (field.Type.Name == "Nullable`1")
				{
					dc = new ExtDataColumn(field.Name, field.Type.UnderlyingSystemType.GenericTypeArguments[0], field.Visible, field.IsColumn);
				}
				else
				{
					dc = new ExtDataColumn(field.Name, field.Type, field.Visible, field.IsColumn);
				}

				dc.ReadOnly = field.ReadOnly;
				dc.Caption = field.DisplayName;
				dt.Columns.Add(dc);
			}

			return new DataView(dt);
		}

		/// <summary>
		/// Appends a DataRow from the fields in the model and adds the row to the underlying model collection.
		/// </summary>
		public void AddRow<T>(DataView view, T model) where T : MappedRecord
		{
			DataRow row = NewRow(view, model);
			view.Table.Rows.Add(row);
			AddRecordToCollection(model);
		}

		/// <summary>
		/// Inserts a DataRow from the fields in the model and adds the row to the underlying model collection.
		/// </summary>
		public void InsertRow<T>(DataView view, T model) where T : MappedRecord
		{
			DataRow row = NewRow(view, model);
			view.Table.Rows.InsertAt(row, 0);
			AddRecordToCollection(model);
		}

		/// <summary>
		/// Updates the backing DataRow with changes made to the model fields.
		/// </summary>
		public void UpdateRow<T>(T model) where T : MappedRecord
		{
			DataRow row = model.Row;
			List<Field> fields = GetFields<T>();

			foreach (Field field in fields.Where(f=>f.IsTableField))
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
			if (((oldVal == null) && (val != DBNull.Value)) ||
				 ((oldVal != null) && (!oldVal.Equals(val))))
			{
				pi.SetValue(record, DbNullConverter(val));

				// We always want this event to fire, whether the change was done in the DataGridView or programmatically.
				// TODO: Should the event fire only when the value hasn't changed?
				// Apparently so, otherwise we can get continuous calls to UpdateRecordField by the app.
				PropertyChanged.Fire(record, new ModelPropertyChangedEventArgs() { FieldName = columnName, Value = val });
			}
		}

		/// <summary>
		/// Sets the out record to the record found in the specified collection.
		/// </summary>
		public bool TryGetRow<T>(List<T> items, Func<T, bool> predicate, out T record) where T : MappedRecord
		{
			Assert.That(mappedRecords.ContainsKey(typeof(T)), "Model Manager does not know about " + typeof(T).Name + ".\r\nCreate an instance of ModuleMgr with this record collection.");

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
			Assert.That(mappedRecords.ContainsKey(typeof(T)), "Model Manager does not know about " + typeof(T).Name + ".\r\nCreate an instance of ModuleMgr with this record collection.");

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
			Assert.That(mappedRecords.ContainsKey(typeof(T)), "Model Manager does not know about " + typeof(T).Name + ".\r\nCreate an instance of ModuleMgr with this record collection.");

			T record = null;
			List<IEntity> items = mappedRecords[typeof(T)];

			T item = items.SingleOrDefault(predicate);

			if (item != null)
			{
				record = item;
			}

			return record;
		}

		public List<T> GetRows<T>(Func<T, bool> predicate) where T : MappedRecord
		{
			Assert.That(mappedRecords.ContainsKey(typeof(T)), "Model Manager does not know about " + typeof(T).Name + ".\r\nCreate an instance of ModuleMgr with this record collection.");

			List<T> records = mappedRecords[typeof(T)].Cast<T>().Where(predicate).ToList();

			return records;
		}

		/// <summary>
		/// Returns the record associated with the specified DataRow.
		/// </summary>
		public T GetRow<T>(DataRow row) where T : MappedRecord
		{
			Assert.That(mappedRecords.ContainsKey(typeof(T)), "Model Manager does not know about " + typeof(T).Name + ".\r\nCreate an instance of ModuleMgr with this record collection.");

			T record = mappedRecords[typeof(T)].Cast<T>().Where(r => r.Row == row).Single();

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

		protected void AddRecordToCollection(MappedRecord record)
		{
			Assert.That(mappedRecords.ContainsKey(record.GetType()), "Model Manager does not know about " + record.GetType().Name + ".\r\nCreate an instance of ModuleMgr with this record collection.");

			mappedRecords[record.GetType()].Add((IEntity)record);
		}

		protected DataRow NewRow<T>(DataView view, T model) where T : MappedRecord
		{
			List<Field> fields = GetFields<T>();
			DataRow row = view.Table.NewRow();

			foreach (Field field in fields.Where(f=>f.IsTableField))
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
						// where Attribute.IsDefined(prop, typeof(DisplayFieldAttribute))
						select new Field()
						{
							Name = prop.Name,
							DisplayName = Attribute.IsDefined(prop, typeof(DisplayNameAttribute)) ? ((DisplayNameAttribute)prop.GetCustomAttribute(typeof(DisplayNameAttribute))).DisplayName : prop.Name,
							Type = prop.PropertyType,
							ReadOnly = Attribute.IsDefined(prop, typeof(ReadOnlyAttribute)),
							Visible = Attribute.IsDefined(prop, typeof(DisplayFieldAttribute)),
							IsColumn = Attribute.IsDefined(prop, typeof(ColumnAttribute)),
							IsDisplayField = Attribute.IsDefined(prop, typeof(DisplayFieldAttribute)),
						};

			return props.ToList();
		}
	}
}
