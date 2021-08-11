﻿using Atlas.Core;
using Atlas.Tabs.Test.DataGrid;
using System;
using System.Collections.Generic;

namespace Atlas.Tabs.Test
{
	[PublicData]
	public class TabSample : ITab
	{
		public override string ToString() => "Sample";

		public TabInstance Create() => new Instance();

		public class Instance : TabInstance
		{
			private ItemCollection<SampleItem> _sampleItems;

			public override void Load(Call call, TabModel model)
			{
				_sampleItems = new ItemCollection<SampleItem>();
				AddItems(5);

				model.Items = new ItemCollection<ListItem>("Items")
				{
					new ListItem("Sample Items", _sampleItems),
					new ListItem("Collections", new TabTestGridCollectionSize()),
					new ListItem("Recursive Copy", new TabSample()),
				};

				model.Actions = new List<TaskCreator>()
				{
					new TaskDelegate("Sleep 10s", Sleep, true),
					new TaskAction("Add 5 Items", new Action(() => AddItems(5)), false), // Foreground task so we can modify collection
				};

				model.Notes =
@"
This is a sample tab that shows some of the different tab features

Actions
DataGrids
";
			}

			private void Sleep(Call call)
			{
				call.TaskInstance.ProgressMax = 10;
				for (int i = 0; i < 10; i++)
				{
					System.Threading.Thread.Sleep(1000);
					call.Log.Add("Slept 1 second");
					call.TaskInstance.Progress++;
				}
			}

			private void AddItems(int count)
			{
				for (int i = 0; i < count; i++)
					_sampleItems.Add(new SampleItem(_sampleItems.Count, "Item " + _sampleItems.Count));
			}
		}
	}

	public class SampleItem
	{
		public int Id { get; set; }
		public string Name { get; set; }

		public SampleItem(int id, string name)
		{
			Id = id;
			Name = name;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
