using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using Clifton.ExtensionMethods;

using PoloronInterfaces;

namespace PoloronGame
{
	static partial class Program
	{
		public static List<Poloron> polorons = new List<Poloron>();
		public static Gate gate;
		public static bool levelStarting;
		public static bool levelComplete;

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
			ApplyForces();
			AdjustEnergy();
			ApplyBrake();
			SpeedLimit();

			if (levelStarting)
			{
				if (GateOpeningAnimation())
				{
					if (PoloronOpeningAnimation())
					{
						levelStarting = false;
					}
				}
			}

			if (!levelComplete)
			{
				if (polorons[0].EncompassedBy(gate))
				{
					levelComplete = true;
					polorons[0].Visible = false;
				}
			}
			else
			{
				if (GateClosingAnimation())
				{
					if (PoloronClosingAnimation())
					{
						// Stop();
						++currentLevel;
						InitializeLevel(currentLevel);
						levelComplete = false;
						levelStarting = true;
						// Start();
					}
				}
			}

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

		public static Poloron CreatePoloron(PoloronId id, Point2D position, Vector2D velocity, PoloronState state)
		{
			Poloron p = new Poloron() { Id = id, Position = position, Velocity = velocity, State = state, Radius = 1, Visible = false };
			polorons.Add(p);

			return p;
		}

		public static void CreateGate(Point2D position, Vector2D velocity)
		{
			gate = new Gate() { Position = position, Velocity = velocity, Radius = 1, Visible = true };
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

		/// <summary>
		/// For the moment, apply forces only to the player's poloron.  We might change this later so that the player's action
		/// also affects the other polorons.
		/// </summary>
		private static void ApplyForces()
		{
			Poloron player = polorons[0];

			if ( (player.State == PoloronState.Positive || player.State == PoloronState.Negative) && (renderer.Energy > 0) )
			{
				Point2D position = player.Position;

				for (int i = 1; i < polorons.Count; i++)
				{
					float distance = position.Distance(polorons[i].Position);
					float force = (float)750 / (distance * distance);
					double angle = position.Angle(polorons[i].Position);
					Vector2D vforce = ApplyForce(player.State, polorons[i].State, force, angle);
					Console.WriteLine("{0},{1}", vforce.X, vforce.Y);
					player.Velocity.Add(vforce);
				}
			}
		}

		private static void AdjustEnergy()
		{
			switch (polorons[0].State)
			{
				case PoloronState.Charging:
					renderer.Energy = (renderer.Energy + 1).Max(1000);
					break;

				case PoloronState.Negative:
				case PoloronState.Positive:
					renderer.Energy = (renderer.Energy - 1).Min(0);
					break;
			}
		}

		private static void ApplyBrake()
		{
			if (polorons[0].State == PoloronState.Charging)
			{
				polorons[0].Velocity.Decrease(Percent.Create(5));
			}
		}

		private static void SpeedLimit()
		{
			polorons.ForEach(p =>
				{
					if (p.Velocity.Magnitude > 10)
					{
						p.Velocity.Scale(10);
					}
				});
		}

		private static Vector2D ApplyForce(PoloronState state, PoloronState other, float force, double angle)
		{
			float multiplier = (state == other) ? force : -force;
			Vector2D vf = new Vector2D((float)(Math.Cos(angle) * multiplier), (float)(Math.Sin(angle) * multiplier));

			return vf;
		}

		/// <summary>
		/// Close the gate.
		/// </summary>
		private static bool GateClosingAnimation()
		{
			if (gate.Radius > 1)
			{
				gate.Radius -= 1;
			}
			else
			{
				gate.Visible = false;
			}

			return gate.Radius == 1;
		}

		private static bool GateOpeningAnimation()
		{
			if (gate.Radius < 40)
			{
				gate.Radius += 1;
			}

			return gate.Radius == 40;
		}

		/// <summary>
		/// Remove polorons from surface.
		/// </summary>
		private static bool PoloronClosingAnimation()
		{
			bool done = false;

			polorons.ForEach(p =>
				{
					done = p.Radius == 1;

					if (!done)
					{
						p.Radius -= 1;
					}
					else
					{
						p.Visible = false;
					}
				});

			return done;
		}

		private static bool PoloronOpeningAnimation()
		{
			bool done = false;

			polorons.ForEach(p =>
			{
				p.Visible=true;
				done = p.Radius == 20;

				if (!done)
				{
					p.Radius += 1;
				}
			});

			return done;
		}
	}
}
