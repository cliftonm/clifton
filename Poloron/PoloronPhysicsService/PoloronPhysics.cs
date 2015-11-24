using Clifton.ServiceInterfaces;

using PoloronInterfaces;

namespace PoloronPhysicsService
{
	public class PoloronPhysicsModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IPoloronPhysicsService, PoloronPhysics>();
		}
	}

	public class PoloronPhysics : ServiceBase, IPoloronPhysicsService
	{
		public void LeftEdgeHandler(Ball2D ball)
		{
			if (ball.LeftEdge < 0)
			{
				ball.Position.X = ball.Radius;
				ball.Velocity.ReflectX();
			}
		}

		public void TopEdgeHandler(Ball2D ball)
		{
			if (ball.TopEdge < 0)
			{
				ball.Position.Y = ball.Radius;
				ball.Velocity.ReflectY();
			}
		}

		public void RightEdgeHandler(Ball2D ball, int width)
		{
			if (ball.RightEdge > width)
			{
				ball.Position.X = width - ball.Radius;
				ball.Velocity.ReflectX();
			}
		}

		public void BottomEdgeHandler(Ball2D ball, int height)
		{
			if (ball.BottomEdge > height)
			{
				ball.Position.Y = height - ball.Radius;
				ball.Velocity.ReflectY();
			}
		}

		public void Collide(Ball2D ball1, Ball2D ball2)
		{
			ElasticCollision.Collide(ball1, ball2);
		}
	}
}
