using Atlas.Core;
using Atlas.UI.Avalonia.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System.Reflection;

namespace Atlas.UI.Avalonia;

public class DataGridButtonColumn : DataGridBoundColumn
{
	public MethodInfo MethodInfo;
	public string ButtonText;
	public string? VisiblePropertyName;

	public DataGridButtonColumn(MethodInfo methodInfo, string buttonText)
	{
		MethodInfo = methodInfo;
		VisiblePropertyName = methodInfo.GetCustomAttribute<ButtonColumnAttribute>()?.VisiblePropertyName;
		ButtonText = buttonText;
		CanUserSort = false;
		Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);
	}

	// This doesn't get called when reusing cells
	protected override IControl GenerateElement(DataGridCell cell, object dataItem)
	{
		return GenerateEditingElementDirect(cell, dataItem);
	}

	protected override IControl GenerateEditingElementDirect(DataGridCell cell, object dataItem)
	{
		//cell.Background = GetCellBrush(cell, dataItem);
		//cell.MaxHeight = 100; // don't let them have more than a few lines each

		var button = new TabControlButton(ButtonText)
		{
			Padding = new Thickness(0),
			Margin = new Thickness(0),
			MinWidth = 10,
			BorderThickness = new Thickness(1),
			BorderBrush = Brushes.Black,
		};
		button.Resources.Add("ButtonPadding", new Thickness(2, 5));
		if (VisiblePropertyName != null)
			button.BindVisible(VisiblePropertyName);
		button.Click += Button_Click;
		return button;
	}

	private void Button_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
	{
		Button button = (Button)sender!;
		MethodInfo.Invoke(button.DataContext, new object[] { });
	}

	protected override object PrepareCellForEdit(IControl editingElement, RoutedEventArgs editingEventArgs)
	{
		return string.Empty;
	}
}
