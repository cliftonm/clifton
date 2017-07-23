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
using System.Web.SessionState;

using Clifton.Core.ModuleManagement;
using Clifton.Core.StateManagement;
using Clifton.Core.ServiceManagement;

using Clifton.WebInterfaces;

namespace Clifton.WebSessionService
{
	public static class IISExtensionMethods
	{
		public static bool TryGetValue<T>(this HttpSessionState session, string key, out T val)
		{
			bool ret = false;
			val = default(T);
			object obj = session[key];

			if (obj != null)
			{
				val = (T)obj;
				ret = true;
			}

			return ret;
		}
	}

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
		// Shared across all sessions.
		// TODO: Get Session working for IIS.  Workaround for right now is we've made these static, but I'm not convinced that that is a robust solution.
		private ConcurrentDictionary<IPAddress, SessionInfo> sessionInfoMap;
		private List<StateInfo<SessionStateInstance>> states = new List<StateInfo<SessionStateInstance>>();
		private const string SESSION_INFO = "_SessionInfo_";

		public WebSession()
		{
			sessionInfoMap = new ConcurrentDictionary<IPAddress, SessionInfo>();
			InitializeStateSystem();
		}

		public virtual SessionState GetState(IContext context)
		{
			SessionInfo si = CreateSessionInfoIfMissing(context);

			return si.CurrentState;
		}

		public virtual bool IsAuthenticated(IContext context)
		{
			SessionInfo si = CreateSessionInfoIfMissing(context);

			return si.CurrentState == SessionState.Authenticated;
		}

		public virtual bool IsExpired(IContext context)
		{
			SessionInfo si = CreateSessionInfoIfMissing(context);

			return si.IsExpired;
		}

		public virtual void UpdateState(IContext context)
		{
			SessionInfo si = CreateSessionInfoIfMissing(context);

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

		public virtual void UpdateLastTransaction(IContext context)
		{
			SessionInfo si = CreateSessionInfoIfMissing(context);
			si.LastTransaction = DateTime.Now;
		}

		public virtual void Authenticate(IContext context)
		{
			SessionInfo si = CreateSessionInfoIfMissing(context);
			si.StateManager.ToState(SessionState.Authenticated, new SessionStateInstance(context, this));
		}

		public virtual void Logout(IContext context)
		{
			SessionInfo si = CreateSessionInfoIfMissing(context);
			si.StateManager.ToState(SessionState.New, new SessionStateInstance(context, this));
		}

		/// <summary>
		/// A return value of null indicates that the object doesn't exist in the collection.
		/// </summary>
		public virtual T GetSessionObject<T>(IContext context, string objectName)
		{
			T ret = default(T);
			SessionInfo si = CreateSessionInfoIfMissing(context);
			object val;

			if (si.SessionObjectMap.TryGetValue(objectName, out val))
			{
				ret = (T)Convert.ChangeType(val, typeof(T));
			}

			return ret;
		}

		public virtual string GetSessionObject(IContext context, string objectName)
		{
			string ret = "";
			SessionInfo si = CreateSessionInfoIfMissing(context);
			object val;

			if (si.SessionObjectMap.TryGetValue(objectName, out val))
			{
				ret = (val == null ? "" : val.ToString());
			}

			return ret;
		}

		public virtual dynamic GetSessionObjectAsDynamic(IContext context, string objectName)
		{
			dynamic ret = null;
			SessionInfo si = CreateSessionInfoIfMissing(context);
			object val;

			if (si.SessionObjectMap.TryGetValue(objectName, out val))
			{
				ret = val;
			}

			return ret;
		}

		public virtual void SetSessionObject(IContext context, string objectName, object val)
		{
			SessionInfo si = CreateSessionInfoIfMissing(context);
			si.SessionObjectMap[objectName] = val;
		}

		public virtual void RemoveSessionObject(IContext context, string objectName)
		{
			SessionInfo si = CreateSessionInfoIfMissing(context);
			object val;     // not used.
			si.SessionObjectMap.TryRemove(objectName, out val);
		}

		public virtual void ClearSession(SessionStateInstance sessionState)
		{
			SessionInfo sessionInfo;
			sessionInfoMap.TryRemove(sessionState.Context.EndpointAddress(), out sessionInfo);
		}

		protected SessionInfo CreateSessionInfoIfMissing(IContext context)
		{
			SessionInfo sessionInfo;

			// if (context is HttpListenerContextWrapper)
			//if (true) 
			//{
				IPAddress addr = context.EndpointAddress();

				if (!sessionInfoMap.TryGetValue(addr, out sessionInfo))
				{
					sessionInfo = new SessionInfo(states);
					sessionInfoMap[addr] = sessionInfo;
				}
			//}
			//else
			//{
			//	if (!context.Session.TryGetValue(SESSION_INFO, out sessionInfo))
			//	{
			//		sessionInfo = new SessionInfo(states);
			//		context.Session[SESSION_INFO] = sessionInfo;
			//	}
			//}

			return sessionInfo;
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
