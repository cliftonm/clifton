using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.Db
{
	public class ViewTableGraph
	{
		public List<ViewTableGraph> ParentTables;
		public List<ViewTableGraph> ChildTables;

		public ViewTableInfo ViewTable;

		public ViewTableGraph(ViewTableInfo viewTable)
		{
			ParentTables = new List<ViewTableGraph>();
			ChildTables = new List<ViewTableGraph>();
			ViewTable = viewTable;
		}
	}
}
