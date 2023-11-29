using Atlas.Tabs;
using Atlas.UI.Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
//using Atlas.UI.Avalonia.Charts.LiveCharts;
//using Atlas.UI.Avalonia.Charts.OxyPlots;
//using Atlas.UI.Avalonia.ScreenCapture;

namespace Atlas.UI.Avalonia.Examples;

public class MainView : BaseView
{
	public MainView() : base(new Project(Settings))
	{
		AddTab(new TabAvalonia());

		//LiveChartCreator.Register();
		//OxyPlotCreator.Register();
		//ScreenCapture.AddControlTo(TabViewer);
	}

	public static ProjectSettings Settings => new()
	{
		Name = "Atlas",
		LinkType = "atlas",
		Version = ProjectSettings.ProgramVersion(),
		DataVersion = new Version(1, 1),
	};
}
