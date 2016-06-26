using Clifton.Core.Semantics;

namespace Clifton.Core.Semantics
{
	public class XmlFileName : ImmutableSemanticType<XmlFileName, string> { };
	public class OptionalPath : ImmutableSemanticType<OptionalPath, string> { };
	public class FullPath : ImmutableSemanticType<FullPath, string> { };
	public class AssemblyFileName : ImmutableSemanticType<AssemblyFileName, string> { }
}
