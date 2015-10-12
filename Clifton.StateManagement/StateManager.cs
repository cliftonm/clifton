using System;
using System.Collections.Generic;
using System.Linq;

namespace Clifton.StateManagement
{
	public class StateManagerException : ApplicationException
	{
		public StateManagerException(string msg)
			: base(msg)
		{
		}
	}

	public class StateManager<T>
	{
		public event EventHandler<EventArgs> StateChange;

		protected Enum state;
		protected Dictionary<Enum, StateInfo<T>> stateTransitionMap;

		// For automation purposes:
		// public Page CurrentPage { get { return currentPage; } }

		public Enum CurrentState {get {return state;}}

		public StateManager()
		{
			stateTransitionMap = new Dictionary<Enum, StateInfo<T>>();
		}

		public void SetState(Enum state)
		{
			this.state = state;
		}

		public void ToState(Enum toState)
		{
		}

		public void ToNextAllowedState()
		{
			bool ret = false;
			var transitions = stateTransitionMap[CurrentState].StateTransitions.Where(t => t.Validate()).ToList();

			if (transitions.Count == 1)
			{
				ToState(transitions[0].ToState);
			}
			else
			{
				throw new StateManagerException("There is no qualified next allowed state from " + CurrentState.ToString());
			}
		}

		public bool CanTransition(Enum toState)
		{
			bool ret = false;

			var transitions = stateTransitionMap[CurrentState].StateTransitions.Where(t => t.Validate()).ToList();
			ret = transitions.Count > 0;

			return ret;
		}

	}
}
