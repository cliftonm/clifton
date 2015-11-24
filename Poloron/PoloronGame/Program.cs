using System;
using System.Reflection;
using System.Windows.Forms;

using Clifton.CoreSemanticTypes;
using Clifton.ModuleManagement;
using Clifton.ServiceInterfaces;
using Clifton.ServiceManagement;

using PoloronInterfaces;

namespace PoloronGame
{
	static partial class Program
	{
		private static IPoloronRenderingService renderer;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Bootstrap();

			renderer = serviceManager.Get<IPoloronRenderingService>();
			Form mainForm = renderer.CreateForm();
			mainForm.Text = "Poloron";
			renderer.CreatePoloron(PoloronId.Create(0), new Point2D(100, 50), new Vector2D(3, 3), PoloronState.Neutral);
			renderer.CreatePoloron(PoloronId.Create(1), new Point2D(150, 100), new Vector2D(4, 4), PoloronState.Negative);
			renderer.CreatePoloron(PoloronId.Create(2), new Point2D(200, 150), new Vector2D(5, 5), PoloronState.Positive);
			renderer.CreateGate(new Point2D(400, 75), new Vector2D(-3, 2));
			mainForm.Shown += OnShown;
			Application.Run(mainForm);
		}

		private static void OnShown(object sender, EventArgs e)
		{
			renderer.Start();
		}
	}
}
