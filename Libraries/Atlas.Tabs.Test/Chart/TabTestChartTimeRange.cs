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

			//var chartGroup = new ChartGroup();
			//chartGroup.Series.Add(new 

			var chartSettings = new ChartSettings(list, "Active Connection Count");
			var chartGroup = chartSettings.ChartViews.Values.First();
			chartGroup.Annotations.Add(new ChartAnnotation()
			{
				Text = "Too High",
				Y = 2000000000,
				Color = Color.Red,
			});
			chartGroup.ShowTimeTracker = true;
			model.AddObject(chartSettings);
		}
	}
}
