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
		public Control Surface { get { return surface; } }
		public List<Poloron> Polorons { get; set; }
		public Gate Gate { get; set; }

		protected GraphicsPanel surface;

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
			SetupPoloron(cfgSvc);
			SetupGate(cfgSvc);

			return form;
		}

		public void Render()
		{
			surface.Invalidate();
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
			surface.ChargingColor = cfgSvc.GetValue("ChargingColor").ToColor();
		}

		protected void SetupGate(IAppConfigService cfgSvc)
		{
			surface.GateColor = cfgSvc.GetValue("GateColor").ToColor();
		}
	}
}
