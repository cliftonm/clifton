using System;
using System.ComponentModel;
using System.Data;

namespace Clifton.Core.ModelTableManagement
{
	public abstract class MappedRecord
	{
		[System.Runtime.Serialization.IgnoreDataMember]
		public DataRow Row { get; set; }
	}
}

