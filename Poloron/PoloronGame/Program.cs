using System;
using System.Reflection;
using System.Runtime.InteropServices;
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
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool AllocConsole();		

		public static IPoloronRenderingService renderer;
		public static IPoloronPhysicsService physics;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			AllocConsole();
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Bootstrap();
			Form mainForm;

			try
			{
				mainForm = InitializeGame();
				mainForm.Shown += OnShown;
				InitializeInputController();
				InitializeController();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Environment.Exit(1);
				return;
			}

			Application.Run(mainForm);
		}

		private static void OnShown(object sender, EventArgs e)
		{
			Start();
		}

		private static Form InitializeGame()
		{
			renderer = serviceManager.Get<IPoloronRenderingService>();
			physics = serviceManager.Get<IPoloronPhysicsService>();
			Form mainForm = renderer.CreateForm();
			mainForm.Text = "Poloron";
			/*
			CreatePoloron(PoloronId.Create(0), new Point2D(100, 50), new Vector2D(3, 3), PoloronState.Neutral);
			CreatePoloron(PoloronId.Create(1), new Point2D(150, 100), new Vector2D(4, 4), PoloronState.Negative);
			CreatePoloron(PoloronId.Create(2), new Point2D(200, 150), new Vector2D(5, 5), PoloronState.Positive);
			CreateGate(new Point2D(400, 75), new Vector2D(-3, 2));
			 */

			CreatePoloron(PoloronId.Create(0), new Point2D(100, 50), new Vector2D((float)0.0, 0), PoloronState.Neutral);
			// CreatePoloron(PoloronId.Create(1), new Point2D(150, 100), new Vector2D(2, 2), PoloronState.Negative);
			CreatePoloron(PoloronId.Create(2), new Point2D(200, 150), new Vector2D(0, 0), PoloronState.Positive);
			// CreatePoloron(PoloronId.Create(2), new Point2D(400, 350), new Vector2D(0, 0), PoloronState.Positive);
			CreateGate(new Point2D(380, 280), new Vector2D((float)0.0, (float)0.0));

			renderer.Polorons = polorons;
			renderer.Gate = gate;
			InitializeGameTick();

			return mainForm;
		}

		private static void InitializeInputController()
		{
			serviceManager.Get<IPoloronInputController>().Initialize(renderer.Surface);
		}
	}
}
