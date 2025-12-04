#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HytaleClient.AssetEditor.Backends;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Protocol;
using NLog;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class ConfigEditor : Element
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public readonly AssetEditorOverlay AssetEditorOverlay;

	public readonly IconExporterModal IconExporterModal;

	public readonly KeyModal KeyModal;

	private Label _errorLabel;

	private TextField _searchInput;

	private Group _container;

	private Group _propertiesContainer;

	private ValueEditor _valueEditor;

	private Panel _loadingOverlay;

	private Label _currentAssetNameLabel;

	private Label _currentAssetTypeLabel;

	public readonly List<TimelineEditor> MountedTimelineEditors = new List<TimelineEditor>();

	public string LastOpenedFileSelectorDirectory;

	private AssetTypeConfig _assetTypeConfig;

	private readonly List<ClientJsonUpdateCommand> _pendingUpdateCommands = new List<ClientJsonUpdateCommand>();

	private Dictionary<string, ConfigEditorState> _assetFileStates = new Dictionary<string, ConfigEditorState>();

	public ConfigEditorState State;

	private bool _isWaitingForBackend;

	private bool _scrollToOffsetFromStateAfterLayout;

	public Dictionary<string, string> Categories = new Dictionary<string, string>();

	public FileDropdownBoxStyle FileDropdownBoxStyle { get; private set; }

	public ColorPickerDropdownBoxStyle ColorPickerDropdownBoxStyle { get; private set; }

	public CheckBox.CheckBoxStyle CheckBoxStyle { get; private set; }

	public DropdownBoxStyle DropdownBoxStyle { get; private set; }

	public SliderStyle SliderStyle { get; private set; }

	public string SearchQuery => _searchInput.Value.Trim();

	public AssetReference CurrentAsset { get; private set; }

	public JObject Value { get; private set; }

	public bool DisplayUnsetProperties { get; private set; } = true;


	public ConfigEditor(AssetEditorOverlay overlay)
		: base(overlay.Desktop, null)
	{
		FlexWeight = 1;
		AssetEditorOverlay = overlay;
		IconExporterModal = new IconExporterModal(this);
		KeyModal = new KeyModal(this);
	}

	public void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("Common.ui", out var document);
		FileDropdownBoxStyle = document.ResolveNamedValue<FileDropdownBoxStyle>(Desktop.Provider, "FileDropdownBoxStyle");
		ColorPickerDropdownBoxStyle = document.ResolveNamedValue<ColorPickerDropdownBoxStyle>(Desktop.Provider, "ColorPickerDropdownBoxStyle");
		DropdownBoxStyle = document.ResolveNamedValue<DropdownBoxStyle>(Desktop.Provider, "DropdownBoxStyle");
		CheckBoxStyle = document.ResolveNamedValue<CheckBox.CheckBoxStyle>(Desktop.Provider, "CheckBoxStyle");
		SliderStyle = document.ResolveNamedValue<SliderStyle>(Desktop.Provider, "SliderStyle");
		Desktop.Provider.TryGetDocument("AssetEditor/ConfigEditor.ui", out var document2);
		UIFragment uIFragment = document2.Instantiate(Desktop, this);
		_currentAssetNameLabel = uIFragment.Get<Group>("CurrentAsset").Find<Label>("AssetName");
		_currentAssetTypeLabel = uIFragment.Get<Group>("CurrentAsset").Find<Label>("AssetType");
		uIFragment.Get<Button>("CurrentAssetWrapper").RightClicking = OpenContextMenu;
		uIFragment.Get<Button>("CurrentAssetWrapper").DoubleClicking = delegate
		{
			AssetEditorOverlay.FocusAssetInTree(CurrentAsset);
		};
		_container = uIFragment.Get<Group>("Container");
		_container.Scrolled = OnContainerScroll;
		_loadingOverlay = uIFragment.Get<Panel>("LoadingOverlay");
		_loadingOverlay.Visible = _isWaitingForBackend;
		_propertiesContainer = uIFragment.Get<Group>("Properties");
		_errorLabel = uIFragment.Get<Label>("ErrorLabel");
		_searchInput = uIFragment.Get<TextField>("PropertySearchInput");
		_searchInput.ValueChanged = delegate
		{
			BuildPropertyEditors();
			Layout();
		};
		IconExporterModal.Build();
		KeyModal.Build();
	}

	protected override void OnMounted()
	{
		SetupDiagnostics();
	}

	protected override void OnUnmounted()
	{
		_propertiesContainer.Clear();
	}

	public void Reset()
	{
		_propertiesContainer.Clear();
		_valueEditor = null;
		SetWaitingForBackend(isWaiting: false);
		_assetFileStates.Clear();
		_assetTypeConfig = null;
	}

	protected override void AfterChildrenLayout()
	{
		if (_scrollToOffsetFromStateAfterLayout)
		{
			_scrollToOffsetFromStateAfterLayout = false;
			_container.SetScroll(State.ScrollOffset.X, State.ScrollOffset.Y);
		}
	}

	public void ToggleDisplayUnsetProperties()
	{
		DisplayUnsetProperties = !DisplayUnsetProperties;
		BuildPropertyEditors();
		Layout();
	}

	public void BuildPropertyEditors()
	{
		_propertiesContainer.Clear();
		_propertiesContainer.Visible = true;
		_errorLabel.Visible = false;
		_valueEditor = null;
		try
		{
			SchemaNode schemaNode = AssetEditorOverlay.ResolveSchemaInCurrentContext(_assetTypeConfig.Schema);
			_valueEditor = ValueEditor.CreateFromSchema(_propertiesContainer, schemaNode, PropertyPath.Root, null, null, this, (JToken)(object)Value);
			_valueEditor.FilterCategory = _valueEditor is ObjectEditor && !State.ActiveCategory.Equals(PropertyPath.Root);
			if (schemaNode.RebuildCaches != null)
			{
				_valueEditor.CachesToRebuild = new CacheRebuildInfo(schemaNode.RebuildCaches, schemaNode.RebuildCachesForChildProperties);
			}
			_valueEditor.ValidateValue();
			_valueEditor.BuildEditor();
			if (base.IsMounted)
			{
				SetupDiagnostics(doLayout: false, clearAlreadySetupDiagnostics: false);
			}
		}
		catch (Exception ex)
		{
			_propertiesContainer.Visible = false;
			Exception ex2 = ex;
			Exception ex3 = ex2;
			if (!(ex3 is ValueEditor.InvalidValueException ex4))
			{
				if (ex3 is ValueEditor.InvalidJsonTypeException)
				{
					_errorLabel.Text = ex.Message;
				}
				else
				{
					_errorLabel.Text = Desktop.Provider.GetText("ui.assetEditor.configEditor.errors.failedToLoad");
				}
			}
			else
			{
				_errorLabel.Text = ex4.DisplayMessage;
			}
			_errorLabel.Visible = true;
			Logger.Error(ex, "Failed to mount editor");
		}
	}

	public void ScrollToTop()
	{
		Group container = _container;
		int? y = 0;
		container.SetScroll(null, y);
	}

	private void GatherCategories()
	{
		SchemaNode schemaNode = AssetEditorOverlay.ResolveSchemaInCurrentContext(_assetTypeConfig.Schema);
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		GatherCategoriesFromObject(schemaNode, dictionary, "", (JToken)(object)Value);
		Categories = dictionary;
		if (State.ActiveCategory.HasValue && !Categories.ContainsKey(State.ActiveCategory.ToString()))
		{
			if (dictionary.Count == 0)
			{
				State.ActiveCategory = null;
			}
			else
			{
				State.ActiveCategory = PropertyPath.FromString(dictionary.FirstOrDefault().Key);
			}
		}
	}

	private void GatherCategoriesFromObject(SchemaNode schemaNode, Dictionary<string, string> categories, string path, JToken value)
	{
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Invalid comparison between Unknown and I4
		if (schemaNode.Properties != null)
		{
			foreach (KeyValuePair<string, SchemaNode> property in schemaNode.Properties)
			{
				if (property.Value.IsHidden || property.Value.SectionStart == null)
				{
					continue;
				}
				string text = path;
				if (text != "")
				{
					text += ".";
				}
				text += property.Key;
				categories.Add(text, property.Value.SectionStart);
				SchemaNode schemaNode2 = AssetEditorOverlay.ResolveSchemaInCurrentContext(property.Value);
				if (schemaNode2.Type == SchemaNode.NodeType.Object)
				{
					GatherCategoriesFromObject(schemaNode2, categories, text, Value[property.Key]);
				}
				else if (schemaNode2.Type == SchemaNode.NodeType.AssetReferenceOrInline)
				{
					JToken val = Value[property.Key];
					if (val != null && (int)val.Type == 1)
					{
						schemaNode2 = AssetEditorOverlay.ResolveSchemaInCurrentContext(schemaNode2.Value);
						GatherCategoriesFromObject(schemaNode2, categories, text, val);
					}
				}
			}
		}
		if (schemaNode.TypePropertyKey != null)
		{
			SchemaNode schemaNode3 = schemaNode;
			if (AssetEditorOverlay.TryResolveTypeSchemaInCurrentContext(value, ref schemaNode3))
			{
				GatherCategoriesFromObject(schemaNode3, categories, path, value);
			}
		}
	}

	public void UpdateCategories()
	{
		GatherCategories();
		AssetEditorOverlay.ConfigEditorContextPane.UpdateCategories();
		AssetEditorOverlay.ConfigEditorContextPane.Layout();
	}

	public void SetWaitingForBackend(bool isWaiting)
	{
		if (_isWaitingForBackend != isWaiting)
		{
			_isWaitingForBackend = isWaiting;
			_loadingOverlay.Visible = isWaiting;
			if (isWaiting)
			{
				_loadingOverlay.Layout(base.RectangleAfterPadding);
			}
		}
	}

	public void OnAssetRenamed(AssetReference oldReference, AssetReference newReference)
	{
		if (oldReference.Equals(CurrentAsset))
		{
			CurrentAsset = newReference;
		}
		if (_assetFileStates.TryGetValue(oldReference.FilePath, out var value))
		{
			_assetFileStates.Remove(oldReference.FilePath);
			_assetFileStates[newReference.FilePath] = value;
		}
	}

	public void OnAssetDeleted(AssetReference assetReference)
	{
		_assetFileStates.Remove(assetReference.FilePath);
	}

	public void Update()
	{
		BuildPropertyEditors();
		Layout();
	}

	public void UpdateJson(JObject value)
	{
		Value = value;
		_valueEditor.SetValueRecursively((JToken)(object)value);
		GatherCategories();
		AssetEditorOverlay.ConfigEditorContextPane.UpdateCategories();
		AssetEditorOverlay.ConfigEditorContextPane.Layout();
		Layout();
	}

	public void Setup(AssetTypeConfig assetTypeConfig, JObject value, AssetReference asset)
	{
		if (!_assetFileStates.TryGetValue(asset.FilePath, out State))
		{
			_assetFileStates[asset.FilePath] = (State = new ConfigEditorState());
		}
		SubmitPendingUpdateCommands();
		_currentAssetNameLabel.Text = AssetEditorOverlay.GetAssetIdFromReference(asset);
		_currentAssetTypeLabel.Text = assetTypeConfig.Name;
		_scrollToOffsetFromStateAfterLayout = true;
		_assetTypeConfig = assetTypeConfig;
		CurrentAsset = asset;
		Value = value;
		SetWaitingForBackend(isWaiting: false);
		GatherCategories();
		if (!State.ActiveCategory.HasValue)
		{
			State.ActiveCategory = ((Categories.Count > 0) ? PropertyPath.FromString(Categories.First().Key) : PropertyPath.Root);
		}
		BuildPropertyEditors();
	}

	private void OnContainerScroll()
	{
		if (!_scrollToOffsetFromStateAfterLayout)
		{
			State.ScrollOffset = _container.ScaledScrollOffset;
		}
	}

	public void OnChangeValue(PropertyPath path, JToken value, JToken previousValue, AssetEditorRebuildCaches cachesToRebuild, bool withheldCommand = false, bool insertItem = false, bool updateDisplayedValue = false)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Expected O, but got Unknown
		JObject value2 = Value;
		bool insertItem2 = insertItem;
		SetProperty(value2, path, value, out var firstCreatedProperty, updateDisplayedValue, insertItem2);
		ClientJsonUpdateCommand clientJsonUpdateCommand = new ClientJsonUpdateCommand
		{
			Type = (JsonUpdateType)(insertItem ? 1 : 0),
			Path = path,
			Value = ((value != null) ? value.DeepClone() : null),
			PreviousValue = previousValue,
			FirstCreatedProperty = firstCreatedProperty,
			RebuildCaches = cachesToRebuild
		};
		ClientJsonUpdateCommand clientJsonUpdateCommand2 = _pendingUpdateCommands.LastOrDefault();
		if (clientJsonUpdateCommand2 != null && clientJsonUpdateCommand2.Path.Equals(path) && clientJsonUpdateCommand2.Type == clientJsonUpdateCommand.Type)
		{
			if (clientJsonUpdateCommand2.RebuildCaches != null)
			{
				if (clientJsonUpdateCommand.RebuildCaches == null)
				{
					clientJsonUpdateCommand.RebuildCaches = clientJsonUpdateCommand2.RebuildCaches;
				}
				else
				{
					clientJsonUpdateCommand.RebuildCaches = new AssetEditorRebuildCaches
					{
						Models = (clientJsonUpdateCommand.RebuildCaches.Models || clientJsonUpdateCommand2.RebuildCaches.Models),
						ModelTextures = (clientJsonUpdateCommand.RebuildCaches.ModelTextures || clientJsonUpdateCommand2.RebuildCaches.ModelTextures),
						BlockTextures = (clientJsonUpdateCommand.RebuildCaches.BlockTextures || clientJsonUpdateCommand2.RebuildCaches.BlockTextures),
						ItemIcons = (clientJsonUpdateCommand.RebuildCaches.ItemIcons || clientJsonUpdateCommand2.RebuildCaches.ItemIcons),
						MapGeometry = (clientJsonUpdateCommand.RebuildCaches.MapGeometry || clientJsonUpdateCommand2.RebuildCaches.MapGeometry)
					};
				}
			}
			clientJsonUpdateCommand.PreviousValue = clientJsonUpdateCommand2.PreviousValue;
			clientJsonUpdateCommand.FirstCreatedProperty = clientJsonUpdateCommand2.FirstCreatedProperty;
			_pendingUpdateCommands[_pendingUpdateCommands.Count - 1] = clientJsonUpdateCommand;
		}
		else
		{
			_pendingUpdateCommands.Add(clientJsonUpdateCommand);
		}
		if (!withheldCommand)
		{
			SubmitPendingUpdateCommands();
		}
		AssetEditorOverlay.Backend.OnValueChanged(path, value);
	}

	public void OnRemoveProperty(PropertyPath path, AssetEditorRebuildCaches cachesToRebuild)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		RemoveProperty(path, out var finalPropertyRemoved, out var finalValueRemoved);
		ClientJsonUpdateCommand item = new ClientJsonUpdateCommand
		{
			Type = (JsonUpdateType)2,
			Path = finalPropertyRemoved,
			PreviousValue = ((finalValueRemoved != null) ? finalValueRemoved.DeepClone() : null),
			RebuildCaches = cachesToRebuild
		};
		Layout();
		_pendingUpdateCommands.Add(item);
		AssetEditorOverlay.Backend.OnValueChanged(path, null);
		SubmitPendingUpdateCommands();
	}

	public void SubmitPendingUpdateCommands()
	{
		if (_pendingUpdateCommands.Count != 0)
		{
			Logger.Info("Submitting {0} update commands", _pendingUpdateCommands.Count);
			AssetEditorOverlay.Backend.UpdateJsonAsset(CurrentAsset, _pendingUpdateCommands);
			_pendingUpdateCommands.Clear();
		}
	}

	public void SetupDiagnostics(bool doLayout = true, bool clearAlreadySetupDiagnostics = true)
	{
		AssetEditorOverlay.Diagnostics.TryGetValue(CurrentAsset.FilePath, out var value);
		if (clearAlreadySetupDiagnostics)
		{
			_valueEditor.ClearDiagnosticsOfDescendants();
		}
		if (value.Errors != null)
		{
			AssetDiagnosticMessage[] errors = value.Errors;
			for (int i = 0; i < errors.Length; i++)
			{
				AssetDiagnosticMessage assetDiagnosticMessage = errors[i];
				if (assetDiagnosticMessage.Property.ElementCount == 0)
				{
					continue;
				}
				PropertyPath path = PropertyPath.Root;
				for (int j = 0; j < assetDiagnosticMessage.Property.ElementCount; j++)
				{
					path = path.GetChild(assetDiagnosticMessage.Property.Elements[j]);
					if (!TryFindPropertyEditor(path, out var propertyEditor) || !propertyEditor.IsMounted)
					{
						break;
					}
					propertyEditor.SetHasError(doLayout);
					if (j != assetDiagnosticMessage.Property.ElementCount - 1)
					{
						propertyEditor.HasChildErrors = true;
					}
				}
			}
		}
		if (value.Warnings == null)
		{
			return;
		}
		AssetDiagnosticMessage[] warnings = value.Warnings;
		for (int k = 0; k < warnings.Length; k++)
		{
			AssetDiagnosticMessage assetDiagnosticMessage2 = warnings[k];
			if (assetDiagnosticMessage2.Property.ElementCount == 0)
			{
				continue;
			}
			PropertyPath path2 = PropertyPath.Root;
			for (int l = 0; l < assetDiagnosticMessage2.Property.ElementCount; l++)
			{
				path2 = path2.GetChild(assetDiagnosticMessage2.Property.Elements[l]);
				if (!TryFindPropertyEditor(path2, out var propertyEditor2) || !propertyEditor2.IsMounted)
				{
					break;
				}
				propertyEditor2.SetHasWarning(doLayout);
				if (l != assetDiagnosticMessage2.Property.ElementCount - 1)
				{
					propertyEditor2.HasChildErrors = true;
				}
			}
		}
	}

	public void FocusPropertySearch()
	{
		if (_searchInput.IsMounted)
		{
			Desktop.FocusElement(_searchInput);
		}
	}

	public bool TryFindPropertyEditor(PropertyPath path, out PropertyEditor propertyEditor)
	{
		if (path.Elements.Length == 0)
		{
			propertyEditor = null;
			return false;
		}
		return _valueEditor.TryFindPropertyEditor(path, out propertyEditor);
	}

	public string GetCurrentAssetId()
	{
		if (CurrentAsset.Type == null)
		{
			return null;
		}
		return AssetPathUtils.GetAssetIdFromReference(CurrentAsset.FilePath, _assetTypeConfig.AssetTree == AssetTreeFolder.Cosmetics);
	}

	private void OpenContextMenu()
	{
		PopupMenuLayer popup = AssetEditorOverlay.Popup;
		List<PopupMenuItem> items = new List<PopupMenuItem>();
		AssetEditorOverlay.SetupAssetPopup(CurrentAsset, items);
		popup.SetTitle(AssetEditorOverlay.GetAssetIdFromReference(CurrentAsset));
		popup.SetItems(items);
		popup.Open();
	}

	private JContainer GetContainer(JContainer root, PropertyPath path, bool create, out PropertyPath? firstCreatedProperty)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Invalid comparison between Unknown and I4
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Expected O, but got Unknown
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Invalid comparison between Unknown and I4
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Expected O, but got Unknown
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Expected O, but got Unknown
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Expected O, but got Unknown
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Expected O, but got Unknown
		firstCreatedProperty = null;
		JContainer val = root;
		PropertyPath propertyPath = PropertyPath.Root;
		string[] elements = path.Elements;
		for (int i = 0; i < elements.Length - 1; i++)
		{
			string text = elements[i];
			propertyPath = propertyPath.GetChild(text);
			if (val is JObject)
			{
				JToken val2 = ((JToken)val)[(object)text];
				if (val2 == null || (int)val2.Type == 10)
				{
					if (!create)
					{
						return null;
					}
					val2 = (((JToken)val)[(object)text] = (JToken)((!elements[i + 1].All(char.IsDigit)) ? ((object)new JObject()) : ((object)new JArray())));
					if (!firstCreatedProperty.HasValue)
					{
						firstCreatedProperty = propertyPath;
					}
					SetEditorValue(propertyPath, val2);
				}
				val = (JContainer)val2;
				continue;
			}
			JArray val4 = (JArray)(object)((val is JArray) ? val : null);
			if (val4 != null)
			{
				if (!int.TryParse(text, out var result))
				{
					throw new Exception("Array index must be number!");
				}
				JToken val5 = ((((JContainer)val4).Count > result) ? val4[result] : null);
				if (val5 == null || (int)val5.Type == 10)
				{
					if (!create)
					{
						return null;
					}
					val5 = (JToken)((!elements[i + 1].All(char.IsDigit)) ? ((object)new JObject()) : ((object)new JArray()));
					if (result >= val.Count)
					{
						val.Add((object)val5);
					}
					else
					{
						((JToken)val)[(object)result] = val5;
					}
					if (!firstCreatedProperty.HasValue)
					{
						firstCreatedProperty = propertyPath;
					}
					SetEditorValue(propertyPath, val5);
				}
				val = (JContainer)val5;
				continue;
			}
			throw new Exception("Value is of unexpected type: " + ((object)val).GetType());
		}
		return val;
	}

	public JToken GetValue(PropertyPath path)
	{
		if (path.Elements.Length == 0)
		{
			return (JToken)(object)Value;
		}
		PropertyPath? firstCreatedProperty;
		JContainer container = GetContainer((JContainer)(object)Value, path, create: false, out firstCreatedProperty);
		JContainer val = container;
		JContainer val2 = val;
		if (val2 != null)
		{
			if (val2 is JArray)
			{
				return ((JToken)container)[(object)int.Parse(path.LastElement)];
			}
			return ((JToken)container)[(object)path.LastElement];
		}
		return null;
	}

	public void RemoveProperty(JObject root, PropertyPath path, out PropertyPath? finalContainerRemoved, out JToken finalValueRemoved, bool updateDisplayedValue = false, bool cleanupEmptyContainers = true)
	{
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		Debug.Assert(path.Elements.Length != 0);
		finalContainerRemoved = null;
		PropertyPath? firstCreatedProperty;
		JContainer container = GetContainer((JContainer)(object)root, path, create: false, out firstCreatedProperty);
		PropertyPath parent = path.GetParent();
		if (cleanupEmptyContainers && TryGetEmptyParentContainerToRemove(container, parent, out var finalPath))
		{
			finalContainerRemoved = finalPath;
			path = finalPath;
			container = GetContainer((JContainer)(object)root, path, create: false, out firstCreatedProperty);
		}
		JContainer val = container;
		JContainer val2 = val;
		JArray val3 = (JArray)(object)((val2 is JArray) ? val2 : null);
		if (val3 == null)
		{
			JObject val4 = (JObject)(object)((val2 is JObject) ? val2 : null);
			if (val4 == null)
			{
				JTokenType type = ((JToken)container).Type;
				throw new Exception("Invalid container type: " + ((object)(JTokenType)(ref type)).ToString());
			}
			string lastElement = path.LastElement;
			finalValueRemoved = val4[lastElement];
			val4.Remove(lastElement);
		}
		else
		{
			int num = int.Parse(path.LastElement);
			finalValueRemoved = val3[num];
			val3.RemoveAt(num);
		}
		SetEditorValueRecursively(path, null, updateDisplayedValue);
		if (!DisplayUnsetProperties && path.Elements.Length != 0 && TryFindPropertyEditor(finalContainerRemoved.GetValueOrDefault(path), out var _))
		{
			_valueEditor.SetValueRecursively((JToken)(object)Value);
			Layout();
		}
		else
		{
			if (path.Elements.Length <= 1 || !TryFindPropertyEditor(path.GetParent(), out var propertyEditor2))
			{
				return;
			}
			ValueEditor valueEditor = propertyEditor2.ValueEditor;
			ValueEditor valueEditor2 = valueEditor;
			if (!(valueEditor2 is ListEditor listEditor))
			{
				if (valueEditor2 is MapEditor mapEditor)
				{
					mapEditor.OnItemRemoved(path.LastElement);
				}
			}
			else
			{
				listEditor.OnItemRemoved(int.Parse(path.LastElement));
			}
		}
	}

	public void SetProperty(JObject root, PropertyPath path, JToken value, out PropertyPath? firstCreatedProperty, bool updateDisplayedValue = false, bool insertItem = false)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		firstCreatedProperty = null;
		if (path.Elements.Length == 0)
		{
			AssetEditorOverlay.SetTrackedAssetData(CurrentAsset.FilePath, (object)(JObject)value);
			return;
		}
		JContainer container = GetContainer((JContainer)(object)root, PropertyPath.FromElements(path.Elements), create: true, out firstCreatedProperty);
		JArray val = (JArray)(object)((container is JArray) ? container : null);
		if (val != null)
		{
			int num = int.Parse(path.LastElement);
			if (insertItem)
			{
				if (!firstCreatedProperty.HasValue)
				{
					firstCreatedProperty = path;
				}
				val.Insert(num, value);
				if (TryFindPropertyEditor(path.GetParent(), out var propertyEditor))
				{
					((ListEditor)propertyEditor.ValueEditor).OnItemInserted(value, num);
				}
				else
				{
					SetEditorValueRecursively(path, value, updateDisplayedValue);
				}
			}
			else
			{
				((JToken)container)[(object)num] = value;
				SetEditorValueRecursively(path, value, updateDisplayedValue);
			}
		}
		else
		{
			string lastElement = path.LastElement;
			if (!((JObject)container).ContainsKey(lastElement))
			{
				if (!firstCreatedProperty.HasValue)
				{
					firstCreatedProperty = path;
				}
				if (TryFindPropertyEditor(path.GetParent(), out var propertyEditor2) && propertyEditor2.ValueEditor is MapEditor mapEditor)
				{
					mapEditor.OnItemInserted(lastElement, value);
				}
				else
				{
					SetEditorValueRecursively(path, value, updateDisplayedValue);
				}
			}
			else
			{
				SetEditorValueRecursively(path, value, updateDisplayedValue);
			}
			((JToken)container)[(object)lastElement] = value;
		}
		if (firstCreatedProperty.HasValue && !DisplayUnsetProperties)
		{
			_valueEditor.SetValueRecursively((JToken)(object)Value);
			Layout();
		}
	}

	private void RemoveProperty(PropertyPath path, out PropertyPath finalPropertyRemoved, out JToken finalValueRemoved)
	{
		RemoveProperty(Value, path, out var finalContainerRemoved, out finalValueRemoved);
		finalPropertyRemoved = finalContainerRemoved.GetValueOrDefault(path);
	}

	private void SetEditorValue(PropertyPath path, JToken value)
	{
		if (TryFindPropertyEditor(path, out var propertyEditor))
		{
			propertyEditor.ValueEditor.SetValue(value);
		}
	}

	private void SetEditorValueRecursively(PropertyPath path, JToken value, bool updateDisplayedValue)
	{
		if (TryFindPropertyEditor(path, out var propertyEditor))
		{
			propertyEditor.ValueEditor.SetValueRecursively(value);
			if (updateDisplayedValue)
			{
				propertyEditor.ValueEditor.UpdateDisplayedValue();
			}
		}
	}

	private bool TryGetEmptyParentContainerToRemove(JContainer container, PropertyPath path, out PropertyPath finalPath)
	{
		JContainer val = container;
		finalPath = PropertyPath.Root;
		while (val != null && val.Count <= 1 && val != Value)
		{
			JContainer parent = ((JToken)val).Parent;
			if (parent is JProperty)
			{
				parent = ((JToken)parent).Parent;
			}
			SchemaNode schemaNodeInCurrentContext = AssetEditorOverlay.GetSchemaNodeInCurrentContext(Value, path);
			if (schemaNodeInCurrentContext.Type == SchemaNode.NodeType.Object && schemaNodeInCurrentContext.AllowEmptyObject)
			{
				break;
			}
			PropertyPath parent2 = path.GetParent();
			SchemaNode schemaNodeInCurrentContext2 = AssetEditorOverlay.GetSchemaNodeInCurrentContext(Value, parent2);
			if (schemaNodeInCurrentContext2.Type == SchemaNode.NodeType.List || schemaNodeInCurrentContext2.Type == SchemaNode.NodeType.Map)
			{
				break;
			}
			finalPath = path;
			val = parent;
			if (val == Value)
			{
				break;
			}
			path = parent2;
		}
		return finalPath.Elements.Length != 0;
	}
}
