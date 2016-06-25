/* The MIT License (MIT)
* 
* Copyright (c) 2015 Marc Clifton
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Clifton.Core.Utils
{
	public static class ProcessHelper
	{
		[DllImport("user32.dll")]
		public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll")]
		public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool BringWindowToTop(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern bool IsIconic(IntPtr hWnd);

		private const int SW_HIDE = 0;
		private const int SW_SHOWNORMAL = 1;
		private const int SW_SHOWMINIMIZED = 2;
		private const int SW_SHOWMAXIMIZED = 3;
		private const int SW_SHOWNOACTIVATE = 4;
		private const int SW_RESTORE = 9;
		private const int SW_SHOWDEFAULT = 10;

		public static void KillProcess(string name)
		{
			foreach (Process clsProcess in Process.GetProcesses().Where(clsProcess => clsProcess.ProcessName.ToLower().StartsWith(name.ToLower())))
			{
				clsProcess.Kill();
				break;
			}
		}

		public static bool ProcessExists(string name)
		{
			return Process.GetProcesses().Where(proc => proc.ProcessName.ToLower() == name.ToLower()).Count() > 0;
		}

		public static void SwitchToProcess(int id)
		{
			Process p = Process.GetProcesses().SingleOrDefault(proc => proc.Id == id);

			if (p != null)
			{
				IntPtr hWnd = p.MainWindowHandle;
				// Restore it and bring it to the front.
				ShowWindowAsync(hWnd, SW_RESTORE);
				SetForegroundWindow(hWnd);
				BringWindowToTop(hWnd);
			}
		}

		// http://www.codeproject.com/Articles/2976/Detect-if-another-process-is-running-and-bring-it 
		public static void SwitchToOtherProcess(string name)
		{
			Process[] processes = Process.GetProcessesByName(name);

			// if there is more than one process...
			if (processes.Length > 1)
			{
				// Assume there is our process, which we will terminate, 
				// and the other process, which we want to bring to the 
				// foreground. This assumes there are only two processes 
				// in the processes array, and we need to find out which 
				// one is NOT us.

				// get our process
				Process p = Process.GetCurrentProcess();
				int n = 0;        // assume the other process is at index 0
				// if this process id is OUR process ID...
				if (processes[0].Id == p.Id)
				{
					// then the other process is at index 1
					n = 1;
				}
				// get the window handle
				IntPtr hWnd = processes[n].MainWindowHandle;

				// if iconic, we need to restore the window
				if (IsIconic(hWnd))
				{
					ShowWindow(hWnd, SW_RESTORE);
					Thread.Sleep(250);
					ShowWindow(hWnd, SW_SHOWNORMAL);
				}
				else
				{
					ShowWindow(hWnd, SW_SHOWMINIMIZED);
					Thread.Sleep(250);
					ShowWindow(hWnd, SW_SHOWNORMAL);
					Thread.Sleep(250);
					BringWindowToTop(hWnd);
				}
			}
		}

		public static int Count(string name)
		{
			return Process.GetProcesses().Where(proc => proc.ProcessName.ToLower() == name.ToLower()).Count();
		}
	}
}
