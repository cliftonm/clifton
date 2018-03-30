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
using System.Net;

using Clifton.Core.Semantics;

namespace Clifton.WebInterfaces
{
	public class UriPath : ImmutableSemanticType<UriPath, string> { }
	public class UriExtension : ImmutableSemanticType<UriExtension, string> { }
	public class HttpVerb : ImmutableSemanticType<HttpVerb, string> { }

	public class SocketMembrane : Membrane { }
	public class WebServerMembrane : Membrane { }

	public class ServerSocketMessage : ISemanticType
	{
        public IWebSocketSession Session { get; set; }
		public string Text { get; set; }
	}

	public class ClientSocketMessage : ISemanticType
	{
		public string Text { get; set; }
	}

	public class ServerSocketError : ISemanticType
	{
		public IWebSocketSession Session { get; set; }
	}

	public class ServerSocketClosed : ISemanticType
	{
		public IWebSocketSession Session { get; set; }
	}

	public class ClientSocketError : ISemanticType { }
	public class ClientSocketClosed : ISemanticType { }

	/// <summary>
	/// A route before it has been semantic-ized.
	/// </summary>
	public class Route : ISemanticType
	{
		public IContext Context { get; set; }
		public string Data { get; set; }
	}

	/// <summary>
	/// Must be derived from so that a semantic route is one tied to a specific class type,
	/// usually implementing the properties specific to the route, for example, a deserialized
	/// Json package and/or URL parameters.
	/// </summary>
	public abstract class SemanticRoute : ISemanticType
	{
		public IContext Context { get; set; }
		public string PostData { get; set; }
	}

	/// <summary>
	/// Semantic type for file data, like HTML and JS files.
	/// </summary>
	public class HtmlPath : SemanticRoute { }
	public class JavascriptPath : SemanticRoute { }
	public class CssPath : SemanticRoute { }
	public class HtmlPageRoute : SemanticRoute { }

    // Used for testing only!!!
    public class AutoLoginRoute : SemanticRoute { }

	public class UnhandledContext : ISemanticType
	{
		public IContext Context { get; set; }
	}

	public class ExceptionResponse : ISemanticType
	{
		public IContext Context { get; set; }
		public Exception Exception { get; set; }
	}

	public class StringResponse : ISemanticType
	{
		public IContext Context { get; set; }
		public int StatusCode { get; set; }
		public string Message { get; set; }
	}

	public class JsonResponse : ISemanticType
	{
		public IContext Context { get; set; }
		public int StatusCode { get; set; }
		public string Json { get; set; }
	}

    public class BinaryResponse : ISemanticType
    {
        public IContext Context { get; set; }
        public string ContentType { get; set; }
        public byte[] BinaryData { get; set; }
    }

    public class ImageResponse : ISemanticType
	{
		public IContext Context { get; set; }
		public string ContentType { get; set; }
		public byte[] BinaryData { get; set; }
	}

	public class FontResponse : ISemanticType
	{
		public IContext Context { get; set; }
		public string ContentType { get; set; }
		public byte[] BinaryData { get; set; }
	}

	public class DataResponse : ISemanticType
	{
		public IContext Context { get; set; }
		public int StatusCode { get; set; }
		public byte[] Data { get; set; }
	}

	public class HtmlResponse : ISemanticType
	{
		public IContext Context { get; set; }
		public string Html { get; set; }
        public bool GZip { get; set; }
	}

	public class JavascriptResponse : ISemanticType
	{
		public IContext Context { get; set; }
		public string Script { get; set; }
        public bool GZip { get; set; }
    }

    public class CssResponse : ISemanticType
	{
		public IContext Context { get; set; }
		public string Script { get; set; }
        public bool GZip { get; set; }
    }
}