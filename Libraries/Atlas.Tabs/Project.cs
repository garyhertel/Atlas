using Atlas.Core;
using Atlas.Extensions;
using Atlas.Network;
using Atlas.Serialize;
using System;
using System.Collections.Generic;

namespace Atlas.Tabs
{
	public class Project
	{
		public string Name => ProjectSettings.Name; // for viewing purposes
		public string LinkType => ProjectSettings.LinkType; // for bookmarking
		public Version Version => ProjectSettings.Version;
		public virtual ProjectSettings ProjectSettings { get; set; } = new();
		public virtual UserSettings UserSettings { get; set; } = new();

		public Linker Linker { get; set; } = new Linker();

		public DataRepo DataShared => new(DataSharedPath, DataRepoName);
		public DataRepo DataApp => new(DataAppPath, DataRepoName);

		public HttpCacheManager Http = new();

		public TypeObjectStore TypeObjectStore { get; set; } = new();
		public BookmarkNavigator Navigator { get; set; } = new();
		public TaskInstanceCollection Tasks { get; set; } = new();

		private string DataSharedPath => Paths.Combine(UserSettings.ProjectPath, "Shared");
		private string DataAppPath => Paths.Combine(UserSettings.ProjectPath, "Versions", ProjectSettings.DataVersion.ToString());

		private string DataRepoName
		{
			get
			{
				if (UserSettings.BookmarkPath != null)
					return Paths.Combine("Bookmarks", UserSettings.BookmarkPath.HashSha256());
				else
					return Paths.Combine("Current");
			}
		}

		public override string ToString() => Name;

		public Project()
		{
		}

		public Project(ProjectSettings projectSettings)
		{
			ProjectSettings = projectSettings;
			UserSettings = new UserSettings()
			{
				ProjectPath = projectSettings.DefaultProjectPath,
			};
		}

		public Project(ProjectSettings projectSettings, UserSettings userSettings)
		{
			ProjectSettings = projectSettings;
			UserSettings = userSettings;
		}

		public void SaveSettings()
		{
			//DataApp.Save(projectSettings, new Call());

			var serializer = SerializerFile.Create(UserSettings.SettingsPath, "");
			serializer.Save(new Call(), ProjectSettings);
		}

		public Project Open(Bookmark bookmark)
		{
			UserSettings userSettings = UserSettings.DeepClone();
			userSettings.BookmarkPath = bookmark.Path;
			var project = new Project(ProjectSettings, userSettings)
			{
				Linker = Linker,
			};
			//project.Import(bookmark);
			bookmark.TabBookmark.Import(project);
			return project;
		}
	}

	public class TypeObjectStore
	{
		public Dictionary<Type, object> Items { get; set; } = new();

		public void Add(object obj)
		{
			Items.Add(obj.GetType(), obj);
		}

		public T Get<T>()
		{
			if (Items.TryGetValue(typeof(T), out object obj))
				return (T)obj;

			return default;
		}

		public object Get(Type type)
		{
			if (Items.TryGetValue(type, out object obj))
				return obj;
			return null;
		}
	}

	public interface IProject
	{
		void Restart();
	}
}
