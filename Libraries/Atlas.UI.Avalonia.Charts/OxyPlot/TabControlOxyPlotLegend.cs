using Atlas.Core;
using Avalonia.Media;
using Avalonia.Threading;
using OxyPlot.Avalonia;

namespace Atlas.UI.Avalonia.Charts;

public class TabControlOxyPlotLegend : TabControlChartLegend<OxyPlotLineSeries>
{
	public TabControlOxyPlot OxyChart;
	public PlotView? PlotView => OxyChart.PlotView;

	public override string? ToString() => ListGroup.ToString();

	public TabControlOxyPlotLegend(TabControlOxyPlot tabControlChart) : base(tabControlChart)
	{
		OxyChart = tabControlChart;
	}

	public override TabChartLegendItem<OxyPlotLineSeries> AddSeries(ChartSeries<OxyPlotLineSeries> chartSeries)
	{
		OxyPlot.Series.Series series = chartSeries.LineSeries;

		Color color = Colors.Green;
		if (series is OxyPlot.Series.LineSeries lineSeries)
			color = lineSeries.Color.ToColor();
		if (series is OxyPlot.Series.ScatterSeries scatterSeries)
			color = scatterSeries.MarkerFill.ToColor();

		var legendItem = new TabOxyPlotLegendItem(this, chartSeries);
		legendItem.OnSelectionChanged += LegendItem_SelectionChanged;
		legendItem.OnVisibleChanged += LegendItem_VisibleChanged;
		legendItem.TextBlock!.PointerPressed += (s, e) =>
		{
			if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
				SelectLegendItem(legendItem);
		};
		LegendItems.Add(legendItem);
		if (series.Title != null)
			_idxLegendItems.Add(series.Title, legendItem);
		return legendItem;
	}

	public void HighlightSeries(OxyPlot.Series.Series oxySeries)
	{
		if (oxySeries.Title == null)
			return;

		// Clear all first before setting to avoid event race conditions
		foreach (TabOxyPlotLegendItem item in LegendItems)
			item.Highlight = false;

		if (_idxLegendItems.TryGetValue(oxySeries.Title, out TabChartLegendItem<OxyPlotLineSeries>? legendItem))
		{
			foreach (TabOxyPlotLegendItem item in LegendItems)
				item.Highlight = (legendItem == item);
		}
		UpdateVisibleSeries();
	}

	public override void UpdateVisibleSeries()
	{
		if (PlotView!.Model == null)
			return;

		foreach (OxyPlot.Series.Series series in PlotView.Model.Series)
		{
			if (series is OxyPlot.Series.LineSeries lineSeries)
			{
				if (lineSeries.Title == null)
					continue;

				if (_idxLegendItems.TryGetValue(lineSeries.Title, out TabChartLegendItem<OxyPlotLineSeries>? legendItem))
				{
					legendItem.UpdateVisible();
				}
			}

			if (series is OxyPlot.Series.ScatterSeries scatterSeries)
			{
				if (scatterSeries.Title == null)
					continue;

				if (_idxLegendItems.TryGetValue(scatterSeries.Title, out TabChartLegendItem<OxyPlotLineSeries>? legendItem))
				{
					legendItem.UpdateVisible();
				}
			}
		}
		Dispatcher.UIThread.InvokeAsync(() => PlotView.Model?.InvalidatePlot(true), DispatcherPriority.Background);
	}

	public override void UpdateHighlight(bool showFaded)
	{
		base.UpdateHighlight(showFaded);

		Dispatcher.UIThread.InvokeAsync(() => PlotView?.Model?.InvalidatePlot(true), DispatcherPriority.Background);
	}
}
