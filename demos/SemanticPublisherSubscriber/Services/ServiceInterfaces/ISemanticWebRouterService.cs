using Clifton.Core.ServiceManagement;

using Semantics;

namespace ServiceInterfaces
{
    public interface ISemanticWebRouterService : IService
    {
        void Register<T>(string verb, string path) where T : ISemanticRoute;
        void RouteRequest(ST_HttpRequest req);
    }
}
