using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using HytaleClient.AssetEditor.Data;

namespace HytaleClient.AssetEditor.Utils;

public static class AssetPathUtils
{
	public const string PathCosmetics = "Cosmetics/CharacterCreator";

	public const string PathCommon = "Common";

	public const string PathServer = "Server";

	public const char PathSeparator = '/';

	public const string PathSeparatorString = "/";

	public const char SingleAssetFileIdSeparator = '#';

	public const string SingleAssetFileIdSeparatorString = "#";

	public static readonly string PathCosmeticsLowercase = "Cosmetics/CharacterCreator".ToLowerInvariant();

	public static readonly string PathCommonLowercase = "Common".ToLowerInvariant();

	public static readonly string PathServerLowercase = "Server".ToLowerInvariant();

	public static string[] GetAssetFilePathElements(string filePath, bool usesSharedAssetFile)
	{
		if (usesSharedAssetFile)
		{
			string[] array = filePath.Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			Array.Resize(ref array, array.Length + 1);
			string text = array[^2];
			int num = text.LastIndexOf('#');
			array[^2] = text.Substring(0, num);
			array[^1] = text.Substring(num + 1, text.Length - (num + 1));
			return array;
		}
		return filePath.Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
	}

	public static string CombinePaths(string path1, string path2)
	{
		if (path1 == "")
		{
			return path2;
		}
		if (path2 == "")
		{
			return path1;
		}
		if (path1[path1.Length - 1] == '/' || path2[0] == '/')
		{
			return path1 + path2;
		}
		return path1 + "/" + path2;
	}

	public static string GetAssetPathWithCommon(string relativeCommonPath)
	{
		return CombinePaths("Common", relativeCommonPath);
	}

	public static string GetPathWithoutAssetId(string filePath)
	{
		string[] array = filePath.Split(new char[1] { '#' }, StringSplitOptions.RemoveEmptyEntries);
		return string.Join("#", array, 0, array.Length - 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool HasAnyFileExtension(string filename, string[] allowedFileExtensions)
	{
		foreach (string text in allowedFileExtensions)
		{
			if (filename.EndsWith("." + text))
			{
				return true;
			}
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsAnyDirectory(string filename, string[] allowedDirectories)
	{
		foreach (string value in allowedDirectories)
		{
			if (filename.StartsWith(value))
			{
				return true;
			}
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsAnyDirectory(string filename, List<string> allowedDirectories)
	{
		foreach (string allowedDirectory in allowedDirectories)
		{
			if (filename.StartsWith(allowedDirectory))
			{
				return true;
			}
		}
		return false;
	}

	public static string GetAssetIdFromReference(string path, bool isSingleAssetFile)
	{
		return isSingleAssetFile ? path.Split(new char[1] { '#' }).Last() : Path.GetFileNameWithoutExtension(path);
	}

	public static bool IsAssetTreeRootDirectory(string path)
	{
		return path == "Common" || path == "Server" || path == "Cosmetics/CharacterCreator";
	}

	public static bool TryGetAssetTreeFolder(string path, out AssetTreeFolder assetTree, bool ignoreCase = false)
	{
		if (ignoreCase)
		{
			return TryGetAssetTreeFolderIgnoreCase(path, out assetTree);
		}
		if (path.StartsWith("Common/"))
		{
			assetTree = AssetTreeFolder.Common;
			return true;
		}
		if (path.StartsWith("Server/"))
		{
			assetTree = AssetTreeFolder.Server;
			return true;
		}
		if (path.StartsWith("Cosmetics/CharacterCreator/"))
		{
			assetTree = AssetTreeFolder.Cosmetics;
			return true;
		}
		assetTree = AssetTreeFolder.Common;
		return false;
	}

	private static bool TryGetAssetTreeFolderIgnoreCase(string path, out AssetTreeFolder assetTree)
	{
		path = path.ToLowerInvariant();
		if (path.StartsWith(PathCommonLowercase + "/"))
		{
			assetTree = AssetTreeFolder.Common;
			return true;
		}
		if (path.StartsWith(PathServerLowercase + "/"))
		{
			assetTree = AssetTreeFolder.Server;
			return true;
		}
		if (path.StartsWith(PathCosmeticsLowercase + "/"))
		{
			assetTree = AssetTreeFolder.Cosmetics;
			return true;
		}
		assetTree = AssetTreeFolder.Common;
		return false;
	}
}
