using System;

using Clifton.Core.Semantics;

namespace SemanticPublisherSubscriberDemo
{
    static partial class Program
    {
        static void Main(string[] args)
        {
            InitializeBootstrap();
            Bootstrap((e) => Console.WriteLine(e.Message));

            ISemanticProcessor semProc = serviceManager.Get<ISemanticProcessor>();

            // Stateless subscriber:
            semProc.Register<SurfaceMembrane, Subscriber>();
            semProc.ProcessInstance<SurfaceMembrane, ST_Message>();
            semProc.ProcessInstance<SurfaceMembrane, ST_Message>(m => m.Text = "Hello World", true);

            // Stateful subscriber:
            semProc.Register<SurfaceMembrane>(new StatefulSubscriber());
            semProc.ProcessInstance<SurfaceMembrane, ST_Message2>(m => m.Text = "Hello World", true);
            semProc.ProcessInstance<SurfaceMembrane, ST_Message2>(m => m.Text = "Hello World", true);
        }
    }

    public class ST_Message : ISemanticType
    {
        public string Text { get; set; }

        public ST_Message()
        {
            Console.WriteLine("Message Instantiated.");
        }
    }

    public class ST_Message2 : ISemanticType
    {
        public string Text { get; set; }
    }

    public class Subscriber : IReceptor
    {
        public Subscriber()
        {
            Console.WriteLine("Subscriber Instantiated.");
        }

        public void Process(ISemanticProcessor semProc, IMembrane membrane, ST_Message msg)
        {
            Console.WriteLine(msg.Text);
        }
    }

    public class StatefulSubscriber : IReceptor
    {
        protected int counter = 0;

        public void Process(ISemanticProcessor semProc, IMembrane membrane, ST_Message2 msg)
        {
            Console.WriteLine(counter + ": " + msg.Text);
            ++counter;
        }
    }
}
