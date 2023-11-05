using Atlas.Core;
using Atlas.Extensions;
using Atlas.Tabs;
using Atlas.UI.Avalonia.Controls;
using Atlas.UI.Avalonia.Themes;
using Atlas.UI.Avalonia.View;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Skia;
using Avalonia.Threading;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Diagnostics;

namespace Atlas.UI.Avalonia.Charts.LiveCharts;

public class LiveChartCreator : IControlCreator
{
	public static void Register()
	{
		TabView.ControlCreators[typeof(ChartView)] = new LiveChartCreator();
	}

	public void AddControl(TabInstance tabInstance, TabControlSplitContainer container, object obj)
	{
		var chartView = (ChartView)obj;

		var tabChart = new TabControlLiveChart(tabInstance, chartView, true);

		container.AddControl(tabChart, true, SeparatorType.Spacer);
	}
}

public class TabControlLiveChart : TabControlChart<ISeries>, IDisposable
{
	public static SKColor TimeTrackerSkColor = TimeTrackerColor.ToSKColor();
	public static SKColor GridLineSkColor = GridLineColor.ToSKColor();
	public static SKColor TextSkColor = TextColor.ToSKColor();
	public static SKColor TooltipBackgroundColor = SKColor.Parse("#102670").WithAlpha(225);

	public CartesianChart Chart;

	public TabControlChartLegend<ISeries> Legend;

	/*public Axis? LinearAxis;
	public DateTimeAxis? DateTimeAxis;*/
	public Axis XAxis { get; set; }
	public Axis ValueAxis { get; set; } // left/right?

	public List<LiveChartSeries> LiveChartSeries { get; private set; } = new();

	public ChartSeries<ISeries>? HoverSeries;

	private List<RectangularSection> _sections = new();
	private RectangularSection? _trackerSection;
	private RectangularSection? _zoomSection;

	public Point? CursorPosition;

	private ChartPoint? _pointClicked;

	private bool _selecting;
	private Point _startScreenPoint;
	private LvcPointD? _startDataPoint;
	private LvcPointD? _endDataPoint;

	public TabControlLiveChart(TabInstance tabInstance, ChartView chartView, bool fillHeight = false) : 
		base(tabInstance, chartView, fillHeight)
	{
		XAxis = CreateXAxis();
		ValueAxis = CreateValueAxis();

		Chart = new CartesianChart()
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			XAxes = new List<Axis> { XAxis },
			YAxes = new List<Axis> { ValueAxis },
			TooltipBackgroundPaint = new SolidColorPaint(TooltipBackgroundColor),
			TooltipTextPaint = new SolidColorPaint(AtlasTheme.TitleForeground.Color.AsSkColor()),
			TooltipFindingStrategy = TooltipFindingStrategy.CompareAllTakeClosest,
			Tooltip = new LiveChartTooltip(this),
			LegendPosition = LegendPosition.Hidden,
			AnimationsSpeed = TimeSpan.Zero,
			MinWidth = 150,
			MinHeight = 120,
			[Grid.RowProperty] = 1,
		};
		Chart.ChartPointPointerDown += Chart_ChartPointPointerDown;
		Chart.PointerExited += Chart_PointerExited;

		/*PlotView = new PlotView()
		{
			Background = Brushes.Transparent,
			ClipToBounds = false,
		};
		ClipToBounds = true; // Slows things down too much without this, could possible change while tracker visible?
		*/
		ReloadView();

		var containerGrid = new Grid()
		{
			ColumnDefinitions = new ColumnDefinitions("*,Auto"),
			RowDefinitions = new RowDefinitions("Auto,*,Auto"),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Background = AtlasTheme.TabBackground, // grid lines look bad when hovering without this
		};

		if (TitleTextBlock != null)
		{
			containerGrid.Children.Add(TitleTextBlock);
		}
		else
		{
			containerGrid.RowDefinitions[0].Height = new GridLength(0);
		}

		containerGrid.Children.Add(Chart);

		Legend = new TabControlLiveChartLegend(this);
		if (ChartView!.LegendPosition == ChartLegendPosition.Bottom)
		{
			SetRow(Legend, 2);
			Legend.MaxHeight = 100;
		}
		else if (ChartView!.LegendPosition == ChartLegendPosition.Right)
		{
			SetRow(Legend, 1);
			SetColumn(Legend, 1);
			Legend.MaxWidth = 300;
		}
		else
		{
			Legend.IsVisible = false;
		}
		containerGrid.Children.Add(Legend);
		Legend.OnSelectionChanged += Legend_OnSelectionChanged;
		//Legend.OnVisibleChanged += Legend_OnVisibleChanged;

		OnMouseCursorChanged += TabControlChart_OnMouseCursorChanged;
		if (ChartView.TimeWindow != null)
		{
			ChartView.TimeWindow.OnSelectionChanged += TimeWindow_OnSelectionChanged;
		}

		if (UseDateTimeAxis)
		{
			AddNowTime();
		}
		AddSections();

		AddMouseListeners();

		Children.Add(containerGrid);
	}

	private void AddSections()
	{
		_sections = ChartView.Annotations
			.Select(a => CreateAnnotation(a))
			.ToList();

		if (UseDateTimeAxis && ChartView.ShowTimeTracker)
		{
			_sections.Add(CreateTrackerLine());

			var skColor = AtlasTheme.ChartBackgroundSelected.Color.AsSkColor();
			_zoomSection = new RectangularSection
			{
				Label = "",
				Stroke = new SolidColorPaint(skColor.WithAlpha(180)),
				Fill = new SolidColorPaint(skColor.WithAlpha((byte)AtlasTheme.ChartBackgroundSelectedAlpha)),
				IsVisible = false,
			};
			_sections.Add(_zoomSection);
		}

		Chart.Sections = _sections;
	}

	private void TimeWindow_OnSelectionChanged(object? sender, TimeWindowEventArgs e)
	{
		UpdateTimeWindow(e.TimeWindow);
	}

	private RectangularSection CreateTrackerLine()
	{
		_trackerSection = new RectangularSection
		{
			Label = "",
			Stroke = new SolidColorPaint(TimeTrackerSkColor),
			IsVisible = false,
		};
		return _trackerSection;
	}

	// Update mouse tracker
	private void TabControlChart_OnMouseCursorChanged(object? sender, MouseCursorMovedEventArgs e)
	{
		if (_trackerSection == null)
			return;

		_trackerSection.Xi = e.X;
		_trackerSection.Xj = e.X;
		_trackerSection.IsVisible = true;

		InvalidateChart();
	}

	public override void InvalidateChart()
	{
		UpdateAxis();
		Dispatcher.UIThread.Post(() => Chart!.InvalidateVisual(), DispatcherPriority.Background);
	}

	private void Chart_ChartPointPointerDown(IChartView chart, ChartPoint? point)
	{
		_pointClicked = point;
		if (point == null) return;

		if (IdxNameToChartSeries.TryGetValue(point!.Context.Series.Name!, out var series))
		{
			OnSelectionChanged(new SeriesSelectedEventArgs(new List<ListSeries>() { series.ListSeries }));
			Legend.SelectSeries(series.LineSeries, series.ListSeries);
		}
	}

	private void Chart_PointerExited(object? sender, PointerEventArgs e)
	{
		if (HoverSeries != null)
		{
			HoverSeries = null;
			Legend.UnhighlightAll(true);
		}

		// Hide cursor when out of scope
		var moveEvent = new MouseCursorMovedEventArgs(0);
		_mouseCursorChangedEventSource?.Raise(sender, moveEvent);
	}

	private Axis CreateXAxis()
	{
		return new Axis
		{
			ShowSeparatorLines = true,
			SeparatorsPaint = new SolidColorPaint(GridLineSkColor),
			LabelsPaint = new SolidColorPaint(TextSkColor),
			TextSize = 14,
		};
	}

	public Axis CreateValueAxis() // AxisPosition axisPosition = AxisPosition.Left, string? key = null)
	{
		Axis axis;
		if (ChartView.Logarithmic)
		{
			// Doesn't work yet
			axis = new LogaritmicAxis(10);
		}
		else
		{
			axis = new Axis();
		}

		axis.Padding = new Padding(10, 2);
		axis.Labeler = NumberExtensions.FormattedShortDecimal;
		axis.SeparatorsPaint = new SolidColorPaint(GridLineSkColor);
		axis.LabelsPaint = new SolidColorPaint(TextSkColor);
		axis.TextSize = 14;

		return axis;

		/*
		axis.Name = "Amount";
		axis.NamePadding = new Padding(0, 15);
		axis.UnitWidth = 1000000000;
		axis.LabelsPaint = new SolidColorPaint
		{
			Color = SKColors.Blue,
			FontFamily = "Times New Roman",
			SKFontStyle = new SKFontStyle(SKFontStyleWeight.ExtraBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic)
		};
		axis.Position = axisPosition;
		axis.IsAxisVisible = true;
		axis.AxislineColor = GridLineColor;
		axis.AxislineStyle = LineStyle.Solid;
		axis.AxislineThickness = 2;
		axis.TickStyle = TickStyle.Outside;
		axis.TicklineColor = GridLineColor;

		if (key != null)
			axis.Key = key;
		*/
	}

	public void UpdateValueAxis() // Axis valueAxis, string axisKey = null
	{
		if (ValueAxis == null)
			return;

		double minimum = double.MaxValue;
		double maximum = double.MinValue;
		bool hasFraction = false;

		foreach (LiveChartSeries series in LiveChartSeries)
		{
			if (!series.LineSeries.IsVisible) continue;

			foreach (var dataPoint in series.LineSeries.Values!)
			{
				double? y = dataPoint.Y;
				if (y == null || double.IsNaN(y.Value))
					continue;

				if (XAxis != null && (dataPoint.X < XAxis.MinLimit || dataPoint.X > XAxis.MaxLimit))
					continue;

				hasFraction |= (y % 1 != 0.0);

				minimum = Math.Min(minimum, y.Value);
				maximum = Math.Max(maximum, y.Value);
			}
		}

		if (minimum == double.MaxValue)
		{
			// didn't find any values
			minimum = 0;
			maximum = 1;
		}
		else
		{
			double difference = maximum - minimum;
			if (difference > 10 || hasFraction)
			{
				ValueAxis.UnitWidth = (difference * 0.2).RoundToSignificantFigures(1);
			}
		}

		foreach (var annotation in Annotations)
		{
			if (annotation.Y != null)
			{
				maximum = Math.Max(annotation.Y.Value * 1.1, maximum);
			}
		}

		ValueAxis.MinStep = hasFraction ? 0 : 1;

		double? minValue = ChartView.MinValue;
		if (minValue != null)
			minimum = minValue.Value;

		if (ChartView.Logarithmic)
		{
			ValueAxis.MinLimit = minimum * 0.85;
			ValueAxis.MaxLimit = maximum * 1.15;
		}
		else
		{
			var margin = (maximum - minimum) * MarginPercent;
			if (minimum == maximum)
				margin = Math.Abs(minimum);

			if (margin == 0)
				margin = 1;

			if (minValue != null)
			{
				ValueAxis.MinLimit = Math.Max(minimum - margin, minValue.Value - Math.Abs(margin) * 0.05);
			}
			else
			{
				ValueAxis.MinLimit = minimum - margin;
			}
			ValueAxis.MaxLimit = maximum + margin;
		}
	}

	public void LoadView(ChartView chartView)
	{
		ChartView = chartView;
		ReloadView();
		Legend.RefreshModel();
	}

	public void Refresh()
	{
		UpdateAxis();

		Legend.RefreshModel();

		//InvalidateChart();
	}

	public override void ReloadView()
	{
		ChartView.SortByTotal();

		Chart.Series = ChartView.Series
			.Take(SeriesLimit)
			.Select(s => AddListSeries(s))
			.ToList();

		UpdateAxis();

		IsVisible = true;
	}

	private void UpdateAxis()
	{
		UpdateValueAxis();
		UpdateLinearAxis();
		//if (ChartView.TimeWindow == null)
		{
			UpdateDateTimeAxisRange();
		}
	}

	private Color? GetSeriesColor(ListSeries listSeries)
	{
		if (listSeries.Name != null && IdxNameToChartSeries.TryGetValue(listSeries.Name, out ChartSeries<ISeries>? prevSeries))
			return prevSeries.Color;
		return null;
	}

	public ISeries AddListSeries(ListSeries listSeries, Color? defaultColor = null)
	{
		Color color = 
			defaultColor ?? 
			listSeries.Color?.AsAvaloniaColor() ??
			GetSeriesColor(listSeries) ??
			GetColor(ChartSeries.Count);

		var liveChartSeries = new LiveChartSeries(this, listSeries, color, UseDateTimeAxis);
		XAxisPropertyInfo = listSeries.XPropertyInfo;

		var chartSeries = new ChartSeries<ISeries>(listSeries, liveChartSeries.LineSeries, color);
		LiveChartSeries.Add(liveChartSeries);
		ChartSeries.Add(chartSeries);
		IdxListToListSeries[listSeries.List] = listSeries;
		if (listSeries.Name != null)
			IdxNameToChartSeries[listSeries.Name] = chartSeries;
		return liveChartSeries.LineSeries;
	}

	public override void AddAnnotation(ChartAnnotation chartAnnotation)
	{
		base.AddAnnotation(chartAnnotation);

		_sections.Add(CreateAnnotation(chartAnnotation));

		UpdateValueAxis();
	}

	public RectangularSection CreateAnnotation(ChartAnnotation chartAnnotation)
	{
		var c = chartAnnotation.Color!.Value;
		var color = new SKColor(c.R, c.G, c.B, c.A);
		var section = new RectangularSection
		{
			Label = chartAnnotation.Text ?? "",
			LabelSize = 14,
			LabelPaint = new SolidColorPaint(color.WithAlpha(220))
			{
				SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Bold),
			},
			Stroke = new SolidColorPaint(color.WithAlpha(200), (float)chartAnnotation.StrokeThickness),
		};

		if (chartAnnotation.X != null)
		{
			section.Xj = chartAnnotation.X;
			section.Xi = chartAnnotation.X;
		}

		if (chartAnnotation.Y != null)
		{
			section.Yj = chartAnnotation.Y;
			section.Yi = chartAnnotation.Y;
		}

		return section;
	}

	private void UpdateDateTimeAxis(TimeWindow? timeWindow)
	{
		if (timeWindow == null)
		{
			XAxis.MinLimit = null;
			XAxis.MaxLimit = null;
			//UpdateDateTimeInterval(timeWindow.Duration.TotalSeconds);
		}
		else
		{
			XAxis.MinLimit = timeWindow.StartTime.Ticks;
			XAxis.MaxLimit = timeWindow.EndTime.Ticks;
			UpdateDateTimeInterval(timeWindow.Duration);
		}
	}

	private void UpdateDateTimeInterval(TimeSpan windowDuration)
	{
		var dateFormat = DateTimeFormat.GetDateTimeFormat(windowDuration)!;

		TimeSpan duration = windowDuration.PeriodDuration(7);

		XAxis.Labeler = value => new DateTime((long)value).ToString(dateFormat.TextFormat);
		XAxis.UnitWidth = duration.Ticks; // Hover depends on this
		XAxis.MinStep = duration.Ticks;
	}

	private void UpdateDateTimeAxisRange()
	{
		if (XAxis == null || !UseDateTimeAxis)
			return;

		var (minimum, maximum, hasFraction) = GetXValueRange();

		/*if (minimum != double.MaxValue)
		{
			XAxis.MinLimit = minimum;
			XAxis.MaxLimit = maximum;
		}*/

		if (ChartView.TimeWindow == null && minimum != double.MaxValue)
		{
			DateTime startTime = new DateTime((long)minimum);
			DateTime endTime = new DateTime((long)maximum);

			ChartView.TimeWindow = new TimeWindow(startTime, endTime).Trim();
		}

		UpdateDateTimeAxis(ChartView.TimeWindow?.Selection ?? ChartView.TimeWindow);

		//UpdateDateTimeInterval(double duration);
	}

	private void UpdateLinearAxis()
	{
		if (XAxis == null || UseDateTimeAxis)
			return;

		var (minimum, maximum, hasFraction) = GetXValueRange();

		if (!hasFraction)
		{
			XAxis.MinStep = 1;
		}

		/*
		if (minimum == double.MaxValue)
		{
			// didn't find any values
			minimum = 0;
			maximum = 1;
		}

		XAxis.MinLimit = minimum;
		XAxis.MaxLimit = maximum;*/
	}

	private (double minimum, double maximum, bool hasFraction) GetXValueRange()
	{
		double minimum = double.MaxValue;
		double maximum = double.MinValue;
		bool hasFraction = false;

		foreach (ISeries series in Chart.Series)
		{
			if (series is LiveChartLineSeries lineSeries)
			{
				// if (!lineSeries.IsVisible) continue;

				foreach (LiveChartPoint chartPoint in lineSeries.Values!)
				{
					double? x = chartPoint.X;
					if (x == null || double.IsNaN(x.Value))
						continue;

					minimum = Math.Min(minimum, x.Value);
					maximum = Math.Max(maximum, x.Value);

					hasFraction |= (x % 1 != 0.0);
				}
			}
		}
		return (minimum, maximum, hasFraction); 
	}

	private void AddMouseListeners()
	{
		Chart.PointerPressed += TabControlLiveChart_PointerPressed;
		Chart.PointerReleased += TabControlLiveChart_PointerReleased;
		Chart.PointerMoved += TabControlLiveChart_PointerMoved;
	}

	private void TabControlLiveChart_PointerMoved(object? sender, PointerEventArgs e)
	{
		// store the mouse down point, check it when mouse button is released to determine if the context menu should be shown
		var point = e.GetPosition(Chart);
		CursorPosition = point;
		try
		{
			ChartPoint? hitPoint = FindClosestPoint(new LvcPoint(point.X, point.Y), LiveChartLineSeries.MaxFindDistance);
			if (hitPoint != null)
			{
				if (hitPoint.Context.Series.Name is string name)
				{
					Legend.HighlightSeries(name);
					if (IdxNameToChartSeries.TryGetValue(name, out ChartSeries<ISeries>? series))
					{
						HoverSeries = series;
					}
				}
			}
			else
			{
				if (HoverSeries != null)
				{
					HoverSeries = null;
					Legend.UnhighlightAll(true);
				}
			}

			LvcPointD dataPoint = Chart!.ScalePixelsToData(new LvcPointD(point.X, point.Y));

			var moveEvent = new MouseCursorMovedEventArgs(dataPoint.X);
			_mouseCursorChangedEventSource?.Raise(sender, moveEvent);

			UpdateZoomSection(dataPoint);
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex);
		}
	}

	private ChartPoint? FindClosestPoint(LvcPoint pointerPosition, double maxDistance)
	{
		return LiveChartSeries
			.Where(series => series.LineSeries.IsVisible)
			.SelectMany(s => s.LineSeries.Fetch(Chart.CoreChart))
			.Select(x => new { distance = LiveChartLineSeries.GetDistanceTo(x, pointerPosition), point = x })
			.Where(x => x.distance < maxDistance)
			.MinBy(x => x.distance)
			?.point;
	}

	private void TabControlLiveChart_PointerPressed(object? sender, PointerPressedEventArgs e)
	{
		var point = e.GetPosition(Chart);
		_startScreenPoint = point;
		_startDataPoint = Chart!.ScalePixelsToData(new LvcPointD(point.X, point.Y));

		if (!_selecting)
		{
			if (_zoomSection != null)
			{
				UpdateZoomSection(_startDataPoint!.Value);
				_zoomSection.IsVisible = true;
			}
			_selecting = true;
			e.Handled = true;
		}
	}

	private void TabControlLiveChart_PointerReleased(object? sender, PointerReleasedEventArgs e)
	{
		if (_pointClicked != null)
		{
			StopSelecting();
			return;
		}

		if (_selecting && _startDataPoint != null)
		{
			var point = e.GetPosition(Chart);
			_endDataPoint = Chart!.ScalePixelsToData(new LvcPointD(point.X, point.Y));
			double width = Math.Abs(point.X - _startScreenPoint.X);
			if (width > MinSelectionWidth)
			{
				ZoomIn();
			}
			else if (ChartView.TimeWindow?.Selection != null)
			{
				ZoomOut();
			}
			else
			{
				// Deselect All
				Legend.SetAllVisible(true, true);
			}
			StopSelecting();
		}
		else
		{
			// Deselect All
			Legend.SetAllVisible(true, true);
		}
	}

	private void UpdateZoomSection(LvcPointD endDataPoint)
	{
		if (_zoomSection == null || _startDataPoint == null) return;

		_zoomSection.Xi = Math.Min(_startDataPoint!.Value.X, endDataPoint.X);
		_zoomSection.Xj = Math.Max(_startDataPoint.Value.X, endDataPoint.X);

		InvalidateChart();
	}

	private void StopSelecting()
	{
		if (_zoomSection != null)
		{
			_zoomSection!.IsVisible = false;
		}
		_startDataPoint = null;
		_selecting = false;
	}

	private void ZoomIn()
	{
		if (!UseDateTimeAxis) return;

		double left = Math.Min(_startDataPoint!.Value.X, _endDataPoint!.Value.X);
		double right = Math.Max(_startDataPoint.Value.X, _endDataPoint!.Value.X);

		if (XAxis.MinLimit == null || double.IsNaN(XAxis.MinLimit.Value))
		{
			UpdateDateTimeAxisRange();
		}

		XAxis.MinLimit = Math.Max(left, XAxis.MinLimit!.Value);
		XAxis.MaxLimit = Math.Min(right, XAxis.MaxLimit!.Value);

		DateTime startTime = new DateTime((long)XAxis.MinLimit!.Value);
		DateTime endTime = new DateTime((long)XAxis.MaxLimit.Value);
		var timeWindow = new TimeWindow(startTime, endTime).Trim();

		UpdateDateTimeAxis(timeWindow);
		if (ChartView.TimeWindow != null)
		{
			ChartView.TimeWindow.Select(timeWindow);
		}
		else
		{
			UpdateTimeWindow(timeWindow);
		}
	}

	private void ZoomOut()
	{
		if (ChartView.TimeWindow != null)
		{
			UpdateDateTimeAxis(ChartView.TimeWindow);
			ChartView.TimeWindow.Select(null);
		}
		else
		{
			UpdateTimeWindow(null);
		}
	}

	private void UpdateTimeWindow(TimeWindow? timeWindow)
	{
		UpdateDateTimeAxis(timeWindow);
		//UpdateValueAxis();

		ChartView.SortByTotal();
		Legend.RefreshModel();

		//InvalidateChart();
	}

	private void Legend_OnSelectionChanged(object? sender, EventArgs e)
	{
		StopSelecting();
		UpdateValueAxis();
		OnSelectionChanged(new SeriesSelectedEventArgs(SelectedSeries));
	}

	private void ClearListeners()
	{
		if (Legend != null)
		{
			Legend.OnSelectionChanged -= Legend_OnSelectionChanged;
			//Legend.OnVisibleChanged -= Legend_OnVisibleChanged;
		}

		Chart.PointerPressed -= TabControlLiveChart_PointerPressed;
		Chart.PointerReleased -= TabControlLiveChart_PointerReleased;
		Chart.PointerMoved -= TabControlLiveChart_PointerMoved;
		Chart.ChartPointPointerDown -= Chart_ChartPointPointerDown;
		Chart.PointerExited -= Chart_PointerExited;
		OnMouseCursorChanged -= TabControlChart_OnMouseCursorChanged;

		if (ChartView.TimeWindow != null)
		{
			ChartView.TimeWindow.OnSelectionChanged -= TimeWindow_OnSelectionChanged;
		}
	}

	public override void Unload()
	{
		IsVisible = false;
		UnloadModel();
	}

	private void UnloadModel()
	{
		//PlotView!.Model = null;
		//XAxis = null;

		ClearSeries();
	}

	private void ClearSeries()
	{
		//Chart?.Series.Clear();

		Legend?.Unload();

		ChartSeries.Clear();
		LiveChartSeries.Clear();
		IdxListToListSeries.Clear();
		IdxNameToChartSeries.Clear();
	}

	public override void MergeView(ChartView chartView)
	{
		var prevListSeries = IdxNameToChartSeries;
		IdxNameToChartSeries = new();
		ClearSeries();

		ChartView.Series = chartView.Series;
		ChartView.TimeWindow = chartView.TimeWindow ?? ChartView.TimeWindow;
		ChartView.SortByTotal();

		List<ISeries> listSeries = new();
		foreach (var series in ChartView.Series.Take(SeriesLimit))
		{
			Color? color = null;
			if (series.Name != null && prevListSeries.TryGetValue(series.Name, out ChartSeries<ISeries>? prevSeries))
			{
				color = prevSeries.Color;
			}

			listSeries.Add(AddListSeries(series, color));
		}
		Chart.Series = listSeries;
		UpdateAxis();
	}

	public void Dispose()
	{
		ClearListeners();
		UnloadModel();
	}

	/*
	private void UpdateVisible()
	{
		if (PlotView == null)
			return;

		bool visible = AvaloniaUtils.IsControlVisible(this);
		if (visible != PlotView.IsVisible)
		{
			PlotView.IsVisible = visible;
			Legend.IsVisible = visible;
			//InvalidateChart();
			Legend.InvalidateArrange();
		}
	}

	public override void Render(DrawingContext context)
	{
		Dispatcher.UIThread.Post(UpdateVisible, DispatcherPriority.Background);
		base.Render(context);
	}

	private void Legend_OnVisibleChanged(object? sender, EventArgs e)
	{
		UpdateValueAxis();
	}

	private void INotifyCollectionChanged_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		lock (Chart.SyncContext)
		{
			//Update();
			int index = ListToTabIndex[(IList)sender];
			ListSeries listSeries = ListToTabSeries[(IList)sender];
			AddPoints((OxyPlot.Series.LineSeries)plotModel.Series[index], listSeries, e.NewItems);
		}

		InvalidateChart();
	}*/
}
