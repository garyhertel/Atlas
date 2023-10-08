using Atlas.Core;
using Atlas.Extensions;
using Atlas.Tabs;
using Atlas.UI.Avalonia.Themes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
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

public class TabControlLiveChart : TabControlChart<ISeries>
{
	//private static Color timeTrackerColor = Theme.TitleBackground;

	public CartesianChart Chart;

	public TabControlChartLegend<ISeries> Legend;

	public ChartSeries<ISeries>? HoverSeries;

	/*public Axis? LinearAxis;
	public DateTimeAxis? DateTimeAxis;*/
	public Axis XAxis { get; set; }
	public Axis ValueAxis { get; set; } // left/right?

	private List<RectangularSection> _sections = new();
	private RectangularSection? _trackerSection;
	private RectangularSection? _zoomSection;

	private static readonly SKColor GridLineColor = SKColor.Parse("#333333");
	private static readonly SKColor TooltipBackgroundColor = SKColor.Parse("#102670").WithAlpha(185);
	//private static readonly SKColor TooltipBackgroundColor = SKColor.Parse(AtlasTheme.ChartBackgroundSelected.Color.AsSkColor().WithAlpha((byte)200));

	public List<LiveChartSeries> LiveChartSeries { get; private set; } = new();

	private ChartPoint? _pointClicked;
	public Point? CursorPosition;

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
			LegendPosition = LegendPosition.Hidden,
			TooltipBackgroundPaint = new SolidColorPaint(TooltipBackgroundColor),
			TooltipTextPaint = new SolidColorPaint(AtlasTheme.TitleForeground.Color.AsSkColor()),
			Tooltip = new LiveChartTooltip2(this),
			TooltipFindingStrategy = TooltipFindingStrategy.CompareAllTakeClosest,
			MinWidth = 150,
			MinHeight = 120,
			AnimationsSpeed = TimeSpan.Zero,
			[Grid.RowProperty] = 1,
		};
		Chart.ChartPointPointerDown += Chart_ChartPointPointerDown;

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

		AddMouseListeners();

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
			Stroke = new SolidColorPaint(AtlasTheme.GridBackgroundSelected.Color.AsSkColor()),
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

	private void InvalidateChart()
	{
		Dispatcher.UIThread.Post(() => Chart!.InvalidateVisual(), DispatcherPriority.Background);
	}

	private void Chart_ChartPointPointerDown(IChartView chart, ChartPoint? point)
	{
		_pointClicked = point;
		if (point == null) return;

		if (IdxNameToSeries.TryGetValue(point!.Context.Series.Name!, out var series))
		{
			OnSelectionChanged(new SeriesSelectedEventArgs(new List<ListSeries>() { series.ListSeries }));
			Legend.SelectSeries(series.LineSeries, series.ListSeries);
		}
	}

	private Axis CreateXAxis()
	{
		return new Axis
		{
			//Labeler,
			//UnitWidth = TimeSpan.FromDays(1).Ticks,

			ShowSeparatorLines = true,
			SeparatorsPaint = new SolidColorPaint(GridLineColor),
			LabelsPaint = new SolidColorPaint(SKColors.LightGray),
			TextSize = 14,
		};
	}

	public Axis CreateValueAxis()//AxisPosition axisPosition = AxisPosition.Left, string? key = null)
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
		axis.SeparatorsPaint = new SolidColorPaint(GridLineColor);
		axis.LabelsPaint = new SolidColorPaint(SKColors.LightGray);
		axis.TextSize = 14;

		//axis.Name = "Amount";
		//axis.NamePadding = new Padding(0, 15);
		//axis.UnitWidth = 1000000000;
		/*axis.LabelsPaint = new SolidColorPaint
		{
			Color = SKColors.Blue,
			FontFamily = "Times New Roman",
			SKFontStyle = new SKFontStyle(SKFontStyleWeight.ExtraBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic)
		};*/

		return axis;

		/*
		axis.Position = axisPosition;
		//axis.MinStep = 20;
		axis.IsAxisVisible = true;
		axis.IsPanEnabled = false;
		axis.AxislineColor = GridLineColor;
		axis.AxislineStyle = LineStyle.Solid;
		axis.AxislineThickness = 2;
		axis.TickStyle = TickStyle.Outside;
		axis.TicklineColor = GridLineColor;
		axis.TitleColor = OxyColors.LightGray;
		axis.TextColor = OxyColors.LightGray;

		if (key != null)
			axis.Key = key;
		*/
	}
	public void UpdateValueAxis() // OxyPlot.Axes.LinearAxis valueAxis, string axisKey = null
	{
		//if (ValueAxis == null)
			return;

		double minimum = double.MaxValue;
		double maximum = double.MinValue;
		bool hasFraction = false;

		foreach (LiveChartSeries series in LiveChartSeries)
		{
			//if (lineSeries.LineStyle == LineStyle.None)
			//	continue;

			foreach (var dataPoint in series.LineSeries.Values)
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
			ValueAxis.UnitWidth = (maximum - minimum) * 0.10;
		}

		/*foreach (OxyPlot.Annotations.Annotation annotation in PlotModel.Annotations)
		{
			if (annotation is OxyPlot.Annotations.LineAnnotation lineAnnotation)
				maximum = Math.Max(lineAnnotation.Y * 1.1, maximum);
		}

		ValueAxis.MinimumMajorStep = hasFraction ? 0 : 1;

		double? minValue = ListGroup.MinValue;
		if (minValue != null)
			minimum = minValue.Value;

		if (ListGroup.Logarithmic)
		{
			ValueAxis.Minimum = minimum * 0.85;
			ValueAxis.Maximum = maximum * 1.15;
		}
		else
		{
			var margin = (maximum - minimum) * MarginPercent;
			if (minimum == maximum)
				margin = Math.Abs(minimum);

			if (margin == 0)
				margin = 1;

			if (minValue != null)
				ValueAxis.Minimum = Math.Max(minimum - margin, minValue.Value - Math.Abs(margin) * 0.05);
			else
				ValueAxis.Minimum = minimum - margin;
			ValueAxis.Maximum = maximum + margin;
		}*/
	}

	public void LoadListGroup(ChartView chartView)
	{
		ChartView = chartView;
		ReloadView();
		Refresh();
	}

	public void Refresh()
	{
		//UpdateValueAxis();
		//UpdateLinearAxis();

		Legend.RefreshModel();

		//PlotView!.InvalidatePlot(true);
	}

	public void ReloadView()
	{
		ChartView.SortByTotal();

		Chart.Series = ChartView.Series
			.Select(s => AddListSeries(s)!)
			.ToList();

		UpdateValueAxis();
		/*AddAxis();
		UpdateLinearAxis();*/
		//if (ListGroup.TimeWindow == null)
		{
			UpdateDateTimeAxisRange();
		}

		IsVisible = true;
	}

	public ISeries? AddListSeries(ListSeries listSeries, Color? defaultColor = null)
	{
		if (ChartSeries.Count >= SeriesLimit)
			return null;

		Color color = 
			defaultColor ?? 
			listSeries.Color?.AsAvaloniaColor() ?? 
			GetColor(ChartSeries.Count);

		var liveChartSeries = new LiveChartSeries(this, listSeries, color, UseDateTimeAxis);
		_xAxisPropertyInfo = liveChartSeries.XAxisPropertyInfo;

		var chartSeries = new ChartSeries<ISeries>(listSeries, liveChartSeries.LineSeries, color);
		LiveChartSeries.Add(liveChartSeries);
		ChartSeries.Add(chartSeries);
		ListToTabSeries[listSeries.List] = listSeries;
		if (listSeries.Name != null)
			IdxNameToSeries[listSeries.Name] = chartSeries;
		return liveChartSeries.LineSeries;
	}

	public override void AddAnnotation(ChartAnnotation chartAnnotation)
	{
		base.AddAnnotation(chartAnnotation);
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
			Stroke = new SolidColorPaint(color.WithAlpha(200)),
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
			//xAxis.IntervalLength = 75;
			//xAxis.StringFormat = null;
			////UpdateDateTimeInterval(timeWindow.Duration.TotalSeconds);
		}
		else
		{
			XAxis.MinLimit = timeWindow.StartTime.Ticks;
			XAxis.MaxLimit = timeWindow.EndTime.Ticks;
			////DateTimeAxis.MinLimit = OxyPlot.Axes.DateTimeAxis.ToDouble(endTime.AddSeconds(duration / 25.0)); // labels get clipped without this
			UpdateDateTimeInterval(timeWindow.Duration);
		}
	}

	private void UpdateDateTimeInterval(TimeSpan windowDuration)
	{
		var dateFormat = DateTimeFormat.GetDateTimeFormat(windowDuration)!;

		XAxis.Labeler = value => new DateTime((long)value).ToString(dateFormat.TextFormat);// "yyyy-M-d H:mm:ss.FFF");
		XAxis.UnitWidth = windowDuration.PeriodDuration(20).Ticks; // Hover depends on this
		//XAxis.MinStep = dateFormat.StepSize.Ticks;
		XAxis.MinStep = windowDuration.PeriodDuration(10).Ticks;
		//XAxis.ForceStepToMin = true;
		//XAxis.MinimumMajorStep = dateFormat.StepSize.TotalDays;

		//double widthPerLabel = 6 * DateTimeAxis.StringFormat.Length + 25;
		//XAxis.IntervalLength = Math.Max(50, widthPerLabel);
	}

	private void UpdateDateTimeAxisRange()
	{
		if (XAxis == null || !UseDateTimeAxis)
			return;

		double minimum = double.MaxValue;
		double maximum = double.MinValue;

		foreach (ISeries series in Chart.Series)
		{
			if (series is LineSeries<LiveChartPoint> lineSeries)
			{
				// if (!lineSeries.IsVisible) continue;

				foreach (LiveChartPoint dataPoint in lineSeries.Values!)
				{
					double? x = dataPoint.X;
					if (x == null || double.IsNaN(x.Value))
						continue;

					minimum = Math.Min(minimum, x.Value);
					maximum = Math.Max(maximum, x.Value);
				}
			}
		}

		if (minimum != double.MaxValue)
		{
			XAxis.MinLimit = minimum;
			XAxis.MaxLimit = maximum;
		}

		if (ChartView.TimeWindow == null && minimum != double.MaxValue)
		{
			DateTime startTime = new DateTime((long)minimum);
			DateTime endTime = new DateTime((long)maximum);

			ChartView.TimeWindow = new TimeWindow(startTime, endTime).Trim();
		}

		UpdateDateTimeAxis(ChartView.TimeWindow);

		//UpdateDateTimeInterval(double duration);
	}

	private bool _selecting;
	private Point _startScreenPoint;
	private LvcPointD? _startDataPoint;
	private LvcPointD? _endDataPoint;

	private void AddMouseListeners()
	{
		Chart.PointerPressed += TabControlLiveChart_PointerPressed;
		Chart.PointerReleased += TabControlLiveChart_PointerReleased;
		Chart.PointerMoved += TabControlLiveChart_PointerMoved;

		//Chart.MouseLeave += PlotModel_MouseLeave;
	}

	private void TabControlLiveChart_PointerMoved(object? sender, global::Avalonia.Input.PointerEventArgs e)
	{
		// store the mouse down point, check it when mouse button is released to determine if the context menu should be shown
		var point = e.GetPosition(Chart);
		CursorPosition = point;
		try
		{
			ChartPoint? hitPoint = FindClosestPoint(new LvcPoint(point.X, point.Y), 30);
			if (hitPoint != null)
			{
				if (hitPoint.Context.Series.Name is string name)
				{
					Legend.HighlightSeries(name);
					if (IdxNameToSeries.TryGetValue(name, out ChartSeries<ISeries>? series))
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

			UpdateMouseSelection(dataPoint);
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex);
		}
	}

	private ChartPoint? FindClosestPoint(LvcPoint pointerPosition, double maxDistance)
	{
		return LiveChartSeries
			.SelectMany(s => s.LineSeries.Fetch(Chart.CoreChart))
			.Select(x => new { distance = LiveChartLineSeries.GetDistanceTo(x, pointerPosition), point = x })
			.Where(x => x.distance < maxDistance)
			.MinBy(x => x.distance)
			?.point;
	}

	private void TabControlLiveChart_PointerPressed(object? sender, global::Avalonia.Input.PointerPressedEventArgs e)
	{
		var point = e.GetPosition(Chart);
		_startScreenPoint = point;
		_startDataPoint = Chart!.ScalePixelsToData(new LvcPointD(point.X, point.Y));

		if (!_selecting)// || _startDataPoint == null)
		{
			if (_zoomSection != null)
			{
				UpdateMouseSelection(_startDataPoint!.Value);
				_zoomSection.IsVisible = true;
			}
			_selecting = true;
			e.Handled = true;
		}
	}

	private void TabControlLiveChart_PointerReleased(object? sender, global::Avalonia.Input.PointerReleasedEventArgs e)
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

	private void UpdateMouseSelection(LvcPointD endDataPoint)
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
			UpdateDateTimeAxisRange();

		XAxis.MinLimit = Math.Max(left, XAxis.MinLimit!.Value);
		XAxis.MaxLimit = Math.Min(right, XAxis.MaxLimit!.Value);

		DateTime startTime = new DateTime((long)XAxis.MinLimit!.Value);
		DateTime endTime = new DateTime((long)XAxis.MaxLimit.Value);
		var timeWindow = new TimeWindow(startTime, endTime).Trim();

		UpdateDateTimeAxis(timeWindow);
		if (ChartView.TimeWindow != null)
			ChartView.TimeWindow.Select(timeWindow);
		else
			UpdateTimeWindow(timeWindow);
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

		//PlotView!.InvalidatePlot(true);
	}

	private void Legend_OnSelectionChanged(object? sender, EventArgs e)
	{
		StopSelecting();
		UpdateValueAxis();
		OnSelectionChanged(new SeriesSelectedEventArgs(SelectedSeries));
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
			//PlotModel.InvalidatePlot(false);
			PlotView.InvalidateArrange();
			Legend.InvalidateArrange();
		}
	}

	public override void Render(DrawingContext context)
	{
		Dispatcher.UIThread.Post(UpdateVisible, DispatcherPriority.Background);
		base.Render(context);
	}

	// Anchor the chart to the top and stretch to max height, available size gets set to max :(
	protected override Size MeasureOverride(Size availableSize)
	{
		Size size = base.MeasureOverride(availableSize);
		if (FillHeight)
			size = size.WithHeight(Math.Max(size.Height, Math.Min(MaxHeight, availableSize.Height)));
		return size;
	}

	private void PlotView_PointerExited(object? sender, PointerEventArgs e)
	{
		if (HoverSeries != null)
		{
			HoverSeries = null;
			Legend.UnhighlightAll(true);
		}
	}

	private void Legend_OnVisibleChanged(object? sender, EventArgs e)
	{
		UpdateValueAxis();
	}

	public void Unload()
	{
		IsVisible = false;
		UnloadModel();
	}

	public OxyPlot.Axes.DateTimeAxis AddDateTimeAxis(TimeWindow? timeWindow = null)
	{
		DateTimeAxis = new OxyPlot.Axes.DateTimeAxis
		{
			Position = AxisPosition.Bottom,
			//MinorIntervalType = DateTimeIntervalType.Days,
			//IntervalType = DateTimeIntervalType.Days,
			IntervalType = DateTimeIntervalType.Hours,
			MajorGridlineStyle = LineStyle.Solid,
			MajorGridlineColor = GridLineColor,
			//MinorGridlineStyle = LineStyle.None,
			IntervalLength = 75,
			IsAxisVisible = true,
			IsPanEnabled = false,
			AxislineColor = OxyColors.Black,
			//AxislineColor = GridLineColor,
			AxislineStyle = LineStyle.Solid,
			AxislineThickness = 2,
			TickStyle = TickStyle.Outside,
			TicklineColor = GridLineColor,
			//MajorTickSize = 5,
			MinorGridlineColor = OxyColors.Gray,
			//MinorTicklineColor = GridLineColor,
			//MinorTickSize = 5,
			AxisTickToLabelDistance = 2,
			//MinimumMajorStep = TimeSpan.FromSeconds(1).TotalDays,
			TitleColor = OxyColors.LightGray,
			TextColor = OxyColors.LightGray,
		};

		if (timeWindow != null)
		{
			UpdateDateTimeAxis(timeWindow);
		}

		PlotModel!.Axes.Add(DateTimeAxis);
		return DateTimeAxis;
	}

	private void AddLinearAxis()
	{
		LinearAxis = new OxyPlot.Axes.LinearAxis
		{
			Position = AxisPosition.Bottom,
			MajorGridlineStyle = LineStyle.Solid,
			MajorGridlineColor = GridLineColor,
			TitleColor = OxyColors.LightGray,
			TextColor = OxyColors.LightGray,
			TicklineColor = GridLineColor,
			MinorGridlineColor = OxyColors.Gray,
		};
		PlotModel!.Axes.Add(LinearAxis);
	}

	private void UpdateLinearAxis()
	{
		if (LinearAxis == null)
			return;

		double minimum = double.MaxValue;
		double maximum = double.MinValue;

		foreach (OxyPlot.Series.Series series in PlotModel!.Series)
		{
			if (series is OxyPlot.Series.LineSeries lineSeries)
			{
				if (lineSeries.LineStyle == LineStyle.None)
					continue;

				foreach (var dataPoint in lineSeries.Points)
				{
					double x = dataPoint.X;
					if (double.IsNaN(x))
						continue;

					minimum = Math.Min(minimum, x);
					maximum = Math.Max(maximum, x);
				}
			}
		}

		if (minimum == double.MaxValue)
		{
			// didn't find any values
			minimum = 0;
			maximum = 1;
		}

		LinearAxis.Minimum = minimum;
		LinearAxis.Maximum = maximum;
	}

	private void ClearListeners()
	{
		if (Legend != null)
		{
			Legend.OnSelectionChanged -= Legend_OnSelectionChanged;
			Legend.OnVisibleChanged -= Legend_OnVisibleChanged;
		}

		OnMouseCursorChanged -= TabControlChart_OnMouseCursorChanged;
	}

	private void UnloadModel()
	{
		PlotView!.Model = null;
		LinearAxis = null;
		DateTimeAxis = null;

		Legend?.Unload();

		ClearSeries();

		//if (plotModel != null)
		//	plotModel.Series.Clear();
		/*foreach (ListSeries listSeries in ChartSettings.ListSeries)
		{
			INotifyCollectionChanged iNotifyCollectionChanged = listSeries.iList as INotifyCollectionChanged;
			//if (iNotifyCollectionChanged != null)
			//	iNotifyCollectionChanged.CollectionChanged -= INotifyCollectionChanged_CollectionChanged;
		}*//*
	}

	private void ClearSeries()
	{
		PlotModel?.Series.Clear();

		OxyListSeriesList.Clear();
		ListToTabSeries.Clear();
		IdxNameToSeries.Clear();
	}

	public void MergeGroup(ListGroup listGroup)
	{
		var prevListSeries = IdxNameToSeries;
		ClearSeries();

		ListGroup.Series = listGroup.Series;
		ListGroup.TimeWindow = listGroup.TimeWindow ?? ListGroup.TimeWindow;
		ListGroup.SortByTotal();

		foreach (var series in ListGroup.Series)
		{
			Color? oxyColor = null;
			if (series.Name != null && prevListSeries.TryGetValue(series.Name, out OxyPlotChartSeries? prevSeries))
				oxyColor = ((TabChartLineSeries)prevSeries.OxySeries).Color;

			AddSeries(series, oxyColor);
		}
	}

	// Hide cursor when out of scope
	private void PlotModel_MouseLeave(object? sender, OxyMouseEventArgs e)
	{
		var moveEvent = new MouseCursorMovedEventArgs(0);
		_mouseCursorChangedEventSource?.Raise(sender, moveEvent);
	}

	private void INotifyCollectionChanged_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		lock (PlotModel.SyncRoot)
		{
			//Update();
			int index = ListToTabIndex[(IList)sender];
			ListSeries listSeries = ListToTabSeries[(IList)sender];
			AddPoints((OxyPlot.Series.LineSeries)plotModel.Series[index], listSeries, e.NewItems);
		}

		Dispatcher.UIThread.InvokeAsync(() => PlotModel.InvalidatePlot(true), DispatcherPriority.Background);
	}

	public void Dispose()
	{
		ClearListeners();
		UnloadModel();
	}*/
}
