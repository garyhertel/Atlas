using LiveChartsCore;

namespace Atlas.UI.Avalonia.Charts.LiveCharts;

public class TabLiveChartLegendItem : TabChartLegendItem<ISeries>
{
	public TabLiveChartLegendItem(TabControlChartLegend<ISeries> legend, ChartSeries<ISeries> chartSeries) : base(legend, chartSeries)
	{
	}

	public override void UpdateVisible()
	{
		ChartSeries.LineSeries.IsVisible = IsSelected || Highlight;
	}
}
