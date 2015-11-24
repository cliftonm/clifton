namespace PoloronInterfaces
{
	public class Vector2D : Point2D
	{
		public Vector2D(float x, float y) : base(x, y)
		{
		}

		public void ReflectX()
		{
			X = -X;
		}

		public void ReflectY()
		{
			Y = -Y;
		}

		public void Add(Vector2D other)
		{
			X += other.X;
			Y += other.Y;
		}

		public static Vector2D Add(Vector2D v1, Vector2D v2)
		{
			return new Vector2D(v1.X + v2.X, v1.Y + v2.Y);
		}
	}
}
