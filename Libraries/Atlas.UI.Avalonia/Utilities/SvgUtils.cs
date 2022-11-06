using Avalonia.Media;
using Avalonia.Svg.Skia;

namespace Atlas.UI.Avalonia.Utilities;

public static class SvgUtils
{
	public static IImage GetSvgImage(Stream bitmapStream)
	{
		IImage sourceImage;
		bitmapStream.Position = 0;


		using var reader = new StreamReader(bitmapStream);
		string text = reader.ReadToEnd();
		//string updated = text.Replace("rgb(0,0,0)", "rgb(101,119,204)");
		string updated = text.Replace("rgb(0,0,0)", "rgb(0,109,240)");

		//6577cc


		//var svgSource = new SvgDocument();
		/*var model = SvgExtensions.Open(bitmapStream);
		//model.Color = new SKColor(255, 255, 255, 255);

		foreach (SvgGroup group in model.Children[0].Children)
		{
			foreach (SvgElement element in group.Children)
			{
				//element.Color = new SKColor(255, 255, 255, 255);
			}
		}*/

		var svgSource = new SvgSource();
		//svgSource.Load(bitmapStream);
		//svgSource.FromSvgDocument(model);
		svgSource.FromSvg(updated);
		/*foreach (CanvasCommand canvasCommand in svgSource!.Model!.Commands!)
		{
			if (canvasCommand is DrawPathCanvasCommand drawCommand)
			{
				var color = new SKColor(255, 255, 255, 255);
				drawCommand.Paint.Color = color;
				drawCommand.Paint.Shader = SKShader.CreateColor(color, SKColorSpace.Srgb);
			}
		}*/
		/*var tempStream = new MemoryStream();
		svgSource.Save(tempStream, SkiaSharp.SKColor.Empty);
		tempStream.Position = 0;
		sourceImage = new Bitmap(tempStream);*/

		//svgSource.Load(tempStream);
		sourceImage = new SvgImage()
		{
			Source = svgSource,
		};


		/*using (var svg = new SKSvg())
		{
			svg.Load(stream);
			//svg.Picture.ToBitmap(SKColor.Empty, 1f, 1f, SKColorType.Unknown)
			svg.Picture.ToImage(stream, SKColors.Empty, SKEncodedImageFormat.Png, 100, 1f, 1f, SKColorType.Unknown, SKAlphaType.Unknown, SKColorSpace.);
		}*/
		return sourceImage;
	}
}
