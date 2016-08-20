using System;

using Clifton.Core.ServiceManagement;

namespace ServiceManagerSingletonDemo
{
    public interface IAnimal : IService
    {
        void Speak();
    }

    public interface ICat : IAnimal { }
    public interface IDog : IAnimal { }

    public class Cat : ServiceBase, ICat
    {
        public void Speak()
        {
            Console.WriteLine("Meow");
        }
    }

    public class Dog : ServiceBase, IDog
    {
        public void Speak()
        {
            Console.WriteLine("Woof");
        }
    }

    public static class SingletonDemo
    {
        public static void Demo()
        {
            ServiceManager svcMgr = new ServiceManager();
            svcMgr.RegisterSingleton<ICat, Cat>();
            svcMgr.RegisterSingleton<IDog, Dog>();
            IAnimal animal1 = svcMgr.Get<ICat>();
            IAnimal animal2 = svcMgr.Get<IDog>();
            animal1.Speak();
            animal2.Speak();
        }
    }
}

