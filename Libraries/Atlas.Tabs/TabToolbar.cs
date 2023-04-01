using Atlas.Core;

namespace Atlas.Tabs;

public class ToolButton
{
	public string Tooltip { get; set; }
	public string? Label { get; set; }
	public string IconResourceName { get; set; }
	public bool ShowTask { get; set; }
	public bool Default { get; set; } // Use Enter as HotKey, add more complex keymapping later?
	public bool DisableWhileRunning { get; set; } = true;
	public object? HotKey { get; set; } // KeyGesture only (in Avalonia.UI)

	public TaskDelegate.CallAction? Action { get; set; }
	public TaskDelegateAsync.CallActionAsync? ActionAsync { get; set; }

	public ToolButton(string tooltip, string iconResourceName, TaskDelegate.CallAction? action = null, bool isDefault = false)
	{
		Tooltip = tooltip;
		IconResourceName = iconResourceName;
		Action = action;
		Default = isDefault;
	}

	public ToolButton(string tooltip, string iconResourceName, TaskDelegateAsync.CallActionAsync? actionAsync, bool isDefault = false)
	{
		Tooltip = tooltip;
		IconResourceName = iconResourceName;
		ActionAsync = actionAsync;
		Default = isDefault;
	}
}

public class TabToolbar
{
	public List<ToolButton> Buttons { get; set; } = new();
}
