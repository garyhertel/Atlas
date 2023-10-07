using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Kernel;
using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.Drawing;

namespace Atlas.UI.Avalonia.Charts;


public class LiveChartPoint : ObservablePoint
{
	public object Object { get; set; }

	public LiveChartPoint(object obj, double? x, double? y) : base(x, y)
	{
		Object = obj;
	}
}

public class LiveChartLineSeries : LineSeries<LiveChartPoint>, ISeries
{
	private const int MaxFindDistance = 30;

	public LiveChartSeries LiveChartSeries;

	public LiveChartLineSeries(LiveChartSeries liveChartSeries)
	{
		LiveChartSeries = liveChartSeries;
	}

	public new IEnumerable<ChartPoint> Fetch(IChart chart) => base.Fetch(chart);

	IEnumerable<ChartPoint> ISeries.FindHitPoints(IChart chart, LvcPoint pointerPosition, TooltipFindingStrategy strategy)
	{
		return FindHitPoints(chart, pointerPosition, MaxFindDistance);
	}

	List<ChartPoint> FindHitPoints(IChart chart, LvcPoint pointerPosition, double maxDistance)
	{
		return Fetch(chart)
			.Select(x => new { distance = GetDistanceTo(x, pointerPosition), point = x })
			.Where(x => x.distance < maxDistance)
			.OrderBy(x => x.distance)
			.SelectFirst(x => x.point)
			.ToList();
	}

	public static double GetDistanceTo(ChartPoint target, LvcPoint location)
	{
		if (target.Context.Chart is not ICartesianChartView<SkiaSharpDrawingContext> cartesianChart)
		{
			throw new NotImplementedException();
		}

		var cartesianSeries = (ICartesianSeries<SkiaSharpDrawingContext>)target.Context.Series;

		var primaryAxis = cartesianChart.Core.YAxes[cartesianSeries.ScalesXAt];
		var secondaryAxis = cartesianChart.Core.XAxes[cartesianSeries.ScalesYAt];

		var drawLocation = cartesianChart.Core.DrawMarginLocation;
		var drawMarginSize = cartesianChart.Core.DrawMarginSize;

		var secondaryScale = new Scaler(drawLocation, drawMarginSize, secondaryAxis);
		var primaryScale = new Scaler(drawLocation, drawMarginSize, primaryAxis);

		var coordinate = target.Coordinate;

		double x = secondaryScale.ToPixels(coordinate.SecondaryValue);
		double y = primaryScale.ToPixels(coordinate.PrimaryValue);

		// calculate the distance
		var dx = location.X - x;
		var dy = location.Y - y;

		double distance = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
		return distance;
	}
}
