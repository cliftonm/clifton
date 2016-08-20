using System;

using Clifton.Core.ServiceManagement;

namespace ServiceManagerInstanceDemo
{
    public interface IAnimal : IService
    {
        string Name { get; set; }
        void Speak();
    }

    public interface ICat : IAnimal { }

    public abstract class Animal : ServiceBase, IAnimal
    {
        public string Name { get; set; }
        public abstract void Speak();
    }

    public class Cat : Animal, ICat
    {
        public override void Speak()
        {
            Console.WriteLine(Name + " says 'Meow'");
        }
    }

    public static class InstanceDemo
    {
        public static void Demo()
        {
            ServiceManager svcMgr = new ServiceManager();
            svcMgr.RegisterInstanceOnly<ICat, Cat>();
            IAnimal cat1 = svcMgr.Get<ICat>();
            cat1.Name = "Fido";
            IAnimal cat2 = svcMgr.Get<ICat>(cat => cat.Name = "Morris");
            cat1.Speak();
            cat2.Speak();
        }
    }
}

