using Atlas.Core;
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

			var list = new List<TimeRangeValue>();

			DateTime startTime = DateTime.Now;
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

			var chartView = new ChartView("Active Connection Count")
			{
				ShowTimeTracker = true,
				Logarithmic = true,
			};
			chartView.AddSeries("Connections", list);
			chartView.Annotations.Add(new ChartAnnotation()
			{
				Text = "Too High",
				Y = 2_000_000_000,
				Color = Color.Red,
			});
			model.AddObject(chartView);
		}
	}
}
