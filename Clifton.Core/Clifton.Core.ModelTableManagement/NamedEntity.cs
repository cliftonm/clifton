using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq.Expressions;

using System.Xml.Serialization;

namespace Clifton.Core.ModelTableManagement
{
	[Table]
	public abstract class NamedEntity : IEntity
	{
		[XmlIgnore]
		public abstract int? Id { get; set; }
		[XmlIgnore]
		public abstract string Name { get; set; }
		[XmlIgnore]
		public abstract int DisplayOrder { get; set; }
	}
}
