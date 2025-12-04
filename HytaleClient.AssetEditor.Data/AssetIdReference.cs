namespace HytaleClient.AssetEditor.Data;

public struct AssetIdReference
{
	public static readonly AssetIdReference None = default(AssetIdReference);

	public readonly string Type;

	public readonly string Id;

	public AssetIdReference(string type, string id)
	{
		Type = type;
		Id = id;
	}
}
