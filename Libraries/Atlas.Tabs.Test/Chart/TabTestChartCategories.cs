using Atlas.Core;
using Atlas.Core.Charts;
using Atlas.Extensions;

namespace Atlas.Tabs.Test.Chart;

public class TabTestChartCategories : ITab
{
	public TabInstance Create() => new Instance();

	public class Instance : TabInstance
	{
		private readonly ItemCollection<ChartSample> _samples = new();
		private readonly Random _random = new();
		private readonly DateTime _baseDateTime = DateTime.Now.Trim(TimeSpan.FromMinutes(1));

		public class TestItem
		{
			public int Amount { get; set; }
		}

		public class ChartSample
		{
			public string? Name { get; set; }

			[XAxis]
			public DateTime TimeStamp { get; set; }

			public string? Category { get; set; }

			public int? Value { get; set; }

			public TestItem TestItem { get; set; } = new();

			public int InstanceAmount => TestItem.Amount;
		}

		public override void Load(Call call, TabModel model)
		{
			model.Actions = new List<TaskCreator>()
			{
				new TaskDelegate("Add Entry", AddEntry),
				new TaskDelegate("Start: 1 Entry / second", StartTask, true),
			};
			AddSeries("Cats");
			AddSeries("Dogs");

			var chartView = new ChartView();
			chartView.AddDimensions(_samples,
				nameof(ChartSample.Category),
				nameof(ChartSample.TimeStamp),
				nameof(ChartSample.Value));
			model.AddObject(chartView, true);
		}

		private void AddSeries(string category)
		{
			for (int i = 0; i < 10; i++)
			{
				if (i == 4 || i == 6)
				{
					AddNullSample(category, i);
				}
				else
				{
					AddSample(category, i);
				}
			}
		}

		private void AddEntry(Call call)
		{
			int param1 = 1;
			string param2 = "abc";
			Invoke(call, AddSampleUI, param1, param2);
		}

		private void StartTask(Call call)
		{
			CancellationToken token = call.TaskInstance!.TokenSource.Token;
			for (int i = 0; i < 20 && !token.IsCancellationRequested; i++)
			{
				Invoke(call, AddSampleUI);
				Thread.Sleep(1000);
			}
		}

		private void AddSample(string category, int i)
		{
			ChartSample sample = new()
			{
				Name = "Name " + i.ToString(),
				TimeStamp = _baseDateTime.AddMinutes(i),
				Category = category,
				Value = _random.Next(50, 100),
				TestItem = new TestItem()
				{
					Amount = _random.Next(0, 100),
				},
			};
			_samples.Add(sample);
		}

		private void AddNullSample(string category, int i)
		{
			ChartSample sample = new()
			{
				Name = "Name " + i.ToString(),
				TimeStamp = _baseDateTime.AddMinutes(i),
				Category = category,
				TestItem = new TestItem()
				{
					Amount = _random.Next(0, 100),
				},
			};
			_samples.Add(sample);
		}

		// UI context
		private void AddSampleUI(Call call, object state)
		{
			AddSample("Cats", _samples.Count);
			AddSample("Dogs", _samples.Count);
		}
	}
}
