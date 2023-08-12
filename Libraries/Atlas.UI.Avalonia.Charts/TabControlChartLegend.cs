using Atlas.Core;
using Atlas.UI.Avalonia.Charts.LiveCharts;
using Atlas.UI.Avalonia.Themes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using LiveChartsCore;

namespace Atlas.UI.Avalonia.Charts;

public abstract class TabControlChartLegend<TSeries> : Grid
{
	public TabControlLiveChart TabControlChart;
	public ListGroup ListGroup => TabControlChart.ListGroup;

	public List<TabChartLegendItem<TSeries>> LegendItems = new();
	protected readonly Dictionary<string, TabChartLegendItem<TSeries>> _idxLegendItems = new();

	protected readonly ScrollViewer _scrollViewer;
	protected readonly WrapPanel _wrapPanel;
	protected readonly TextBlock? _textBlockTotal;

	public event EventHandler<EventArgs>? OnSelectionChanged;
	public event EventHandler<EventArgs>? OnVisibleChanged;

	public override string? ToString() => ListGroup.ToString();

	public TabControlChartLegend(TabControlLiveChart tabControlChart)
	{
		TabControlChart = tabControlChart;

		HorizontalAlignment = HorizontalAlignment.Stretch;
		VerticalAlignment = VerticalAlignment.Stretch;

		_wrapPanel = new WrapPanel()
		{
			Orientation = ListGroup.Horizontal ? Orientation.Horizontal : Orientation.Vertical,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Margin = new Thickness(6),
		};

		_scrollViewer = new ScrollViewer()
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Content = _wrapPanel,
		};

		Children.Add(_scrollViewer);

		if (ListGroup.ShowLegend && ListGroup.ShowOrder && !ListGroup.Horizontal)
		{
			_textBlockTotal = new TextBlock()
			{
				Foreground = AtlasTheme.BackgroundText,
				Margin = new Thickness(2, 2, 2, 2),
				HorizontalAlignment = HorizontalAlignment.Right,
			};
		}

		RefreshModel();
	}

	private string GetTotalName()
	{
		var seriesType = SeriesType.Other;

		foreach (var series in ListGroup.Series)
		{
			if (seriesType == SeriesType.Other)
				seriesType = series.SeriesType;
			else if (series.SeriesType != seriesType)
				return "Total";
		}

		return seriesType.ToString();
	}

	abstract public TabChartLegendItem<TSeries> AddSeries(ChartSeries<TSeries> chartSeries);

	// Show items in order of count, retaining original order for unused values
	private void UpdatePositions()
	{
		_wrapPanel.Children.Clear();
		if (_textBlockTotal != null)
			_wrapPanel.Children.Add(_textBlockTotal);

		var nonzero = new List<TabChartLegendItem<TSeries>>();
		var unused = new List<TabChartLegendItem<TSeries>>();
		foreach (TabChartLegendItem<TSeries> legendItem in _idxLegendItems.Values)
		{
			if (legendItem.Count > 0)
				nonzero.Add(legendItem);
			else
				unused.Add(legendItem);
		}

		var ordered = nonzero.OrderByDescending(a => a.Total).ToList();
		ordered.AddRange(unused);
		if (ListGroup.ShowLegend && ListGroup.ShowOrder && !ListGroup.Horizontal)
		{
			for (int i = 0; i < ordered.Count; i++)
				ordered[i].Index = i + 1;
		}
		_wrapPanel.Children.AddRange(ordered);
	}

	protected void SelectLegendItem(TabChartLegendItem<TSeries> legendItem)
	{
		int selectedCount = 0;
		foreach (TabChartLegendItem<TSeries> item in LegendItems)
		{
			if (item.IsSelected)
				selectedCount++;
		}

		if (legendItem.IsSelected == false || selectedCount > 1)
		{
			SetAllVisible(false);
			legendItem.IsSelected = true;
			//OnSelectionChanged?.Invoke(this, legendItem.oxyListSeries);
		}
		else
		{
			SetAllVisible(true);
		}

		UpdateVisibleSeries();
		OnSelectionChanged?.Invoke(this, EventArgs.Empty);
		//if (legendItem.checkBox.IsChecked == true)
		//SetSelectionAll(legendItem.checkBox.IsChecked == true);
	}

	public void SelectSeries(TSeries series, ListSeries listSeries)
	{
		if (listSeries.Name == null)
			return;

		if (_idxLegendItems.TryGetValue(listSeries.Name, out TabChartLegendItem<TSeries>? legendItem))
		{
			SelectLegendItem(legendItem);
		}
	}

	/*public void HighlightSeries(TSeries oxySeries)
	{
		if (oxySeries.Title == null)
			return;

		// Clear all first before setting to avoid event race conditions
		foreach (TabChartLegendItem<TSeries> item in LegendItems)
			item.Highlight = false;

		if (_idxLegendItems.TryGetValue(oxySeries.Title, out TabChartLegendItem<TSeries>? legendItem))
		{
			foreach (TabChartLegendItem<TSeries> item in LegendItems)
				item.Highlight = (legendItem == item);
		}
		UpdateVisibleSeries();
	}*/

	public void SetAllVisible(bool selected, bool update = false)
	{
		bool changed = false;
		foreach (TabChartLegendItem<TSeries> legendItem in LegendItems)
		{
			changed |= (legendItem.IsSelected != selected);
			legendItem.IsSelected = selected;
		}

		if (update && changed)
		{
			UpdateVisibleSeries();
			OnSelectionChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public void RefreshModel()
	{
		//if (PlotView!.Model == null)
			//return;

		_wrapPanel.Children.Clear();
		foreach (ChartSeries<ISeries> chartSeries in TabControlChart.ChartSeries)
		{
			string title = chartSeries.ToString();
			if (title == null)
				continue;

			if (!_idxLegendItems.TryGetValue(title, out TabChartLegendItem<TSeries>? legendItem))
			{
				// Can't cast
				if (chartSeries is ChartSeries<TSeries> tSeries)
				{
					legendItem = AddSeries(tSeries);
				}
				else
				{
					// Shouldn't ever get here?
					legendItem = AddSeries(new ChartSeries<TSeries>(chartSeries.ListSeries, (TSeries)chartSeries.LineSeries));
				}
			}
			else
			{
				legendItem.UpdateTotal();
			}

			if (!_wrapPanel.Children.Contains(legendItem))
				_wrapPanel.Children.Add(legendItem);
		}
		UpdatePositions();

		if (_textBlockTotal != null)
		{
			_textBlockTotal.Text = ListGroup.LegendTitle ?? GetTotalName();
		}

		// Possibly faster? But more likely to cause problems
		/*var prevLegends = idxLegendItems.Clone<Dictionary<string, TabChartLegendItem>>();
		idxLegendItems = new Dictionary<string, TabChartLegendItem>();
		int row = 0;
		foreach (var series in plotView.Model.Series)
		{
			TabChartLegendItem legendItem;
			if (!prevLegends.TryGetValue(series.Title, out legendItem))
			{
				legendItem = AddSeries(series);
				prevLegends.Remove(series.Title);
			}
			idxLegendItems.Add(series.Title, legendItem);
			Grid.SetRow(legendItem, row++);
		}*/

		//Dispatcher.UIThread.InvokeAsync(() => PlotView.Model?.InvalidatePlot(true), DispatcherPriority.Background);
	}

	public void Unload()
	{
		_wrapPanel.Children.Clear();
		_idxLegendItems.Clear();
		LegendItems.Clear();
	}

	abstract public void UpdateVisibleSeries();

	protected void LegendItem_SelectionChanged(object? sender, EventArgs e)
	{
		UpdateVisibleSeries();
		OnSelectionChanged?.Invoke(this, EventArgs.Empty);
	}

	protected void LegendItem_VisibleChanged(object? sender, EventArgs e)
	{
		UpdateVisibleSeries();
		OnVisibleChanged?.Invoke(this, EventArgs.Empty);
	}

	public void UnhighlightAll(bool update = false)
	{
		foreach (TabChartLegendItem<TSeries> item in LegendItems)
		{
			item.Highlight = false;
		}

		if (update)
			UpdateVisibleSeries();
	}

	public void UpdateHighlight(bool showFaded)
	{
		foreach (TabChartLegendItem<TSeries> item in LegendItems)
		{
			item.UpdateHighlight(showFaded);
		}
	}
}

