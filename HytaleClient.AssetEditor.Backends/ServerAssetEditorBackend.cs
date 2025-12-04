#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hypixel.ProtoPlus;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Config;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.AssetEditor.Networking;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Data.UserSettings;
using HytaleClient.Graphics;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Networking;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Backends;

internal class ServerAssetEditorBackend : AssetEditorBackend
{
	public enum AssetExportStatus
	{
		Pending,
		Complete,
		Failed
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private bool _isInitializing;

	private bool _isInitializingOrInitialized;

	private bool _areAssetTypesInitialized;

	private readonly CancellationTokenSource _backendLifetimeCancellationTokenSource = new CancellationTokenSource();

	private readonly CancellationToken _backendLifetimeCancellationToken;

	private Action<List<TimestampedAssetReference>> _exportCompleteCallback;

	private Dictionary<string, List<string>> _dropdownDatasetEntriesCache = new Dictionary<string, List<string>>();

	private List<string> _loadingDropdownDatasets = new List<string>();

	private List<Texture> _iconTextures = new List<Texture>();

	private List<AssetFile> _commonAssets;

	private List<AssetFile> _serverAssets;

	private AssetEditorFileEntry[] _serverAssetFileEntries;

	private AssetEditorFileEntry[] _commonAssetFileEntries;

	public readonly ConcurrentDictionary<string, AssetExportStatus> AssetExportStatuses = new ConcurrentDictionary<string, AssetExportStatus>();

	private AssetInfo[] _lastModifiedAssets = (AssetInfo[])(object)new AssetInfo[0];

	private SchemaFile[] schemaFilesToProcess;

	private CancellationTokenSource _currentAssetCancellationToken = new CancellationTokenSource();

	private string _localAssetsDirectoryPath;

	private readonly ConnectionToServer _connection;

	private readonly AssetEditorPacketHandler _packetHandler;

	public string LocalAssetsDirectoryPathForCurrentExport { get; private set; }

	public string Hostname => _connection.Hostname;

	public int Port => _connection.Port;

	public ServerAssetEditorBackend(AssetEditorOverlay assetEditorOverlay, ConnectionToServer connection, AssetEditorPacketHandler packetHandler)
		: base(assetEditorOverlay)
	{
		_connection = connection;
		_connection.OnDisconnected = OnDisconnected;
		_packetHandler = packetHandler;
		_backendLifetimeCancellationToken = _backendLifetimeCancellationTokenSource.Token;
		base.SupportedAssetTreeFolders = new AssetTreeFolder[2]
		{
			AssetTreeFolder.Server,
			AssetTreeFolder.Common
		};
		base.IsEditingRemotely = true;
	}

	protected override void DoDispose()
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected O, but got Unknown
		_exportCompleteCallback = null;
		_backendLifetimeCancellationTokenSource.Cancel();
		foreach (Texture iconTexture in _iconTextures)
		{
			iconTexture.Dispose();
		}
		_packetHandler.Dispose();
		_connection.OnDisconnected = null;
		_connection.SendPacketImmediate((ProtoPacket)new Disconnect("User leave", (DisconnectType)0));
		_connection.Close();
	}

	public override void Initialize()
	{
		Debug.Assert(!_isInitializingOrInitialized);
		if (!_isInitializingOrInitialized)
		{
			_isInitializingOrInitialized = true;
			_isInitializing = true;
			_localAssetsDirectoryPath = AssetEditorOverlay.Interface.App.Settings.AssetsPath;
		}
	}

	private void OnDisconnected(Exception exception)
	{
		if (!_backendLifetimeCancellationTokenSource.IsCancellationRequested)
		{
			Logger.Info("got disconnected from server!");
			if (exception != null)
			{
				Logger.Error<Exception>(exception);
			}
			AssetEditorOverlay.Interface.App.MainMenu.OpenWithDisconnectPopup(_connection.Hostname, _connection.Port);
		}
	}

	public void OnLocalAssetsDirectoryPathChanged()
	{
		_localAssetsDirectoryPath = AssetEditorOverlay.Interface.App.Settings.AssetsPath;
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

	public void SetupSchemas(SchemaFile[] schemaFiles)
	{
		schemaFilesToProcess = schemaFiles;
	}

	private bool TryParseVirtualAssetTypeFromSchema(JObject json, SchemaNode schema, IDictionary<string, string> translationMapping, out AssetTypeConfig assetTypeConfig)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		assetTypeConfig = null;
		JObject val = (JObject)json["hytale"];
		if (val == null)
		{
			return false;
		}
		if (!val.ContainsKey("virtualPath") && val.ContainsKey("uiEditorIgnore") && (bool)val["uiEditorIgnore"])
		{
			return false;
		}
		string text = Path.Combine(Paths.BuiltInAssets, "Server");
		string text2 = (string)val["virtualPath"];
		if (text2 == null || !Paths.IsSubPathOf(Path.Combine(text, text2), text))
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
			Schema = schema,
			Id = Path.GetFileNameWithoutExtension(schema.Id),
			Name = value,
			Path = AssetPathUtils.CombinePaths("Server", text2),
			AssetTree = AssetTreeFolder.Server,
			EditorType = (AssetEditorEditorType)3,
			FileExtension = fileExtension,
			IsVirtual = true
		};
		ApplySchemaMetadata(assetTypeConfig, val);
		return true;
	}

	public void SetupAssetTypes(AssetEditorAssetType[] networkAssetTypes)
	{
		Task.Run(delegate
		{
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Expected O, but got Unknown
			//IL_024c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0251: Unknown result type (might be due to invalid IL or missing references)
			//IL_025a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0260: Invalid comparison between Unknown and I4
			//IL_0291: Unknown result type (might be due to invalid IL or missing references)
			//IL_0298: Expected O, but got Unknown
			Dictionary<string, JObject> dictionary = new Dictionary<string, JObject>();
			Dictionary<string, SchemaNode> schemas = new Dictionary<string, SchemaNode>();
			Dictionary<string, AssetTypeConfig> assetTypes = new Dictionary<string, AssetTypeConfig>();
			IDictionary<string, string> dictionary2 = Language.LoadServerLanguageFile("assetTypes.lang", AssetEditorOverlay.Interface.App.Settings.Language);
			SchemaFile[] array = schemaFilesToProcess;
			foreach (SchemaFile val in array)
			{
				CancellationToken backendLifetimeCancellationToken2 = _backendLifetimeCancellationToken;
				if (backendLifetimeCancellationToken2.IsCancellationRequested)
				{
					return;
				}
				JObject val2 = (JObject)BsonHelper.FromBson(val.Content);
				SchemaNode schemaNode = LoadSchema(val2, schemas);
				dictionary[schemaNode.Id] = val2;
				if (TryParseVirtualAssetTypeFromSchema(val2, schemaNode, dictionary2, out var assetTypeConfig))
				{
					assetTypes.Add(assetTypeConfig.Id, assetTypeConfig);
				}
			}
			schemaFilesToProcess = null;
			AssetEditorAssetType[] array2 = networkAssetTypes;
			foreach (AssetEditorAssetType val3 in array2)
			{
				AssetTreeFolder assetTree;
				if (val3.Path.StartsWith("Server/"))
				{
					assetTree = AssetTreeFolder.Server;
				}
				else
				{
					if (!val3.Path.StartsWith("Common/") && !(val3.Path == "Common"))
					{
						throw new Exception("Path of asset type " + val3.Path + " must be either in Server/ or Common/ directory: " + val3.Path);
					}
					assetTree = AssetTreeFolder.Common;
				}
				if (!dictionary2.TryGetValue(val3.Id + ".title", out var value))
				{
					value = val3.Id;
				}
				AssetTypeConfig assetTypeConfig2 = new AssetTypeConfig
				{
					Name = value,
					Id = val3.Id,
					AssetTree = assetTree,
					IsColoredIcon = (val3.Icon != null && val3.IsColoredIcon),
					Icon = new PatchStyle("AssetEditor/AssetIcons/" + (val3.Icon ?? "File.png")),
					Path = val3.Path,
					FileExtension = val3.FileExtension,
					EditorType = val3.EditorType
				};
				if ((int)assetTypeConfig2.EditorType == 3 && dictionary.TryGetValue(assetTypeConfig2.Id + ".json", out var value2))
				{
					JObject val4 = (JObject)value2["hytale"];
					if (val4 != null)
					{
						assetTypeConfig2.Schema = schemas[assetTypeConfig2.Id + ".json"];
						ApplySchemaMetadata(assetTypeConfig2, val4);
					}
				}
				assetTypes.Add(val3.Id, assetTypeConfig2);
			}
			AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
			{
				CancellationToken backendLifetimeCancellationToken3 = _backendLifetimeCancellationToken;
				if (!backendLifetimeCancellationToken3.IsCancellationRequested)
				{
					foreach (AssetTypeConfig value3 in assetTypes.Values)
					{
						if (value3.IconImage != null)
						{
							Image iconImage = value3.IconImage;
							Texture texture = new Texture(Texture.TextureTypes.Texture2D);
							texture.CreateTexture2D(iconImage.Width, iconImage.Height, iconImage.Pixels);
							_iconTextures.Add(texture);
							value3.Icon = new PatchStyle(new TextureArea(texture, 0, 0, iconImage.Width, iconImage.Height, 1));
							value3.IconImage = null;
						}
					}
					AssetEditorOverlay.SetupAssetTypes(schemas, assetTypes);
					_areAssetTypesInitialized = true;
					if (_serverAssetFileEntries != null)
					{
						SetupAssetList((AssetEditorFileTree)0, _serverAssetFileEntries);
					}
					if (_commonAssetFileEntries != null)
					{
						SetupAssetList((AssetEditorFileTree)1, _commonAssetFileEntries);
					}
				}
			});
		}).ContinueWith(delegate(Task task)
		{
			if (task.IsFaulted)
			{
				Logger.Error((Exception)task.Exception, "Failed to initialize asset list");
				AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
				{
					CancellationToken backendLifetimeCancellationToken = _backendLifetimeCancellationToken;
					if (!backendLifetimeCancellationToken.IsCancellationRequested)
					{
						AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.errors.failedToSetupAssetTypes"));
						AssetEditorOverlay.SetupAssetFiles(new List<AssetFile>(), new List<AssetFile>(), new List<AssetFile>());
					}
				});
			}
		});
	}

	public void UpdateAssetList(AssetEditorFileEntry[] additions, AssetEditorFileEntry[] deletions)
	{
		Debug.Assert(!_isInitializing);
		if (deletions != null)
		{
			foreach (AssetEditorFileEntry val in deletions)
			{
				string assetType;
				if (val.IsDirectory)
				{
					AssetEditorOverlay.OnDirectoryDeleted(val.Path);
				}
				else if (AssetEditorOverlay.AssetTypeRegistry.TryGetAssetTypeFromPath(val.Path, out assetType))
				{
					AssetEditorOverlay.OnAssetDeleted(new AssetReference(assetType, val.Path));
				}
			}
		}
		if (additions == null)
		{
			return;
		}
		foreach (AssetEditorFileEntry val2 in additions)
		{
			if (!AssetEditorOverlay.Assets.TryGetAsset(val2.Path, out var _))
			{
				string assetType2;
				if (val2.IsDirectory)
				{
					AssetEditorOverlay.OnDirectoryCreated(val2.Path);
				}
				else if (AssetEditorOverlay.AssetTypeRegistry.TryGetAssetTypeFromPath(val2.Path, out assetType2))
				{
					AssetEditorOverlay.OnAssetAdded(new AssetReference(assetType2, val2.Path), openInEditor: false);
				}
			}
		}
	}

	public void SetupAssetList(AssetEditorFileTree fileTree, AssetEditorFileEntry[] assets)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Invalid comparison between Unknown and I4
		Debug.Assert(ThreadHelper.IsMainThread());
		AssetEditorFileTree val = fileTree;
		AssetEditorFileTree val2 = val;
		if ((int)val2 != 0)
		{
			if ((int)val2 == 1)
			{
				_commonAssetFileEntries = assets;
			}
		}
		else
		{
			_serverAssetFileEntries = assets;
		}
		if (!_areAssetTypesInitialized)
		{
			return;
		}
		Dictionary<string, Dictionary<string, AssetTypeConfig>> serverDirectoryMapping = new Dictionary<string, Dictionary<string, AssetTypeConfig>>();
		Dictionary<string, AssetTypeConfig> commonExtensionMapping = new Dictionary<string, AssetTypeConfig>();
		foreach (KeyValuePair<string, AssetTypeConfig> assetType in AssetEditorOverlay.AssetTypeRegistry.AssetTypes)
		{
			switch (assetType.Value.AssetTree)
			{
			case AssetTreeFolder.Common:
				commonExtensionMapping[assetType.Value.FileExtension] = assetType.Value;
				break;
			case AssetTreeFolder.Server:
			{
				if (!serverDirectoryMapping.TryGetValue(assetType.Value.Path, out var value))
				{
					Dictionary<string, AssetTypeConfig> dictionary2 = (serverDirectoryMapping[assetType.Value.Path] = new Dictionary<string, AssetTypeConfig>());
					value = dictionary2;
				}
				value[assetType.Value.FileExtension] = assetType.Value;
				break;
			}
			}
		}
		List<AssetFile> targetList = new List<AssetFile>(assets.Length);
		Task.Run(delegate
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Invalid comparison between Unknown and I4
			if ((int)fileTree == 0)
			{
				SetupServerAssetList(assets, serverDirectoryMapping, targetList);
			}
			else
			{
				SetupCommonAssetList(assets, commonExtensionMapping, targetList);
			}
			targetList.Sort(AssetFileComparer.Instance);
			AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
			{
				//IL_001e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0023: Unknown result type (might be due to invalid IL or missing references)
				//IL_0024: Unknown result type (might be due to invalid IL or missing references)
				//IL_0025: Unknown result type (might be due to invalid IL or missing references)
				//IL_0026: Unknown result type (might be due to invalid IL or missing references)
				//IL_002b: Unknown result type (might be due to invalid IL or missing references)
				//IL_002d: Invalid comparison between Unknown and I4
				CancellationToken backendLifetimeCancellationToken2 = _backendLifetimeCancellationToken;
				if (!backendLifetimeCancellationToken2.IsCancellationRequested)
				{
					AssetEditorFileTree val3 = fileTree;
					AssetEditorFileTree val4 = val3;
					if ((int)val4 != 0)
					{
						if ((int)val4 == 1)
						{
							_commonAssets = targetList;
						}
					}
					else
					{
						_serverAssets = targetList;
					}
					if (_commonAssets != null && _serverAssets != null)
					{
						_isInitializing = false;
						AssetEditorOverlay.SetupAssetFiles(_serverAssets, _commonAssets, new List<AssetFile>());
						_commonAssets = null;
						_serverAssets = null;
					}
				}
			});
		}).ContinueWith(delegate(Task task)
		{
			if (task.IsFaulted)
			{
				Logger.Error((Exception)task.Exception, "Failed to initialize asset list");
				AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
				{
					CancellationToken backendLifetimeCancellationToken = _backendLifetimeCancellationToken;
					if (!backendLifetimeCancellationToken.IsCancellationRequested)
					{
						AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.errors.failedToLoadAssetFiles"));
						AssetEditorOverlay.SetupAssetFiles(new List<AssetFile>(), new List<AssetFile>(), new List<AssetFile>());
					}
				});
			}
		});
	}

	private void SetupServerAssetList(AssetEditorFileEntry[] assets, Dictionary<string, Dictionary<string, AssetTypeConfig>> directoryMapping, List<AssetFile> list)
	{
		Dictionary<string, AssetTypeConfig> dictionary = null;
		int num = 0;
		foreach (AssetEditorFileEntry val in assets)
		{
			string path = val.Path;
			string[] array = path.Split(new char[1] { '/' });
			if (dictionary != null && array.Length <= num)
			{
				dictionary = null;
			}
			AssetTypeConfig value3;
			if (val.IsDirectory)
			{
				if (directoryMapping.TryGetValue(path, out var value))
				{
					dictionary = value;
					num = array.Length;
					if (value.Count == 1)
					{
						AssetTypeConfig value2 = value.First().Value;
						list.Add(AssetFile.CreateAssetTypeDirectory(value2.Name, path, value2.Id, array));
					}
					else
					{
						list.Add(AssetFile.CreateDirectory(UnixPathUtil.GetFileName(path), path, array));
					}
				}
				else
				{
					list.Add(AssetFile.CreateDirectory(UnixPathUtil.GetFileName(path), path, array));
				}
			}
			else if (dictionary != null && dictionary.TryGetValue(UnixPathUtil.GetExtension(path), out value3))
			{
				list.Add(AssetFile.CreateFile(UnixPathUtil.GetFileNameWithoutExtension(path), path, value3.Id, array));
			}
		}
	}

	private void SetupCommonAssetList(AssetEditorFileEntry[] assets, Dictionary<string, AssetTypeConfig> extensionMapping, List<AssetFile> list)
	{
		foreach (AssetEditorFileEntry val in assets)
		{
			string path = val.Path;
			string[] pathElements = path.Split(new char[1] { '/' });
			AssetTypeConfig value;
			if (val.IsDirectory)
			{
				list.Add(AssetFile.CreateDirectory(Path.GetFileNameWithoutExtension(path), path, pathElements));
			}
			else if (extensionMapping.TryGetValue(Path.GetExtension(path), out value))
			{
				list.Add(AssetFile.CreateFile(Path.GetFileNameWithoutExtension(path), path, value.Id, pathElements));
			}
		}
	}

	public override void CreateDirectory(string path, bool applyLocally, Action<string, FormattedMessage> callback)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		string localAssetsPath = _localAssetsDirectoryPath;
		int token = ((BasePacketHandler)_packetHandler).AddPendingCallback<SuccessReply>((Disposable)this, (Action<FailureReply, SuccessReply>)delegate(FailureReply err, SuccessReply reply)
		{
			AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
			{
				if (err == null)
				{
					AssetEditorOverlay.OnDirectoryCreated(path);
				}
				if (err?.Message != null)
				{
					ProcessErrorCallback(callback, BsonHelper.ObjectFromBson<FormattedMessage>(err.Message));
				}
				else
				{
					if (applyLocally)
					{
						CreateDirectoryLocally(path, localAssetsPath);
					}
					ProcessSuccessCallback(callback, path);
				}
			});
		});
		_connection.SendPacket((ProtoPacket)new AssetEditorCreateDirectory
		{
			Token = token,
			Path = path
		});
	}

	private void CreateDirectoryLocally(string path, string localAssetsPath)
	{
		try
		{
			string fullPath = Path.GetFullPath(Path.Combine(localAssetsPath, path));
			string fullPath2 = Path.GetFullPath(localAssetsPath);
			if (!Paths.IsSubPathOf(fullPath, fullPath2))
			{
				throw new Exception("Tried creating directory outside of asset directory at " + fullPath);
			}
			if (!Directory.Exists(fullPath))
			{
				Directory.CreateDirectory(fullPath);
			}
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to delete directory locally {0}", new object[1] { path });
		}
	}

	public override void DeleteDirectory(string path, bool applyLocally, Action<string, FormattedMessage> callback)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		string localAssetsPath = _localAssetsDirectoryPath;
		int token = ((BasePacketHandler)_packetHandler).AddPendingCallback<SuccessReply>((Disposable)this, (Action<FailureReply, SuccessReply>)delegate(FailureReply err, SuccessReply reply)
		{
			AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
			{
				if (err == null)
				{
					AssetEditorOverlay.OnDirectoryDeleted(path);
				}
				if (err?.Message != null)
				{
					ProcessErrorCallback(callback, BsonHelper.ObjectFromBson<FormattedMessage>(err.Message));
				}
				else
				{
					if (applyLocally)
					{
						DeleteDirectoryLocally(path, localAssetsPath);
					}
					ProcessSuccessCallback(callback, path);
				}
			});
		});
		_connection.SendPacket((ProtoPacket)new AssetEditorDeleteDirectory
		{
			Token = token,
			Path = path
		});
	}

	private void DeleteDirectoryLocally(string path, string localAssetsPath)
	{
		try
		{
			string fullPath = Path.GetFullPath(Path.Combine(localAssetsPath, path));
			string fullPath2 = Path.GetFullPath(localAssetsPath);
			if (!Paths.IsSubPathOf(fullPath, fullPath2))
			{
				throw new Exception("Tried removing directory outside of asset directory at " + fullPath);
			}
			if (Directory.Exists(fullPath))
			{
				Directory.Delete(fullPath, recursive: true);
			}
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to delete directory locally {0}", new object[1] { path });
		}
	}

	public override void RenameDirectory(string path, string newPath, bool applyLocally, Action<string, FormattedMessage> callback)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		string localAssetsPath = _localAssetsDirectoryPath;
		int token = ((BasePacketHandler)_packetHandler).AddPendingCallback<SuccessReply>((Disposable)this, (Action<FailureReply, SuccessReply>)delegate(FailureReply err, SuccessReply reply)
		{
			AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
			{
				if (err == null)
				{
					AssetEditorOverlay.OnDirectoryRenamed(path, newPath);
				}
				if (err?.Message != null)
				{
					ProcessErrorCallback(callback, BsonHelper.ObjectFromBson<FormattedMessage>(err.Message));
				}
				else
				{
					if (applyLocally)
					{
						RenameDirectoryLocally(path, newPath, localAssetsPath);
					}
					ProcessSuccessCallback(callback, newPath);
				}
			});
		});
		_connection.SendPacket((ProtoPacket)new AssetEditorRenameDirectory
		{
			Token = token,
			Path = path,
			NewPath = newPath
		});
	}

	private void RenameDirectoryLocally(string path, string newPath, string localAssetsPath)
	{
		try
		{
			string fullPath = Path.GetFullPath(Path.Combine(localAssetsPath, path));
			string fullPath2 = Path.GetFullPath(Path.Combine(localAssetsPath, newPath));
			string fullPath3 = Path.GetFullPath(localAssetsPath);
			if (!Paths.IsSubPathOf(fullPath, fullPath3))
			{
				throw new Exception("Tried moving directory outside of asset directory at " + fullPath);
			}
			if (!Paths.IsSubPathOf(fullPath2, fullPath3))
			{
				throw new Exception("Tried moving directory outside of asset directory at " + fullPath2);
			}
			if (Directory.Exists(fullPath) && !Directory.Exists(fullPath2))
			{
				Directory.Move(fullPath, fullPath2);
			}
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to delete directory locally {0}", new object[1] { path });
		}
	}

	public override void FetchAsset(AssetReference assetReference, Action<object, FormattedMessage> callback, bool fromOpenedTab = false)
	{
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Expected O, but got Unknown
		if (fromOpenedTab)
		{
			_currentAssetCancellationToken.Cancel();
			_currentAssetCancellationToken = new CancellationTokenSource();
		}
		if (!AssetEditorOverlay.AssetTypeRegistry.AssetTypes.TryGetValue(assetReference.Type, out var assetType))
		{
			Logger.Warn("Tried opening asset with unknown type: {0}", assetReference.Type);
			FormattedMessage message = new FormattedMessage
			{
				MessageId = "ui.assetEditor.errors.unknownAssetType",
				Params = new Dictionary<string, object> { { "assetType", assetReference.Type } }
			};
			AssetEditorOverlay.Interface.Engine.RunOnMainThread(AssetEditorOverlay.Interface, delegate
			{
				callback(null, message);
			}, allowCallFromMainThread: true);
			return;
		}
		int num = ((BasePacketHandler)_packetHandler).AddPendingCallback<AssetEditorFetchAssetReply>((Disposable)this, (Action<FailureReply, AssetEditorFetchAssetReply>)delegate(FailureReply err, AssetEditorFetchAssetReply reply)
		{
			AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
			{
				//IL_005b: Unknown result type (might be due to invalid IL or missing references)
				if (err != null)
				{
					FormattedMessage arg = ((err.Message != null) ? BsonHelper.ObjectFromBson<FormattedMessage>(err.Message) : null);
					callback(null, arg);
				}
				else
				{
					DataConversionUtils.TryDecodeBytes(reply.Contents, assetType.EditorType, out var result, out var error);
					callback(result, error);
				}
			});
		});
		_connection.SendPacket((ProtoPacket)new AssetEditorFetchAsset(num, assetReference.FilePath, fromOpenedTab));
	}

	public override void FetchJsonAssetWithParents(AssetReference assetReference, Action<Dictionary<string, TrackedAsset>, FormattedMessage> callback, bool fromOpenedTab = false)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected O, but got Unknown
		if (fromOpenedTab)
		{
			_currentAssetCancellationToken.Cancel();
			_currentAssetCancellationToken = new CancellationTokenSource();
		}
		int num = ((BasePacketHandler)_packetHandler).AddPendingCallback<AssetEditorFetchJsonAssetWithParentsReply>((Disposable)this, (Action<FailureReply, AssetEditorFetchJsonAssetWithParentsReply>)delegate(FailureReply err, AssetEditorFetchJsonAssetWithParentsReply reply)
		{
			AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
			{
				if (err != null)
				{
					FormattedMessage arg = ((err.Message != null) ? BsonHelper.ObjectFromBson<FormattedMessage>(err.Message) : null);
					callback(null, arg);
				}
				else
				{
					Dictionary<string, TrackedAsset> dictionary = new Dictionary<string, TrackedAsset>();
					foreach (KeyValuePair<string, string> asset in reply.Assets)
					{
						JObject data;
						try
						{
							data = JObject.Parse(asset.Value);
						}
						catch (Exception)
						{
							callback(null, new FormattedMessage
							{
								MessageId = "ui.assetEditor.errors.invalidJson"
							});
							return;
						}
						if (!AssetEditorOverlay.AssetTypeRegistry.TryGetAssetTypeFromPath(asset.Key, out var assetType))
						{
							callback(null, new FormattedMessage
							{
								MessageId = "ui.assetEditor.errors.invalidAssetType"
							});
							return;
						}
						dictionary.Add(value: new TrackedAsset(new AssetReference(assetType, asset.Key), data), key: asset.Key);
					}
					callback(dictionary, null);
				}
			});
		});
		_connection.SendPacket((ProtoPacket)new AssetEditorFetchJsonAssetWithParents(num, assetReference.FilePath, fromOpenedTab));
	}

	public override void UpdateJsonAsset(AssetReference assetReference, List<ClientJsonUpdateCommand> jsonUpdateCommands, Action<FormattedMessage> callback = null)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Expected O, but got Unknown
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Expected O, but got Unknown
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Expected O, but got Unknown
		int token = ((BasePacketHandler)_packetHandler).AddPendingCallback<SuccessReply>((Disposable)this, (Action<FailureReply, SuccessReply>)delegate(FailureReply err, SuccessReply reply)
		{
			AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
			{
				if (err != null)
				{
					FormattedMessage formattedMessage = ((err.Message != null) ? BsonHelper.ObjectFromBson<FormattedMessage>(err.Message) : null);
					AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, formattedMessage);
					Logger.Error("Failed to update json asset: {0}", FormattedMessageConverter.GetString(formattedMessage, AssetEditorOverlay.Interface));
					if (AssetEditorOverlay.CurrentAsset.Equals(assetReference) && AssetEditorOverlay.TrackedAssets.TryGetValue(assetReference.FilePath, out var value) && !value.IsLoading)
					{
						if (AssetEditorOverlay.AssetTypeRegistry.AssetTypes.ContainsKey(assetReference.Type))
						{
							AssetEditorOverlay.FetchOpenAsset();
						}
						else
						{
							AssetEditorOverlay.CloseTab(assetReference);
						}
					}
					callback?.Invoke(formattedMessage);
				}
				else
				{
					Logger.Info("Updated json asset");
					callback?.Invoke(null);
				}
			});
		});
		JsonUpdateCommand[] array = (JsonUpdateCommand[])(object)new JsonUpdateCommand[jsonUpdateCommands.Count];
		for (int i = 0; i < jsonUpdateCommands.Count; i++)
		{
			ClientJsonUpdateCommand clientJsonUpdateCommand = jsonUpdateCommands[i];
			JObject val = new JObject();
			val.Add("value", clientJsonUpdateCommand.Value);
			sbyte[] array2 = BsonHelper.ToBson((JToken)val);
			JObject val2 = new JObject();
			val2.Add("value", clientJsonUpdateCommand.PreviousValue);
			sbyte[] array3 = BsonHelper.ToBson((JToken)val2);
			array[i] = new JsonUpdateCommand(clientJsonUpdateCommand.Type, clientJsonUpdateCommand.Path.Elements, array2, array3, clientJsonUpdateCommand.FirstCreatedProperty?.Elements, clientJsonUpdateCommand.RebuildCaches);
		}
		_connection.SendPacket((ProtoPacket)new AssetEditorUpdateJsonAsset
		{
			Token = token,
			AssetType = assetReference.Type,
			Path = assetReference.FilePath,
			Commands = array
		});
	}

	public override void UpdateAsset(AssetReference assetReference, object data, Action<FormattedMessage> callback = null)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		int token = ((BasePacketHandler)_packetHandler).AddPendingCallback<SuccessReply>((Disposable)this, (Action<FailureReply, SuccessReply>)delegate(FailureReply err, SuccessReply reply)
		{
			AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
			{
				if (err != null)
				{
					FormattedMessage formattedMessage = ((err.Message != null) ? BsonHelper.ObjectFromBson<FormattedMessage>(err.Message) : null);
					AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, formattedMessage);
					Logger.Error("Failed to update asset: {0}", FormattedMessageConverter.GetString(formattedMessage, AssetEditorOverlay.Interface));
					callback?.Invoke(formattedMessage);
				}
				else
				{
					Logger.Info("Updated json asset");
					callback?.Invoke(null);
				}
			});
		});
		sbyte[] data2 = DataConversionUtils.EncodeObject(data);
		_connection.SendPacket((ProtoPacket)new AssetEditorUpdateAsset
		{
			Token = token,
			AssetType = assetReference.Type,
			Path = assetReference.FilePath,
			Data = data2
		});
	}

	public override void SetOpenEditorAsset(AssetReference assetReference)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		_connection.SendPacket((ProtoPacket)new AssetEditorSelectAsset(assetReference.FilePath));
	}

	public override void OnSidebarButtonActivated(string action)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		_connection.SendPacket((ProtoPacket)new AssetEditorActivateButton(action));
	}

	public override void FetchAutoCompleteData(string dataset, string query, Action<HashSet<string>, FormattedMessage> callback)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		int token = ((BasePacketHandler)_packetHandler).AddPendingCallback<AssetEditorFetchAutoCompleteDataReply>((Disposable)this, (Action<FailureReply, AssetEditorFetchAutoCompleteDataReply>)delegate(FailureReply err, AssetEditorFetchAutoCompleteDataReply reply)
		{
			if (err != null)
			{
				FormattedMessage message = BsonHelper.ObjectFromBson<FormattedMessage>(err.Message);
				Logger.Error("Failed to fetch auto complete data: {0}", FormattedMessageConverter.GetString(message, AssetEditorOverlay.Interface));
			}
			else
			{
				ProcessSuccessCallback(callback, new HashSet<string>(reply.Results));
			}
		});
		_connection.SendPacket((ProtoPacket)new AssetEditorFetchAutoCompleteData
		{
			Token = token,
			Dataset = dataset,
			Query = query
		});
	}

	public override bool TryGetDropdownEntriesOrFetch(string dataset, out List<string> entries, object extraValue = null)
	{
		if (base.TryGetDropdownEntriesOrFetch(dataset, out entries, extraValue))
		{
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

	private void LoadDropdownEntries(string dataset)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		_connection.SendPacket((ProtoPacket)new AssetEditorRequestDataset
		{
			Name = dataset
		});
	}

	public void OnDropdownDatasetReceived(string dataset, List<string> entries)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_loadingDropdownDatasets.Remove(dataset);
		_dropdownDatasetEntriesCache[dataset] = entries;
		AssetEditorOverlay.OnDropdownDatasetUpdated(dataset, entries);
	}

	public override void CreateAsset(AssetReference assetReference, object data, string buttonId = null, bool openAssetInTab = false, Action<FormattedMessage> callback = null)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Expected O, but got Unknown
		int token = ((BasePacketHandler)_packetHandler).AddPendingCallback<SuccessReply>((Disposable)this, (Action<FailureReply, SuccessReply>)delegate(FailureReply err, SuccessReply reply)
		{
			AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
			{
				if (err != null)
				{
					FormattedMessage formattedMessage = ((err.Message != null) ? BsonHelper.ObjectFromBson<FormattedMessage>(err.Message) : null);
					callback?.Invoke(formattedMessage);
					AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, formattedMessage);
				}
				else
				{
					callback?.Invoke(null);
					AssetEditorOverlay.OnAssetAdded(assetReference, openInEditor: false);
					if (openAssetInTab)
					{
						AssetEditorOverlay.OpenCreatedAsset(assetReference, data);
					}
				}
			});
		});
		sbyte[] data2 = DataConversionUtils.EncodeObject(data);
		_connection.SendPacket((ProtoPacket)new AssetEditorCreateAsset
		{
			Token = token,
			Path = assetReference.FilePath,
			Data = data2,
			RebuildCaches = GetCachesToRebuild(assetReference.Type),
			ButtonId = buttonId
		});
	}

	public override void DeleteAsset(AssetReference assetReference, bool applyLocally)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		string localAssetsPath = _localAssetsDirectoryPath;
		int token = ((BasePacketHandler)_packetHandler).AddPendingCallback<SuccessReply>((Disposable)this, (Action<FailureReply, SuccessReply>)delegate(FailureReply err, SuccessReply reply)
		{
			AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
			{
				if (err != null)
				{
					FormattedMessage message = ((err.Message != null) ? BsonHelper.ObjectFromBson<FormattedMessage>(err.Message) : null);
					AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, message);
				}
				else
				{
					if (applyLocally)
					{
						DeleteFileLocally(assetReference, localAssetsPath);
					}
					AssetEditorOverlay.OnAssetDeleted(assetReference);
				}
			});
		});
		_connection.SendPacket((ProtoPacket)new AssetEditorDeleteAsset
		{
			Token = token,
			Path = assetReference.FilePath
		});
	}

	private void DeleteFileLocally(AssetReference assetReference, string localAssetsPath)
	{
		try
		{
			string fullPath = Path.GetFullPath(Path.Combine(localAssetsPath, assetReference.FilePath));
			string fullPath2 = Path.GetFullPath(localAssetsPath);
			if (!Paths.IsSubPathOf(fullPath, fullPath2))
			{
				throw new Exception("Tried removing asset file outside of asset directory at " + fullPath);
			}
			if (File.Exists(fullPath))
			{
				File.Delete(fullPath);
			}
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to delete asset locally {0}", new object[1] { assetReference });
			AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.errors.failedToDeleteAsset"));
		}
	}

	public override void RenameAsset(AssetReference assetReference, string newAssetFilePath, bool applyLocally)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		string localAssetsPath = _localAssetsDirectoryPath;
		int token = ((BasePacketHandler)_packetHandler).AddPendingCallback<SuccessReply>((Disposable)this, (Action<FailureReply, SuccessReply>)delegate(FailureReply err, SuccessReply reply)
		{
			AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
			{
				if (err != null)
				{
					FormattedMessage message = ((err.Message != null) ? BsonHelper.ObjectFromBson<FormattedMessage>(err.Message) : null);
					AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, message);
				}
				else
				{
					if (applyLocally)
					{
						RenameAssetLocally(assetReference, newAssetFilePath, localAssetsPath);
					}
					AssetEditorOverlay.OnAssetRenamed(assetReference, new AssetReference(assetReference.Type, newAssetFilePath));
				}
			});
		});
		_connection.SendPacket((ProtoPacket)new AssetEditorRenameAsset
		{
			Token = token,
			Path = assetReference.FilePath,
			NewPath = newAssetFilePath
		});
	}

	private void RenameAssetLocally(AssetReference assetReference, string newFilePath, string localAssetsPath)
	{
		string fullPath = Path.GetFullPath(Path.Combine(localAssetsPath, assetReference.FilePath));
		string fullPath2 = Path.GetFullPath(Path.Combine(localAssetsPath, newFilePath));
		string fullPath3 = Path.GetFullPath(localAssetsPath);
		try
		{
			if (!Paths.IsSubPathOf(fullPath, fullPath3))
			{
				Logger.Warn("Tried moving asset file from folder outside of asset directory at " + fullPath);
				AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.errors.failedToRenameAsset"));
			}
			else if (!Paths.IsSubPathOf(fullPath2, fullPath3))
			{
				Logger.Warn("Tried moving asset file to folder outside of asset directory at " + fullPath);
				AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.errors.failedToRenameAsset"));
			}
			else if (Directory.Exists(fullPath))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(fullPath2));
				File.Move(fullPath, fullPath2);
			}
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to move file locally from {0} to {1}", new object[2] { fullPath, fullPath2 });
			AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.errors.failedToRenameAsset"));
		}
	}

	private int CreateUndoRedoCallback(AssetReference assetReference)
	{
		CancellationToken cancellationToken = _currentAssetCancellationToken.Token;
		AssetEditorOverlay.ConfigEditor.SetWaitingForBackend(isWaiting: true);
		return ((BasePacketHandler)_packetHandler).AddPendingCallback<AssetEditorUndoRedoReply>((Disposable)this, (Action<FailureReply, AssetEditorUndoRedoReply>)delegate(FailureReply err, AssetEditorUndoRedoReply reply)
		{
			AssetEditorOverlay.Interface.Engine.RunOnMainThread(this, delegate
			{
				//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
				//IL_00be: Expected O, but got Unknown
				//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
				//IL_0101: Invalid comparison between Unknown and I4
				//IL_0145: Unknown result type (might be due to invalid IL or missing references)
				//IL_014a: Unknown result type (might be due to invalid IL or missing references)
				//IL_014c: Unknown result type (might be due to invalid IL or missing references)
				//IL_014e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0150: Unknown result type (might be due to invalid IL or missing references)
				//IL_0153: Invalid comparison between Unknown and I4
				//IL_010f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0115: Expected O, but got Unknown
				//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
				//IL_01bf: Invalid comparison between Unknown and I4
				//IL_0157: Unknown result type (might be due to invalid IL or missing references)
				//IL_015a: Invalid comparison between Unknown and I4
				if (!cancellationToken.IsCancellationRequested)
				{
					AssetEditorOverlay.ConfigEditor.SetWaitingForBackend(isWaiting: false);
					if (err != null)
					{
						FormattedMessage message = ((err.Message != null) ? BsonHelper.ObjectFromBson<FormattedMessage>(err.Message) : null);
						AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, message);
					}
					else
					{
						JObject root = (JObject)AssetEditorOverlay.TrackedAssets[assetReference.FilePath].Data;
						JsonUpdateCommand command = reply.Command;
						JToken val = ((command.Value != null) ? BsonHelper.FromBson(command.Value)[(object)"value"] : null);
						if (command.Path.Length == 0 && (int)command.Type == 0)
						{
							root = (JObject)val;
							AssetEditorOverlay.SetTrackedAssetData(assetReference.FilePath, root);
						}
						else
						{
							JsonUpdateType type = command.Type;
							JsonUpdateType val2 = type;
							PropertyPath? firstCreatedProperty;
							if ((int)val2 > 1)
							{
								if ((int)val2 == 2)
								{
									AssetEditorOverlay.ConfigEditor.RemoveProperty(root, PropertyPath.FromElements(command.Path), out firstCreatedProperty, out var _, updateDisplayedValue: true, cleanupEmptyContainers: false);
								}
							}
							else
							{
								AssetEditorOverlay.ConfigEditor.SetProperty(root, PropertyPath.FromElements(command.Path), val, out firstCreatedProperty, updateDisplayedValue: true, (int)command.Type == 1);
							}
						}
						AssetEditorOverlay.Layout();
					}
				}
			});
		});
	}

	public override void UndoChanges(AssetReference assetReference)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		int num = CreateUndoRedoCallback(assetReference);
		_connection.SendPacket((ProtoPacket)new AssetEditorUndoChanges(num, assetReference.FilePath));
	}

	public override void RedoChanges(AssetReference assetReference)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		int num = CreateUndoRedoCallback(assetReference);
		_connection.SendPacket((ProtoPacket)new AssetEditorRedoChanges(num, assetReference.FilePath));
	}

	public override void DiscardChanges(List<TimestampedAssetReference> assetsToDiscard)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		TimestampedAssetReference[] array = (TimestampedAssetReference[])(object)new TimestampedAssetReference[assetsToDiscard.Count];
		for (int i = 0; i < assetsToDiscard.Count; i++)
		{
			array[i] = assetsToDiscard[i].ToPacket();
		}
		_connection.SendPacket((ProtoPacket)new AssetEditorDiscardChanges(array));
	}

	public override void ExportAssets(List<AssetReference> assetReferences, Action<List<TimestampedAssetReference>> callback = null)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		if (base.IsExportingAssets)
		{
			AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, AssetEditorOverlay.Interface.GetText("ui.assetEditor.errors.exportInProgress"));
			return;
		}
		AssetEditorExportAssets val = new AssetEditorExportAssets(new string[assetReferences.Count]);
		for (int i = 0; i < assetReferences.Count; i++)
		{
			val.Paths[i] = assetReferences[i].FilePath;
			AssetExportStatuses[assetReferences[i].FilePath] = AssetExportStatus.Pending;
		}
		LocalAssetsDirectoryPathForCurrentExport = _localAssetsDirectoryPath;
		base.IsExportingAssets = true;
		_exportCompleteCallback = callback;
		AssetEditorOverlay.ExportModal.UpdateExportButtonState();
		_connection.SendPacket((ProtoPacket)(object)val);
	}

	public void OnExportProgress()
	{
		if (!base.IsExportingAssets)
		{
			return;
		}
		int num = 0;
		foreach (AssetExportStatus value in AssetExportStatuses.Values)
		{
			if (value != 0)
			{
				num++;
			}
		}
		Logger.Info($"Export progress {num}/{AssetExportStatuses.Count}");
	}

	public void OnExportComplete(TimestampedAssetReference[] sentAssets)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		int num = 0;
		int num2 = 0;
		foreach (AssetExportStatus value2 in AssetExportStatuses.Values)
		{
			if (value2 != 0)
			{
				num++;
			}
			if (value2 == AssetExportStatus.Complete)
			{
				num2++;
			}
		}
		Debug.Assert(num == AssetExportStatuses.Count);
		Debug.Assert(base.IsExportingAssets);
		if (!base.IsExportingAssets)
		{
			return;
		}
		List<TimestampedAssetReference> list = new List<TimestampedAssetReference>();
		foreach (TimestampedAssetReference timestampedAssetReference in sentAssets)
		{
			if (AssetExportStatuses.TryGetValue(timestampedAssetReference.Path, out var value) && value == AssetExportStatus.Complete)
			{
				list.Add(timestampedAssetReference);
			}
		}
		base.IsExportingAssets = false;
		AssetExportStatuses.Clear();
		AssetEditorOverlay.ExportModal.UpdateExportButtonState();
		Logger.Info("Export complete");
		AssetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)1, $"Exported {num2}/{num} assets");
		if (_exportCompleteCallback != null)
		{
			Action<List<TimestampedAssetReference>> exportCompleteCallback = _exportCompleteCallback;
			_exportCompleteCallback = null;
			exportCompleteCallback(list);
		}
	}

	public override void FetchLastModifiedAssets()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		_connection.SendPacket((ProtoPacket)new AssetEditorFetchLastModifiedAssets());
	}

	public override void UpdateSubscriptionToModifiedAssetsUpdates(bool subscribe)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		_connection.SendPacket((ProtoPacket)new AssetEditorSubscribeModifiedAssetsChanges(subscribe));
	}

	public override AssetInfo[] GetLastModifiedAssets()
	{
		return _lastModifiedAssets;
	}

	public void SetupLastModifiedAssets(AssetInfo[] assets)
	{
		CancellationToken backendLifetimeCancellationToken = _backendLifetimeCancellationToken;
		if (!backendLifetimeCancellationToken.IsCancellationRequested)
		{
			Array.Sort(assets, (AssetInfo a, AssetInfo b) => (int)((float)(b.LastModificationDate - a.LastModificationDate) / 1000f));
			_lastModifiedAssets = assets;
			AssetEditorOverlay.SetModifiedAssetsCount(_lastModifiedAssets.Length);
			if (AssetEditorOverlay.ExportModal.IsMounted)
			{
				AssetEditorOverlay.ExportModal.Setup();
			}
		}
	}

	public void SetupModifiedAssetsCount(int count)
	{
		CancellationToken backendLifetimeCancellationToken = _backendLifetimeCancellationToken;
		if (!backendLifetimeCancellationToken.IsCancellationRequested)
		{
			AssetEditorOverlay.SetModifiedAssetsCount(count);
		}
	}

	public void OnAssetUpdated(string filePath, sbyte[] data)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		AssetTypeRegistry assetTypeRegistry = AssetEditorOverlay.AssetTypeRegistry;
		if (AssetEditorOverlay.TrackedAssets.ContainsKey(filePath) && assetTypeRegistry.TryGetAssetTypeFromPath(filePath, out var assetType) && assetTypeRegistry.AssetTypes.TryGetValue(assetType, out var value) && DataConversionUtils.TryDecodeBytes(data, value.EditorType, out var result, out var _))
		{
			AssetEditorOverlay.SetTrackedAssetData(filePath, result);
		}
	}

	public override void SetGameTime(DateTime time, bool paused)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		InstantData val = TimeHelper.DateTimeToInstantData(time);
		_connection.SendPacket((ProtoPacket)new AssetEditorSetGameTime(val, paused));
	}

	public override void SetWeatherAndTimeLock(bool locked)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		_connection.SendPacket((ProtoPacket)new AssetEditorUpdateWeatherPreviewLock(locked));
	}

	public void OnJsonAssetUpdated(string assetPath, JsonUpdateCommand[] commands)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Invalid comparison between Unknown and I4
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Invalid comparison between Unknown and I4
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected O, but got Unknown
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Invalid comparison between Unknown and I4
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Invalid comparison between Unknown and I4
		if (!AssetEditorOverlay.TrackedAssets.TryGetValue(assetPath, out var value))
		{
			return;
		}
		JObject val = (JObject)value.Data;
		foreach (JsonUpdateCommand val2 in commands)
		{
			JToken val3 = ((val2.Value != null) ? BsonHelper.FromBson(val2.Value)[(object)"value"] : null);
			if (val2.Path.Length == 0 && (int)val2.Type == 0)
			{
				val = (JObject)val3;
				AssetEditorOverlay.SetTrackedAssetData(assetPath, val);
				continue;
			}
			JsonUpdateType type = val2.Type;
			JsonUpdateType val4 = type;
			PropertyPath? firstCreatedProperty;
			if ((int)val4 > 1)
			{
				if ((int)val4 == 2)
				{
					AssetEditorOverlay.ConfigEditor.RemoveProperty(val, PropertyPath.FromElements(val2.Path), out firstCreatedProperty, out var _, updateDisplayedValue: true, cleanupEmptyContainers: false);
				}
			}
			else
			{
				AssetEditorOverlay.ConfigEditor.SetProperty(val, PropertyPath.FromElements(val2.Path), val3, out firstCreatedProperty, updateDisplayedValue: true, (int)val2.Type == 1);
			}
		}
		AssetEditorOverlay.Layout();
	}

	public override void OnLanguageChanged()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		AssetEditorSettings settings = AssetEditorOverlay.Interface.App.Settings;
		_connection.SendPacket((ProtoPacket)new UpdateLanguage(settings.Language ?? Language.SystemLanguage));
	}

	private AssetEditorRebuildCaches GetCachesToRebuild(string assetType)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Expected O, but got Unknown
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		AssetTypeConfig assetTypeConfig = AssetEditorOverlay.AssetTypeRegistry.AssetTypes[assetType];
		if (assetTypeConfig.RebuildCaches == null)
		{
			return new AssetEditorRebuildCaches();
		}
		return new AssetEditorRebuildCaches
		{
			Models = assetTypeConfig.RebuildCaches.Contains(AssetTypeConfig.RebuildCacheType.Models),
			ModelTextures = assetTypeConfig.RebuildCaches.Contains(AssetTypeConfig.RebuildCacheType.ModelTextures),
			BlockTextures = assetTypeConfig.RebuildCaches.Contains(AssetTypeConfig.RebuildCacheType.BlockTextures),
			ItemIcons = assetTypeConfig.RebuildCaches.Contains(AssetTypeConfig.RebuildCacheType.ItemIcons),
			MapGeometry = assetTypeConfig.RebuildCaches.Contains(AssetTypeConfig.RebuildCacheType.MapGemoetry)
		};
	}
}
