using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using PoloronInterfaces;

namespace PoloronGame
{
	static partial class Program
	{
		public static List<Poloron> polorons = new List<Poloron>();
		public static Gate gate;

		private static Timer refreshTimer;

		public static void InitializeGameTick()
		{
			refreshTimer = new Timer();
			refreshTimer.Interval = 1000 / 60;	// 60 times a second.
			refreshTimer.Tick += GameTick;
		}

		public static void GameTick(object sender, EventArgs e)
		{
			MovePolorons();
			MoveGate();
			EdgeHandler();
			CollisionHandler();
			renderer.Render();
		}

		public static void Start()
		{
			refreshTimer.Start();
		}

		public static void Stop()
		{
			refreshTimer.Stop();
		}

		// TODO: Config for poloron and gate radius

		public static void CreatePoloron(PoloronId id, Point2D position, Vector2D velocity, PoloronState state)
		{
			Poloron p = new Poloron() { Id = id, Position = position, Velocity = velocity, State = state, Radius = 20 };
			polorons.Add(p);
		}

		public static void CreateGate(Point2D position, Vector2D velocity)
		{
			gate = new Gate() { Position = position, Velocity = velocity, Radius = 40 };
		}

		public static void SetState(PoloronId id, PoloronState state)
		{
			polorons.Single(p => p.Id.Value == id.Value).State = state;
		}

		private static void MovePolorons()
		{
			polorons.ForEach(p => p.Move());
		}

		private static void MoveGate()
		{
			gate.Move();
		}

		private static void EdgeHandler()
		{
			polorons.ForEach(p =>
			{
				CheckEdgeCollision(p);
			});

			CheckEdgeCollision(gate);
		}

		private static void CheckEdgeCollision(Ball2D ball)
		{
			physics.LeftEdgeHandler(ball);
			physics.TopEdgeHandler(ball);
			physics.RightEdgeHandler(ball, renderer.Surface.Width);
			physics.BottomEdgeHandler(ball, renderer.Surface.Height);
		}

		private static void CollisionHandler()
		{
			for (int i = 0; i < polorons.Count(); i++)
			{
				for (int j = i + 1; j < polorons.Count(); j++)
				{
					if (polorons[i].Intersects(polorons[j]))
					{
						physics.Collide(polorons[i], polorons[j]);
					}
				}
			}
		}


	}
}
