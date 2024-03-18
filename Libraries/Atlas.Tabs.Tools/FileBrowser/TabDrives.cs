using Atlas.Core;

namespace Atlas.Tabs.Tools;

public class TabDrives : ITab
{
	public TabFile.SelectFile? SelectFileDelegate;

	public TabDrives(TabFile.SelectFile? selectFileDelegate = null)
	{
		SelectFileDelegate = selectFileDelegate;
	}

	public TabInstance Create() => new Instance(this);

	public class Instance : TabInstance
	{
		public TabDrives Tab;

		public Instance(TabDrives tab)
		{
			Tab = tab;
		}

		public override void Load(Call call, TabModel model)
		{
			DriveInfo[] drives = DriveInfo.GetDrives();

			model.Items = drives
				.Select(d => new TabDirectory(d.Name, Tab.SelectFileDelegate))
				.ToList();
		}
	}
}
