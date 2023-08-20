using Avalonia.Media;
using OxyPlot;

namespace Atlas.UI.Avalonia.Charts;

public class TabOxyPlotLegendItem : TabChartLegendItem<OxyPlotLineSeries>
{
	public readonly TabControlOxyPlotLegend OxyLegend;
	public readonly OxyPlotChartSeries OxyPlotChartSeries;

	//private OxyColor oxyColor;
	private MarkerType markerType;

	public List<DataPoint>? Points { get; internal set; }

	public override string ToString() => Series.ToString() ?? GetType().ToString();

	public TabOxyPlotLegendItem(TabControlOxyPlotLegend legend, ChartSeries<OxyPlotLineSeries> chartSeries) :
		base(legend, chartSeries)
	{
		OxyLegend = legend;
	}

	/*
	public void UpdateTotal()
	{
		if (OxyPlotChartSeries.ListSeries != null)
		{
			Total = OxyPlotChartSeries.ListSeries.Total;
			Count = OxyPlotChartSeries.ListSeries.List.Count;
			if (TextBlockTotal != null)
				TextBlockTotal.Text = DateTimeFormat.ValueFormatter(Total);
			return;
		}

		Total = 0;
		Count = 0;

		if (Series is OxyPlot.Series.LineSeries lineSeries)
		{
			if (lineSeries.Points.Count > 0)
			{
				Count = lineSeries.Points.Count;
				foreach (DataPoint dataPoint in lineSeries.Points)
				{
					if (!double.IsNaN(dataPoint.Y))
						Total += dataPoint.Y;
				}
			}
			else if (lineSeries.ItemsSource != null)
			{
				// todo: finish
				Count = lineSeries.ItemsSource.GetEnumerator().MoveNext() ? 1 : 0;
				Total = Count;
			}
		}

		if (Total > 100)
			Total = Math.Round(Total);

		if (Series is OxyPlot.Series.ScatterSeries scatterSeries)
		{
			// todo: finish
			Count = Math.Max(scatterSeries.Points.Count, scatterSeries.ItemsSource.GetEnumerator().MoveNext() ? 1 : 0);
			Total = Count;
		}
	}*/

	public override void UpdateVisible()
	{
		var lineSeries = ChartSeries.LineSeries;
		//lineSeries.IsVisible = IsSelected || Highlight;
		 
		if (IsSelected || _highlight)
		{
			if (Points != null)
			{
				lineSeries.Points.Clear();
				lineSeries.Points.AddRange(Points);
			}
			//lineSeries.ItemsSource = lineSeries.ItemsSource ?? ItemsSource; // never gonna let you go...
			//ItemsSource = null;
			lineSeries.LineStyle = LineStyle.Solid;
			lineSeries.MarkerType = markerType;
			lineSeries.Selectable = true;
		}
		else
		{
			if (lineSeries.Points.Count > 0)
			{
				Points = new List<DataPoint>(lineSeries.Points);
			}
			lineSeries.Points.Clear();
			//lineSeries.Points = new List<DataPoint>();
			//ItemsSource = lineSeries.ItemsSource ?? ItemsSource;
			//lineSeries.ItemsSource = null;
			lineSeries.LineStyle = LineStyle.None;
			lineSeries.MarkerType = MarkerType.None;
			lineSeries.Selectable = false;
			//lineSeries.SelectionMode = OxyPlot.SelectionMode.
			lineSeries.Unselect();
		}
	}

	/*public void UpdateVisible(OxyPlot.Series.ScatterSeries scatterSeries)
	{
		Series = scatterSeries;
		if (IsSelected || Highlight)
		{
			scatterSeries.ItemsSource ??= ItemsSource;
			// ItemsSource = null;
			scatterSeries.MarkerType = markerType;
			scatterSeries.Selectable = true;
		}
		else
		{
			ItemsSource = scatterSeries.ItemsSource ?? ItemsSource;
			scatterSeries.ItemsSource = null;
			scatterSeries.MarkerType = MarkerType.None;
			scatterSeries.Selectable = false;
			//lineSeries.SelectionMode = OxyPlot.SelectionMode.
			scatterSeries.Unselect();
		}
	}

	public void UpdateHighlight(bool showFaded)
	{
		OxyColor newColor;
		if (Highlight || !showFaded)
			newColor = oxyColor;
		else
			newColor = OxyColor.FromAColor(32, oxyColor);

		if (Series is OxyPlot.Series.LineSeries lineSeries)
		{
			lineSeries.MarkerFill = newColor;
			lineSeries.Color = newColor;
		}
	}*/


	public override void UpdateColor(Color color)
	{
		if (Series is OxyPlot.Series.LineSeries lineSeries)
		{
			var newColor = OxyColor.FromArgb(color.A, color.R, color.G, color.B);
			lineSeries.MarkerFill = newColor;
			lineSeries.Color = newColor;
		}
	}

	/*public override void UpdateVisible()
	{
		ChartSeries.LineSeries.IsVisible = IsSelected || Highlight;
	}*/
}
