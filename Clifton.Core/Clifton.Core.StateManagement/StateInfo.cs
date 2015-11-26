using System;
using System.Collections.Generic;

namespace Clifton.Core.StateManagement
{
	public class StateInfo<T> where T : IStateContext
	{
		public Enum State { get; set; }
		public Action<T> OnEnter { get; set; }
		public Action<T> OnLeave { get; set; }
		public List<StateTransition> StateTransitions { get; set; }

		public StateInfo()
		{
			OnEnter = (_) => { };
			OnLeave = (_) => { };
			StateTransitions = new List<StateTransition>();
		}
	}
}
