using Atlas.Core;
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

	public override void UpdateVisibleSeries()
	{
		if (PlotView!.Model == null)
			return;

		base.UpdateVisibleSeries();

		// Update axis for new visible
		Dispatcher.UIThread.InvokeAsync(() => PlotView.Model?.InvalidatePlot(true), DispatcherPriority.Background);
	}

	public override void UpdateHighlight(bool showFaded)
	{
		base.UpdateHighlight(showFaded);

		Dispatcher.UIThread.InvokeAsync(() => PlotView?.Model?.InvalidatePlot(true), DispatcherPriority.Background);
	}
}
