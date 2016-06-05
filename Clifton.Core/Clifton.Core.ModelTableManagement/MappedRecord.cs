using System;
using System.ComponentModel;
using System.Data;

using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Clifton.Core.ModelTableManagement
{
	public abstract class MappedRecord
	{
		[IgnoreDataMember, XmlIgnore]
		public DataRow Row { get; set; }
	}
}

