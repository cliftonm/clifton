using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.Core.Db
{
	public static class Extensions
	{
		public static void Dump(this Dictionary<string, object> dictionary)
		{
			if (dictionary == null)
			{
				Console.WriteLine("No parameters.");
			}
			else
			{
				foreach (KeyValuePair<string, object> kvp in dictionary)
				{
					if (kvp.Value == null)
					{
						Console.WriteLine(kvp.Key + " => null");
					}
					else
					{
						Console.WriteLine(kvp.Key + " => " + kvp.Value.ToString());
					}
				}
			}
		}
	}
}
