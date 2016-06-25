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

namespace Clifton.Core.Semantics
{
	public class Membrane : IMembrane
	{
		protected Membrane parent { get; set; }
		protected List<Membrane> childMembranes { get; set; }
		protected List<Type> outboundPermeableTo { get; set; }
		protected List<Type> inboundPermeableTo { get; set; }

		public Membrane()
		{
			parent = null;
			childMembranes = new List<Membrane>();
			outboundPermeableTo = new List<Type>();
			inboundPermeableTo = new List<Type>();
		}

		public void AddChild(Membrane child)
		{
			childMembranes.Add(child);
			child.parent = this;
		}

		public void OutboundPermeableTo<T>()
			where T : ISemanticType
		{
			outboundPermeableTo.Add(typeof(T));
		}

		public void InboundPermeableTo<T>()
			where T : ISemanticType
		{
			inboundPermeableTo.Add(typeof(T));
		}

		/// <summary>
		/// Given this membrane's outbound list, what membranes are inbound permeabe to the ST as well?
		/// </summary>
		public List<IMembrane> PermeateTo(ISemanticType st)
		{
			List<IMembrane> ret = new List<IMembrane>();
			Type sttype = st.GetType();

			if (outboundPermeableTo.Contains(sttype))
			{
				// Can we traverse to the parent?
				if ((parent != null) && (parent.inboundPermeableTo.Contains(sttype)))
				{
					ret.Add(parent);
				}

				// Can we traverse to children?
				foreach (Membrane child in childMembranes)
				{
					if (child.inboundPermeableTo.Contains(sttype))
					{
						ret.Add(child);
					}
				}
			}

			return ret;
		}
	}

	/// <summary>
	/// Type for our built-in membrane
	/// </summary>
	public class SurfaceMembrane : Membrane
	{
	}

	/// <summary>
	/// Type for our built-in membrane
	/// </summary>
	public class LoggerMembrane : Membrane
	{
	}
}
