using System.Windows.Forms;

using Clifton.ServiceInterfaces;

namespace PoloronInterfaces
{
	public interface IPoloronRenderingService : IService
	{
		Form CreateForm();
		void SetPoloronState(PoloronId id, XPos x, YPos y, PoloronState state);
	}
}
