#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Hypixel.ProtoPlus;
using HytaleClient.AssetEditor.Backends;
using HytaleClient.AssetEditor.Data;
using HytaleClient.Interface.Messages;
using HytaleClient.Networking;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.AssetEditor.Networking;

internal class AssetEditorPacketHandler : BasePacketHandler
{
	[Flags]
	private enum ConnectionStage : byte
	{
		Auth = 2,
		Complete = 4
	}

	private FileStream _blobFileStream;

	private Asset _blobAsset;

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly AssetEditorApp _app;

	private readonly Stopwatch _stageStopwatch = Stopwatch.StartNew();

	private readonly HashSet<string> _unhandledPacketTypes = new HashSet<string>();

	private ConnectionStage _stage = ConnectionStage.Auth;

	private void ProcessAssetEditorAssetListSetup(AssetEditorAssetListSetup packet)
	{
		_app.Engine.RunOnMainThread(this, delegate
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			_app.Editor.ServerBackend.SetupAssetList(packet.Tree, packet.Paths);
		});
	}

	private void ProcessAssetEditorAssetListUpdate(AssetEditorAssetListUpdate packet)
	{
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Editor.ServerBackend.UpdateAssetList(packet.Additions, packet.Deletions);
		});
	}

	private void ProcessAssetEditorPopupNotification(AssetEditorPopupNotification packet)
	{
		_app.Engine.RunOnMainThread(this, delegate
		{
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			if (FormattedMessage.TryParseFromBson(packet.Message, out var message))
			{
				_app.Interface.AssetEditor.ToastNotifications.AddNotification(packet.Type, message);
			}
			else
			{
				Logger.Warn("Failed to parse asset editor popup notification");
			}
		});
	}

	private void ProcessAssetEditorSetupSchemas(AssetEditorSetupSchemas packet)
	{
		Logger.Info("Received schemas for asset editor setup");
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Editor.ServerBackend.SetupSchemas(packet.Schemas);
		});
	}

	private void ProcessAssetEditorSetupAssetTypes(AssetEditorSetupAssetTypes packet)
	{
		Logger.Info("Received asset types for asset editor setup");
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Editor.ServerBackend.SetupAssetTypes(packet.AssetTypes);
		});
	}

	private void ProcessAssetEditorAssetUpdated(AssetEditorAssetUpdated packet)
	{
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Editor.ServerBackend.OnAssetUpdated(packet.Path, packet.Data);
		});
	}

	private void ProcessAssetEditorAssetUpdated(AssetEditorJsonAssetUpdated packet)
	{
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Editor.ServerBackend.OnJsonAssetUpdated(packet.Path, packet.Commands);
		});
	}

	private void ProcessAssetEditorFetchAssetReply(AssetEditorFetchAssetReply packet)
	{
		CallPendingCallback(packet.Token, (ProtoPacket)(object)packet, null);
	}

	private void ProcessAssetEditorFetchJsonAssetWithParentsReply(AssetEditorFetchJsonAssetWithParentsReply packet)
	{
		CallPendingCallback(packet.Token, (ProtoPacket)(object)packet, null);
	}

	private void ProcessAssetEditorRequestDatasetReply(AssetEditorRequestDatasetReply packet)
	{
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Editor.ServerBackend.OnDropdownDatasetReceived(packet.Name, packet.Ids.ToList());
		});
	}

	private void ProcessAssetEditorFetchAutoCompleteDataReply(AssetEditorFetchAutoCompleteDataReply packet)
	{
		CallPendingCallback(packet.Token, (ProtoPacket)(object)packet, null);
	}

	private void ProcessAssetEditorDeleteAssets(AssetEditorExportDeleteAssets packet)
	{
		if (!_app.Editor.Backend.IsExportingAssets)
		{
			throw new Exception("Received export asset while not exporting");
		}
		string localAssetsDirectoryPathForCurrentExport = _app.Editor.ServerBackend.LocalAssetsDirectoryPathForCurrentExport;
		Asset[] asset_ = packet.Asset_;
		foreach (Asset asset in asset_)
		{
			if (!_app.Editor.ServerBackend.AssetExportStatuses.ContainsKey(asset.Name))
			{
				throw new Exception("Received unexpected asset during export");
			}
			string fullPath = Path.GetFullPath(Path.Combine(localAssetsDirectoryPathForCurrentExport, asset.Name));
			string fullPath2 = Path.GetFullPath(Path.Combine(localAssetsDirectoryPathForCurrentExport, "Common"));
			string fullPath3 = Path.GetFullPath(Path.Combine(localAssetsDirectoryPathForCurrentExport, "Server"));
			if (!Paths.IsSubPathOf(fullPath, fullPath2) && !Paths.IsSubPathOf(fullPath, fullPath3))
			{
				throw new Exception("Path must be within assets directory");
			}
			if (Path.GetFileName(fullPath).StartsWith("."))
			{
				throw new Exception("File cannot start with .");
			}
			if (File.Exists(fullPath))
			{
				File.Delete(fullPath);
			}
			Logger.Info("Exported (deleted) {0} from asset editor", fullPath);
			_app.Engine.RunOnMainThread(this, delegate
			{
				_app.Editor.ServerBackend.AssetExportStatuses[asset.Name] = ServerAssetEditorBackend.AssetExportStatus.Complete;
				_app.Editor.ServerBackend.OnExportProgress();
			});
		}
	}

	private void ProcessAssetEditorInitialize(AssetEditorExportAssetInitialize packet)
	{
		if (!_app.Editor.Backend.IsExportingAssets)
		{
			throw new Exception("Received export asset while not exporting");
		}
		if (!_app.Editor.ServerBackend.AssetExportStatuses.ContainsKey(packet.Asset_.Name))
		{
			throw new Exception("Received unexpected asset during export");
		}
		if (_blobAsset != null)
		{
			throw new Exception("A blob download has already started! Name: " + _blobAsset.Name + ", Hash: " + _blobAsset.Hash);
		}
		if (packet.Failed)
		{
			_app.Engine.RunOnMainThread(this, delegate
			{
				_app.Editor.ServerBackend.AssetExportStatuses[packet.Asset_.Name] = ServerAssetEditorBackend.AssetExportStatus.Failed;
				_app.Editor.ServerBackend.OnExportProgress();
			});
		}
		else
		{
			_blobFileStream = File.Create(Paths.TempAssetEditorDownload);
			_blobAsset = packet.Asset_;
		}
	}

	private void ProcessAssetEditorPart(AssetEditorExportAssetPart packet)
	{
		if (!_app.Editor.ServerBackend.IsExportingAssets)
		{
			throw new Exception("Received export asset while not exporting");
		}
		_blobFileStream.Write((byte[])(object)packet.Part, 0, packet.Part.Length);
	}

	private void ProcessAssetEditorFinalize(AssetEditorExportAssetFinalize packet)
	{
		if (!_app.Editor.ServerBackend.IsExportingAssets)
		{
			throw new Exception("Received export asset while not exporting");
		}
		string localAssetsDirectoryPathForCurrentExport = _app.Editor.ServerBackend.LocalAssetsDirectoryPathForCurrentExport;
		_blobFileStream.Flush(flushToDisk: true);
		_blobFileStream.Close();
		_blobFileStream = null;
		string assetName = _blobAsset.Name;
		_blobAsset = null;
		string fullPath = Path.GetFullPath(Path.Combine(localAssetsDirectoryPathForCurrentExport, assetName));
		string fullPath2 = Path.GetFullPath(Path.Combine(localAssetsDirectoryPathForCurrentExport, "Common"));
		string fullPath3 = Path.GetFullPath(Path.Combine(localAssetsDirectoryPathForCurrentExport, "Server"));
		if (!Paths.IsSubPathOf(fullPath, fullPath2) && !Paths.IsSubPathOf(fullPath, fullPath3))
		{
			throw new Exception("Path must be within assets directory");
		}
		if (Path.GetFileName(fullPath).StartsWith("."))
		{
			throw new Exception("File cannot start with .");
		}
		Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
		if (File.Exists(fullPath))
		{
			File.Delete(fullPath);
		}
		File.Move(Paths.TempAssetEditorDownload, fullPath);
		Logger.Info("Exported {0} from asset editor", fullPath);
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Editor.ServerBackend.AssetExportStatuses[assetName] = ServerAssetEditorBackend.AssetExportStatus.Complete;
			_app.Editor.ServerBackend.OnExportProgress();
		});
	}

	public void ProcessAssetEditorExportComplete(AssetEditorExportComplete packet)
	{
		if (!_app.Editor.ServerBackend.IsExportingAssets)
		{
			throw new Exception("Received $ProcessAssetEditorExportComplete asset while no asset export is active");
		}
		_app.Engine.RunOnMainThread(this, delegate
		{
			TimestampedAssetReference[] array = new TimestampedAssetReference[packet.Assets.Length];
			for (int i = 0; i < packet.Assets.Length; i++)
			{
				TimestampedAssetReference val = packet.Assets[i];
				array[i] = new TimestampedAssetReference(val.Path, val.Timestamp);
			}
			_app.Editor.ServerBackend.OnExportComplete(array);
		});
	}

	private void ProcessAssetEditorUndoRedoReply(AssetEditorUndoRedoReply packet)
	{
		CallPendingCallback(packet.Token, (ProtoPacket)(object)packet, null);
	}

	private void ProcessAssetEditorReceiveLastModifiedAssets(AssetEditorLastModifiedAssets packet)
	{
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Editor.ServerBackend.SetupLastModifiedAssets(packet.Assets);
		});
	}

	private void ProcessAssetEditorModifiedAssetsCount(AssetEditorModifiedAssetsCount packet)
	{
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Editor.ServerBackend.SetupModifiedAssetsCount(packet.Count);
		});
	}

	private void ProcessAuth2Packet(Auth2 packet)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		_app.AuthManager.HandleAuth2(packet.NonceA, packet.Cert, out var encryptedNonceA, out var encryptedNonceB);
		_connection.SendPacket((ProtoPacket)new Auth3(encryptedNonceA, encryptedNonceB));
	}

	private void ProcessAuth4Packet(Auth4 packet)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		_app.AuthManager.HandleAuth4(packet.Secret, packet.NonceB);
		_connection.SendPacket((ProtoPacket)new Auth5());
	}

	private void ProcessAuth6Packet(Auth6 packet)
	{
		_app.AuthManager.HandleAuth6();
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.MainMenu.OnAuthenticated();
		});
		SetStage(ConnectionStage.Complete);
	}

	private void ProcessFailureReply(FailureReply packet)
	{
		CallPendingCallback(packet.Token, null, packet);
	}

	private void ProcessSuccessReply(SuccessReply packet)
	{
		CallPendingCallback(packet.Token, (ProtoPacket)(object)packet, null);
	}

	public AssetEditorPacketHandler(AssetEditorApp app, ConnectionToServer connection)
		: base(app.Engine, connection)
	{
		_app = app;
	}

	private void SetStage(ConnectionStage stage)
	{
		Logger.Info<ConnectionStage, ConnectionStage, long>("Stage {0} -> {1} took {2}ms", _stage, stage, _stageStopwatch.ElapsedMilliseconds);
		_stage = stage;
		_stageStopwatch.Restart();
	}

	protected override void SetDisconnectReason(string reason)
	{
		_app.Engine.RunOnMainThread(_app.Engine, delegate
		{
			_app.MainMenu.ServerDisconnectReason = reason;
		}, allowCallFromMainThread: true);
	}

	protected override void ProcessPacket(ProtoPacket packet)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Expected O, but got Unknown
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Expected O, but got Unknown
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Expected O, but got Unknown
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c9: Expected O, but got Unknown
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Expected O, but got Unknown
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Expected O, but got Unknown
		//IL_02e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ed: Expected O, but got Unknown
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Expected O, but got Unknown
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Expected O, but got Unknown
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Expected O, but got Unknown
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Expected O, but got Unknown
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Expected O, but got Unknown
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Expected O, but got Unknown
		//IL_0307: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Expected O, but got Unknown
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Expected O, but got Unknown
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Expected O, but got Unknown
		//IL_0379: Unknown result type (might be due to invalid IL or missing references)
		//IL_0383: Expected O, but got Unknown
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Expected O, but got Unknown
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Expected O, but got Unknown
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Expected O, but got Unknown
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0335: Expected O, but got Unknown
		//IL_036a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0374: Expected O, but got Unknown
		//IL_0319: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Expected O, but got Unknown
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0365: Expected O, but got Unknown
		//IL_034c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Expected O, but got Unknown
		Debug.Assert(ThreadHelper.IsOnThread(_thread));
		switch (_stage)
		{
		case ConnectionStage.Auth:
			switch (packet.GetId())
			{
			case 58:
				ProcessAuth2Packet((Auth2)packet);
				break;
			case 60:
				ProcessAuth4Packet((Auth4)packet);
				break;
			case 62:
				ProcessAuth6Packet((Auth6)packet);
				break;
			default:
				if (_unhandledPacketTypes.Add(((object)packet).GetType().Name))
				{
					Logger.Warn("Received unhandled packet type: {0}", ((object)packet).GetType().Name);
				}
				break;
			}
			break;
		case ConnectionStage.Complete:
			switch (packet.GetId())
			{
			case 7:
				ProcessAssetEditorAssetListSetup((AssetEditorAssetListSetup)packet);
				break;
			case 8:
				ProcessAssetEditorAssetListUpdate((AssetEditorAssetListUpdate)packet);
				break;
			case 33:
				ProcessAssetEditorPopupNotification((AssetEditorPopupNotification)packet);
				break;
			case 45:
				ProcessAssetEditorSetupSchemas((AssetEditorSetupSchemas)packet);
				break;
			case 44:
				ProcessAssetEditorSetupAssetTypes((AssetEditorSetupAssetTypes)packet);
				break;
			case 9:
				ProcessAssetEditorAssetUpdated((AssetEditorAssetUpdated)packet);
				break;
			case 30:
				ProcessAssetEditorAssetUpdated((AssetEditorJsonAssetUpdated)packet);
				break;
			case 22:
				ProcessAssetEditorFetchAssetReply((AssetEditorFetchAssetReply)packet);
				break;
			case 26:
				ProcessAssetEditorFetchJsonAssetWithParentsReply((AssetEditorFetchJsonAssetWithParentsReply)packet);
				break;
			case 41:
				ProcessAssetEditorRequestDatasetReply((AssetEditorRequestDatasetReply)packet);
				break;
			case 24:
				ProcessAssetEditorFetchAutoCompleteDataReply((AssetEditorFetchAutoCompleteDataReply)packet);
				break;
			case 16:
				ProcessAssetEditorInitialize((AssetEditorExportAssetInitialize)packet);
				break;
			case 17:
				ProcessAssetEditorPart((AssetEditorExportAssetPart)packet);
				break;
			case 15:
				ProcessAssetEditorFinalize((AssetEditorExportAssetFinalize)packet);
				break;
			case 20:
				ProcessAssetEditorDeleteAssets((AssetEditorExportDeleteAssets)packet);
				break;
			case 19:
				ProcessAssetEditorExportComplete((AssetEditorExportComplete)packet);
				break;
			case 31:
				ProcessAssetEditorReceiveLastModifiedAssets((AssetEditorLastModifiedAssets)packet);
				break;
			case 32:
				ProcessAssetEditorModifiedAssetsCount((AssetEditorModifiedAssetsCount)packet);
				break;
			case 48:
				ProcessAssetEditorUndoRedoReply((AssetEditorUndoRedoReply)packet);
				break;
			case 229:
				ProcessUpdateTranslations((UpdateTranslations)packet);
				break;
			case 194:
				ProcessUpdateEditorTimeOverride((UpdateEditorTimeOverride)packet);
				break;
			case 52:
				ProcessAssetEditorUpdateSecondsPerGameDay((AssetEditorUpdateSecondsPerGameDay)packet);
				break;
			case 51:
				ProcessUpdateModelPreview((AssetEditorUpdateModelPreview)packet);
				break;
			case 164:
				ProcessSuccessReply((SuccessReply)packet);
				break;
			case 103:
				ProcessFailureReply((FailureReply)packet);
				break;
			default:
				if (_unhandledPacketTypes.Add(((object)packet).GetType().Name))
				{
					Logger.Warn("Received unhandled packet type: {0}", ((object)packet).GetType().Name);
				}
				break;
			}
			break;
		}
	}

	private void ProcessUpdateTranslations(UpdateTranslations packet)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		UpdateType updateType = packet.Type;
		Dictionary<string, string> translations = new Dictionary<string, string>(packet.Translations.Count);
		foreach (KeyValuePair<string, string> translation in packet.Translations)
		{
			translations[string.Copy(translation.Key)] = string.Copy(translation.Value);
		}
		_app.Engine.RunOnMainThread(this, delegate
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Expected I4, but got Unknown
			Stopwatch stopwatch = Stopwatch.StartNew();
			Logger.Info($"[UpdateTranslations] Starting update... ({updateType})");
			UpdateType val = updateType;
			UpdateType val2 = val;
			switch ((int)val2)
			{
			case 0:
				_app.Interface.SetServerMessages(translations);
				break;
			case 1:
				_app.Interface.AddServerMessages(translations);
				break;
			case 2:
				_app.Interface.RemoveServerMessages(translations.Keys);
				break;
			}
			stopwatch.Stop();
			Logger.Info($"[UpdateTranslations] Update complete. Took {stopwatch.Elapsed.TotalMilliseconds}ms");
		});
	}

	private void ProcessUpdateModelPreview(AssetEditorUpdateModelPreview packet)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		string assetPath = packet.AssetPath;
		if (assetPath == null)
		{
			return;
		}
		AssetEditorPreviewCameraSettings camera = ((packet.Camera == null) ? ((AssetEditorPreviewCameraSettings)null) : new AssetEditorPreviewCameraSettings(packet.Camera));
		BlockType block = ((packet.Block == null) ? ((BlockType)null) : new BlockType(packet.Block));
		Model model = ((packet.Model_ == null) ? ((Model)null) : new Model(packet.Model_));
		_app.Engine.RunOnMainThread(this, delegate
		{
			if (!(_app.Interface.AssetEditor.CurrentAsset.FilePath != assetPath))
			{
				if (block != null)
				{
					_app.Editor.SetBlockPreview(block, camera);
				}
				else if (model != null)
				{
					_app.Editor.SetModelPreview(model, camera);
				}
				else
				{
					_app.Editor.ClearPreview();
				}
			}
		});
	}

	private void ProcessUpdateEditorTimeOverride(UpdateEditorTimeOverride packet)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		InstantData gameTime = new InstantData(packet.GameTime);
		bool isPaused = packet.Paused;
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Editor.GameTime.ProcessServerTimeUpdate(gameTime, isPaused);
		});
	}

	private void ProcessAssetEditorUpdateSecondsPerGameDay(AssetEditorUpdateSecondsPerGameDay packet)
	{
		int seconds = packet.SecondsPerGameDay;
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Editor.GameTime.SecondsPerGameDay = seconds;
		});
	}
}
