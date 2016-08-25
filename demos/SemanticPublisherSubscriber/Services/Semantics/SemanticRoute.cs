using System.Net;

namespace Semantics
{
    public class SemanticRoute : ISemanticRoute
    {
        public HttpListenerContext Context { get; set; }
    }
}
