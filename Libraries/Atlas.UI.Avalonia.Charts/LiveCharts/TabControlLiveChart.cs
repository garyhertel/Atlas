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
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Themes;
using SkiaSharp;
using System.Diagnostics;

namespace Atlas.UI.Avalonia.Charts.LiveCharts;

public class TabControlLiveChart : TabControlChart<ISeries>
{
	//private static readonly Color NowColor = Colors.Green;
	//private static Color timeTrackerColor = Theme.TitleBackground;

	public CartesianChart Chart;

	public TabControlChartLegend<ISeries> Legend;

	public ChartSeries<ISeries>? HoverSeries;

	/*public OxyPlot.Axes.LinearAxis? LinearAxis;
	public OxyPlot.Axes.DateTimeAxis? DateTimeAxis;*/
	public Axis ValueAxis { get; set; } // left/right?
	public Axis XAxis { get; set; }

	private RectangularSection? _trackerAnnotation;
	private RectangularSection? _zoomSection;

	private static readonly SKColor GridLineColor = SKColor.Parse("#333333");
	//private readonly LvcColor[] colors = ColorPalletes.FluentDesign;

	public List<LiveChartSeries> LiveChartSeries { get; private set; } = new();

	public TabControlLiveChart(TabInstance tabInstance, ChartView chartView, bool fillHeight = false) : 
		base(tabInstance, chartView, fillHeight)
	{
		XAxis = GetXAxis();
		ValueAxis = GetValueAxis();
		Chart = new CartesianChart()
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,

			//Series = ListGroup.Series.Select(s => AddListSeries(s)).ToList(),
			XAxes = new List<Axis> { XAxis },
			YAxes = new List<Axis> { ValueAxis },
			LegendPosition = LegendPosition.Hidden,
			TooltipBackgroundPaint = new SolidColorPaint(AtlasTheme.ChartBackgroundSelected.Color.AsSkColor().WithAlpha(64)),
			TooltipTextPaint = new SolidColorPaint(AtlasTheme.TitleForeground.Color.AsSkColor()),
			//Tooltip = new LiveChartTooltip(this),
			//TooltipFindingStrategy = TooltipFindingStrategy.CompareOnlyXTakeClosest, // All doesn't work well
			TooltipFindingStrategy = TooltipFindingStrategy.CompareAllTakeClosest,
			//MinWidth = 150,
			//MinHeight = 80,
			AnimationsSpeed = TimeSpan.Zero,
			[Grid.RowProperty] = 1,
		};
		Chart.ChartPointPointerDown += Chart_ChartPointPointerDown;

		/*PlotView = new PlotView()
		{
			Background = Brushes.Transparent,
			BorderBrush = Brushes.LightGray,
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
		if (ChartView!.Horizontal)
		{
			// Bottom
			SetRow(Legend, 2);
			Legend.MaxHeight = 100;
		}
		else
		{
			// Right Side
			SetRow(Legend, 1);
			SetColumn(Legend, 1);
			Legend.MaxWidth = 300;
		}
		containerGrid.Children.Add(Legend);
		//Legend.OnSelectionChanged += Legend_OnSelectionChanged;
		//Legend.OnVisibleChanged += Legend_OnVisibleChanged;

		OnMouseCursorChanged += TabControlChart_OnMouseCursorChanged;
		if (ChartView.TimeWindow != null)
			ChartView.TimeWindow.OnSelectionChanged += TimeWindow_OnSelectionChanged;

		if (UseDateTimeAxis)
		{
			AddNowTime();
		}

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
				//LabelSize = 14,
				/*LabelPaint = new SolidColorPaint(color.WithAlpha(220))
				{
					SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Bold),
				},*/
				Stroke = new SolidColorPaint(skColor.WithAlpha(180)),
				Fill = new SolidColorPaint(skColor.WithAlpha((byte)AtlasTheme.ChartBackgroundSelectedAlpha)),
				IsVisible = false,
			};
			_sections.Add(_zoomSection);

			AddMouseListeners();
		}

		Chart.Sections = _sections;

		Children.Add(containerGrid);
	}

	private void TimeWindow_OnSelectionChanged(object? sender, TimeWindowEventArgs e)
	{
		UpdateTimeWindow(e.TimeWindow);
	}

	private List<RectangularSection> _sections = new();

	private RectangularSection CreateTrackerLine()
	{
		_trackerAnnotation = new RectangularSection
		{
			Label = "",
			//LabelSize = 14,
			/*LabelPaint = new SolidColorPaint(color.WithAlpha(220))
			{
				SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Bold),
			},*/
			Stroke = new SolidColorPaint(AtlasTheme.GridBackgroundSelected.Color.AsSkColor()),
		};
		return _trackerAnnotation;
	}

	// Update mouse tracker
	private void TabControlChart_OnMouseCursorChanged(object? sender, MouseCursorMovedEventArgs e)
	{
		if (_trackerAnnotation == null) // sender == Chart |
			return;

		_trackerAnnotation.Xi = e.X;
		_trackerAnnotation.Xj = e.X;
		Dispatcher.UIThread.Post(() => Chart!.InvalidateVisual(), DispatcherPriority.Background);
	}

	private void Chart_ChartPointPointerDown(IChartView chart, LiveChartsCore.Kernel.ChartPoint? point)
	{
		if (point == null) return;

		// todo: check for background click
		if (IdxNameToSeries.TryGetValue(point!.Context.Series.Name!, out var series))
		{
			OnSelectionChanged(new SeriesSelectedEventArgs(new List<ListSeries>() { series.ListSeries }));
			Legend.SelectSeries(series.LineSeries, series.ListSeries);
		}
	}

	private Axis GetXAxis()
	{
		return new Axis
		{
			//Labeler,
			//UnitWidth = TimeSpan.FromDays(1).Ticks,

			ShowSeparatorLines = true,
			SeparatorsPaint = new SolidColorPaint(GridLineColor),
			LabelsPaint = new SolidColorPaint(SKColors.LightGray),
			//CrosshairPaint = new SolidColorPaint(SKColors.LightGray),
		};
	}

	public Axis GetValueAxis()//AxisPosition axisPosition = AxisPosition.Left, string? key = null)
	{
		return new Axis
		{
			//Name = "Amount",
			//NamePadding = new Padding(0, 15),
			Labeler = DateTimeFormat.ValueFormatter,
			LabelsPaint = new SolidColorPaint(SKColors.LightGray),
			SeparatorsPaint = new SolidColorPaint(GridLineColor),
			//UnitWidth = 1000000000,
			/*LabelsPaint = new SolidColorPaint
			{
				Color = SKColors.Blue,
				FontFamily = "Times New Roman",
				SKFontStyle = new SKFontStyle(SKFontStyleWeight.ExtraBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic)
			},*/
		};

		/*if (ListGroup.Logarithmic)
		{
			ValueAxis = new OxyPlot.Axes.LogarithmicAxis();
		}
		else
		{
			ValueAxis = new OxyPlot.Axes.LinearAxis()
			{
				IntervalLength = 25,
			};
		}

		ValueAxis.Position = axisPosition;
		ValueAxis.MajorGridlineStyle = LineStyle.Solid;
		ValueAxis.MinorGridlineStyle = LineStyle.None;
		//ValueAxis.MinorStep = 20;
		//ValueAxis.MajorStep = 10;
		//ValueAxis.MinimumMinorStep = 20;
		ValueAxis.MinorTickSize = 0;
		ValueAxis.IsAxisVisible = true;
		ValueAxis.IsPanEnabled = false;
		ValueAxis.AxislineColor = GridLineColor;
		ValueAxis.AxislineStyle = LineStyle.Solid;
		ValueAxis.AxislineThickness = 2;
		ValueAxis.TickStyle = TickStyle.Outside;
		ValueAxis.TicklineColor = GridLineColor;
		//ValueAxis.MajorTickSize = 2;
		ValueAxis.MinorGridlineColor = OxyColors.Gray;
		ValueAxis.TitleColor = OxyColors.LightGray;
		ValueAxis.TextColor = OxyColors.LightGray;

		if (key != null)
			ValueAxis.Key = key;
		PlotModel!.Axes.Add(ValueAxis);
		return ValueAxis;*/
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
		/*
		PlotModel = new PlotModel()
		{
			PlotAreaBorderColor = Color.Parse("#888888"),
			TextColor = OxyColors.Black,
			SelectionColor = OxyColors.Blue,
		};*/

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

		Color color = defaultColor ?? GetColor(ChartSeries.Count);

		var liveChartSeries = new LiveChartSeries(this, listSeries, color, UseDateTimeAxis);
		_xAxisPropertyInfo = liveChartSeries.XAxisPropertyInfo;
		//lineSeries.LineSeries.ChartPointPointerHover += LineSeries_ChartPointPointerHover;
		liveChartSeries.Hover += LineSeries_Hover;
		liveChartSeries.HoverLost += LineSeries_HoverLost;

		var chartSeries = new ChartSeries<ISeries>(listSeries, liveChartSeries.LineSeries, color);
		LiveChartSeries.Add(liveChartSeries);
		ChartSeries.Add(chartSeries);
		ListToTabSeries[listSeries.List] = listSeries;
		if (listSeries.Name != null)
			IdxNameToSeries[listSeries.Name] = chartSeries;
		return liveChartSeries.LineSeries;
	}

	private void LineSeries_Hover(object? sender, SeriesHoverEventArgs e)
	{
		string name = e.Series.Name!;
		Legend.HighlightSeries(name);
		if (IdxNameToSeries.TryGetValue(name, out ChartSeries<ISeries>? series))
		{
			HoverSeries = series;
		}
	}

	private void LineSeries_HoverLost(object? sender, SeriesHoverEventArgs e)
	{
		if (HoverSeries?.ListSeries == e.Series)
		{
			Legend.UnhighlightAll(true);
		}
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
			if (series is LineSeries<ObservablePoint> lineSeries)
			{
				//if (lineSeries.LineStyle == LineStyle.None)
				//	continue;

				foreach (ObservablePoint dataPoint in lineSeries.Values!)
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
		PointerPressed += TabControlLiveChart_PointerPressed;
		PointerReleased += TabControlLiveChart_PointerReleased;
		PointerMoved += TabControlLiveChart_PointerMoved;

		//Chart!.PointerMoved += TabControlLiveChart_PointerMoved;

		//PlotModel.MouseMove += PlotModel_MouseMove;
		//PlotModel.MouseUp += PlotModel_MouseUp;
		//PlotModel.MouseLeave += PlotModel_MouseLeave;
	}

	private void TabControlLiveChart_PointerMoved(object? sender, global::Avalonia.Input.PointerEventArgs e)
	{
		//e.Pointer.Capture(this); // Steals focus

		// store the mouse down point, check it when mouse button is released to determine if the context menu should be shown
		var point = e.GetPosition(this);
		try
		{
			LvcPointD dataPoint = Chart!.ScalePixelsToData(new LvcPointD(point.X, point.Y));

			var moveEvent = new MouseCursorMovedEventArgs(dataPoint.X);
			_mouseCursorChangedEventSource?.Raise(sender, moveEvent);

			//Debug.WriteLine("pointer moved" + sender.GetType());

			UpdateMouseSelection(dataPoint);
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex);
		}
	}

	private void TabControlLiveChart_PointerPressed(object? sender, global::Avalonia.Input.PointerPressedEventArgs e)
	{
		if (!_selecting)// || _startDataPoint == null)
		{
			var point = e.GetPosition(this);
			_startScreenPoint = point;
			_startDataPoint = Chart!.ScalePixelsToData(new LvcPointD(point.X, point.Y));
			_zoomSection!.IsVisible = true;
			_selecting = true;
			e.Handled = true;
		}
	}

	private void TabControlLiveChart_PointerReleased(object? sender, global::Avalonia.Input.PointerReleasedEventArgs e)
	{
		if (_selecting && _startDataPoint != null)
		{
			var point = e.GetPosition(this);
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
	}

	private void UpdateMouseSelection(LvcPointD endDataPoint)
	{
		/*if (_zoomSection == null)
		{
			_zoomSection = new RectangularSection
			{
				Label = "",
				//LabelSize = 14,
				Stroke = new SolidColorPaint(SKColors.LightCoral),
				Fill = new SolidColorPaint(SKColors.LightCoral.WithAlpha(200)),
			};
		}*/

		/*try
		{
			if (!_sections.Contains(_zoomSection))
				_sections.Add(_zoomSection);
		}
		catch (Exception)
		{
		}*/
		if (_zoomSection == null || _startDataPoint == null) return;

		_zoomSection.Xi = Math.Min(_startDataPoint!.Value.X, endDataPoint.X);
		_zoomSection.Xj = Math.Max(_startDataPoint.Value.X, endDataPoint.X);

		Debug.WriteLine($"Start: {_zoomSection.Xi}, End: {_zoomSection.Xj}");

		Dispatcher.UIThread.Post(() => Chart!.InvalidateVisual(), DispatcherPriority.Background);
	}

	private void StopSelecting()
	{
		_zoomSection!.IsVisible = false;
		_startDataPoint = null;
		//if (_zoomSection != null)
		//	PlotModel!.Annotations.Remove(_zoomSection);
		_selecting = false;
	}

	private void ZoomIn()
	{
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
		//PlotView.Model.InvalidatePlot(true);
	}

	/*private void AddAxis()
	{
		if (UseDateTimeAxis)
		{
			AddDateTimeAxis(ListGroup.TimeWindow);
			AddNowTime();
			if (ListGroup.ShowTimeTracker)
				AddTrackerLine();

			AddMouseListeners();
		}
		else
		{
			AddLinearAxis();
		}

		AddValueAxis();
	}

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

	// Anchor the chart to the top and stretch to max height, available size gets set to max :(
	protected override Size MeasureOverride(Size availableSize)
	{
		Size size = base.MeasureOverride(availableSize);
		if (FillHeight)
			size = size.WithHeight(Math.Max(size.Height, Math.Min(MaxHeight, availableSize.Height)));
		return size;
	}

	public override void Render(DrawingContext context)
	{
		Dispatcher.UIThread.Post(UpdateVisible, DispatcherPriority.Background);
		base.Render(context);
	}

	public class MouseHoverManipulator : TrackerManipulator
	{
		public TabControlChart Chart;

		public MouseHoverManipulator(TabControlChart chart)
			: base(chart.PlotView)
		{
			Chart = chart;
			LockToInitialSeries = false;
			Snap = true;
			PointsOnly = false;
		}

		public override void Delta(OxyMouseEventArgs e)
		{
			base.Delta(e);

			var series = PlotView.ActualModel.GetSeriesFromPoint(e.Position, 20);
			if (Chart.HoverSeries == series)
				return;

			if (series != null)
			{
				Chart.Legend.HighlightSeries(series);
			}
			else
			{
				Chart.Legend.UnhighlightAll(true);
			}
			Chart.HoverSeries = series;

			// todo: replace tracker here
		}
	}

	private void PlotView_PointerExited(object? sender, PointerEventArgs e)
	{
		if (HoverSeries != null)
		{
			HoverSeries = null;
			Legend.UnhighlightAll(true);
		}
	}

	private void Legend_OnSelectionChanged(object? sender, EventArgs e)
	{
		StopSelecting();
		UpdateValueAxis();
		OnSelectionChanged?.Invoke(sender, new SeriesSelectedEventArgs(SelectedSeries));
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
