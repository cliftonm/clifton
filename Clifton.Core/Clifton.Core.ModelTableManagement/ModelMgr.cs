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
using System.ComponentModel;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Clifton.Core.Assertions;
using Clifton.Core.ExtensionMethods;

namespace Clifton.Core.ModelTableManagement
{
    public class ExtDataColumn : DataColumn
    {
        public bool Visible { get; set; }
        public bool IsDbColumn { get; set; }
        public LookupAttribute Lookup { get; set; }
        public string MappedColumn { get; set; }
        public string Format { get; set; }
        public string ActualType { get; set; }
        public int FieldMaxLength { get; set; }

        public string ActualColumnName { get { return MappedColumn ?? ColumnName; } }

        public ExtDataColumn(string colName, Type colType, bool visible, bool isDbColumn, string format, string actualType, int maxLength, LookupAttribute lookup = null)
            : base(colName, colType)
        {
            Visible = visible;
            IsDbColumn = isDbColumn;
            Lookup = lookup;
            Format = format;
            ActualType = actualType;
            FieldMaxLength = maxLength;
        }
    }

    public class Field
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }

        /// <summary>
        /// Mapped column is used when we have a property that is not a column, but directly maps to an actual column.
        /// Useful when needing to convert the field's data type to a different internal data type.
        /// </summary>
        public string MappedColumn { get; set; }

        public Type Type { get; set; }
        public string ActualType { get; set; }
        public bool ReadOnly { get; set; }
        public bool Visible { get; set; }
        public bool IsColumn { get; set; }
        public bool IsDisplayField { get; set; }
        public string Format { get; set; }
        public LookupAttribute Lookup { get; set; }
        public int MaxLength { get; set; }

        public bool IsTableField { get { return IsColumn || IsDisplayField; } }
    }

    public class ModelPropertyChangedEventArgs : EventArgs
    {
        public string FieldName { get; set; }
        public object Value { get; set; }
        public object OldValue { get; set; }
    }

    /*
    public class TypeKey : IEquatable<TypeKey>
    {
        public Type Type { get; set; }
        public int Key { get; set; }

        public override int GetHashCode()
        {
            return Type.GetHashCode() ^ Key.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TypeKey);
        }

        public bool Equals(TypeKey obj)
        {
            return obj != null && obj.Type == Type && obj.Key == Key;
        }
    }
    */

    public class ModelManagerDataSet
    {
        public DataSet DataSet { get { return dataset; } }

        protected ModelMgr modelMgr;
        protected DataSet dataset;
        protected List<Type> entityTypes;
        protected Dictionary<Type, DataTable> typeTableMap;

        public ModelManagerDataSet(ModelMgr modelMgr)
        {
            this.modelMgr = modelMgr;
            dataset = new DataSet();
            entityTypes = new List<Type>();
            typeTableMap = new Dictionary<Type, DataTable>();
        }

        public ModelManagerDataSet WithTable<T>(Expression<Func<T, bool>> whereClause = null) where T : MappedRecord, IEntity
        {
            var dt = modelMgr.CreateViewAndLoadRecords(whereClause).Table;
            dataset.Tables.Add(dt);
            Type t = typeof(T);
            entityTypes.Add(t);
            typeTableMap[t] = dt;

            return this;
        }

        public ModelManagerDataSet BuildAssociations()
        {
            // In the order tables were added:
            for (int i = 0; i < dataset.Tables.Count; i++)
            {
                DataTable dt = dataset.Tables[i];

                // What are our FK's?
                Type t = typeTableMap.Single(kvp => kvp.Value == dt).Key;

                var fks = t.GetProperties().
                Where(p => Attribute.IsDefined(p, typeof(ForeignKeyAttribute))).
                Select(p => new
                {
                    Property = p,
                    FKAttr = ((ForeignKeyAttribute)p.GetCustomAttribute(typeof(ForeignKeyAttribute))),
                });

                // The for each FK...
                foreach (var prop in fks)
                {
                    // If the FK references a table in our dataset...
                    if (typeTableMap.Keys.TryGetSingle(k => k.Name == prop.FKAttr.ForeignKeyTable, out Type fkType))
                    {
                        int idx = typeTableMap.IndexOf(kvp => kvp.Key == fkType);

                        // If the index references a table further down in the list, then this is a child table
                        // Forward references must have unique parent ID's, these should always be 1:1 relationships and should be handled with a join
                        // to populate the forward referencing values.  Example: ProductSale references Product which is a 1:1 relationship.
                        if (idx > i)
                        {
                            //DataTable parent = dt;
                            //string parentCol = prop.Property.Name;
                            //DataTable child = typeTableMap[fkType];
                            //string childCol = prop.FKAttr.ForeignKeyColumn;
                            //dataset.Relations.Add(parent.TableName + "-" + child.TableName, parent.Columns[parentCol], child.Columns[childCol]);
                            //Console.WriteLine("Relationship: parent {0}.{1} with child {2}.{3}", parent.TableName, parentCol, child.TableName, childCol);
                        }
                        else if (idx < i)
                        {
                            // Otherwise, if it references a table earlier in the list, then this table is a "parent" table (reverse relationship)
                            DataTable parent = typeTableMap[fkType];
                            string parentCol = prop.FKAttr.ForeignKeyColumn;
                            DataTable child = dt;
                            string childCol = prop.Property.Name;
                            dataset.Relations.Add(parent.TableName + "-" + child.TableName, parent.Columns[parentCol], child.Columns[childCol]);
                            Console.WriteLine("Relationship: parent {0}.{1} with child {2}.{3}", parent.TableName, parentCol, child.TableName, childCol);
                        }
                    }
                }
            }

            return this;
        }

        /*
        public ModelManagerDataSet BuildAssociations()
        {
            foreach (KeyValuePair<Type, DataTable> kvp in typeTableMap)
            {
                var fks = kvp.Key.GetProperties().
                    Where(p => Attribute.IsDefined(p, typeof(ForeignKeyAttribute))).
                    Select(p => new
                    {
                        Property = p,
                        FKAttr = ((ForeignKeyAttribute)p.GetCustomAttribute(typeof(ForeignKeyAttribute))),
                    });

                foreach (var prop in fks)
                {
                    Type fkType;

                    if (typeTableMap.Keys.TryGetSingle(k => k.Name == prop.FKAttr.ForeignKeyTable, out fkType))
                    {
                        DataTable parent = typeTableMap[fkType];
                        string parentCol = prop.FKAttr.ForeignKeyColumn;
                        DataTable child = kvp.Value;
                        string childCol = prop.Property.Name;
                        dataset.Relations.Add(parent.TableName + "-" + child.TableName, parent.Columns[parentCol], child.Columns[childCol]);
                        Console.WriteLine("Relationship: parent {0}.{1} with child {2}.{3}", parent.TableName, parentCol, child.TableName, childCol);
                    }
                }
            }

            return this;
        }
        */
    }

    public static class ModelMgrExtensionMethods
    {
        public static ModelManagerDataSet CreateDataSet(this ModelMgr mmgr)
        {
            var mmds = new ModelManagerDataSet(new ModelMgr(mmgr.DataContext));

            return mmds;
        }
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

        public DataContext DataContext { get { return context; } }

		protected Dictionary<Type, List<IEntity>> mappedRecords;
		protected Dictionary<Type, List<IModelTable>> modelTables;
		protected Dictionary<Type, DataView> modelViewMap;
		protected DataContext context;

		public ModelMgr(DataContext context)
		{
			this.context = context;
			mappedRecords = new Dictionary<Type, List<IEntity>>();
			modelTables = new Dictionary<Type, List<IModelTable>>();
			modelViewMap = new Dictionary<Type, DataView>();
		}

        public void DisposeOfAllTables()
        {
            modelTables.ForEach(kvp => kvp.Value.Cast<IDisposable>().ForEach(mt => mt.Dispose()));
        }

        public void RemoveView<T>()
        {
            Type t = typeof(T);
            mappedRecords.Remove(t);
            modelTables.Remove(t);
            modelViewMap.Remove(t);
        }

        public void Replace(MappedRecord oldEntity, MappedRecord withEntity)
        {
            var oldEntityType = oldEntity.GetType();
            var mappedEntities = mappedRecords[oldEntityType];
            int idx = mappedEntities.Cast<MappedRecord>().IndexOf(e => e.Row == oldEntity.Row);

            if (idx != -1)
            {
                mappedEntities[idx] = (IEntity)withEntity;
            }

            var tables = modelTables[oldEntityType];

            tables.ForEach(mt => mt.Replace((IEntity)oldEntity, (IEntity)withEntity));
        }

		public void Register<T>() where T : MappedRecord, IEntity
		{
			Type recType = typeof(T);
			Register(recType);
		}

		public void Clear<T>() where T : MappedRecord, IEntity
		{
			Type recType = typeof(T);
			Clear(recType);
		}

		public void Clear(Type recType)
		{
			mappedRecords[recType] = new List<IEntity>();
		}

        public DataView CreateViewAndLoadRecords<T>(Expression<Func<T, bool>> whereClause = null) where T : MappedRecord, IEntity
        {
            DataView dv = CreateView<T>();
            LoadRecords(dv, whereClause);

            return dv;
        }

        /// <summary>
        /// Loads all the records for the model type into the DataView and our underlying model collection for that model type.
        /// </summary>
        public List<IEntity> LoadRecords<T>(DataView dv, Expression<Func<T, bool>> whereClause = null) where T : MappedRecord, IEntity
		{
			Clear<T>();
			Type recType = typeof(T);
            SqlConnection connection;
            DataContext newContext;
            ContextExtensionMethods.CreateNewContext(context, out connection, out newContext);
            newContext.Query(whereClause).ForEach(m => AppendRow(dv, m));

			return mappedRecords[recType];
		}

        public List<IEntity> LoadRecords(Type recType, DataView dv)
		{
			Clear(recType);
            // We create a new context because the existing context caches the previously queried model.
            SqlConnection connection;
            DataContext newContext;
            ContextExtensionMethods.CreateNewContext(context, out connection, out newContext);
            var records = newContext.GetTable(recType.Name);
            records.ForEach(m => AppendDecoupledRow(dv, recType, m));
            // newContext.Dispose();

			return records.Cast<IEntity>().ToList();
		}

        public List<IEntity> LoadRecords(Type recType, DataView dv, Func<MappedRecord, bool> where)
        {
            Clear(recType);
            // We create a new context because the existing context caches the previously queried model.
            SqlConnection connection;
            DataContext newContext;
            ContextExtensionMethods.CreateNewContext(context, out connection, out newContext);
            var records = newContext.GetTable(recType.Name).Where(where);
            records.ForEach(m => AppendDecoupledRow(dv, recType, m));
            // newContext.Dispose();

            return records.Cast<IEntity>().ToList();
        }

        public List<IEntity> LoadRecords(Type recType, DataView dv, out List<IEntity> records)
        {
            Clear(recType);
            // We create a new context because the existing context caches the previously queried model.
            SqlConnection connection;
            DataContext newContext;
            ContextExtensionMethods.CreateNewContext(context, out connection, out newContext);
            var mappedRecords = newContext.GetTable(recType.Name);
            records = mappedRecords.Cast<IEntity>().ToList();
            mappedRecords.ForEach(m => AppendDecoupledRow(dv, recType, m));
            // newContext.Dispose();

            return mappedRecords.Cast<IEntity>().ToList();
        }

        public List<IEntity> LoadRecords(Type recType, DataView dv, Func<MappedRecord, bool> where, out List<IEntity> records)
        {
            Clear(recType);
            // We create a new context because the existing context caches the previously queried model.
            SqlConnection connection;
            DataContext newContext;
            ContextExtensionMethods.CreateNewContext(context, out connection, out newContext);
            var mappedRecords = newContext.GetTable(recType.Name).Where(where);
            records = mappedRecords.Cast<IEntity>().ToList();
            mappedRecords.ForEach(m => AppendDecoupledRow(dv, recType, m));
            // newContext.Dispose();

            return mappedRecords.Cast<IEntity>().ToList();
        }

        /// <summary>
        /// Simply creates a view and loads it with the records, decoupled from the model manager.
        /// We provide this method so that a lookup view can be loaded without referencing an existing view used in a different grid.
        /// This is particularly necessary when changing records in one grid that are archived, and we need to force reloading the
        /// lookup view in another grid so it gets the changes without causing a reload of the view on the grid for the record being changed,
        /// which will disrupt the mapping.
        /// </summary>
        /// <param name="recType"></param>
        /// <returns></returns>
        public DataView LoadDecoupledView(Type recType)
        {
            DataView dv = CreateDecoupledView(recType);
            SqlConnection connection;
            DataContext newContext;
            ContextExtensionMethods.CreateNewContext(context, out connection, out newContext);
            newContext.GetTable(recType.Name).ForEach(m => AppendDecoupledRow(dv, recType, m));

            return dv;
        }

        public DataView LoadDecoupledView(Type recType, Func<MappedRecord, bool> where)
        {
            DataView dv = CreateDecoupledView(recType);
            SqlConnection connection;
            DataContext newContext;
            ContextExtensionMethods.CreateNewContext(context, out connection, out newContext);
            newContext.GetTable(recType.Name).Where(where).ForEach(m => AppendDecoupledRow(dv, recType, m));

            return dv;
        }

        public DataView LoadDecoupledView(Type recType, out List<IEntity> records)
        {
            DataView dv = CreateDecoupledView(recType);
            SqlConnection connection;
            DataContext newContext;
            ContextExtensionMethods.CreateNewContext(context, out connection, out newContext);
            var mappedRecords = newContext.GetTable(recType.Name);
            records = mappedRecords.Cast<IEntity>().ToList();
            mappedRecords.ForEach(m => AppendDecoupledRow(dv, recType, m));

            return dv;
        }

        public DataView LoadDecoupledView(Type recType, Func<MappedRecord, bool> where, out List<IEntity> records)
        {
            DataView dv = CreateDecoupledView(recType);
            SqlConnection connection;
            DataContext newContext;
            ContextExtensionMethods.CreateNewContext(context, out connection, out newContext);
            var mappedRecords = newContext.GetTable(recType.Name).Where(where);
            records = mappedRecords.Cast<IEntity>().ToList();
            mappedRecords.ForEach(m => AppendDecoupledRow(dv, recType, m));

            return dv;
        }

        /// <summary>
        /// Reloads records into the existing table.
        /// </summary>
        public List<IEntity> ReloadRecords<T>(DataView dv, Expression<Func<T, bool>> whereClause = null) where T : MappedRecord, IEntity
        {
            ClearView<T>();
            Type recType = typeof(T);
            context.Query(whereClause).ForEach(m => AppendRow(dv, m));

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
					DataRow row = NewRow(dv, typeof(T), (MappedRecord)m);
					dv.Table.Rows.Add(row);
				});
		}

        public void DeleteAllRecords<T>(DataView dv) where T : MappedRecord, IEntity
		{
			Assert.That(mappedRecords.ContainsKey(typeof(T)), "Model Manager does not know about " + typeof(T).Name + ".\r\nCreate an instance of ModuleMgr with this record collection.");
			List<DataRow> rows = new List<DataRow>();
			
			// Put into a separate list so we're not modifying the collection as we delete rows.
			foreach (DataRow row in dv.Table.Rows)
			{
				rows.Add(row);
			}

			foreach (DataRow row in rows)
			{
				dv.Table.Rows.Remove(row);
			}

			Clear<T>();
		}

		/// <summary>
		/// Returns a collection of type IEntity.  This method is used to initialize a ModelTable instance.
		/// </summary>
		public List<IEntity> GetEntityRecordCollection<T>() where T : MappedRecord, IEntity, new()
		{
			Type recType = typeof(T);

			return mappedRecords[recType];
		}

        public List<IEntity> GetEntityRecordCollection(Type recType)
        {
            return mappedRecords[recType];
        }

        /// <summary>
        /// Returns a collection of T.
        /// </summary>
        public List<T> GetRecords<T>() where T : MappedRecord, IEntity, new()
		{
			Type recType = typeof(T);

			return mappedRecords[recType].Cast<T>().ToList();
		}

		/*
		/// <summary>
		/// Returns a collection cast to the specified model type, which can by used by the application to get a concrete collection of the model type, as opposed
		/// the the GetEntityRecordCollection, which returns a collection of IEntity.
		/// </summary>
		public List<T> GetRecordCollection<T>() where T : MappedRecord, IEntity, new()
		{
			Type recType = typeof(T);

			return mappedRecords[recType].Cast<T>().ToList();
		}
		*/

		public bool TryGetView<T>(out DataView dv)
		{
			return modelViewMap.TryGetValue(typeof(T), out dv);
		}

		public bool TryGetView(Type t, out DataView dv)
		{
			return modelViewMap.TryGetValue(t, out dv);
		}

		/// <summary>
		/// Returns a view by inspecting, via reflection, the model's properties.
		/// Those properties decorated with the DisplayField attribute are added, in order, to the underlying DataTable column collection.
		/// Optionally, properties with the DisplayName attribute set the caption for the column.
		/// </summary>
		public DataView CreateView<T>() where T : MappedRecord, IEntity
		{
			return CreateView(typeof(T));
		}

        public DataView CreateView(Type t)
		{
			Register(t);
			DataTable dt = new DataTable();
			dt.TableName = t.Name;
			List<Field> fields = GetFields(t);
			CreateColumns(dt, fields);
			DataView dv = new DataView(dt);
			modelViewMap[t] = dv;

			return dv;
		}

        public void ClearView<T>() where T : MappedRecord, IEntity
        {
            ClearView(typeof(T));
        }

        public void ClearView(Type t)
        {
            modelTables[t].ForEach(mt => mt.BeginProgrammaticUpdate());
            modelViewMap[t].Table.Rows.Clear();
            mappedRecords[t].Clear();
            modelTables[t].ForEach(mt => mt.EndProgrammaticUpdate());
        }

        /// <summary>
        /// Appends a DataRow from the fields in the model and adds the row to the underlying model collection.
        /// </summary>
        public void AppendRow<T>(DataView view, T model) where T : MappedRecord
		{
			Type recType = model.GetType();
			AppendRow(view, recType, model);
		}

		/// <summary>
		/// Inserts a DataRow from the fields in the model and adds the row to the underlying model collection.
		/// </summary>
		public void InsertRow<T>(DataView view, T model) where T : MappedRecord
		{
            // Have to use model.GetType(), as typeof(T) is MappedRecord.
            Assert.That(modelTables.ContainsKey(model.GetType()), "Model Manager does not know about " + model.GetType().Name + ".\r\nCreate an instance of ModuleMgr with this record collection.");
            modelTables[model.GetType()].ForEach(mt => mt.BeginProgrammaticUpdate());
			DataRow row = NewRow(view, model.GetType(), model);
			view.Table.Rows.InsertAt(row, 0);
			modelTables[model.GetType()].ForEach(mt => mt.EndProgrammaticUpdate());
			AddRecordToCollection(model);
		}

        public void DeleteRecord<T>(DataView dv, T model) where T : MappedRecord, IEntity
        {
            // Have to use model.GetType(), as typeof(T) is MappedRecord.
            Assert.That(modelTables.ContainsKey(model.GetType()), "Model Manager does not know about " + model.GetType().Name + ".\r\nCreate an instance of ModuleMgr with this record collection.");
            modelTables[model.GetType()].ForEach(mt => mt.BeginProgrammaticUpdate());
            Type recType = typeof(T);
            dv.Table.Rows.Remove(model.Row);
            mappedRecords[recType].Remove(mappedRecords[recType].Single(mr => mr == model));
            modelTables[model.GetType()].ForEach(mt => mt.EndProgrammaticUpdate());
        }

        /// <summary>
        /// Updates the backing DataRow with changes made to the model fields.
        /// </summary>
        public void UpdateRow<T>(T model) where T : MappedRecord
		{
            // Have to use model.GetType(), as typeof(T) is MappedRecord.
            Assert.That(modelTables.ContainsKey(model.GetType()), "Model Manager does not know about " + model.GetType().Name + ".\r\nCreate an instance of ModuleMgr with this record collection.");
            modelTables[model.GetType()].ForEach(mt => mt.BeginProgrammaticUpdate());
            DataRow row = model.Row;
			List<Field> fields = GetFields<T>();

			foreach (Field field in fields.Where(f=>f.IsTableField))
			{
				Type modelType = typeof(T);
				object val = model.GetType().GetProperty(field.Name).GetValue(model);
				UpdateTableRowField(row, field.Name, val);
			}

            modelTables[model.GetType()].ForEach(mt => mt.EndProgrammaticUpdate());
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

			// TODO: Is this test for oldValue != newValue nececssary anymore???
			// TODO: CAN PROBABLY BE REMOVED NOW THAT WE HAVE THE MODEL MANAGER SETTING THE PROGRAMMATIC FLAG.

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
				PropertyChanged.Fire(record, new ModelPropertyChangedEventArgs() { FieldName = columnName, Value = val, OldValue = oldVal });
			}
		}

		/// <summary>
		/// Explicity fire the property changed event.
		/// </summary>
		public void FirePropertyChangedEvent(IEntity record, string columnName, object val, object oldVal)
		{
			PropertyChanged.Fire(record, new ModelPropertyChangedEventArgs() { FieldName = columnName, Value = val, OldValue = oldVal });
		}

		// TODO: Rename to TryGetRecord
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

		// TODO: Rename to TryGetRecord
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

		// TODO: Rename to GetRecord
		/// <summary>
		/// Returns the record, or null if not found, in the collection of the model type.
		/// </summary>
		public T GetRow<T>(Func<T, bool> predicate) where T : MappedRecord
		{
			Assert.That(mappedRecords.ContainsKey(typeof(T)), "Model Manager does not know about " + typeof(T).Name + ".\r\nCreate an instance of ModuleMgr with this record collection.");

			List<IEntity> items = mappedRecords[typeof(T)];
			T record = items.SingleOrDefault(predicate);

			return record;
		}

		// TODO: Rename to GetRecords
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

        public bool TryGetRow<T>(DataRow row, out T record) where T : MappedRecord
        {
            Assert.That(mappedRecords.ContainsKey(typeof(T)), "Model Manager does not know about " + typeof(T).Name + ".\r\nCreate an instance of ModuleMgr with this record collection.");

            record = mappedRecords[typeof(T)].Cast<T>().Where(r => r.Row == row).SingleOrDefault();

            return record != null;
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
                row.Table.AcceptChanges();
			}
		}

		public void Register<T>(IModelTable mt) where T : IEntity
		{
			modelTables[typeof(T)].Add(mt);
		}

		public void Unregister<T>(IModelTable mt) where T : IEntity
		{
			modelTables[typeof(T)].Remove(mt);
		}

		protected object DbNullConverter(object val)
		{
			return (val == DBNull.Value ? null : val);
		}

		protected void AppendRow(DataView view, Type recType, MappedRecord record)
		{
			modelTables[recType].ForEach(mt => mt.BeginProgrammaticUpdate());
			DataRow row = NewRow(view, recType, record);
			view.Table.Rows.Add(row);
			AddRecordToCollection(record);
			modelTables[recType].ForEach(mt => mt.EndProgrammaticUpdate());
		}

        protected DataView CreateDecoupledView(Type t)
        {
            DataTable dt = new DataTable();
            dt.TableName = t.Name;
            List<Field> fields = GetFields(t);
            CreateColumns(dt, fields);
            DataView dv = new DataView(dt);

            return dv;
        }

        protected void AppendDecoupledRow(DataView view, Type recType, MappedRecord record)
        {
            DataRow row = NewRow(view, recType, record);
            view.Table.Rows.Add(row);
        }

        protected void AddRecordToCollection(MappedRecord record)
		{
			Assert.That(mappedRecords.ContainsKey(record.GetType()), "Model Manager does not know about " + record.GetType().Name + ".\r\nCreate an instance of ModuleMgr with this record collection.");
			mappedRecords[record.GetType()].Add((IEntity)record);
		}

		protected void Register(Type recType)
		{
			mappedRecords[recType] = new List<IEntity>();
			modelTables[recType] = new List<IModelTable>();
		}

		protected DataRow NewRow(DataView view, Type modelType, MappedRecord record)
		{
			List<Field> fields = GetFields(modelType);
			DataRow row = view.Table.NewRow();

			foreach (Field field in fields.Where(f => f.IsTableField))
			{
				object val = modelType.GetProperty(field.Name).GetValue(record);
				row[field.Name] = val ?? DBNull.Value;
			}

			record.Row = row;

			return row;
		}

		// TODO: Get cached field lists for models.
		protected List<Field> GetFields<T>()
		{
			Type modelType = typeof(T);

			return GetFields(modelType);
		}

		protected List<Field> GetFields(Type modelType)
		{
            var props = from prop in modelType.GetProperties()
                        where Attribute.IsDefined(prop, typeof(ColumnAttribute)) || Attribute.IsDefined(prop, typeof(DisplayFieldAttribute))
                        select new Field()
                        {
                            Name = prop.Name,
                            DisplayName = Attribute.IsDefined(prop, typeof(DisplayNameAttribute)) ? ((DisplayNameAttribute)prop.GetCustomAttribute(typeof(DisplayNameAttribute))).DisplayName : prop.Name,
                            Type = prop.PropertyType,
                            ActualType = Attribute.IsDefined(prop, typeof(ActualTypeAttribute)) ? ((ActualTypeAttribute)prop.GetCustomAttribute(typeof(ActualTypeAttribute))).ActualTypeName : null,
                            MaxLength = Attribute.IsDefined(prop, typeof(MaxLengthAttribute)) ? ((MaxLengthAttribute)prop.GetCustomAttribute(typeof(MaxLengthAttribute))).MaxLength : 0,
                            ReadOnly = Attribute.IsDefined(prop, typeof(ReadOnlyAttribute)),
                            Visible = Attribute.IsDefined(prop, typeof(DisplayFieldAttribute)),
                            IsColumn = Attribute.IsDefined(prop, typeof(ColumnAttribute)),
                            IsDisplayField = Attribute.IsDefined(prop, typeof(DisplayFieldAttribute)),
                            Format = Attribute.IsDefined(prop, typeof(FormatAttribute)) ? ((FormatAttribute)prop.GetCustomAttribute(typeof(FormatAttribute))).Format : null,
							Lookup = Attribute.IsDefined(prop, typeof(LookupAttribute)) ? ((LookupAttribute)prop.GetCustomAttribute(typeof(LookupAttribute))) : null,
                            MappedColumn = Attribute.IsDefined(prop, typeof(MappedColumnAttribute)) ? ((MappedColumnAttribute)prop.GetCustomAttribute(typeof(MappedColumnAttribute))).Name : null,
						};

			return props.ToList();
		}

		protected void CreateColumns(DataTable dt, List<Field> fields)
		{
			foreach (Field field in fields.Where(f => f.IsTableField))
			{
				// Only create columns in the underlying data table for those properties in the model that have Column or DisplayField attributes.
				// Otherwise, we start tracking things like EntityRef properties, etc, that we don't want to track!
				ExtDataColumn dc;

				// Handle nullable types by creating the column type as the underlying, non-nullable, type.
				if (field.Type.Name == "Nullable`1")
				{
					dc = new ExtDataColumn(field.Name, field.Type.UnderlyingSystemType.GenericTypeArguments[0], field.Visible, field.IsColumn, field.Format, field.ActualType, field.MaxLength, field.Lookup);
				}
				else
				{
					dc = new ExtDataColumn(field.Name, field.Type, field.Visible, field.IsColumn, field.Format, field.ActualType, field.MaxLength, field.Lookup);
				}

				dc.ReadOnly = field.ReadOnly;
				dc.Caption = field.DisplayName;
                dc.MappedColumn = field.MappedColumn;
				dt.Columns.Add(dc);
			}
		}
	}
}
