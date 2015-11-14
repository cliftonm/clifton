using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.Db
{
	/// <summary>
	/// View tables mirror the physical table but can specify an alias to support multiple joins against the same table.
	/// </summary>
	public class ViewTableInfo
	{
		protected string alias;
		
		public List<ViewFieldInfo> Fields { get; protected set; }

		public TableInfo TableInfo { get; set; }

		public string Alias
		{
			get { return String.IsNullOrEmpty(alias) ? TableInfo.Name : alias; }
			set { alias = value; }
		}

		public ViewTableInfo()
		{
			Fields = new List<ViewFieldInfo>();
		}

		public ViewTableInfo Clone()
		{
			ViewTableInfo vti = new ViewTableInfo();
			vti.alias = alias;
			vti.TableInfo = TableInfo;

			foreach (ViewFieldInfo vfi in Fields)
			{
				vti.Fields.Add(vfi.Clone());
			}

			return vti;
		}
	}
}
