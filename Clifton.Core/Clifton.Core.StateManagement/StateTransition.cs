using System;

namespace Clifton.Core.StateManagement
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

		/// <summary>
		/// Function that returns true if the transition to the ToState is valid given an application-specific requirement.
		/// </summary>
		public Func<bool> Validate { get; set; }

		public Action OnTransition { get; set; }            // When the specific to-from state transition occurs.

		public StateTransition()
		{
			Validate = () => true;
			OnTransition = () => { };
		}

		public StateTransition(Func<bool> validate)
		{
			Validate = validate;
			OnTransition = () => { };
		}
	}
}
