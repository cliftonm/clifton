using System;

using Clifton.Core.ServiceManagement;

namespace ServiceManagerByTypeDemo
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

    public static class ByTypeDemo
    {
        private static ServiceManager svcMgr;

        public static void RegisterServices()
        {
            svcMgr = new ServiceManager();
            svcMgr.RegisterSingleton<ICat, Cat>();
            svcMgr.RegisterSingleton<IDog, Dog>();
        }

        public static void ByTypeParameter(Type someAnimal)
        {
            IAnimal animal = svcMgr.Get<IAnimal>(someAnimal);
            animal.Speak();
        }

        public static void ByGenericParameter<T>() where T : IAnimal
        {
            IAnimal animal = svcMgr.Get<T>();
            animal.Speak();
        }
    }
}

