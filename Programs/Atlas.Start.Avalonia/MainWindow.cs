using Atlas.Core;
using Atlas.Resources;
using Atlas.Start.Avalonia.Tabs;
using Atlas.Tabs;
using Atlas.UI.Avalonia;
using Atlas.UI.Avalonia.Clipboard;
using Atlas.UI.Avalonia.Tabs;
using System;

namespace Atlas.Start.Avalonia
{
	public class MainWindow : BaseWindow
	{
		public MainWindow() : base(new Project(Settings))
		{
			AddTab(new TabAvalonia());
			AddSnapshotButton();
		}

		private void AddSnapshotButton()
		{
			TabViewer.Toolbar.AddSeparator();

			ToolbarButton snapshotButton = TabViewer.Toolbar.AddButton("Snapshot", Icons.Streams.Screenshot);
			snapshotButton.Add(Snapshot);
		}

		private void Snapshot(Call call)
		{
			var screenCapture = new ScreenCapture(TabViewer, TabViewer.ScrollViewer);
			TabViewer.SetContent(screenCapture);
		}

		public static ProjectSettings Settings => new ProjectSettings()
		{
			Name = "Atlas",
			LinkType = "atlas",
			Version = ProjectSettings.ProgramVersion(),
			DataVersion = new Version(1, 1),
		};
	}
}
