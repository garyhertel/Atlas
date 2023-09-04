using Atlas.Core;
using Atlas.Extensions;
using System.Collections;
using System.Reflection;

namespace Atlas.Tabs;

// Call ChartGroupControl.Register() to register UI Control
public class ChartSettings
{
	private const string DefaultViewName = "Default";

	public string? Name { get; set; }
	private ChartView DefaultChartView { get; set; } = new(DefaultViewName);
	public Dictionary<string, ChartView> ChartViews { get; set; } = new();
	//public ItemCollection<ChartView> ChartViews { get; set; } = new();
	public ItemCollection<ListSeries> ListSeries { get; set; } = new();

	public override string ToString() => string.Join(" ", ListSeries);

	public ChartSettings() { }

	public ChartSettings(ChartView chartView)
	{
		AddView(chartView);
	}

	public ChartSettings(ListSeries listSeries, string? name = null)
	{
		Name = name;
		DefaultChartView.Name = name ?? DefaultChartView.Name;

		AddSeries(listSeries);
	}

	public ChartSettings(IList iList, string? name = null)
	{
		Name = name;
		DefaultChartView.Name = name ?? DefaultChartView.Name;
		LoadList(iList);
	}

	public void LoadList(IList iList)
	{
		Type type = iList.GetType();
		Type? elementType = null;
		if (iList is Array)
			elementType = type.GetElementType();
		else //if (type.GenericTypeArguments.Length > 0)
			elementType = type.GenericTypeArguments[0];

		if (elementType!.IsPrimitive)
		{
			AddList("Values", iList);
			return;
		}

		PropertyInfo xAxisPropertyInfo = elementType!.GetPropertyWithAttribute<XAxisAttribute>()!;

		PropertyInfo[] properties = elementType.GetProperties()
			.OrderBy(x => x.MetadataToken)
			.ToArray();

		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.DeclaringType!.IsNotPublic)
				continue;

			if (propertyInfo.PropertyType.IsNumeric())
			{
				var listSeries = new ListSeries(iList, xAxisPropertyInfo, propertyInfo);
				//listProperties.Add(listSeries);

				ChartView? chartView = DefaultChartView;
				UnitAttribute? attribute = propertyInfo.GetCustomAttribute<UnitAttribute>();
				if (attribute != null)
				{
					if (!ChartViews.TryGetValue(attribute.Name, out chartView))
					{
						chartView = new ChartView(attribute.Name);
						ChartViews[attribute.Name] = chartView;
					}
				}
				else
				{
					if (!ChartViews.ContainsKey(chartView.Name!))
					{
						ChartViews[chartView.Name!] = chartView;
					}
				}
				// Will add to Default Group if no Unit specified, and add the Default Group if needed
				chartView.Series.Add(listSeries);
				ListSeries.Add(listSeries);
			}
		}
	}

	// todo: this needs to be reworked when a use is found
	public void AddList(string label, IList iList)
	{
		var listSeries = new ListSeries(label, iList);

		//if (ListGroups.TryGetValue(label, out ListGroup listGroup)

		ChartView chartView = DefaultChartView;
		chartView.Name = label ?? chartView.Name;
		// Will add to Default Group if no Unit specified, and add the Default Group if needed
		ChartViews.Add(chartView.Name!, chartView);
		chartView.Series.Add(listSeries);
		ListSeries.Add(listSeries);
	}

	public void AddView(ChartView chartView)
	{
		ChartViews.Add(chartView.Name!, chartView);
		ListSeries.AddRange(chartView.Series);
	}

	public void AddSeries(ListSeries listSeries)
	{
		ChartView chartView = DefaultChartView;
		if (chartView.Name == DefaultViewName)
			chartView.Name = listSeries.Name ?? chartView.Name;

		// Will add to Default Group if no Unit specified, and add the Default Group if needed
		ChartViews.Add(chartView.Name!, chartView);
		chartView.Series.Add(listSeries);
		ListSeries.Add(listSeries);
	}

	public void SetTimeWindow(TimeWindow timeWindow, bool showTimeTracker)
	{
		foreach (ChartView chartView in ChartViews.Values)
		{
			chartView.TimeWindow = timeWindow;
			chartView.ShowTimeTracker = showTimeTracker;
		}
	}

	// todo: add Append?
}
