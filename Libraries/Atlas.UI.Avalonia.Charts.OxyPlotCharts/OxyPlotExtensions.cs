using OxyPlot;
using System.Drawing;

namespace Atlas.UI.Avalonia.Charts.OxyPlotCharts;

public static class OxyPlotExtensions
{
	public static OxyColor ToOxyColor(this Color color)
	{
		return OxyColor.FromArgb(color.A, color.R, color.G, color.B);
	}
}
