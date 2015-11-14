using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.Db
{
	/// <summary>
	/// Used to map properties in a class to database aliased (!) field names.
	/// </summary>
	public class DbFieldNameAttribute : Attribute
	{
		public string FieldName { get; protected set; }

		public DbFieldNameAttribute(string name)
		{
			FieldName = name;
		}
	}
}
