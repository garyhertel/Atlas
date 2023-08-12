using Atlas.Core;
using Atlas.Tabs;
using Atlas.UI.Avalonia.Charts.LiveCharts;
using Atlas.UI.Avalonia.Controls;
using Atlas.UI.Avalonia.Themes;
using Atlas.UI.Avalonia.View;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System.Collections;

namespace Atlas.UI.Avalonia.Charts;

public class ChartSeries<TSeries>
{
	public ListSeries ListSeries { get; set; }
	public TSeries LineSeries { get; set; }
	public Color Color { get; set; }

	public bool IsVisible { get; set; } = true;
	public bool IsSelected { get; set; }

	public override string ToString() => ListSeries.ToString();

	public ChartSeries(ListSeries listSeries, TSeries lineSeries)
	{
		ListSeries = listSeries;
		LineSeries = lineSeries;
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

		foreach (var listGroupPair in chartSettings.ListGroups)
		{
			var tabChart = new TabControlLiveChart(tabInstance, listGroupPair.Value, true);

			container.AddControl(tabChart, true, SeparatorType.Spacer);
			//tabChart.OnSelectionChanged += ListData_OnSelectionChanged;
		}
	}
}

public class TabControlChart<TSeries> : Grid //, IDisposable
{
	public int SeriesLimit { get; set; } = 25;
	protected const double MarginPercent = 0.1; // This needs a min height so this can be lowered
	protected const int MinSelectionWidth = 10;

	public readonly TabInstance TabInstance;
	public ListGroup ListGroup { get; set; }
	public bool FillHeight { get; set; }

	public List<ChartSeries<TSeries>> ChartSeries = new();
	protected Dictionary<string, ChartSeries<TSeries>> IdxNameToSeries { get; set; } = new();
	protected Dictionary<IList, ListSeries> ListToTabSeries { get; set; } = new();

	public TextBlock? TitleTextBlock { get; protected set; }
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

	public bool IsTitleSelectable { get; set; }

	public override string? ToString() => ListGroup.ToString();

	public TabControlChart(TabInstance tabInstance, ListGroup listGroup, bool fillHeight = false)
	{
		TabInstance = tabInstance;
		ListGroup = listGroup;
		FillHeight = fillHeight;

		HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch; // OxyPlot import collision
																				   //if (FillHeight)
																				   //			VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top;
																				   //	else
		VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch;

		ColumnDefinitions = new ColumnDefinitions("*");
		RowDefinitions = new RowDefinitions("*");

		MaxWidth = 1500;
		MaxHeight = 645; // 25 Items

		if (TabInstance.TabViewSettings.ChartDataSettings.Count == 0)
			TabInstance.TabViewSettings.ChartDataSettings.Add(new TabDataSettings());


		string? title = ListGroup.Name;
		if (title != null)
		{
			TitleTextBlock = new TextBlock()
			{
				Text = ListGroup.Name,
				FontSize = 16,
				Foreground = AtlasTheme.BackgroundText,
				Margin = new Thickness(10, 5),
				//FontWeight = FontWeight.Medium,
				[ColumnSpanProperty] = 2,
			};
			if (!ListGroup.ShowOrder || ListGroup.Horizontal)
				TitleTextBlock.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center;
			else
				TitleTextBlock.Margin = new Thickness(40, 5, 5, 5);
			TitleTextBlock.PointerEntered += TitleTextBlock_PointerEntered;
			TitleTextBlock.PointerExited += TitleTextBlock_PointerExited;
		}
	}

	private void TitleTextBlock_PointerEntered(object? sender, PointerEventArgs e)
	{
		if (IsTitleSelectable)
			TitleTextBlock!.Foreground = AtlasTheme.GridBackgroundSelected;
	}

	private void TitleTextBlock_PointerExited(object? sender, PointerEventArgs e)
	{
		TitleTextBlock!.Foreground = AtlasTheme.BackgroundText;
	}
}
