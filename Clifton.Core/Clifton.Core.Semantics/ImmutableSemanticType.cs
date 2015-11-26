using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.Core.Semantics
{
	/// <summary>
	/// Topmost abstraction.
	/// </summary>
	public interface ISemanticType
	{
	}

	public interface ISemanticType<T>
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
