using Atlas.Extensions;
using System.Collections;
using System.Drawing;
using System.Reflection;

namespace Atlas.Core;

public class ChartAnnotation
{
	public string? Text { get; set; }
	//public Color? TextColor { get; set; }
	public Color? Color { get; set; }

	public bool Horizontal { get; set; } = true;
	public double? X { get; set; }
	public double? Y { get; set; }

	public double StrokeThickness { get; set; } = 2;
}

public class ChartView
{
	public string? Name { get; set; }
	public string? LegendTitle { get; set; }

	public bool Horizontal { get; set; }
	public bool ShowLegend { get; set; } = true;
	public bool ShowOrder { get; set; } = true;
	public bool ShowTimeTracker { get; set; }
	public bool Logarithmic { get; set; }

	public double? MinValue { get; set; }
	public double XBinSize { get; set; }

	public TimeWindow? TimeWindow { get; set; }

	public ItemCollection<ListSeries> Series { get; set; } = new();

	public List<ChartAnnotation> Annotations { get; set; } = new();

	public override string? ToString() => Name;

	public ChartView(string? name = null, TimeWindow? timeWindow = null)
	{
		Name = name;
		TimeWindow = timeWindow;
	}

	public ChartView(string? name, DateTime startTime, DateTime endTime)
	{
		Name = name;
		TimeWindow = new TimeWindow(startTime, endTime);
	}

	public void AddDimensions(IList iList, string categoryPropertyName, string xPropertyName, string yPropertyName)
	{
		Type listType = iList.GetType();
		Type elementType = iList.GetType().GetElementTypeForAll()!;
		PropertyInfo categoryPropertyInfo = elementType.GetProperty(categoryPropertyName)!;

		var dimensions = new Dictionary<string, IList>();
		foreach (var obj in iList)
		{
			var categoryObject = categoryPropertyInfo.GetValue(obj)!;

			string category = categoryObject.ToString()!;

			if (!dimensions.TryGetValue(category, out IList? categoryList))
			{
				categoryList = (IList)Activator.CreateInstance(listType)!;
				dimensions.Add(category, categoryList);

				var listSeries = new ListSeries(category, categoryList, xPropertyName, yPropertyName)
				{
					XBinSize = XBinSize,
				};
				Series.Add(listSeries);
			}
			categoryList.Add(obj);
		}
		SortByTotal();
	}

	public void SortByTotal()
	{
		var timeWindow = TimeWindow;
		int averageCount = Series.Count(s => s.SeriesType == SeriesType.Average);
		if (averageCount == Series.Count)
		{
			// Use the Min/Max times from the series data points (should this be configurable?)
			timeWindow = GetSeriesTimeWindow();
		}

		var sums = new Dictionary<ListSeries, double>();
		foreach (var listSeries in Series)
		{
			sums.Add(listSeries, listSeries.CalculateTotal(timeWindow));
		}

		var sortedDict = from entry in sums orderby entry.Value descending select entry.Key;

		Series = new ItemCollection<ListSeries>(sortedDict);
	}

	public TimeWindow GetSeriesTimeWindow()
	{
		DateTime startTime = DateTime.MaxValue;
		DateTime endTime = DateTime.MinValue;
		foreach (ListSeries listSeries in Series)
		{
			TimeWindow seriesWindow = listSeries.GetTimeWindow();
			startTime = startTime.Min(seriesWindow.StartTime);
			endTime = endTime.Max(seriesWindow.EndTime);
		}
		return new TimeWindow(startTime, endTime);
	}
}

/*
TabSeries
	Pros
		Has iList
			
ListChart(x) -> ListChartItem(x) -> ListSeries -> ItemSeries -> ItemList
	Cons
		Bad name

Binding an iList
Who should update


*/
