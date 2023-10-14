using Avalonia.Media;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using System.Diagnostics;

namespace Atlas.UI.Avalonia.Charts.LiveCharts;

public class TabLiveChartLegendItem : TabChartLegendItem<ISeries>
{
	public TabLiveChartLegendItem(TabControlChartLegend<ISeries> legend, ChartSeries<ISeries> chartSeries) : 
		base(legend, chartSeries)
	{
	}

	public override void UpdateColor(Color color)
	{
		if (ChartSeries.LineSeries is LiveChartLineSeries lineSeries)
		{
			var skColor = color.AsSkColor();

			lineSeries.Stroke = new SolidColorPaint(skColor) { StrokeThickness = 2 };
			/*if (lineSeries.GeometryStroke != null)
			{
				lineSeries.GeometryStroke = new SolidColorPaint(skColor) { StrokeThickness = 2 };
			}*/
			if (lineSeries.GeometryFill != null)
			{
				lineSeries.GeometryFill = new SolidColorPaint(skColor);
			}
		}
	}

	public override void UpdateVisible()
	{
		/*if (ChartSeries.LineSeries is LiveChartLineSeries lineSeries)
		{
			if (IsSelected || _highlight)
			{
				UpdateHighlight(!_highlight);
				//lineSeries.IsVisible = true;
			}
			else
			{
				UpdateColor(Colors.Transparent);
				//lineSeries.IsVisible = false;
			}
		}*/
		ChartSeries.LineSeries.IsVisible = IsSelected || Highlight; // Doesn't always remove rendered line
		//Debug.WriteLine($"{ChartSeries}: Visible: {ChartSeries.LineSeries.IsVisible}");
	}
}
