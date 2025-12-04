using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Config;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.AssetEditor.Interface.Elements;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using HytaleClient.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Modals;

internal class CreateAssetModal : Element
{
	private enum Tab
	{
		Properties,
		Source
	}

	private readonly AssetEditorOverlay _assetEditorOverlay;

	private string _assetType;

	private Action<string, FormattedMessage> _assetCreatedCallback;

	private Group _container;

	private Label _titleLabel;

	private Label _errorLabel;

	private Label _sourceErrorLabel;

	private TextField _idTextInput;

	private AssetSelectorDropdown _copyAssetDropdown;

	private DropdownBox _assetTypeDropdown;

	private TabNavigation _tabs;

	private FileDropdownBox _folderSelector;

	private TextButton _saveButton;

	private Group _propertyList;

	private Group _sourceEditor;

	private JObject _json;

	private Tab _activeTab = Tab.Properties;

	private string _buttonId;

	public readonly WebCodeEditor CodeEditor;

	private List<TextButton> _customButtons = new List<TextButton>();

	public CreateAssetModal(AssetEditorOverlay assetEditorOverlay)
		: base(assetEditorOverlay.Desktop, null)
	{
		_assetEditorOverlay = assetEditorOverlay;
		CodeEditor = new WebCodeEditor(assetEditorOverlay.Interface, Desktop, null)
		{
			ValueChanged = OnCodeEditorDidChange
		};
	}

	public void Build()
	{
		Clear();
		CodeEditor.Parent?.Remove(CodeEditor);
		Desktop.Provider.TryGetDocument("AssetEditor/CreateAssetModal.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_tabs = uIFragment.Get<TabNavigation>("Tabs");
		_tabs.SelectedTabChanged = delegate
		{
			OnChangeTab((Tab)Enum.Parse(typeof(Tab), _tabs.SelectedTab));
		};
		_tabs.SelectedTab = _activeTab.ToString();
		_container = uIFragment.Get<Group>("Container");
		_titleLabel = uIFragment.Get<Label>("Title");
		_errorLabel = uIFragment.Get<Label>("ErrorMessage");
		_sourceErrorLabel = uIFragment.Get<Label>("SourceErrorMessage");
		_propertyList = uIFragment.Get<Group>("PropertyList");
		_propertyList.Visible = _activeTab == Tab.Properties;
		_sourceEditor = uIFragment.Get<Group>("SourceEditor");
		_sourceEditor.Visible = _activeTab == Tab.Source;
		_idTextInput = uIFragment.Get<TextField>("AssetIdInput");
		_idTextInput.Validating = Validate;
		_assetTypeDropdown = uIFragment.Get<DropdownBox>("AssetTypeDropdownBox");
		_assetTypeDropdown.ValueChanged = delegate
		{
			Open(_assetTypeDropdown.Value);
			Layout();
		};
		_copyAssetDropdown = new AssetSelectorDropdown(Desktop, uIFragment.Get<Group>("CopyAssetGroup"), _assetEditorOverlay)
		{
			ValueChanged = delegate
			{
				if (_copyAssetDropdown.Value != null)
				{
					LoadAssetToCopy();
				}
			},
			FlexWeight = 1,
			AssetType = _assetType,
			Style = document.ResolveNamedValue<FileDropdownBoxStyle>(Desktop.Provider, "FileDropdownBoxStyle")
		};
		_folderSelector = new FileDropdownBox(Desktop, uIFragment.Get<Group>("FolderGroup"), "AssetEditor/FileSelector.ui", () => GetFolders())
		{
			FlexWeight = 1,
			SelectedFiles = new HashSet<string>(),
			AllowDirectorySelection = true,
			AllowDirectoryCreation = true,
			DropdownToggled = OnToggleDropdown,
			CreatingDirectory = CreateDirectory,
			Style = _assetEditorOverlay.ConfigEditor.FileDropdownBoxStyle
		};
		_saveButton = uIFragment.Get<TextButton>("SaveButton");
		_saveButton.Activating = Validate;
		uIFragment.Get<TextButton>("CancelButton").Activating = Dismiss;
		uIFragment.Get<TextButton>("CopyAssetClearButton").Activating = delegate
		{
			_copyAssetDropdown.Value = null;
			_json = null;
			CodeEditor.Value = "{}";
			_propertyList.Clear();
			_propertyList.Layout();
		};
		_sourceEditor.Add(CodeEditor);
	}

	private void CreateDirectory(string name, Action<FormattedMessage> callback)
	{
		AssetTypeConfig assetTypeConfig = _assetEditorOverlay.AssetTypeRegistry.AssetTypes[_assetType];
		string path = assetTypeConfig.Path + _folderSelector.CurrentPath.TrimEnd(new char[1] { '/' }) + "/" + name;
		_assetEditorOverlay.Backend.CreateDirectory(path, applyLocally: false, delegate(string createdPath, FormattedMessage error)
		{
			callback(error);
			if (error == null)
			{
				_folderSelector.Setup(_folderSelector.CurrentPath, GetFolders());
			}
		});
	}

	private void OnToggleDropdown()
	{
		_folderSelector.Setup("/", GetFolders("/"));
	}

	private List<FileSelector.File> GetFolders(string currentPath = null)
	{
		List<FileSelector.File> list = new List<FileSelector.File>();
		string text = _folderSelector.SearchQuery.Trim();
		if (text != "" && text.Length < 3)
		{
			return list;
		}
		AssetTypeConfig assetTypeConfig = _assetEditorOverlay.AssetTypeRegistry.AssetTypes[_assetType];
		if (assetTypeConfig.AssetTree == AssetTreeFolder.Cosmetics)
		{
			throw new Exception("Unsupported");
		}
		string path = assetTypeConfig.Path;
		currentPath = (path + (currentPath ?? _folderSelector.CurrentPath)).Trim(new char[1] { '/' });
		string[] array = currentPath.Trim(new char[1] { '/' }).Split(new char[1] { '/' });
		string[] array2 = (from q in text.ToLower().Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
			select q.Trim()).ToArray();
		foreach (AssetFile asset in _assetEditorOverlay.Assets.GetAssets(assetTypeConfig.AssetTree))
		{
			if (!asset.IsDirectory)
			{
				continue;
			}
			if (array2.Length != 0)
			{
				if (!asset.Path.StartsWith(path + "/") && asset.Path != path)
				{
					continue;
				}
				bool flag = true;
				string text2 = asset.PathElements.Last().ToLowerInvariant();
				string[] array3 = array2;
				foreach (string value in array3)
				{
					if (!text2.Contains(value))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					list.Add(new FileSelector.File
					{
						IsDirectory = true,
						Name = asset.Path.Replace(path + "/", "")
					});
				}
			}
			else if (asset.PathElements.Length == array.Length + 1 && asset.Path.StartsWith(currentPath + "/"))
			{
				list.Add(new FileSelector.File
				{
					IsDirectory = true,
					Name = asset.PathElements.Last()
				});
			}
		}
		return list;
	}

	private void TryBuildPropertyList()
	{
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Invalid comparison between Unknown and I4
		_propertyList.Clear();
		AssetTypeConfig assetTypeConfig = _assetEditorOverlay.AssetTypeRegistry.AssetTypes[_assetType];
		if (assetTypeConfig.AssetTree == AssetTreeFolder.Common || assetTypeConfig.Schema == null)
		{
			if (base.IsMounted)
			{
				_propertyList.Layout();
			}
			return;
		}
		Desktop.Provider.TryGetDocument("AssetEditor/CreateAssetModalPropertyEntry.ui", out var doc);
		SchemaNode schemaNode = _assetEditorOverlay.ResolveSchema(assetTypeConfig.Schema, assetTypeConfig.Schema);
		if (schemaNode.TypeSchemas != null)
		{
			JToken obj2 = _json[schemaNode.TypePropertyKey];
			if (obj2 != null && (int)obj2.Type == 8)
			{
				_assetEditorOverlay.TryResolveTypeSchema((string)_json[schemaNode.TypePropertyKey], ref schemaNode, schemaNode);
			}
			else if (schemaNode.DefaultTypeSchema != null)
			{
				_assetEditorOverlay.TryResolveTypeSchema(schemaNode.DefaultTypeSchema, ref schemaNode, schemaNode);
			}
		}
		if (schemaNode != null && schemaNode.Type == SchemaNode.NodeType.Object && schemaNode.Properties != null)
		{
			AddProperties(schemaNode, _json, "");
		}
		if (base.IsMounted)
		{
			_propertyList.Layout();
		}
		void Add(string path, bool isUnknown)
		{
			UIFragment fragment = doc.Instantiate(Desktop, _propertyList);
			fragment.Get<TextButton>("RemoveButton").Activating = delegate
			{
				JsonUtils.RemoveProperty(_json, PropertyPath.FromString(path));
				_propertyList.Remove(fragment.RootElements[0]);
				_propertyList.Layout();
				CodeEditor.Value = ((object)_json).ToString();
			};
			fragment.Get<Label>("PropertyLabel").Text = (isUnknown ? ("Unknown: " + path) : path);
		}
		void AddProperties(SchemaNode node, JObject obj, string path)
		{
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_0076: Invalid comparison between Unknown and I4
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a7: Expected O, but got Unknown
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, SchemaNode> property in node.Properties)
			{
				JToken val = obj[property.Key];
				if (val != null)
				{
					list.Add(property.Key);
					Add(path + property.Key, isUnknown: false);
					if (property.Value.Type == SchemaNode.NodeType.Object && (int)val.Type == 1)
					{
						AddProperties(property.Value, (JObject)val, path + property.Key + ".");
					}
				}
			}
			foreach (JProperty item in obj.Properties())
			{
				if (!list.Contains(item.Name))
				{
					Add(path + item.Name, isUnknown: true);
				}
			}
		}
	}

	private void OnChangeTab(Tab tab)
	{
		if (tab == _activeTab)
		{
			return;
		}
		_sourceErrorLabel.Visible = false;
		if (tab == Tab.Properties)
		{
			try
			{
				JsonUtils.ValidateJson(CodeEditor.Value);
				JObject json = JObject.Parse(CodeEditor.Value);
				_json = json;
				TryBuildPropertyList();
			}
			catch (JsonReaderException)
			{
				_sourceErrorLabel.Visible = true;
				Layout();
				return;
			}
		}
		_activeTab = tab;
		_propertyList.Visible = _activeTab == Tab.Properties;
		_sourceEditor.Visible = _activeTab == Tab.Source;
		Layout();
	}

	protected override void OnMounted()
	{
		Desktop.FocusElement(_idTextInput);
		_assetEditorOverlay.AttachNotifications(this);
	}

	protected override void OnUnmounted()
	{
		_json = null;
		_idTextInput.Value = "";
		_assetType = null;
		_errorLabel.Visible = false;
		_copyAssetDropdown.Value = null;
		_sourceErrorLabel.Visible = false;
		_assetEditorOverlay.ReparentNotifications();
	}

	private void LoadAssetToCopy()
	{
		if (!_assetEditorOverlay.Assets.TryGetPathForAssetId(_assetType, _copyAssetDropdown.Value, out var filePath))
		{
			return;
		}
		_assetEditorOverlay.Backend.FetchAsset(new AssetReference(_assetType, filePath), delegate(object asset, FormattedMessage error)
		{
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Expected O, but got Unknown
			if (asset == null)
			{
				_assetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, error ?? FormattedMessage.FromMessageId("ui.assetEditor.errors.errorOccurredFetching"));
			}
			else
			{
				_json = (JObject)asset;
				CodeEditor.Value = asset.ToString();
				TryBuildPropertyList();
			}
		});
	}

	private void OnCodeEditorDidChange()
	{
		if (_sourceErrorLabel.Visible)
		{
			_sourceErrorLabel.Visible = false;
			Layout();
		}
	}

	public void Open(string assetType, string assetToCopyPath = null, string path = null, string id = null, JObject json = null, Action<string, FormattedMessage> callback = null)
	{
		//IL_0339: Unknown result type (might be due to invalid IL or missing references)
		AssetTypeConfig assetTypeConfig = _assetEditorOverlay.AssetTypeRegistry.AssetTypes[assetType];
		_buttonId = null;
		_assetCreatedCallback = callback;
		_assetType = assetType;
		_copyAssetDropdown.AssetType = assetType;
		_idTextInput.Value = id ?? "";
		_titleLabel.Text = Desktop.Provider.GetText("ui.assetEditor.createAssetModal.title", new Dictionary<string, string> { { "assetType", assetTypeConfig.Name } });
		_folderSelector.Parent.Visible = assetTypeConfig.AssetTree != AssetTreeFolder.Cosmetics;
		foreach (TextButton customButton2 in _customButtons)
		{
			customButton2.Parent.Remove(customButton2);
		}
		_customButtons.Clear();
		if (assetTypeConfig.CreateButtons != null && _assetEditorOverlay.Backend.IsEditingRemotely)
		{
			Desktop.Provider.TryGetDocument("AssetEditor/CustomButton.ui", out var document);
			foreach (AssetTypeConfig.Button customButton in assetTypeConfig.CreateButtons)
			{
				UIFragment uIFragment = document.Instantiate(Desktop, null);
				TextButton textButton = uIFragment.Get<TextButton>("Button");
				textButton.Text = Desktop.Provider.GetText(customButton.TextId);
				textButton.Activating = delegate
				{
					_buttonId = customButton.Action;
					Validate();
				};
				_saveButton.Parent.Add(textButton, 1);
				_customButtons.Add(textButton);
			}
		}
		if (path == null)
		{
			path = ((assetToCopyPath == null) ? "" : Path.GetDirectoryName(assetToCopyPath.Replace(assetTypeConfig.Path + "/", "")).Replace(Path.DirectorySeparatorChar, '/'));
		}
		_folderSelector.SelectedFiles = new HashSet<string> { path };
		List<DropdownBox.DropdownEntryInfo> list = new List<DropdownBox.DropdownEntryInfo>();
		foreach (KeyValuePair<string, AssetTypeConfig> assetType2 in _assetEditorOverlay.AssetTypeRegistry.AssetTypes)
		{
			if (assetType2.Value.AssetTree != AssetTreeFolder.Common && !assetType2.Value.IsVirtual)
			{
				list.Add(new DropdownBox.DropdownEntryInfo(assetType2.Value.Name, assetType2.Key));
			}
		}
		list.Sort((DropdownBox.DropdownEntryInfo a, DropdownBox.DropdownEntryInfo b) => string.Compare(a.Label, b.Label, StringComparison.InvariantCulture));
		_assetTypeDropdown.Entries = list;
		_assetTypeDropdown.Value = assetType;
		_json = (JObject)(((object)assetTypeConfig.BaseJsonAsset) ?? ((object)new JObject()));
		TryBuildPropertyList();
		CodeEditor.Value = ((object)_json).ToString();
		if (assetToCopyPath != null)
		{
			_copyAssetDropdown.Value = _assetEditorOverlay.GetAssetIdFromReference(new AssetReference(assetType, assetToCopyPath));
			LoadAssetToCopy();
		}
		else if (json != null)
		{
			_json = json;
			CodeEditor.Value = ((object)json).ToString();
			TryBuildPropertyList();
		}
		if (!base.IsMounted)
		{
			Desktop.SetLayer(4, this);
		}
	}

	private void SetError(string message)
	{
		_errorLabel.Text = message;
		_errorLabel.Visible = true;
		Layout();
	}

	private void SetError(FormattedMessage formattedMessage)
	{
		_errorLabel.TextSpans = FormattedMessageConverter.GetLabelSpans(formattedMessage, _assetEditorOverlay.Interface);
		_errorLabel.Visible = true;
		Layout();
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

	protected internal override void Dismiss()
	{
		Desktop.ClearLayer(4);
	}

	protected internal override void Validate()
	{
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		_errorLabel.Visible = false;
		if (_activeTab == Tab.Source)
		{
			try
			{
				JsonUtils.ValidateJson(CodeEditor.Value);
				JObject json = JObject.Parse(CodeEditor.Value);
				_json = json;
				TryBuildPropertyList();
			}
			catch (JsonReaderException)
			{
				_sourceErrorLabel.Visible = true;
				Layout();
				return;
			}
		}
		string text = _idTextInput.Value.Trim();
		if (!_assetEditorOverlay.ValidateAssetId(text, out var errorMessage))
		{
			SetError(errorMessage);
			return;
		}
		if (!_assetEditorOverlay.AssetTypeRegistry.AssetTypes.ContainsKey(_assetType))
		{
			SetError(Desktop.Provider.GetText("ui.assetEditor.createAssetModal.errors.invalidAssetType"));
			return;
		}
		AssetTypeConfig assetTypeConfig = _assetEditorOverlay.AssetTypeRegistry.AssetTypes[_assetType];
		string assetType = _assetType;
		JObject val2 = (JObject)(((object)_json) ?? ((object)new JObject()));
		if (assetTypeConfig.HasIdField)
		{
			val2["Id"] = JToken.op_Implicit(text);
		}
		string filePath;
		if (assetTypeConfig.AssetTree == AssetTreeFolder.Cosmetics)
		{
			filePath = assetTypeConfig.Path + "#" + text;
		}
		else
		{
			filePath = AssetPathUtils.CombinePaths(assetTypeConfig.Path, _folderSelector.SelectedFiles.FirstOrDefault() ?? "");
			filePath = AssetPathUtils.CombinePaths(filePath, text + assetTypeConfig.FileExtension);
		}
		if (_assetEditorOverlay.Assets.TryGetPathForAssetId(assetType, text, out var filePath2, ignoreCase: true) && _assetEditorOverlay.Assets.TryGetAsset(filePath2, out var _))
		{
			SetError(Desktop.Provider.GetText("ui.assetEditor.createAssetModal.errors.existingId"));
			return;
		}
		_assetEditorOverlay.CreateAsset(new AssetReference(assetType, filePath), val2, _buttonId, delegate(FormattedMessage err)
		{
			_assetCreatedCallback?.Invoke(filePath, err);
			if (base.IsMounted)
			{
				if (err != null)
				{
					SetError(err);
				}
				else
				{
					Dismiss();
				}
			}
		});
	}
}
