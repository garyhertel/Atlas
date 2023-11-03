using Atlas.Core;
using Atlas.Extensions;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections;
using System.Reflection;
using Avalonia.Media;
using LiveChartsCore.Kernel;
using Atlas.Core.Collections;

namespace Atlas.UI.Avalonia.Charts.LiveCharts;

public class SeriesHoverEventArgs : EventArgs
{
	public ListSeries Series { get; set; }

	public SeriesHoverEventArgs(ListSeries series)
	{
		Series = series;
	}
}

public class LiveChartSeries //: ChartSeries<ISeries>
{
	private const int MaxPointsToShowMarkers = 8;
	private const int MaxTitleLength = 200;

	public readonly TabControlLiveChart Chart;
	public readonly ListSeries ListSeries;
	public readonly bool UseDateTimeAxis;
	public LiveChartLineSeries LineSeries;

	public PropertyInfo? XAxisPropertyInfo;

	public override string? ToString() => ListSeries?.ToString();

	public LiveChartSeries(TabControlLiveChart chart, ListSeries listSeries, Color color, bool useDateTimeAxis)
	{
		Chart = chart;
		ListSeries = listSeries;
		UseDateTimeAxis = useDateTimeAxis;

		SKColor skColor = color.AsSkColor();

		var dataPoints = GetDataPoints(listSeries, listSeries.List);

		LineSeries = new LiveChartLineSeries(this)
		{
			Name = listSeries.Name,
			Values = dataPoints,
			Fill = null,
			LineSmoothness = 0, // 1 = Curved
			GeometrySize = 5,
			EnableNullSplitting = true,

			Stroke = new SolidColorPaint(skColor) { StrokeThickness = 2 },
			GeometryStroke = null,
			GeometryFill = null,
		};

		if (listSeries.List.Count > 0 && listSeries.List.Count <= MaxPointsToShowMarkers || HasSinglePoint(dataPoints))
		{
			//lineSeries.GeometryStroke = new SolidColorPaint(skColor) { StrokeThickness = 2f };
			LineSeries.GeometryFill = new SolidColorPaint(skColor);
		}

		/*
		MarkerSize = 3;

		// can't add gaps with ItemSource so convert to LiveChartPoint ourselves

		if (listSeries.List is INotifyCollectionChanged iNotifyCollectionChanged)
		{
			//iNotifyCollectionChanged.CollectionChanged += INotifyCollectionChanged_CollectionChanged;
			iNotifyCollectionChanged.CollectionChanged += new NotifyCollectionChangedEventHandler(delegate (object? sender, NotifyCollectionChangedEventArgs e)
			{
				// can we remove this later when disposing?
				SeriesChanged(listSeries, e);
			});
		}*/
	}

	private bool HasSinglePoint(List<LiveChartPoint> dataPoints)
	{
		bool prevNan1 = false;
		bool prevNan2 = false;
		foreach (LiveChartPoint dataPoint in dataPoints)
		{
			if (dataPoint.Y == null) return true;

			bool nan = double.IsNaN(dataPoint.Y.Value);
			if (prevNan2 && !prevNan1 && nan)
				return true;

			prevNan2 = prevNan1;
			prevNan1 = nan;
		}
		return false;
	}

	public string? GetTooltipTitle()
	{
		string? title = ListSeries.Name;
		if (title != null && title.Length > MaxTitleLength)
		{
			title = title[..MaxTitleLength] + "...";
		}
		return title;
	}

	public string[] GetTooltipLines(ChartPoint point)
	{
		List<string> lines = new();

		if (point.Context.DataSource is LiveChartPoint liveChartPoint)
		{
			string valueLabel = ListSeries.YPropertyLabel ?? "Value";
			if (liveChartPoint.Object is TimeRangeValue timeRangeValue)
			{
				lines.Add($"Time: {timeRangeValue.TimeText}");
				lines.Add($"Duration: {timeRangeValue.Duration.FormattedDecimal()}");
				lines.Add($"{valueLabel}: {timeRangeValue.Value.Formatted()}");
			}
			else
			{
				if (ListSeries.XPropertyInfo?.PropertyType == typeof(DateTime))
				{
					var startTime = new DateTime((long)liveChartPoint.X!);
					if (ListSeries.PeriodDuration is TimeSpan timeSpan)
					{
						string timeText = DateTimeUtils.FormatTimeRange(startTime, startTime.Add(timeSpan), false);
						lines.Add($"Time: {timeText}");
					}
					else
					{
						lines.Add($"Time: {startTime.Formatted()}");
					}
				}
				else
				{
					lines.Add($"X: {liveChartPoint.X}");
				}
				lines.Add($"{valueLabel}: {liveChartPoint.Y!.Formatted()}");
			}

			if (liveChartPoint.Object is ITags tags && tags.Tags.Count > 0)
			{
				lines.Add("");

				foreach (Tag tag in tags.Tags)
				{
					lines.Add($"{tag.Name}: {tag.Value}");
				}
			}
		}
		if (ListSeries.Description != null)
		{
			lines.Add("");
			lines.AddRange(ListSeries.Description.Split('\n'));
		}
		return lines.ToArray();
	}

	private List<LiveChartPoint> GetDataPoints(ListSeries listSeries, IList iList)
	{
		UpdateXAxisProperty(listSeries);
		double x = 0; // Points.Count;
		var chartPoints = new List<LiveChartPoint>();
		// faster than using ItemSource?
		foreach (object obj in iList)
		{
			double? d = null;
			if (listSeries.YPropertyInfo != null)
			{
				if (XAxisPropertyInfo != null)
				{
					object? xObj = XAxisPropertyInfo.GetValue(obj);
					if (xObj is DateTime dateTime)
					{
						x = dateTime.Ticks;
					}
					else if (xObj == null)
					{
						continue;
					}
					else
					{
						x = Convert.ToDouble(xObj);
					}
				}

				object? value = listSeries.YPropertyInfo.GetValue(obj);
				d = null;
				if (value != null)
				{
					d = Convert.ToDouble(value);
					if (double.IsNaN(d.Value))
						d = null;
				}
			}
			else
			{
				d = Convert.ToDouble(obj);
			}

			var chartPoint = new LiveChartPoint(obj, x++, d);
			chartPoints.Add(chartPoint);
		}

		chartPoints = chartPoints
			.OrderBy(d => d.X)
			.ToList();

		/*if (chartPoints.Count > 0 && listSeries.XBinSize > 0)
		{
			chartPoints = BinDataPoints(chartPoints, listSeries.XBinSize);
		}*/
		return chartPoints;
	}

	private void UpdateXAxisProperty(ListSeries listSeries)
	{
		if (listSeries.YPropertyInfo != null)
		{
			if (listSeries.XPropertyInfo != null)
				XAxisPropertyInfo = listSeries.XPropertyInfo;

			if (XAxisPropertyInfo == null)
			{
				Type elementType = listSeries.List.GetType().GetElementTypeForAll()!;
				foreach (PropertyInfo propertyInfo in elementType.GetProperties())
				{
					if (propertyInfo.GetCustomAttribute<XAxisAttribute>() != null)
						XAxisPropertyInfo = propertyInfo;
				}
			}
		}
	}

	/*private static List<LiveChartPoint> BinDataPoints(List<LiveChartPoint> dataPoints, double xBinSize)
	{
		double firstX = dataPoints.First().X;
		double firstBinX = ((int)(firstX / xBinSize)) * xBinSize; // use start of interval
		double lastBinX = dataPoints.Last().X;
		int numBins = (int)Math.Ceiling((lastBinX - firstBinX) / xBinSize) + 1;
		double[] bins = new double[numBins];
		foreach (LiveChartPoint dataPoint in dataPoints)
		{
			int bin = (int)((dataPoint.X - firstBinX) / xBinSize);
			bins[bin] += dataPoint.Y;
		}

		bool prevNan = false;
		var binDataPoints = new List<LiveChartPoint>();
		for (int i = 0; i < numBins; i++)
		{
			double value = bins[i];
			if (value == 0)
			{
				if (prevNan)
					continue;

				prevNan = true;
				value = double.NaN;
			}
			else
			{
				prevNan = true;
			}
			binDataPoints.Add(new LiveChartPoint(firstBinX + i * xBinSize, value));
		}

		return binDataPoints;
	}
	
	private void SeriesChanged(ListSeries listSeries, NotifyCollectionChangedEventArgs e)
	{
		lock (Chart.PlotModel!.SyncRoot)
		{
			if (e.Action == NotifyCollectionChangedAction.Add)
			{
				var dataPoints = GetDataPoints(listSeries, e.NewItems!);
				Points.AddRange(dataPoints);
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove)
			{
				var dataPoints = GetDataPoints(listSeries, e.OldItems!);
				foreach (LiveChartPoint datapoint in dataPoints)
				{
					if (Points.FirstOrDefault().X == datapoint.X)
					{
						Points.RemoveAt(0);
					}
				}
			}
		}

		Dispatcher.UIThread.InvokeAsync(Chart.Refresh, DispatcherPriority.Background);
	}*/
}
