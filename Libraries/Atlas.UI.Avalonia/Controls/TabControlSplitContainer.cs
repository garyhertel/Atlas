using Atlas.UI.Avalonia.Themes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System.Diagnostics;

namespace Atlas.UI.Avalonia.Controls;

public enum SeparatorType
{
	None,
	Splitter,
	Spacer,
}

// Grid wrapper that allows multiple children and optional splitters in between each
// Only updates controls that change
// Vertical only right now
public class TabControlSplitContainer : Grid
{
	public Dictionary<object, Control> GridControls = new();
	public List<GridSplitter> GridSplitters = new(); // reattach each time controls change

	public double MinDesiredWidth = 100;
	public double MaxDesiredWidth = double.MaxValue;

	public new bool IsArrangeValid;

	private List<Item> _gridItems = new();

	public class Item
	{
		public object? Object { get; set; }
		public Control? Control { get; set; }
		public bool Fill { get; set; }
	}

	public TabControlSplitContainer()
	{
		HorizontalAlignment = HorizontalAlignment.Stretch;
		VerticalAlignment = VerticalAlignment.Stretch; // not taking up maximum

		Focusable = true;

		GotFocus += TabView_GotFocus;
		LostFocus += TabView_LostFocus;
	}

	// Causes extra resizing and flashing
	// real DesiredSize doesn't work because of HorizontalAlign = Stretch?
	/*public new Size DesiredSize
	{
		get
		{
			Size desiredSize = new(MinDesiredWidth, 0);
			foreach (var control in Children)
			{
				Size childDesiredSize = control.DesiredSize;
				desiredSize = new Size(Math.Max(desiredSize.Width, childDesiredSize.Width), Math.Max(desiredSize.Height, childDesiredSize.Height));
			}
			double desiredWidth = Math.Min(MaxDesiredWidth, desiredSize.Width);
			desiredSize = new Size(desiredWidth, desiredSize.Height);
			return desiredSize;
		}
	}*/

	protected override Size MeasureCore(Size availableSize)
	{
		if (MaxDesiredWidth != double.MaxValue)
			availableSize = availableSize.WithWidth(Math.Min(MaxDesiredWidth, availableSize.Width));
		Size measured = base.MeasureCore(availableSize);
		double desiredWidth = Math.Min(MaxDesiredWidth, measured.Width);
		desiredWidth = Math.Max(desiredWidth, MinDesiredWidth);
		Size maxSize = measured.WithWidth(desiredWidth);
		return maxSize;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		IsArrangeValid = !double.IsNaN(finalSize.Width);
		return base.ArrangeOverride(finalSize);
	}

	private void TabView_GotFocus(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
	{
		Background = Theme.BackgroundFocused;
	}

	private void TabView_LostFocus(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
	{
		Background = Theme.TabBackground;
	}

	protected override void OnPointerEnter(PointerEventArgs e)
	{
		base.OnPointerEnter(e);
		Background = Theme.BackgroundFocused;
	}

	protected override void OnPointerLeave(PointerEventArgs e)
	{
		base.OnPointerLeave(e);
		Background = Theme.TabBackground;
	}

	public void AddControl(Control control, bool fill, SeparatorType separatorType)
	{
		var item = new Item()
		{
			Control = control,
			Fill = fill,
		};
		_gridItems.Add(item);

		int splitterIndex = RowDefinitions.Count;
		AddRowDefinition(false, splitterIndex);
		bool addSplitter = false;

		/*if (separatorType == SeparatorType.Splitter && fill)
		{
			// Add a Grid Splitter if there's been a single star row definition before this one
			for (int prevRowIndex = splitterIndex - 1; prevRowIndex >= 0; prevRowIndex--)
			{
				// Grid Splitter doesn't work due to the Tasks being a fixed sized between the 2 stars (bug?)
				//addSplitter |= (RowDefinitions[prevRowIndex].Height.IsStar);
			}
		}*/

		int controlIndex = splitterIndex + 1;
		RowDefinition rowDefinition = AddRowDefinition(fill, controlIndex);
		rowDefinition.MaxHeight = control.MaxHeight;

		SetRow(control, controlIndex);
		Children.Add(control);

		if (addSplitter)
			AddHorizontalGridSplitter(splitterIndex);

		//gridControls[oldChild.Key] = control;
		InvalidateMeasure();
	}

	private RowDefinition AddRowDefinition(bool fill, int? index = null)
	{
		var rowDefinition = new RowDefinition();
		if (fill)
			rowDefinition.Height = new GridLength(1, GridUnitType.Star);
		else
			rowDefinition.Height = GridLength.Auto;

		if (index is int i)
			RowDefinitions.Insert(i, rowDefinition);
		else
			RowDefinitions.Add(rowDefinition);

		return rowDefinition;
	}

	// always show splitters if their is a fill before or after?
	// Do we allow changing an auto to a fill?
	// always add a RowDefinition before and after
	public void InsertControl(Control control, bool fill, int rowIndex)
	{
		AddRowDefinition(false, rowIndex - 1);

		AddRowDefinition(fill, rowIndex);

		SetRow(control, rowIndex);
		//Children.Insert(index, control);
		Children.Add(control);
	}

	// Avalonia GridSplitter hardcodes neighbors in it's OnAttachedToVisualTree
	// Reattach them whenever we change neighbors
	private void ReattachSplitters()
	{
		foreach (var gridSplitter in GridSplitters)
		{
			Children.Remove(gridSplitter);
		}
		GridSplitters.Clear();

		Debug.Assert(_gridItems.Count * 2 == RowDefinitions.Count);
		int index = 0;
		//int prevStretch = -1; // todo: we can figure out splitters vs spacers automatically via fill property
		foreach (var gridItem in _gridItems)
		{
			if (index > 0)
				AddHorizontalGridSplitter(index);

			// separator
			index++;

			RowDefinition rowDefinition = RowDefinitions[index];
			if (gridItem.Fill)
				rowDefinition.Height = new GridLength(1, GridUnitType.Star);
			else
				rowDefinition.Height = GridLength.Auto;

			SetRow(gridItem.Control, index);
			index++;
		}
	}

	private void AddHorizontalGridSplitter(int rowIndex)
	{
		//AddRowDefinition(false, rowIndex);

		var gridSplitter = new GridSplitter()
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Background = Brushes.Black,

			//ShowsPreview = true,
			Height = Theme.SplitterSize,
		};
		GridSplitters.Add(gridSplitter);
		SetRow(gridSplitter, rowIndex);
		//Children.Insert(index, gridSplitter);
		Children.Add(gridSplitter);
	}

	private void AddVerticalGridSplitter(int columnIndex)
	{
		var gridSplitter = new GridSplitter()
		{
			VerticalAlignment = VerticalAlignment.Stretch,
			Background = Brushes.Black,
			Width = Theme.SplitterSize,
		};
		//GridSplitters.Add(gridSplitter);
		SetColumn(gridSplitter, columnIndex);
		//Children.Insert(index, gridSplitter);
		Children.Add(gridSplitter);
	}

	private void AddRowSpacer(int rowIndex)
	{
		//if (Children.Count <= 1)
		//	return;

		var border = new Border
		{
			//Width = 100,
			Height = 6,
			[Grid.RowProperty] = rowIndex
		};
		// Add a dummy panel so the children count equals the rowdefinition count, otherwise we need to track which rowdefinitions belong to which control
		//Bounds border = new Bounds();
		//SetRow(border, index);
		Children.Add(border);
	}

	private void RemoveControls(Dictionary<object, Control> controls)
	{
		var hashedControls = new HashSet<Control>(); // one line linq?
		foreach (var pair in GridControls)
		{
			hashedControls.Add(pair.Value);
		}

		// Remove any children not in use anymore
		foreach (var oldChild in controls)
		{
			if (hashedControls.Contains(oldChild.Value))
				continue;

			RowDefinitions.RemoveAt(RowDefinitions.Count - 1);
			RowDefinitions.RemoveAt(RowDefinitions.Count - 1);

			Children.Remove(oldChild.Value);

			if (oldChild.Value is IDisposable disposable)
				disposable.Dispose();
		}
	}

	private void AddControls(List<Control> orderedControls)
	{
		//RowDefinitions.Clear();
		_gridItems.Clear();
		int newIndex = 1;
		foreach (Control control in orderedControls)
		{
			var item = new Item()
			{
				Control = control,
				Fill = true,
			};
			_gridItems.Add(item);

			bool fill = true;// !(control is TabNotes); // don't show for notes, needs to be configurable
			if (!Children.Contains(control))
			{
				// Add a new control
				InsertControl(control, fill, newIndex);
			}
			newIndex += 2; // leave spot for splitters
		}
	}

	// Only used for child controls right now
	public void SetControls(Dictionary<object, Control> newControls, List<Control> orderedControls)
	{
		Dictionary<object, Control> oldControls = GridControls;

		// don't clear old controls so we invalidate container as little as possible when we resize the remaining

		GridControls = newControls;

		BeginInit();

		RemoveControls(oldControls);
		AddControls(orderedControls);

		ReattachSplitters();

		EndInit();

		// Add all child controls to the view
		InvalidateMeasure();
		InvalidateArrange();
		//UpdateSelectedTabInstances();
	}

	internal void Clear(bool dispose)
	{
		// objects might still be referenced and re-added again
		if (dispose)
		{
			foreach (IControl control in Children)
			{
				if (control is IDisposable disposable)
					disposable.Dispose(); // does Children.Clear() already handle this?
			}
		}
		RowDefinitions.Clear();
		Children.Clear();
		GridControls.Clear();
		_gridItems.Clear();
	}
}
