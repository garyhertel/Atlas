using Atlas.UI.Avalonia.Themes;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace Atlas.UI.Avalonia.Controls;

public class ToolbarTextBox : TextBox, IStyleable, ILayoutable
{
	Type IStyleable.StyleKey => typeof(TextBox);

	public ToolbarTextBox(string text)
	{
		Text = text;
		//MinWidth = minWidth;
		//Margin = DefaultMargin;
		TextWrapping = TextWrapping.NoWrap;
		VerticalAlignment = VerticalAlignment.Center;
		Background = Theme.ToolbarTextBackground;
		Foreground = Theme.ToolbarTextForeground;
		CaretBrush = Theme.ToolbarCaret;

		// Fluent
		Resources.Add("TextControlBackgroundPointerOver", Background);
		Resources.Add("TextControlBackgroundFocused", Background);
		Resources.Add("TextControlPlaceholderForegroundFocused", Foreground);
		Resources.Add("TextControlPlaceholderForegroundPointerOver", Foreground);
	}
}