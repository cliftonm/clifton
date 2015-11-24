using System;
using System.Drawing;
using System.Windows.Forms;

using Clifton.ExtensionMethods;

using PoloronInterfaces;

namespace PoloronRenderingService
{
	public class GraphicsPanel : Panel
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

		protected Brush[] poloronBrushes = new Brush[3];
		protected Color neutralColor;
		protected Color negativeColor;
		protected Color positiveColor;
		protected Color gridColor;
		protected Pen gridPen = new Pen(Color.Black, 1);
		protected Brush neutralBrush;
		protected Brush negativeBrush;
		protected Brush positiveBrush;
		protected PoloronRenderer renderer;

		public GraphicsPanel(PoloronRenderer renderer, Color backColor)
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
		}

		protected void DrawGrid(Graphics gr)
		{
			DrawVerticalLines(gr);
			DrawHorizontalLines(gr);
		}

		protected void DrawPolorons(Graphics gr)
		{
			renderer.Polorons.ForEach(p => gr.FillEllipse(poloronBrushes[(int)p.State], p.LeftEdge, p.TopEdge, p.Diameter, p.Diameter));
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
