using Atlas.Core;
using Atlas.Tabs;
using Atlas.UI.Avalonia.Charts.LiveCharts;
using Atlas.UI.Avalonia.Controls;
using Atlas.UI.Avalonia.Themes;
using Atlas.UI.Avalonia.View;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System.Collections;
using System.Reflection;
using WeakEvent;

namespace Atlas.UI.Avalonia.Charts;

public class ChartSeries<TSeries>
{
	public ListSeries ListSeries { get; set; }
	public TSeries LineSeries { get; set; }
	public Color Color { get; set; }

	public bool IsVisible { get; set; } = true;
	public bool IsSelected { get; set; }

	public override string? ToString() => ListSeries.Name;

	public ChartSeries(ListSeries listSeries, TSeries lineSeries, Color color)
	{
		ListSeries = listSeries;
		LineSeries = lineSeries;
		Color = color;
	}
}

public class SeriesSelectedEventArgs : EventArgs
{
	public List<ListSeries> Series { get; set; }

	public SeriesSelectedEventArgs(List<ListSeries> series)
	{
		Series = series;
	}
}

public class MouseCursorMovedEventArgs : EventArgs
{
	public double X { get; set; }

	public MouseCursorMovedEventArgs(double x)
	{
		X = x;
	}
}

public class ChartGroupControl : IControlCreator
{
	public static void Register()
	{
		TabView.ControlCreators[typeof(ChartSettings)] = new ChartGroupControl();
	}

	public void AddControl(TabInstance tabInstance, TabControlSplitContainer container, object obj)
	{
		var chartSettings = (ChartSettings)obj;

		foreach (var listGroupPair in chartSettings.ChartViews)
		{
			//var tabChart = new TabControlOxyPlot(tabInstance, listGroupPair.Value, true);
			var tabChart = new TabControlLiveChart(tabInstance, listGroupPair.Value, true);

			container.AddControl(tabChart, true, SeparatorType.Spacer);
			//tabChart.OnSelectionChanged += ListData_OnSelectionChanged;
		}
	}
}

public class ChartViewControl : IControlCreator
{
	public static void Register()
	{
		TabView.ControlCreators[typeof(ChartView)] = new ChartViewControl();
	}

	public void AddControl(TabInstance tabInstance, TabControlSplitContainer container, object obj)
	{
		var chartView = (ChartView)obj;

		//var tabChart = new TabControlOxyPlot(tabInstance, chartView, true);
		var tabChart = new TabControlLiveChart(tabInstance, chartView, true);

		container.AddControl(tabChart, true, SeparatorType.Spacer);
		//tabChart.OnSelectionChanged += ListData_OnSelectionChanged;
	}
}

public interface ITabControlChart
{
	public static ITabControlChart Create(TabInstance tabInstance, ChartView chartView, bool fillHeight = false)
	{
		//return new TabControlOxyPlot(tabInstance, chartView, fillHeight);
		return new TabControlLiveChart(tabInstance, chartView, fillHeight);
	}

	public void AddAnnotation(ChartAnnotation chartAnnotation);

	public List<ChartAnnotation> Annotations { get; }
}

public class TabControlChart<TSeries> : Grid, ITabControlChart //, IDisposable
{
	public int SeriesLimit { get; set; } = 25;
	protected const double MarginPercent = 0.1; // This needs a min height so this can be lowered
	protected const int MinSelectionWidth = 10;

	public TabInstance TabInstance { get; init; }
	public ChartView ChartView { get; set; }
	public bool FillHeight { get; set; }

	public List<ChartSeries<TSeries>> ChartSeries { get; private set; } = new();
	protected Dictionary<string, ChartSeries<TSeries>> IdxNameToSeries { get; set; } = new();
	protected Dictionary<IList, ListSeries> ListToTabSeries { get; set; } = new();

	public List<ListSeries> SelectedSeries
	{
		get
		{
			List<ListSeries> selected = ChartSeries
				.Where(s => s.IsSelected)
				.Select(s => s.ListSeries)
				.ToList();

			if (selected.Count == ChartSeries.Count && selected.Count > 1)
				selected.Clear(); // If all are selected, none are selected?
			return selected;
		}
	}

	public TextBlock? TitleTextBlock { get; protected set; }

	private static readonly System.Drawing.Color NowColor = System.Drawing.Color.Green;
	public static Color[] DefaultColors { get; set; } = new Color[]
	{
		Colors.LawnGreen,
		Colors.Fuchsia,
		Colors.Cyan,
		//Colors.Aquamarine, // too close to Cyan (but more matte)
		Colors.Gold,
		Colors.DodgerBlue,
		Colors.Red,
		Colors.BlueViolet,
		//Colors.SlateBlue,
		Colors.Orange,
		//Colors.Pink,
		//Colors.Coral,
		//Colors.YellowGreen,
		Colors.Salmon,
		Colors.MediumSpringGreen,
	};
	public static Color GetColor(int index) => DefaultColors[index % DefaultColors.Length];

	protected PropertyInfo? _xAxisPropertyInfo;
	public bool UseDateTimeAxis => (_xAxisPropertyInfo?.PropertyType == typeof(DateTime)) ||
									(ChartView.TimeWindow != null);

	public bool IsTitleSelectable { get; set; }

	public List<ChartAnnotation> Annotations { get; set; } = new();

	protected static readonly WeakEventSource<MouseCursorMovedEventArgs> _mouseCursorChangedEventSource = new();

	public static event EventHandler<MouseCursorMovedEventArgs> OnMouseCursorChanged
	{
		add { _mouseCursorChangedEventSource.Subscribe(value); }
		remove { _mouseCursorChangedEventSource.Unsubscribe(value); }
	}

	public event EventHandler<SeriesSelectedEventArgs>? SelectionChanged;

	protected virtual void OnSelectionChanged(SeriesSelectedEventArgs e)
	{
		// Safely raise the event for all subscribers
		SelectionChanged?.Invoke(this, e);
	}

	public override string? ToString() => ChartView.ToString();

	public TabControlChart(TabInstance tabInstance, ChartView chartView, bool fillHeight = false)
	{
		TabInstance = tabInstance;
		ChartView = chartView;
		FillHeight = fillHeight;

		HorizontalAlignment = HorizontalAlignment.Stretch;
		//if (FillHeight)
		//			VerticalAlignment = VerticalAlignment.Top;
		//	else
		VerticalAlignment = VerticalAlignment.Stretch;

		ColumnDefinitions = new ColumnDefinitions("*");
		RowDefinitions = new RowDefinitions("*");

		MaxWidth = 1500;
		MaxHeight = 645; // 25 Items

		if (TabInstance.TabViewSettings.ChartDataSettings.Count == 0)
		{
			TabInstance.TabViewSettings.ChartDataSettings.Add(new TabDataSettings());
		}

		_xAxisPropertyInfo = chartView.Series.FirstOrDefault()?.XPropertyInfo;

		AddTitle();
	}

	private void AddTitle()
	{
		string? title = ChartView.Name;
		if (title != null)
		{
			TitleTextBlock = new TextBlock()
			{
				Text = ChartView.Name,
				FontSize = 16,
				Foreground = AtlasTheme.BackgroundText,
				Margin = new Thickness(10, 5),
				//FontWeight = FontWeight.Medium,
				[ColumnSpanProperty] = 2,
			};
			if (!ChartView.ShowOrder || ChartView.Horizontal)
			{
				TitleTextBlock.HorizontalAlignment = HorizontalAlignment.Center;
			}
			else
			{
				TitleTextBlock.Margin = new Thickness(40, 5, 5, 5);
			}
			TitleTextBlock.PointerEntered += TitleTextBlock_PointerEntered;
			TitleTextBlock.PointerExited += TitleTextBlock_PointerExited;
		}
	}

	private void TitleTextBlock_PointerEntered(object? sender, PointerEventArgs e)
	{
		if (IsTitleSelectable)
		{
			TitleTextBlock!.Foreground = AtlasTheme.GridBackgroundSelected;
		}
	}

	private void TitleTextBlock_PointerExited(object? sender, PointerEventArgs e)
	{
		TitleTextBlock!.Foreground = AtlasTheme.BackgroundText;
	}

	public virtual void AddAnnotation(ChartAnnotation chartAnnotation)
	{
		Annotations.Add(chartAnnotation);
	}

	public void AddNowTime()
	{
		var now = DateTime.UtcNow;
		if (ChartView.TimeWindow != null && ChartView.TimeWindow.EndTime < now.AddMinutes(1))
			return;

		var annotation = new ChartAnnotation
		{
			Text = "Now",
			Horizontal = false,
			X = now.Ticks,
			Color = NowColor,
			// LineStyle = LineStyle.Dot,
		};

		ChartView.Annotations.Add(annotation);
	}
}
