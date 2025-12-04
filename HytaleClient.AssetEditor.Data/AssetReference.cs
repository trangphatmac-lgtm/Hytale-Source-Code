using System;

namespace HytaleClient.AssetEditor.Data;

public struct AssetReference : IEquatable<AssetReference>
{
	public static readonly AssetReference None = new AssetReference(null, null);

	public readonly string Type;

	public readonly string FilePath;

	public AssetReference(string type, string filePath)
	{
		Type = type;
		FilePath = filePath;
	}

	public bool Equals(AssetReference other)
	{
		return Type == other.Type && FilePath == other.FilePath;
	}

	public override bool Equals(object obj)
	{
		return obj is AssetReference other && Equals(other);
	}

	public override int GetHashCode()
	{
		return (((Type != null) ? Type.GetHashCode() : 0) * 397) ^ ((FilePath != null) ? FilePath.GetHashCode() : 0);
	}

	public override string ToString()
	{
		return "Type: " + Type + ", FilePath: " + FilePath;
	}
}
