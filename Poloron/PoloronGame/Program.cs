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
			Application.Run(mainForm);
		}
	}
}
