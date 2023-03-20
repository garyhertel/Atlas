using Atlas.Core;
using System;

namespace Atlas.Serialize;

public class DataRepoIndex
{
	public DataRepo DataRepo { get; set; }

	public record Item(long Index, string Key);

	public DataRepoIndex(DataRepo dataRepo)
	{
		DataRepo = dataRepo;
	}
}

public class DataRepoIndexInstance<T>
{
	public DataRepoInstance<T> DataRepoInstance { get; set; }

	public string GroupId => DataRepoInstance.GroupId;
	public string GroupPath => DataRepoInstance.GroupPath;

	public string DataPath => Paths.Combine(GroupPath, "Index.dat");

	public record Item(long Index, string Key);

	public class Indices
	{
		public List<Item> Items { get; set; }
		public long NextIndex { get; set; }
	}

	public DataRepoIndexInstance(DataRepoInstance<T> dataRepoInstance)
	{
		DataRepoInstance = dataRepoInstance;
	}

	// todo: Simplify
	public Item? Add(Call call, string key)
	{
		using var mutex = new Mutex(false, DataRepoInstance.GroupId);

		try
		{
			if (!mutex.WaitOne(TimeSpan.FromSeconds(5))) return null;
		}
		catch (AbandonedMutexException e)
		{
			// Mutex acquired
			call.Log.Add(e);
		}
		catch (Exception e)
		{
			call.Log.Add(e);
			return null;
		}

		try
		{
			// Do operation
			Indices indices = Load(call);
			long index = indices.NextIndex++;
			Item item = new(index, key);

			indices.Items.Add(item);
			Save(indices);
			return item;
		}
		catch (ApplicationException e)
		{
			call.Log.Add(e);
		}
		finally
		{
			mutex.ReleaseMutex();
		}
		return null;
	}

	// todo: Simplify
	public void Remove(Call call, string key)
	{
		using var mutex = new Mutex(false, DataRepoInstance.GroupId);

		try
		{
			if (!mutex.WaitOne(TimeSpan.FromSeconds(5))) return;
		}
		catch (AbandonedMutexException e)
		{
			// Mutex acquired
			call.Log.Add(e);
		}
		catch (Exception e)
		{
			call.Log.Add(e);
			return;
		}

		try
		{
			// Do operation
			Indices indices = Load(call);
			indices.Items.RemoveAll(item => item.Key == key);
			Save(indices);
		}
		catch (ApplicationException e)
		{
			call.Log.Add(e);
		}
		finally
		{
			mutex.ReleaseMutex();
		}
	}

	public void RemoveAll(Call call)
	{
		using var mutex = new Mutex(false, DataRepoInstance.GroupId);

		try
		{
			if (!mutex.WaitOne(TimeSpan.FromSeconds(5))) return;
		}
		catch (AbandonedMutexException e)
		{
			// Mutex acquired
			call.Log.Add(e);
		}
		catch (Exception e)
		{
			call.Log.Add(e);
			return;
		}

		try
		{
			// Do operation
			Indices indices = Load(call);
			indices.Items.Clear();
			Save(indices);
		}
		catch (ApplicationException e)
		{
			call.Log.Add(e);
		}
		finally
		{
			mutex.ReleaseMutex();
		}
	}

	private void Save(Indices indices)
	{
		using var stream = new FileStream(DataPath!, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
		using var writer = new BinaryWriter(stream);

		writer.Write(indices.Items.Count);
		writer.Write(indices.NextIndex);

		foreach (Item item in indices.Items)
		{
			writer.Write(item.Index);
			writer.Write(item.Key);
		}
	}

	public Indices Load(Call call)
	{
		if (!File.Exists(DataPath!)) return BuildIndices(call);

		using var stream = new FileStream(DataPath!, FileMode.Open, FileAccess.Read, FileShare.Read);
		using var reader = new BinaryReader(stream);

		List<Item> items = new();
		int count = reader.ReadInt32();
		long nextIndex = reader.ReadInt64();
		for (int i = 0; i < count; i++)
		{
			long index = reader.ReadInt64();
			string key = reader.ReadString();
			if (index > nextIndex)
			{
				call.Log.AddWarning("Index > NextIndex", 
					new Tag("Index", index), 
					new Tag("Key", key));
				nextIndex = index + 1;
			}
			items.Add(new Item(index, key));
		}
		return new Indices()
		{
			Items = items,
			NextIndex = nextIndex,
		};
	}

	private Indices BuildIndices(Call call)
	{
		ItemCollection<Header> headers = DataRepoInstance.LoadHeaders(call);

		int index = 0;
		var items = headers
			.Select(h => new Item(index++, h.Name))
			.ToList();

		return new Indices()
		{
			Items = items,
			NextIndex = index,
		};
	}
}
