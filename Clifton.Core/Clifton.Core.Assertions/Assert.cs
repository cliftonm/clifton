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

namespace Clifton.Core.Assertions
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

        [Conditional("DEBUG")]
        public static void That<T>(bool b, string msg) where T : Exception, new()
        {
            if (!b)
            {
                Exception ex = (Exception)Activator.CreateInstance(typeof(T), new object[] { msg });
                throw ex;
            }
        }


        public static void Try(Action a, Action<Exception> onErr = null)
		{
			try 
			{ 
				a(); 
			}
			catch(Exception ex)
			{
				if (onErr != null)
				{
					onErr(ex);
				}
				else
				{
					throw;
				}
			}
		}

		public static void SilentTry(Action a)
		{
			try { a(); }
			catch { }
		}
	}
}
