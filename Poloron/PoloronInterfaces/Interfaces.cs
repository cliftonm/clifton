using System.Collections.Generic;
using System.Windows.Forms;

using Clifton.ServiceInterfaces;

namespace PoloronInterfaces
{
	public interface IPoloronRenderingService : IService
	{
		Control Surface { get; }
		List<Poloron> Polorons { get; set; }
		Gate Gate { get; set; }
		int Energy { get; set; }

		Form CreateForm();
		void Render();
	}

	public interface IPoloronPhysicsService : IService
	{
		void LeftEdgeHandler(Ball2D ball);
		void TopEdgeHandler(Ball2D ball);
		void RightEdgeHandler(Ball2D ball, int width);
		void BottomEdgeHandler(Ball2D ball, int height);
		void Collide(Ball2D ball1, Ball2D ball2);
	}

	public interface IPoloronInputController : IService 
	{
		void Initialize(Control control);
	}
}
