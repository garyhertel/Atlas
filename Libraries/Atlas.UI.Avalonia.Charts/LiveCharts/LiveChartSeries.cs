using Atlas.Core;
using Atlas.Extensions;
using Atlas.UI.Avalonia.Charts.LiveCharts;
using Avalonia.Threading;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using Avalonia.Media;
using Avalonia;

namespace Atlas.UI.Avalonia.Charts;

public class LiveChartSeries //: ChartSeries<ISeries>
{
	private const int MaxPointsToShowMarkers = 8;
	private const int MaxTitleLength = 200;

	public readonly TabControlLiveChart Chart;
	public readonly ListSeries ListSeries;
	public readonly bool UseDateTimeAxis;
	public ISeries LineSeries;

	public PropertyInfo? XAxisPropertyInfo;

	// ObservablePoint is sealed
	private readonly Dictionary<ObservablePoint, object> _datapointLookup = new();

	public override string? ToString() => ListSeries?.ToString();

	public LiveChartSeries(TabControlLiveChart chart, ListSeries listSeries, Color color, bool useDateTimeAxis)
	{
		Chart = chart;
		ListSeries = listSeries;
		UseDateTimeAxis = useDateTimeAxis;

		SKColor skColor = color.AsSkColor();

		var dataPoints = GetDataPoints(listSeries, listSeries.List, _datapointLookup);

		//LineSeries = new LineSeries<DateTimePoint>
		var lineSeries = new LineSeries<ObservablePoint>
		{
			Name = listSeries.Name,
			Values = dataPoints,
			Fill = null,
			LineSmoothness = 0, // 1 = Curved
			GeometrySize = 1,
			EnableNullSplitting = true,

			Stroke = new SolidColorPaint(skColor) { StrokeThickness = 2 },
			GeometryStroke = null,
		};

		if (listSeries.List.Count > 0 && listSeries.List.Count <= MaxPointsToShowMarkers || HasSinglePoint(dataPoints))
		{
			lineSeries.GeometryStroke = new SolidColorPaint(skColor) { StrokeThickness = 5 };
		}

		LineSeries = lineSeries;

		// Title must be unique among all series
		/*Title = listSeries.Name;
		if (Title?.Length == 0)
			Title = "<NA>";

		TextColor = OxyColors.Black;
		CanTrackerInterpolatePoints = false;
		MinimumSegmentLength = 2;
		MarkerSize = 3;
		LoadTrackFormat();

		// can't add gaps with ItemSource so convert to ObservablePoint ourselves

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

	private bool HasSinglePoint(List<ObservablePoint> dataPoints)
	{
		bool prevNan1 = false;
		bool prevNan2 = false;
		foreach (ObservablePoint dataPoint in dataPoints)
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

	// Override the default tracker text
	/*public override TrackerHitResult? GetNearestPoint(ScreenPoint point, bool interpolate)
	{
		TrackerHitResult result = base.GetNearestPoint(point, interpolate);
		if (result == null)
			return null;

		string valueLabel = ListSeries.YPropertyLabel ?? "Value";

		if (_datapointLookup.TryGetValue(result.ObservablePoint, out object? obj))
		{
			if (obj is TimeRangeValue timeRangeValue)
			{
				string? title = ListSeries.Name;
				if (title != null && title.Length > MaxTitleLength)
					title = title[..MaxTitleLength] + "...";

				result.Text = title + "\n\nTime: " + timeRangeValue.TimeText + "\nDuration: " + timeRangeValue.Duration.FormattedDecimal() + "\n" + valueLabel + ": " + timeRangeValue.Value.Formatted();
			}

			if (obj is ITags tags && tags.Tags.Count > 0)
			{
				result.Text += "\n";

				foreach (Tag tag in tags.Tags)
				{
					result.Text += "\n" + tag.Name + ": " + tag.Value;
				}
			}
		}
		if (ListSeries.Description != null)
			result.Text += "\n\n" + ListSeries.Description;
		return result;
	}

	/*
		{0} the title of the series
		{1} the title of the x-axis
		{2} the x-value
		{3} the title of the y-axis
		{4} the y-value
	*/
	private void LoadTrackFormat()
	{
		string xTrackerFormat = ListSeries.XPropertyName ?? "Index: {2:#,0.###}";
		if (UseDateTimeAxis || ListSeries.XPropertyInfo?.PropertyType == typeof(DateTime))
		{
			xTrackerFormat = "Time: {2:yyyy-M-d H:mm:ss.FFF}";
		}
		//TrackerFormatString = "{0}\n" + xTrackerFormat + "\nValue: {4:#,0.###}";
	}

	private List<ObservablePoint> GetDataPoints(ListSeries listSeries, IList iList, Dictionary<ObservablePoint, object>? datapointLookup = null)
	{
		UpdateXAxisProperty(listSeries);
		double x = 0; // Points.Count;
		var dataPoints = new List<ObservablePoint>();
		if (listSeries.YPropertyInfo != null)
		{
			// faster than using ItemSource?
			foreach (object obj in iList)
			{
				object? value = listSeries.YPropertyInfo.GetValue(obj);
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
				double d = double.NaN;
				if (value != null)
					d = Convert.ToDouble(value);

				var dataPoint = new ObservablePoint(x++, d);
				if (datapointLookup != null && !double.IsNaN(d) && !datapointLookup.ContainsKey(dataPoint))
					datapointLookup.Add(dataPoint, obj);
				dataPoints.Add(dataPoint);
			}
			dataPoints = dataPoints.OrderBy(d => d.X).ToList();

			/*if (dataPoints.Count > 0 && listSeries.XBinSize > 0)
			{
				dataPoints = BinDataPoints(dataPoints, listSeries.XBinSize);
			}*/
		}
		else
		{
			foreach (object obj in iList)
			{
				double value = Convert.ToDouble(obj);
				dataPoints.Add(new ObservablePoint(x++, value));
			}
		}
		return dataPoints;
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

	/*private static List<ObservablePoint> BinDataPoints(List<ObservablePoint> dataPoints, double xBinSize)
	{
		double firstX = dataPoints.First().X;
		double firstBinX = ((int)(firstX / xBinSize)) * xBinSize; // use start of interval
		double lastBinX = dataPoints.Last().X;
		int numBins = (int)Math.Ceiling((lastBinX - firstBinX) / xBinSize) + 1;
		double[] bins = new double[numBins];
		foreach (ObservablePoint dataPoint in dataPoints)
		{
			int bin = (int)((dataPoint.X - firstBinX) / xBinSize);
			bins[bin] += dataPoint.Y;
		}

		bool prevNan = false;
		var binDataPoints = new List<ObservablePoint>();
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
			binDataPoints.Add(new ObservablePoint(firstBinX + i * xBinSize, value));
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
				foreach (ObservablePoint datapoint in dataPoints)
				{
					if (Points.FirstOrDefault().X == datapoint.X)
					{
						Points.RemoveAt(0);
					}
				}
			}
		}

		Dispatcher.UIThread.InvokeAsync(() => Chart.Refresh(), DispatcherPriority.Background);
	}*/
}
