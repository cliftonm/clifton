using System;
using System.Drawing;
using System.Linq;

using Clifton.ExtensionMethods;

namespace PoloronRenderingService
{
	public static class ExtensionMethods
	{
		public static Color ToColor(this string colorText)
		{
			int[] rgb = colorText.Split(',').Select(t=>t.to_i()).ToArray();
			Color color = Color.FromArgb(rgb[0], rgb[1], rgb[2]);

			return color;
		}
	}
}
