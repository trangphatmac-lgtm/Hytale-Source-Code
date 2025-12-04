using HytaleClient.AssetEditor.Backends;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.AssetEditor.Networking;
using HytaleClient.Networking;
using HytaleClient.Protocol;
using NLog;

namespace HytaleClient.AssetEditor;

internal class AssetEditorAppEditor
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly AssetEditorApp _app;

	public readonly GameTimeState GameTime;

	public AssetEditorBackend Backend { get; private set; }

	public ServerAssetEditorBackend ServerBackend => Backend as ServerAssetEditorBackend;

	public Model ModelPreview { get; private set; }

	public BlockType BlockPreview { get; private set; }

	public AssetEditorPreviewCameraSettings PreviewCameraSettings { get; private set; }

	public AssetEditorAppEditor(AssetEditorApp app)
	{
		_app = app;
		GameTime = new GameTimeState(app);
	}

	public void OpenCosmeticsEditor()
	{
		AssetEditorOverlay assetEditor = _app.Interface.AssetEditor;
		Backend = new LocalAssetEditorBackend(assetEditor, new AssetTreeFolder[1] { AssetTreeFolder.Cosmetics });
		assetEditor.SetupBackend(Backend);
		_app.SetStage(AssetEditorApp.AppStage.Editor);
	}

	public void OpenAssetEditor(ConnectionToServer connectionToServer, AssetEditorPacketHandler packetHandler)
	{
		Backend = new ServerAssetEditorBackend(_app.Interface.AssetEditor, connectionToServer, packetHandler);
		_app.Interface.AssetEditor.SetupBackend(Backend);
		_app.SetStage(AssetEditorApp.AppStage.Editor);
	}

	public void CloseEditor()
	{
		_app.SetStage(AssetEditorApp.AppStage.MainMenu);
	}

	public void OpenAsset(string assetPath)
	{
		_app.Interface.AssetEditor.OpenExistingAsset(assetPath, bringAssetIntoAssetTreeView: true);
	}

	public void OpenAsset(AssetIdReference assetReference)
	{
		_app.Interface.AssetEditor.OpenExistingAssetById(assetReference, bringAssetIntoAssetTreeView: true);
	}

	public void OnAssetEditorPathChanged()
	{
		AssetEditorOverlay assetEditor = _app.Interface.AssetEditor;
		if (!assetEditor.IsBackendInitialized)
		{
			return;
		}
		assetEditor.FinishWork();
		AssetEditorBackend backend = Backend;
		AssetEditorBackend assetEditorBackend = backend;
		if (!(assetEditorBackend is LocalAssetEditorBackend))
		{
			if (assetEditorBackend is ServerAssetEditorBackend serverAssetEditorBackend)
			{
				serverAssetEditorBackend.OnLocalAssetsDirectoryPathChanged();
			}
		}
		else
		{
			_app.MainMenu.Open();
			_app.Editor.OpenCosmeticsEditor();
		}
	}

	public void CleanUp()
	{
		Backend?.Dispose();
		Backend = null;
		AssetEditorOverlay assetEditor = _app.Interface.AssetEditor;
		assetEditor.FinishWork();
		assetEditor.CleanupWebViews();
		assetEditor.Reset();
		GameTime.Cleanup();
		ModelPreview = null;
		BlockPreview = null;
	}

	public void SetModelPreview(Model model, AssetEditorPreviewCameraSettings camera)
	{
		ModelPreview = model;
		BlockPreview = null;
		if (camera != null)
		{
			PreviewCameraSettings = camera;
		}
		_app.Interface.AssetEditor.UpdateModelPreview();
	}

	public void SetBlockPreview(BlockType blockType, AssetEditorPreviewCameraSettings camera)
	{
		BlockPreview = blockType;
		ModelPreview = null;
		if (camera != null)
		{
			PreviewCameraSettings = camera;
		}
		_app.Interface.AssetEditor.UpdateModelPreview();
	}

	public void ClearPreview(bool updateUi = true)
	{
		BlockPreview = null;
		ModelPreview = null;
		PreviewCameraSettings = null;
		if (updateUi)
		{
			_app.Interface.AssetEditor.UpdateModelPreview();
		}
	}

	public void ShowIpcServerConnectionPrompt(string serverName)
	{
		_app.Interface.AssetEditor.OpenIpcOpenEditorConfirmationModal(serverName);
	}
}
