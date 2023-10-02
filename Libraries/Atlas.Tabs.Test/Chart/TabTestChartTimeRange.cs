using Atlas.Core;
using Atlas.Extensions;
using System.Drawing;

namespace Atlas.Tabs.Test.Chart;

public class TabTestChartTimeRangeValue : ITab
{
	public TabInstance Create() => new Instance();

	public class Instance : TabInstance, ITabAsync
	{
		private readonly Random _random = new();

		public async Task LoadAsync(Call call, TabModel model)
		{
			await Task.Delay(10);

			var chartView = new ChartView("Animals")
			{
				ShowTimeTracker = true,
				//Logarithmic = true,
			};

			DateTime startTime = DateTime.Now.Trim(TimeSpan.TicksPerHour);
			chartView.AddSeries("Cats", CreateSeries(startTime));
			chartView.AddSeries("Dogs", CreateSeries(startTime));

			chartView.Annotations.Add(new ChartAnnotation()
			{
				Text = "Too Many",
				Y = 2_000_000_000,
				Color = Color.Red,
			});

			model.AddObject(chartView);
		}

		private List<TimeRangeValue> CreateSeries(DateTime startTime)
		{
			var list = new List<TimeRangeValue>();
			for (int i = 0; i < 24; i++)
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
	}
}
