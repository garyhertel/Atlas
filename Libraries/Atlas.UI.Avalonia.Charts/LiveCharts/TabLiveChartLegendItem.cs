using ExCSS;
using LiveChartsCore;

namespace Atlas.UI.Avalonia.Charts.LiveCharts;

public class TabLiveChartLegendItem : TabChartLegendItem<ISeries>
{
	public TabLiveChartLegendItem(TabControlChartLegend<ISeries> legend, ChartSeries<ISeries> chartSeries) : base(legend, chartSeries)
	{
	}

	public override void UpdateVisible(ISeries lineSeries)
	{
		Series = lineSeries;
		/*if (IsSelected || _highlight)
		{
			if (Points != null)
			{
				lineSeries.Points.Clear();
				lineSeries.Points.AddRange(Points);
			}
			//lineSeries.ItemsSource = lineSeries.ItemsSource ?? ItemsSource; // never gonna let you go...
			//ItemsSource = null;
			lineSeries.LineStyle = LineStyle.Solid;
			lineSeries.MarkerType = markerType;
			lineSeries.Selectable = true;
		}
		else
		{
			if (lineSeries.Points.Count > 0)
			{
				Points = new List<DataPoint>(lineSeries.Points);
			}
			lineSeries.Points.Clear();
			//lineSeries.Points = new List<DataPoint>();
			//ItemsSource = lineSeries.ItemsSource ?? ItemsSource;
			//lineSeries.ItemsSource = null;
			lineSeries.LineStyle = LineStyle.None;
			lineSeries.MarkerType = MarkerType.None;
			lineSeries.Selectable = false;
			//lineSeries.SelectionMode = OxyPlot.SelectionMode.
			lineSeries.Unselect();
		}*/
	}
}
