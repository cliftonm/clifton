using Clifton.ServiceInterfaces;
using Clifton.SemanticProcessorInterfaces;
using Clifton.Semantics;

using PoloronInterfaces;

namespace PoloronGame
{
	static partial class Program
	{
		public static void InitializeController()
		{
			ISemanticProcessor semProc = serviceManager.Get<ISemanticProcessor>();
			semProc.Register<ControllerMembrane, ControllerReceptor>();
		}
	}

	public class ControllerReceptor : IReceptor
	{
		public void Process(ISemanticProcessor proc, IMembrane membrane, PolarizeChargeUp st)
		{
			Program.renderer.SetState(PoloronId.Create(0), PoloronState.Charging);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, PolarizeNegative st)
		{
			Program.renderer.SetState(PoloronId.Create(0), PoloronState.Negative);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, PolarizePositive st)
		{
			Program.renderer.SetState(PoloronId.Create(0), PoloronState.Positive);
		}

		public void Process(ISemanticProcessor proc, IMembrane membrane, PolarizeNeutral st)
		{
			Program.renderer.SetState(PoloronId.Create(0), PoloronState.Neutral);
		}
	}
}
