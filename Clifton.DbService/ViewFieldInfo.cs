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
	public class ViewFieldInfo
	{
		public TableFieldInfo TableFieldInfo { get; set; }

		public string Caption
		{
			get
			{
				string ret = caption;
				TableFieldInfo.Caption.IfNotNull(c => ret = c).Else(() => ret = caption);

				return ret;
			}
			set { caption = value; }
		}
		
		public bool IsVisible
		{
			get
			{
				bool ret = isVisible;

				if (!isVisibleDefined)
				{
					ret = TableFieldInfo.IsVisible;
				}

				return ret;
			}
			set 
			{ 
				isVisible = value;
				isVisibleDefined = true;
			}
		}

		public bool IsExcludedFromQuery { get; set; } // most often used when needing to get a distinct set of records and we don't want an FK ID in that set.
		public bool IsReadOnly { get; set; }
		public bool IsPassword { get; set; }
		
		public Dictionary<string, string> ClientProperties;

		/// <summary>
		/// Optional alias, required to when fields in a table join have the same name and must be resolved.
		/// The return value is the alias (if defined) or the field name (if alias is not defined.)
		/// </summary>
		public string Alias
		{
			get { return !String.IsNullOrEmpty(alias) ? alias : TableFieldInfo.FieldName; }
			set { alias = value; }
		}

		// TODO: Get rid of this.  If we have a view field that doesn't correspond to a table, we can still have a TabieFieldInfo pointing to a "virtual" TableInfo, or something like that.
		/// <summary>
		/// View field may not correspond to a table field.
		/// </summary>
		public string FieldName { get; set; }

		/// <summary>
		/// If the value must be computed in realtime, use this property.  This creates a placeholder column in the returned DataTable.
		/// Computed values can be read-only or read-write, depending on what you want to use the computed value for.
		/// </summary>
		public Func<object> ComputedValue 
		{
			get { return computedValue; }
			set
			{
				computedValue = value;

				// Check for clone assignment, which may pass in a null.
				if (computedValue != null)
				{
					// For a computed value, the sql format is an empty string so we get a placeholder when we do a query.
					sqlFormat = "''";
				}
			}
		}

		/// <summary>
		/// True if the field value is computed by the database.  This prevents the field from ever being written to.
		/// </summary>
		public bool IsSqlComputed { get; set; }

        /// <summary>
        /// The type of input in HTML.
        /// Don't 'calculate' this value as one CLR type can translate to multiple input types (such as string can be text, email or password).
        /// </summary>
        // public string InputType { get; set; }

		/// <summary>
		/// Test if the field is part of a foreign key.
		/// </summary>
		public bool IsFK { get { return TableFieldInfo.TableInfo.References.Where(r => r.FkPkMap.ContainsKey(FieldName)).Count() != 0; } }

		public List<TableForeignKey> References { get { return TableFieldInfo.TableInfo.References.Where(r => r.FkPkMap.ContainsKey(FieldName)).ToList(); } }

		protected string alias;
		protected string sqlFormat;
		protected string caption;
		protected Func<object> computedValue;
		protected bool isVisible;
		protected bool isVisibleDefined;

		public ViewFieldInfo()
		{
			IsVisible = true;
			ClientProperties = new Dictionary<string, string>();
		}

		public XmlNode Serialize(XmlNode xfields)
		{
			XmlNode xfield = xfields.AppendChildElement("Field");
			xfield.AddAttribute("Name", FieldName);
			xfield.AddAttribute("Alias", Alias);
			xfield.AddAttribute("Caption", Caption);
			xfield.AddAttribute("IsVisible", IsVisible.ToString());
			xfield.AddAttribute("IsExcludedFromQuery", IsExcludedFromQuery.ToString());
			xfield.AddAttribute("IsReadOnly", IsReadOnly.ToString());
			xfield.AddAttribute("IsSqlComputed", IsSqlComputed.ToString());

			// Can't serialize ComputedValue

			return xfield;
		}

		public static ViewFieldInfo Deserialize(XmlNode xfield)
		{
			ViewFieldInfo vfi = new ViewFieldInfo();
			vfi.FieldName = xfield.GetAttributeValue("Name");
			vfi.Alias = xfield.GetAttributeValue("Alias");
			vfi.Caption = xfield.GetAttributeValue("Caption");
			vfi.IsVisible = xfield.GetAttributeValue("IsVisible").to_b();
			vfi.IsExcludedFromQuery = xfield.GetAttributeValue("IsExcludedFromQuery").to_b();
			vfi.IsReadOnly = xfield.GetAttributeValue("IsReadOnly").to_b();
			vfi.IsSqlComputed = xfield.GetAttributeValue("IsSqlComputed").to_b();

			return vfi;
		}

		/// <summary>
		/// Deep clone.
		/// </summary>
		public ViewFieldInfo Clone()
		{
			ViewFieldInfo vfi = new ViewFieldInfo();
			vfi.TableFieldInfo = TableFieldInfo;
			vfi.Caption = Caption;
			vfi.IsVisible = IsVisible;
			vfi.IsReadOnly = IsReadOnly;
			vfi.FieldName = FieldName;
			vfi.IsPassword = IsPassword;
			vfi.Alias = alias;		// Copy the internal field, not the property, which returns the field name if the alias isn't defined.
			vfi.ComputedValue = ComputedValue;
			vfi.IsSqlComputed = IsSqlComputed;

			return vfi;
		}
	}
}
