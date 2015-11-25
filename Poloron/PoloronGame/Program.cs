using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

using Clifton.CoreSemanticTypes;
using Clifton.ExtensionMethods;
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
		public static IAppConfigService cfgSvc;
		public static GameLevels gameLevels;
		public static Random rnd;
		public static int currentLevel;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			// AllocConsole();
			rnd = new Random(DateTime.Now.Millisecond);
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
			levelStarting = true;
			Start();
		}

		private static Form InitializeGame()
		{
			renderer = serviceManager.Get<IPoloronRenderingService>();
			physics = serviceManager.Get<IPoloronPhysicsService>();
			cfgSvc = serviceManager.Get<IAppConfigService>();
			Form mainForm = renderer.CreateForm();
			mainForm.Text = "Poloron";
			LoadLevels();
			currentLevel = cfgSvc.GetValue("StartingLevel").to_i();
			InitializeLevel(currentLevel);
			InitializeGameTick();

			return mainForm;
		}

		private static void InitializeInputController()
		{
			serviceManager.Get<IPoloronInputController>().Initialize(renderer.Surface);
		}

		private static void LoadLevels()
		{
			XmlSerializer serializer = new XmlSerializer(typeof(GameLevels));
			XmlTextReader xtr = new XmlTextReader("Levels.xml");
			gameLevels = (GameLevels)serializer.Deserialize(xtr);
			xtr.Close();
		}

		private static void InitializeLevel(int lvl)
		{
			polorons.Clear();
			// Player always starts here.
			CreatePoloron(PoloronId.Create(0), new Point2D(100, 50), new Vector2D(0, 0), PoloronState.Neutral);
			// Gate is always at center.
			CreateGate(new Point2D(380, 280), new Vector2D((float)0.0, (float)0.0));
			
			Level level = gameLevels.Levels.Single(l => l.Id == lvl);

			for (int i = 1; i <= level.Polorons; i++)
			{
				Point2D pos;

				if (level.Positions.Count == 0)
				{
					pos = new Point2D(GetPoloronStart(renderer.Surface.Width), GetPoloronStart(renderer.Surface.Height));
				}
				else
				{
					pos = new Point2D(level.Positions[i - 1].X, level.Positions[i - 1].Y);
				}

				Vector2D vel = new Vector2D(0, 0);

				if (level.Moving)
				{
					vel.X = (float)(rnd.Next(6) - 2.5);
					vel.Y = (float)(rnd.Next(6) - 2.5);
				}

				Poloron p = CreatePoloron(PoloronId.Create(i), pos, vel, rnd.Next(2)==0 ? PoloronState.Negative : PoloronState.Positive);

				if (!level.Moving)
				{
					p.Mass = 10000;
				}
			}

			if (level.GateMoving)
			{
				gate.Velocity.X = (float)(rnd.Next(6) - 2.5);
				gate.Velocity.Y = (float)(rnd.Next(6) - 2.5);
			}

			renderer.Polorons = polorons;
			renderer.Gate = gate;
			renderer.Energy = 1000;
		}

		/// <summary>
		/// Return a random value between 40 to range-40, excluding gate location at center of screen.
		/// </summary>
		private static int GetPoloronStart(int range)
		{
			int n = 0;

			do
			{
				n = rnd.Next(40, range - 40);
			} while (n.Between(range / 2 - 40, range / 2 + 40));

			return n;
		}
	}
}
