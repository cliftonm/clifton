using System;

using Clifton.Semantics;

namespace Clifton.CoreSemanticTypes
{
	public class XmlFileName : ImmutableSemanticType<XmlFileName, string> { };
	public class FullPath : ImmutableSemanticType<FullPath, string> { };
	public class AssemblyFileName : ImmutableSemanticType<AssemblyFileName, string> { }
}
