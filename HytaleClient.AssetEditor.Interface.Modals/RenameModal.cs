using System.IO;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.AssetEditor.Interface.Modals;

internal class RenameModal : Element
{
	private AssetReference _assetReference;

	private string _directoryPath;

	private TextField _newIdInput;

	private Label _title;

	private Label _errorLabel;

	private Group _container;

	private Group _applyChangesLocallyContainer;

	private CheckBox _applyChangesLocally;

	private readonly AssetEditorOverlay _assetEditorOverlay;

	public RenameModal(AssetEditorOverlay assetEditorOverlay)
		: base(assetEditorOverlay.Desktop, null)
	{
		_assetEditorOverlay = assetEditorOverlay;
	}

	public void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("AssetEditor/RenameModal.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		uIFragment.Get<TextButton>("SaveButton").Activating = Validate;
		uIFragment.Get<TextButton>("CancelButton").Activating = Dismiss;
		_container = uIFragment.Get<Group>("Container");
		_title = uIFragment.Get<Label>("Title");
		_newIdInput = uIFragment.Get<TextField>("Input");
		_errorLabel = uIFragment.Get<Label>("ErrorMessage");
		_applyChangesLocallyContainer = uIFragment.Get<Group>("ApplyChangesLocallyContainer");
		_applyChangesLocally = uIFragment.Get<CheckBox>("ApplyChangesLocally");
	}

	public void OpenForAsset(AssetReference assetReference, bool displayApplyLocalChangeCheckBox = false)
	{
		_assetReference = assetReference;
		_directoryPath = null;
		_errorLabel.Visible = false;
		_applyChangesLocallyContainer.Visible = displayApplyLocalChangeCheckBox;
		_title.Text = Desktop.Provider.GetText("ui.assetEditor.renameModal.titleAsset");
		_newIdInput.PlaceholderText = Desktop.Provider.GetText("ui.assetEditor.renameModal.newId");
		Desktop.SetLayer(4, this);
		Desktop.FocusElement(_newIdInput);
		_newIdInput.Value = _assetEditorOverlay.GetAssetIdFromReference(assetReference);
		_newIdInput.SelectAll();
	}

	public void OpenForDirectory(string path, bool displayApplyLocalChangeCheckBox = false)
	{
		_directoryPath = path;
		_assetReference = AssetReference.None;
		_errorLabel.Visible = false;
		_applyChangesLocallyContainer.Visible = displayApplyLocalChangeCheckBox;
		_title.Text = Desktop.Provider.GetText("ui.assetEditor.renameModal.titleFolder");
		_newIdInput.PlaceholderText = Desktop.Provider.GetText("ui.assetEditor.renameModal.newName");
		Desktop.SetLayer(4, this);
		Desktop.FocusElement(_newIdInput);
		_newIdInput.Value = Path.GetFileName(path);
		_newIdInput.SelectAll();
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		base.OnMouseButtonUp(evt, activate);
		if (activate && !_container.AnchoredRectangle.Contains(Desktop.MousePosition))
		{
			Dismiss();
		}
	}

	private void ValidateAssetId()
	{
		string text = _newIdInput.Value.Trim();
		if (!_assetEditorOverlay.ValidateAssetId(text, out var errorMessage))
		{
			ShowError(errorMessage);
			return;
		}
		AssetTypeConfig assetTypeConfig = _assetEditorOverlay.AssetTypeRegistry.AssetTypes[_assetReference.Type];
		string text2;
		if (assetTypeConfig.AssetTree == AssetTreeFolder.Cosmetics)
		{
			text2 = assetTypeConfig.Path + "#" + text;
		}
		else
		{
			string[] array = _assetReference.FilePath.Split(new char[1] { '/' });
			string path = string.Join("/", array, 0, array.Length - 1);
			text2 = AssetPathUtils.CombinePaths(path, text + assetTypeConfig.FileExtension);
		}
		string path2 = text2.ToLowerInvariant();
		if (_assetEditorOverlay.Assets.TryGetAsset(path2, out var _, ignoreCase: true))
		{
			ShowError(Desktop.Provider.GetText("ui.assetEditor.createAssetModal.errors.existingId"));
			return;
		}
		_assetEditorOverlay.Backend.RenameAsset(_assetReference, text2, _applyChangesLocallyContainer.Visible && _applyChangesLocally.Value);
		Desktop.ClearLayer(4);
	}

	private void ValidateDirectoryName()
	{
		string text = _newIdInput.Value.Trim();
		if (text.Contains("/") || text.Contains("\\"))
		{
			ShowError(Desktop.Provider.GetText("ui.assetEditor.errors.directoryInvalidCharacters"));
			return;
		}
		string text2 = Path.GetDirectoryName(_directoryPath) + "/" + text;
		string path2 = text2.ToLowerInvariant();
		if (_assetEditorOverlay.Assets.TryGetAsset(path2, out var _, ignoreCase: true))
		{
			ShowError(Desktop.Provider.GetText("ui.assetEditor.errors.renameDirectoryExists"));
			return;
		}
		_assetEditorOverlay.Backend.RenameDirectory(_directoryPath, text2, _applyChangesLocally.Visible && _applyChangesLocally.Value, delegate(string path, FormattedMessage error)
		{
			if (error != null)
			{
				_assetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, error);
			}
			else
			{
				_assetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)1, Desktop.Provider.GetText("ui.assetEditor.messages.directoryCreated"));
			}
		});
		Desktop.ClearLayer(4);
	}

	protected internal override void Validate()
	{
		if (_directoryPath != null)
		{
			ValidateDirectoryName();
		}
		else
		{
			ValidateAssetId();
		}
	}

	private void ShowError(string message)
	{
		_errorLabel.Text = message;
		_errorLabel.Visible = true;
		Layout();
	}

	protected internal override void Dismiss()
	{
		Desktop.ClearLayer(4);
	}

	public override Element HitTest(Point position)
	{
		return base.HitTest(position) ?? this;
	}
}
