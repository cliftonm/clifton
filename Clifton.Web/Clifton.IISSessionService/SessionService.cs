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
using System.Linq;
using System.Web.SessionState;

using Clifton.Core.ModuleManagement;
using Clifton.Core.StateManagement;
using Clifton.Core.ServiceManagement;

using Clifton.WebInterfaces;
using Clifton.WebSessionService;

namespace Clifton.IISSessionService
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

	public class IISSessionModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IWebSessionService, IISSession>();
		}
	}

	public class IISSession : ServiceBase, IWebSessionService
	{
		// Shared across all sessions.
		protected List<StateInfo<SessionStateInstance>> states = new List<StateInfo<SessionStateInstance>>();
		private const string SESSION_INFO = "_SessionInfo_";

		public IISSession()
		{
			// sessionInfoMap = new ConcurrentDictionary<IPAddress, SessionInfo>();
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

		protected void ClearSession(IContext context)
		{
			SessionInfo si = CreateSessionInfoIfMissing(context);
			List<string> keys = si.SessionObjectMap.Keys.ToList();
			object val;			// not used.
			keys.ForEach(key => si.SessionObjectMap.TryRemove(key, out val));
		}

		protected SessionInfo CreateSessionInfoIfMissing(IContext context)
		{
			SessionInfo sessionInfo;

			if (!context.Session.TryGetValue(SESSION_INFO, out sessionInfo))
			{
				sessionInfo = new SessionInfo(states);
				context.Session[SESSION_INFO] = sessionInfo;
			}

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
					OnEnter = sessionState => ClearSession(sessionState.Context),
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
