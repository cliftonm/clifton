using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Clifton.ExtensionMethods;
using Clifton.Utils;

namespace Clifton.DbServices
{
	public class TableInfo
	{
		public string Name { get; set; }

		/// <summary>
		/// The collection of fields in this table.
		/// </summary>
		public List<TableFieldInfo> Fields { get; protected set; }

		// Foreign key specifications for this table.
		public List<TableForeignKey> References { get; protected set; }

		protected string alias;

		/// <summary>
		/// For deserialization.
		/// </summary>
		protected TableInfo()
		{
			Fields = new List<TableFieldInfo>();
			References = new List<TableForeignKey>();							
		}

		public TableInfo(string name)
		{
			Name = name;
			Fields = new List<TableFieldInfo>();
			References = new List<TableForeignKey>();
		}

		public TableFieldInfo GetField(string fieldName)
		{
			return Fields.Single(f => f.FieldName == fieldName);
		}

		/// <summary>
		/// Return the list of fields joining the parent (PK table) to this table (the child table)
		/// </summary>
		/// <param name="table"></param>
		/// <returns></returns>
		public string GetJoinFields(TableInfo table)
		{
			string joinClause = null;

			// We could have multiple ways to join the two tables, pick the first way.
			try
			{
				TableForeignKey fk = References.First(r => r.PkTable == table.Name);
				List<string> joins = new List<string>();

				foreach(KeyValuePair<string, string> field in fk.FkPkMap)
				{
					// [PK table].[PK field] = [FK table].[FK field]
					joins.Add(table.Name.Brackets() + "." + field.Value.Brackets() + " = "+Name.Brackets()+"."+field.Key.Brackets());
				}

				joinClause = String.Join(" and ", joins);
			}
			catch
			{
				throw new ApplicationException("Can't join: the table " + Name + " is not a child of the table " + table.Name);
			}

			return joinClause;
		}

		public XmlNode Serialize(XmlNode xtables)
		{
			XmlNode xtable = xtables.AppendChildElement("Table");
			xtable.AddAttribute("Name", Name);
			XmlNode xfields = xtable.AppendChildElement("Fields");

			foreach (TableFieldInfo tfi in Fields)
			{
				tfi.Serialize(xfields);
			}

			if (References.Count > 0)
			{
				XmlNode xrefs = xtable.AppendChildElement("ForeignKeys");

				foreach (TableForeignKey tfk in References)
				{
					tfk.Serialize(xrefs);
				}
			}

			return xtable;
		}

		public static TableInfo Deserialize(XmlNode node)
		{
			TableInfo ti = new TableInfo();
			ti.Name = node.GetAttributeValue("Name");
			XmlNode xfields = node["Fields"];

			foreach (XmlNode xfield in xfields)
			{
				TableFieldInfo tfi = TableFieldInfo.Deserialize(xfield);
				ti.Fields.Add(tfi);
				tfi.TableInfo = ti;
			}

			if (node["ForeignKeys"] != null)
			{
				XmlNode xfks = node["ForeignKeys"];

				foreach (XmlNode xfk in xfks)
				{
					TableForeignKey tfk = TableForeignKey.Deserialize(xfk);
					ti.References.Add(tfk);
				}
			}

			return ti;
		}
	}
}
