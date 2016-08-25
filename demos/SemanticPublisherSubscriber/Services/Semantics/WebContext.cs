using System.Net;

using Clifton.Core.Semantics;

namespace Semantics
{
    public class WebContext : ISemanticType
    {
        public HttpListenerContext Context { get; set; }
    }
}
