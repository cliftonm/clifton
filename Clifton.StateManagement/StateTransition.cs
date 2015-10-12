using System;

namespace Clifton.StateManagement
{
	public class StateTransition
	{
		public enum BeforeTransitionReturn
		{
			AllowTransition,
			DisallowTransition,
			DeferTransition,
		}

		public Enum ToState;

		public Func<bool> PreValidate { get; set; }
		public Func<bool> PostValidate { get; set; }

		public Action OnTransition { get; set; }            // When the specific to-from state transition occurs.

		public StateTransition()
		{
			PreValidate = () => true;
			PostValidate = () => true;
			OnTransition = () => { };
		}

		public StateTransition(Func<bool> preValidate, Func<bool> postValidate)
		{
			PreValidate = preValidate;
			PostValidate = postValidate;
			OnTransition = () => { };
		}
	}
}
