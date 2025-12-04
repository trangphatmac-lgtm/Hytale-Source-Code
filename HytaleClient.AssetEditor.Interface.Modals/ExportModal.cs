using System;
using System.Collections.Generic;
using System.IO;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using HytaleClient.Protocol;
using NLog;

namespace HytaleClient.AssetEditor.Interface.Modals;

internal class ExportModal : Element
{
	private class ExportModalEntry : Element
	{
		private readonly ExportModal _exportModal;

		private readonly bool _hasDiagnostics;

		private readonly Button _button;

		private Button.ButtonStyle _defaultStyle;

		private Button.ButtonStyle _selectedStyle;

		public readonly AssetReference Asset;

		public ExportModalEntry(Element parent, ExportModal exportModal, AssetReference asset, AssetTypeConfig assetTypeConfig, AssetInfo assetInfo)
			: base(parent.Desktop, parent)
		{
			_exportModal = exportModal;
			Asset = asset;
			Desktop.Provider.TryGetDocument("AssetEditor/ExportEntry.ui", out var document);
			_defaultStyle = document.ResolveNamedValue<Button.ButtonStyle>(Desktop.Provider, "DefaultStyle");
			_selectedStyle = document.ResolveNamedValue<Button.ButtonStyle>(Desktop.Provider, "SelectedStyle");
			UIFragment uIFragment = document.Instantiate(Desktop, this);
			Label label = uIFragment.Get<Label>("NameLabel");
			_button = uIFragment.Get<Button>("Button");
			_button.Activating = OnButtonActivating;
			_button.DoubleClicking = OnButtonDoubleClicking;
			_button.Style = (_exportModal._selectedAssets.Contains(Asset.FilePath) ? _selectedStyle : _defaultStyle);
			uIFragment.Get<Label>("UsernameLabel").Text = assetInfo.LastModificationUsername;
			uIFragment.Get<Label>("DateLabel").Text = Desktop.Provider.FormatRelativeTime(DateTimeOffset.FromUnixTimeMilliseconds(assetInfo.LastModificationDate).LocalDateTime);
			uIFragment.Get<TextButton>("DiscardButton").Activating = delegate
			{
				exportModal._assetEditorOverlay.Backend.DiscardChanges(new TimestampedAssetReference(asset.FilePath, null));
			};
			UInt32Color value;
			if (assetInfo.IsDeleted)
			{
				value = document.ResolveNamedValue<UInt32Color>(Desktop.Provider, "DeletedColor");
				_button.TooltipText = assetTypeConfig.Name + " - " + Desktop.Provider.GetText("ui.assetEditor.exportModal.tooltips.deleted");
			}
			else if (assetInfo.IsNew)
			{
				value = document.ResolveNamedValue<UInt32Color>(Desktop.Provider, "NewColor");
				_button.TooltipText = assetTypeConfig.Name + " - " + Desktop.Provider.GetText("ui.assetEditor.exportModal.tooltips.new");
			}
			else if (assetInfo.OldPath != null)
			{
				value = document.ResolveNamedValue<UInt32Color>(Desktop.Provider, "RenamedColor");
				_button.TooltipText = assetTypeConfig.Name + " - " + Desktop.Provider.GetText("ui.assetEditor.exportModal.tooltips.renamed", new Dictionary<string, string> { { "oldPath", assetInfo.OldPath } });
			}
			else
			{
				value = document.ResolveNamedValue<UInt32Color>(Desktop.Provider, "DefaultColor");
				_button.TooltipText = assetTypeConfig.Name + " - " + Desktop.Provider.GetText("ui.assetEditor.exportModal.tooltips.changed");
			}
			List<Label.LabelSpan> list = new List<Label.LabelSpan>();
			string fileName = Path.GetFileName(asset.FilePath);
			string extension = Path.GetExtension(asset.FilePath);
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(asset.FilePath);
			list.Add(new Label.LabelSpan
			{
				Color = value,
				Text = asset.FilePath.Substring(0, asset.FilePath.Length - fileName.Length)
			});
			list.Add(new Label.LabelSpan
			{
				Color = value,
				IsBold = true,
				Text = fileNameWithoutExtension
			});
			list.Add(new Label.LabelSpan
			{
				Color = value,
				Text = extension
			});
			label.TextSpans = list;
			uIFragment.Get<Group>("Icon").Background = assetTypeConfig.Icon;
			if (exportModal._assetEditorOverlay.Diagnostics.TryGetValue(asset.FilePath, out var value2))
			{
				if (value2.Errors != null && value2.Errors.Length != 0)
				{
					_hasDiagnostics = true;
					Group group = uIFragment.Get<Group>("DiagnosticsIcon");
					group.Visible = true;
					group.Background = new PatchStyle("AssetEditor/ErrorIcon.png");
				}
				else if (value2.Warnings != null && value2.Warnings.Length != 0)
				{
					_hasDiagnostics = true;
					Group group2 = uIFragment.Get<Group>("DiagnosticsIcon");
					group2.Visible = true;
					group2.Background = new PatchStyle("AssetEditor/WarningIcon.png");
				}
			}
		}

		public void OnStateChanged()
		{
			_button.Style = (_exportModal._selectedAssets.Contains(Asset.FilePath) ? _selectedStyle : _defaultStyle);
			_button.Layout();
		}

		private void OnButtonActivating()
		{
			if (!_exportModal._selectedAssets.Contains(Asset.FilePath))
			{
				_exportModal._selectedAssets.Add(Asset.FilePath);
			}
			else
			{
				_exportModal._selectedAssets.Remove(Asset.FilePath);
			}
			OnStateChanged();
			_exportModal.UpdateExportButtonState();
		}

		private void OnButtonDoubleClicking()
		{
			_exportModal._assetEditorOverlay.OpenExistingAsset(Asset, bringAssetIntoAssetTreeView: true);
		}

		protected override void OnMouseEnter()
		{
			if (!_hasDiagnostics)
			{
				return;
			}
			AssetEditorOverlay assetEditorOverlay = _exportModal._assetEditorOverlay;
			if (!assetEditorOverlay.Diagnostics.TryGetValue(Asset.FilePath, out var value))
			{
				return;
			}
			List<Label.LabelSpan> list = new List<Label.LabelSpan>();
			if (value.Errors != null && value.Errors.Length != 0)
			{
				list.Add(new Label.LabelSpan
				{
					Text = Desktop.Provider.GetText("ui.assetEditor.diagnosticsTooltip.errors"),
					IsBold = true
				});
				AssetDiagnosticMessage[] errors = value.Errors;
				for (int i = 0; i < errors.Length; i++)
				{
					AssetDiagnosticMessage assetDiagnosticMessage = errors[i];
					list.Add(new Label.LabelSpan
					{
						Text = "\n- " + assetDiagnosticMessage.Property.ToString() + ": " + assetDiagnosticMessage.Message
					});
				}
			}
			if (value.Warnings != null && value.Warnings.Length != 0)
			{
				if (list.Count > 0)
				{
					list.Add(new Label.LabelSpan
					{
						Text = "\n\n"
					});
				}
				list.Add(new Label.LabelSpan
				{
					Text = Desktop.Provider.GetText("ui.assetEditor.diagnosticsTooltip.warnings"),
					IsBold = true
				});
				AssetDiagnosticMessage[] warnings = value.Warnings;
				for (int j = 0; j < warnings.Length; j++)
				{
					AssetDiagnosticMessage assetDiagnosticMessage2 = warnings[j];
					list.Add(new Label.LabelSpan
					{
						Text = "\n- " + assetDiagnosticMessage2.Property.ToString() + ": " + assetDiagnosticMessage2.Message
					});
				}
			}
			TextTooltipLayer textTooltipLayer = assetEditorOverlay.TextTooltipLayer;
			textTooltipLayer.TextSpans = list;
			textTooltipLayer.Start();
		}

		protected override void OnMouseLeave()
		{
			_exportModal._assetEditorOverlay.TextTooltipLayer.Stop();
		}

		public override Element HitTest(Point position)
		{
			return _anchoredRectangle.Contains(position) ? (base.HitTest(position) ?? this) : null;
		}
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly AssetEditorOverlay _assetEditorOverlay;

	private Group _container;

	private Group _entriesContainer;

	private TextButton _exportButton;

	private CheckBox _discardCheckBox;

	private HashSet<string> _selectedAssets = new HashSet<string>();

	public ExportModal(AssetEditorOverlay assetEditorOverlay)
		: base(assetEditorOverlay.Desktop, null)
	{
		_assetEditorOverlay = assetEditorOverlay;
	}

	public void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("AssetEditor/ExportModal.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_container = uIFragment.Get<Group>("Container");
		_entriesContainer = uIFragment.Get<Group>("Entries");
		_discardCheckBox = uIFragment.Get<CheckBox>("DiscardCheckBox");
		_exportButton = uIFragment.Get<TextButton>("ExportButton");
		_exportButton.Activating = Validate;
		uIFragment.Get<TextButton>("CancelButton").Activating = Dismiss;
		uIFragment.Get<TextButton>("SelectAllButton").Activating = OnActivateSelectAll;
		uIFragment.Get<TextButton>("DeselectAllButton").Activating = OnActivateDeselectAll;
		if (base.IsMounted)
		{
			Setup();
		}
	}

	protected override void OnMounted()
	{
		_assetEditorOverlay.AttachNotifications(this);
		_assetEditorOverlay.Backend.FetchLastModifiedAssets();
		_assetEditorOverlay.Backend.UpdateSubscriptionToModifiedAssetsUpdates(subscribe: true);
	}

	protected override void OnUnmounted()
	{
		_assetEditorOverlay.Backend?.UpdateSubscriptionToModifiedAssetsUpdates(subscribe: false);
		_assetEditorOverlay.ReparentNotifications();
	}

	public override Element HitTest(Point position)
	{
		return base.HitTest(position) ?? this;
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		base.OnMouseButtonUp(evt, activate);
		if (activate && !_container.AnchoredRectangle.Contains(Desktop.MousePosition))
		{
			Dismiss();
		}
	}

	private void OnActivateDeselectAll()
	{
		_selectedAssets.Clear();
		foreach (Element child in _entriesContainer.Children)
		{
			ExportModalEntry exportModalEntry = (ExportModalEntry)child;
			exportModalEntry.OnStateChanged();
		}
		UpdateExportButtonState();
	}

	private void OnActivateSelectAll()
	{
		_selectedAssets.Clear();
		foreach (Element child in _entriesContainer.Children)
		{
			ExportModalEntry exportModalEntry = (ExportModalEntry)child;
			_selectedAssets.Add(exportModalEntry.Asset.FilePath);
			exportModalEntry.OnStateChanged();
		}
		UpdateExportButtonState();
	}

	protected internal override void Dismiss()
	{
		Desktop.ClearLayer(4);
	}

	protected internal override void Validate()
	{
		if (_assetEditorOverlay.Backend.IsExportingAssets || _selectedAssets.Count == 0)
		{
			return;
		}
		List<AssetReference> list = new List<AssetReference>();
		foreach (string selectedAsset in _selectedAssets)
		{
			_assetEditorOverlay.AssetTypeRegistry.TryGetAssetTypeFromPath(selectedAsset, out var assetType);
			list.Add(new AssetReference(assetType, selectedAsset));
		}
		if (_discardCheckBox.Value)
		{
			_assetEditorOverlay.Backend.ExportAndDiscardAssets(list);
		}
		else
		{
			_assetEditorOverlay.Backend.ExportAssets(list);
		}
	}

	public void UpdateExportButtonState()
	{
		_exportButton.Disabled = _assetEditorOverlay.Backend.IsExportingAssets || _selectedAssets.Count == 0;
		if (base.IsMounted)
		{
			_exportButton.Layout();
		}
	}

	public void ResetState()
	{
		_entriesContainer.Clear();
		_selectedAssets.Clear();
		_exportButton.Disabled = false;
	}

	public void Setup()
	{
		_entriesContainer.Clear();
		_selectedAssets.Clear();
		AssetInfo[] lastModifiedAssets = _assetEditorOverlay.Backend.GetLastModifiedAssets();
		foreach (AssetInfo val in lastModifiedAssets)
		{
			if (!_assetEditorOverlay.AssetTypeRegistry.TryGetAssetTypeFromPath(val.Path, out var assetType) || !_assetEditorOverlay.AssetTypeRegistry.AssetTypes.TryGetValue(assetType, out var value))
			{
				Logger.Info("Asset type for file in export list not found: {0}", val.Path);
			}
			else
			{
				new ExportModalEntry(_entriesContainer, this, new AssetReference(assetType, val.Path), value, val);
			}
		}
		UpdateExportButtonState();
		if (base.IsMounted)
		{
			_entriesContainer.Layout();
		}
	}

	public void Open()
	{
		Setup();
		Desktop.SetLayer(4, this);
	}
}
