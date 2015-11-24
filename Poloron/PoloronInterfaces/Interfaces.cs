using System.Windows.Forms;

using Clifton.ServiceInterfaces;

namespace PoloronInterfaces
{
	public interface IPoloronRenderingService : IService
	{
		Form CreateForm();
		void CreatePoloron(PoloronId id, Point2D location, Vector2D velocity, PoloronState state);
		void CreateGate(Point2D position, Vector2D velocity);
		void Start();
		void Stop();
	}

	public interface IPoloronPhysicsService : IService
	{
		void LeftEdgeHandler(Ball2D ball);
		void TopEdgeHandler(Ball2D ball);
		void RightEdgeHandler(Ball2D ball, int width);
		void BottomEdgeHandler(Ball2D ball, int height);
		void Collide(Ball2D ball1, Ball2D ball2);
	}
}
