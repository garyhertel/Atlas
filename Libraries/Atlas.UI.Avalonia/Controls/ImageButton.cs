using Atlas.Resources;
using Atlas.UI.Avalonia.Themes;
using Atlas.UI.Avalonia.Utilities;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;

namespace Atlas.UI.Avalonia.Controls;

public class ImageButton : Button, IStyleable, ILayoutable
{
	Type IStyleable.StyleKey => typeof(Button);

	public string? Tooltip { get; set; }

	public ImageButton(ResourceView imageResource)
	{
		IImage image = LoadImageStream(imageResource.Stream);
		Initialize(image);
	}

	public ImageButton(Stream bitmapStream)
	{
		IImage image = LoadImageStream(bitmapStream);
		Initialize(image);
	}

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

		Resources.Add("ButtonBackgroundPointerOver", Theme.ToolbarButtonBackgroundHover);
		//button.Resources.Add("ButtonBackgroundPressed", Theme.ToolbarButtonBackgroundHover);
		//button.Resources.Add("ButtonPlaceholderForegroundFocused", Foreground);
		//button.Resources.Add("ButtonPlaceholderForegroundPointerOver", Foreground);

		grid.Children.Add(image);

		Content = grid;
		//Command = command;
		Background = Theme.ToolbarButtonBackground;
		BorderBrush = Background;
		BorderThickness = new Thickness(0);
		Margin = new Thickness(1);
		Padding = new Thickness(0);
		//Foreground = new SolidColorBrush(Theme.ButtonForegroundColor),
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
			//MaxWidth = 24,
			//MaxHeight = 24,
			Stretch = Stretch.None,
			Margin = new Thickness(0),
		};
	}

	private static IImage LoadImageStream(Stream bitmapStream)
	{
		IImage sourceImage;
		try
		{
			bitmapStream.Position = 0;
			sourceImage = new Bitmap(bitmapStream);
		}
		catch (Exception)
		{
			sourceImage = SvgUtils.GetSvgImage(bitmapStream);
		}

		return sourceImage;
	}
}
