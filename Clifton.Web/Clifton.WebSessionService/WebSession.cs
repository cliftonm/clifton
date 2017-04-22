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
using System.Collections.Concurrent;
using System.Net;

using Clifton.Core.ModuleManagement;
using Clifton.Core.StateManagement;
using Clifton.Core.ServiceManagement;

using Clifton.WebInterfaces;

namespace Clifton.WebSessionService
{
	public class SessionInfo : IStateContext
	{
		public ConcurrentDictionary<string, object> SessionObjectMap { get; protected set; }
		public StateManager<SessionStateInstance> StateManager { get; set; }
		public DateTime LastTransaction { get; set; }
		public int ExpiresInSeconds { get; set; }
		public SessionState CurrentState { get { return (SessionState)StateManager.CurrentState; } }

		/// <summary>
		/// By default, session expires in 5 minutes.
		/// </summary>
		public bool IsExpired { get { return (DateTime.Now - LastTransaction).TotalSeconds > ExpiresInSeconds; } }

		public SessionInfo(List<StateInfo<SessionStateInstance>> states)
		{
			SessionObjectMap = new ConcurrentDictionary<string, object>();
			StateManager = new StateManager<SessionStateInstance>();
			RegisterStates(states);
			StateManager.InitialState(SessionState.New);
			LastTransaction = DateTime.Now;
			ExpiresInSeconds = 60 * 60;
		}

		protected void RegisterStates(List<StateInfo<SessionStateInstance>> states)
		{
			states.ForEach(state =>
				{
					StateManager.StateTransitionMap.Add(state.State, state);
				});
		}
	}

    public class WebSessionModule : IModule
    {
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IWebSessionService, WebSession>();
		}
	}

	public class WebSession : ServiceBase, IWebSessionService
	{
		protected ConcurrentDictionary<IPAddress, SessionInfo> sessionInfoMap;

		// Shared across all sessions.
		protected List<StateInfo<SessionStateInstance>> states = new List<StateInfo<SessionStateInstance>>();

		public WebSession()
		{
			sessionInfoMap = new ConcurrentDictionary<IPAddress, SessionInfo>();
			InitializeStateSystem();
		}

		public virtual SessionState GetState(HttpListenerContext context)
		{
			CreateSessionIfMissing(context);

			return sessionInfoMap[context.EndpointAddress()].CurrentState;
		}

		public virtual bool IsAuthenticated(HttpListenerContext context)
		{
			CreateSessionIfMissing(context);

			return sessionInfoMap[context.EndpointAddress()].CurrentState == SessionState.Authenticated;
		}

		public virtual bool IsExpired(HttpListenerContext context)
		{
			CreateSessionIfMissing(context);

			return sessionInfoMap[context.EndpointAddress()].IsExpired;
		}

		public virtual void UpdateState(HttpListenerContext context)
		{
			CreateSessionIfMissing(context);
			SessionInfo si = sessionInfoMap[context.EndpointAddress()];

			switch (si.CurrentState)
			{
				case SessionState.Authenticated:
					if (si.IsExpired)
					{
						si.StateManager.ToState(SessionState.Expired, new SessionStateInstance(context, this));
					}

					break;
			}
		}

		public virtual void Authenticate(HttpListenerContext context)
		{
			CreateSessionIfMissing(context);
			SessionInfo si = sessionInfoMap[context.EndpointAddress()];
			si.StateManager.ToState(SessionState.Authenticated, new SessionStateInstance(context, this));
		}

		public virtual void Logout(HttpListenerContext context)
		{
			CreateSessionIfMissing(context);
			SessionInfo si = sessionInfoMap[context.EndpointAddress()];
			si.StateManager.ToState(SessionState.New, new SessionStateInstance(context, this));
		}

		/// <summary>
		/// A return value of null indicates that the object doesn't exist in the collection.
		/// </summary>
		public virtual T GetSessionObject<T>(HttpListenerContext context, string objectName)
		{
			T ret = default(T);
			ConcurrentDictionary<string, object> sessionObjects = CreateSessionIfMissing(context);
			object val;

			if (sessionObjects.TryGetValue(objectName, out val))
			{
				ret = (T)Convert.ChangeType(val, typeof(T));
			}

			return ret;
		}

		public virtual string GetSessionObject(HttpListenerContext context, string objectName)
		{
			string ret = "";
			ConcurrentDictionary<string, object> sessionObjects = CreateSessionIfMissing(context);
			object val;

			if (sessionObjects.TryGetValue(objectName, out val))
			{
				ret = (val == null ? "" : val.ToString());
			}

			return ret;
		}

		public virtual dynamic GetSessionObjectAsDynamic(HttpListenerContext context, string objectName)
		{
			dynamic ret = null;
			ConcurrentDictionary<string, object> sessionObjects = CreateSessionIfMissing(context);
			object val;

			if (sessionObjects.TryGetValue(objectName, out val))
			{
				ret = val;
			}

			return ret;
		}

		public virtual void SetSessionObject(HttpListenerContext context, string objectName, object val)
		{
			ConcurrentDictionary<string, object> sessionObjects = CreateSessionIfMissing(context);
			sessionObjects[objectName] = val;
		}

		public virtual void RemoveSessionObject(HttpListenerContext context, string objectName)
		{
			ConcurrentDictionary<string, object> sessionObjects = CreateSessionIfMissing(context);
			object val;		// not used.
			sessionObjects.TryRemove(objectName, out val);
		}

		public virtual void ClearSession(SessionStateInstance sessionState)
		{
			SessionInfo sessionInfo;
			sessionInfoMap.TryRemove(sessionState.Context.EndpointAddress(), out sessionInfo);
		}

		protected ConcurrentDictionary<string, object> CreateSessionIfMissing(HttpListenerContext context)
		{
			SessionInfo sessionInfo;
			IPAddress addr = context.EndpointAddress();

			if (!sessionInfoMap.TryGetValue(addr, out sessionInfo))
			{
				sessionInfo = new SessionInfo(states);
				sessionInfoMap[addr] = sessionInfo;
			}

			return sessionInfo.SessionObjectMap;
		}

		/// <summary>
		/// Implement three states and their transitions: new (not authenticated), authenticated, and expired (also not authenticated.)
		/// </summary>
		protected virtual void InitializeStateSystem()
		{
			states = new List<StateInfo<SessionStateInstance>>()
			{
				new StateInfo<SessionStateInstance>() 
				{
					State=SessionState.New,
					OnEnter = s => s.SessionService.SetSessionObject(s.Context, "Authenticated", "false"),
					StateTransitions=new List<StateTransition>()
					{
						// A new session can move to authenticated.
						new StateTransition()
						{
							ToState=SessionState.Authenticated
						},
					}
				},
				new StateInfo<SessionStateInstance>()
				{
					State=SessionState.Authenticated,
					OnEnter = s => s.SessionService.SetSessionObject(s.Context, "Authenticated", "true"),
					StateTransitions=new List<StateTransition>()
					{
						// An authenticated session can transition to expired, or if logging out, new.
						new StateTransition()
						{
							ToState=SessionState.New
						},
						new StateTransition()
						{
							ToState=SessionState.Expired
						}
					}
				},
				new StateInfo<SessionStateInstance>()
				{
					State=SessionState.Expired,
					OnEnter = sessionState => ClearSession(sessionState),
					StateTransitions=new List<StateTransition>()
					{
						// An expired session can transition to new (a login page for example) or authenticated, if authentication provided by some other means (a login web service)
						new StateTransition()
						{
							ToState=SessionState.New
						},
						new StateTransition()
						{
							ToState=SessionState.Authenticated
						}
					}
				}
			};
		}
	}
}
