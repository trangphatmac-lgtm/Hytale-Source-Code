using System.Collections.Generic;
using System.Threading;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Previews;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using NLog;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Editor;

internal class IconMassExportModal : Element
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private Label _statusLabel;

	private TextButton _startButton;

	private TextButton _cancelButton;

	private TextButton _closeButton;

	private NumberField _scaleField;

	private bool _isExporting;

	private readonly AssetEditorOverlay _assetEditorOverlay;

	private readonly Queue<string> _pathQueue = new Queue<string>();

	private int _totalCount;

	private int _completeCount;

	private int _failedCount;

	private int _skippedCount;

	private AssetPreview _modelPreview;

	private CancellationTokenSource _cancellationTokenSource;

	private int _iconSize;

	public IconMassExportModal(AssetEditorOverlay assetEditorOverlay)
		: base(assetEditorOverlay.Desktop, null)
	{
		_assetEditorOverlay = assetEditorOverlay;
	}

	protected override void OnMounted()
	{
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
		if (_isExporting)
		{
			CancelExport();
		}
	}

	private void Animate(float deltaTime)
	{
		if (_isExporting)
		{
			_statusLabel.Text = Desktop.Provider.GetText("ui.assetEditor.iconMassExportModal.status", new Dictionary<string, string>
			{
				{
					"completeCount",
					Desktop.Provider.FormatNumber(_completeCount)
				},
				{
					"totalCount",
					Desktop.Provider.FormatNumber(_totalCount)
				},
				{
					"failedCount",
					Desktop.Provider.FormatNumber(_failedCount)
				},
				{
					"skippedCount",
					Desktop.Provider.FormatNumber(_skippedCount)
				},
				{
					"percentage",
					Desktop.Provider.FormatNumber((int)((float)_completeCount / (float)_totalCount * 100f))
				}
			});
			_statusLabel.Layout();
		}
	}

	public void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("AssetEditor/IconMassExportModal.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_statusLabel = uIFragment.Get<Label>("StatusLabel");
		_startButton = uIFragment.Get<TextButton>("StartButton");
		_startButton.Activating = Validate;
		_startButton.Disabled = _isExporting;
		_closeButton = uIFragment.Get<TextButton>("CloseButton");
		_closeButton.Activating = Dismiss;
		_scaleField = uIFragment.Get<NumberField>("Scale");
		_scaleField.Value = 64m;
		_cancelButton = uIFragment.Get<TextButton>("CancelButton");
		_cancelButton.Activating = CancelExport;
	}

	protected internal override void Validate()
	{
		StartExport();
	}

	protected internal override void Dismiss()
	{
		if (!_isExporting)
		{
			Desktop.ClearLayer(4);
		}
	}

	private void CancelExport()
	{
		if (_cancellationTokenSource != null)
		{
			_cancellationTokenSource.Cancel();
			FinishExport();
		}
	}

	private void StartExport()
	{
		if (!_isExporting)
		{
			_isExporting = true;
			_cancellationTokenSource = new CancellationTokenSource();
			_iconSize = (int)_scaleField.Value;
			_statusLabel.Text = Desktop.Provider.GetText("ui.assetEditor.iconMassExportModal.starting");
			_statusLabel.Layout();
			_closeButton.Disabled = true;
			_closeButton.Layout();
			_startButton.Disabled = true;
			_startButton.Layout();
			PrepareQueue();
		}
	}

	private void PrepareQueue()
	{
		string path = _assetEditorOverlay.AssetTypeRegistry.AssetTypes["Item"].Path;
		List<AssetFile> assets = _assetEditorOverlay.Assets.GetAssets(AssetTreeFolder.Server);
		if (!_assetEditorOverlay.Assets.TryGetDirectoryIndex(path, out var index))
		{
			FinishExport();
			return;
		}
		for (int i = index + 1; i < assets.Count; i++)
		{
			AssetFile assetFile = assets[i];
			if (!assetFile.Path.StartsWith(path + "/"))
			{
				break;
			}
			if (!assetFile.IsDirectory)
			{
				_pathQueue.Enqueue(assetFile.Path);
			}
		}
		_totalCount = _pathQueue.Count;
		_completeCount = 0;
		_failedCount = 0;
		_skippedCount = 0;
		ExportNextIcon(_cancellationTokenSource.Token);
	}

	private void FinishExport()
	{
		_pathQueue.Clear();
		_isExporting = false;
		_statusLabel.Text = "";
		_startButton.Disabled = false;
		_closeButton.Disabled = false;
		_cancellationTokenSource = null;
		if (base.IsMounted)
		{
			Layout();
		}
	}

	private void ExportNextIcon(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}
		if (_pathQueue.Count == 0)
		{
			FinishExport();
			return;
		}
		string path = _pathQueue.Dequeue();
		_completeCount++;
		SchemaNode schema = _assetEditorOverlay.AssetTypeRegistry.AssetTypes["Item"].Schema;
		_assetEditorOverlay.Backend.FetchJsonAssetWithParents(new AssetReference("Item", path), delegate(Dictionary<string, TrackedAsset> data, FormattedMessage err)
		{
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Expected O, but got Unknown
			if (err != null)
			{
				_failedCount++;
				ExportNextIcon(cancellationToken);
			}
			else
			{
				JObject val = (JObject)data[path].Data;
				if (val["Icon"] == null || val["IconProperties"] == null || !((string)val["Icon"]).StartsWith("Icons/ItemsGenerated/"))
				{
					_skippedCount++;
					Logger.Info("Skipping {0} because it doesn't have a generated icon setup.", path);
					ExportNextIcon(cancellationToken);
				}
				else
				{
					_assetEditorOverlay.ApplyAssetInheritance(schema, val, data, schema);
					ExportNextIcon(cancellationToken);
				}
			}
		});
	}

	public void Open()
	{
		Desktop.SetLayer(4, this);
	}
}
