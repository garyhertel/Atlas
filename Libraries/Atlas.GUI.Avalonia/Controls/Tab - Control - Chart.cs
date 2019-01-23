﻿using Atlas.Core;
using Atlas.Tabs;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Avalonia;

namespace Atlas.GUI.Avalonia.Controls
{
	public class TabControlChart : UserControl //, IDisposable
	{
		//private string name;
		private TabInstance tabInstance;
		public ChartSettings ChartSettings { get; set; }
		public ListGroup ListGroup { get; set; }

		//private List<ListSeries> ListSeries { get; set; }
		private Dictionary<IList, ListSeries> ListToTabSeries { get; set; } = new Dictionary<IList, ListSeries>();
		private Dictionary<IList, int> ListToTabIndex { get; set; } = new Dictionary<IList, int>(); // not used

		//public SeriesCollection SeriesCollection { get; set; }
		public string[] Labels { get; set; }
		public Func<double, string> YFormatter { get; set; }

		// try to change might be lower or higher than the rendering interval
		private const int UpdateInterval = 20;

		//private bool disposed;
		//private readonly Timer timer;
		//private int numberOfSeries;

		private TabControlDataGrid tabControlDataGrid;
		private PlotModel plotModel;
		private PlotView plotView;


		//public event EventHandler<EventArgs> OnSelectionChanged;
		//private bool autoSelectNew = true;

		public TabControlChart(TabInstance tabInstance, ChartSettings chartSettings, ListGroup listGroup)
		{
			this.tabInstance = tabInstance;
			this.ChartSettings = chartSettings;
			this.ListGroup = listGroup;

			InitializeControls();
			//DataContext = new TestNode().Children;
		}

		public override string ToString()
		{
			return ChartSettings.ToString(); // todo: fix for multiple
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			return base.MeasureOverride(availableSize);
		}

		private void Initialize()
		{
			InitializeControls();
		}

		protected override void OnMeasureInvalidated()
		{
			base.OnMeasureInvalidated();
		}

		///public class LabelControl
		//{
			/*TextBlock labelTitle = new TextBlock()
			{
				Text = ToString(),
				Background = new SolidColorBrush(Theme.TitleBackgroundColor),
				Foreground = new SolidColorBrush(Theme.TitleForegroundColor),
				FontSize = 14,
				//Margin = new Thickness(2), // Shows as black, Need Padding so Border not needed
				HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
				//VerticalAlignment = VerticalAlignment.Auto, // doesn't exist
				//Height = 24,
			};
			//this.Children.Add(labelTitle);

			Border borderTitle = new Border()
			{
				//BorderThickness = new Thickness(10),
				BorderThickness = new Thickness(5, 2, 2, 2),
				//Background = new SolidColorBrush(Theme.GridColumnHeaderBackgroundColor),
				//Background = new SolidColorBrush(Colors.Crimson),
				BorderBrush = new SolidColorBrush(Theme.TitleBackgroundColor),
				[Grid.RowProperty] = 0,
				[Grid.ColumnSpanProperty] = 2,
			};
			borderTitle.Child = labelTitle;*/
		//}

		// don't want to reload this because 
		private void InitializeControls()
		{
			this.Background = new SolidColorBrush(Theme.BackgroundColor);
			this.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch; // OxyPlot import collision
			this.VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch;
			//this.Width = 1000;
			//this.Height = 1000;
			//this.Children.Add(border);
			//this.Orientation = Orientation.Vertical;

			// autogenerate columns
			if (tabInstance.tabViewSettings.ChartDataSettings.Count == 0)
				tabInstance.tabViewSettings.ChartDataSettings.Add(new TabDataSettings());
			//tabDataGrid = new TabDataGrid(tabInstance, ChartSettings.ListSeries, true, tabInstance.tabViewSettings.ChartDataSettings);
			tabControlDataGrid = new TabControlDataGrid(tabInstance, ListGroup.ListSeries, true, tabInstance.tabViewSettings.ChartDataSettings[0]);
			//Grid.SetRow(tabDataGrid, 1);

			//tabDataGrid.AddButtonColumn("<>", nameof(TaskInstance.Cancel));

			//tabDataGrid.AutoLoad = tabModel.AutoLoad;
			tabControlDataGrid.OnSelectionChanged += TabData_OnSelectionChanged;
			//tabDataGrid.Width = 1000;
			//tabDataGrid.Height = 1000;
			//tabDataGrid.Initialize();
			//bool addSplitter = false;
			//tabParentControls.AddControl(tabDataGrid, true, false);
			
			plotView = new PlotView()
			{
				HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
				VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch,
				MaxWidth = 1000,
				MaxHeight = 1000,
				//[Grid.RowProperty] = 1,
				[Grid.ColumnProperty] = 1,
				
				//
				//Background = new SolidColorBrush(Colors.White),
			};
			LoadPlotModel();
			/*plotView.Template = new ControlTemplate() // todo: fix
			{
				Content = new object(),
				TargetType = typeof(object),
			};*/

			// Doesn't work for Children that Stretch?
			/*StackPanel stackPanel = new StackPanel();
			stackPanel.Orientation = Orientation.Vertical;
			stackPanel.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch;
			stackPanel.VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch;
			stackPanel.Children.Add(borderTitle);
			stackPanel.Children.Add(plotView);*/


			Grid containerGrid = new Grid()
			{
				ColumnDefinitions = new ColumnDefinitions("Auto,*"),
				RowDefinitions = new RowDefinitions("*"), // Header, Body
				HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
				VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch,
				//Background = new SolidColorBrush(Theme.BackgroundColor),
			};
			//containerGrid.Children.Add(borderTitle);

			containerGrid.Children.Add(tabControlDataGrid);

			containerGrid.Children.Add(plotView);

			//this.watch.Start();
			this.Content = containerGrid;

			this.Focusable = true;
			this.GotFocus += Tab_GotFocus;
			this.LostFocus += Tab_LostFocus;
		}

		private void LoadPlotModel()
		{
			UnloadModel();

			plotModel = new OxyPlot.PlotModel()
			{
				//Title = name,
				LegendPlacement = LegendPlacement.Outside,
			};
			plotModel.Axes.Add(new OxyPlot.Axes.LinearAxis { Position = AxisPosition.Left });

			foreach (ListSeries listSeries in tabControlDataGrid.SelectedItems)
				AddSeries(listSeries);

			// would need to be able to disable to use
			//foreach (ListSeries listSeries in ChartSettings.ListSeries)
			//	AddSeries(listSeries);

			plotView.Model = plotModel;
		}

		private void UnloadModel()
		{
			//if (plotModel != null)
			//	plotModel.Series.Clear();
			foreach (ListSeries listSeries in ChartSettings.ListSeries)
			{
				INotifyCollectionChanged iNotifyCollectionChanged = listSeries.iList as INotifyCollectionChanged;
				//if (iNotifyCollectionChanged != null)
				//	iNotifyCollectionChanged.CollectionChanged -= INotifyCollectionChanged_CollectionChanged;
			}
		}

		private void AddSeries(ListSeries listSeries)
		{
			var lineSeries = new OxyPlot.Series.LineSeries
			{
				Title = listSeries.Name,
				LineStyle = LineStyle.Solid,
				StrokeThickness = 2,
			};
			AddPoints(listSeries, listSeries.iList, lineSeries);

			plotModel.Series.Add(lineSeries);

			INotifyCollectionChanged iNotifyCollectionChanged = listSeries.iList as INotifyCollectionChanged;
			if (iNotifyCollectionChanged != null)
				//iNotifyCollectionChanged.CollectionChanged += INotifyCollectionChanged_CollectionChanged;
				iNotifyCollectionChanged.CollectionChanged += new NotifyCollectionChangedEventHandler(delegate (object sender, NotifyCollectionChangedEventArgs e)
				{
					// can we remove this later when disposing?
					SeriesChanged(listSeries, e.NewItems, lineSeries);
				});

			ListToTabSeries[listSeries.iList] = listSeries;
			ListToTabIndex[listSeries.iList] = ListToTabIndex.Count;
		}

		private static void AddPoints(ListSeries listSeries, IList iList, OxyPlot.Series.LineSeries lineSeries)
		{
			if (listSeries.propertyInfo != null)
			{
				foreach (object obj in iList)
				{
					object value = listSeries.propertyInfo.GetValue(obj);
					lineSeries.Points.Add(new DataPoint(lineSeries.Points.Count, (dynamic)value));
				}
			}
			else
			{
				foreach (object obj in iList)
				{
					lineSeries.Points.Add(new DataPoint(lineSeries.Points.Count, (dynamic)obj));
				}
			}
		}

		private void TabData_OnSelectionChanged(object sender, EventArgs e)
		{
			UnloadModel();
			LoadPlotModel();
		}

		private void SeriesChanged(ListSeries listSeries, IList iList, OxyPlot.Series.LineSeries lineSeries)
		{
			lock (this.plotModel.SyncRoot)
			{
				//this.Update();
				AddPoints(listSeries, iList, lineSeries);
			}

			Dispatcher.UIThread.InvokeAsync(() => this.plotModel.InvalidatePlot(true), DispatcherPriority.Background);
		}
		/*private void INotifyCollectionChanged_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			lock (this.plotModel.SyncRoot)
			{
				//this.Update();
				int index = ListToTabIndex[(IList)sender];
				ListSeries listSeries = ListToTabSeries[(IList)sender];
				AddPoints((OxyPlot.Series.LineSeries)plotModel.Series[index], listSeries, e.NewItems);
			}

			Dispatcher.UIThread.InvokeAsync(() => this.plotModel.InvalidatePlot(true), DispatcherPriority.Background);
		}*/

		public void Dispose()
		{
			UnloadModel();
		}

		private void Tab_LostFocus(object sender, RoutedEventArgs e)
		{
			this.Background = new SolidColorBrush(Theme.BackgroundColor);
		}

		private void Tab_GotFocus(object sender, RoutedEventArgs e)
		{
			this.Background = new SolidColorBrush(Theme.BackgroundFocusedColor);
		}
	}
}
