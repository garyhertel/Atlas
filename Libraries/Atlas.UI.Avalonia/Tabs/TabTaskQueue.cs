using Atlas.Core;
using System.ComponentModel;

namespace Atlas.UI.Avalonia.Controls;

// No longer used: remove?
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
