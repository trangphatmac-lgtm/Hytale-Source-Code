namespace HytaleClient.AssetEditor.Data;

public class AssetFile
{
	public readonly string DisplayName;

	public string Path;

	public readonly bool IsDirectory;

	public string[] PathElements;

	public string AssetType;

	private AssetFile(string displayName, string path, bool isDirectory, string assetType, string[] pathElements)
	{
		DisplayName = displayName;
		Path = path;
		IsDirectory = isDirectory;
		AssetType = assetType;
		PathElements = pathElements;
	}

	public static AssetFile CreateFile(string id, string path, string assetType, string[] pathElements)
	{
		return new AssetFile(id, path, isDirectory: false, assetType, pathElements);
	}

	public static AssetFile CreateDirectory(string id, string path, string[] pathElements)
	{
		return new AssetFile(id, path, isDirectory: true, null, pathElements);
	}

	public static AssetFile CreateAssetTypeDirectory(string id, string path, string assetType, string[] pathElements)
	{
		return new AssetFile(id, path, isDirectory: true, assetType, pathElements);
	}
}
