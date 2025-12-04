#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Config;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.AssetEditor.Interface.Elements;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Data.Audio;
using HytaleClient.Data.Characters;
using HytaleClient.Graphics;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Backends;

internal class LocalAssetEditorBackend : AssetEditorBackend
{
	private class AssetUndoRedoStacks
	{
		public readonly DropOutStack<ClientJsonUpdateCommand> UndoStack = new DropOutStack<ClientJsonUpdateCommand>(100);

		public readonly DropOutStack<ClientJsonUpdateCommand> RedoStack = new DropOutStack<ClientJsonUpdateCommand>(100);

		public string SaveFileHash;
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private const string ItemCategoryAssetTypeId = "ItemCategory";

	private const string ItemCategoriesDatasetId = "ItemCategories";

	private const string WwiseEventIdsDatasetId = "WwiseEventIds";

	private Dictionary<string, JObject> _unsavedJsonAssets = new Dictionary<string, JObject>();

	private Dictionary<string, string> _unsavedTextAssets = new Dictionary<string, string>();

	private Dictionary<string, Image> _unsavedImageAssets = new Dictionary<string, Image>();

	private Dictionary<string, string> _fileExtensionAssetTypeMapping = new Dictionary<string, string>();

	private Dictionary<string, List<string>> _dropdownDatasetEntriesCache = new Dictionary<string, List<string>>();

	private List<string> _loadingDropdownDatasets = new List<string>();

	private List<Texture> _iconTextures = new List<Texture>();

	private bool _isInitializingOrInitialized;

	private readonly CancellationTokenSource _backendLifetimeCancellationTokenSource = new CancellationTokenSource();

	private readonly CancellationToken _backendLifetimeCancellationToken;

	private IDictionary<string, string> _translationMessages = new Dictionary<string, string>();

	private string _assetsDirectoryPath;

	private List<FileSystemWatcher> _fileSystemWatchers = new List<FileSystemWatcher>();

	private readonly ConcurrentQueue<FileSystemEventArgs> _fileWatcherEventQueue = new ConcurrentQueue<FileSystemEventArgs>();

	private readonly CancellationTokenSource _threadCancellationTokenSource = new CancellationTokenSource();

	private CancellationToken _threadCancellationToken;

	private Thread _fileWatcherHandlerThread;

	private Dictionary<string, AssetUndoRedoStacks> _undoRedoStacks = new Dictionary<string, AssetUndoRedoStacks>();

	public override void OnValueChanged(PropertyPath path, JToken value)
	{
		string text = path.ToString();
		if (AssetEditorOverlay.AssetTypeRegistry.AssetTypes[AssetEditorOverlay.CurrentAsset.Type].AssetTree != AssetTreeFolder.Cosmetics || value == null)
		{
			return;
		}
		if (text == "Model")
		{
			if (AssetEditorOverlay.ConfigEditor.Value["Textures"] == null && AssetEditorOverlay.ConfigEditor.Value["GreyscaleTexture"] == null)
			{
				FindAndAddTextures((string)value, Path.GetFileNameWithoutExtension((string)value), PropertyPath.FromString(""));
			}
		}
		else if (text.StartsWith("Variants.") && AssetEditorOverlay.ConfigEditor.Value["Variants"] == null && path.Elements.Length == 3 && !(path.Elements[2] != "Model"))
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension((string)value);
			FindAndAddTextures((string)value, fileNameWithoutExtension, PropertyPath.FromString("Variants." + path.Elements[1]));
			string[] array = fileNameWithoutExtension.Split(new char[1] { '_' });
			if (array.Length != 0)
			{
				Array.Resize(ref array, array.Length - 1);
				FindAndAddTextures((string)value, string.Join("_", array), PropertyPath.FromString("Variants." + path.Elements[1]));
			}
		}
	}

	private void FindAndAddTextures(string modelFile, string baseFileName, PropertyPath basePropertyPath)
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Expected O, but got Unknown
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Expected O, but got Unknown
		string path = Path.GetDirectoryName(modelFile).Replace(Path.DirectorySeparatorChar, '/');
		string path2 = Path.GetDirectoryName(modelFile).Replace(Path.DirectorySeparatorChar, '/');
		string text = AssetPathUtils.CombinePaths(path, baseFileName + "_Textures");
		string text2 = baseFileName + "_Greyscale.png";
		List<FileSelector.File> commonFileSelectorFiles = AssetEditorOverlay.GetCommonFileSelectorFiles(path2, "", null, null, -1);
		List<FileSelector.File> commonFileSelectorFiles2 = AssetEditorOverlay.GetCommonFileSelectorFiles(text, "", null, null, -1);
		JObject val = new JObject();
		string text3 = null;
		foreach (FileSelector.File item in commonFileSelectorFiles)
		{
			if (item.IsDirectory || item.Name != text2)
			{
				continue;
			}
			text3 = AssetPathUtils.CombinePaths(path, item.Name);
			break;
		}
		foreach (FileSelector.File item2 in commonFileSelectorFiles2)
		{
			if (!item2.IsDirectory && item2.Name.EndsWith(".png"))
			{
				string? fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item2.Name);
				JObject val2 = new JObject();
				val2.Add("Texture", JToken.op_Implicit(AssetPathUtils.CombinePaths(text, item2.Name)));
				val[fileNameWithoutExtension] = (JToken)val2;
			}
		}
		ConfigEditor configEditor = AssetEditorOverlay.ConfigEditor;
		PropertyPath? firstCreatedProperty;
		if (text3 != null)
		{
			configEditor.SetProperty(configEditor.Value, basePropertyPath.GetChild("GreyscaleTexture"), JToken.op_Implicit(text3), out firstCreatedProperty, updateDisplayedValue: true);
		}
		if (((JContainer)val).Count > 0)
		{
			configEditor.SetProperty(configEditor.Value, basePropertyPath.GetChild("Textures"), (JToken)(object)val, out firstCreatedProperty, updateDisplayedValue: true);
		}
		if (configEditor.Value["Name"] == null)
		{
			configEditor.SetProperty(configEditor.Value, PropertyPath.FromString("Name"), JToken.op_Implicit(baseFileName), out firstCreatedProperty, updateDisplayedValue: true);
		}
		configEditor.Layout();
		ValidateJsonAsset(AssetEditorOverlay.CurrentAsset, configEditor.Value);
	}

	public LocalAssetEditorBackend(AssetEditorOverlay assetEditorOverlay, AssetTreeFolder[] supportedFolders)
		: base(assetEditorOverlay)
	{
		_backendLifetimeCancellationToken = _backendLifetimeCancellationTokenSource.Token;
		base.SupportedAssetTreeFolders = supportedFolders;
	}

	protected override void DoDispose()
	{
		foreach (FileSystemWatcher fileSystemWatcher in _fileSystemWatchers)
		{
			fileSystemWatcher.Dispose();
		}
		_fileSystemWatchers.Clear();
		_backendLifetimeCancellationTokenSource.Cancel();
		_threadCancellationTokenSource.Cancel();
		_fileWatcherHandlerThread?.Join();
		foreach (Texture iconTexture in _iconTextures)
		{
			iconTexture.Dispose();
		}
		SaveUnsavedChangesInternal(ignoreErrors: true);
	}

	public override void Initialize()
	{
		Debug.Assert(!_isInitializingOrInitialized);
		if (_isInitializingOrInitialized)
		{
			return;
		}
		_isInitializingOrInitialized = true;
		_assetsDirectoryPath = AssetEditorOverlay.Interface.App.Settings.AssetsPath;
		AssetEditorOverlay.SetFileSaveStatus(AssetEditorOverlay.SaveStatus.Saved);
		CancellationToken cancellationToken = _backendLifetimeCancellationTokenSource.Token;
		Dictionary<string, AssetTypeConfig> assetTypes = null;
		Dictionary<string, SchemaNode> schemas = null;
		IDictionary<string, string> translationMapping = null;
		AssetEditorOverlay.SetAssetTreeInitializing(isInitializing: true);
		Task.Run(delegate
		{
			InitializeAssetTypeConfigs(out assetTypes, out schemas, out translationMapping);
		}).ContinueWith(delegate(Task t)
		{
			if (t.IsFaulted)
			{
				Logger.Error((Exception)t.Exception, "Failed to initialize asset types");
			}
			else if (!cancellationToken.IsCancellationRequested)
			{
				InitializeAssetMap(assetTypes);
				AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
				{
					if (!cancellationToken.IsCancellationRequested)
					{
						_translationMessages = translationMapping;
						InitializeCommonAssetFileWatcher();
						foreach (AssetTypeConfig value in assetTypes.Values)
						{
							if (value.IconImage != null)
							{
								Image iconImage = value.IconImage;
								Texture texture = new Texture(Texture.TextureTypes.Texture2D);
								texture.CreateTexture2D(iconImage.Width, iconImage.Height, iconImage.Pixels);
								_iconTextures.Add(texture);
								value.Icon = new PatchStyle(new TextureArea(texture, 0, 0, iconImage.Width, iconImage.Height, 1));
								value.IconImage = null;
							}
						}
						AssetEditorOverlay.SetupAssetTypes(schemas, assetTypes);
					}
				});
				Task.Run(delegate
				{
					ValidateAllCosmeticAssets(cancellationToken);
				}).ContinueWith(delegate(Task task)
				{
					if (task.IsFaulted)
					{
						Logger.Error((Exception)task.Exception, "Failed to validate cosmetic assets");
					}
				});
			}
		}).ContinueWith(delegate(Task t)
		{
			if (t.IsFaulted)
			{
				Logger.Error((Exception)t.Exception, "Failed to initialize backend");
			}
		});
	}

	public override string GetButtonText(string messageId)
	{
		int length = "assetTypes.".Length;
		if (messageId.Length > length && _translationMessages.TryGetValue(messageId.Substring(length), out var value))
		{
			return value;
		}
		return messageId;
	}

	private void SetupCommonAssetTypes(Dictionary<string, AssetTypeConfig> assetTypes, Dictionary<string, string> fileExtensionAssetTypeMapping)
	{
		RegisterCommonAssetType("Model", "Model", ".blockymodel", (AssetEditorEditorType)2, "Model.png", isColoredIcon: true);
		RegisterCommonAssetType("Texture", "Texture", ".png", (AssetEditorEditorType)5, "Texture.png", isColoredIcon: true);
		RegisterCommonAssetType("Animation", "Animation", ".blockyanim", (AssetEditorEditorType)6, "Animation.png", isColoredIcon: true);
		RegisterCommonAssetType("Sound", "Sound", ".ogg", (AssetEditorEditorType)0, "Audio.png");
		RegisterCommonAssetType("UI", "UI Markup", ".ui", (AssetEditorEditorType)1);
		RegisterCommonAssetType("Language", "Language", ".lang", (AssetEditorEditorType)5);
		void RegisterCommonAssetType(string type, string name, string fileExtension, AssetEditorEditorType editorType, string icon = "File.png", bool isColoredIcon = false)
		{
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			if (fileExtensionAssetTypeMapping != null)
			{
				fileExtensionAssetTypeMapping[fileExtension] = type;
			}
			assetTypes.Add(type, new AssetTypeConfig
			{
				Name = name,
				Id = type,
				IsColoredIcon = isColoredIcon,
				Icon = new PatchStyle("AssetEditor/AssetIcons/" + icon),
				Path = "Common",
				AssetTree = AssetTreeFolder.Common,
				FileExtension = fileExtension,
				EditorType = editorType
			});
		}
	}

	private void InitializeAssetTypeConfigs(out Dictionary<string, AssetTypeConfig> assetTypesOut, out Dictionary<string, SchemaNode> schemasOut, out IDictionary<string, string> translationMapping)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		Dictionary<string, SchemaNode> schemas = new Dictionary<string, SchemaNode>();
		Dictionary<string, AssetTypeConfig> assetTypes = new Dictionary<string, AssetTypeConfig>();
		schemasOut = schemas;
		assetTypesOut = assetTypes;
		translationMapping = Language.LoadServerLanguageFile("assetTypes.lang", AssetEditorOverlay.Interface.App.Settings.Language);
		SetupCommonAssetTypes(assetTypes, _fileExtensionAssetTypeMapping);
		string[] files = Directory.GetFiles(Path.Combine(Paths.EditorData, "CosmeticSchemas"));
		foreach (string text in files)
		{
			CancellationToken backendLifetimeCancellationToken = _backendLifetimeCancellationToken;
			if (backendLifetimeCancellationToken.IsCancellationRequested)
			{
				return;
			}
			if (text.EndsWith(".json"))
			{
				JObject jObject = JObject.Parse(File.ReadAllText(Path.Combine(Paths.EditorData, "CosmeticSchemas", text)));
				LoadSchema(jObject, schemas);
			}
		}
		RegisterCosmeticAssetType("Haircut", "Haircuts.json", CosmeticSchema.CreateHaircutSchema("/Characters/Haircuts/"), "Haircut");
		RegisterCosmeticAssetType("Overtop", "Overtops.json", CosmeticSchema.CreateCosmeticSchema("/Cosmetics/Chest/", "/Cosmetics/Overtops/"), "Overtop");
		RegisterCosmeticAssetType("Undertop", "Undertops.json", CosmeticSchema.CreateCosmeticSchema("/Cosmetics/Chest/", "/Cosmetics/Undertops/"), "Undertop");
		RegisterCosmeticAssetType("Pants", "Pants.json", CosmeticSchema.CreateCosmeticSchema("/Cosmetics/Legs/", "/Cosmetics/Pants/"), "Pants");
		RegisterCosmeticAssetType("Overpants", "Overpants.json", CosmeticSchema.CreateCosmeticSchema("/Cosmetics/Overpants/", "/Cosmetics/Overpants/"), "Overpants");
		RegisterCosmeticAssetType("EarAccessory", "EarAccessory.json", CosmeticSchema.CreateCosmeticSchema("/Cosmetics/Head/", "/Cosmetics/Ear_Accessories/"), "Ear Accessory");
		RegisterCosmeticAssetType("Ears", "Ears.json", CosmeticSchema.CreateCosmeticSchema("/Characters/Body_Attachments/Ears/"), "Ears");
		RegisterCosmeticAssetType("Eyebrows", "Eyebrows.json", CosmeticSchema.CreateCosmeticSchema("/Characters/Body_Attachments/Eyebrows/"), "Eyebrows");
		RegisterCosmeticAssetType("FacialHair", "FacialHair.json", CosmeticSchema.CreateCosmeticSchema("/Characters/Body_Attachments/Beards/"), "Facial Hair");
		RegisterCosmeticAssetType("HeadAccessory", "HeadAccessory.json", CosmeticSchema.CreateHeadAccessorySchema("/Cosmetics/Head/", "/Cosmetics/Head_Accessories/"), "Head Accessory");
		RegisterCosmeticAssetType("FaceAccessory", "FaceAccessory.json", CosmeticSchema.CreateHeadAccessorySchema("/Cosmetics/Head/", "/Cosmetics/Face_Accessories/"), "Face Accessory");
		RegisterCosmeticAssetType("Gloves", "Gloves.json", CosmeticSchema.CreateCosmeticSchema("/Cosmetics/Hands/", "/Cosmetics/Gloves/"), "Gloves");
		RegisterCosmeticAssetType("Mouth", "Mouths.json", CosmeticSchema.CreateCosmeticSchema("/Characters/Body_Attachments/Mouths/"), "Mouth");
		RegisterCosmeticAssetType("Shoes", "Shoes.json", CosmeticSchema.CreateCosmeticSchema("/Cosmetics/Shoes/", "/Cosmetics/Shoes/"), "Shoes");
		RegisterCosmeticAssetType("SkinFeature", "SkinFeatures.json", CosmeticSchema.CreateCosmeticSchema("/Characters/Body_Attachments/SkinFeatures/"), "Skin Feature");
		RegisterCosmeticAssetType("Eyes", "Eyes.json", CosmeticSchema.CreateCosmeticSchema("/Characters/Body_Attachments/Eyes/"), "Eyes");
		RegisterCosmeticAssetType("Face", "Faces.json", CosmeticSchema.CreateCosmeticSchema("/Characters/Body_Attachments/Faces/"), "Face");
		RegisterCosmeticAssetType("GradientSet", "GradientSets.json", schemas["https://schema.hytale.com/cosmetics/GradientSet.json"], "Gradient Set", "Color.png");
		RegisterCosmeticAssetType("EyeColor", "EyeColors.json", schemas["https://schema.hytale.com/cosmetics/TintColor.json"], "Eye Color", "Color.png");
		RegisterCosmeticAssetType("Emote", "Emotes.json", schemas["https://schema.hytale.com/cosmetics/Emote.json"], "Emote", "Emote.png");
		void RegisterCosmeticAssetType(string cosmeticType, string file, SchemaNode schema, string name, string icon = "Cosmetic.png")
		{
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Expected O, but got Unknown
			//IL_0128: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c5: Expected O, but got Unknown
			if (schema.Id == null)
			{
				schema.Id = "https://schema.hytale.com/cosmetics/" + cosmeticType.ToLowerInvariant() + ".json";
			}
			schemas[schema.Id] = schema;
			string text2 = "Cosmetics." + cosmeticType;
			JObject baseJsonAsset = null;
			if (cosmeticType == "Haircut" || cosmeticType == "FacialHair" || cosmeticType == "Eyebrows")
			{
				JObject val = new JObject();
				val.Add("GradientSet", JToken.op_Implicit("Hair"));
				baseJsonAsset = val;
			}
			else if (cosmeticType == "Mouth")
			{
				JObject val2 = new JObject();
				val2.Add("GradientSet", JToken.op_Implicit("Skin"));
				baseJsonAsset = val2;
			}
			assetTypes.Add(text2, new AssetTypeConfig
			{
				HasIdField = true,
				Schema = schema,
				Name = name,
				Id = text2,
				Icon = new PatchStyle("AssetEditor/AssetIcons/" + icon),
				BaseJsonAsset = baseJsonAsset,
				Path = "Cosmetics/CharacterCreator/" + file,
				AssetTree = AssetTreeFolder.Cosmetics,
				EditorType = (AssetEditorEditorType)3,
				FileExtension = ".json"
			});
		}
	}

	private bool TryParseAssetType(JObject json, Dictionary<string, SchemaNode> schemas, IDictionary<string, string> translationMapping, out AssetTypeConfig assetTypeConfig)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		assetTypeConfig = null;
		SchemaNode schemaNode = LoadSchema(json, schemas);
		JObject val = (JObject)json["hytale"];
		if (val == null)
		{
			return false;
		}
		if (val.ContainsKey("uiEditorIgnore") && (bool)val["uiEditorIgnore"])
		{
			return false;
		}
		string text = null;
		bool isVirtual = false;
		if (val.ContainsKey("path"))
		{
			text = (string)val["path"];
		}
		else if (val.ContainsKey("virtualPath"))
		{
			text = (string)val["virtualPath"];
			isVirtual = true;
		}
		string text2 = Path.Combine(Paths.BuiltInAssets, "Server");
		if (text == null || !Paths.IsSubPathOf(Path.Combine(text2, text), text2))
		{
			return false;
		}
		string fileExtension = ".json";
		if (val.ContainsKey("extension"))
		{
			fileExtension = (string)val["extension"];
		}
		if (!translationMapping.TryGetValue((string)json["title"] + ".title", out var value))
		{
			value = (string)json["title"];
		}
		assetTypeConfig = new AssetTypeConfig
		{
			Schema = schemaNode,
			Id = Path.GetFileNameWithoutExtension(schemaNode.Id),
			Name = value,
			Path = AssetPathUtils.CombinePaths("Server", text),
			AssetTree = AssetTreeFolder.Server,
			EditorType = (AssetEditorEditorType)3,
			FileExtension = fileExtension,
			IsVirtual = isVirtual
		};
		ApplySchemaMetadata(assetTypeConfig, val);
		return true;
	}

	public override void SaveUnsavedChanges()
	{
		SaveUnsavedChangesInternal(ignoreErrors: true);
	}

	private void ProcessCallback<T>(Action<T, FormattedMessage> action, T value, FormattedMessage error)
	{
		if (!_backendLifetimeCancellationTokenSource.IsCancellationRequested)
		{
			AssetEditorOverlay.Interface.Engine.RunOnMainThread(AssetEditorOverlay.Interface, delegate
			{
				action(value, error);
			}, allowCallFromMainThread: true);
		}
	}

	private void ProcessErrorCallback<T>(Action<T, FormattedMessage> action, FormattedMessage error)
	{
		ProcessCallback(action, default(T), error);
	}

	private void ProcessSuccessCallback<T>(Action<T, FormattedMessage> action, T value)
	{
		ProcessCallback(action, value, null);
	}

	private void FetchJsonAsset(AssetTypeConfig assetTypeConfig, AssetReference assetReference, Action<JObject, FormattedMessage> callback, bool fromOpenedTab)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		if (_unsavedJsonAssets.TryGetValue(assetReference.FilePath, out var value))
		{
			ProcessSuccessCallback(callback, (JObject)((JToken)value).DeepClone());
			return;
		}
		string assetId = AssetEditorOverlay.GetAssetIdFromReference(assetReference);
		string lastSaveFileHash = null;
		if (_undoRedoStacks.TryGetValue(assetReference.FilePath, out var value2))
		{
			lastSaveFileHash = value2.SaveFileHash;
		}
		Task.Run(delegate
		{
			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e5: Expected O, but got Unknown
			if (assetTypeConfig.AssetTree == AssetTreeFolder.Cosmetics)
			{
				string path = assetTypeConfig.Path;
				byte[] array = File.ReadAllBytes(Path.Combine(_assetsDirectoryPath, path));
				string @string = Encoding.UTF8.GetString(array);
				JArray val = JArray.Parse(@string);
				bool clearUndoRedoStack2 = false;
				if (fromOpenedTab && lastSaveFileHash != null)
				{
					clearUndoRedoStack2 = AssetManager.ComputeHash(array) != lastSaveFileHash;
				}
				JObject assetJson = null;
				foreach (JToken item in val)
				{
					if (!((string)item[(object)"Id"] != assetId))
					{
						assetJson = (JObject)item;
						break;
					}
				}
				AssetEditorOverlay.Interface.Engine.RunOnMainThread(AssetEditorOverlay.Interface, delegate
				{
					if (clearUndoRedoStack2)
					{
						_undoRedoStacks.Remove(assetReference.FilePath);
					}
					if (assetJson == null)
					{
						ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.assetNotFound"));
					}
					else
					{
						ProcessSuccessCallback(callback, assetJson);
					}
				});
			}
			else
			{
				string fullPath = Path.GetFullPath(Path.Combine(_assetsDirectoryPath, assetReference.FilePath));
				string fullPath2 = Path.GetFullPath(_assetsDirectoryPath);
				if (!Paths.IsSubPathOf(fullPath, fullPath2))
				{
					throw new Exception("Path must be within assets directory");
				}
				if (!File.Exists(fullPath))
				{
					ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.assetNotFound"));
				}
				else
				{
					byte[] array2 = File.ReadAllBytes(fullPath);
					string string2 = Encoding.UTF8.GetString(array2);
					bool clearUndoRedoStack = false;
					if (fromOpenedTab && lastSaveFileHash != null)
					{
						clearUndoRedoStack = AssetManager.ComputeHash(array2) != lastSaveFileHash;
					}
					JObject json;
					try
					{
						json = JObject.Parse(string2);
					}
					catch (Exception)
					{
						ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.invalidJson"));
						return;
					}
					AssetEditorOverlay.Interface.Engine.RunOnMainThread(AssetEditorOverlay.Interface, delegate
					{
						if (clearUndoRedoStack)
						{
							_undoRedoStacks.Remove(assetReference.FilePath);
						}
						ProcessSuccessCallback(callback, json);
					});
				}
			}
		}).ContinueWith(delegate(Task t)
		{
			if (t.IsFaulted)
			{
				Logger.Error((Exception)t.Exception, "Failed to fetch asset");
				ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.errorOccurredFetching"));
			}
		});
	}

	private void FetchImageAsset(AssetReference assetReference, Action<Image, FormattedMessage> callback)
	{
		if (_unsavedImageAssets.TryGetValue(assetReference.FilePath, out var value))
		{
			ProcessSuccessCallback(callback, value);
			return;
		}
		string fullPath = Path.GetFullPath(Path.Combine(_assetsDirectoryPath, assetReference.FilePath));
		string fullPath2 = Path.GetFullPath(_assetsDirectoryPath);
		if (!Paths.IsSubPathOf(fullPath, fullPath2))
		{
			throw new Exception("Path must be within assets directory");
		}
		Task.Run(delegate
		{
			if (!File.Exists(fullPath))
			{
				ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.assetNotFound"));
			}
			else
			{
				byte[] data = File.ReadAllBytes(fullPath);
				Image value2 = new Image(data);
				ProcessSuccessCallback(callback, value2);
			}
		}).ContinueWith(delegate(Task t)
		{
			if (t.IsFaulted)
			{
				Logger.Error((Exception)t.Exception, "Failed to fetch asset");
				ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.errorOccurredFetching"));
			}
		});
	}

	private void FetchTextAsset(AssetReference assetReference, Action<string, FormattedMessage> callback)
	{
		if (_unsavedTextAssets.TryGetValue(assetReference.FilePath, out var value))
		{
			ProcessSuccessCallback(callback, value);
			return;
		}
		string fullPath = Path.GetFullPath(Path.Combine(_assetsDirectoryPath, assetReference.FilePath));
		string fullPath2 = Path.GetFullPath(_assetsDirectoryPath);
		if (!Paths.IsSubPathOf(fullPath, fullPath2))
		{
			throw new Exception("Path must be within assets directory");
		}
		Task.Run(delegate
		{
			if (!File.Exists(fullPath))
			{
				ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.assetNotFound"));
			}
			else
			{
				string value2 = File.ReadAllText(fullPath);
				ProcessSuccessCallback(callback, value2);
			}
		}).ContinueWith(delegate(Task t)
		{
			if (t.IsFaulted)
			{
				Logger.Error((Exception)t.Exception, "Failed to fetch asset");
				ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.errorOccurredFetching"));
			}
		});
	}

	public override void FetchJsonAssetWithParents(AssetReference rootAssetReference, Action<Dictionary<string, TrackedAsset>, FormattedMessage> callback, bool fromOpenedTab = false)
	{
		if (!AssetEditorOverlay.AssetTypeRegistry.AssetTypes.TryGetValue(rootAssetReference.Type, out var assetTypeConfig))
		{
			Logger.Warn("Tried opening asset with unknown type: {0}", rootAssetReference.Type);
			FormattedMessage error = FormattedMessage.FromMessageId("ui.assetEditor.errors.unknownAssetType", new Dictionary<string, object> { { "assetType", rootAssetReference.Type } });
			ProcessErrorCallback(callback, error);
			return;
		}
		if (assetTypeConfig.AssetTree == AssetTreeFolder.Cosmetics)
		{
			throw new Exception("Single file assets are not supported");
		}
		Dictionary<string, TrackedAsset> results = new Dictionary<string, TrackedAsset>();
		bool clearUndoRedoStack = false;
		string lastSaveFileHash = null;
		if (_undoRedoStacks.TryGetValue(rootAssetReference.FilePath, out var value))
		{
			lastSaveFileHash = value.SaveFileHash;
		}
		TryLoadAsset(rootAssetReference, isInitialAsset: true).ContinueWith(delegate(Task<bool> t)
		{
			if (t.IsFaulted)
			{
				Logger.Error((Exception)t.Exception, "Failed to fetch asset");
				ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.errorOccurredFetching"));
			}
			if (t.Result)
			{
				AssetEditorOverlay.Interface.Engine.RunOnMainThread(AssetEditorOverlay.Interface, delegate
				{
					if (clearUndoRedoStack)
					{
						_undoRedoStacks.Remove(rootAssetReference.FilePath);
					}
					ProcessSuccessCallback(callback, results);
				});
			}
		});
		async Task<bool> TryLoadAsset(AssetReference assetToLoad, bool isInitialAsset = false)
		{
			Debug.Assert(ThreadHelper.IsMainThread());
			if (results.ContainsKey(assetToLoad.FilePath))
			{
				return true;
			}
			if (!AssetEditorOverlay.AssetTypeRegistry.AssetTypes.TryGetValue(rootAssetReference.Type, out var assetType2))
			{
				Logger.Warn("Tried opening asset with unknown type: {0}", rootAssetReference.Type);
				ProcessErrorCallback(callback, new FormattedMessage
				{
					MessageId = "ui.assetEditor.errors.unknownAssetType",
					Params = new Dictionary<string, object> { { "assetType", rootAssetReference.Type } }
				});
				return false;
			}
			AssetEditorOverlay.GetAssetIdFromReference(assetToLoad);
			SchemaNode schema2 = AssetEditorOverlay.ResolveSchema(assetType2.Schema, assetType2.Schema);
			if (_unsavedJsonAssets.TryGetValue(assetToLoad.FilePath, out var unsavedAsset))
			{
				results[assetToLoad.FilePath] = new TrackedAsset(assetToLoad, unsavedAsset);
				return await TryLoadDependencies(schema2, unsavedAsset);
			}
			string fullPath = Path.GetFullPath(Path.Combine(_assetsDirectoryPath, assetToLoad.FilePath));
			if (!Paths.IsSubPathOf(baseDirPath: Path.GetFullPath(_assetsDirectoryPath), path: fullPath))
			{
				throw new Exception("Path must be within assets directory");
			}
			return await Task.Run(async delegate
			{
				if (!File.Exists(fullPath))
				{
					ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.assetNotFound"));
					return false;
				}
				byte[] bytes = File.ReadAllBytes(fullPath);
				string str = Encoding.UTF8.GetString(bytes);
				if (isInitialAsset && fromOpenedTab && lastSaveFileHash != null)
				{
					clearUndoRedoStack = AssetManager.ComputeHash(bytes) != lastSaveFileHash;
				}
				JObject json;
				try
				{
					json = JObject.Parse(str);
				}
				catch (Exception)
				{
					ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.invalidJson"));
					return false;
				}
				results[assetToLoad.FilePath] = new TrackedAsset(assetToLoad, json);
				TaskCompletionSource<bool> t2 = new TaskCompletionSource<bool>();
				AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, async delegate
				{
					bool res = await TryLoadDependencies(schema2, json);
					t2.SetResult(res);
				});
				return await t2.Task;
			});
		}
		async Task<bool> TryLoadDependencies(SchemaNode schema, JObject obj)
		{
			Debug.Assert(ThreadHelper.IsMainThread());
			if (schema.TypePropertyKey != null)
			{
				int num;
				if (schema.HasParentProperty)
				{
					JToken obj2 = obj["Parent"];
					num = ((obj2 != null && (int)obj2.Type == 8) ? 1 : 0);
				}
				else
				{
					num = 0;
				}
				if (num != 0)
				{
					SchemaNode typeSchema = AssetEditorOverlay.ResolveSchema(schema.TypeSchemas[0], assetTypeConfig.Schema);
					string parentAssetType = typeSchema.Properties["Parent"].AssetType;
					if (!AssetEditorOverlay.AssetTypeRegistry.AssetTypes.TryGetValue(parentAssetType, out var parentAssetTypeConfig))
					{
						ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.unknownAssetType", new Dictionary<string, object> { { "assetType", parentAssetType } }));
						return false;
					}
					if (!AssetEditorOverlay.Assets.TryGetPathForAssetId(parentAssetTypeConfig.IdProvider ?? parentAssetTypeConfig.Id, (string)obj["Parent"], out var assetPath2))
					{
						ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.assetNotFound"));
						return false;
					}
					if (!(await TryLoadAsset(new AssetReference(parentAssetTypeConfig.IdProvider ?? parentAssetTypeConfig.Id, assetPath2))))
					{
						return false;
					}
				}
				return true;
			}
			foreach (KeyValuePair<string, JToken> prop in obj)
			{
				if (schema.Properties.TryGetValue(prop.Key, out var schemaProp))
				{
					if (schemaProp.IsParentProperty && schemaProp.AssetType != null && AssetEditorOverlay.AssetTypeRegistry.AssetTypes.TryGetValue(schemaProp.AssetType, out var assetType) && AssetEditorOverlay.Assets.TryGetPathForAssetId(assetType.IdProvider ?? schemaProp.AssetType, (string)prop.Value, out var assetPath) && !results.ContainsKey(assetPath) && !(await TryLoadAsset(new AssetReference(assetType.IdProvider ?? schemaProp.AssetType, assetPath))))
					{
						return false;
					}
					schemaProp = AssetEditorOverlay.ResolveSchema(schemaProp, assetTypeConfig.Schema);
					JToken value2 = prop.Value;
					if (value2 != null && (int)value2.Type == 1)
					{
						if (schemaProp.Type == SchemaNode.NodeType.AssetReferenceOrInline)
						{
							schemaProp = AssetEditorOverlay.ResolveSchema(schemaProp.Value, assetTypeConfig.Schema);
						}
						if (schemaProp.Type == SchemaNode.NodeType.Object && !(await TryLoadDependencies(schemaProp, (JObject)prop.Value)))
						{
							return false;
						}
					}
					schemaProp = null;
					assetType = null;
					assetPath = null;
				}
			}
			return true;
		}
	}

	public override void FetchAsset(AssetReference assetReference, Action<object, FormattedMessage> callback, bool fromOpenedTab = false)
	{
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected I4, but got Unknown
		if (!AssetEditorOverlay.AssetTypeRegistry.AssetTypes.TryGetValue(assetReference.Type, out var value))
		{
			Logger.Warn("Tried opening asset with unknown type: {0}", assetReference.Type);
			ProcessErrorCallback(callback, new FormattedMessage
			{
				MessageId = "ui.assetEditor.errors.unknownAssetType",
				Params = new Dictionary<string, object> { { "assetType", assetReference.Type } }
			});
			return;
		}
		try
		{
			AssetEditorEditorType editorType = value.EditorType;
			AssetEditorEditorType val = editorType;
			switch (val - 1)
			{
			case 4:
				FetchImageAsset(assetReference, callback);
				break;
			case 1:
			case 2:
			case 3:
			case 5:
				FetchJsonAsset(value, assetReference, callback, fromOpenedTab);
				break;
			case 0:
				FetchTextAsset(assetReference, callback);
				break;
			default:
				throw new Exception("Unhandled file type " + ((object)(AssetEditorEditorType)(ref value.EditorType)).ToString());
			}
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to fetch asset");
			ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.errorOccurredFetching"));
		}
	}

	public override void UpdateJsonAsset(AssetReference assetReference, List<ClientJsonUpdateCommand> commands, Action<FormattedMessage> callback = null)
	{
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Expected O, but got Unknown
		if (!_undoRedoStacks.TryGetValue(assetReference.FilePath, out var value))
		{
			AssetUndoRedoStacks assetUndoRedoStacks2 = (_undoRedoStacks[assetReference.FilePath] = new AssetUndoRedoStacks());
			value = assetUndoRedoStacks2;
		}
		if (value.RedoStack.Count > 0)
		{
			value.RedoStack.Clear();
		}
		foreach (ClientJsonUpdateCommand command in commands)
		{
			value.UndoStack.Push(command);
		}
		JObject asset = (JObject)AssetEditorOverlay.TrackedAssets[assetReference.FilePath].Data;
		UpdateJsonAsset(assetReference, asset);
		if (callback != null)
		{
			AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
			{
				callback(null);
			}, allowCallFromMainThread: true);
		}
	}

	private void UpdateJsonAsset(AssetReference assetReference, JObject asset)
	{
		AssetEditorOverlay.SetFileSaveStatus(AssetEditorOverlay.SaveStatus.Unsaved);
		_unsavedJsonAssets[assetReference.FilePath] = asset;
		ValidateJsonAsset(assetReference, asset);
		string type = assetReference.Type;
		string text = type;
		if (text == "ItemCategory")
		{
			_dropdownDatasetEntriesCache.Remove("ItemCategories");
		}
	}

	public override void SetOpenEditorAsset(AssetReference assetReference)
	{
		SaveUnsavedChangesInternal(ignoreErrors: true);
	}

	public override void UpdateAsset(AssetReference assetReference, object data, Action<FormattedMessage> callback = null)
	{
		AssetEditorOverlay.SetFileSaveStatus(AssetEditorOverlay.SaveStatus.Unsaved);
		if (!(data is Image value))
		{
			JObject val = (JObject)((data is JObject) ? data : null);
			if (val == null)
			{
				if (data is string value2)
				{
					_unsavedTextAssets[assetReference.FilePath] = value2;
				}
			}
			else
			{
				_unsavedJsonAssets[assetReference.FilePath] = val;
			}
		}
		else
		{
			_unsavedImageAssets[assetReference.FilePath] = value;
		}
		if (callback != null)
		{
			AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
			{
				callback(null);
			}, allowCallFromMainThread: true);
		}
	}

	private void ValidateEmote(JObject asset, List<AssetDiagnosticMessage> errors, List<AssetDiagnosticMessage> warnings)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I4
		ValidateName(asset, errors, warnings);
		JToken val = asset["Animation"];
		if (JsonUtils.IsNull(val) || (int)val.Type != 8 || string.IsNullOrWhiteSpace((string)val))
		{
			errors.Add(new AssetDiagnosticMessage("Animation", "An emote must have a animation defined."));
		}
	}

	private void ValidateCosmetic(JObject asset, List<AssetDiagnosticMessage> errors, List<AssetDiagnosticMessage> warnings, bool requiresBaseColor = true)
	{
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Invalid comparison between Unknown and I4
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Invalid comparison between Unknown and I4
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Invalid comparison between Unknown and I4
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Invalid comparison between Unknown and I4
		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Invalid comparison between Unknown and I4
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Invalid comparison between Unknown and I4
		//IL_03ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f2: Invalid comparison between Unknown and I4
		//IL_049f: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a5: Invalid comparison between Unknown and I4
		//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Invalid comparison between Unknown and I4
		//IL_04fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0504: Invalid comparison between Unknown and I4
		//IL_0595: Unknown result type (might be due to invalid IL or missing references)
		//IL_059b: Invalid comparison between Unknown and I4
		ValidateName(asset, errors, warnings);
		if (JsonUtils.IsNull(asset["Model"]) && JsonUtils.IsNull(asset["Variants"]))
		{
			errors.Add(new AssetDiagnosticMessage("Model", "A cosmetic must have a model or variant defined."));
			errors.Add(new AssetDiagnosticMessage("Variants", "A cosmetic must have a model or variant defined."));
		}
		JToken obj = asset["Tags"];
		JArray val = (JArray)(object)((obj is JArray) ? obj : null);
		if (val != null)
		{
			for (int i = 0; i < ((JContainer)val).Count; i++)
			{
				JToken val2 = val[i];
				if (val2 == null || (int)val2.Type != 8 || ((string)val2).Trim() == "")
				{
					errors.Add(new AssetDiagnosticMessage("Tags." + i, "A valid tag must be specified"));
				}
			}
		}
		JToken obj2 = asset["Model"];
		if (obj2 != null && (int)obj2.Type == 8)
		{
			JToken obj3 = asset["GreyscaleTexture"];
			if (obj3 == null || (int)obj3.Type != 8)
			{
				JToken obj4 = asset["Textures"];
				JObject val3 = (JObject)(object)((obj4 is JObject) ? obj4 : null);
				if (val3 == null || ((JContainer)val3).Count == 0)
				{
					errors.Add(new AssetDiagnosticMessage("Textures", "At least one texture must be defined."));
					errors.Add(new AssetDiagnosticMessage("GreyscaleTexture", "At least one texture must be defined."));
					goto IL_0300;
				}
			}
			JToken obj5 = asset["Textures"];
			JObject val4 = (JObject)(object)((obj5 is JObject) ? obj5 : null);
			if (val4 != null)
			{
				foreach (KeyValuePair<string, JToken> item in val4)
				{
					if ((int)item.Value.Type != 1)
					{
						errors.Add(new AssetDiagnosticMessage("Textures." + item.Key, "A valid texture must be assigned."));
						continue;
					}
					JToken obj6 = item.Value[(object)"Texture"];
					if (obj6 == null || (int)obj6.Type != 8 || string.IsNullOrWhiteSpace((string)item.Value[(object)"Texture"]))
					{
						errors.Add(new AssetDiagnosticMessage("Textures." + item.Key + ".Texture", "A valid texture must be assigned."));
					}
					if (requiresBaseColor)
					{
						JToken obj7 = item.Value[(object)"BaseColor"];
						if (obj7 == null || (int)obj7.Type != 2)
						{
							errors.Add(new AssetDiagnosticMessage("Textures." + item.Key + ".BaseColor", "A valid preview color must be assigned"));
						}
					}
				}
			}
		}
		goto IL_0300;
		IL_0300:
		JToken obj8 = asset["Variants"];
		if (obj8 == null || (int)obj8.Type != 1)
		{
			return;
		}
		foreach (KeyValuePair<string, JToken> item2 in (JObject)asset["Variants"])
		{
			if (JsonUtils.IsNull(item2.Value) || JsonUtils.IsNull(item2.Value[(object)"Model"]))
			{
				errors.Add(new AssetDiagnosticMessage("Variants." + item2.Key + ".Model", "This field cannot be empty."));
			}
			if (!JsonUtils.IsNull(item2.Value))
			{
				JToken obj9 = item2.Value[(object)"Textures"];
				JObject val5 = (JObject)(object)((obj9 is JObject) ? obj9 : null);
				if (val5 == null || ((JContainer)val5).Count == 0)
				{
					JToken obj10 = item2.Value[(object)"GreyscaleTexture"];
					if (obj10 == null || (int)obj10.Type != 8)
					{
						goto IL_0403;
					}
				}
				JToken obj11 = item2.Value[(object)"Textures"];
				JObject val6 = (JObject)(object)((obj11 is JObject) ? obj11 : null);
				if (val6 == null)
				{
					continue;
				}
				foreach (KeyValuePair<string, JToken> item3 in val6)
				{
					if ((int)item3.Value.Type != 1)
					{
						errors.Add(new AssetDiagnosticMessage("Variants." + item2.Key + ".Textures." + item3.Key, "A valid texture must be assigned."));
						continue;
					}
					JToken obj12 = item3.Value[(object)"Texture"];
					if (obj12 == null || (int)obj12.Type != 8 || string.IsNullOrWhiteSpace((string)item3.Value[(object)"Texture"]))
					{
						errors.Add(new AssetDiagnosticMessage("Variants." + item2.Key + ".Textures." + item3.Key + ".Texture", "A valid texture must be assigned."));
					}
					if (requiresBaseColor)
					{
						JToken obj13 = item3.Value[(object)"BaseColor"];
						if (obj13 == null || (int)obj13.Type != 2)
						{
							errors.Add(new AssetDiagnosticMessage("Variants." + item2.Key + ".Textures." + item3.Key + ".BaseColor", "A valid preview color must be assigned"));
						}
					}
				}
				continue;
			}
			goto IL_0403;
			IL_0403:
			errors.Add(new AssetDiagnosticMessage("Variants." + item2.Key + ".Textures", "At least one texture must be defined."));
			errors.Add(new AssetDiagnosticMessage("Variants." + item2.Key + ".GreyscaleTexture", "At least one texture must be defined."));
		}
	}

	private void ValidateEyeColor(JObject asset, List<AssetDiagnosticMessage> errors, List<AssetDiagnosticMessage> warnings)
	{
		if (JsonUtils.IsNull(asset["BaseColor"]))
		{
			errors.Add(new AssetDiagnosticMessage("BaseColor", "A preview color must be specified"));
		}
	}

	private void ValidateGradientSet(JObject asset, List<AssetDiagnosticMessage> errors, List<AssetDiagnosticMessage> warnings)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Invalid comparison between Unknown and I4
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected O, but got Unknown
		if (JsonUtils.IsNull(asset["Gradients"]) || (int)asset["Gradients"].Type != 1 || ((JContainer)(JObject)asset["Gradients"]).Count == 0)
		{
			errors.Add(new AssetDiagnosticMessage("Gradients", "At least one gradient must be specified"));
			return;
		}
		JObject val = (JObject)asset["Gradients"];
		foreach (KeyValuePair<string, JToken> item in val)
		{
			if (JsonUtils.IsNull(item.Value) || JsonUtils.IsNull(item.Value[(object)"Texture"]))
			{
				errors.Add(new AssetDiagnosticMessage("Gradients." + item.Key + ".Texture", "A gradient texture must be specified"));
			}
			if (JsonUtils.IsNull(item.Value) || JsonUtils.IsNull(item.Value[(object)"BaseColor"]))
			{
				errors.Add(new AssetDiagnosticMessage("Gradients." + item.Key + ".BaseColor", "A preview color must be specified"));
			}
		}
	}

	private void ValidateName(JObject asset, List<AssetDiagnosticMessage> errors, List<AssetDiagnosticMessage> warnings)
	{
		if (JsonUtils.IsNull(asset["Name"]))
		{
			errors.Add(new AssetDiagnosticMessage("Name", "This field cannot be empty."));
		}
		else if (((string)asset["Name"]).Length < 3)
		{
			warnings.Add(new AssetDiagnosticMessage("Name", "A name should be at least 3 characters long."));
		}
	}

	private AssetDiagnostics GetAssetDiagnostics(AssetReference assetReference, JObject asset)
	{
		if (AssetEditorOverlay.AssetTypeRegistry.AssetTypes[assetReference.Type].AssetTree != AssetTreeFolder.Cosmetics)
		{
			return AssetDiagnostics.None;
		}
		List<AssetDiagnosticMessage> list = new List<AssetDiagnosticMessage>();
		List<AssetDiagnosticMessage> list2 = new List<AssetDiagnosticMessage>();
		try
		{
			switch (assetReference.Type)
			{
			case "Cosmetics.Emote":
				ValidateEmote(asset, list, list2);
				break;
			case "Cosmetics.EyeColor":
				ValidateEyeColor(asset, list, list2);
				break;
			case "Cosmetics.GradientSet":
				ValidateGradientSet(asset, list, list2);
				break;
			case "Cosmetics.Ears":
			case "Cosmetics.Mouth":
			case "Cosmetics.Eyes":
				ValidateCosmetic(asset, list, list2, requiresBaseColor: false);
				break;
			default:
				ValidateCosmetic(asset, list, list2);
				break;
			}
		}
		catch (Exception ex)
		{
			Logger.Warn(ex, "Validation error");
			list.Add(new AssetDiagnosticMessage(null, "An error occurred during asset validation"));
		}
		return new AssetDiagnostics(list.ToArray(), list2.ToArray());
	}

	private void ValidateJsonAsset(AssetReference assetReference, JObject asset)
	{
		AssetDiagnostics assetDiagnostics = GetAssetDiagnostics(assetReference, asset);
		AssetEditorOverlay.OnDiagnosticsUpdated(new Dictionary<string, AssetDiagnostics> { { assetReference.FilePath, assetDiagnostics } });
	}

	private void LoadWwiseDropdownEntries()
	{
		WwiseHeaderParser.Parse(Path.Combine(Paths.BuiltInAssets, "Common/SoundBanks/Wwise_IDs.h"), out var upcomingWwiseIds);
		AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
		{
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, WwiseResource> item in upcomingWwiseIds)
			{
				if (item.Value.Type == WwiseResource.WwiseResourceType.Event)
				{
					list.Add(item.Key);
				}
			}
			_loadingDropdownDatasets.Remove("WwiseEventIds");
			_dropdownDatasetEntriesCache["WwiseEventIds"] = list;
			AssetEditorOverlay.OnDropdownDatasetUpdated("WwiseEventIds", list);
		});
	}

	private void LoadItemCategoriesEntries()
	{
		Dictionary<string, JObject> fileJsonMapping = new Dictionary<string, JObject>();
		string text = UnixPathUtil.ConvertToUnixPath(Path.GetFullPath(Path.Combine(_assetsDirectoryPath)));
		string[] files = Directory.GetFiles(Path.Combine(_assetsDirectoryPath, "Server", "Item", "Category", "CreativeLibrary"), "*.json");
		foreach (string path in files)
		{
			string key = Paths.StripBasePath(UnixPathUtil.ConvertToUnixPath(path), text + "/");
			try
			{
				JObject value = JObject.Parse(File.ReadAllText(path));
				fileJsonMapping.Add(key, value);
			}
			catch (Exception)
			{
			}
		}
		AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
		{
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Invalid comparison between Unknown and I4
			//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f1: Invalid comparison between Unknown and I4
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, JObject> item in fileJsonMapping)
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item.Key);
				if (!_unsavedJsonAssets.TryGetValue(item.Key, out var value2))
				{
					value2 = item.Value;
				}
				if (value2["Children"] != null)
				{
					JToken obj = value2["Children"];
					if (obj != null && (int)obj.Type == 2)
					{
						foreach (JToken item2 in (IEnumerable<JToken>)value2["Children"])
						{
							if (item2 != null && item2 != null)
							{
								JToken obj2 = item2[(object)"Id"];
								if ((int)((obj2 != null) ? new JTokenType?(obj2.Type) : null).GetValueOrDefault() == 8)
								{
									list.Add(string.Format("{0}.{1}", fileNameWithoutExtension, item2[(object)"Id"]));
								}
							}
						}
					}
				}
			}
			_loadingDropdownDatasets.Remove("ItemCategories");
			_dropdownDatasetEntriesCache["ItemCategories"] = list;
			AssetEditorOverlay.OnDropdownDatasetUpdated("ItemCategories", list);
		});
	}

	private void LoadDropdownEntries(string dataset)
	{
		Task.Run(delegate
		{
			string text = dataset;
			string text2 = text;
			if (!(text2 == "WwiseEventIds"))
			{
				if (text2 == "ItemCategories")
				{
					LoadItemCategoriesEntries();
				}
			}
			else
			{
				LoadWwiseDropdownEntries();
			}
		}).ContinueWith(delegate(Task t)
		{
			if (t.IsFaulted)
			{
				Logger.Error((Exception)t.Exception, "Failed to fetch dataset");
			}
		});
	}

	public override bool TryGetDropdownEntriesOrFetch(string dataset, out List<string> entries, object extraValue = null)
	{
		if (base.TryGetDropdownEntriesOrFetch(dataset, out entries, extraValue))
		{
			return true;
		}
		if (dataset != "ItemCategories" && dataset != "WwiseEventIds")
		{
			entries = new List<string>();
			return true;
		}
		if (_dropdownDatasetEntriesCache.TryGetValue(dataset, out entries))
		{
			return true;
		}
		if (!_loadingDropdownDatasets.Contains(dataset))
		{
			_loadingDropdownDatasets.Add(dataset);
			LoadDropdownEntries(dataset);
		}
		return false;
	}

	public override void FetchAutoCompleteData(string dataset, string query, Action<HashSet<string>, FormattedMessage> callback)
	{
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Invalid comparison between Unknown and I4
		AssetReference currentAsset = AssetEditorOverlay.CurrentAsset;
		query = query.ToLowerInvariant();
		HashSet<string> hashSet = new HashSet<string>();
		if (AssetEditorOverlay.AssetTypeRegistry.AssetTypes[currentAsset.Type].AssetTree == AssetTreeFolder.Cosmetics && dataset == "Cosmetics.Tags")
		{
			foreach (CharacterPart characterPart in GetCharacterParts(currentAsset.Type))
			{
				if (characterPart.Tags == null)
				{
					continue;
				}
				string[] tags = characterPart.Tags;
				foreach (string text in tags)
				{
					if (text != null && !(text.Trim() == "") && (!(query != "") || text.ToLowerInvariant().Contains(query)))
					{
						hashSet.Add(text);
					}
				}
			}
			foreach (JObject value in _unsavedJsonAssets.Values)
			{
				JToken obj = value["Tags"];
				JArray val = (JArray)(object)((obj is JArray) ? obj : null);
				if (val == null)
				{
					continue;
				}
				foreach (JToken item in val)
				{
					if (item != null && (int)item.Type == 8)
					{
						string text2 = (string)item;
						if (!(text2.Trim() == "") && (!(query != "") || text2.ToLowerInvariant().Contains(query)))
						{
							hashSet.Add(text2);
						}
					}
				}
			}
		}
		ProcessSuccessCallback(callback, hashSet);
	}

	private List<CharacterPart> GetCharacterParts(string assetType)
	{
		return assetType switch
		{
			"Cosmetics.Haircut" => new List<CharacterPart>(AssetEditorOverlay.Interface.App.CharacterPartStore.Haircuts), 
			"Cosmetics.Overtop" => AssetEditorOverlay.Interface.App.CharacterPartStore.Overtops, 
			"Cosmetics.Undertop" => AssetEditorOverlay.Interface.App.CharacterPartStore.Undertops, 
			"Cosmetics.Pants" => AssetEditorOverlay.Interface.App.CharacterPartStore.Pants, 
			"Cosmetics.Overpants" => AssetEditorOverlay.Interface.App.CharacterPartStore.Overpants, 
			"Cosmetics.EarAccessory" => AssetEditorOverlay.Interface.App.CharacterPartStore.EarAccessory, 
			"Cosmetics.Ears" => AssetEditorOverlay.Interface.App.CharacterPartStore.Ears, 
			"Cosmetics.Eyebrows" => AssetEditorOverlay.Interface.App.CharacterPartStore.Eyebrows, 
			"Cosmetics.FacialHair" => AssetEditorOverlay.Interface.App.CharacterPartStore.FacialHair, 
			"Cosmetics.HeadAccessory" => new List<CharacterPart>(AssetEditorOverlay.Interface.App.CharacterPartStore.HeadAccessory), 
			"Cosmetics.Gloves" => AssetEditorOverlay.Interface.App.CharacterPartStore.Gloves, 
			"Cosmetics.Mouth" => AssetEditorOverlay.Interface.App.CharacterPartStore.Mouths, 
			"Cosmetics.Shoes" => AssetEditorOverlay.Interface.App.CharacterPartStore.Shoes, 
			"Cosmetics.SkinFeature" => AssetEditorOverlay.Interface.App.CharacterPartStore.SkinFeatures, 
			"Cosmetics.Eyes" => AssetEditorOverlay.Interface.App.CharacterPartStore.Eyes, 
			_ => null, 
		};
	}

	public override void CreateAsset(AssetReference assetReference, object data, string buttonId = null, bool openInTab = false, Action<FormattedMessage> callback = null)
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected O, but got Unknown
		Logger.Info<string, string>("Creating asset of type {0} in {1}", assetReference.Type, assetReference.FilePath);
		AssetTypeConfig assetTypeConfig = AssetEditorOverlay.AssetTypeRegistry.AssetTypes[assetReference.Type];
		if (assetTypeConfig.AssetTree == AssetTreeFolder.Cosmetics)
		{
			try
			{
				string path = Path.Combine(_assetsDirectoryPath, assetTypeConfig.Path);
				JArray val = JArray.Parse(File.ReadAllText(path));
				JObject val2 = (JObject)data;
				bool flag = false;
				for (int i = 0; i < ((JContainer)val).Count; i++)
				{
					JToken val3 = val[i];
					if (!((string)val3[(object)"Id"] != (string)val2["Id"]))
					{
						val3[(object)i] = (JToken)(object)val2;
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					val.Add((JToken)(object)val2);
				}
				File.WriteAllText(path, ((object)val).ToString());
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to crate asset {0}", new object[1] { assetReference });
				callback?.Invoke(FormattedMessage.FromMessageId("ui.assetEditor.errors.failedToCreateAsset"));
				AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.errors.failedToCreateAsset"));
				return;
			}
		}
		else
		{
			try
			{
				string fullPath = Path.GetFullPath(Path.Combine(_assetsDirectoryPath, assetReference.FilePath));
				string fullPath2 = Path.GetFullPath(_assetsDirectoryPath);
				if (!Paths.IsSubPathOf(fullPath, fullPath2))
				{
					throw new Exception("Tried saving asset file outside of asset directory at " + fullPath);
				}
				JObject val4 = (JObject)((data is JObject) ? data : null);
				if (val4 == null)
				{
					if (!(data is string contents))
					{
						if (data is Image image)
						{
							image.SavePNG(fullPath);
						}
					}
					else
					{
						File.WriteAllText(fullPath, contents);
					}
				}
				else
				{
					File.WriteAllText(fullPath, ((object)val4).ToString());
				}
			}
			catch (Exception ex2)
			{
				Logger.Error(ex2, "Failed to create asset {0}", new object[1] { assetReference });
				callback?.Invoke(FormattedMessage.FromMessageId("ui.assetEditor.errors.failedToCreateAsset"));
				AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.errors.failedToCreateAsset"));
				return;
			}
		}
		callback?.Invoke(null);
		AssetEditorOverlay.OnAssetAdded(assetReference, openInEditor: false);
		if (openInTab)
		{
			AssetEditorOverlay.OpenCreatedAsset(assetReference, data);
		}
		JObject val5 = (JObject)((data is JObject) ? data : null);
		if (val5 != null)
		{
			ValidateJsonAsset(assetReference, val5);
		}
		if (assetReference.Type == "ItemCategory")
		{
			_dropdownDatasetEntriesCache.Remove("ItemCategories");
		}
	}

	public override void DeleteAsset(AssetReference assetReference, bool applyLocally)
	{
		Logger.Info<string, string>("Deleting asset of type {0} in {1}", assetReference.Type, assetReference.FilePath);
		_unsavedJsonAssets.Remove(assetReference.FilePath);
		AssetTypeConfig assetTypeConfig = AssetEditorOverlay.AssetTypeRegistry.AssetTypes[assetReference.Type];
		if (assetTypeConfig.AssetTree == AssetTreeFolder.Cosmetics)
		{
			try
			{
				string assetIdFromReference = AssetEditorOverlay.GetAssetIdFromReference(assetReference);
				string path = Path.Combine(_assetsDirectoryPath, assetTypeConfig.Path);
				JArray val = JArray.Parse(File.ReadAllText(path));
				for (int i = 0; i < ((JContainer)val).Count; i++)
				{
					JToken val2 = val[i];
					if (!((string)val2[(object)"Id"] != assetIdFromReference))
					{
						val.RemoveAt(i);
						break;
					}
				}
				File.WriteAllText(path, ((object)val).ToString());
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to delete asset {0}", new object[1] { assetReference });
				AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.errors.failedToDeleteAsset"));
				return;
			}
		}
		else
		{
			try
			{
				string fullPath = Path.GetFullPath(Path.Combine(_assetsDirectoryPath, assetReference.FilePath));
				string fullPath2 = Path.GetFullPath(_assetsDirectoryPath);
				if (!Paths.IsSubPathOf(fullPath, fullPath2))
				{
					throw new Exception("Tried removing asset file outside of asset directory at " + fullPath);
				}
				File.Delete(fullPath);
			}
			catch (Exception ex2)
			{
				Logger.Error(ex2, "Failed to delete asset {0}", new object[1] { assetReference });
				AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.errors.failedToDeleteAsset"));
				return;
			}
		}
		AssetEditorOverlay.OnAssetDeleted(assetReference);
		string type = assetReference.Type;
		string text = type;
		if (text == "ItemCategory")
		{
			_dropdownDatasetEntriesCache.Remove("ItemCategories");
		}
		AssetEditorOverlay.OnDiagnosticsUpdated(new Dictionary<string, AssetDiagnostics> { 
		{
			assetReference.FilePath,
			AssetDiagnostics.None
		} });
	}

	public override void RenameAsset(AssetReference assetReference, string newFilePath, bool applyLocally)
	{
		Logger.Info<string, string, string>("Renaming asset {1} -> {2} ({0})", assetReference.Type, assetReference.FilePath, newFilePath);
		string assetIdFromReference = AssetEditorOverlay.GetAssetIdFromReference(assetReference);
		string assetIdFromReference2 = AssetEditorOverlay.GetAssetIdFromReference(new AssetReference(assetReference.Type, newFilePath));
		AssetTypeConfig assetTypeConfig = AssetEditorOverlay.AssetTypeRegistry.AssetTypes[assetReference.Type];
		SaveUnsavedChangesInternal(ignoreErrors: true);
		if (assetTypeConfig.AssetTree == AssetTreeFolder.Cosmetics)
		{
			string text = Path.Combine(_assetsDirectoryPath, assetTypeConfig.Path);
			try
			{
				JArray val = JArray.Parse(File.ReadAllText(text));
				foreach (JToken item in val)
				{
					if ((string)item[(object)"Id"] != assetIdFromReference)
					{
						continue;
					}
					item[(object)"Id"] = JToken.op_Implicit(assetIdFromReference2);
					break;
				}
				File.WriteAllText(text, ((object)val).ToString());
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to rename asset from {0} to {1} in {2}", new object[3] { assetIdFromReference, assetIdFromReference2, text });
				AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.errors.failedToRenameAsset"));
				return;
			}
		}
		else
		{
			string fullPath = Path.GetFullPath(Path.Combine(_assetsDirectoryPath, assetReference.FilePath));
			string fullPath2 = Path.GetFullPath(Path.Combine(_assetsDirectoryPath, newFilePath));
			string fullPath3 = Path.GetFullPath(_assetsDirectoryPath);
			try
			{
				if (!Paths.IsSubPathOf(fullPath, fullPath3))
				{
					Logger.Warn("Tried moving asset file from folder outside of asset directory at " + fullPath);
					AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.errors.failedToRenameAsset"));
					return;
				}
				if (!Paths.IsSubPathOf(fullPath2, fullPath3))
				{
					Logger.Warn("Tried moving asset file to folder outside of asset directory at " + fullPath);
					AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.errors.failedToRenameAsset"));
					return;
				}
				File.Move(fullPath, fullPath2);
			}
			catch (Exception ex2)
			{
				Logger.Error(ex2, "Failed to move file from {0} to {1}", new object[2] { fullPath, fullPath2 });
				AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.errors.failedToRenameAsset"));
				return;
			}
		}
		AssetEditorOverlay.OnAssetRenamed(assetReference, new AssetReference(assetReference.Type, newFilePath));
		if (assetReference.Type == "ItemCategory")
		{
			_dropdownDatasetEntriesCache.Remove("ItemCategories");
		}
	}

	public void SaveImage(string path, Image image)
	{
		image.SavePNG(Path.GetFullPath(Path.Combine(_assetsDirectoryPath, path)));
	}

	private void SaveUnsavedChangesInternal(bool ignoreErrors)
	{
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Expected O, but got Unknown
		bool flag = false;
		if (!ignoreErrors)
		{
			foreach (KeyValuePair<string, JObject> unsavedJsonAsset in _unsavedJsonAssets)
			{
				if (AssetEditorOverlay.Diagnostics.TryGetValue(unsavedJsonAsset.Key, out var value) && value.Errors != null && value.Errors.Length != 0)
				{
					AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.messages.assetExport.errors"));
					return;
				}
			}
		}
		int num = 0;
		Dictionary<string, JToken> dictionary = new Dictionary<string, JToken>();
		Dictionary<string, JToken> dictionary2 = new Dictionary<string, JToken>();
		Dictionary<string, string> dictionary3 = new Dictionary<string, string>();
		Dictionary<string, Image> dictionary4 = new Dictionary<string, Image>();
		foreach (KeyValuePair<string, JObject> unsavedJsonAsset2 in _unsavedJsonAssets)
		{
			if (!AssetEditorOverlay.AssetTypeRegistry.TryGetAssetTypeFromPath(unsavedJsonAsset2.Key, out var assetType))
			{
				continue;
			}
			AssetTypeConfig assetTypeConfig = AssetEditorOverlay.AssetTypeRegistry.AssetTypes[assetType];
			if (assetTypeConfig.AssetTree == AssetTreeFolder.Cosmetics)
			{
				flag = true;
			}
			if (assetTypeConfig.AssetTree == AssetTreeFolder.Cosmetics)
			{
				JArray val;
				if (!dictionary2.TryGetValue(assetTypeConfig.Path, out var value2))
				{
					string text = File.ReadAllText(Path.Combine(_assetsDirectoryPath, AssetEditorOverlay.AssetTypeRegistry.AssetTypes[assetType].Path));
					val = JArray.Parse(text);
					dictionary2[assetTypeConfig.Path] = (JToken)(object)val;
				}
				else
				{
					val = (JArray)value2;
				}
				string assetIdFromReference = AssetEditorOverlay.GetAssetIdFromReference(new AssetReference(assetType, unsavedJsonAsset2.Key));
				for (int i = 0; i < ((JContainer)val).Count; i++)
				{
					JToken val2 = val[i];
					if (!((string)val2[(object)"Id"] != assetIdFromReference))
					{
						val[i] = (JToken)(object)unsavedJsonAsset2.Value;
						num++;
						break;
					}
				}
			}
			else
			{
				string fullPath = Path.GetFullPath(Path.Combine(_assetsDirectoryPath, unsavedJsonAsset2.Key));
				string fullPath2 = Path.GetFullPath(_assetsDirectoryPath);
				if (!Paths.IsSubPathOf(fullPath, fullPath2))
				{
					Logger.Warn("Tried saving asset file to folder outside of asset directory at " + fullPath);
					AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.messages.assetExport.failedSaving"));
					return;
				}
				dictionary[unsavedJsonAsset2.Key] = (JToken)(object)unsavedJsonAsset2.Value;
				num++;
			}
		}
		_unsavedJsonAssets.Clear();
		foreach (KeyValuePair<string, Image> unsavedImageAsset in _unsavedImageAssets)
		{
			string fullPath3 = Path.GetFullPath(Path.Combine(_assetsDirectoryPath, unsavedImageAsset.Key));
			string fullPath4 = Path.GetFullPath(_assetsDirectoryPath);
			if (!Paths.IsSubPathOf(fullPath3, fullPath4))
			{
				Logger.Warn("Tried saving asset file to folder outside of asset directory at " + fullPath3);
				AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.messages.assetExport.failedSaving"));
				return;
			}
			dictionary4[unsavedImageAsset.Key] = unsavedImageAsset.Value;
			num++;
		}
		_unsavedImageAssets.Clear();
		foreach (KeyValuePair<string, string> unsavedTextAsset in _unsavedTextAssets)
		{
			string fullPath5 = Path.GetFullPath(Path.Combine(_assetsDirectoryPath, unsavedTextAsset.Key));
			string fullPath6 = Path.GetFullPath(_assetsDirectoryPath);
			if (!Paths.IsSubPathOf(fullPath5, fullPath6))
			{
				Logger.Warn("Tried saving asset file to folder outside of asset directory at " + fullPath5);
				AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.messages.assetExport.failedSaving"));
				return;
			}
			dictionary3[unsavedTextAsset.Key] = unsavedTextAsset.Value;
			num++;
		}
		_unsavedTextAssets.Clear();
		if (num == 0)
		{
			AssetEditorOverlay.SetFileSaveStatus(AssetEditorOverlay.SaveStatus.Saved);
			return;
		}
		foreach (KeyValuePair<string, JToken> item in dictionary)
		{
			Logger.Info("{0} has changes. Saving...", item.Key);
			string s = JsonConvert.SerializeObject((object)item.Value, (Formatting)1);
			byte[] bytes = Encoding.UTF8.GetBytes(s);
			if (_undoRedoStacks.TryGetValue(item.Key, out var value3))
			{
				value3.SaveFileHash = AssetManager.ComputeHash(bytes);
			}
			File.WriteAllBytes(Path.GetFullPath(Path.Combine(_assetsDirectoryPath, item.Key)), bytes);
		}
		foreach (KeyValuePair<string, JToken> item2 in dictionary2)
		{
			Logger.Info("{0} has changes. Saving...", item2.Key);
			string s2 = JsonConvert.SerializeObject((object)item2.Value, (Formatting)1);
			byte[] bytes2 = Encoding.UTF8.GetBytes(s2);
			string text2 = null;
			foreach (KeyValuePair<string, AssetUndoRedoStacks> undoRedoStack in _undoRedoStacks)
			{
				if (undoRedoStack.Key.StartsWith(item2.Key + "#"))
				{
					if (text2 == null)
					{
						text2 = AssetManager.ComputeHash(bytes2);
					}
					undoRedoStack.Value.SaveFileHash = text2;
				}
			}
			File.WriteAllBytes(Path.GetFullPath(Path.Combine(_assetsDirectoryPath, item2.Key)), bytes2);
		}
		foreach (KeyValuePair<string, string> item3 in dictionary3)
		{
			Logger.Info("{0} has changes. Saving...", item3.Key);
			File.WriteAllText(Path.GetFullPath(Path.Combine(_assetsDirectoryPath, item3.Key)), item3.Value);
		}
		foreach (KeyValuePair<string, Image> item4 in dictionary4)
		{
			Logger.Info("{0} has changes. Saving...", item4.Key);
			SaveImage(item4.Key, item4.Value);
		}
		AssetEditorOverlay.SetFileSaveStatus(AssetEditorOverlay.SaveStatus.Saved);
		AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)1, AssetEditorOverlay.Interface.GetText("ui.assetEditor.messages.assetExport.success", new Dictionary<string, string> { 
		{
			"count",
			AssetEditorOverlay.Interface.FormatNumber(num)
		} }));
	}

	private void ValidateAllCosmeticAssets(CancellationToken cancellationToken)
	{
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Expected O, but got Unknown
		Debug.Assert(!ThreadHelper.IsMainThread());
		Dictionary<string, AssetDiagnostics> diagnosticsMapping = new Dictionary<string, AssetDiagnostics>();
		foreach (KeyValuePair<string, AssetTypeConfig> assetType in AssetEditorOverlay.AssetTypeRegistry.AssetTypes)
		{
			if (assetType.Value.AssetTree != AssetTreeFolder.Cosmetics)
			{
				continue;
			}
			string text = File.ReadAllText(Path.GetFullPath(Path.Combine(_assetsDirectoryPath, assetType.Value.Path)));
			JArray val = JArray.Parse(text);
			foreach (JToken item in val)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					break;
				}
				string text2 = (string)item[(object)"Id"];
				AssetDiagnostics assetDiagnostics = GetAssetDiagnostics(new AssetReference(assetType.Key, text2), (JObject)item);
				diagnosticsMapping[assetType.Value.Path + "#" + text2] = assetDiagnostics;
			}
		}
		AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
		{
			AssetEditorOverlay.OnDiagnosticsUpdated(diagnosticsMapping);
		});
	}

	private void InitializeAssetMap(Dictionary<string, AssetTypeConfig> assetTypes)
	{
		List<AssetFile> cosmeticAssetFiles = new List<AssetFile>();
		List<AssetFile> commonAssetFiles = new List<AssetFile>();
		List<AssetFile> serverAssetFiles = new List<AssetFile>();
		Stopwatch stopWatch = Stopwatch.StartNew();
		List<KeyValuePair<string, AssetTypeConfig>> sortedAssetTypes = assetTypes.ToList();
		sortedAssetTypes.Sort((KeyValuePair<string, AssetTypeConfig> a, KeyValuePair<string, AssetTypeConfig> b) => string.Compare(a.Value.Path, b.Value.Path, StringComparison.InvariantCulture));
		Task task2 = Task.Run(delegate
		{
			cosmeticAssetFiles = LoadAssetFiles(sortedAssetTypes, AssetTreeFolder.Cosmetics, "Cosmetics/CharacterCreator");
		});
		Task task3 = Task.Run(delegate
		{
			LoadCommonAssets(commonAssetFiles);
		});
		Task.WhenAll(task3, task2).ContinueWith(delegate(Task task)
		{
			CancellationToken backendLifetimeCancellationToken = _backendLifetimeCancellationToken;
			if (!backendLifetimeCancellationToken.IsCancellationRequested)
			{
				stopWatch.Stop();
				if (task.IsFaulted)
				{
					Logger.Error((Exception)task.Exception, "Failed to initialize asset list");
					AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
					{
						CancellationToken backendLifetimeCancellationToken3 = _backendLifetimeCancellationToken;
						if (!backendLifetimeCancellationToken3.IsCancellationRequested)
						{
							AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.errors.failedToLoadAssetFiles"));
							AssetEditorOverlay.SetupAssetFiles(new List<AssetFile>(), new List<AssetFile>(), new List<AssetFile>());
						}
					});
				}
				else
				{
					Logger.Info("Initialized asset list in {0}", stopWatch.Elapsed.TotalMilliseconds / 1000.0);
					AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
					{
						CancellationToken backendLifetimeCancellationToken2 = _backendLifetimeCancellationToken;
						if (!backendLifetimeCancellationToken2.IsCancellationRequested)
						{
							AssetEditorOverlay.SetupAssetFiles(serverAssetFiles, commonAssetFiles, cosmeticAssetFiles);
						}
					});
				}
			}
		}, _threadCancellationToken);
	}

	public override void DeleteDirectory(string path, bool applyLocally, Action<string, FormattedMessage> callback)
	{
		string fullPath = Path.GetFullPath(_assetsDirectoryPath);
		string fullPath2 = Path.GetFullPath(Path.Combine(_assetsDirectoryPath, path));
		if (!Paths.IsSubPathOf(fullPath2, fullPath))
		{
			ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.deleteDirectoryInvalidPath"));
			return;
		}
		if (!Directory.Exists(fullPath2))
		{
			ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.deleteDirectoryMissing"));
			return;
		}
		try
		{
			Directory.Delete(fullPath2, recursive: true);
			Logger.Info("Deleted directory {0}", fullPath2);
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to create directory " + fullPath2);
			ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.deleteDirectoryOther"));
			return;
		}
		AssetEditorOverlay.OnDirectoryDeleted(path);
		ProcessSuccessCallback(callback, path);
	}

	public override void RenameDirectory(string path, string newPath, bool applyLocally, Action<string, FormattedMessage> callback)
	{
		string fullPath = Path.GetFullPath(_assetsDirectoryPath);
		string fullPath2 = Path.GetFullPath(Path.Combine(_assetsDirectoryPath, path));
		string fullPath3 = Path.GetFullPath(Path.Combine(_assetsDirectoryPath, newPath));
		if (!Paths.IsSubPathOf(fullPath2, fullPath) || !Paths.IsSubPathOf(fullPath3, fullPath))
		{
			ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.renameDirectoryInvalidPath"));
			return;
		}
		if (Directory.Exists(fullPath3))
		{
			ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.renameDirectoryExists"));
			return;
		}
		if (!Directory.Exists(fullPath2))
		{
			ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.renameDirectoryMissing"));
			return;
		}
		try
		{
			Directory.Move(fullPath2, fullPath3);
			Logger.Info<string, string>("Moved directory {0} to {1}", fullPath2, fullPath3);
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to create directory " + fullPath2);
			ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.renameDirectoryOther"));
			return;
		}
		AssetEditorOverlay.OnDirectoryRenamed(path, newPath);
		ProcessSuccessCallback(callback, path);
	}

	public override void CreateDirectory(string path, bool applyLocally, Action<string, FormattedMessage> callback)
	{
		string fullPath = Path.GetFullPath(_assetsDirectoryPath);
		string fullPath2 = Path.GetFullPath(Path.Combine(_assetsDirectoryPath, path));
		if (!Paths.IsSubPathOf(fullPath2, fullPath))
		{
			ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.createDirectoryInvalidPath"));
			return;
		}
		if (Directory.Exists(fullPath2))
		{
			ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.createDirectoryExists"));
			return;
		}
		try
		{
			Directory.CreateDirectory(fullPath2);
			Logger.Info("Created directory " + fullPath2);
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to create directory " + fullPath2);
			ProcessErrorCallback(callback, FormattedMessage.FromMessageId("ui.assetEditor.errors.createDirectoryOther"));
			return;
		}
		AssetEditorOverlay.OnDirectoryCreated(path);
		ProcessSuccessCallback(callback, path);
	}

	private void InitializeCommonAssetFileWatcher()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		CreateFileWatcher(Path.Combine(_assetsDirectoryPath, "Common"));
		CreateFileWatcher(Path.Combine(_assetsDirectoryPath, "Server"));
		_threadCancellationToken = _threadCancellationTokenSource.Token;
		_fileWatcherHandlerThread = new Thread((ThreadStart)delegate
		{
			ProcessFileWatcherEventQueue(_threadCancellationToken);
		});
		_fileWatcherHandlerThread.IsBackground = true;
		_fileWatcherHandlerThread.Name = "FileWatcherEventQueueHandler";
		_fileWatcherHandlerThread.Start();
		void CreateFileWatcher(string path)
		{
			if (!Directory.Exists(path))
			{
				Logger.Warn("Skipping file monitor creation for {0} due to missing directory", path);
			}
			else
			{
				FileSystemWatcher fileSystemWatcher = new FileSystemWatcher
				{
					Path = path,
					IncludeSubdirectories = true,
					NotifyFilter = (NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.LastAccess)
				};
				fileSystemWatcher.Created += delegate(object _, FileSystemEventArgs args)
				{
					_fileWatcherEventQueue.Enqueue(args);
				};
				fileSystemWatcher.Deleted += delegate(object _, FileSystemEventArgs args)
				{
					_fileWatcherEventQueue.Enqueue(args);
				};
				fileSystemWatcher.Renamed += delegate(object _, RenamedEventArgs args)
				{
					_fileWatcherEventQueue.Enqueue(args);
				};
				fileSystemWatcher.EnableRaisingEvents = true;
				_fileSystemWatchers.Add(fileSystemWatcher);
			}
		}
	}

	private void ProcessFileWatcherEventQueue(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			if (_fileWatcherEventQueue.TryDequeue(out var result))
			{
				switch (result.ChangeType)
				{
				case WatcherChangeTypes.Created:
					OnAssetFileCreated(result, cancellationToken);
					break;
				case WatcherChangeTypes.Deleted:
					OnAssetFileDeleted(result, cancellationToken);
					break;
				case WatcherChangeTypes.Renamed:
					OnAssetFileRenamed((RenamedEventArgs)result, cancellationToken);
					break;
				}
			}
		}
	}

	private void OnAssetFileCreated(FileSystemEventArgs args, CancellationToken cancellationToken)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		bool isDirectory = Directory.Exists(args.FullPath);
		string text = UnixPathUtil.ConvertToUnixPath(Path.GetFullPath(_assetsDirectoryPath));
		string path = UnixPathUtil.ConvertToUnixPath(args.FullPath).Substring(text.Length).TrimStart(new char[1] { '/' })
			.Normalize(NormalizationForm.FormC);
		try
		{
			if ((File.GetAttributes(args.FullPath) & FileAttributes.Hidden) == FileAttributes.Hidden)
			{
				return;
			}
		}
		catch (IOException)
		{
			return;
		}
		AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				Logger.Info("File '{0}' has been created", path);
				string text2 = Path.GetDirectoryName(path).Replace(Path.DirectorySeparatorChar, '/');
				if ((!(text2 != "Common") || AssetEditorOverlay.Assets.TryGetAsset(text2, out var assetFile)) && !AssetEditorOverlay.Assets.TryGetAsset(path.Replace(Path.DirectorySeparatorChar, '/'), out assetFile) && HasPathCompatibleAssetType(path))
				{
					string assetType;
					if (isDirectory)
					{
						AssetEditorOverlay.OnDirectoryCreated(path);
						LoadAssetsInDirectory(path);
					}
					else if (AssetEditorOverlay.AssetTypeRegistry.TryGetAssetTypeFromPath(path, out assetType))
					{
						AssetEditorOverlay.OnAssetAdded(new AssetReference(assetType, path), openInEditor: false);
					}
				}
			}
		});
	}

	private void OnAssetFileDeleted(FileSystemEventArgs args, CancellationToken cancellationToken)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		string text = UnixPathUtil.ConvertToUnixPath(Path.GetFullPath(_assetsDirectoryPath));
		string path = UnixPathUtil.ConvertToUnixPath(args.FullPath).Substring(text.Length).TrimStart(new char[1] { '/' })
			.Normalize(NormalizationForm.FormC);
		AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				Logger.Info("File '{0}' has been deleted", path);
				if (AssetEditorOverlay.Assets.TryGetAsset(path, out var assetFile) && HasPathCompatibleAssetType(path))
				{
					string assetType;
					if (assetFile.IsDirectory)
					{
						AssetEditorOverlay.OnDirectoryDeleted(path);
					}
					else if (AssetEditorOverlay.AssetTypeRegistry.TryGetAssetTypeFromPath(path, out assetType))
					{
						AssetEditorOverlay.OnAssetDeleted(new AssetReference(assetType, path));
					}
				}
			}
		});
	}

	private void OnAssetFileRenamed(RenamedEventArgs args, CancellationToken cancellationToken)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		string text = UnixPathUtil.ConvertToUnixPath(Path.GetFullPath(_assetsDirectoryPath));
		string pathOld = UnixPathUtil.ConvertToUnixPath(args.OldFullPath).Substring(text.Length).TrimStart(new char[1] { '/' })
			.Normalize(NormalizationForm.FormC);
		string pathNew = UnixPathUtil.ConvertToUnixPath(args.FullPath).Substring(text.Length).TrimStart(new char[1] { '/' })
			.Normalize(NormalizationForm.FormC);
		bool existsOnFileSystem = true;
		bool isHidden = false;
		bool isDirectory = Directory.Exists(args.FullPath);
		try
		{
			isHidden = (File.GetAttributes(args.FullPath) & FileAttributes.Hidden) == FileAttributes.Hidden;
		}
		catch (FileNotFoundException)
		{
			existsOnFileSystem = false;
		}
		catch (IOException)
		{
		}
		AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				string path = Path.GetDirectoryName(pathNew).Replace(Path.DirectorySeparatorChar, '/');
				if (AssetPathUtils.IsAssetTreeRootDirectory(path) || AssetEditorOverlay.Assets.TryGetDirectory(path, out var assetFile))
				{
					if (isDirectory)
					{
						Logger.Info<string, string>("Directory '{0}' has been renamed to '{1}'", pathOld, pathNew);
						bool flag = HasPathCompatibleAssetType(pathOld);
						bool flag2 = HasPathCompatibleAssetType(pathNew);
						if (flag || flag2)
						{
							bool flag3 = AssetEditorOverlay.Assets.TryGetDirectory(pathOld, out assetFile);
							bool flag4 = AssetEditorOverlay.Assets.TryGetAsset(pathNew, out assetFile);
							if (flag3 && !flag4 && flag2 && !isHidden && existsOnFileSystem)
							{
								AssetEditorOverlay.OnDirectoryRenamed(pathOld, pathNew);
							}
							else
							{
								if (flag3)
								{
									AssetEditorOverlay.OnDirectoryDeleted(pathOld);
								}
								if (!flag4 && flag2 && !isHidden && existsOnFileSystem)
								{
									AssetEditorOverlay.OnDirectoryCreated(pathNew);
									LoadAssetsInDirectory(pathNew);
								}
							}
						}
					}
					else
					{
						Logger.Info<string, string>("File '{0}' has been renamed to '{1}'", pathOld, pathNew);
						AssetEditorOverlay.AssetTypeRegistry.TryGetAssetTypeFromPath(pathOld, out var assetType);
						AssetEditorOverlay.AssetTypeRegistry.TryGetAssetTypeFromPath(pathNew, out var assetType2);
						if (assetType != null || assetType2 != null)
						{
							bool flag5 = AssetEditorOverlay.Assets.TryGetFile(pathOld, out assetFile);
							bool flag6 = AssetEditorOverlay.Assets.TryGetAsset(pathNew, out assetFile);
							if (assetType2 == assetType && flag5 && !flag6 && !isHidden && existsOnFileSystem)
							{
								AssetEditorOverlay.OnAssetRenamed(new AssetReference(assetType, pathOld), new AssetReference(assetType2, pathNew));
							}
							else
							{
								if (flag5 && assetType != null)
								{
									AssetEditorOverlay.OnAssetDeleted(new AssetReference(assetType, pathOld));
								}
								if (!flag6 && assetType2 != null && !isHidden && existsOnFileSystem)
								{
									AssetEditorOverlay.OnAssetAdded(new AssetReference(assetType2, pathNew), openInEditor: false);
								}
							}
						}
					}
				}
			}
		});
	}

	private bool HasPathCompatibleAssetType(string path)
	{
		if (path.StartsWith("Common/"))
		{
			return true;
		}
		foreach (AssetTypeConfig value in AssetEditorOverlay.AssetTypeRegistry.AssetTypes.Values)
		{
			if (!value.IsVirtual && value.AssetTree != AssetTreeFolder.Cosmetics && path.StartsWith(value.Path + "/"))
			{
				return true;
			}
		}
		return false;
	}

	private void LoadAssetFiles(string basePath, Dictionary<string, AssetTypeConfig> compatibleAssetTypes, List<AssetFile> assetFiles, out int countAdded)
	{
		string builtInAssetsPath = Path.GetFullPath(_assetsDirectoryPath).Replace(Path.DirectorySeparatorChar, '/');
		int assetCount = 0;
		if (Directory.Exists(Path.Combine(_assetsDirectoryPath, basePath)))
		{
			Walk(Path.Combine(_assetsDirectoryPath, basePath));
		}
		countAdded = assetCount;
		void Walk(string path)
		{
			string[] files = Directory.GetFiles(path);
			foreach (string text in files)
			{
				CancellationToken backendLifetimeCancellationToken = _backendLifetimeCancellationToken;
				if (backendLifetimeCancellationToken.IsCancellationRequested)
				{
					return;
				}
				string text2 = text.Replace(Path.DirectorySeparatorChar, '/').Replace(builtInAssetsPath + "/", "").Normalize(NormalizationForm.FormC);
				string extension = Path.GetExtension(text);
				if (compatibleAssetTypes.TryGetValue(extension, out var value))
				{
					assetFiles.Add(AssetFile.CreateFile(Path.GetFileNameWithoutExtension(text2), text2, value.Id, text2.Split(new char[1] { '/' })));
					int num = assetCount + 1;
					assetCount = num;
				}
			}
			string[] directories = Directory.GetDirectories(path);
			foreach (string text3 in directories)
			{
				string text4 = text3.Replace(Path.DirectorySeparatorChar, '/').Replace(builtInAssetsPath + "/", "").Normalize(NormalizationForm.FormC);
				assetFiles.Add(AssetFile.CreateDirectory(Path.GetFileName(text4).Normalize(NormalizationForm.FormC), text4, text4.Split(new char[1] { '/' })));
				int num = assetCount + 1;
				assetCount = num;
				Walk(text3);
			}
		}
	}

	private List<AssetFile> LoadAssetFiles(List<KeyValuePair<string, AssetTypeConfig>> assetTypes, AssetTreeFolder assetTree, string basePath)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		List<AssetFile> list = new List<AssetFile>();
		string[] array = (basePath + "/\\").Split(new char[1] { '/' });
		Dictionary<string, Dictionary<string, AssetTypeConfig>> dictionary = new Dictionary<string, Dictionary<string, AssetTypeConfig>>();
		foreach (KeyValuePair<string, AssetTypeConfig> assetType in assetTypes)
		{
			if (!dictionary.TryGetValue(assetType.Value.Path, out var value))
			{
				Dictionary<string, AssetTypeConfig> dictionary3 = (dictionary[assetType.Value.Path] = new Dictionary<string, AssetTypeConfig>());
				value = dictionary3;
			}
			value[assetType.Value.FileExtension] = assetType.Value;
		}
		foreach (KeyValuePair<string, AssetTypeConfig> assetType2 in assetTypes)
		{
			if (assetType2.Value.AssetTree != assetTree)
			{
				continue;
			}
			if (assetType2.Value.AssetTree == AssetTreeFolder.Cosmetics)
			{
				string path = assetType2.Value.Path;
				if (!File.Exists(Path.Combine(_assetsDirectoryPath, path)))
				{
					continue;
				}
				string text = File.ReadAllText(Path.Combine(_assetsDirectoryPath, path));
				JArray val = JArray.Parse(text);
				list.Add(AssetFile.CreateAssetTypeDirectory(assetType2.Value.Name, path, assetType2.Key, path.Split(new char[1] { '/' })));
				int count = list.Count;
				int num = 0;
				foreach (JToken item in val)
				{
					string text2 = (string)item[(object)"Id"];
					string text3 = path + "#" + text2;
					list.Add(AssetFile.CreateFile(text2, text3, assetType2.Key, AssetPathUtils.GetAssetFilePathElements(text3, usesSharedAssetFile: true)));
					num++;
				}
				list.Sort(count, num, AssetFileComparer.Instance);
			}
			else
			{
				if (!dictionary.TryGetValue(assetType2.Value.Path, out var value2))
				{
					continue;
				}
				if (value2.Count > 1)
				{
					dictionary.Remove(assetType2.Value.Path);
				}
				string path2 = assetType2.Value.Path;
				string[] array2 = assetType2.Value.Path.Split(new char[1] { '/' });
				bool flag = false;
				for (int i = 0; i < array2.Length - 1; i++)
				{
					if (flag || i >= array.Length - 1 || array[i] != array2[i])
					{
						flag = true;
						string[] array3 = new string[i + 1];
						Array.Copy(array2, 0, array3, 0, i + 1);
						string path3 = string.Join("/", array3);
						list.Add(AssetFile.CreateDirectory(array2[i], path3, array3.ToArray()));
					}
				}
				array = array2;
				if (value2.Count == 1)
				{
					list.Add(AssetFile.CreateAssetTypeDirectory(assetType2.Value.Name, path2, assetType2.Key, path2.Split(new char[1] { '/' })));
				}
				else
				{
					list.Add(AssetFile.CreateDirectory(array2.Last(), path2, path2.Split(new char[1] { '/' })));
				}
				int count2 = list.Count;
				LoadAssetFiles(path2, value2, list, out var countAdded);
				list.Sort(count2, countAdded, AssetFileComparer.Instance);
			}
		}
		Logger.Info<AssetTreeFolder, double>("Loaded {0} assets in {1}s", assetTree, stopwatch.Elapsed.TotalMilliseconds / 1000.0);
		return list;
	}

	private void WalkCommonAssetDirectory(string path, string builtInAssetsPath, List<AssetFile> assetFiles)
	{
		CancellationToken backendLifetimeCancellationToken = _backendLifetimeCancellationToken;
		if (backendLifetimeCancellationToken.IsCancellationRequested)
		{
			return;
		}
		DirectoryInfo directoryInfo = new DirectoryInfo(path);
		DirectoryInfo[] directories = directoryInfo.GetDirectories("*.*", SearchOption.TopDirectoryOnly);
		DirectoryInfo[] array = directories;
		foreach (DirectoryInfo directoryInfo2 in array)
		{
			if ((directoryInfo2.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
			{
				string name = directoryInfo2.Name;
				string text = directoryInfo2.FullName.Replace(Path.DirectorySeparatorChar, '/').Replace(builtInAssetsPath, "").TrimStart(new char[1] { '/' })
					.Normalize(NormalizationForm.FormC);
				assetFiles.Add(AssetFile.CreateDirectory(name, text, text.Split(new char[1] { '/' })));
				WalkCommonAssetDirectory(directoryInfo2.FullName, builtInAssetsPath, assetFiles);
			}
		}
		FileInfo[] files = directoryInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly);
		FileInfo[] array2 = files;
		foreach (FileInfo fileInfo in array2)
		{
			if ((fileInfo.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden && _fileExtensionAssetTypeMapping.TryGetValue(fileInfo.Extension, out var value))
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileInfo.Name);
				string text2 = fileInfo.FullName.Replace(Path.DirectorySeparatorChar, '/').Replace(builtInAssetsPath, "").TrimStart(new char[1] { '/' })
					.Normalize(NormalizationForm.FormC);
				assetFiles.Add(AssetFile.CreateFile(fileNameWithoutExtension, text2, value, text2.Split(new char[1] { '/' })));
			}
		}
	}

	private void LoadCommonAssets(List<AssetFile> assetFiles)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		string path = Path.Combine(_assetsDirectoryPath, "Common").Replace(Path.DirectorySeparatorChar, '/');
		if (Directory.Exists(path))
		{
			string builtInAssetsPath = Path.GetFullPath(_assetsDirectoryPath).Replace(Path.DirectorySeparatorChar, '/');
			WalkCommonAssetDirectory(path, builtInAssetsPath, assetFiles);
		}
		else
		{
			Logger.Error("Common asset directory does not exist. Skipping...");
		}
		stopwatch.Stop();
		Logger.Info("Loaded common assets in {0}s", stopwatch.Elapsed.TotalMilliseconds / 1000.0);
		assetFiles.Sort(AssetFileComparer.Instance);
	}

	private void LoadAssetsInDirectory(string path)
	{
		List<AssetFile> assetFiles = new List<AssetFile>();
		IReadOnlyDictionary<string, AssetTypeConfig> assetTypes = AssetEditorOverlay.AssetTypeRegistry.AssetTypes;
		Task.Run(delegate
		{
			if (path.StartsWith("Common/"))
			{
				string path2 = Path.Combine(_assetsDirectoryPath, path).Replace(Path.DirectorySeparatorChar, '/');
				string builtInAssetsPath = Path.GetFullPath(_assetsDirectoryPath).Replace(Path.DirectorySeparatorChar, '/');
				WalkCommonAssetDirectory(path2, builtInAssetsPath, assetFiles);
			}
			else
			{
				Dictionary<string, AssetTypeConfig> dictionary = new Dictionary<string, AssetTypeConfig>();
				foreach (AssetTypeConfig value in assetTypes.Values)
				{
					if (path.StartsWith(value.Path + "/"))
					{
						dictionary[value.FileExtension] = value;
					}
				}
				LoadAssetFiles(path, dictionary, assetFiles, out var _);
			}
			if (assetFiles.Count != 0)
			{
				assetFiles.Sort(AssetFileComparer.Instance);
				AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
				{
					CancellationToken backendLifetimeCancellationToken = _backendLifetimeCancellationToken;
					if (!backendLifetimeCancellationToken.IsCancellationRequested)
					{
						AssetEditorOverlay.OnDirectoryContentsUpdated(path, assetFiles);
					}
				});
			}
		}).ContinueWith(delegate(Task t)
		{
			if (t.IsFaulted)
			{
				Logger.Error((Exception)t.Exception, "Failed to load assets in directory {0}", new object[1] { path });
			}
		});
	}

	public override void UndoChanges(AssetReference assetReference)
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Expected O, but got Unknown
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Invalid comparison between Unknown and I4
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Expected O, but got Unknown
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Invalid comparison between Unknown and I4
		if (!_undoRedoStacks.TryGetValue(assetReference.FilePath, out var value) || value.UndoStack.Count == 0)
		{
			AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)0, AssetEditorOverlay.Interface.GetText("ui.assetEditor.messages.undoStackEmpty"));
			return;
		}
		ClientJsonUpdateCommand clientJsonUpdateCommand = value.UndoStack.Pop();
		value.RedoStack.Push(clientJsonUpdateCommand);
		JObject val = (JObject)AssetEditorOverlay.TrackedAssets[assetReference.FilePath].Data;
		PropertyPath? firstCreatedProperty;
		if (clientJsonUpdateCommand.Path.Elements.Length == 0 && (int)clientJsonUpdateCommand.Type == 0)
		{
			JToken previousValue = clientJsonUpdateCommand.PreviousValue;
			val = (JObject)((previousValue != null) ? previousValue.DeepClone() : null);
			AssetEditorOverlay.SetTrackedAssetData(assetReference.FilePath, val);
		}
		else if (clientJsonUpdateCommand.FirstCreatedProperty.HasValue)
		{
			AssetEditorOverlay.ConfigEditor.RemoveProperty(val, clientJsonUpdateCommand.FirstCreatedProperty.Value, out firstCreatedProperty, out var _, updateDisplayedValue: true, cleanupEmptyContainers: false);
		}
		else
		{
			ConfigEditor configEditor = AssetEditorOverlay.ConfigEditor;
			JObject root = val;
			PropertyPath path = clientJsonUpdateCommand.Path;
			JToken previousValue2 = clientJsonUpdateCommand.PreviousValue;
			configEditor.SetProperty(root, path, (previousValue2 != null) ? previousValue2.DeepClone() : null, out firstCreatedProperty, updateDisplayedValue: true, (int)clientJsonUpdateCommand.Type == 2);
		}
		AssetEditorOverlay.Layout();
		UpdateJsonAsset(assetReference, val);
	}

	public override void RedoChanges(AssetReference assetReference)
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Expected O, but got Unknown
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Invalid comparison between Unknown and I4
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Invalid comparison between Unknown and I4
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Invalid comparison between Unknown and I4
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Expected O, but got Unknown
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Invalid comparison between Unknown and I4
		if (!_undoRedoStacks.TryGetValue(assetReference.FilePath, out var value) || value.RedoStack.Count == 0)
		{
			AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)0, AssetEditorOverlay.Interface.GetText("ui.assetEditor.messages.redoStackEmpty"));
			return;
		}
		ClientJsonUpdateCommand clientJsonUpdateCommand = value.RedoStack.Pop();
		value.UndoStack.Push(clientJsonUpdateCommand);
		JObject val = (JObject)AssetEditorOverlay.TrackedAssets[assetReference.FilePath].Data;
		if (clientJsonUpdateCommand.Path.Elements.Length == 0 && (int)clientJsonUpdateCommand.Type == 0)
		{
			JToken value2 = clientJsonUpdateCommand.Value;
			val = (JObject)((value2 != null) ? value2.DeepClone() : null);
			AssetEditorOverlay.SetTrackedAssetData(assetReference.FilePath, val);
		}
		else
		{
			JsonUpdateType type = clientJsonUpdateCommand.Type;
			JsonUpdateType val2 = type;
			PropertyPath? firstCreatedProperty;
			if ((int)val2 > 1)
			{
				if ((int)val2 == 2)
				{
					AssetEditorOverlay.ConfigEditor.RemoveProperty(val, clientJsonUpdateCommand.Path, out firstCreatedProperty, out var _, updateDisplayedValue: true, cleanupEmptyContainers: false);
				}
			}
			else
			{
				ConfigEditor configEditor = AssetEditorOverlay.ConfigEditor;
				JObject root = val;
				PropertyPath path = clientJsonUpdateCommand.Path;
				JToken value3 = clientJsonUpdateCommand.Value;
				configEditor.SetProperty(root, path, (value3 != null) ? value3.DeepClone() : null, out firstCreatedProperty, updateDisplayedValue: true, (int)clientJsonUpdateCommand.Type == 1);
			}
		}
		AssetEditorOverlay.Layout();
		UpdateJsonAsset(assetReference, val);
	}
}
