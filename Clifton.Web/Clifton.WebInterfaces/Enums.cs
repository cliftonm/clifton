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

	public enum Role
	{
		SuperAdmin = 128,
		Admin = 64,
		User = 1,
		None = 0,
	}
}