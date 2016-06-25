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

namespace Clifton.ConsoleLoggerService
{
	public class LoggerModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IConsoleLoggerService, LoggerService>();
		}
	}

	public class LoggerService : ServiceBase, IConsoleLoggerService
	{
		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			ServiceManager.Get<ISemanticProcessor>().Register<LoggerMembrane, LoggerReceptor>();
		}

		public virtual void Log(string msg)
		{
			Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss : ") + msg);
		}

		public virtual void Log(LogMessage msg)
		{
			Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss : ") + msg.Value);
		}

		public virtual void Log(ExceptionMessage msg)
		{
			Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss EXCEPTION : ") + msg.Value);
		}

		public virtual void Log(Exception ex)
		{
			while (ex != null)
			{
				Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss EXCEPTION : ") + ex.Message);
				Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss EXCEPTION : ") + ex.StackTrace);
				ex = ex.InnerException;

				if (ex != null)
				{
					Console.WriteLine("Inner Exception:");
				}
			}
		}
	}

	public class LoggerReceptor : IReceptor
	{
		public void Process(ISemanticProcessor proc, IMembrane membrane, ST_Log msg)
		{
			proc.ServiceManager.Get<IConsoleLoggerService>().Log(msg.Message);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, ST_Exception exception)
		{
			proc.ServiceManager.Get<IConsoleLoggerService>().Log(exception.Exception);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, ST_ExceptionObject exception)
		{
			proc.ServiceManager.Get<IConsoleLoggerService>().Log(exception.ExceptionMessage.Value);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, ST_CompilerError error)
		{
			proc.ServiceManager.Get<IConsoleLoggerService>().Log(error.Error);
		}
	}
}
