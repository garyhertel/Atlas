using Atlas.Core;
using Atlas.Resources;

namespace Atlas.Tabs.Tools;

public interface IFileTypeView
{
	string? Path { get; set; }
}

public class TabFile : ITab
{
	public static Dictionary<string, Type> ExtensionTypes = new();

	public delegate void SelectFile(Call call, string path);

	public SelectFile? SelectFileDelegate;

	public static void RegisterType<T>(params string[] extensions)
	{
		foreach (string extension in extensions)
		{
			ExtensionTypes[extension] = typeof(T);
		}
	}

	public string Path;

	public TabFile(string path, SelectFile? selectFileDelegate = null)
	{
		Path = path;
		SelectFileDelegate = selectFileDelegate;
	}

	public TabInstance Create() => new Instance(this);

	public class Toolbar : TabToolbar
	{
		public ToolButton? ButtonSelect { get; set; }

		[Separator]
		public ToolButton ButtonOpenFolder { get; set; } = new("Open Folder", Icons.Streams.OpenFolder);

		[Separator]
		public ToolButton ButtonDelete { get; set; } = new("Delete", Icons.Streams.Delete);
	}

	public class Instance : TabInstance
	{
		public TabFile Tab;

		public Instance(TabFile tab)
		{
			Tab = tab;
		}

		public override void Load(Call call, TabModel model)
		{
			string path = Tab.Path;
			if (!File.Exists(path))
			{
				model.AddObject("File doesn't exist");
				return;
			}

			Toolbar toolbar = new();
			if (Tab.SelectFileDelegate != null)
			{
				toolbar.ButtonSelect = new("Select", Icons.Streams.Enter);
				toolbar.ButtonSelect.Action = SelectClicked;
			}
			toolbar.ButtonOpenFolder.Action = OpenFolder;
			toolbar.ButtonDelete.Action = Delete;
			model.AddObject(toolbar);

			List<ListItem> items = new();

			string extension = System.IO.Path.GetExtension(path).ToLower();

			if (ExtensionTypes.TryGetValue(extension, out Type? type))
			{
				var tab = (IFileTypeView)Activator.CreateInstance(type)!;
				tab.Path = path;
				items.Add(new ListItem(extension, tab));
			}

			if (extension == ".json")
			{
				string text = File.ReadAllText(path);
				items.Add(new ListItem("Contents", text));
				items.Add(new ListItem("Json", LazyJsonNode.Parse(text)));
			}
			else
			{
				if (FileUtils.IsTextFile(path))
				{
					items.Add(new ListItem("Contents", new FilePath(path)));
				}
			}
			items.Add(new ListItem("File Info", new FileInfo(path)));

			model.Items = items;
		}

		private void SelectClicked(Call call)
		{
			Tab.SelectFileDelegate!(call, Tab.Path);
		}

		private void OpenFolder(Call call)
		{
			ProcessUtils.OpenFolder(Tab.Path);
		}

		private void Delete(Call call)
		{
			if (File.Exists(Tab.Path))
				File.Delete(Tab.Path);

			Refresh();
		}
	}
}
