using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.DbServices
{
	public class ForeignKey
	{
		[DbFieldName("IndexName")]
		public string Name { get; set; }

		[DbFieldName("SchemaName")]
		public string SchemaName { get; set; }

		[DbFieldName("TableName")]
		public string TableName { get; set; }

		[DbFieldName("ColName")]
		public string ColumnName { get; set; }

		[DbFieldName("ReferencedSchemaName")]
		public string ReferencedSchemaName { get; set; }

		[DbFieldName("ReferencedTableName")]
		public string ReferencedTableName { get; set; }

		[DbFieldName("ReferencedColumnName")]
		public string ReferencedColumnName { get; set; }
	}
}
