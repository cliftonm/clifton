using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.Utils
{
	public static class Assert
	{
		public static void That(bool b, string msg)
		{
			if (!b)
			{
				throw new ApplicationException(msg);
			}
		}
	}
}
