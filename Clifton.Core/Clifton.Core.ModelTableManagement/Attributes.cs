using System;

namespace Clifton.Core.ModelTableManagement
{
	public class DisplayFieldAttribute : Attribute { }
	public class UniqueAttribute : Attribute { }
	public class ReadOnlyAttribute : Attribute { }

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

		public LookupAttribute(string fkTable)
			: base()
		{
			ForeignKeyTable = fkTable;
		}
	}
}
