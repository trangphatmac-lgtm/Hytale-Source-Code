#define DEBUG
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Utils;

namespace HytaleClient.AssetEditor.Data;

public class AssetTypeRegistry
{
	public IReadOnlyDictionary<string, AssetTypeConfig> AssetTypes { get; private set; } = new Dictionary<string, AssetTypeConfig>();


	public void SetupAssetTypes(IReadOnlyDictionary<string, AssetTypeConfig> assetTypes)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		AssetTypes = assetTypes;
	}

	public bool TryGetAssetTypeFromPath(string filePath, out string assetType)
	{
		filePath = filePath.ToLowerInvariant();
		if (filePath.StartsWith(AssetPathUtils.PathCosmeticsLowercase + "/"))
		{
			string pathWithoutAssetId = AssetPathUtils.GetPathWithoutAssetId(filePath);
			foreach (KeyValuePair<string, AssetTypeConfig> assetType2 in AssetTypes)
			{
				if (assetType2.Value.AssetTree != AssetTreeFolder.Cosmetics || assetType2.Value.PathLowercase != pathWithoutAssetId)
				{
					continue;
				}
				assetType = assetType2.Key;
				return true;
			}
			assetType = null;
			return false;
		}
		string text = Path.GetExtension(filePath).ToLowerInvariant();
		if (text == "")
		{
			assetType = null;
			return false;
		}
		foreach (AssetTypeConfig value in AssetTypes.Values)
		{
			if (!filePath.StartsWith(value.PathLowercase + "/") || value.FileExtensionLowercase != text)
			{
				continue;
			}
			assetType = value.Id;
			return true;
		}
		assetType = null;
		return false;
	}

	public bool TryGetAssetTypesFromDirectoryPath(string filePath, out List<string> assetTypes)
	{
		if (filePath.StartsWith("Common/"))
		{
			assetTypes = null;
			return false;
		}
		if (filePath.StartsWith("Cosmetics/CharacterCreator/"))
		{
			string pathWithoutAssetId = AssetPathUtils.GetPathWithoutAssetId(filePath);
			foreach (KeyValuePair<string, AssetTypeConfig> assetType in AssetTypes)
			{
				if (assetType.Value.AssetTree == AssetTreeFolder.Cosmetics && assetType.Value.Path == pathWithoutAssetId)
				{
					assetTypes = new List<string> { assetType.Key };
					return true;
				}
			}
			assetTypes = null;
			return false;
		}
		assetTypes = new List<string>();
		foreach (KeyValuePair<string, AssetTypeConfig> assetType2 in AssetTypes)
		{
			if (assetType2.Value.AssetTree == AssetTreeFolder.Server && filePath.StartsWith(assetType2.Value.Path + "/"))
			{
				assetTypes.Add(assetType2.Key);
			}
		}
		return assetTypes.Count > 0;
	}

	public void Clear()
	{
		AssetTypes = new Dictionary<string, AssetTypeConfig>();
	}
}
