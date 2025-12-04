#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.AssetEditor.Data;

public class AssetList
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private List<AssetFile> _cosmeticAssets = new List<AssetFile>();

	private List<AssetFile> _serverAssets = new List<AssetFile>();

	private List<AssetFile> _commonAssets = new List<AssetFile>();

	private readonly AssetTypeRegistry _assetTypeRegistry;

	public AssetList(AssetTypeRegistry assetTypeRegistry)
	{
		_assetTypeRegistry = assetTypeRegistry;
	}

	public void SetupAssets(List<AssetFile> serverAssetFiles, List<AssetFile> commonAssetFiles, List<AssetFile> cosmeticAssetFiles)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_cosmeticAssets = cosmeticAssetFiles;
		_serverAssets = serverAssetFiles;
		_commonAssets = commonAssetFiles;
	}

	public bool TryGetDirectory(string path, out AssetFile assetFile, bool ignoreCase = false)
	{
		if (!AssetPathUtils.TryGetAssetTreeFolder(path, out var assetTree, ignoreCase))
		{
			Logger.Warn("TryGetDirectory: Invalid folder path " + path);
			assetFile = null;
			return false;
		}
		List<AssetFile> assets = GetAssets(assetTree);
		int directoryIndex = GetDirectoryIndex(assets, path, ignoreCase);
		if (directoryIndex > -1)
		{
			assetFile = assets[directoryIndex];
			return true;
		}
		assetFile = null;
		return false;
	}

	public bool TryGetDirectoryIndex(string path, out int index, bool ignoreCase = false)
	{
		if (!AssetPathUtils.TryGetAssetTreeFolder(path, out var assetTree, ignoreCase))
		{
			Logger.Warn("TryGetDirectoryIndex: Invalid folder path " + path);
			index = -1;
			return false;
		}
		List<AssetFile> assets = GetAssets(assetTree);
		index = GetDirectoryIndex(assets, path, ignoreCase);
		return index > -1;
	}

	private static int GetDirectoryIndex(List<AssetFile> files, string path, bool ignoreCase)
	{
		AssetFile item = AssetFile.CreateDirectory(null, path, AssetPathUtils.GetAssetFilePathElements(path, usesSharedAssetFile: false));
		return files.BinarySearch(item, ignoreCase ? AssetFileComparer.IgnoreCaseInstance : AssetFileComparer.Instance);
	}

	public bool TryGetFile(string path, out AssetFile assetFile, bool ignoreCase = false)
	{
		if (!AssetPathUtils.TryGetAssetTreeFolder(path, out var assetTree, ignoreCase))
		{
			Logger.Warn("TryGetFile: Invalid folder path " + path);
			assetFile = null;
			return false;
		}
		List<AssetFile> assets = GetAssets(assetTree);
		int assetFileIndex = GetAssetFileIndex(assets, path, ignoreCase, assetTree == AssetTreeFolder.Cosmetics);
		if (assetFileIndex > -1)
		{
			assetFile = assets[assetFileIndex];
			return true;
		}
		assetFile = null;
		return false;
	}

	public bool TryGetFileIndex(string path, out int index, bool ignoreCase = false)
	{
		if (!AssetPathUtils.TryGetAssetTreeFolder(path, out var assetTree, ignoreCase))
		{
			Logger.Warn("TryGetFile: Invalid folder path " + path);
			index = -1;
			return false;
		}
		List<AssetFile> assets = GetAssets(assetTree);
		index = GetAssetFileIndex(assets, path, ignoreCase, assetTree == AssetTreeFolder.Cosmetics);
		return index > -1;
	}

	private static int GetAssetFileIndex(List<AssetFile> files, string path, bool ignoreCase, bool usesSharedAssetFile)
	{
		AssetFile item = AssetFile.CreateFile(null, path, null, AssetPathUtils.GetAssetFilePathElements(path, usesSharedAssetFile));
		return files.BinarySearch(item, ignoreCase ? AssetFileComparer.IgnoreCaseInstance : AssetFileComparer.Instance);
	}

	public bool TryGetAsset(string path, out AssetFile assetFile, bool ignoreCase = false)
	{
		if (TryGetFile(path, out assetFile, ignoreCase))
		{
			return true;
		}
		if (TryGetDirectory(path, out assetFile, ignoreCase))
		{
			return true;
		}
		assetFile = null;
		return false;
	}

	public List<AssetFile> GetAssets(AssetTreeFolder assetTree)
	{
		return assetTree switch
		{
			AssetTreeFolder.Server => _serverAssets, 
			AssetTreeFolder.Common => _commonAssets, 
			AssetTreeFolder.Cosmetics => _cosmeticAssets, 
			_ => throw new Exception("Invalid Asset folder: " + assetTree), 
		};
	}

	public bool TryReplaceDirectoryContents(string path, List<AssetFile> newAssetFiles)
	{
		if (!AssetPathUtils.TryGetAssetTreeFolder(path, out var assetTree))
		{
			Logger.Warn("TryReplaceDirectoryContents: Invalid folder path " + path);
			return false;
		}
		if (!TryGetDirectoryContentsBounds(path, out var index, out var size))
		{
			return false;
		}
		List<AssetFile> assets = GetAssets(assetTree);
		if (size > 0)
		{
			assets.RemoveRange(index, size);
		}
		assets.InsertRange(index, newAssetFiles);
		return true;
	}

	public bool TryInsertDirectory(string path)
	{
		if (!AssetPathUtils.TryGetAssetTreeFolder(path, out var assetTree))
		{
			Logger.Warn("TryInsertDirectory: Invalid folder path {0}", path);
			return false;
		}
		string[] array = path.Split(new char[1] { '/' });
		string directoryPath = string.Join('/'.ToString(), array, 0, array.Length - 1);
		List<AssetFile> assets = GetAssets(assetTree);
		if (!TryGetDirectoryContentsBounds(directoryPath, out var index, out var size))
		{
			return false;
		}
		string id = array.Last();
		assets.Insert(index, AssetFile.CreateDirectory(id, path, path.Split(new char[1] { '/' })));
		assets.Sort(index, size + 1, AssetFileComparer.Instance);
		return true;
	}

	public bool TryMoveDirectory(string oldPath, string newPath, out Dictionary<string, AssetFile> renamedAssets)
	{
		renamedAssets = null;
		if (!AssetPathUtils.TryGetAssetTreeFolder(newPath, out var assetTree))
		{
			Logger.Warn("TryMoveDirectory: Invalid new folder path " + newPath);
			return false;
		}
		if (!AssetPathUtils.TryGetAssetTreeFolder(oldPath, out var assetTree2))
		{
			Logger.Warn("TryMoveDirectory: Invalid old folder path " + oldPath);
			return false;
		}
		if (assetTree != assetTree2)
		{
			Logger.Warn("TryMoveDirectory: Directory cannot be moved to another asset tree");
			return false;
		}
		List<AssetFile> assets = GetAssets(assetTree);
		int directoryIndex = GetDirectoryIndex(assets, oldPath, ignoreCase: false);
		if (directoryIndex < 0)
		{
			return false;
		}
		string[] assetFilePathElements = AssetPathUtils.GetAssetFilePathElements(newPath, usesSharedAssetFile: false);
		assets[directoryIndex] = AssetFile.CreateDirectory(Path.GetFileName(newPath), newPath, assetFilePathElements);
		renamedAssets = new Dictionary<string, AssetFile>();
		for (int i = directoryIndex + 1; i < assets.Count; i++)
		{
			AssetFile assetFile = assets[i];
			if (!assetFile.Path.StartsWith(oldPath + "/"))
			{
				break;
			}
			string path = assetFile.Path;
			assetFile.Path = newPath + assetFile.Path.Substring(oldPath.Length);
			assetFile.AssetType = (_assetTypeRegistry.TryGetAssetTypeFromPath(assetFile.Path, out var assetType) ? assetType : null);
			assetFile.PathElements = AssetPathUtils.GetAssetFilePathElements(assetFile.Path, !assetFile.IsDirectory && assetTree == AssetTreeFolder.Cosmetics);
			renamedAssets.Add(path, assetFile);
		}
		assets.Sort(AssetFileComparer.Instance);
		return true;
	}

	public bool TryRemoveDirectory(string path, out List<AssetFile> removedEntries)
	{
		removedEntries = null;
		if (!AssetPathUtils.TryGetAssetTreeFolder(path, out var assetTree))
		{
			Logger.Warn("TryRemoveDirectory: Invalid folder path " + path);
			return false;
		}
		List<AssetFile> assets = GetAssets(assetTree);
		int directoryIndex = GetDirectoryIndex(assets, path, ignoreCase: false);
		if (directoryIndex < 0)
		{
			return false;
		}
		int num = -1;
		removedEntries = new List<AssetFile>();
		for (int i = directoryIndex + 1; i < assets.Count; i++)
		{
			AssetFile assetFile = assets[i];
			if (!assetFile.Path.StartsWith(path + "/"))
			{
				break;
			}
			num = i;
			removedEntries.Add(assetFile);
		}
		if (num > -1)
		{
			assets.RemoveRange(directoryIndex, num - directoryIndex + 1);
		}
		else
		{
			assets.RemoveAt(directoryIndex);
		}
		return true;
	}

	public bool TryRemoveFile(string path)
	{
		if (!AssetPathUtils.TryGetAssetTreeFolder(path, out var assetTree))
		{
			Logger.Warn("TryRemoveFile: Invalid folder path " + path);
			return false;
		}
		List<AssetFile> assets = GetAssets(assetTree);
		int assetFileIndex = GetAssetFileIndex(assets, path, ignoreCase: false, assetTree == AssetTreeFolder.Cosmetics);
		if (assetFileIndex < 0)
		{
			return false;
		}
		assets.RemoveAt(assetFileIndex);
		return true;
	}

	public bool TryMoveFile(string oldPath, string newPath)
	{
		if (!AssetPathUtils.TryGetAssetTreeFolder(newPath, out var assetTree))
		{
			Logger.Warn("TryMoveFile: Invalid new folder path " + newPath);
			return false;
		}
		if (!AssetPathUtils.TryGetAssetTreeFolder(oldPath, out var assetTree2))
		{
			Logger.Warn("TryMoveFile: Invalid old folder path " + oldPath);
			return false;
		}
		if (assetTree != assetTree2)
		{
			return TryRemoveFile(oldPath) && TryInsertDirectory(newPath);
		}
		if (!_assetTypeRegistry.TryGetAssetTypeFromPath(newPath, out var assetType))
		{
			return false;
		}
		List<AssetFile> assets = GetAssets(assetTree);
		int assetFileIndex = GetAssetFileIndex(assets, oldPath, ignoreCase: false, assetTree2 == AssetTreeFolder.Cosmetics);
		if (assetFileIndex < 0)
		{
			return false;
		}
		string assetIdFromReference = AssetPathUtils.GetAssetIdFromReference(newPath, assetTree == AssetTreeFolder.Cosmetics);
		string[] assetFilePathElements = AssetPathUtils.GetAssetFilePathElements(newPath, assetTree == AssetTreeFolder.Cosmetics);
		assets[assetFileIndex] = AssetFile.CreateFile(assetIdFromReference, newPath, assetType, assetFilePathElements);
		assets.Sort(AssetFileComparer.Instance);
		return true;
	}

	public bool TryInsertFile(string path)
	{
		if (!_assetTypeRegistry.TryGetAssetTypeFromPath(path, out var assetType) || !_assetTypeRegistry.AssetTypes.TryGetValue(assetType, out var value))
		{
			Logger.Warn("TryInsertFile: No asset type found matching path " + path);
			return false;
		}
		if (!TryGetDirectoryContentsBounds(value.Path, out var index, out var size))
		{
			Logger.Warn("TryInsertFile: Asset type directory not found " + value.Path);
			return false;
		}
		AssetPathUtils.TryGetAssetTreeFolder(path, out var assetTree);
		List<AssetFile> assets = GetAssets(assetTree);
		string assetIdFromReference = AssetPathUtils.GetAssetIdFromReference(path, value.AssetTree == AssetTreeFolder.Cosmetics);
		string[] assetFilePathElements = AssetPathUtils.GetAssetFilePathElements(path, value.AssetTree == AssetTreeFolder.Cosmetics);
		assets.Insert(index, AssetFile.CreateFile(assetIdFromReference, path, assetType, assetFilePathElements));
		assets.Sort(index, size + 1, AssetFileComparer.Instance);
		return true;
	}

	public bool TryInsertAssets(string path, List<AssetFile> newAssets)
	{
		if (!AssetPathUtils.TryGetAssetTreeFolder(path, out var assetTree))
		{
			Logger.Warn("TryInsertAssets: Invalid new folder path " + path);
			return false;
		}
		if (!TryGetDirectoryContentsBounds(path, out var index, out var size))
		{
			Logger.Warn("TryInsertAssets: Folder not found " + path);
			return false;
		}
		List<AssetFile> assets = GetAssets(assetTree);
		assets.InsertRange(index, newAssets);
		assets.Sort(index, size + newAssets.Count, AssetFileComparer.Instance);
		return true;
	}

	private bool TryGetDirectoryContentsBounds(string directoryPath, out int index, out int size)
	{
		switch (directoryPath)
		{
		case "Common":
			index = 0;
			size = _commonAssets.Count;
			return true;
		case "Server":
			index = 0;
			size = _serverAssets.Count;
			return true;
		case "Cosmetics/CharacterCreator":
			index = 0;
			size = _cosmeticAssets.Count;
			return true;
		default:
		{
			index = -1;
			size = 0;
			if (!AssetPathUtils.TryGetAssetTreeFolder(directoryPath, out var assetTree))
			{
				return false;
			}
			List<AssetFile> assets = GetAssets(assetTree);
			index = assets.BinarySearch(AssetFile.CreateDirectory(null, directoryPath, AssetPathUtils.GetAssetFilePathElements(directoryPath, usesSharedAssetFile: false)), AssetFileComparer.Instance);
			if (index == -1)
			{
				return false;
			}
			int num = assets[index].PathElements.Length;
			index++;
			for (int i = index; i < assets.Count; i++)
			{
				AssetFile assetFile = assets[i];
				if (assetFile.PathElements.Length <= num)
				{
					size = i - index;
					return true;
				}
			}
			size = assets.Count - index;
			return true;
		}
		}
	}

	public bool TryGetPathForAssetId(string assetType, string assetId, out string filePath, bool ignoreCase = false)
	{
		filePath = null;
		if (!_assetTypeRegistry.AssetTypes.TryGetValue(assetType, out var value))
		{
			return false;
		}
		List<AssetFile> assets = GetAssets(value.AssetTree);
		int directoryIndex = GetDirectoryIndex(assets, value.Path, ignoreCase: false);
		if (directoryIndex < 0)
		{
			return false;
		}
		int num = assets[directoryIndex].PathElements.Length;
		for (int i = directoryIndex + 1; i < assets.Count; i++)
		{
			AssetFile assetFile = assets[i];
			if (assetFile.PathElements.Length <= num)
			{
				break;
			}
			if (!assetFile.IsDirectory && !(assetFile.AssetType != assetType) && assetFile.DisplayName.Equals(assetId, ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture))
			{
				filePath = assetFile.Path;
				return true;
			}
		}
		return false;
	}
}
