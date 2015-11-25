using System;

namespace PoloronInterfaces
{
	public class Vector2D : Point2D
	{
		public float Magnitude { get { return (float)Math.Sqrt(X * X + Y * Y); } }

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

		/// <summary>
		/// Scale the magnitude of the vector to max.
		/// We use this function to constrain velocity.
		/// </summary>
		public void Scale(float max)
		{
			float percent = max / Magnitude;
			X *= percent;
			Y *= percent;
		}

		/// <summary>
		/// Decrease velocity proportionally by the specified percent.
		/// </summary>
		public void Decrease(Percent p)
		{
			X *= (100 - p.Value) / 100;
			Y *= (100 - p.Value) / 100;
		}

		public static Vector2D Add(Vector2D v1, Vector2D v2)
		{
			return new Vector2D(v1.X + v2.X, v1.Y + v2.Y);
		}
	}
}
