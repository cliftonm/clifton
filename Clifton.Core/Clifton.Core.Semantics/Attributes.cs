using System;

namespace Clifton.Core.Semantics
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class PublishesAttribute : Attribute
    {
        public Type PublishesType { get; protected set; }

        public PublishesAttribute(Type t)
        {
            PublishesType = t;
        }
    }
}