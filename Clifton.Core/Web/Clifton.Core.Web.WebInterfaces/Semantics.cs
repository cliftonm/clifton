using WebSocketSharp;
using WebSocketSharp.Server;

using Clifton.Core.Semantics;

namespace Clifton.Core.Web.WebInterfaces
{
	public class SocketMembrane : Membrane { }

	public class ServerSocketMessage : ISemanticType
	{
		public WebSocketBehavior Session { get; set; }
		public string Text { get; set; }
	}

	public class ClientSocketMessage : ISemanticType
	{
		public string Text { get; set; }
	}
}