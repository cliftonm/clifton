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

namespace Clifton.Core.Semantics
{
    /// <summary>
    /// Topmost abstraction.
    /// </summary>
    public interface ISemanticType { }

    public interface ISemanticQualifier : ISemanticType { }

	public interface ISemanticType<T> : ISemanticType
	{
		T Value { get; }
	}

	/// <summary>
	/// Enforces a semantic type of type T with a setter.
	/// </summary>
	/// <typeparam name="T">The native type.</typeparam>
	public abstract class SemanticType<T> : ISemanticType<T>
	{
		public virtual T Value { get; protected set; }
	}

	/// <summary>
	/// Abstract native semantic type.  Implements the native type T and the setter/getter.
	/// This abstraction implements an immutable native type due to the fact that the setter
	/// always returns a new concrete instance.
	/// </summary>
	/// <typeparam name="R">The concrete instance.</typeparam>
	/// <typeparam name="T">The native type backing the concrete instance.</typeparam>
	public abstract class ImmutableSemanticType<R, T> : SemanticType<T>
		where R : ImmutableSemanticType<R, T>, new()
	{
		public static R Create(T val)
		{
			R ret = new R();
			ret.Value = val;

			return ret;
		}
	}
}
