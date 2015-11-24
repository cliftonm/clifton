using System;
using System.Drawing;

using PoloronInterfaces;

namespace PoloronPhysicsService
{
	public static class ElasticCollision
	{
		// 2D elastic collisions: http://www.vobarian.com/collisions/2dcollisions2.pdf
		// From: http://manyrootsofallevilrants.blogspot.com/2011/10/2-d-simple-elastic-collision-using-xna.html
		public static void Collide(Ball2D ball1, Ball2D ball2)
		{
			//find normal vector
			Vector2D normal = new Vector2D(ball2.Position.X - ball1.Position.X, ball2.Position.Y - ball1.Position.Y);

			//find normal vector's modulus, i.e. length
			float normalmod = (float)Math.Sqrt(Math.Pow(normal.X, 2) + Math.Pow(normal.Y, 2));

			//find unitnormal vector
			Vector2D unitnormal = new Vector2D((ball2.Position.X - ball1.Position.X) / normalmod, (ball2.Position.Y - ball1.Position.Y) / normalmod);

			//find tangent vector
			Vector2D unittan = new Vector2D(-1 * unitnormal.Y, unitnormal.X);

			//first ball normal speed before collision
			float inormalspeedb = unitnormal.X * ball1.Velocity.X + unitnormal.Y * ball1.Velocity.Y;

			//first ball tangential speed 
			float itanspeed = unittan.X * ball1.Velocity.X + unittan.Y * ball1.Velocity.Y;

			//second ball normal speed before collision
			float ynormalspeedb = unitnormal.X * ball2.Velocity.X + unitnormal.Y * ball2.Velocity.Y;

			//second ball tangential speed
			float ytanspeed = unittan.X * ball2.Velocity.X + unittan.Y * ball2.Velocity.Y;

			//tangential speeds don't change whereas normal speeds do

			//Calculate normal speeds after the collision
			float inormalspeeda = (inormalspeedb * (ball1.Mass - ball2.Mass) + 2 * ball2.Mass * ynormalspeedb) / (ball1.Mass + ball2.Mass);
			float ynormalspeeda = (ynormalspeedb * (ball2.Mass - ball1.Mass) + 2 * ball1.Mass * inormalspeedb) / (ball1.Mass + ball2.Mass);

			//Calculate first ball Velocity vector components (tangential and normal)
			Vector2D inormala = new Vector2D(unitnormal.X * inormalspeeda, unitnormal.Y * inormalspeeda);
			Vector2D itana = new Vector2D(unittan.X * itanspeed, unittan.Y * itanspeed);

			//Calculate second ball Velocity vector components (tangential and normal)
			Vector2D ynormala = new Vector2D(unitnormal.X * ynormalspeeda, unitnormal.Y * ynormalspeeda);
			Vector2D ytana = new Vector2D(unittan.X * ytanspeed, unittan.Y * ytanspeed);

			//Add Vector components to each balls' Velocity
			ball1.Velocity = Vector2D.Add(inormala, itana);
			ball2.Velocity = Vector2D.Add(ynormala, ytana);
		}
	}
}
