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


