namespace Atlas.Core;

public class TaskQueue
{
	public ItemCollection<TaskInstance> TaskInstances { get; set; } = new();
}
