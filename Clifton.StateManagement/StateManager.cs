using System;
using System.Collections.Generic;
using System.Linq;

using Clifton.ExtensionMethods;

namespace Clifton.StateManagement
{
	public class StateManagerException : ApplicationException
	{
		public StateManagerException(string msg)
			: base(msg)
		{
		}
	}

	public class StateManagerTransitionNotAllowedException : StateManagerException
	{
		public StateManagerTransitionNotAllowedException(string msg)
			: base(msg)
		{
		}
	}

	public class StateManagerNoQualifiedStateException : StateManagerException
	{
		public StateManagerNoQualifiedStateException(string msg)
			: base(msg)
		{
		}
	}

	public class StateManagerMoreThanOneQualifiedStateException : StateManagerException
	{
		public StateManagerMoreThanOneQualifiedStateException(string msg)
			: base(msg)
		{
		}
	}

	public class StateManager<T> where T : IStateContext
	{
		public event EventHandler<EventArgs> StateChange;

		protected Enum state;
		protected Dictionary<Enum, StateInfo<T>> stateTransitionMap;

		// For automation purposes:
		// public Page CurrentPage { get { return currentPage; } }

		public Enum CurrentState { get { return state; } }
		public string CurrentStateName { get { return CurrentState.ToString(); } }

		public StateManager()
		{
			stateTransitionMap = new Dictionary<Enum, StateInfo<T>>();
		}

		/// <summary>
		/// We allow the current state to be explicitly set, without testing for allowed transitions.
		/// </summary>
		public void SetState(Enum state)
		{
			this.state = state;
			StateChange.Fire(this, EventArgs.Empty);
		}

		/// <summary>
		/// The caller normally uses ToState to transition from the currrent state to the next desired state.
		/// This call will throw an exception if transition validation fails.
		/// </summary>
		public void ToState(Enum state)
		{
			if (CanTransition(state))
			{
				SetState(state);
			}
			else
			{
				throw new StateManagerTransitionNotAllowedException("Transition from " + CurrentStateName + " to state " + state.ToString() + " is currently not permitted.");
			}
		}

		/// <summary>
		/// This function allows us to change state based on transition validation, in which one an only one "to state" is a valid transition.
		/// This simplifies the application logic when a specific state change is qualified by the application context.
		/// If no transitions validate, or more than one transition validates, an exception is thrown.
		/// </summary>
		public void ToNextAllowedState()
		{
			var transitions = stateTransitionMap[CurrentState].StateTransitions.Where(t => t.Validate()).ToList();

			switch(transitions.Count())
			{
				case 0:
					throw new StateManagerNoQualifiedStateException("There is no qualified next allowed state from " + CurrentStateName);

				case 1:
					SetState(transitions[0].ToState);
					break;

				default:
					throw new StateManagerMoreThanOneQualifiedStateException("There are multiple qualified next allowed state from " + CurrentStateName);
			}
		}

		/// <summary>
		/// Returns true if the "to state" validates.
		/// </summary>
		public bool CanTransition(Enum toState)
		{
			var transitions = stateTransitionMap[CurrentState].StateTransitions.Where(t => t.Validate()).ToList();

			return transitions.Count > 0; 
		}
	}
}
