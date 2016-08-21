using System;

using Clifton.Core.ServiceManagement;

namespace ServiceManagerExclusiveDemo
{
    public interface IAnimal : IService
    {
        void Speak();
    }


    public class Cat : ServiceBase, IAnimal
    {
        public void Speak()
        {
            Console.WriteLine("Meow");
        }
    }

    public static class ExclusiveDemo
    {
        public static void Demo()
        {
            ServiceManager svcMgr = new ServiceManager();
            svcMgr.RegisterSingleton<IAnimal, Cat>();
            IAnimal animal = svcMgr.Get<IAnimal>();
            animal.Speak();
        }
    }
}

