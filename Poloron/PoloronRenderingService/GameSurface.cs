using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Clifton.ExtensionMethods;

using PoloronInterfaces;

namespace PoloronRenderingService
{
	public class GameSurface : Panel
	{
		public Brush BackgroundBrush { get; set; }
		public Rectangle RectRegion { get { return new Rectangle(0, 0, Width, Height); } }
		public int GridSpacing { get; set; }

		public Color GridColor 
		{
			get { return gridColor; }
			set
			{
				gridColor = value;
				gridPen = new Pen(gridColor, 1);
			}
		}

		public Color NeutralColor
		{
			get { return neutralColor; }
			set
			{
				neutralColor = value;
				neutralBrush = new SolidBrush(neutralColor);
				poloronBrushes[(int)PoloronState.Neutral] = neutralBrush;
			}
		}

		public Color NegativeColor
		{
			get { return negativeColor; }
			set
			{
				negativeColor = value;
				negativeBrush = new SolidBrush(negativeColor);
				poloronBrushes[(int)PoloronState.Negative] = negativeBrush;
			}
		}

		public Color PositiveColor
		{
			get { return positiveColor; }
			set
			{
				positiveColor = value;
				positiveBrush = new SolidBrush(positiveColor);
				poloronBrushes[(int)PoloronState.Positive] = positiveBrush;
			}
		}

		public Color ChargingColor
		{
			get { return chargingColor; }
			set
			{
				chargingColor = value;
				chargingBrush = new SolidBrush(chargingColor);
				poloronBrushes[(int)PoloronState.Charging] = chargingBrush;
			}
		}

		public Color GateColor
		{
			get { return gateColor; }
			set
			{
				gateColor = value;
				gateBrush = new SolidBrush(gateColor);
			}
		}

		protected Brush[] poloronBrushes = new Brush[4];
		protected Brush gateBrush;
		protected Color gateColor;
		protected Color neutralColor;
		protected Color negativeColor;
		protected Color positiveColor;
		protected Color chargingColor;
		protected Color gridColor;
		protected Pen gridPen = new Pen(Color.Black, 1);
		protected Brush neutralBrush;
		protected Brush negativeBrush;
		protected Brush positiveBrush;
		protected Brush chargingBrush;
		protected PoloronRenderer renderer;

		public GameSurface(PoloronRenderer renderer, Color backColor)
		{
			this.renderer = renderer;
			DoubleBuffered = true;
			BackgroundBrush = new SolidBrush(backColor);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			e.Graphics.FillRectangle(BackgroundBrush, RectRegion);
			DrawGrid(e.Graphics);
			DrawPolorons(e.Graphics);
			DrawGate(e.Graphics);
		}

		protected void DrawGrid(Graphics gr)
		{
			DrawVerticalLines(gr);
			DrawHorizontalLines(gr);
		}

		protected void DrawPolorons(Graphics gr)
		{
			renderer.Polorons.Where(p => p.Visible).ForEach(p => gr.FillEllipse(poloronBrushes[(int)p.State], p.LeftEdge, p.TopEdge, p.Diameter, p.Diameter));
		}

		protected void DrawGate(Graphics gr)
		{
			if (renderer.Gate.Visible)
			{
				gr.FillEllipse(gateBrush, renderer.Gate.LeftEdge, renderer.Gate.TopEdge, renderer.Gate.Diameter, renderer.Gate.Diameter);
			}
		}

		protected void DrawVerticalLines(Graphics gr)
		{
			for (int x = GridSpacing; x < Width; x += GridSpacing)
			{
				gr.DrawLine(gridPen, x, 0, x, Height);
			}
		}

		protected void DrawHorizontalLines(Graphics gr)
		{
			for (int y = GridSpacing; y < Height; y += GridSpacing)
			{
				gr.DrawLine(gridPen, 0, y, Width, y);
			}
		}
	}
}
