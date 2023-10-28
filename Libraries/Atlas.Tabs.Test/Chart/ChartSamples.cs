using Atlas.Core;

namespace Atlas.Tabs.Test.Chart;

public class ChartSamples
{
	private static readonly Random _random = new();

	public static List<TimeRangeValue> CreateTimeSeries(DateTime startTime, int sampleCount = 24)
	{
		var list = new List<TimeRangeValue>();
		for (int i = 0; i < sampleCount; i++)
		{
			var value = new TimeRangeValue()
			{
				StartTime = startTime,
				EndTime = startTime.AddHours(1),
				Value = _random.Next() % int.MaxValue,
			};
			list.Add(value);
			startTime = startTime.AddHours(1);
		}
		return list;
	}

	public static List<TimeRangeValue> CreateIdenticalTimeSeries(DateTime startTime, int sampleCount = 24)
	{
		var list = new List<TimeRangeValue>();
		for (int i = 0; i < sampleCount; i++)
		{
			var value = new TimeRangeValue()
			{
				StartTime = startTime,
				EndTime = startTime.AddHours(1),
				Value = 1000,
			};
			list.Add(value);
			startTime = startTime.AddHours(1);
		}
		return list;
	}
}
