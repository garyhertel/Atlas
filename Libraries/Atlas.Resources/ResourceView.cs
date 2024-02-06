using System.Reflection;

namespace Atlas.Resources;

public interface IResourceView
{
	string ResourceType { get; }
	string Path { get; }

	Stream Stream { get; }
}

public record ResourceView(Assembly assembly, string BasePath, string GroupPath, string ResourceName, string ResourceType) : IResourceView
{
	public string Path => $"{BasePath}.{GroupPath}.{ResourceName}.{ResourceType}";

	public Stream Stream => assembly.GetManifestResourceStream(Path)!; 
}
