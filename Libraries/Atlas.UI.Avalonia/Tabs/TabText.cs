using Atlas.Core;
using Atlas.Tabs;
using Atlas.UI.Avalonia.Controls;

namespace Atlas.UI.Avalonia.Tabs;

public class TabText : ITab
{
	public object Object;

	public TabText(object obj)
	{
		Object = obj;
	}

	public TabInstance Create() => new Instance(this);

	public class Instance : TabInstance
	{
		public readonly TabText Tab;

		public Instance(TabText tab)
		{
			Tab = tab;
		}

		public override void LoadUI(Call call, TabModel model)
		{
			model.MinDesiredWidth = 100;
			model.MaxDesiredWidth = 1000;

			var tabAvaloniaEdit = new TabControlAvaloniaEdit(this);
			if (Tab.Object is string text)
			{
				tabAvaloniaEdit.SetFormattedJson(text);
			}
			else
			{
				tabAvaloniaEdit.Text = Tab.Object.ToString()!;
			}

			model.AddObject(tabAvaloniaEdit, true);
		}
	}
}
/*
Markdown support?
- Avalonia.Markdown slow for large text and doesn't allow text selection (yet?)
*/
