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

using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;

namespace Clifton.Core.Services.ConsoleCriticalExceptionService
{
	public class ConsoleCriticalExceptionModule : IModule
	{
		public virtual void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IConsoleCriticalExceptionService, ConsoleCriticalException>();
		}
	}

	public class ConsoleCriticalException : ServiceBase, IConsoleCriticalExceptionService
	{
		public override void Initialize(IServiceManager svcMgr)
		{
			base.Initialize(svcMgr);
			AppDomain.CurrentDomain.UnhandledException += GlobalExceptionHandler;
		}

		protected virtual void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs e)
		{
			try
			{
				if (e.ExceptionObject is Exception)
				{
					ServiceManager.Get<ISemanticProcessor>().ProcessInstance<LoggerMembrane, ST_Exception>(ex2 => ex2.Exception = ((Exception)e.ExceptionObject));
				}
				else
				{
					ServiceManager.Get<ISemanticProcessor>().ProcessInstance<LoggerMembrane, ST_ExceptionObject>(em=> em.ExceptionMessage = ExceptionMessage.Create(e.ExceptionObject.GetType().Name));
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
			}

			Environment.Exit(1);
		}
	}
}
