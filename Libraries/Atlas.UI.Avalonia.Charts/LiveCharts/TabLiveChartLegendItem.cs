using Avalonia.Media;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

namespace Atlas.UI.Avalonia.Charts.LiveCharts;

public class TabLiveChartLegendItem : TabChartLegendItem<ISeries>
{
	public TabLiveChartLegendItem(TabControlChartLegend<ISeries> legend, ChartSeries<ISeries> chartSeries) : 
		base(legend, chartSeries)
	{
	}

	public override void UpdateColor(Color color)
	{
		if (ChartSeries.LineSeries is LineSeries<ObservablePoint> lineSeries)
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
		ChartSeries.LineSeries.IsVisible = IsSelected || Highlight;
	}
}
