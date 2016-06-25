/* The MIT License (MIT)
* 
* Copyright (c) 2015 Marc Clifton
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;

using Clifton.Core.ExtensionMethods;

namespace Clifton.Core.StateManagement
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
		public Dictionary<Enum, StateInfo<T>> StateTransitionMap { get { return stateTransitionMap; } }

		public StateManager()
		{
			stateTransitionMap = new Dictionary<Enum, StateInfo<T>>();
		}

		/// <summary>
		/// Initialize the state without events firing.
		/// </summary>
		public void InitialState(Enum state)
		{
			this.state = state;
		}

		/// <summary>
		/// We allow the current state to be explicitly set, without testing for allowed transitions.
		/// </summary>
		public void SetState(Enum state, T context)
		{
			stateTransitionMap[CurrentState].OnLeave(context);
			this.state = state;
			StateChange.Fire(this, EventArgs.Empty);
			stateTransitionMap[CurrentState].OnEnter(context);
		}

		/// <summary>
		/// The caller normally uses ToState to transition from the currrent state to the next desired state.
		/// This call will throw an exception if transition validation fails.
		/// </summary>
		public void ToState(Enum state, T context)
		{
			if (CanTransition(state))
			{
				SetState(state, context);
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
		public void ToNextAllowedState(T context)
		{
			var transitions = stateTransitionMap[CurrentState].StateTransitions.Where(t => t.Validate()).ToList();

			switch(transitions.Count())
			{
				case 0:
					throw new StateManagerNoQualifiedStateException("There is no qualified next allowed state from " + CurrentStateName);

				case 1:
					SetState(transitions[0].ToState, context);
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
