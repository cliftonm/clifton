using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.Core.Db
{
	public class PrimaryKey
	{
		[DbFieldName("IndexName")]
		public string Name { get; set; }
	
		[DbFieldName("TableName")]
		public string TableName { get; set; }

		[DbFieldName("ColumnName")]
		public string ColumnName { get; set; }
	}
}
