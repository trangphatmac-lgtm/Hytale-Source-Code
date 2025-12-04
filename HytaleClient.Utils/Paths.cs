using System;
using System.IO;
using System.Reflection;

namespace HytaleClient.Utils;

public static class Paths
{
	public static readonly string App = Path.GetDirectoryName(Uri.UnescapeDataString(new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath));

	public static string UserData { get; private set; }

	public static string CachedAssets { get; private set; }

	public static string Saves { get; private set; }

	public static string TempAssetDownload { get; private set; }

	public static string TempAssetEditorDownload { get; private set; }

	public static string SharedData { get; private set; }

	public static string GameData { get; private set; }

	public static string EditorData { get; private set; }

	public static string CoherentUI { get; private set; }

	public static string MonacoEditor { get; private set; }

	public static string Language { get; private set; }

	public static string Java { get; private set; }

	public static string Server { get; private set; }

	public static string BuiltInAssets { get; private set; }

	public static void Setup()
	{
		Java = OptionsHelper.JavaExecutable;
		Server = OptionsHelper.ServerJar;
		BuiltInAssets = OptionsHelper.AssetsDirectory;
		UserData = OptionsHelper.UserDataDirectory;
		CachedAssets = Path.Combine(UserData, "CachedAssets");
		Saves = Path.Combine(UserData, "Saves");
		TempAssetDownload = Path.Combine(UserData, "AssetDownload.tmp");
		TempAssetEditorDownload = Path.Combine(UserData, "AssetEditorDownload.tmp");
		string dataDirectory = OptionsHelper.DataDirectory;
		SharedData = Path.Combine(dataDirectory, "Shared");
		GameData = Path.Combine(dataDirectory, "Game");
		EditorData = Path.Combine(dataDirectory, "Editor");
		CoherentUI = Path.Combine(GameData, "CoherentUI");
		MonacoEditor = Path.Combine(SharedData, "MonacoEditor");
		Language = Path.Combine(SharedData, "Language");
	}

	public static string TrimBackslash(string serverPath)
	{
		if (serverPath.EndsWith("\\"))
		{
			serverPath = serverPath.Substring(0, serverPath.Length - 1);
		}
		return serverPath;
	}

	public static string EnsureUniqueDirname(string dirname)
	{
		int num = 0;
		string text;
		while (true)
		{
			text = dirname;
			if (num > 0)
			{
				text += $".{num}";
			}
			if (!Directory.Exists(text))
			{
				break;
			}
			num++;
		}
		return text;
	}

	public static string EnsureUniqueFilename(string filename, string extension)
	{
		int num = 0;
		string text;
		while (true)
		{
			text = filename;
			if (num > 0)
			{
				text += $".{num}";
			}
			text += extension;
			if (!File.Exists(text))
			{
				break;
			}
			num++;
		}
		return text;
	}

	public static string StripBasePath(string path, string basePath)
	{
		return path.StartsWith(basePath) ? path.Substring(basePath.Length) : path;
	}

	public static bool IsSubPathOf(string path, string baseDirPath)
	{
		string text = UnixPathUtil.ConvertToUnixPath(Path.GetFullPath(path));
		if (!text.EndsWith("/"))
		{
			text += "/";
		}
		string text2 = UnixPathUtil.ConvertToUnixPath(Path.GetFullPath(baseDirPath));
		if (!text2.EndsWith("/"))
		{
			text2 += "/";
		}
		return text.StartsWith(text2, StringComparison.OrdinalIgnoreCase);
	}
}
