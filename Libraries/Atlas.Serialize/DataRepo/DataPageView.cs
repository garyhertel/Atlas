using Atlas.Core;

namespace Atlas.Serialize;

public class DataPageView<T>
{
	public DataRepoInstance<T> DataRepoInstance;
	public bool Ascending;
	public int PageSize = 100;

	private IEnumerable<string>? _keyIterator;

	public List<string>? Keys => GetEnumerable()?.ToList();

	public DataPageView(DataRepoInstance<T> dataRepoInstance, bool ascending) : base()
	{
		DataRepoInstance = dataRepoInstance;
		Ascending = ascending;
	}

	public IEnumerable<string>? GetEnumerable()
	{
		string groupPath = DataRepoInstance.GroupPath;
		if (!Directory.Exists(groupPath)) return null;

		IEnumerable<string> enumerable;
		if (DataRepoInstance.Index != null)
		{
			var indices = DataRepoInstance.Index.Load(new());
			enumerable = indices.Items.Select(i => i.Key);
		}
		else
		{
			enumerable = Directory.EnumerateDirectories(groupPath);
		}

		if (Ascending)
		{
			return enumerable;
		}
		else
		{
			return enumerable.Reverse();
		}
	}

	public IEnumerable<DataItem<T>> Next()
	{
		List<DataItem<T>> items = new();

		_keyIterator ??= GetEnumerable();
		if (_keyIterator == null) return items;

		Call call = new();
		foreach (string key in _keyIterator.Take(PageSize))
		{
			T? value = DataRepoInstance.Load(call, key);
			if (value != null)
			{
				items.Add(new DataItem<T>(key, value));
			}
		}
		return items;
	}
}
