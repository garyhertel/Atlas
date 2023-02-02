using Atlas.Core;
using Atlas.Resources;
using Atlas.Tabs;
using System.ComponentModel;

namespace Atlas.UI.Avalonia.Controls;

public class TabTaskQueue : ITab
{
	public TaskQueue TaskQueue;

	public TabTaskQueue()
	{
		TaskQueue = TaskManager.TaskQueue;// new TaskQueue();
	}

	/*public void AddBookmark(Call call, TaskInstance taskInstance)
	{
		TaskQueue.AddNew(call, taskInstance);
	}*/

	public TabInstance Create() => new Instance(this);

	public class Toolbar : TabToolbar
	{
		public ToolButton ButtonRefresh { get; set; } = new ToolButton("Refresh", Icons.Streams.Refresh);
		//public ToolButton ButtonReset { get; set; } = new ToolButton("Reset", Icons.Streams.Refresh);

		[Separator]
		public ToolButton ButtonClearAll { get; set; } = new ToolButton("Clear All", Icons.Streams.DeleteList);
	}

	public class Instance : TabInstance
	{
		public readonly TabTaskQueue Tab;

		public Instance(TabTaskQueue tab)
		{
			Tab = tab;
		}

		public override void Load(Call call, TabModel model)
		{
			//Tab.Bookmarks.Load(call, true);

			//model.AddData(Tab.TaskQueue.TaskInstances);

			model.Items = Tab.TaskQueue.TaskInstances
				.Select(t => new TaskQueueItem(t))
				.ToList();
		}

		public override void LoadUI(Call call, TabModel model)
		{
			var toolbar = new Toolbar();
			toolbar.ButtonRefresh.Action = Refresh;
			//toolbar.ButtonReset.Action = Reset;
			toolbar.ButtonClearAll.Action = DeleteAll;
			model.AddObject(toolbar);

			/*if (Tab.Bookmarks.NewBookmark != null)
			{
				SelectItem(Tab.Bookmarks.NewBookmark);
				Tab.Bookmarks.NewBookmark = null;
			}*/
		}

		public override void GetBookmark(TabBookmark tabBookmark)
		{
			base.GetBookmark(tabBookmark);

			foreach (var child in tabBookmark.ChildBookmarks.Values)
				child.IsRoot = true;
		}

		private void Refresh(Call call)
		{
			Refresh();
		}

		/*private void Reset(Call call)
		{
			foreach (TabBookmarkItem item in SelectedItems)
			{
				SelectItem(item);
			}
		}*/

		private void DeleteAll(Call call)
		{
			Tab.TaskQueue.TaskInstances.Clear();
			//Tab.TaskQueue.Load(call, true);
		}
	}
}

public class TaskQueueItem : INotifyPropertyChanged
{
	public event EventHandler<EventArgs>? OnDelete;
	public event PropertyChangedEventHandler? PropertyChanged;

	[ButtonColumn("-")]
	public void Delete()
	{
		OnDelete?.Invoke(this, EventArgs.Empty);
		TaskInstance.Cancel();
	}

	public readonly TaskInstance TaskInstance;

	public string? Task => TaskInstance.Label;
	public double Percent => TaskInstance.Percent;
	public string Status => TaskInstance.Status;

	public override string? ToString() => TaskInstance.ToString();

	public TaskQueueItem(TaskInstance taskInstance)
	{
		TaskInstance = taskInstance;
		TaskInstance.PropertyChanged += TaskInstance_PropertyChanged;
	}

	private void TaskInstance_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		PropertyChanged?.Invoke(sender, e);
	}
}
