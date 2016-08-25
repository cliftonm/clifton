using System;

using Clifton.Core.ModuleManagement;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;

using ServiceInterfaces;

namespace ConsoleLoggerService
{
    public class LoggerModule : IModule
    {
        public void InitializeServices(IServiceManager serviceManager)
        {
            serviceManager.RegisterSingleton<IConsoleLoggerService, LoggerService>();
        }
    }

    public class LoggerService : ServiceBase, IConsoleLoggerService, IReceptor
    {
        public override void FinishedInitialization()
        {
            ISemanticProcessor semProc = ServiceManager.Get<ISemanticProcessor>();
            semProc.Register<LoggerMembrane, GenericTypeLogger>();
            semProc.Register<LoggerMembrane, LoggerService>();
        }

        public void Log(string msg)
        {
            Console.WriteLine(msg);
        }

        public void Process(ISemanticProcessor semProc, IMembrane membrane, ST_Exception msg)
        {
            Log(msg.Exception.Message);
            Log(msg.Exception.StackTrace);
        }

        public void Process(ISemanticProcessor semProc, IMembrane membrane, ST_Log msg)
        {
            Log(msg.Message);
        }
    }

    public class GenericTypeLogger : IReceptor
    {
        public void Process(ISemanticProcessor semProc, IMembrane membrane, ISemanticType t)
        {
            if ( (!(t is ST_Log)) && (!(t is ST_Exception)) )
            {
                Console.WriteLine("Publishing type: " + t.GetType().Name);
            }
        }
    }
}

