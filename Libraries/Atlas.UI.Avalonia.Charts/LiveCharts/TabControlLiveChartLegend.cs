using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.Avalonia;

namespace Atlas.UI.Avalonia.Charts.LiveCharts;

public class TabControlLiveChartLegend : TabControlChartLegend<ISeries>
{
	public TabControlLiveChart LiveChart;
	public CartesianChart Chart => LiveChart.Chart;

	public TabControlLiveChartLegend(TabControlLiveChart tabControlChart) : base(tabControlChart)
	{
		LiveChart = tabControlChart;
	}

	public override TabChartLegendItem<ISeries> AddSeries(ChartSeries<ISeries> chartSeries)
	{
		ISeries lineSeries = chartSeries.LineSeries;

		var legendItem = new TabLiveChartLegendItem(this, chartSeries);
		legendItem.OnSelectionChanged += LegendItem_SelectionChanged;
		legendItem.OnVisibleChanged += LegendItem_VisibleChanged;
		legendItem.TextBlock!.PointerPressed += (s, e) =>
		{
			if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
			{
				SelectLegendItem(legendItem);
			}
		};
		LegendItems.Add(legendItem);
		if (lineSeries.Name != null)
		{
			_idxLegendItems.Add(lineSeries.Name, legendItem);
		}
		return legendItem;
	}
}

