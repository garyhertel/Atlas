using Atlas.Core;

namespace Atlas.UI.Avalonia.Examples;

[Params]
public class MyParams //: INotifyPropertyChanged
{
	public string Name { get; set; } = "Sprite";
	public int Amount { get; set; } = 3;
	public DateTime? Start { get; set; }
	public DateTime? End { get; set; }

	public override string ToString() => Name;
}
