using System;
using System.Windows.Forms;

namespace Clifton.Core.ExtensionMethods
{
	public static class WinFormExtensionMethods
	{
		/// <summary>
		/// Asynchronous invoke on application thread.  Will return immediately unless invocation is not required.
		/// </summary>
		public static void BeginInvoke(this Control control, Action action)
		{
			if (control.InvokeRequired)
			{
				// We want a synchronous call here!!!!
				control.BeginInvoke((Delegate)action);
			}
			else
			{
				action();
			}
		}

		/// <summary>
		/// Synchronous invoke on application thread.  Will not return until action is completed.
		/// </summary>
		public static void Invoke(this Control control, Action action)
		{
			if (control.InvokeRequired)
			{
				// We want a synchronous call here!!!!
				control.Invoke((Delegate)action);
			}
			else
			{
				action();
			}
		}
	}
}
