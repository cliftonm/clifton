using System;


namespace Clifton.Core.Semantics
{
	public class XmlFileName : ImmutableSemanticType<XmlFileName, string> { };
	public class FullPath : ImmutableSemanticType<FullPath, string> { };
	public class AssemblyFileName : ImmutableSemanticType<AssemblyFileName, string> { }

	public class ConnectionString : ImmutableSemanticType<ConnectionString, string> { }
	public class UserName : ImmutableSemanticType<UserName, string> { }
	public class HashedPassword : ImmutableSemanticType<HashedPassword, string> { }
	public class PlainTextPassword : ImmutableSemanticType<PlainTextPassword, string> { }
	public class UserId : ImmutableSemanticType<UserId, int> { }
	public class ViewName : ImmutableSemanticType<ViewName, string> { }
	public class WhereClause : ImmutableSemanticType<WhereClause, string> { }
}
