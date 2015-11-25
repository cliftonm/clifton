using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Clifton.ExtensionMethods;

using PoloronInterfaces;

namespace PoloronRenderingService
{
	public class EnergyBar : Panel
	{
		public Brush BackgroundBrush { get; set; }
		public Rectangle RectRegion { get { return new Rectangle(0, 0, Width, Height); } }
		
		protected PoloronRenderer renderer;
		protected Brush energyBrush;
		protected Brush borderBrush;

		public EnergyBar(PoloronRenderer renderer, Color backColor)
		{
			this.renderer = renderer;
			DoubleBuffered = true;
			BackgroundBrush = new SolidBrush(backColor);
			energyBrush = new SolidBrush(Color.LightGreen);
			borderBrush = new SolidBrush(Color.DarkGreen);			
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			e.Graphics.FillRectangle(BackgroundBrush, RectRegion);
			Rectangle border = RectRegion;
			border.Offset(3, 3);
			border.Size = border.Size - new Size(3, 6);
			e.Graphics.FillRectangle(borderBrush, border);

			// energy is 0 to 1000.
			Rectangle energyRect = border;
			energyRect.Offset(1, 1);
			energyRect.Size = energyRect.Size - new Size((1000 - renderer.Energy) * border.Width / 1000 + 2, 2);
			e.Graphics.FillRectangle(energyBrush, energyRect);
		}
	}
}
