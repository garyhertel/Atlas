using Atlas.Resources;
using Atlas.UI.Avalonia.Themes;
using Atlas.UI.Avalonia.Utilities;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Atlas.UI.Avalonia.Controls;

public class ImageButton : Button
{
	protected override Type StyleKeyOverride => typeof(Button);

	public string? Tooltip { get; set; }

	public ImageButton(ResourceView imageResource)
	{
		IImage image = LoadImageStream(imageResource);
		Initialize(image);
	}

	/*public ImageButton(Stream bitmapStream)
	{
		IImage image = LoadImageStream(bitmapStream);
		Initialize(image);
	}*/

	public ImageButton(IImage image)
	{
		Initialize(image);
	}

	private void Initialize(IImage sourceImage)
	{
		Grid grid = new()
		{
			ColumnDefinitions = new ColumnDefinitions("Auto"),
			RowDefinitions = new RowDefinitions("Auto"),
			Margin = new Thickness(4),
		};
		Image image = CreateImage(sourceImage);

		Resources.Add("ButtonBackgroundPointerOver", AtlasTheme.ToolbarButtonBackgroundPointerOver);
		//button.Resources.Add("ButtonBackgroundPressed", Theme.ToolbarButtonBackgroundHover);
		//button.Resources.Add("ButtonPlaceholderForegroundFocused", Foreground);
		//button.Resources.Add("ButtonPlaceholderForegroundPointerOver", Foreground);

		grid.Children.Add(image);

		Content = grid;
		//Command = command;
		Background = AtlasTheme.ToolbarBackground;
		BorderBrush = Background;
		BorderThickness = new Thickness(0);
		Margin = new Thickness(1);
		Padding = new Thickness(0);
		//Foreground = new SolidColorBrush(AtlasTheme.ButtonForegroundColor),
		//BorderBrush = new SolidColorBrush(Colors.Black),
		ToolTip.SetTip(this, Tooltip);

		BorderBrush = Background;
	}

	private Image CreateImage(IImage sourceImage)
	{
		return new Image()
		{
			Source = sourceImage,
			Width = 24,
			Height = 24,
			Stretch = Stretch.None,
			Margin = new Thickness(0),
		};
	}

	private static IImage LoadImageStream(ResourceView imageResource)
	{
		if (imageResource.ResourceType == "svg")
		{
			return SvgUtils.GetSvgColorImage(imageResource);
		}
		else
		{
			Stream stream = imageResource.Stream;
			return new Bitmap(stream);
		}
	}
}
