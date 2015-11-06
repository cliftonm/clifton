using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Clifton.Utils;

namespace Clifton.DbServices
{
	public class ViewSchema
	{
		public static ViewSchema Schema
		{
			get
			{
				if (viewSchema == null)
				{
					viewSchema = new ViewSchema();
				}

				return viewSchema;
			}
		}

		public Dictionary<string, TableInfo> Tables { get; protected set; }
		public Dictionary<string, ViewInfo> Views { get; protected set; }

		protected static ViewSchema viewSchema;

		protected ViewSchema()
		{
			Initialize();
		}

		public ViewInfo GetView(string viewName)
		{
			return Views[viewName];
		}

		public bool TryGetView(string viewName, out ViewInfo view)
		{
			return Views.TryGetValue(viewName, out view);
		}

		/// <summary>
		/// Register a unique table.
		/// </summary>
		/// <param name="table"></param>
		public void RegisterTable(TableInfo table)
		{
			Assert.That(!Tables.ContainsKey(table.Name), "View " + table.Name + " is already registered.");
			Tables[table.Name] = table;
		}

		/// <summary>
		/// Register a unique view.
		/// </summary>
		public void RegisterView(ViewInfo view)
		{
			Assert.That(!Views.ContainsKey(view.Name), "View " + view.Name + " is already registered.");
			Views[view.Name] = view;
		}

		// TODO: Somewhat awkward, as we're serializing the registered views as part of this process.
		public void Serialize(string fn = "ViewSchema.xml")
		{
			XmlDocument xdoc = new XmlDocument();
			XmlNode root = xdoc.CreateElement("ViewSchema");
			xdoc.AppendChild(root);
			XmlNode xtables = root.AppendChildElement("Tables");
			XmlNode xviews = root.AppendChildElement("Views");

			foreach (TableInfo table in Tables.Values)
			{
				table.Serialize(xtables);
			}

			foreach (ViewInfo view in Views.Values)
			{
				view.Serialize(xviews);
			}

			xdoc.Save(fn);
		}

		public void Deserialize(string fn = "ViewSchema.xml")
		{
			Initialize();
			XmlDocument xdoc = new XmlDocument();
			xdoc.Load(fn);
			XmlNode root = xdoc.ChildNodes[0];

			XmlNode xtables = root["Tables"];
			XmlNode xviews = root["Views"];

			foreach (XmlNode xtable in xtables)
			{
				TableInfo table = TableInfo.Deserialize(xtable);
				Tables[table.Name] = table;
			}

			foreach (XmlNode xview in xviews)
			{
				ViewInfo view = ViewInfo.Deserialize(Tables.Values.ToList(), xview);
				Views[view.Name] = view;
			}
		}

		protected void Initialize()
		{
			Tables = new Dictionary<string, TableInfo>();
			Views = new Dictionary<string, ViewInfo>();
		}
	}
}
