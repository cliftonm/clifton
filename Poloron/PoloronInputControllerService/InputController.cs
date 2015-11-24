using System.Windows.Forms;

using Clifton.ServiceInterfaces;
using Clifton.SemanticProcessorInterfaces;
using Clifton.Semantics;

using PoloronInterfaces;

namespace InputControllerService
{
	public class InputControllerModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IPoloronInputController, InputController>();
		}
	}

	public class InputController : ServiceBase, IPoloronInputController
	{
		protected ISemanticProcessor semProc;
		protected MouseButtons buttonState;

		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			semProc = ServiceManager.Get<ISemanticProcessor>();
		}

		public void Initialize(Control control)
		{
			control.MouseDown += OnMouseDown;
			control.MouseUp += OnMouseUp;
		}

		protected void OnMouseDown(object sender, MouseEventArgs e)
		{
			buttonState |= e.Button;
			BroadcastAction(buttonState);
		}

		protected void OnMouseUp(object sender, MouseEventArgs e)
		{
			buttonState &= ~e.Button;
			BroadcastAction(buttonState);
		}

		protected void BroadcastAction(MouseButtons state)
		{
			if ((state & (MouseButtons.Left | MouseButtons.Right)) == (MouseButtons.Left | MouseButtons.Right))
			{
				semProc.ProcessInstance<ControllerMembrane, PolarizeChargeUp>();
			}
			else if ((state & MouseButtons.Left) == MouseButtons.Left)
			{
				semProc.ProcessInstance<ControllerMembrane, PolarizeNegative>();
			}
			else if ((state & MouseButtons.Right) == MouseButtons.Right)
			{
				semProc.ProcessInstance<ControllerMembrane, PolarizePositive>();
			}
			else
			{
				semProc.ProcessInstance<ControllerMembrane, PolarizeNeutral>();
			}
		}
	}
}
