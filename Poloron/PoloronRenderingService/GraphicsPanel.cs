using System;
using System.Drawing;
using System.Windows.Forms;

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

		protected Color gridColor;
		protected Pen gridPen = new Pen(Color.Black, 1);

		public GraphicsPanel(Color backColor)
		{
			DoubleBuffered = true;
			BackgroundBrush = new SolidBrush(backColor);
		}

		public void DrawGrid(Graphics gr)
		{
			DrawVerticalLines(gr);
			DrawHorizontalLines(gr);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			e.Graphics.FillRectangle(BackgroundBrush, RectRegion);
			DrawGrid(e.Graphics);
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
