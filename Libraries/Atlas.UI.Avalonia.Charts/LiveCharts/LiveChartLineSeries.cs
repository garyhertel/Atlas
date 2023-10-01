using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Kernel;
using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.Drawing;

namespace Atlas.UI.Avalonia.Charts;

public class LiveChartLineSeries : LineSeries<ObservablePoint>, ISeries
{
	IEnumerable<ChartPoint> ISeries.FindHitPoints(IChart chart, LvcPoint pointerPosition, TooltipFindingStrategy strategy)
	{
		return FindHitPoints(chart, pointerPosition, 30);
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
