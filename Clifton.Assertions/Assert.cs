using System;
using System.Diagnostics;

namespace Clifton.Assertions
{
	public class Assert
	{
		/// <summary>
		/// Assert that the condition is false.
		/// </summary>
		[Conditional("DEBUG")]
		public static void Not(bool b, string msg)
		{
			That(!b, msg);
		}

		/// <summary>
		/// Assert that the condition is true.
		/// </summary>
		[Conditional("DEBUG")]
		public static void That(bool b, string msg)
		{
			if (!b)
			{
				throw new ApplicationException(msg);
			}
		}
	}
}
