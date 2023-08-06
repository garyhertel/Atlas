using Avalonia.Media;
using Avalonia.Threading;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.Avalonia;

namespace Atlas.UI.Avalonia.Charts.LiveCharts;

public class TabControlLiveChartLegend : TabControlChartLegend<ISeries>
{
	public CartesianChart Chart;

	public TabControlLiveChartLegend(TabControlChart tabControlChart) : base(tabControlChart)
	{
	}


	public override TabChartLegendItem<ISeries> AddSeries(ChartSeries<ISeries> chartSeries)
	{
		ISeries lineSeries = chartSeries.LineSeries;

		Color color = Colors.Green;
		//if (series is OxyPlot.Series.LineSeries lineSeries)
		//color = lineSeries.Color.ToColor();

		var legendItem = new TabLiveChartLegendItem(this, chartSeries);
		legendItem.OnSelectionChanged += LegendItem_SelectionChanged;
		legendItem.OnVisibleChanged += LegendItem_VisibleChanged;
		legendItem.TextBlock!.PointerPressed += (s, e) =>
		{
			if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
				SelectLegendItem(legendItem);
		};
		LegendItems.Add(legendItem);
		if (lineSeries.Name != null)
			_idxLegendItems.Add(lineSeries.Name, legendItem);
		return legendItem;
	}

	public override void UpdateVisibleSeries()
	{
		if (Chart == null)
			return;

		foreach (ISeries series in Chart.Series)
		{
			//if (series is OxyPlot.Series.LineSeries lineSeries)
			{
				//if (lineSeries.Title == null)
				//	continue;

				if (_idxLegendItems.TryGetValue(series.Name, out TabChartLegendItem<ISeries>? legendItem))
				{
					//legendItem.UpdateVisible(lineSeries);
				}
			}

			/*if (series is OxyPlot.Series.ScatterSeries scatterSeries)
			{
				if (scatterSeries.Title == null)
					continue;

				if (_idxLegendItems.TryGetValue(scatterSeries.Title, out TabChartLegendItem? legendItem))
				{
					legendItem.UpdateVisible(scatterSeries);
				}
			}*/
		}
		//Dispatcher.UIThread.InvokeAsync(() => PlotView.Model?.InvalidatePlot(true), DispatcherPriority.Background);
	}
}

