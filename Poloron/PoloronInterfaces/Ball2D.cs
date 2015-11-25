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

			if ((Position.HorizontalDistance(withBall.Position) <= Diameter) &&
				 (Position.VerticalDistance(withBall.Position) <= Diameter))
			{
				// Full check:
				ret = Position.Distance(withBall.Position) <= Diameter;
			}

			return ret;
		}

		/// <summary>
		/// Return true if this ball is completely encompassed by the withBall.
		/// Assumes withBall.Radius > this.Radius.
		/// </summary>
		public bool EncompassedBy(Ball2D withBall)
		{
			bool ret = false;

			// Quick check:

			if ((Position.HorizontalDistance(withBall.Position) <= withBall.Radius - Radius) &&
				 (Position.VerticalDistance(withBall.Position) <= withBall.Radius - Radius))
			{
				// Full check:
				ret = Position.Distance(withBall.Position) <= withBall.Radius - Radius;
			}

			return ret;
		}
	}
}
