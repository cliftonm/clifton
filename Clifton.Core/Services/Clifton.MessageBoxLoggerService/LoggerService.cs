using System;
using System.Windows.Forms;

using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceInterfaces;
using Clifton.Core.ServiceManagement;

namespace Clifton.MessageBoxLoggerService
{
	public class LoggerModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IMessageBoxLoggerService, LoggerService>();
		}
	}

	public class LoggerService : ServiceBase, IMessageBoxLoggerService
	{
		public virtual void Log(string msg)
		{
			MessageBox.Show(msg, "Logger", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		public virtual void Log(LogMessage msg)
		{
			MessageBox.Show(msg.Value, "Logger", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		public virtual void Log(ExceptionMessage msg)
		{
			MessageBox.Show(msg.Value, "Logger", MessageBoxButtons.OK, MessageBoxIcon.Error);

		}

		public virtual void Log(Exception ex)
		{
			MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace, "Logger", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}
}
