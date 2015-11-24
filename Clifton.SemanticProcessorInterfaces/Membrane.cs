using System;
using System.Collections.Generic;

using Clifton.Semantics;

namespace Clifton.SemanticProcessorInterfaces
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
