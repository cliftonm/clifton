namespace Clifton.WebInterfaces
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