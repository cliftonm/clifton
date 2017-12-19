using System;

namespace Clifton.WebRouterService
{
    public class RouterException : Exception
    {
        public RouterException(string msg) : base(msg) { }
    }
}
