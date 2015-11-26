using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using Clifton.Core.ExtensionMethods;

namespace Clifton.Core.Utils
{
	public static class Utils
	{
		public static XmlNode AppendChildElement(this XmlNode node, string key)
		{
			XmlNode newNode = node.OwnerDocument.CreateElement(key);
			node.AppendChild(newNode);

			return newNode;
		}

		public static void AddAttribute(this XmlNode node, string key, string val)
		{
			XmlAttribute attr = node.OwnerDocument.CreateAttribute(key);
			attr.Value = val;
			node.Attributes.Append(attr);
		}

		/// <summary>
		/// Returns null if the attribute is missing, else the attribute value.
		/// </summary>
		public static string GetAttributeValue(this XmlNode node, string attrName)
		{
			string val = null;
			node.Attributes[attrName].IfNotNull(a => val = a.Value);

			return val;
		}

		public static bool TryGetAttributeValue(this XmlNode node, string attrName, out string val)
		{
			bool found = false;
			val = null;

			if (node.Attributes[attrName] != null)
			{
				found = true;
				val = node.Attributes[attrName].Value;
			}

			return found;
		}
	}
}


