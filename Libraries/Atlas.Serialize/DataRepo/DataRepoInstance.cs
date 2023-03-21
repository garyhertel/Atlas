using Atlas.Core;

namespace Atlas.Serialize;

public interface IDataRepoInstance
{
	string GroupId { get; }

	//object GetObject(string key);
}

[Unserialized]
public class DataRepoInstance<T> : IDataRepoInstance
{
	protected const string DefaultKey = ".Default"; // todo: support multiple directory levels?

	public readonly DataRepo DataRepo;
	public string GroupId { get; set; }
	public string GroupPath => DataRepo.GetGroupPath(typeof(T), GroupId);

	//public bool Indexed { get; set; }
	public DataRepoIndexInstance<T>? Index;

	public override string ToString() => GroupId;

	public DataRepoInstance(DataRepo dataRepo, string groupId)
	{
		DataRepo = dataRepo;
		GroupId = groupId;
	}

	public void AddIndex()
	{
		Index = new(this);
	}

	public virtual void Save(Call? call, T item)
	{
		Save(call, DefaultKey, item);
	}

	public virtual void Save(Call? call, string key, T item)
	{
		call ??= new();
		Index?.Add(call, key);
		DataRepo.Save<T>(GroupId, key, item, call);
	}

	public virtual T? Load(Call? call, string? key = null, bool createIfNeeded = false, bool lazy = false)
	{
		return DataRepo.Load<T>(GroupId, key ?? DefaultKey, call, createIfNeeded, lazy);
	}

	public virtual DataPageView<T>? LoadPageView(Call? call, bool ascending = true)
	{
		return new DataPageView<T>(this, ascending);
	}

	public DataItemCollection<T> LoadAll(Call? call = null, bool lazy = false)
	{
		return DataRepo.LoadAll<T>(call, GroupId, lazy);
	}

	public ItemCollection<Header> LoadHeaders(Call? call = null)
	{
		return DataRepo.LoadHeaders(typeof(T), GroupId, call);
	}

	public virtual void Delete(Call? call, T item)
	{
		Delete(call, item!.ToString());
	}

	public virtual void Delete(Call? call = null, string? key = null)
	{
		call ??= new();
		key ??= DefaultKey;
		Index?.Remove(call, key);
		DataRepo.Delete<T>(call, GroupId, key);
	}

	public virtual void DeleteAll(Call? call)
	{
		call ??= new();
		Index?.RemoveAll(call);
		DataRepo.DeleteAll<T>(call, GroupId);
	}
}
