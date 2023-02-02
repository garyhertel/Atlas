using Atlas.Core;
using Atlas.Extensions;
using Atlas.Tabs;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;
using System.Collections;
using System.Reflection;
using Atlas.UI.Avalonia.Themes;

namespace Atlas.UI.Avalonia.Controls;

public class TabControlParams : Grid
{
	public const int ControlMaxWidth = 500;
	public object? Object;

	public TabControlParams(object? obj, bool autoGenerateRows = true, string columnDefinitions = "Auto,*")
	{
		Object = obj;

		InitializeControls(columnDefinitions);

		if (autoGenerateRows)
			LoadObject(obj);
	}

	private void InitializeControls(string columnDefinitions)
	{
		HorizontalAlignment = HorizontalAlignment.Stretch;
		ColumnDefinitions = new ColumnDefinitions(columnDefinitions);

		Margin = new Thickness(6);

		MinWidth = 100;
		MaxWidth = 2000;
	}

	private void ClearControls()
	{
		Children.Clear();
		RowDefinitions.Clear();
	}

	public void LoadObject(object? obj)
	{
		ClearControls();

		if (obj == null) return;

		AddSummary();

		ItemCollection<ListProperty> properties = ListProperty.Create(obj);

		foreach (ListProperty property in properties)
		{
			int columnIndex = property.GetCustomAttribute<ColumnAttribute>()?.Index ?? 0;
			AddColumnIndex(columnIndex + 1); // label + value controls
		}

		Control? lastControl = null;
		foreach (ListProperty property in properties)
		{
			var newControl = AddPropertyRow(property);
			if (newControl != null && lastControl != null && Grid.GetRow(lastControl) != Grid.GetRow(newControl))
			{
				int columnIndex = Grid.GetColumn(lastControl);
				int columnSpan = Grid.GetColumnSpan(lastControl);
				if (columnIndex + columnSpan < ColumnDefinitions.Count)
				{
					Grid.SetColumnSpan(lastControl, ColumnDefinitions.Count - columnIndex);
				}
			}
			lastControl = newControl;
		}
	}

	private void AddSummary()
	{
		var summaryAttribute = Object!.GetType().GetCustomAttribute<SummaryAttribute>();
		if (summaryAttribute == null)
			return;

		AddRowDefinition();

		TextBlock textBlock = new()
		{
			Text = summaryAttribute.Summary,
			FontSize = 14,
			Margin = new Thickness(0, 3, 10, 3),
			Foreground = Theme.BackgroundText,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			TextWrapping = TextWrapping.Wrap,
			MaxWidth = 500,
			[Grid.ColumnSpanProperty] = 2,
		};
		Children.Add(textBlock);
	}

	public List<Control> AddObjectRow(object obj, List<PropertyInfo>? properties = null)
	{
		properties ??= obj.GetType().GetVisibleProperties();

		int rowIndex = AddRowDefinition();
		int columnIndex = 0;

		List<Control> controls = new();
		foreach (PropertyInfo propertyInfo in properties)
		{
			var property = new ListProperty(obj, propertyInfo);
			Control? control = CreatePropertyControl(property);
			if (control == null)
				continue;

			AddControl(control, columnIndex, rowIndex);
			controls.Add(control);
			columnIndex++;
		}
		return controls;
	}

	private int AddRowDefinition()
	{
		int rowIndex = RowDefinitions.Count;
		RowDefinition rowDefinition = new()
		{
			Height = new GridLength(1, GridUnitType.Auto),
		};
		RowDefinitions.Add(rowDefinition);
		return rowIndex;
	}

	private void AddControl(Control control, int columnIndex, int rowIndex)
	{
		AddColumnIndex(columnIndex);

		SetColumn(control, columnIndex);
		SetRow(control, rowIndex);
		Children.Add(control);
	}

	private void AddColumnIndex(int columnIndex)
	{
		while (columnIndex >= ColumnDefinitions.Count)
		{
			GridUnitType type = (ColumnDefinitions.Count % 2 == 0) ? GridUnitType.Auto : GridUnitType.Star;
			var columnDefinition = new ColumnDefinition(1, type);
			ColumnDefinitions.Add(columnDefinition);
		}
	}

	public Control? AddPropertyRow(string propertyName)
	{
		PropertyInfo propertyInfo = Object!.GetType().GetProperty(propertyName)!;
		return AddPropertyRow(new ListProperty(Object, propertyInfo));
	}

	public Control? AddPropertyRow(PropertyInfo propertyInfo)
	{
		return AddPropertyRow(new ListProperty(Object!, propertyInfo));
	}

	public Control? AddPropertyRow(ListProperty property)
	{
		int columnIndex = property.GetCustomAttribute<ColumnAttribute>()?.Index ?? 0;

		Control? control = CreatePropertyControl(property);
		if (control == null)
			return null;

		int rowIndex = RowDefinitions.Count;

		if (rowIndex > 0 && columnIndex > 0)
		{
			rowIndex--; // Reuse previous row
		}
		else
		{
			if (columnIndex == 0)
			{
				RowDefinition spacerRow = new()
				{
					Height = new GridLength(5),
				};
				RowDefinitions.Add(spacerRow);
				rowIndex++;
			}

			RowDefinition rowDefinition = new()
			{
				Height = new GridLength(1, GridUnitType.Auto),
			};
			RowDefinitions.Add(rowDefinition);
		}

		TextBlock textLabel = new()
		{
			Text = property.Name,
			Margin = new Thickness(10, 3),
			Foreground = Theme.BackgroundText,
			VerticalAlignment = VerticalAlignment.Center,
			MaxWidth = 500,
			[Grid.RowProperty] = rowIndex,
			[Grid.ColumnProperty] = columnIndex++,
		};
		Children.Add(textLabel);

		AddControl(control, columnIndex, rowIndex);

		return control;
	}

	private Control? CreatePropertyControl(ListProperty property)
	{
		Type type = property.UnderlyingType;

		BindListAttribute? listAttribute = type.GetCustomAttribute<BindListAttribute>();
		listAttribute ??= property.GetCustomAttribute<BindListAttribute>();

		Control? control = null;
		if (type == typeof(bool))
		{
			control = new TabControlCheckBox(property);
		}
		else if (type.IsEnum || listAttribute != null)
		{
			control = new TabControlComboBox(property, listAttribute?.PropertyName);
		}
		else if (typeof(DateTime).IsAssignableFrom(type))
		{
			control = new TabDateTimePicker(property);
		}
		else if (!typeof(IList).IsAssignableFrom(type))
		{
			control = new TabControlTextBox(property);
		}

		return control;
	}

	// Focus first input control
	// Add [Focus] attribute if more control needed?
	public new void Focus()
	{
		foreach (IControl control in Children)
		{
			if (control is TextBox textBox)
			{
				textBox.Focus();
				return;
			}
		}
		base.Focus();
	}
}

