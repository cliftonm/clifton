using Clifton.Semantics;
using Clifton.SemanticProcessorInterfaces;

namespace PoloronInterfaces
{
	public class PoloronId : ImmutableSemanticType<PoloronId, int> { };

	public class ControllerMembrane : Membrane { }

	public class PolarizeNegative : ISemanticType { }
	public class PolarizePositive : ISemanticType { }
	public class PolarizeNeutral : ISemanticType { }
	public class PolarizeChargeUp : ISemanticType { }

	//public class XPos : ImmutableSemanticType<XPos, int> { };
	//public class YPos : ImmutableSemanticType<YPos, int> { };
}
