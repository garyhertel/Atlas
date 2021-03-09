﻿using Atlas.Core;
using Atlas.Serialize;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Atlas.Tabs
{
	public interface IDataTab
	{
		void Load(Call call, object obj, params object[] tabParams);
		event EventHandler<EventArgs> OnDelete;
	}

	// An Item collection that shows a Tab interface for every item
	public class DataCollection<TDataType, TTabType> where TTabType : IDataTab, new()
	{
		//public const string DataKey = "Saved";
		//public event EventHandler<EventArgs> OnDelete;

		public string Path;
		public ItemCollectionUI<TTabType> Items { get; set; } = new ItemCollectionUI<TTabType>();
		public TTabType NewTabItem { get; set; }

		public DataRepoView<TDataType> DataRepoView;
		public DataRepoView<TDataType> DataRepoSecondary; // saves and deletes goto a 2nd copy
		public object[] TabParams;
		private Dictionary<TTabType, IDataItem> _dataItemLookup;

		public DataCollection(DataRepoView<TDataType> dataRepoView, params object[] tabParams)
		{
			DataRepoView = dataRepoView;
			TabParams = tabParams;
			DataRepoView.Items.CollectionChanged += Items_CollectionChanged;
			Reload();
			//Items.CollectionChanged += Items_CollectionChanged;
		}

		private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Add)
			{
				foreach (IDataItem item in e.NewItems)
				{
					Add(item);
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove)
			{
				foreach (IDataItem item in e.OldItems)
				{
					Remove(item.Key);
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				Items.Clear();
			}
		}

		public void Reload()
		{
			Items.Clear();
			_dataItemLookup = new Dictionary<TTabType, IDataItem>();

			//dataRepoBookmarks = project.DataApp.Open<TDataType>(null, DataKey);
			foreach (DataItem<TDataType> dataItem in DataRepoView.Items)
			{
				// for autoselecting?
				//if (bookmark.Name == TabInstance.CurrentBookmarkName)
				//	continue;
				Add(dataItem);
			}
		}

		public TTabType Add(IDataItem dataItem)
		{
			var tabItem = new TTabType();
			tabItem.Load(new Call(), dataItem.Object, TabParams);
			tabItem.OnDelete += Item_OnDelete;
			Items.Add(tabItem);
			_dataItemLookup.Add(tabItem, dataItem);
			return tabItem;
		}

		/*public TTabType Add(TDataType dataObject)
		{
			var tabItem = new TTabType();
			tabItem.Load(new Call(), dataObject);
			tabItem.OnDelete += Item_OnDelete;
			Items.Add(tabItem);
			return tabItem;
		}

		public void AddNew(Call call, TDataType dataObject)
		{
			Remove(dataObject.ToString()); // Remove previous version
			dataRepoInstance.Save(call, dataObject.ToString(), dataObject);
			NewTabItem = Add(dataObject);
		}*/

		private void Item_OnDelete(object sender, EventArgs e)
		{
			TTabType tab = (TTabType)sender;
			if (!_dataItemLookup.TryGetValue(tab, out IDataItem dataItem))
				return;

			DataRepoView.Delete(dataItem.Key);
			DataRepoSecondary?.Delete(dataItem.Key);
			Items.Remove(tab);
			//Reload();
		}

		public void Remove(string key)
		{
			DataRepoView.Delete(key);
			TTabType existing = Items.FirstOrDefault(i => DataUtils.GetItemId(i) == key);
			if (existing != null)
				Items.Remove(existing);
		}

		public void AddDataRepo(DataRepoView<TDataType> dataRepoView)
		{
			DataRepoSecondary = dataRepoView;
		}
	}
}
