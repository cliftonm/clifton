using System;
using System.Collections.Generic;
using System.Drawing;
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
		public Control Surface { get { return surface; } }
		public List<Poloron> Polorons { get; set; }
		public Gate Gate { get; set; }
		public int Energy { get; set; }

		protected GameSurface surface;
		protected EnergyBar energyBar;

		public PoloronRenderer()
		{
		}

		public override void FinishedInitialization()
		{
			base.FinishedInitialization();
		}

		public Form CreateForm()
		{
			Form form = new Form();
			IAppConfigService cfgSvc = ServiceManager.Get<IAppConfigService>();
			SetupLocationAndSize(form, cfgSvc);
			surface = SetupRenderingSurface(form, cfgSvc);
			energyBar = SetupEnergyBar(form, cfgSvc);
			SetupPoloron(cfgSvc);
			SetupGate(cfgSvc);

			return form;
		}

		public void Render()
		{
			surface.Invalidate();
			energyBar.Invalidate();
		}

		protected void SetupLocationAndSize(Form form, IAppConfigService cfgSvc)
		{
			form.Width = cfgSvc.GetValue("Width").to_i();
			form.Height = cfgSvc.GetValue("Height").to_i() + 20;
			form.StartPosition = FormStartPosition.CenterScreen;
		}

		protected GameSurface SetupRenderingSurface(Form form, IAppConfigService cfgSvc)
		{
			GameSurface surface = new GameSurface(this, cfgSvc.GetValue("BackgroundColor").ToColor());
			surface.Width = form.ClientSize.Width;
			surface.Height = form.ClientSize.Height - 20;
			surface.Location = new Point(0, 20);
			surface.GridColor = cfgSvc.GetValue("GridColor").ToColor();
			surface.GridSpacing = cfgSvc.GetValue("GridSpacing").to_i();
			form.Controls.Add(surface);

			return surface;
		}

		protected EnergyBar SetupEnergyBar(Form form, IAppConfigService cfgSvc)
		{
			EnergyBar energyBar = new EnergyBar(this, Color.White);
			energyBar.Width = form.ClientSize.Width - 3;
			energyBar.Height = 20;
			energyBar.Location = new Point(0, 0);
			form.Controls.Add(energyBar);

			return energyBar;
		}

		protected void SetupPoloron(IAppConfigService cfgSvc)
		{
			surface.NeutralColor = cfgSvc.GetValue("NeutralColor").ToColor();
			surface.NegativeColor = cfgSvc.GetValue("NegativeColor").ToColor();
			surface.PositiveColor = cfgSvc.GetValue("PositiveColor").ToColor();
			surface.ChargingColor = cfgSvc.GetValue("ChargingColor").ToColor();
		}

		protected void SetupGate(IAppConfigService cfgSvc)
		{
			surface.GateColor = cfgSvc.GetValue("GateColor").ToColor();
		}
	}
}
