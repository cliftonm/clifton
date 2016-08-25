using System.Net;

using Clifton.Core.Semantics;

namespace Semantics
{
    public interface ISemanticRoute : ISemanticType
    {
        HttpListenerContext Context { get; set; }
    }
}
