using Atlas.Core;

namespace Atlas.Tabs.Test.Chart;

[ListItem]
public class TabTestChart
{
	public static TabTestChartList List => new();
	public static TabTestChartProperties Properties => new();
	public static TabTestChartCategories Categories => new();
	public static TabTestChartTimeRangeValue TimeRange => new();
	public static TabTestChartNoData NoData => new();
}
