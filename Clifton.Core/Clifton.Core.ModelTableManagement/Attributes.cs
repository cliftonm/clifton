using System;

namespace Clifton.Core.ModelTableManagement
{
	public class DisplayFieldAttribute : Attribute { }
	public class UniqueAttribute : Attribute { }
	public class ReadOnlyAttribute : Attribute { }

	/// <summary>
	/// Add View attribute to the data context to indicate that a Table is a view and should not be created in the database.
	/// </summary>
	public class ViewAttribute : Attribute { }			

	public class DisplayNameAttribute : Attribute
	{
		public string DisplayName { get; set; }

		public DisplayNameAttribute(string name)
			: base()
		{
			DisplayName = name;
		}
	}

	public class LookupAttribute : Attribute
	{
		public string ForeignKeyTable { get; set; }
		public Type ModelType { get; set; }
		public string DisplayField { get; set; }

		public LookupAttribute(string fkTable)
			: base()
		{
			ForeignKeyTable = fkTable;
			DisplayField = "Name";			// Default
		}
	}
}
