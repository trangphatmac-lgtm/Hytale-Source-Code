using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Editor;

internal class SourceEditor : Element
{
	private readonly AssetEditorOverlay _assetEditorOverlay;

	public readonly WebCodeEditor CodeEditor;

	private TextButton _applyButton;

	private TextButton _discardButton;

	private Label _errorMessageLabel;

	private Label _currentAssetNameLabel;

	private Label _currentAssetTypeLabel;

	private AssetReference _assetReference;

	public bool HasUnsavedChanges { get; private set; }

	public SourceEditor(AssetEditorOverlay assetEditorOverlay)
		: base(assetEditorOverlay.Desktop, null)
	{
		_assetEditorOverlay = assetEditorOverlay;
		_layoutMode = LayoutMode.Top;
		FlexWeight = 1;
		CodeEditor = new WebCodeEditor(assetEditorOverlay.Interface, assetEditorOverlay.Desktop, null)
		{
			FlexWeight = 1,
			ValueChanged = OnChange
		};
	}

	public void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("AssetEditor/SourceEditor.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_applyButton = uIFragment.Get<TextButton>("ApplyButton");
		_applyButton.Activating = delegate
		{
			if (ApplyChanges())
			{
				_assetEditorOverlay.Backend.SaveUnsavedChanges();
			}
		};
		_discardButton = uIFragment.Get<TextButton>("DiscardButton");
		_discardButton.Activating = Discard;
		_currentAssetNameLabel = uIFragment.Get<Group>("CurrentAsset").Find<Label>("AssetName");
		_currentAssetTypeLabel = uIFragment.Get<Group>("CurrentAsset").Find<Label>("AssetType");
		_errorMessageLabel = uIFragment.Get<Label>("ErrorMessage");
		Add(CodeEditor);
	}

	protected override void OnUnmounted()
	{
		CodeEditor.Value = "";
		HasUnsavedChanges = false;
		_errorMessageLabel.Visible = false;
	}

	public void Setup(string value, WebCodeEditor.EditorLanguage language, AssetReference assetReference)
	{
		HasUnsavedChanges = false;
		_applyButton.Disabled = true;
		_discardButton.Disabled = true;
		_errorMessageLabel.Visible = false;
		_assetReference = assetReference;
		_currentAssetNameLabel.Text = _assetEditorOverlay.GetAssetIdFromReference(assetReference);
		_currentAssetTypeLabel.Text = _assetEditorOverlay.AssetTypeRegistry.AssetTypes[assetReference.Type].Name;
		CodeEditor.Language = language;
		CodeEditor.Value = value;
	}

	private void OnChange()
	{
		if (!HasUnsavedChanges)
		{
			HasUnsavedChanges = true;
			_applyButton.Disabled = false;
			_discardButton.Disabled = false;
			_applyButton.Parent.Layout();
		}
		if (_errorMessageLabel.Visible)
		{
			_errorMessageLabel.Visible = false;
			Layout();
		}
	}

	public bool ApplyChanges()
	{
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Expected O, but got Unknown
		if (!HasUnsavedChanges)
		{
			return false;
		}
		_errorMessageLabel.Visible = false;
		JObject val = null;
		AssetTypeConfig assetTypeConfig = _assetEditorOverlay.AssetTypeRegistry.AssetTypes[_assetEditorOverlay.CurrentAsset.Type];
		if (CodeEditor.Language == WebCodeEditor.EditorLanguage.Json)
		{
			try
			{
				JsonUtils.ValidateJson(CodeEditor.Value);
				val = JObject.Parse(CodeEditor.Value);
			}
			catch (JsonReaderException)
			{
				_errorMessageLabel.Text = Desktop.Provider.GetText("ui.assetEditor.errors.invalidJson");
				_errorMessageLabel.Visible = true;
				Layout();
				return false;
			}
			if (assetTypeConfig.HasIdField && (string)val["Id"] != _assetEditorOverlay.GetAssetIdFromReference(_assetEditorOverlay.CurrentAsset))
			{
				_errorMessageLabel.Text = Desktop.Provider.GetText("ui.assetEditor.errors.idImmutableInSourceEditor");
				_errorMessageLabel.Visible = true;
				Layout();
				return false;
			}
		}
		HasUnsavedChanges = false;
		_applyButton.Disabled = true;
		_discardButton.Disabled = true;
		Layout();
		if (CodeEditor.Language == WebCodeEditor.EditorLanguage.Json)
		{
			if (assetTypeConfig.Schema != null)
			{
				SchemaNode schemaNode = _assetEditorOverlay.ResolveSchemaInCurrentContext(assetTypeConfig.Schema);
				_assetEditorOverlay.TryResolveTypeSchemaInCurrentContext((JToken)(object)val, ref schemaNode);
				_assetEditorOverlay.UpdateJsonAsset(_assetReference.FilePath, val, schemaNode.RebuildCaches);
			}
			else
			{
				_assetEditorOverlay.UpdateJsonAsset(_assetReference.FilePath, val, new AssetEditorRebuildCaches());
			}
		}
		else
		{
			_assetEditorOverlay.UpdateTextAsset(_assetReference.FilePath, CodeEditor.Value);
		}
		return true;
	}

	public void Discard()
	{
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Expected O, but got Unknown
		if (HasUnsavedChanges)
		{
			HasUnsavedChanges = false;
			_applyButton.Disabled = true;
			_discardButton.Disabled = true;
			_errorMessageLabel.Visible = false;
			Layout();
			if (CodeEditor.Language == WebCodeEditor.EditorLanguage.Json)
			{
				CodeEditor.Value = ((object)(JObject)_assetEditorOverlay.TrackedAssets[_assetReference.FilePath].Data).ToString();
			}
			else
			{
				CodeEditor.Value = _assetEditorOverlay.TrackedAssets[_assetReference.FilePath].Data.ToString();
			}
		}
	}
}
