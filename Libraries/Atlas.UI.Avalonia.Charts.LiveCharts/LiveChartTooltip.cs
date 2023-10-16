using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.VisualElements;
using LiveChartsCore;
using SkiaSharp;
using LiveChartsCore.SkiaSharpView.VisualElements;
using Avalonia.Controls;
using Avalonia.Media;
using System.Diagnostics;
using Avalonia.Threading;
using System.Reflection;
using LiveChartsCore.SkiaSharpView.Painting.ImageFilters;
using Atlas.Extensions;

namespace Atlas.UI.Avalonia.Charts.LiveCharts;

public class LiveChartTooltip : IChartTooltip<SkiaSharpDrawingContext>
{
	private StackPanel<RoundedRectangleGeometry, SkiaSharpDrawingContext>? _stackPanel;
	private static readonly int s_zIndex = 10050;
	private readonly SolidColorPaint _backgroundPaint = new(new SKColor(28, 49, 58)) { ZIndex = s_zIndex };
	private readonly SolidColorPaint _fontPaint = new(new SKColor(230, 230, 230)) { ZIndex = s_zIndex + 1 };

	private TextBlock? _textBlock;
	private CustomFlyout? _flyout;

	public TabControlLiveChart TabControlLiveChart { get; }

	public LiveChartTooltip(TabControlLiveChart tabControlLiveChart)
	{
		TabControlLiveChart = tabControlLiveChart;
	}

	public void Show(IEnumerable<ChartPoint> foundPoints, Chart<SkiaSharpDrawingContext> chart)
	{
		if (_stackPanel is null)
		{
			_stackPanel = new StackPanel<RoundedRectangleGeometry, SkiaSharpDrawingContext>
			{
				Padding = new Padding(25),
				Orientation = ContainerOrientation.Vertical,
				HorizontalAlignment = Align.Start,
				VerticalAlignment = Align.Middle,
				BackgroundPaint = _backgroundPaint
			};

			_stackPanel
				.Animate(
					new Animation(EasingFunctions.EaseOut, TimeSpan.FromMilliseconds(150)),
					nameof(_stackPanel.X),
					nameof(_stackPanel.Y));
		}

		// clear the previous elements.
		foreach (var child in _stackPanel.Children.ToArray())
		{
			_ = _stackPanel.Children.Remove(child);
			chart.RemoveVisual(child);
		}

		foreach (var point in foundPoints)
		{
			var sketch = ((IChartSeries<SkiaSharpDrawingContext>)point.Context.Series).GetMiniaturesSketch();
			var relativePanel = sketch.AsDrawnControl();

			var label = new LabelVisual
			{
				Text = point.SecondaryValue.ToString("C2"),
				Paint = _fontPaint,
				TextSize = 15,
				Padding = new Padding(8, 0, 0, 0),
				VerticalAlignment = Align.Start,
				HorizontalAlignment = Align.Start
			};

			var sp = new StackPanel<RoundedRectangleGeometry, SkiaSharpDrawingContext>
			{
				Padding = new Padding(0, 4),
				VerticalAlignment = Align.Middle,
				HorizontalAlignment = Align.Middle,
				Children =
				{
					relativePanel,
					label
				}
			};

			_stackPanel?.Children.Add(sp);
		}

		var size = _stackPanel.Measure(chart);

		var location = foundPoints.GetTooltipLocation(size, chart);

		_stackPanel.X = location.X;
		_stackPanel.Y = location.Y;

		chart.AddVisual(_stackPanel);
		//Dispatcher.UIThread.Post(ShowFlyout);
		//ShowFlyout();
	}

	private void ShowFlyout()
	{
		if (_flyout == null)
		{
			_flyout = new CustomFlyout()
			{
				OverlayInputPassThroughElement = TabControlLiveChart,
				//Placement = PlacementMode.Pointer,
				ShowMode = FlyoutShowMode.Transient,
				
				//Placement = PlacementMode.BottomEdgeAlignedLeft,
			};
			_textBlock = new TextBlock()
			{
				Text = "Test",
				Foreground = Brushes.White,
			};
			_flyout.Content = _textBlock;
		}
		if (_flyout.IsOpen)
		{
			Debug.WriteLine("Flyout hide");
			//_flyout.Hide();
			_flyout.UpdatePosition();
		}
		else
		{
			Debug.WriteLine("Not open");
			Debug.WriteLine("Flyout show");
			_flyout.ShowAt(TabControlLiveChart, true);
		}
	}

	public void Hide(Chart<SkiaSharpDrawingContext> chart)
	{
		if (chart is null || _stackPanel is null) return;
		chart.RemoveVisual(_stackPanel);

		if (_flyout != null && _flyout.IsOpen)
		{
			_flyout.Hide();
		}
	}
}


public class CustomFlyout : Flyout
{
	private MethodInfo _positionChangedMethod;

	public CustomFlyout()
	{
		_positionChangedMethod = Popup.GetType().GetMethod("HandlePositionChange",
			BindingFlags.NonPublic | BindingFlags.Instance)!;

		//Popup.IsLightDismissEnabled = false;
		//Popup.OverlayDismissEventPassThrough = false;
		//Popup.Focusable = false;
		//Popup.IsHitTestVisible = false;
	}

	public void UpdatePosition()
	{
		_positionChangedMethod.Invoke(Popup, new object[] {  });
	}
}

public class LiveChartTooltip2 : IChartTooltip<SkiaSharpDrawingContext>
{
	public double TextSize { get; set; } = 15;
	public double MaxTooltipsAndLegendsLabelsWidth { get; set; } = 300;
	 
	private static readonly int s_zIndex = 10100;

	internal StackPanel<PopUpGeometry, SkiaSharpDrawingContext>? _panel;
	private IPaint<SkiaSharpDrawingContext>? _backgroundPaint;
	public TabControlLiveChart LiveChart;

	public LiveChartTooltip2(TabControlLiveChart liveChart)
	{
		LiveChart = liveChart;
		FontPaint = new SolidColorPaint(new SKColor(28, 49, 58));
		BackgroundPaint = new SolidColorPaint(new SKColor(235, 235, 235, 230))
		{
			ImageFilter = new DropShadow(2, 2, 6, 6, new SKColor(50, 0, 0, 100))
		};
	}

	public IPaint<SkiaSharpDrawingContext>? FontPaint { get; set; }

	public IPaint<SkiaSharpDrawingContext>? BackgroundPaint
	{
		get => _backgroundPaint;
		set
		{
			_backgroundPaint = value;
			if (value is not null)
			{
				value.IsFill = true;
			}
		}
	}

	public void Show(IEnumerable<ChartPoint> foundPoints, Chart<SkiaSharpDrawingContext> chart)
	{
		const int wedge = 10;

		if (chart.View.TooltipTextSize is not null) TextSize = chart.View.TooltipTextSize.Value;
		if (chart.View.TooltipBackgroundPaint is not null) BackgroundPaint = chart.View.TooltipBackgroundPaint;
		if (chart.View.TooltipTextPaint is not null) FontPaint = chart.View.TooltipTextPaint;

		if (_panel is null)
		{
			_panel = new StackPanel<PopUpGeometry, SkiaSharpDrawingContext>
			{
				Orientation = ContainerOrientation.Vertical,
				HorizontalAlignment = Align.Middle,
				VerticalAlignment = Align.Middle,
				BackgroundPaint = BackgroundPaint
			};

			_panel.BackgroundGeometry.Wedge = wedge;
			_panel.BackgroundGeometry.WedgeThickness = 3;

			_panel
				.Animate(
					new Animation(EasingFunctions.EaseOut, TimeSpan.FromMilliseconds(150)),
					nameof(RoundedRectangleGeometry.X),
					nameof(RoundedRectangleGeometry.Y));
		}

		if (BackgroundPaint is not null) BackgroundPaint.ZIndex = s_zIndex;
		if (FontPaint is not null) FontPaint.ZIndex = s_zIndex + 1;

		foreach (var child in _panel.Children.ToArray())
		{
			_ = _panel.Children.Remove(child);
			chart.RemoveVisual(child);
		}

		var tableLayout = new TableLayout<RoundedRectangleGeometry, SkiaSharpDrawingContext>
		{
			HorizontalAlignment = Align.Middle,
			VerticalAlignment = Align.Middle
		};

		var lw = (float)MaxTooltipsAndLegendsLabelsWidth;

		if (LiveChart.CursorPosition == null || !foundPoints.Any()) return;
		var cursorPosition = LiveChart.CursorPosition.Value;
		var cursorPoint = new LvcPoint(cursorPosition.X, cursorPosition.Y);

		ChartPoint closestPoint = foundPoints
			.Select(x => new { distance = LiveChartLineSeries.GetDistanceTo(x, cursorPoint), point = x })
			.MinBy(x => x.distance)!
			.point;

		// Points are in chart series order, not closest
		// Use pointer moved value in chart to find closest?
		if (closestPoint.Context.Series is LiveChartLineSeries lineSeries)
		{
			string? title = lineSeries.LiveChartSeries.GetTooltipTitle();
			if (title != null)
			{
				_panel.Children.Add(
					new LabelVisual
					{
						Text = title,
						Paint = FontPaint,
						TextSize = TextSize,
						Padding = new Padding(0, 0, 0, 0),
						MaxWidth = lw,
						VerticalAlignment = Align.Start,
						HorizontalAlignment = Align.Start,
						ClippingMode = LiveChartsCore.Measure.ClipMode.XY
					});

				_panel.Children.Add(
					new StackPanel<LiveChartsCore.SkiaSharpView.Drawing.Geometries.RectangleGeometry, SkiaSharpDrawingContext> { Padding = new(0, 8) });
			}

			var lines = lineSeries.LiveChartSeries.GetTooltipLines(closestPoint);

			for (int j = 0; j < lines.Length; j++)
			{
				string line = lines[j];
				if (!line.IsNullOrEmpty())
				{
					tableLayout.AddChild(
						new LabelVisual
						{
							Text = lines[j],
							Paint = FontPaint,
							TextSize = TextSize,
							Padding = new Padding(0, 0, 0, 0),
							MaxWidth = lw,
							VerticalAlignment = Align.Start,
							HorizontalAlignment = Align.Start,
							ClippingMode = LiveChartsCore.Measure.ClipMode.None
						}, j, 1, horizontalAlign: Align.Start);
				}
				else
				{
					tableLayout.AddChild(
						new StackPanel<LiveChartsCore.SkiaSharpView.Drawing.Geometries.RectangleGeometry, SkiaSharpDrawingContext> { Padding = new(0, 8) }, j, 1);
				}
			}


			/*var series = (IChartSeries<SkiaSharpDrawingContext>)point.Context.Series;

			tableLayout.AddChild(series.GetMiniaturesSketch().AsDrawnControl(s_zIndex), i, 0);

			tableLayout.AddChild(
				new LabelVisual
				{
					Text = point.Context.Series.Name ?? string.Empty,
					Paint = FontPaint,
					TextSize = TextSize,
					Padding = new Padding(10, 0, 0, 0),
					MaxWidth = lw,
					VerticalAlignment = Align.Start,
					HorizontalAlignment = Align.Start,
					ClippingMode = LiveChartsCore.Measure.ClipMode.None
				}, i, 1, horizontalAlign: Align.Start);

			tableLayout.AddChild(
				new LabelVisual
				{
					Text = content,
					Paint = FontPaint,
					TextSize = TextSize,
					Padding = new Padding(10, 0, 0, 0),
					MaxWidth = lw,
					VerticalAlignment = Align.Start,
					HorizontalAlignment = Align.Start,
					ClippingMode = LiveChartsCore.Measure.ClipMode.None
				}, i, 2, horizontalAlign: Align.End);
			*/
		}

		_panel.Children.Add(tableLayout);

		var size = _panel.Measure(chart);
		_ = foundPoints.GetTooltipLocation(size, chart);
		_panel.BackgroundGeometry.Placement = chart.AutoToolTipsInfo.ToolTipPlacement;

		switch (chart.AutoToolTipsInfo.ToolTipPlacement)
		{
			case LiveChartsCore.Measure.PopUpPlacement.Top:
				_panel.Padding = new Padding(12, 8, 12, 8 + wedge); break;
			case LiveChartsCore.Measure.PopUpPlacement.Bottom:
				_panel.Padding = new Padding(12, 8 + wedge, 12, 8); break;
			case LiveChartsCore.Measure.PopUpPlacement.Left:
				_panel.Padding = new Padding(12, 8, 12 + wedge, 8); break;
			case LiveChartsCore.Measure.PopUpPlacement.Right:
				_panel.Padding = new Padding(12 + wedge, 8, 12, 8); break;
			default: break;
		}

		// the size changed... we need to do the math again
		size = _panel.Measure(chart);
		var location = foundPoints.GetTooltipLocation(size, chart);

		_panel.X = location.X;
		_panel.Y = location.Y;

		chart.AddVisual(_panel);
	}

	public void Hide(Chart<SkiaSharpDrawingContext> chart)
	{
		if (chart is null || _panel is null) return;
		chart.RemoveVisual(_panel);
	}
}
