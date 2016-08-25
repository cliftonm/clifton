using System.Net;

using Clifton.Core.Semantics;

namespace Semantics
{
    public class ST_HttpRequest : WebContext
    {
        public string Verb { get; set; }
        public string Path { get; set; }
        public string Data { get; set; }
    }
}
