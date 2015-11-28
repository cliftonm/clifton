using System;

namespace Clifton.WebInterfaces
{
	public class RouteInfo
	{
		public Type ReceptorSemanticType { get; protected set; }
		public RouteType RouteType { get; protected set; }
		public uint RoleMask { get; protected set; }

		/// <summary>
		/// By default, the role mask is 0: no role.
		/// The application determines how the uint bits determine role permissions.
		/// Any bits that are set with a binary "and" of the route's role mask and the current role passes the authorization test.
		/// </summary>
		public RouteInfo(Type receptorSemanticType, RouteType routeType = RouteType.PublicRoute, uint roleMask = 0)
		{
			ReceptorSemanticType = receptorSemanticType;
			RouteType = routeType;
			RoleMask = roleMask;
		}
	}
}
