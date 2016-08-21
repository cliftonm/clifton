using System;

using ServiceManagerSingletonDemo;
using ServiceManagerInstanceDemo;
using ServiceManagerExclusiveDemo;
using ServiceManagerByTypeDemo;

namespace ServiceManagerDemo
{
	class Program
	{
		static void Main(string[] args)
		{
            Console.WriteLine("Singleton:");
            SingletonDemo.Demo();

            Console.WriteLine("\r\nInstance:");
            InstanceDemo.Demo();

            Console.WriteLine("\r\nExclusive:");
            ExclusiveDemo.Demo();

            Console.WriteLine("\r\nBy Type:");
            ByTypeDemo.RegisterServices();
            ByTypeDemo.ByTypeParameter(typeof(ServiceManagerByTypeDemo.ICat));
            ByTypeDemo.ByTypeParameter(typeof(ServiceManagerByTypeDemo.IDog));
            ByTypeDemo.ByGenericParameter<ServiceManagerByTypeDemo.ICat>();
            ByTypeDemo.ByGenericParameter<ServiceManagerByTypeDemo.IDog>();
        }
    }
}
