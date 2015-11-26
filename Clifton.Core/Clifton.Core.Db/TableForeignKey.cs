using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Clifton.Core.ExtensionMethods;
using Clifton.Core.Utils;

namespace Clifton.Core.Db
{
	/// <summary>
	/// Used for handling foreign key lookups.
	/// </summary>
	public class TableForeignKey
	{
		/// <summary>
		/// The PK Table name in this PK cluster.
		/// </summary>
		public string PkTable { get; set; }

		/// <summary>
		/// Mapping of foreign keys fields to primary keys fields, supporting clustered keys.
		/// </summary>
		public Dictionary<string, string> FkPkMap { get; protected set; }

		/// <summary>
		/// Value fields, used for display purposes on the FK table.
		/// </summary>
		public List<string> ValueFields { get; protected set; }

		// TODO: Should this be here, or in the view's table definitions?
		/// <summary>
		/// The URL for populating the lookup information, used for web-based apps.
		/// </summary>
		public string Url { get; set; }

		public TableForeignKey()
		{
			FkPkMap = new Dictionary<string, string>();
			ValueFields = new List<string>();
		}

		public void Serialize(XmlNode xrefs)
		{
			XmlNode xref = xrefs.AppendChildElement("ForeignKey");
			xref.AddAttribute("PKTable", PkTable);
			Url.IfNotNull(f => xref.AddAttribute("Url", f));

			XmlNode xrefFields = xref.AppendChildElement("Fields");

			foreach (KeyValuePair<string, string> kvp in FkPkMap)
			{
				XmlNode xrefField = xrefFields.AppendChildElement("Field");
				xrefField.AddAttribute("FKField", kvp.Key);
				xrefField.AddAttribute("PKField", kvp.Value);
			}

			XmlNode xrefValFields = xref.AppendChildElement("ValueFields");

			foreach (string valField in ValueFields)
			{
				XmlNode xvalField = xrefValFields.AppendChildElement("Field");
				xvalField.AddAttribute("Name", valField);
			}
		}

		public static TableForeignKey Deserialize(XmlNode xref)
		{
			TableForeignKey tfk = new TableForeignKey();
			tfk.PkTable = xref.GetAttributeValue("PKTable");
			XmlNode xrefFields = xref["Fields"];

			// PK - FK map
			foreach (XmlNode xrefField in xrefFields)
			{
				string fkColName = xrefField.GetAttributeValue("FKField");
				string pkColName = xrefField.GetAttributeValue("PKField");
				tfk.FkPkMap[fkColName] = pkColName;
			}

			XmlNode xrefValFields = xref["ValueFields"];

			// Display value fields.
			foreach (XmlNode xrefValField in xrefValFields)
			{
				tfk.ValueFields.Add(xrefValField.GetAttributeValue("Name"));
			}

			return tfk;
		}

		public TableForeignKey Clone()
		{
			TableForeignKey l = new TableForeignKey();
			l.FkPkMap = FkPkMap;					// The pk-fk map never changes.  A cloned view should not change this.
			l.ValueFields = ValueFields;			// Value fields also don't change.  They can be selected from in the UI implementation or possibly overridden by the view if this feature needs to be implemented.
			l.Url = Url;

			return l;
		}
	}
}
