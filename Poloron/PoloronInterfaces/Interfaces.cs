using System.Windows.Forms;

using Clifton.ServiceInterfaces;

namespace PoloronInterfaces
{
	public interface IPoloronRenderingService : IService
	{
		Form CreateForm();
	}
}
