using System;

namespace Clifton.Core.ModelTableManagement
{
	public interface ICreateUpdate
	{
		DateTime? CreatedOn { get; set; }
		DateTime? UpdatedOn { get; set; }
	}
}
