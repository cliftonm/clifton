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
	public class TableFieldInfo
	{
		public TableInfo TableInfo { get; set; }

		public string FieldName { get; set; }
		public string Caption { get; set; }
		public Type DataType { get; set; }
		public bool IsVisible { get; set; }
		public bool IsPK { get; set; }
		public bool IsNullable { get; set; }
		public bool IsPassword { get; set; }

		/// <summary>
		/// Optional field that requires a SQL-server operation.
		/// </summary>
		public string SqlFormat
		{
			get { return sqlFormat; }
			set { sqlFormat = value; }
		}

		protected string sqlFormat;

		public TableFieldInfo()
		{
		}

		public XmlNode Serialize(XmlNode xfields)
		{
			XmlNode xfield = xfields.AppendChildElement("Field");
			xfield.AddAttribute("Name", FieldName);
			xfield.AddAttribute("Caption", Caption);
			xfield.AddAttribute("DataType", DataType.Name);
			xfield.AddAttribute("IsVisible", IsVisible.ToString());
			xfield.AddAttribute("IsPK", IsPK.ToString());
			xfield.AddAttribute("IsNullable", IsNullable.ToString());
			xfield.AddAttribute("IsPassword", IsPassword.ToString());
			SqlFormat.IfNotNull(s => xfield.AddAttribute("SqlFormat", s.ToString()));

			// Can't serialize ComputedValue

			return xfield;
		}

		public static TableFieldInfo Deserialize(XmlNode xfield)
		{
			string sqlFormat;

			TableFieldInfo tfi = new TableFieldInfo();
			tfi.FieldName = xfield.GetAttributeValue("Name");
			tfi.Caption = xfield.GetAttributeValue("Caption");
			tfi.IsVisible = xfield.GetAttributeValue("IsVisible").to_b();
			tfi.IsPK = xfield.GetAttributeValue("IsPK").to_b();
			tfi.IsNullable = xfield.GetAttributeValue("IsNullable").to_b();
			tfi.IsPassword = xfield.GetAttributeValue("IsPassword").to_b();
			xfield.TryGetAttributeValue("SqlFormat", out sqlFormat).If(()=>tfi.SqlFormat = sqlFormat);

			string dataType = xfield.GetAttributeValue("DataType");
			tfi.DataType = Type.GetType("System." + dataType);

			return tfi;
		}
	}
}
