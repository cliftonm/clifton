using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebServerInterfaces
{
	public enum SessionState
	{
		New,
		Authenticated,
		Expired,
	}

	public enum RouteType
	{
		PublicRoute,
		AuthenticatedRoute,
		RoleRoute,					// A role route implies authentication
	}
}
