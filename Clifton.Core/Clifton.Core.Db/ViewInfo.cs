using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Clifton.Core.Assertions;
using Clifton.Core.ExtensionMethods;
using Clifton.Core.Utils;

namespace Clifton.Core.Db
{
	public class ViewInfo : IViewInfo
	{
		public string Name { get; set; }
		public string OrderBy { get; set; }

		public bool IsDistinct { get; set; }

		public List<ViewTableInfo> ViewTables { get; protected set; }

		public IReadOnlyCollection<ViewFieldInfo> AllFields
		{
			get
			{
				List<ViewFieldInfo> ret = new List<ViewFieldInfo>();
				ViewTables.ForEach(vt => ret.AddRange(vt.Fields));

				return ret.AsReadOnly();
			}
		}

		/// <summary>
		/// Get the ViewTableInfo given the aliased table name.
		/// </summary>
		public ViewTableInfo this[string aliasedTableName]
		{
			get { return ViewTables.Single(vt => vt.Alias == aliasedTableName); }
		}

		/// <summary>
		/// For deserialization.
		/// </summary>
		protected ViewInfo()
		{
			ViewTables = new List<ViewTableInfo>();
		}

		public ViewInfo(string name)
		{
			Name = name;
			ViewTables = new List<ViewTableInfo>();
		}

		/// <summary>
		/// Remove a collection of aliased field names from the view.
		/// </summary>
		public void RemoveFields(string[] aliasedFieldNames)
		{
			foreach (string f in aliasedFieldNames)
			{
				// Find the view table with the aliased field.
				foreach(ViewTableInfo vti in ViewTables)
				{
					if (vti.Fields.Exists(tf=>tf.Alias == f))
					{
						ViewFieldInfo vfi = vti.Fields.Single(tf => tf.Alias == f);
						vti.Fields.Remove(vfi);
					}
				}
			}
		}

		public void Serialize(XmlNode xviews)
		{
			XmlNode xview = xviews.AppendChildElement("View");
			xview.AddAttribute("Name", Name);
			OrderBy.IfNotNull(f => xview.AddAttribute("OrderBy", f));
			xview.AddAttribute("IsDistinct", IsDistinct.ToString());
			XmlNode xtables = xview.OwnerDocument.CreateElement("Tables");
			xview.AppendChild(xtables);

			foreach (ViewTableInfo table in ViewTables)
			{
				XmlNode xtable = xtables.AppendChildElement("Table");
				xtable.AddAttribute("Name", table.TableInfo.Name);
				xtable.AddAttribute("Alias", table.Alias);
				XmlNode xfields = xtable.AppendChildElement("Fields");

				foreach (ViewFieldInfo vfi in table.Fields)
				{
					vfi.Serialize(xfields);
				}
			}
		}

		public static ViewInfo Deserialize(List<TableInfo> tables, XmlNode xview)
		{
			ViewInfo vi = new ViewInfo();
			vi.Name = xview.GetAttributeValue("Name");
			vi.OrderBy = xview.GetAttributeValue("OrderBy");
			vi.IsDistinct = xview.GetAttributeValue("IsDistinct").to_b();

			XmlNode xtables = xview.ChildNodes[0];

			foreach (XmlNode xtable in xtables.ChildNodes)		// <Fields>
			{
				string tableName = xtable.GetAttributeValue("Name");
				string tableAlias = xtable.GetAttributeValue("Alias");
				ViewTableInfo vti = new ViewTableInfo();
				vti.TableInfo = tables.Single(t => t.Name == tableName);
				vi.ViewTables.Add(vti);

				if (tableName != tableAlias)
				{
					vti.Alias = tableAlias;
				}

				XmlNode xfields = xtable["Fields"];

				foreach (XmlNode xfield in xfields.ChildNodes)	// <Field>
				{
					ViewFieldInfo vfi = ViewFieldInfo.Deserialize(xfield);
					vfi.TableFieldInfo = tables.Single(t => t.Name == tableName).Fields.Single(f => f.FieldName == vfi.FieldName);
					vti.Fields.Add(vfi);
				}
			}

			return vi;
		}

		/// <summary>
		/// Return true if the aliased field name is defined by this view.
		/// </summary>
		public bool Exists(string alisedFieldName)
		{
			return GetFieldInternal(alisedFieldName) != null;
		}

		/// <summary>
		/// Returns the ViewFieldInfo instance matching the alias.
		/// </summary>
		public ViewFieldInfo GetField(string aliasFieldName)
		{
			ViewFieldInfo vfi = GetFieldInternal(aliasFieldName);
			Assert.That(vfi != null, "The aliased view field name " + aliasFieldName + " does not exist in the collection of table fields for the view " + Name);

			return vfi;
		}

		/// <summary>
		/// Returns the view field info instance or null if the aliased field name isn't defined by this view.
		/// </summary>
		protected ViewFieldInfo GetFieldInternal(string aliasFieldName)
		{
			foreach (ViewTableInfo vti in ViewTables)
			{
				foreach (ViewFieldInfo vfi in vti.Fields)
				{
					if (vfi.Alias == aliasFieldName)
					{
						return vfi;
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Returns a deep clone of the current view.  Registers the view.
		/// </summary>
		public ViewInfo Clone(string name)
		{
			ViewInfo view = new ViewInfo(name);

			foreach (ViewTableInfo vti in ViewTables)
			{
				view.ViewTables.Add(vti.Clone());
			}

			ViewSchema.Schema.RegisterView(view);

			return view;
		}

		/// <summary>
		/// Add a view field to the collection for this table.
		/// </summary>
		public void AddField(string aliasedTableName, ViewFieldInfo vfi)
		{
			ViewTables.Single(t => t.Alias == aliasedTableName).Fields.Add(vfi);
		}

		/// <summary>
		/// Remove the specified field.
		/// </summary>
		/// <param name="fieldName"></param>
		public void RemoveField(string aliasedTableName, string aliasedFieldName)
		{
			List<ViewFieldInfo> fields = ViewTables.Single(t => t.Alias == aliasedTableName).Fields;
			fields.Remove(fields.Single(f => f.Alias == aliasedFieldName));
		}

		/// <summary>
		/// TODO: We really need to solve the casing issue so we don't have to do all these "tolower()" calls during computation of the SQL statement!
		/// </summary>
		public int IndexOf(string aliasedTableName)
		{
			int idx = -1;
			int ret = idx;

			foreach (ViewTableInfo t in ViewTables)
			{
				++idx;

				if (t.Alias == aliasedTableName)
				{
					ret = idx;
					break;
				}
			}

			if (ret == -1)
			{
				throw new ApplicationException("Table alias " + aliasedTableName + " is not part of the view " + Name + " table collection");
			}

			return ret;
		}
	}
}
