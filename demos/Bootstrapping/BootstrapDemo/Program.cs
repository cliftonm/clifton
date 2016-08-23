using System;

using Clifton.Core.ServiceInterfaces;

namespace BootstrapDemo
{
    static partial class Program
	{
		static void Main(string[] args)
		{
            InitializeBootstrap();
            Bootstrap((e) => Console.WriteLine(e.Message));

            //Console.WriteLine("some connection string".Encrypt("somepassword", "somesalt"));
            //Console.WriteLine("someKeyValue".Encrypt("somepassword", "somesalt"));

            IConfigService cfgSvc = serviceManager.Get<IConfigService>();
            Console.WriteLine(cfgSvc.GetConnectionString("myConnectionString"));
            Console.WriteLine(cfgSvc.GetValue("someKey"));
		}
	}
}
