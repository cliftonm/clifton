using System;
using System.Net;

using Clifton.Core.StateManagement;
using Clifton.WebInterfaces;

namespace Clifton.WebSessionService
{
	/// <summary>
	/// Used to pass a session state instance to the Validate handler.
	/// The SessionState includes the context and session object, from which
	/// the ServiceManager can be acquired to communicate with other services
	/// to determine session state transactions.
	/// </summary>
	public class SessionStateInstance : IStateContext
	{
		public HttpListenerContext Context { get; protected set; }
		public IWebSessionService SessionService { get; protected set; }

		public SessionStateInstance(HttpListenerContext context, IWebSessionService sessionService)
		{
			Context = context;
			SessionService = sessionService;
		}
	}
}
