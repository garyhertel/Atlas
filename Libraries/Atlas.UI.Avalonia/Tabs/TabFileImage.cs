using Atlas.Core;
using Atlas.Resources;
using Atlas.Tabs;
using Atlas.Tabs.Tools;
using Atlas.UI.Avalonia.Utilities;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Atlas.UI.Avalonia.Tabs;

public class TabFileImage : ITab, IFileTypeView
{
	public static readonly string[] DefaultExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".svg" };

	public string? Path { get; set; }

	public TabFileImage() { }

	public TabFileImage(string path)
	{
		Path = path;
	}

	public TabInstance Create() => new Instance(this);

	public class Toolbar : TabToolbar
	{
		public ToolButton ButtonOpenFolder { get; set; } = new("Open Folder", Icons.Svg.OpenFolder);

		[Separator]
		public ToolButton ButtonDelete { get; set; } = new("Delete", Icons.Svg.Delete);
	}

	public class Instance(TabFileImage tab) : TabInstance
	{
		public string Path => tab.Path!;

		public Image? Image;

		public override void LoadUI(Call call, TabModel model)
		{
			model.CustomSettingsPath = Path;

			if (!File.Exists(Path))
			{
				model.AddObject("File doesn't exist");
				return;
			}

			Image = new Image()
			{
				VerticalAlignment = VerticalAlignment.Top,
			};

			try
			{
				if (Path.ToLower().EndsWith(".svg"))
				{
					if (SvgUtils.TryGetSvgImage(Path, out IImage? imageSource))
					{
						Image.Source = imageSource;
						model.MaxDesiredWidth = Math.Max(100, (int)imageSource.Size.Width);
					}
				}
				else
				{
					Bitmap bitmap = ImageUtils.LoadImage(Image, Path)!;
					model.MaxDesiredWidth = Math.Max(100, (int)bitmap.Size.Width);
				}
				model.AddObject(Image, true);
			}
			catch (Exception ex)
			{
				call.Log.Add(ex);
				model.AddObject(ex);
			}
		}
	}
}
