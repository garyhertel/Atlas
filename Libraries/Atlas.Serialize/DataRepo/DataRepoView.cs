﻿using Atlas.Core;
using System.Linq;

namespace Atlas.Serialize
{
	public class DataRepoView<T> : DataRepoInstance<T>
	{
		//public DataRepo<T> DataRepo;

		public DataItemCollection<T> Items { get; set; }

		public DataRepoView(DataRepo dataRepo, string groupId) : base(dataRepo, groupId)
		{
		}

		public DataRepoView(DataRepoInstance<T> dataRepo) : base(dataRepo.DataRepo, dataRepo.GroupId)
		{
		}

		public override void Save(Call call, string key, T item)
		{
			lock (DataRepo)
			{
				Delete(key);
				base.Save(call, key, item);
				Items.Add(key, item);
			}
		}

		public override void Delete(string key = null)
		{
			lock (DataRepo)
			{
				base.Delete(key);
				var item = Items.Where(d => d.Key == key).FirstOrDefault();
				if (item != null)
					Items.Remove(item);
			}
		}

		public override void DeleteAll()
		{
			lock (DataRepo)
			{
				base.DeleteAll();
				Items.Clear();
			}
		}

		public void LoadAll(Call call)
		{
			lock (DataRepo)
			{
				Items = base.LoadAll(call);
			}
		}

		public void LoadAllOrderBy(Call call, string orderByMemberName)
		{
			lock (DataRepo)
			{
				DataItemCollection<T> items = base.LoadAll(call);
				Items = new DataItemCollection<T>(items.OrderBy(orderByMemberName));
			}
		}

		public void SortBy(string memberName)
		{
			lock (DataRepo)
			{
				var ordered = Items.OrderBy(memberName);
				Items = new DataItemCollection<T>(ordered);
			}
		}

		public void SortByDescending(string memberName)
		{
			lock (DataRepo)
			{
				var ordered = Items.OrderByDescending(memberName);
				Items = new DataItemCollection<T>(ordered);
			}
		}
	}
}
