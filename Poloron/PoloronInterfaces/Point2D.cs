using System;

namespace PoloronInterfaces
{
	public class Point2D
	{
		public float X { get; set; }
		public float Y { get; set; }

		public Point2D(float x, float y)
		{
			X = x;
			Y = y;
		}

		public float HorizontalDistance(Point2D other)
		{
			return Math.Abs(X - other.X);
		}

		public float VerticalDistance(Point2D other)
		{
			return Math.Abs(Y - other.Y);
		}

		public float Distance(Point2D other)
		{
			float dx = X - other.X;
			float dy = Y - other.Y;

			return (float)Math.Sqrt(dx * dx + dy * dy);
		}

		public double Angle(Point2D other)
		{
			return Math.Atan2(Y - other.Y, X - other.X);
		}
	}
}
