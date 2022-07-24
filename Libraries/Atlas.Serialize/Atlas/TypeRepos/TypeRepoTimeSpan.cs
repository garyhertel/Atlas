using System;
using System.IO;

namespace Atlas.Serialize;

public class TypeRepoTimeSpan : TypeRepo
{
	public class Creator : IRepoCreator
	{
		public TypeRepo? TryCreateRepo(Serializer serializer, TypeSchema typeSchema)
		{
			if (CanAssign(typeSchema.Type!))
				return new TypeRepoTimeSpan(serializer, typeSchema);
			return null;
		}
	}

	public TypeRepoTimeSpan(Serializer serializer, TypeSchema typeSchema) :
		base(serializer, typeSchema)
	{
	}

	public static bool CanAssign(Type type)
	{
		return type == typeof(TimeSpan);
	}

	public override void SaveObject(BinaryWriter writer, object obj)
	{
		TimeSpan timeSpan = (TimeSpan)obj;
		writer.Write(timeSpan.Ticks);
	}

	protected override object? CreateObject(int objectIndex)
	{
		long position = Reader!.BaseStream.Position;
		Reader.BaseStream.Position = ObjectOffsets![objectIndex];

		object? obj = null;
		try
		{
			if (CanAssign(LoadableType!))
			{
				long ticks = Reader.ReadInt64();
				obj = new TimeSpan(ticks);
			}
			else
			{
				throw new Exception("Unhandled primitive type");
			}
		}
		catch (Exception)
		{
			//log.Add(e);
		}
		Reader.BaseStream.Position = position;

		ObjectsLoaded[objectIndex] = obj; // must assign before loading any more refs
		return obj;
	}

	public override object LoadObject()
	{
		object obj = Enum.ToObject(TypeSchema.Type!, Reader!.ReadInt32());
		return obj;
	}

	// not called, it's a struct and a value
	public override void Clone(object source, object dest)
	{
		//dest = new DateTime(((DateTime)source).Ticks, ((DateTime)source).Kind);
	}
}
