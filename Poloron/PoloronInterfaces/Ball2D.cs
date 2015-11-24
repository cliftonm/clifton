using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoloronInterfaces
{
	public class Ball2D
	{
		public Point2D Position { get; set; }
		public Vector2D Velocity { get; set; }
		public float Radius { get; set; }
		public float Mass { get; set; }

		public float LeftEdge { get { return Position.X - Radius; } }
		public float TopEdge { get { return Position.Y - Radius; } }
		public float RightEdge { get { return Position.X + Radius; } }
		public float BottomEdge { get { return Position.Y + Radius; } }
		public float Diameter { get { return Radius * 2; } }

		public Ball2D()
		{
			Mass = 1;
		}

		public void Move()
		{
			Position.X += Velocity.X;
			Position.Y += Velocity.Y;
		}

		public bool Intersects(Ball2D withBall)
		{
			bool ret = false;

			// Quick check:

			if ((HorizontalDistance(withBall) <= Diameter) &&
				 (VerticalDistance(withBall) <= Diameter))
			{
				ret = Distance(withBall) <= Diameter;
			}

			return ret;
		}

		protected float HorizontalDistance(Ball2D withBall)
		{
			return Math.Abs(Position.X - withBall.Position.X);
		}

		protected float VerticalDistance(Ball2D withBall)
		{
			return Math.Abs(Position.Y - withBall.Position.Y);
		}

		protected float Distance(Ball2D withBall)
		{
			float dx = Position.X - withBall.Position.X;
			float dy = Position.Y - withBall.Position.Y;

			return (float)Math.Sqrt(dx * dx + dy * dy);
		}
	}
}
