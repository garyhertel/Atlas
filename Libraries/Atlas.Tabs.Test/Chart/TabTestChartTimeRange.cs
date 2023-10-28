using Atlas.Core;
using Atlas.Core.Charts;
using Atlas.Extensions;
using System.Drawing;

namespace Atlas.Tabs.Test.Chart;

public class TabTestChartTimeRangeValue : ITab
{
	public TabInstance Create() => new Instance();

	public class Instance : TabInstance
	{
		public override void Load(Call call, TabModel model)
		{
			DateTime startTime = DateTime.Now.Trim(TimeSpan.TicksPerHour);

			AddAnimals(model, startTime);
			AddToys(model, startTime);
		}

		private void AddToys(TabModel model, DateTime startTime)
		{
			var chartViewToys = new ChartView("Toys")
			{
				ShowTimeTracker = true,
			};
			chartViewToys.AddSeries("Toys", ChartSamples.CreateIdenticalTimeSeries(startTime), seriesType: SeriesType.Average);
			model.AddObject(chartViewToys);
		}

		private DateTime AddAnimals(TabModel model, DateTime startTime)
		{
			var chartView = new ChartView("Animals")
			{
				ShowTimeTracker = true,
				//Logarithmic = true,
			};

			chartView.AddSeries("Cats", ChartSamples.CreateTimeSeries(startTime), seriesType: SeriesType.Average);
			chartView.AddSeries("Dogs", ChartSamples.CreateTimeSeries(startTime), seriesType: SeriesType.Average);

			chartView.Annotations.Add(new ChartAnnotation()
			{
				Text = "Too Many",
				Y = 2_000_000_000,
				Color = Color.Red,
			});
			model.AddObject(chartView);
			return startTime;
		}
	}
}
