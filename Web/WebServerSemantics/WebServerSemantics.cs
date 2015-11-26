using System;
using System.Net;
using System.Net.Sockets;

using Clifton.SemanticProcessorInterfaces;
using Clifton.SemanticProcessorService;
using Clifton.Semantics;

namespace WebServerSemantics
{
	public class UriPath : ImmutableSemanticType<UriPath, string> { }
	public class UriExtension : ImmutableSemanticType<UriExtension, string> { }
	public class HttpVerb : ImmutableSemanticType<HttpVerb, string> { }

	public class SocketMembrane : Membrane { }
	public class WebServerMembrane : Membrane { }

	/// <summary>
	/// A route before it has been semantic-ized.
	/// </summary>
	public class Route : ISemanticType
	{
		public HttpListenerContext Context { get; set; }
	}

	public class SocketPacket : ISemanticType
	{
		public Socket Socket { get; set; }
		public byte[] Packet { get; set; }
	}

	/// <summary>
	/// Must be derived from so that a semantic route is one tied to a specific class type,
	/// usually implementing the properties specific to the route, for example, a deserialized
	/// Json package and/or URL parameters.
	/// </summary>
	public abstract class SemanticRoute : ISemanticType
	{
		public HttpListenerContext Context { get; set; }
		public string PostData { get; set; }
	}

	/// <summary>
	/// Semantic type for file data, like HTML and JS files.
	/// </summary>
	public class HtmlPath : SemanticRoute { }
	public class JavascriptPath : SemanticRoute { }
	public class CssPath : SemanticRoute { }
	public class HtmlPageRoute : SemanticRoute { }

	public class UnhandledContext : ISemanticType
	{
		public HttpListenerContext Context { get; set; }
	}

	public class ExceptionResponse : ISemanticType
	{
		public HttpListenerContext Context { get; set; }
		public Exception Exception { get; set; }
	}

	public class StringResponse : ISemanticType
	{
		public HttpListenerContext Context { get; set; }
		public int StatusCode { get; set; }
		public string Message { get; set; }
	}

	public class JsonResponse : ISemanticType
	{
		public HttpListenerContext Context { get; set; }
		public int StatusCode { get; set; }
		public string Json { get; set; }
	}

	public class ImageResponse : ISemanticType
	{
		public HttpListenerContext Context { get; set; }
		public string ContentType { get; set; }
		public byte[] BinaryData { get; set; }
	}

	public class DataResponse : ISemanticType
	{
		public HttpListenerContext Context { get; set; }
		public int StatusCode { get; set; }
		public byte[] Data { get; set; }
	}

	public class HtmlResponse : ISemanticType
	{
		public HttpListenerContext Context { get; set; }
		public string Html { get; set; }
	}

	public class JavascriptResponse : ISemanticType
	{
		public HttpListenerContext Context { get; set; }
		public string Script { get; set; }
	}

	public class CssResponse : ISemanticType
	{
		public HttpListenerContext Context { get; set; }
		public string Script { get; set; }
	}

	public class Login : SemanticRoute
	{
		public string Username { get; set; }
		public string Password { get; set; }
	}

	public class Logout : SemanticRoute { }

	public class ViewPersistenceListRoute : SemanticRoute { }
	public class ViewPersistenceInsertRoute : SemanticRoute { }
	public class ViewPersistenceDeleteRoute : SemanticRoute { }
	public class ViewPersistenceUpdateRoute : SemanticRoute { }
}
