using Atlas.Core;
using Atlas.Tabs;
using Atlas.UI.Avalonia.Themes;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
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
using System.Reflection;
using WeakEvent;

namespace Atlas.UI.Avalonia.Charts.LiveCharts;

public class TabControlLiveChart : TabControlChart<ISeries>
{
	//private static readonly Color NowColor = Colors.Green;
	//private static OxyColor timeTrackerColor = Theme.TitleBackground;

	//public ChartSettings ChartSettings { get; set; }

	//private List<ListSeries> ListSeries { get; set; }

	public CartesianChart Chart;

	public TabControlChartLegend<ISeries> Legend;
	private PropertyInfo? _xAxisPropertyInfo;

	/*public OxyPlot.Series.Series? HoverSeries;

	//public SeriesCollection SeriesCollection { get; set; }

	public OxyPlot.Axes.Axis? ValueAxis; // left/right?

	public OxyPlot.Axes.LinearAxis? LinearAxis;
	public OxyPlot.Axes.DateTimeAxis? DateTimeAxis;*/
	public List<Axis> XAxis { get; set; }

	/*private OxyPlot.Annotations.LineAnnotation? _trackerAnnotation;*/

	private static readonly SKColor GridLineColor = SKColor.Parse("#333333");
	private readonly LvcColor[] colors = ColorPalletes.FluentDesign;

	private bool UseDateTimeAxis => (_xAxisPropertyInfo?.PropertyType == typeof(DateTime)) ||
									(ListGroup.TimeWindow != null);

	private static readonly WeakEventSource<MouseCursorMovedEventArgs> _mouseCursorChangedEventSource = new();

	public static event EventHandler<MouseCursorMovedEventArgs> OnMouseCursorChanged
	{
		add { _mouseCursorChangedEventSource.Subscribe(value); }
		remove { _mouseCursorChangedEventSource.Unsubscribe(value); }
	}

	public TabControlLiveChart(TabInstance tabInstance, ListGroup listGroup, bool fillHeight = false) : 
		base(tabInstance, listGroup, fillHeight)
	{
		_xAxisPropertyInfo = listGroup.Series.FirstOrDefault()?.XPropertyInfo;

		Chart = new CartesianChart()
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,

			//Series = ListGroup.Series.Select(s => AddListSeries(s)).ToList(),
			XAxes = XAxis = GetXAxis(),
			YAxes = GetValueAxis(),
			LegendPosition = LegendPosition.Hidden,
			TooltipBackgroundPaint = new SolidColorPaint(AtlasTheme.ChartBackgroundSelected.Color.AsSkColor().WithAlpha(64)),
			TooltipTextPaint = new SolidColorPaint(AtlasTheme.TitleForeground.Color.AsSkColor()),
			//MinWidth = 150,
			//MinHeight = 80,
			[Grid.RowProperty] = 1,
		};
		Chart.ChartPointPointerDown += Chart_ChartPointPointerDown;

		/*PlotView = new PlotView()
		{
			Background = Brushes.Transparent,
			BorderBrush = Brushes.LightGray,
			IsMouseWheelEnabled = false,
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
		if (ListGroup!.Horizontal)
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

		//OnMouseCursorChanged += TabControlChart_OnMouseCursorChanged;
		//if (ListGroup.TimeWindow != null)
		//	ListGroup.TimeWindow.OnSelectionChanged += ListGroup_OnTimesChanged;*/

		Children.Add(containerGrid);
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

	private List<Axis> GetXAxis()
	{
		var axis = new Axis
		{
			//Labeler,
			//UnitWidth = TimeSpan.FromDays(1).Ticks,

			ShowSeparatorLines = true,
			SeparatorsPaint = new SolidColorPaint(GridLineColor),
			LabelsPaint = new SolidColorPaint(SKColors.LightGray),
		};

		return new List<Axis>
		{
			axis
		};
	}

	public List<Axis> GetValueAxis()//AxisPosition axisPosition = AxisPosition.Left, string? key = null)
	{
		return new List<Axis>
		{
			new Axis
			{
				//Name = "Sales amount",
				//NamePadding = new Padding(0, 15),
				Labeler = Labelers.Default,
				LabelsPaint = new SolidColorPaint(SKColors.LightGray),
				SeparatorsPaint = new SolidColorPaint(GridLineColor),
				/*LabelsPaint = new SolidColorPaint
				{
					Color = SKColors.Blue,
					FontFamily = "Times New Roman",
					SKFontStyle = new SKFontStyle(SKFontStyleWeight.ExtraBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic)
				},*/
			}
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
		ValueAxis.LabelFormatter = ValueFormatter;

		if (key != null)
			ValueAxis.Key = key;
		PlotModel!.Axes.Add(ValueAxis);
		return ValueAxis;*/
	}

	private static List<DateTimePoint> GetValues(ListSeries listSeries)
	{
		DateTime now = DateTime.Now;
		return listSeries
			.Values()
			.Select(t => new DateTimePoint(now = now.AddSeconds(1), t))
			.ToList();

		/*DateTime dateTime = DateTime.UtcNow;

		double value = s_random.Next(1, 100);

		var values = new List<DateTimePoint>();
		for (int i = 0; i < DataPointCount; i++)
		{
			value += s_random.Next(-5, 5);
			var point = new DateTimePoint()
			{
				DateTime = dateTime.AddHours(i),
				Value = value,
			};
			values.Add(point);

			// Sparse points require visible markers instead of just lines, which can have performance impacts
			if (sparse && s_random.Next(0, 1) == 1)
			{
				i += s_random.Next(1, 20);

				// Show visible gaps between sparse points
				// Doesn't work (or this is the wrong way)
				/*var nullPoint = new DateTimePoint()
				{
					DateTime = dateTime.AddHours(i),
					Value = null,
				};
				values.Add(nullPoint);*//*
			}
		}
		return values;*/
	}

	public void LoadListGroup(ListGroup listGroup)
	{
		ListGroup = listGroup;
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
			PlotAreaBorderColor = OxyColor.Parse("#888888"),
			TextColor = OxyColors.Black,
			SelectionColor = OxyColors.Blue,
		};*/

		ListGroup.SortByTotal();

		Chart.Series = ListGroup.Series
			.Select(s => AddListSeries(s))
			.ToList();

		/*AddAxis();
		UpdateValueAxis();
		UpdateLinearAxis();*/
		//if (ListGroup.TimeWindow == null)
		{
			UpdateDateTimeAxisRange();
		}

		IsVisible = true;
	}

	public ISeries? AddListSeries(ListSeries listSeries, LvcColor? oxyColor = null)
	{
		if (ChartSeries.Count >= SeriesLimit)
			return null;

		//var nextColorIndex = i % colors.Length;
		//var color = colors[nextColorIndex];

		Color color = GetColor(ChartSeries.Count);

		var lineSeries = new LiveChartSeries(this, listSeries, color, UseDateTimeAxis);
		_xAxisPropertyInfo = lineSeries.XAxisPropertyInfo;

		var chartSeries = new ChartSeries<ISeries>(listSeries, lineSeries.LineSeries, color);
		ChartSeries.Add(chartSeries);
		ListToTabSeries[listSeries.List] = listSeries;
		if (listSeries.Name != null)
			IdxNameToSeries[listSeries.Name] = chartSeries;
		return lineSeries.LineSeries;
	}

	public override void AddAnnotation(ChartAnnotation chartAnnotation)
	{
		base.AddAnnotation(chartAnnotation);

		/*var annotationThreshold = new Anno
		{
			Text = chartAnnotation.Text,
			Type = chartAnnotation.Horizontal ? LineAnnotationType.Horizontal : LineAnnotationType.Vertical,
			X = chartAnnotation.X ?? 0,
			Y = chartAnnotation.Y ?? 0,
			Color = (chartAnnotation.Color ?? chartAnnotation.TextColor)!.Value.ToOxyColor(),
			TextColor = (chartAnnotation.TextColor ?? chartAnnotation.Color)!.Value.ToOxyColor(),
			StrokeThickness = 2,
			LineStyle = LineStyle.Dot,
		};
		PlotModel!.Annotations.Add(annotationThreshold);
		UpdateValueAxis();*/
	}

	/*private void UpdateVisible()
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

	private void AddAxis()
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

		if (ListGroup.Series.Count > 0 && ListGroup.Series[0].IsStacked)
			AddCategoryAxis();
		else
			AddValueAxis();
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
	}*/

	private void UpdateDateTimeAxis(TimeWindow? timeWindow)
	{
		if (timeWindow == null)
		{
			//DateTimeAxis!.Minimum = double.NaN;
			//DateTimeAxis.Maximum = double.NaN;
			//DateTimeAxis.IntervalLength = 75;
			//DateTimeAxis.StringFormat = null;
			////UpdateDateTimeInterval(timeWindow.Duration.TotalSeconds);
		}
		else
		{
			//DateTimeAxis!.Minimum = OxyPlot.Axes.DateTimeAxis.ToDouble(timeWindow.StartTime);
			//DateTimeAxis.Maximum = OxyPlot.Axes.DateTimeAxis.ToDouble(timeWindow.EndTime);
			////DateTimeAxis.Maximum = OxyPlot.Axes.DateTimeAxis.ToDouble(endTime.AddSeconds(duration / 25.0)); // labels get clipped without this
			UpdateDateTimeInterval(timeWindow.Duration.TotalSeconds);
		}
	}

	private void UpdateDateTimeInterval(double duration)
	{
		var dateFormat = DateTimeFormat.GetDateTimeFormat(duration)!;

		XAxis[0].Labeler = value => new DateTime((long)value).ToString(dateFormat.TextFormat);// "yyyy-M-d H:mm:ss.FFF");
		//XAxis.LabelsRotation = 15;
			//axis.UnitWidth = TimeSpan.FromDays(1).Ticks;
		//DateTimeAxis.MinimumMajorStep = dateFormat.StepSize.TotalDays;

		//double widthPerLabel = 6 * DateTimeAxis.StringFormat.Length + 25;
		//DateTimeAxis.IntervalLength = Math.Max(50, widthPerLabel);
	}
	/*
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

	public OxyPlot.Axes.CategoryAxis AddCategoryAxis(AxisPosition axisPosition = AxisPosition.Left, string? key = null)
	{
		categoryAxis = new OxyPlot.Axes.CategoryAxis
		{
			Position = axisPosition,
			IntervalLength = 20,
			MajorGridlineStyle = LineStyle.Solid,
			MajorGridlineColor = GridLineColor,
			MinorGridlineStyle = LineStyle.None,
			MinorTickSize = 0,
			MinorStep = 20,
			MinimumMinorStep = 10,
			IsAxisVisible = true,
			IsPanEnabled = false,
			AxislineColor = GridLineColor,
			AxislineStyle = LineStyle.Solid,
			AxislineThickness = 2,
			TickStyle = TickStyle.Outside,
			TicklineColor = GridLineColor,
			//MajorTickSize = 2,
			MinorGridlineColor = OxyColors.Gray,
			TitleColor = OxyColors.LightGray,
			TextColor = OxyColors.LightGray,
			LabelFormatter = ValueFormatter,
		};
		if (key != null)
			categoryAxis.Key = key;

		foreach (ListSeries listSeries in ListGroup.Series)
		{
			categoryAxis.Labels.Add(listSeries.Name);
		}

		PlotModel!.Axes.Add(categoryAxis);
		return categoryAxis;
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

	public void UpdateValueAxis() // OxyPlot.Axes.LinearAxis valueAxis, string axisKey = null
	{
		if (ValueAxis == null)
			return;

		double minimum = double.MaxValue;
		double maximum = double.MinValue;
		bool hasFraction = false;

		foreach (OxyPlot.Series.Series series in PlotModel!.Series)
		{
			if (series is OxyPlot.Series.LineSeries lineSeries)
			{
				if (lineSeries.LineStyle == LineStyle.None)
					continue;

				foreach (var dataPoint in lineSeries.Points)
				{
					double y = dataPoint.Y;
					if (double.IsNaN(y))
						continue;

					if (DateTimeAxis != null && (dataPoint.X < DateTimeAxis.Minimum || dataPoint.X > DateTimeAxis.Maximum))
						continue;

					hasFraction |= (y % 1 != 0.0);

					minimum = Math.Min(minimum, y);
					maximum = Math.Max(maximum, y);
				}
			}

			if (series is OxyPlot.Series.ScatterSeries scatterSeries)
			{
				if (scatterSeries.ItemsSource == null)
					continue;

				//if (axisKey == "right" && lineSeries.YAxisKey != "right")
				//	continue;

				PropertyInfo? propertyInfo = null;
				foreach (var item in scatterSeries.ItemsSource)
				{
					if (propertyInfo == null)
						propertyInfo = item.GetType().GetProperty(scatterSeries.DataFieldY);

					var value = propertyInfo!.GetValue(item);
					double d = Convert.ToDouble(value);
					if (double.IsNaN(d))
						continue;

					minimum = Math.Min(minimum, d);
					maximum = Math.Max(maximum, d);
				}
			}
		}

		if (minimum == double.MaxValue)
		{
			// didn't find any values
			minimum = 0;
			maximum = 1;
		}

		foreach (OxyPlot.Annotations.Annotation annotation in PlotModel.Annotations)
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
		}
	}*/

	private void UpdateDateTimeAxisRange()
	{
		if (XAxis == null || !UseDateTimeAxis)
			return;

		double minimum = double.MaxValue;
		double maximum = double.MinValue;

		foreach (ISeries series in Chart.Series)
		{
			if (series is OxyPlot.Series.LineSeries lineSeries)
			{
				//if (lineSeries.LineStyle == LineStyle.None)
				//	continue;

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

		/*if (minimum != double.MaxValue)
		{
			DateTimeAxis.Minimum = minimum;
			DateTimeAxis.Maximum = maximum;
		}*/

		if (ListGroup.TimeWindow == null)
		{
			DateTime startTime = new DateTime((long)minimum);
			DateTime endTime = new DateTime((long)maximum);

			ListGroup.TimeWindow = new TimeWindow(startTime, endTime).Trim();
		}

		UpdateDateTimeAxis(ListGroup.TimeWindow);

		//UpdateDateTimeInterval(double duration);
	}

	/*private void ClearListeners()
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
			OxyColor? oxyColor = null;
			if (series.Name != null && prevListSeries.TryGetValue(series.Name, out OxyPlotChartSeries? prevSeries))
				oxyColor = ((TabChartLineSeries)prevSeries.OxySeries).Color;

			AddSeries(series, oxyColor);
		}
	}

	public void AddSeries(ListSeries listSeries, OxyColor? oxyColor = null)
	{
		//if (listSeries.IsStacked)
		//	AddBarSeries(listSeries);
		//else
		AddListSeries(listSeries, oxyColor);
	}

	/*private void AddBarSeries(ListSeries listSeries)
	{
		var barSeries = new OxyPlot.Series.BarSeries
		{
			Title = listSeries.Name,
			StrokeThickness = 2,
			FillColor = GetColor(plotModel.Series.Count),
			TextColor = OxyColors.Black,
			IsStacked = listSeries.IsStacked,
			TrackerFormatString = "{0}\nTime: {2:yyyy-M-d H:mm:ss.FFF}\nValue: {4:#,0.###}",
		};
		var dataPoints = GetDataPoints(listSeries, listSeries.iList);
		foreach (DataPoint dataPoint in dataPoints)
		{
			barSeries.Items.Add(new BarItem(dataPoint.X, (int)dataPoint.Y));
		}

		plotModel.Series.Add(barSeries);

		//ListToTabSeries[listSeries.iList] = listSeries;
		//ListToTabIndex[listSeries.iList] = ListToTabIndex.Count;
	}*/

	/*private void AddNowTime()
	{
		var now = DateTime.UtcNow;
		if (ListGroup.TimeWindow != null && ListGroup.TimeWindow.EndTime < now.AddMinutes(1))
			return;

		var annotation = new OxyPlot.Annotations.LineAnnotation
		{
			Type = LineAnnotationType.Vertical,
			X = OxyPlot.Axes.DateTimeAxis.ToDouble(now.ToUniversalTime()),
			Color = NowColor,
			// LineStyle = LineStyle.Dot, // doesn't work for vertical?
		};

		PlotModel!.Annotations.Add(annotation);
	}*/

	/*private void AddTrackerLine()
	{
		_trackerAnnotation = new OxyPlot.Annotations.LineAnnotation
		{
			Type = LineAnnotationType.Vertical,
			//Color = Theme.TitleBackground.ToOxyColor(),
			//Color = Color.Parse("#21a094").ToOxyColor(),
			Color = AtlasTheme.GridBackgroundSelected.ToOxyColor(),
			//Color = timeTrackerColor,
			// LineStyle = LineStyle.Dot, // doesn't work for vertical?
		};

		PlotModel!.Annotations.Add(_trackerAnnotation);
	}

	private void AddMouseListeners()
	{
		PlotModel!.MouseDown += PlotModel_MouseDown;
		PlotModel.MouseMove += PlotModel_MouseMove;
		PlotModel.MouseUp += PlotModel_MouseUp;
		PlotModel.MouseLeave += PlotModel_MouseLeave;
	}

	/*private bool _selecting;
	private ScreenPoint _startScreenPoint;
	private DataPoint? _startDataPoint;
	private DataPoint? _endDataPoint;
	private OxyPlot.Annotations.RectangleAnnotation? _rectangleAnnotation;

	private void UpdateMouseSelection(DataPoint endDataPoint)
	{
		if (_rectangleAnnotation == null)
		{
			_rectangleAnnotation = new OxyPlot.Annotations.RectangleAnnotation()
			{
				Fill = OxyColor.FromAColor((byte)AtlasTheme.ChartBackgroundSelectedAlpha, AtlasTheme.ChartBackgroundSelected.ToOxyColor()),
				Stroke = OxyColor.FromAColor((byte)180, AtlasTheme.ChartBackgroundSelected.ToOxyColor()),
				StrokeThickness = 1,
			};
		}

		try
		{
			if (!PlotModel!.Annotations.Contains(_rectangleAnnotation))
				PlotModel.Annotations.Add(_rectangleAnnotation);
		}
		catch (Exception)
		{
		}

		_rectangleAnnotation.MinimumX = Math.Min(_startDataPoint!.Value.X, endDataPoint.X);
		_rectangleAnnotation.MaximumX = Math.Max(_startDataPoint.Value.X, endDataPoint.X);

		_rectangleAnnotation.MinimumY = ValueAxis!.Minimum;
		_rectangleAnnotation.MaximumY = ValueAxis.Maximum;

		Dispatcher.UIThread.Post(() => PlotModel!.InvalidatePlot(false), DispatcherPriority.Background);
	}

	private void PlotModel_MouseDown(object? sender, OxyMouseDownEventArgs e)
	{
		if (!_selecting || _startDataPoint == null)
		{
			_startDataPoint = OxyPlot.Axes.DateTimeAxis.InverseTransform(e.Position, DateTimeAxis, ValueAxis);
			_startScreenPoint = e.Position;
			_selecting = true;
			e.Handled = true;
		}
	}

	private void PlotModel_MouseMove(object? sender, OxyMouseEventArgs e)
	{
		DataPoint dataPoint = OxyPlot.Axes.DateTimeAxis.InverseTransform(e.Position, DateTimeAxis, ValueAxis);
		var moveEvent = new MouseCursorMovedEventArgs(dataPoint.X);
		_mouseCursorChangedEventSource?.Raise(sender, moveEvent);

		if (_selecting && _startDataPoint != null)
		{
			_endDataPoint = OxyPlot.Axes.DateTimeAxis.InverseTransform(e.Position, DateTimeAxis, ValueAxis);
			UpdateMouseSelection(_endDataPoint.Value);
		}
	}

	private void PlotModel_MouseUp(object? sender, OxyMouseEventArgs e)
	{
		if (_selecting && _startDataPoint != null)
		{
			_endDataPoint = OxyPlot.Axes.DateTimeAxis.InverseTransform(e.Position, DateTimeAxis, ValueAxis);
			double width = Math.Abs(e.Position.X - _startScreenPoint.X);
			if (width > MinSelectionWidth)
			{
				ZoomIn();
			}
			else if (ListGroup.TimeWindow?.Selection != null)
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

	private void ZoomIn()
	{
		double left = Math.Min(_startDataPoint!.Value.X, _endDataPoint!.Value.X);
		double right = Math.Max(_startDataPoint.Value.X, _endDataPoint!.Value.X);

		if (double.IsNaN(DateTimeAxis!.Minimum))
			UpdateDateTimeAxisRange();

		DateTimeAxis.Minimum = Math.Max(left, DateTimeAxis.Minimum);
		DateTimeAxis.Maximum = Math.Min(right, DateTimeAxis.Maximum);

		DateTime startTime = OxyPlot.Axes.DateTimeAxis.ToDateTime(DateTimeAxis.Minimum);
		DateTime endTime = OxyPlot.Axes.DateTimeAxis.ToDateTime(DateTimeAxis.Maximum);
		var timeWindow = new TimeWindow(startTime, endTime).Trim();

		UpdateDateTimeAxis(timeWindow);
		if (ListGroup.TimeWindow != null)
			ListGroup.TimeWindow.Select(timeWindow);
		else
			UpdateTimeWindow(timeWindow);
	}

	private void ZoomOut()
	{
		if (ListGroup.TimeWindow != null)
		{
			UpdateDateTimeAxis(ListGroup.TimeWindow);
			ListGroup.TimeWindow.Select(null);
		}
		else
		{
			UpdateTimeWindow(null);
		}
	}*/

	/*private void ListGroup_OnTimesChanged(object? sender, TimeWindowEventArgs e)
	{
		UpdateTimeWindow(e.TimeWindow);
	}

	private void UpdateTimeWindow(TimeWindow? timeWindow)
	{
		UpdateDateTimeAxis(timeWindow);
		UpdateValueAxis();

		ListGroup.SortByTotal();
		Legend.RefreshModel();

		PlotView!.InvalidatePlot(true);
		PlotView.Model.InvalidatePlot(true);
	}

	private void StopSelecting()
	{
		if (_rectangleAnnotation != null)
			PlotModel!.Annotations.Remove(_rectangleAnnotation);
		_selecting = false;
	}

	// Hide cursor when out of scope
	private void PlotModel_MouseLeave(object? sender, OxyMouseEventArgs e)
	{
		var moveEvent = new MouseCursorMovedEventArgs(0);
		_mouseCursorChangedEventSource?.Raise(sender, moveEvent);
	}*/

	// Update mouse tracker
	/*private void TabControlChart_OnMouseCursorChanged(object? sender, MouseCursorMovedEventArgs e)
	{
		if (sender == PlotView?.Controller || _trackerAnnotation == null)
			return;

		_trackerAnnotation.X = e.X;
		Dispatcher.UIThread.Post(() => PlotModel!.InvalidatePlot(false), DispatcherPriority.Background);
	}*/

	/*private void INotifyCollectionChanged_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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
