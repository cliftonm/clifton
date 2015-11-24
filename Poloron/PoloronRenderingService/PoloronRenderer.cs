using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using Clifton.ExtensionMethods;
using Clifton.Semantics;
using Clifton.ServiceInterfaces;

using PoloronInterfaces;

namespace PoloronRenderingService
{
	public class PoloronRenderingModule : IModule
	{
		public void InitializeServices(IServiceManager serviceManager)
		{
			serviceManager.RegisterSingleton<IPoloronRenderingService, PoloronRenderer>();
		}
	}

	public class PoloronRenderer : ServiceBase, IPoloronRenderingService
	{
		public List<Poloron> Polorons { get { return polorons; } }
		public Gate Gate { get { return gate; } }

		protected IPoloronPhysicsService physics;
		protected GraphicsPanel surface;
		protected List<Poloron> polorons;
		protected Gate gate;
		protected Timer refreshTimer;

		public PoloronRenderer()
		{
			polorons = new List<Poloron>();
			refreshTimer = new Timer();
			refreshTimer.Interval = 1000 / 60;	// 60 times a second.
			refreshTimer.Tick += UpdateSurface;
		}

		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
			physics = ServiceManager.Get<IPoloronPhysicsService>();
		}

		public Form CreateForm()
		{
			Form form = new Form();
			IAppConfigService cfgSvc = ServiceManager.Get<IAppConfigService>();
			SetupLocationAndSize(form, cfgSvc);
			surface = SetupRenderingSurface(form, cfgSvc);
			SetupPoloron(cfgSvc);
			SetupGate(cfgSvc);

			return form;
		}

		public void Start()
		{
			refreshTimer.Start();
		}

		public void Stop()
		{
			refreshTimer.Stop();
		}

		// TODO: Config for poloron and gate radius

		public void CreatePoloron(PoloronId id, Point2D position, Vector2D velocity, PoloronState state)
		{
			Poloron p = new Poloron() { Id = id, Position = position, Velocity = velocity, State = state, Radius = 20 };
			polorons.Add(p);
		}

		public void CreateGate(Point2D position, Vector2D velocity)
		{
			gate = new Gate() { Position = position, Velocity = velocity, Radius = 40 };
		}

		protected void SetupLocationAndSize(Form form, IAppConfigService cfgSvc)
		{
			form.Width = cfgSvc.GetValue("Width").to_i();
			form.Height = cfgSvc.GetValue("Height").to_i();
			form.StartPosition = FormStartPosition.CenterScreen;
		}

		protected GraphicsPanel SetupRenderingSurface(Form form, IAppConfigService cfgSvc)
		{
			GraphicsPanel surface = new GraphicsPanel(this, cfgSvc.GetValue("BackgroundColor").ToColor());
			surface.Dock = DockStyle.Fill;
			surface.GridColor = cfgSvc.GetValue("GridColor").ToColor();
			surface.GridSpacing = cfgSvc.GetValue("GridSpacing").to_i();
			form.Controls.Add(surface);

			return surface;
		}

		protected void SetupPoloron(IAppConfigService cfgSvc)
		{
			surface.NeutralColor = cfgSvc.GetValue("NeutralColor").ToColor();
			surface.NegativeColor = cfgSvc.GetValue("NegativeColor").ToColor();
			surface.PositiveColor = cfgSvc.GetValue("PositiveColor").ToColor();
		}

		protected void SetupGate(IAppConfigService cfgSvc)
		{
			surface.GateColor = cfgSvc.GetValue("GateColor").ToColor();
		}

		protected void UpdateSurface(object sender, EventArgs e)
		{
			MovePolorons();
			MoveGate();
			EdgeHandler();
			CollisionHandler();
			surface.Invalidate();
		}

		protected void MovePolorons()
		{
			Polorons.ForEach(p=>p.Move());
		}

		protected void MoveGate()
		{
			gate.Move();
		}

		protected void EdgeHandler()
		{
			Polorons.ForEach(p =>
				{
					CheckEdgeCollision(p);
				});

			CheckEdgeCollision(gate);
		}

		protected void CheckEdgeCollision(Ball2D ball)
		{
			physics.LeftEdgeHandler(ball);
			physics.TopEdgeHandler(ball);
			physics.RightEdgeHandler(ball, surface.Width);
			physics.BottomEdgeHandler(ball, surface.Height);
		}

		protected void CollisionHandler()
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
