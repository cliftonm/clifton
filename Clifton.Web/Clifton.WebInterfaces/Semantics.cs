using Clifton.Core.Semantics;

namespace Clifton.WebInterfaces
{
	public class SocketMembrane : Membrane { }

	public class ServerSocketMessage : ISemanticType
	{
		public IWebSocketSession Session { get; set; }
		public string Text { get; set; }
	}

	public class ClientSocketMessage : ISemanticType
	{
		public string Text { get; set; }
	}
}