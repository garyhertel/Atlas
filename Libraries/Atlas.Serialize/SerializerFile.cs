using Atlas.Core;

namespace Atlas.Serialize;

public abstract class SerializerFile
{
	private const string DefaultName = "<Default>";

	public string? HeaderPath { get; set; }
	public string? DataPath { get; set; }
	public string BasePath { get; set; }
	public string Name { get; set; }

	public bool Exists => File.Exists(DataPath) && new FileInfo(DataPath).Length > 0;

	public override string ToString() => BasePath;

	public SerializerFile(string basePath, string name = "")
	{
		BasePath = basePath;
		Name = name;
	}

	// check for writeability and no open locks
	public void TestWrite()
	{
		File.WriteAllText(DataPath!, "");
	}

	public void Save(Call call, object obj, string? name = null)
	{
		name ??= DefaultName;

		using CallTimer callTimer = call.Timer(LogLevel.Debug, "Saving object: " + name, new Tag("Path", BasePath));

		if (!Directory.Exists(BasePath))
			Directory.CreateDirectory(BasePath);

		SaveInternal(callTimer, obj, name);
	}

	public abstract void SaveInternal(Call call, object obj, string? name = null);

	public T? Load<T>(Call? call = null, bool lazy = false, TaskInstance? taskInstance = null)
	{
		call ??= new Call();
		object? obj = Load(call, lazy, taskInstance);
		if (obj is T loaded) return loaded;

		if (obj != null)
		{
			call.Log.AddError("Loaded type doesn't match type specified",
				new Tag("Loaded Type", obj.GetType()),
				new Tag("Expected Type", typeof(T)));
		}

		return default;

		/*Type type = typeof(T);
		//if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
		{
			type = Nullable.GetUnderlyingType(type);
		}

		return (T)Convert.ChangeType(obj, type);*/
	}

	public object? Load(Call call, bool lazy = false, TaskInstance? taskInstance = null)
	{
		using CallTimer callTimer = call.Timer(LogLevel.Debug, "Loading object: " + Name);

		try
		{
			return LoadInternal(callTimer, lazy, taskInstance);
		}
		catch (Exception e)
		{
			callTimer.Log.AddError("Exception loading file", new Tag("Exception", e.ToString()));
			return null; // returns null if reference type, otherwise default value (i.e. 0)
		}
	}

	public Header LoadHeader(Call call)
	{
		call ??= new Call();

		using CallTimer callReadAllBytes = call.Timer(LogLevel.Debug, "Loading header: " + Name);

		var memoryStream = new MemoryStream(File.ReadAllBytes(HeaderPath!));

		var reader = new BinaryReader(memoryStream);
		var header = new Header();
		header.Load(reader);
		return header;
	}

	protected abstract object? LoadInternal(Call call, bool lazy, TaskInstance? taskInstance);

	public static SerializerFile Create(string dataPath, string name = "")
	{
		return new SerializerFileAtlas(dataPath, name);
		// todo: Add SerializerFileJson
	}
}
