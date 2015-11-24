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
		public List<Poloron> Polorons { get { return poloronList; } }

		protected IPoloronPhysicsService physics;
		protected GraphicsPanel surface;
		protected Dictionary<int, Poloron> poloronMap;
		protected List<Poloron> poloronList;
		protected Timer refreshTimer;

		public PoloronRenderer()
		{
			poloronMap = new Dictionary<int, Poloron>();
			poloronList = new List<Poloron>();
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
			SetupPoloronColors(cfgSvc);			

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

		public void SetPoloronState(PoloronId id, Point2D position, Vector2D velocity, PoloronState state)
		{
			Poloron p = new Poloron() { Id = id, Position = position, Velocity = velocity, State = state, Radius = 20 };
			poloronMap[id.Value] = p;
			poloronList.Add(p);
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

		protected void SetupPoloronColors(IAppConfigService cfgSvc)
		{
			surface.NeutralColor = cfgSvc.GetValue("NeutralColor").ToColor();
			surface.NegativeColor = cfgSvc.GetValue("NegativeColor").ToColor();
			surface.PositiveColor = cfgSvc.GetValue("PositiveColor").ToColor();
		}

		protected void UpdateSurface(object sender, EventArgs e)
		{
			MovePolorons();
			EdgeHandler();
			CollisionHandler();
			surface.Invalidate();
		}

		protected void MovePolorons()
		{
			Polorons.ForEach(p=>p.Move());
		}

		protected void EdgeHandler()
		{
			Polorons.ForEach(p =>
				{
					physics.LeftEdgeHandler(p);
					physics.TopEdgeHandler(p);
					physics.RightEdgeHandler(p, surface.Width);
					physics.BottomEdgeHandler(p, surface.Height);
				});
		}

		protected void CollisionHandler()
		{
			for (int i = 0; i < poloronList.Count(); i++)
			{
				for (int j = i + 1; j < poloronList.Count(); j++)
				{
					if (poloronList[i].Intersects(poloronList[j]))
					{
						physics.Collide(poloronList[i], poloronList[j]);
					}
				}
			}
		}
	}
}
