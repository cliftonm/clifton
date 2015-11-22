using System;
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
		protected GraphicsPanel surface;

		public PoloronRenderer()
		{
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

		public void SetPoloronState(PoloronId id, XPos x, YPos y, PoloronState state)
		{
			surface.SetPoloronState(id, x, y, state);
		}

		protected void SetupLocationAndSize(Form form, IAppConfigService cfgSvc)
		{
			form.Width = cfgSvc.GetValue("Width").to_i();
			form.Height = cfgSvc.GetValue("Height").to_i();
			form.StartPosition = FormStartPosition.CenterScreen;
		}

		protected GraphicsPanel SetupRenderingSurface(Form form, IAppConfigService cfgSvc)
		{
			GraphicsPanel surface = new GraphicsPanel(cfgSvc.GetValue("BackgroundColor").ToColor());
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
	}
}
