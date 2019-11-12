﻿using Atlas.Core;
using Atlas.Network;
using Atlas.Serialize;
using System;
using System.Collections.Generic;

namespace Atlas.Tabs
{
	public class Project
	{
		public string Name => projectSettings.Name;	// for viewing purposes
		public string LinkType => projectSettings.LinkType; // for bookmarking
		public string Version => projectSettings.Version;
		public virtual ProjectSettings projectSettings { get; set; }
		public virtual UserSettings userSettings { get; set; }

		public DataRepo DataShared => new DataRepo(DataRepoPath, "Shared");
		public DataRepo DataApp => new DataRepo(DataRepoPath, "Programs/" + Name + "/" + projectSettings.DataVersion);

		public HttpCacheManager httpCacheManager = new HttpCacheManager();

		public TypeObjectStore TypeObjectStore { get; set; } = new TypeObjectStore();
		public BookmarkNavigator Navigator { get; set; } = new BookmarkNavigator();
		public TaskInstanceCollection Tasks { get; set; } = new TaskInstanceCollection();

		private string DataRepoPath => Paths.Combine(userSettings.ProjectPath, "Data");


		public Project()
		{
		}

		public Project(ProjectSettings projectSettings, UserSettings userSettings)
		{
			this.projectSettings = projectSettings;
			this.userSettings = userSettings;
		}

		public void SaveSettings()
		{
			//tabInstance.project.DataApp.Save(projectSettings, new Call());

			var serializer = new SerializerFile(userSettings.SettingsPath, "");
			serializer.Save(new Call(), projectSettings);
		}

		public override string ToString()
		{
			return Name;
		}
	}

	public class TypeObjectStore
	{
		public Dictionary<Type, object> Items { get; set; } = new Dictionary<Type, object>();

		public void Add(object obj)
		{
			Items.Add(obj.GetType(), obj);
		}

		public T Get<T>()
		{
			object obj;
			if (Items.TryGetValue(typeof(T), out obj))
				return (T)obj;
			return default(T);
		}

		public object Get(Type type)
		{
			object obj;
			if (Items.TryGetValue(type, out obj))
				return obj;
			return null;
		}
	}

	public interface IProject
	{
		void Restart();
	}
}
