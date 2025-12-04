using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HytaleClient.AssetEditor.Data;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Data.UserSettings;

internal class AssetEditorSettings
{
	public enum Panes
	{
		AssetBrowser,
		ConfigEditorSidebar,
		ConfigEditorSidebarPreviewModel,
		ConfigEditorSidebarPreviewItem,
		Diagnostics,
		ConfigEditorPropertyNames
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public const int CurrentVersion = 1;

	public int FormatVersion;

	public bool Fullscreen;

	public bool UseBorderlessForFullscreen;

	public bool Maximized;

	public string Language;

	public bool DiagnosticMode = true;

	public List<string> UncollapsedDirectories = new List<string>();

	public AssetTreeFolder ActiveAssetTree = AssetTreeFolder.Server;

	public string AssetsPath;

	public bool DisplayDefaultAssetPathWarning;

	public string LastUsedVersion;

	public readonly Dictionary<Panes, int> PaneSizes = new Dictionary<Panes, int>
	{
		{
			Panes.AssetBrowser,
			300
		},
		{
			Panes.ConfigEditorSidebar,
			280
		},
		{
			Panes.ConfigEditorSidebarPreviewModel,
			380
		},
		{
			Panes.ConfigEditorSidebarPreviewItem,
			280
		},
		{
			Panes.Diagnostics,
			250
		},
		{
			Panes.ConfigEditorPropertyNames,
			250
		}
	};

	private readonly object _saveLock = new object();

	private int _lastSavedCounter;

	private int _saveCounter;

	public static void Migrate(JObject jObject, int version)
	{
	}

	public void Initialize()
	{
		if (AssetsPath == null)
		{
			InitializeAssetsPath();
		}
	}

	private void InitializeAssetsPath()
	{
		if (File.Exists(Path.GetFullPath(Path.Combine(Paths.BuiltInAssets, "CommonAssetsIndex.hashes"))))
		{
			DisplayDefaultAssetPathWarning = true;
			AssetsPath = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "HytaleAssets"));
			Logger.Info("Initializing asset editor path with default directory at " + AssetsPath);
		}
		else
		{
			DisplayDefaultAssetPathWarning = false;
			AssetsPath = Paths.BuiltInAssets;
			Logger.Info("Initializing asset editor path with current assets directory at " + AssetsPath);
		}
	}

	public void Load()
	{
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		string path = Path.Combine(Paths.UserData, "AssetEditorSettings.json");
		if (!File.Exists(path))
		{
			InitializeAssetsPath();
			Save();
			return;
		}
		JObject val;
		try
		{
			val = JObject.Parse(File.ReadAllText(path));
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to load asset editor settings json");
			InitializeAssetsPath();
			Save();
			return;
		}
		UncollapsedDirectories = val["UncollapsedDirectories"].ToObject<List<string>>();
		if (val.ContainsKey("ActiveAssetTree"))
		{
			ActiveAssetTree = (AssetTreeFolder)Enum.Parse(typeof(AssetTreeFolder), (string)val["ActiveAssetTree"]);
		}
		if (val.ContainsKey("AssetsPath"))
		{
			AssetsPath = (string)val["AssetsPath"];
			DisplayDefaultAssetPathWarning = false;
		}
		else
		{
			InitializeAssetsPath();
		}
		if (val.ContainsKey("LastUsedVersion"))
		{
			LastUsedVersion = (string)val["LastUsedVersion"];
		}
		if (!val.ContainsKey("PaneSizes"))
		{
			return;
		}
		foreach (KeyValuePair<string, JToken> item in (JObject)val["PaneSizes"])
		{
			if (Enum.TryParse<Panes>(item.Key, out var result))
			{
				PaneSizes[result] = (int)item.Value;
			}
		}
	}

	public void Save()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Expected O, but got Unknown
		_saveCounter++;
		int version = _saveCounter;
		JObject val = new JObject();
		foreach (KeyValuePair<Panes, int> paneSize in PaneSizes)
		{
			val[paneSize.Key.ToString()] = JToken.op_Implicit(paneSize.Value);
		}
		JObject val2 = new JObject();
		val2.Add("UncollapsedDirectories", (JToken)(object)JArray.FromObject((object)UncollapsedDirectories));
		val2.Add("ActiveAssetTree", JToken.op_Implicit(ActiveAssetTree.ToString()));
		val2.Add("LastUsedVersion", JToken.op_Implicit(LastUsedVersion));
		val2.Add("AssetsPath", JToken.op_Implicit(AssetsPath));
		val2.Add("PaneSizes", (JToken)(object)val);
		JObject data = val2;
		Task.Run(delegate
		{
			SaveToFile(data, version);
		}).ContinueWith(delegate(Task t)
		{
			if (t.IsFaulted)
			{
				Logger.Error((Exception)t.Exception, "Failed to save asset editor settings");
			}
		});
	}

	private void SaveToFile(JObject data, int count)
	{
		lock (_saveLock)
		{
			if (_lastSavedCounter <= count)
			{
				string text = Path.Combine(Paths.UserData, "AssetEditorSettings.json");
				File.WriteAllText(text + ".new", ((object)data).ToString());
				if (File.Exists(text))
				{
					File.Replace(text + ".new", text, text + ".bak");
				}
				else
				{
					File.Move(text + ".new", text);
				}
				_lastSavedCounter = count;
			}
		}
	}

	public AssetEditorSettings Clone()
	{
		return new AssetEditorSettings
		{
			FormatVersion = FormatVersion,
			UncollapsedDirectories = new List<string>(UncollapsedDirectories),
			ActiveAssetTree = ActiveAssetTree,
			AssetsPath = AssetsPath,
			DisplayDefaultAssetPathWarning = DisplayDefaultAssetPathWarning,
			LastUsedVersion = LastUsedVersion,
			DiagnosticMode = DiagnosticMode,
			Language = Language,
			Maximized = Maximized,
			UseBorderlessForFullscreen = UseBorderlessForFullscreen,
			Fullscreen = Fullscreen
		};
	}
}
