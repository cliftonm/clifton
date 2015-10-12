using System;
using System.Collections.Generic;
using System.Linq;

namespace Clifton.StateManagement
{
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

		public bool ToNextAllowedState()
		{
			bool ret = false;
			var transitions = stateTransitionMap[CurrentState].StateTransitions.Where(t => t.PostValidate()).ToList();

			if (transitions.Count==1)
			{
				ToState(transitions[0].ToState);
				ret = true;
			}

			return ret;
		}

		public bool CanTransition(Enum toState)
		{
			bool ret = false;

			var transitions = stateTransitionMap[CurrentState].StateTransitions.Where(t => t.PreValidate()).ToList();
			ret = transitions.Count > 0;

			return ret;
		}

		public bool PostValidation(State toState)
		{
			bool ret = false;

			if (toState == State.ToNextAllowedState)
			{
				ret = stateTransitionMap[CurrentState].Transitions.Where(t => t.PostValidate()).ToList().Count == 1;
			}
			else
			{
				try
				{
					Transition t = GetTransition(CurrentState, toState);
					ret = t.PostValidate();
				}
				catch
				{
					//TODO: how can this be prevented in production? What should we do in production?
					string msg = "Unknown transition from state " + CurrentState.ToString() + " to state " + toState.ToString();
					Debug.WriteLine("!sm:" + msg);
					throw;
				}
			}

			return ret;
		}

		/// <summary>
		/// Performs the transition to the deferred next state.
		/// The caller can override the deferred state so that a different state is used.
		/// </summary>
		public void PerformDeferredTransition(State? toThisState = null)
		{
			if (toThisState != null)
			{
				deferredState = (State)toThisState;
			}
			Debug.WriteLine("!sm:PerformDeferredTransition calling PerformTransition for {0}", deferredState);
			PerformTransition(deferredState, false);
			deferredState = State.None;
		}

		/// <summary>
		/// There's no real reason to ever call this, here for semantic completion for the operation.
		/// </summary>
		public void CancelDeferredTransition()
		{
			deferredState = State.None;
		}

		/// <summary>
		/// Execute any associated action for transitioning from the current state to the new state.
		/// </summary>
		/// <param name="newState"></param>
		protected void ExecuteTransitionAction(State newState)
		{
			try
			{
				GetTransition(CurrentState, newState).OnTransition();
			}
			catch
			{
				// Allow this exception if taking the kiosk down or resurrecting it.
				if ((newState != State.AtmDown) && (newState != IdleState) && (newState != State.CommunicationFailure))
				{
					string msg = "Unknown transition from state " + CurrentState.ToString() + " to state " + newState.ToString();
					Debug.WriteLine("!sm:" + msg);
					// AN EXCEPTION WILL OCCUR WHEN BOUNCING AROUND SCREENS WHEN USING THE JUMP-TO UI
					// or when doing automation testing.
					// TODO: Can we get this to know whether automation testing or JumpTo is happening so we can actually throw an exception?
				}
			}
		}

		/// <summary>
		/// Internal transition to the new state.
		/// No checking is done here, as the caller should have done all necessary validation / verification before calling here.
		/// </summary>
		protected void NavToState(State newState, bool goingBack = false)
		{
			// Only run BeforeTransition if we're not popping the state stack.
			if (!goingBack)
			{
				Transition.BeforeTransitionReturn ret = Transition.BeforeTransitionReturn.AllowTransition;

				if (TransitionDefined(CurrentState, newState))
				{
					deferredState = newState;           // Possible deferred state...
					Debug.WriteLine("!sm:NavToState calling BeforeTransition for {0} to {1}", CurrentState, newState);
					ret = GetTransition(CurrentState, newState).BeforeTransition();
				}

				Debug.WriteLine("!sm:NavToState {0} to {1}: We {2}", CurrentState, newState, ret.ToString());

				switch (ret)
				{
					case Transition.BeforeTransitionReturn.DisallowTransition:
						deferredState = State.None;
						break;

					case Transition.BeforeTransitionReturn.DeferTransition:
						break;

					case Transition.BeforeTransitionReturn.AllowTransition:
						deferredState = State.None;
						PerformTransition(newState, goingBack);
						break;
				}
			}
			else
			{
				PerformTransition(newState, goingBack);
			}
		}

		protected void PerformTransition(State newState, bool goingBack)
		{
			stateTransitionMap[CurrentState].OnLeave();

			// Execute the transition only if we're not going back to a previous state.
			if (!goingBack)
			{
				ExecuteTransitionAction(newState);
			}

			// Set new state before we navigate so that any UI dependent pieces know the new state
			// when the page is created.
			CurrentState = newState;
			string typeName = GetEnumDescription(newState);
			// Call the app to do app-specific handling of the transition (usually UI stuff)
			PerformTransitionCallback.Fire(this, new PerformTransitionEventArgs() { NewState = CurrentState, TypeName = typeName });

			stateTransitionMap[CurrentState].OnEnter();
		}

		/// <summary>
		/// Return the description, which in this case is the Page type.
		/// </summary>
		protected string GetEnumDescription(State enumValue)
		{
			FieldInfo fi = enumValue.GetType().GetField(enumValue.ToString());
			Attribute attr = fi.GetCustomAttribute(typeof(DescriptionAttribute), false);
			string descr = ((DescriptionAttribute)attr).Description;

			return descr;
		}

		/// <summary>
		/// Ensure that page types exist for each state's descriptor.
		/// </summary>
		protected void ValidateStatePages()
		{
			StringBuilder sb = new StringBuilder();

			Enum.GetValues(typeof(State)).ForEach<State>(s =>
			{
				string pageName = GetEnumDescription(s);
				(pageName == "*").Else(() =>
				{
					Type.GetType(pageName).IfNull(() =>
					{
						sb.AppendLine("Missing page for state " + s.ToString() + " with page name descriptor '" + pageName + "'");
					});
				});
			});

			Debug.WriteLine("!sm:" + sb);
		}

		/// <summary>
		/// Verify we have enums for CKiosk.Page pages.
		/// </summary>
		protected void ValidatePageEnumImplementations()
		{
			StringBuilder sb = new StringBuilder();

			var types = from t in Assembly.GetExecutingAssembly().GetTypes()
						where t.IsClass && t.Namespace == "CKiosk.Pages"
						select t;

			types.ForEach(t =>
			{
				Enum.GetValues(typeof(State)).Contains<State>(s => GetEnumDescription(s) == t.FullName).Else(() =>
				{
					sb.AppendLine("Missing state for page " + t.FullName);
				});
			});

			Debug.WriteLine("!sm:" + sb);
		}

		/// <summary>
		/// Verify we have "from" states for each enum.
		/// </summary>
		protected void ValidateEnumStateImplementations()
		{
			StringBuilder sb = new StringBuilder();

			Enum.GetValues(typeof(State)).ForEach<State>(s =>
			{
				if (!stateTransitionMap.ContainsKey(s))
				{
					sb.AppendLine("Missing state implementation for " + s.ToString());
				}
			});

			Debug.WriteLine("!sm:" + sb);
		}

		/// <summary>
		/// Recursive function to check if there are state transistions between two states.
		/// </summary>
		private int level = 0;

		protected bool ValidateGetToEnd(State fromState, State endState, StringBuilder sb, Dictionary<State, object> stack, Dictionary<State, object> knownGood)
		{
			bool ans = true;
			string lstr = new string(' ', level);
			StateInfo stateInfo;
			sb.AppendLine(string.Format("{0}Validate From {1}", lstr, fromState));

			if (stateTransitionMap.TryGetValue(fromState, out stateInfo))
			{
				if ((stateInfo != null) && (stateInfo.Transitions != null))
				{
					//can you get from this state to the end state (SelectLanguage)?
					foreach (Transition t in stateInfo.Transitions)
					{
						if (t.ToState == endState)
						{
							//we made it to the end, try the next transition
							sb.AppendLine(string.Format("{0}Finish! {1}", lstr, t.ToState));
							continue;
						}
						if (knownGood.ContainsKey(t.ToState))
						{
							//we have already verified this state from our own history, so we don't need to check any further.
							//sb.AppendLine(string.Format("{0}Already Verified {1}", lstr, t.ToState));
							continue;
						}
						if (stack.ContainsKey(t.ToState))
						{
							//we are looping back to the same state from our own history, so we don't need to check any further.
							sb.AppendLine(string.Format("{0}Looping to {1}", lstr, t.ToState));
							continue;
						}

						//need to look more, save the state so we know we have been here
						stack.Add(t.ToState, null);
						level++;

						if (ValidateGetToEnd(t.ToState, endState, sb, stack, knownGood))
						{
							//made it!
							sb.AppendLine(string.Format("{0}Returning from {1}", lstr, t.ToState));
						}
						else
						{
							sb.AppendLine(string.Format("{0}Failed at {1}", lstr, t.ToState));
							ans = false;
						}
						stack.Remove(t.ToState);
						level--;
					}
				}

				if (ans)
				{
					//all paths fromState have been checked, so put it in the known good state list
					knownGood.Add(fromState, null);
				}
				//return the answer
				return ans;
			}

			sb.AppendLine(string.Format("Failed to find transition from state {0} to state {1} in map.", fromState, endState));

			return false;
		}

		/// <summary>
		/// Walk the state map to make sure from the starting page, a valid end page is available.
		/// </summary>
		/// <returns></returns>
		protected bool ValidateStateMap()
		{
			bool success = true;
			StringBuilder sb = new StringBuilder();
			Dictionary<State, object> stack = new Dictionary<State, object>();
			State startState = IdleState;
			Dictionary<State, object> knownGood = new Dictionary<State, object>();

			if (ValidateGetToEnd(startState, startState, sb, stack, knownGood))
			{
				sb.AppendLine("Success!");
			}
			else
			{
				sb.AppendLine("*****FAILED******");
				success = false;
			}

			Debug.WriteLine("!sm:" + sb);
			return success;
		}
	}
}
