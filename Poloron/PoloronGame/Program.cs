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
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Bootstrap();

			IPoloronRenderingService renderer = serviceManager.Get<IPoloronRenderingService>();
			Form mainForm = renderer.CreateForm();
			mainForm.Text = "Poloron";
			renderer.SetPoloronState(PoloronId.Create(0), XPos.Create(100), YPos.Create(50), PoloronState.Neutral);
			renderer.SetPoloronState(PoloronId.Create(1), XPos.Create(150), YPos.Create(100), PoloronState.Negative);
			renderer.SetPoloronState(PoloronId.Create(2), XPos.Create(200), YPos.Create(150), PoloronState.Positive);
			Application.Run(mainForm);
		}
	}
}
