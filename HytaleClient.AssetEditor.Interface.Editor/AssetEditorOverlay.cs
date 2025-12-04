#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using HytaleClient.AssetEditor.Backends;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Config;
using HytaleClient.AssetEditor.Interface.Elements;
using HytaleClient.AssetEditor.Interface.Modals;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Data;
using HytaleClient.Data.UserSettings;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json.Linq;
using SDL2;

namespace HytaleClient.AssetEditor.Interface.Editor;

internal class AssetEditorOverlay : Element
{
	private class AssetToOpen
	{
		public AssetIdReference Id;

		public string FilePath;
	}

	public enum SaveStatus
	{
		Disabled,
		Saved,
		Unsaved,
		Saving
	}

	public enum EditorMode
	{
		Editor,
		Source
	}

	public readonly AssetTypeRegistry AssetTypeRegistry;

	public readonly AssetList Assets;

	private IReadOnlyDictionary<string, SchemaNode> _schemas = new Dictionary<string, SchemaNode>();

	public readonly Dictionary<string, TrackedAsset> TrackedAssets = new Dictionary<string, TrackedAsset>();

	private int _errorCount;

	private int _warningCount;

	private const string Version = "0.1.0";

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public readonly AssetTree DropdownAssetTree;

	private readonly AssetTree _serverAssetTree;

	private readonly AssetTree _commonAssetTree;

	private readonly AssetTree _cosmeticsAssetTree;

	private Group _rootContainer;

	private Group _editorPane;

	private DynamicPane _contextPane;

	private DynamicPane _contentPane;

	private TextField _assetBrowserSearchInput;

	private Group _assetBrowserSpinner;

	private DynamicPane _assetBrowser;

	private Element _assetLoadingIndicator;

	private Button _errorsInfo;

	private Button _warningsInfo;

	private Button _validatedInfo;

	private Group _footer;

	private Group _fileSaveStatusGroup;

	private DynamicPane _diagnosticsPane;

	private TabNavigation _modeSelection;

	private TabNavigation _assetTreeSelection;

	private Button _exportButton;

	private TextButton _assetsSourceButton;

	private Label _modifiedAssetsCountLabel;

	private Group _tabs;

	private Group _assetPathWarning;

	public readonly AssetEditorInterface Interface;

	public readonly ConfirmationModal ConfirmationModal;

	public readonly PopupMenuLayer Popup;

	public readonly RenameModal RenameModal;

	public readonly CreateAssetModal CreateAssetModal;

	public readonly ChangelogModal ChangelogModal;

	public readonly AutoCompleteMenu AutoCompleteMenu;

	public readonly TextTooltipLayer TextTooltipLayer;

	public readonly WeatherDaytimeBar WeatherDaytimeBar;

	public readonly ConfigEditor ConfigEditor;

	public readonly ConfigEditorContextPane ConfigEditorContextPane;

	public readonly ExportModal ExportModal;

	public readonly FilterModal FilterModal;

	public readonly IconMassExportModal IconMassExportModal;

	private readonly SourceEditor _sourceEditor;

	private readonly HashSet<string> DisplayedCosmeticAssetTypes = new HashSet<string>();

	private readonly HashSet<string> DisplayedServerAssetTypes = new HashSet<string>();

	private readonly HashSet<string> DisplayedCommonAssetTypes = new HashSet<string>();

	private bool _isDiagnosticsPaneOpen;

	private AssetToOpen _assetToOpenOnceAssetFilesInitialized;

	private bool _areAssetFilesInitialized;

	private bool _areAssetTypesAndSchemasInitialized;

	private SaveStatus _fileSaveStatus = SaveStatus.Disabled;

	private int _modifiedAssetsCount;

	private Dictionary<string, List<DropdownBox>> _dropdownBoxesWithDataset = new Dictionary<string, List<DropdownBox>>();

	private CancellationTokenSource _reloadInheritanceStackCancellationTokenSource;

	private CancellationTokenSource _currentAssetCancellationToken;

	private bool _awaitingInitialEditorSetup;

	public AssetReference CurrentAsset { get; private set; }

	public Dictionary<string, AssetDiagnostics> Diagnostics { get; private set; } = new Dictionary<string, AssetDiagnostics>();


	public ToastNotifications ToastNotifications { get; private set; }

	public EditorMode Mode { get; private set; } = EditorMode.Editor;


	public bool IsBackendInitialized { get; private set; }

	public AssetEditorBackend Backend => Interface.App.Editor.Backend;

	public void UpdateTextAsset(string path, string text)
	{
		TrackedAssets[path].Data = text;
		Backend.UpdateAsset(CurrentAsset, text);
	}

	public void UpdateJsonAsset(string path, JObject json, AssetEditorRebuildCaches cachesToRebuild)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		JObject val = (JObject)TrackedAssets[path].Data;
		JToken previousValue = (((int)val != 0) ? ((JToken)val).DeepClone() : null);
		TrackedAssets[path].Data = json;
		Backend.UpdateJsonAsset(CurrentAsset, new List<ClientJsonUpdateCommand>
		{
			new ClientJsonUpdateCommand
			{
				Type = (JsonUpdateType)0,
				Value = (JToken)(object)json,
				PreviousValue = previousValue,
				Path = PropertyPath.FromString(""),
				RebuildCaches = cachesToRebuild
			}
		});
		if (base.IsMounted && path == CurrentAsset.FilePath)
		{
			if (ConfigEditor.IsMounted)
			{
				ConfigEditor.UpdateJson(json);
			}
			else
			{
				SetupEditorPane();
			}
		}
	}

	public void UpdateImageAsset(string path, Image image)
	{
		TrackedAssets[path].Data = image;
		Backend.UpdateAsset(CurrentAsset, image);
	}

	public void SetTrackedAssetData(string path, object asset)
	{
		if (TrackedAssets.TryGetValue(path, out var value) && !value.IsLoading && value.FetchError == null)
		{
			value.Data = asset;
			OnTrackedAssetUpdated(value);
		}
	}

	public void CreateAsset(AssetReference assetReference, JObject data, string buttonId, Action<FormattedMessage> callback)
	{
		Backend.CreateAsset(assetReference, data, buttonId, openInTab: true, callback);
	}

	private void UpdateAssetTreeForPath(string path, bool doLayout = true)
	{
		AssetPathUtils.TryGetAssetTreeFolder(path, out var assetTree);
		List<AssetFile> assets = Assets.GetAssets(assetTree);
		AssetTree assetTree2 = GetAssetTree(assetTree);
		assetTree2.UpdateFiles(assets);
		if (doLayout)
		{
			assetTree2.Layout();
		}
	}

	public void OnAssetDeleted(AssetReference assetReference)
	{
		if (CurrentAsset.FilePath != null && CurrentAsset.Equals(assetReference))
		{
			CloseTab(CurrentAsset);
		}
		if (Assets.TryRemoveFile(assetReference.FilePath))
		{
			UpdateAssetTreeForPath(assetReference.FilePath);
			ConfigEditor.OnAssetDeleted(assetReference);
		}
	}

	public void OnAssetRenamed(AssetReference oldReference, AssetReference newReference)
	{
		if (!Assets.TryMoveFile(oldReference.FilePath, newReference.FilePath))
		{
			return;
		}
		UpdateAssetTreeForPath(oldReference.FilePath);
		UpdateTab(oldReference, newReference);
		if (TrackedAssets.TryGetValue(oldReference.FilePath, out var value))
		{
			TrackedAssets.Remove(oldReference.FilePath);
			TrackedAssets[newReference.FilePath] = new TrackedAsset(newReference, value.Data);
			AssetTypeConfig assetTypeConfig = AssetTypeRegistry.AssetTypes[newReference.Type];
			if (assetTypeConfig.IsJson && assetTypeConfig.HasIdField)
			{
				object data = value.Data;
				JObject val = (JObject)((data is JObject) ? data : null);
				if (val != null)
				{
					val["Id"] = JToken.op_Implicit(GetAssetIdFromReference(newReference));
				}
			}
		}
		if (CurrentAsset.FilePath == oldReference.FilePath)
		{
			CurrentAsset = newReference;
			Backend.SetOpenEditorAsset(newReference);
			SelectAssetTreeEntry(newReference);
		}
		ConfigEditor.OnAssetRenamed(oldReference, newReference);
	}

	public void OnAssetAdded(AssetReference assetReference, bool openInEditor)
	{
		if (Assets.TryInsertFile(assetReference.FilePath))
		{
			UpdateAssetTreeForPath(assetReference.FilePath, doLayout: false);
			if (assetReference.Equals(CurrentAsset))
			{
				SelectAssetTreeEntry(assetReference, bringIntoView: true, doLayout: false);
			}
			_assetBrowser.Layout();
			if (openInEditor)
			{
				AddTab(assetReference);
				SetupEditorPane();
			}
		}
	}

	public void OpenCreatedAsset(AssetReference assetReference, object data)
	{
		ResetTrackedAssets();
		SelectAssetTreeEntry(assetReference, bringIntoView: true);
		AddTab(assetReference);
		CurrentAsset = assetReference;
		TrackedAssets[assetReference.FilePath] = new TrackedAsset(assetReference, data);
		Interface.App.Editor.ClearPreview(updateUi: false);
		SetupEditorPane();
		Backend.SetOpenEditorAsset(assetReference);
	}

	public void OnDirectoryContentsUpdated(string path, List<AssetFile> newAssetFiles)
	{
		if (Assets.TryReplaceDirectoryContents(path, newAssetFiles))
		{
			UpdateAssetTreeForPath(path);
		}
	}

	public void OnDirectoryCreated(string path)
	{
		if (Assets.TryInsertDirectory(path))
		{
			UpdateAssetTreeForPath(path);
		}
	}

	public void OnDirectoryRenamed(string oldPath, string newPath)
	{
		if (!Assets.TryMoveDirectory(oldPath, newPath, out var renamedAssets))
		{
			return;
		}
		foreach (KeyValuePair<string, AssetFile> item in renamedAssets)
		{
			if (!item.Value.IsDirectory && AssetTypeRegistry.TryGetAssetTypeFromPath(item.Key, out var assetType))
			{
				AssetReference assetReference = new AssetReference(assetType, item.Key);
				AssetReference assetReference2 = new AssetReference(item.Value.AssetType, item.Value.Path);
				UpdateTab(assetReference, assetReference2);
				if (CurrentAsset.Equals(assetReference))
				{
					CurrentAsset = assetReference2;
					SelectAssetTreeEntry(assetReference2);
				}
				ConfigEditor.OnAssetRenamed(assetReference, assetReference2);
			}
		}
		UpdateAssetTreeForPath(oldPath);
	}

	public void OnDirectoryDeleted(string path)
	{
		if (!Assets.TryRemoveDirectory(path, out var removedEntries))
		{
			return;
		}
		foreach (AssetFile item in removedEntries)
		{
			if (!item.IsDirectory)
			{
				CloseTab(new AssetReference(item.AssetType, item.Path));
			}
		}
		UpdateAssetTreeForPath(path);
	}

	public void OnDiagnosticsUpdated(Dictionary<string, AssetDiagnostics> diagnostics)
	{
		bool flag = false;
		foreach (KeyValuePair<string, AssetDiagnostics> diagnostic in diagnostics)
		{
			if (Diagnostics.TryGetValue(diagnostic.Key, out var value))
			{
				_errorCount -= value.Errors.Length;
				_warningCount -= value.Warnings.Length;
			}
			if (diagnostic.Value.Errors != null)
			{
				_errorCount += diagnostic.Value.Errors.Length;
			}
			if (diagnostic.Value.Warnings != null)
			{
				_warningCount += diagnostic.Value.Warnings.Length;
			}
			if (diagnostic.Value.Errors != null || diagnostic.Value.Warnings != null)
			{
				Diagnostics[diagnostic.Key] = diagnostic.Value;
			}
			else
			{
				Diagnostics.Remove(diagnostic.Key);
			}
			if (diagnostic.Key == CurrentAsset.FilePath)
			{
				flag = true;
			}
		}
		UpdateDiagnostics();
		_errorsInfo.Parent.Layout();
		if (_diagnosticsPane.IsMounted)
		{
			_diagnosticsPane.Layout();
		}
		if (flag && ConfigEditor.IsMounted)
		{
			ConfigEditor.SetupDiagnostics();
		}
	}

	public void SetupAssetTypes(IReadOnlyDictionary<string, SchemaNode> schemas, IReadOnlyDictionary<string, AssetTypeConfig> assetTypes)
	{
		_schemas = schemas;
		_areAssetTypesAndSchemasInitialized = true;
		AssetTypeRegistry.SetupAssetTypes(assetTypes);
		FilterModal.Setup();
	}

	public void SetupAssetFiles(List<AssetFile> serverAssetFiles, List<AssetFile> commonAssetFiles, List<AssetFile> cosmeticAssetFiles)
	{
		Assets.SetupAssets(serverAssetFiles, commonAssetFiles, cosmeticAssetFiles);
		_serverAssetTree.UpdateFiles(serverAssetFiles);
		_commonAssetTree.UpdateFiles(commonAssetFiles);
		_cosmeticsAssetTree.UpdateFiles(cosmeticAssetFiles);
		SetAssetTreeInitializing(isInitializing: false);
		if (CurrentAsset.Equals(AssetReference.None))
		{
			if (_assetToOpenOnceAssetFilesInitialized != null)
			{
				if (_assetToOpenOnceAssetFilesInitialized.FilePath != null)
				{
					OpenExistingAsset(_assetToOpenOnceAssetFilesInitialized.FilePath);
				}
				else if (_assetToOpenOnceAssetFilesInitialized.Id.Id != null)
				{
					OpenExistingAssetById(_assetToOpenOnceAssetFilesInitialized.Id);
				}
				_assetToOpenOnceAssetFilesInitialized = null;
			}
		}
		else
		{
			SelectAssetTreeEntry(CurrentAsset, bringIntoView: true);
		}
	}

	public string GetAssetIdFromReference(AssetReference assetReference)
	{
		return (AssetTypeRegistry.AssetTypes[assetReference.Type].AssetTree == AssetTreeFolder.Cosmetics) ? assetReference.FilePath.Split(new char[1] { '#' }).Last() : Path.GetFileNameWithoutExtension(assetReference.FilePath);
	}

	public List<FileSelector.File> GetCommonFileSelectorFiles(string path, string searchQuery, string[] fileExtensionFilter, string[] directoryFilter, int limit)
	{
		List<FileSelector.File> list = new List<FileSelector.File>();
		if (searchQuery != "")
		{
			string[] array = (from q in searchQuery.ToLower().Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
				select q.Trim()).ToArray();
			List<AssetFile> assets = Assets.GetAssets(AssetTreeFolder.Common);
			foreach (AssetFile item in assets)
			{
				if (list.Count >= limit)
				{
					break;
				}
				string path2 = item.Path;
				bool flag = true;
				string text = path2.ToLowerInvariant();
				string[] array2 = array;
				foreach (string value in array2)
				{
					if (!text.Contains(value))
					{
						flag = false;
						break;
					}
				}
				if (flag && (item.IsDirectory || fileExtensionFilter == null || AssetPathUtils.HasAnyFileExtension(path2, fileExtensionFilter)))
				{
					string text2 = item.Path.Substring("Common/".Length);
					if (directoryFilter == null || AssetPathUtils.IsAnyDirectory("/" + text2, directoryFilter))
					{
						list.Add(new FileSelector.File
						{
							Name = text2,
							IsDirectory = item.IsDirectory
						});
					}
				}
			}
		}
		else
		{
			string[] array3 = path.Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			List<AssetFile> assets2 = Assets.GetAssets(AssetTreeFolder.Common);
			bool flag2 = path == "Common/";
			foreach (AssetFile item2 in assets2)
			{
				if (!flag2)
				{
					if (item2.Path == path)
					{
						flag2 = true;
					}
					continue;
				}
				if (item2.PathElements.Length <= array3.Length)
				{
					break;
				}
				if (array3.Length + 1 == item2.PathElements.Length)
				{
					string text3 = item2.PathElements.Last();
					if (item2.IsDirectory || fileExtensionFilter == null || AssetPathUtils.HasAnyFileExtension(text3, fileExtensionFilter))
					{
						list.Add(new FileSelector.File
						{
							Name = text3,
							IsDirectory = item2.IsDirectory
						});
					}
				}
			}
		}
		return list;
	}

	public bool ValidateAssetId(string id, out string errorMessage)
	{
		id = id.Trim();
		if (id == "")
		{
			errorMessage = Desktop.Provider.GetText("ui.assetEditor.idValidation.empty");
			return false;
		}
		if (id.Length < 3)
		{
			errorMessage = Desktop.Provider.GetText("ui.assetEditor.idValidation.minLength", new Dictionary<string, string> { { "count", "3" } });
			return false;
		}
		if (id.Length > 64)
		{
			errorMessage = Desktop.Provider.GetText("ui.assetEditor.idValidation.maxLength", new Dictionary<string, string> { { "count", "64" } });
			return false;
		}
		string[] array = id.Split(new char[1] { '_' });
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (text == "")
			{
				errorMessage = Desktop.Provider.GetText("ui.assetEditor.idValidation.underscoreEmpty");
				return false;
			}
			if (!char.IsLetter(text[0]))
			{
				errorMessage = Desktop.Provider.GetText("ui.assetEditor.idValidation.firstLetter");
				return false;
			}
			if (!char.IsUpper(text[0]))
			{
				errorMessage = Desktop.Provider.GetText("ui.assetEditor.idValidation.firstUppercase");
				return false;
			}
			string text2 = text;
			foreach (char c in text2)
			{
				if (!char.IsDigit(c) && !char.IsLetter(c))
				{
					errorMessage = Desktop.Provider.GetText("ui.assetEditor.idValidation.onlyLettersAndDigits");
					return false;
				}
			}
		}
		errorMessage = null;
		return true;
	}

	public AssetEditorOverlay(AssetEditorInterface @interface, Desktop desktop)
		: base(desktop, null)
	{
		AssetTypeRegistry = new AssetTypeRegistry();
		Assets = new AssetList(AssetTypeRegistry);
		Interface = @interface;
		ConfirmationModal = new ConfirmationModal(desktop, null);
		Popup = new PopupMenuLayer(Desktop, null);
		RenameModal = new RenameModal(this);
		ConfigEditor = new ConfigEditor(this);
		CreateAssetModal = new CreateAssetModal(this);
		ChangelogModal = new ChangelogModal(this);
		ConfigEditorContextPane = new ConfigEditorContextPane(this);
		_sourceEditor = new SourceEditor(this);
		ExportModal = new ExportModal(this);
		FilterModal = new FilterModal(this);
		IconMassExportModal = new IconMassExportModal(this);
		WeatherDaytimeBar = new WeatherDaytimeBar(this);
		AutoCompleteMenu = new AutoCompleteMenu(Desktop);
		TextTooltipLayer = new TextTooltipLayer(Desktop)
		{
			ShowDelay = 0.25f
		};
		DropdownAssetTree = new AssetTree(this, "", null)
		{
			PopupMenuEnabled = false,
			ShowVirtualAssets = true
		};
		DropdownAssetTree.ScrollbarStyle.Size = 5;
		_cosmeticsAssetTree = new AssetTree(this, "Cosmetics/CharacterCreator", null)
		{
			FocusSearch = FocusSearch,
			FileEntryActivating = delegate(AssetTree.AssetTreeEntry entry)
			{
				OpenExistingAsset(new AssetReference(entry.AssetType, entry.Path));
			},
			SelectingDirectoryFilter = SetupSearchFilter,
			CollapseStateChanged = OnAssetTreeCollapseStateChanged
		};
		_commonAssetTree = new AssetTree(this, "Common", null)
		{
			FocusSearch = FocusSearch,
			FileEntryActivating = delegate(AssetTree.AssetTreeEntry entry)
			{
				OpenExistingAsset(new AssetReference(entry.AssetType, entry.Path));
			},
			SelectingDirectoryFilter = SetupSearchFilter,
			CollapseStateChanged = OnAssetTreeCollapseStateChanged,
			PopupMenuEnabled = false
		};
		_serverAssetTree = new AssetTree(this, "Server", null)
		{
			FocusSearch = FocusSearch,
			FileEntryActivating = delegate(AssetTree.AssetTreeEntry entry)
			{
				OpenExistingAsset(new AssetReference(entry.AssetType, entry.Path));
			},
			SelectingDirectoryFilter = SetupSearchFilter,
			CollapseStateChanged = OnAssetTreeCollapseStateChanged
		};
	}

	public void Build()
	{
		AssetEditorSettings settings = Interface.App.Settings;
		_fileSaveStatus = SaveStatus.Disabled;
		AutoCompleteMenu.Parent?.Remove(AutoCompleteMenu);
		WeatherDaytimeBar.Parent?.Remove(WeatherDaytimeBar);
		Clear();
		_editorPane?.Clear();
		_contextPane?.Clear();
		_serverAssetTree.Parent?.Remove(_serverAssetTree);
		_commonAssetTree.Parent?.Remove(_commonAssetTree);
		_cosmeticsAssetTree.Parent?.Remove(_cosmeticsAssetTree);
		Desktop.Provider.TryGetDocument("AssetEditor/AssetEditorOverlay.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		TextTooltipLayer.Style = document.ResolveNamedValue<TextTooltipStyle>(Desktop.Provider, "TooltipStyle");
		TextTooltipStyle tooltipStyle = document.ResolveNamedValue<TextTooltipStyle>(Desktop.Provider, "AssetTreeTooltipStyle");
		_serverAssetTree.TooltipStyle = tooltipStyle;
		_commonAssetTree.TooltipStyle = tooltipStyle;
		_cosmeticsAssetTree.TooltipStyle = tooltipStyle;
		_rootContainer = uIFragment.Get<Group>("RootContainer");
		_contentPane = uIFragment.Get<DynamicPane>("ContentPane");
		_editorPane = uIFragment.Get<Group>("EditorPane");
		_contextPane = uIFragment.Get<DynamicPane>("ContextPane");
		_contextPane.Visible = false;
		_contextPane.Anchor.Width = settings.PaneSizes[AssetEditorSettings.Panes.ConfigEditorSidebar];
		_contextPane.MouseButtonReleased = delegate
		{
			UpdatePaneSize(AssetEditorSettings.Panes.ConfigEditorSidebar, _contextPane.Anchor.Width.Value);
		};
		TextButton textButton = uIFragment.Get<TextButton>("ChangelogButton");
		textButton.Activating = delegate
		{
			Desktop.SetLayer(4, ChangelogModal);
		};
		textButton.Text = "v0.1.0";
		uIFragment.Get<Button>("OptionsButton").Activating = OnActivateOptionsButton;
		_assetsSourceButton = uIFragment.Get<TextButton>("AssetsSourceButton");
		_assetsSourceButton.Activating = delegate
		{
			if (!OpenUtils.TryOpenDirectoryInContainingDirectory(Interface.App.Settings.AssetsPath, Interface.App.Settings.AssetsPath))
			{
				Logger.Warn("Failed to open folder {0}", Interface.App.Settings.AssetsPath);
			}
		};
		UpdateAssetsSourceButton();
		uIFragment.Get<TextButton>("CloseButton").Activating = TryClose;
		uIFragment.Get<TextButton>("NewAssetButton").Activating = OpenCreateAssetModal;
		uIFragment.Get<Button>("CollapseAllButton").Activating = CollapseAllDirectoriesInAssetTree;
		uIFragment.Get<Button>("FilterButton").Activating = delegate
		{
			FilterModal.Open();
		};
		List<Element> list = ((_tabs != null) ? new List<Element>(_tabs.Children) : null);
		_tabs?.Clear();
		_tabs = uIFragment.Get<Group>("Tabs");
		if (list != null)
		{
			foreach (Element item in list)
			{
				((EditorTabButton)item).Build();
				_tabs.Add(item);
			}
		}
		_modifiedAssetsCountLabel = uIFragment.Get<Label>("ModifiedAssetsCountLabel");
		_modifiedAssetsCountLabel.Text = Desktop.Provider.FormatNumber(_modifiedAssetsCount);
		_modifiedAssetsCountLabel.Visible = _modifiedAssetsCount > 0;
		_modeSelection = uIFragment.Get<TabNavigation>("Mode");
		_modeSelection.SelectedTab = Mode.ToString();
		_modeSelection.SelectedTabChanged = delegate
		{
			OnChangeMode((EditorMode)Enum.Parse(typeof(EditorMode), _modeSelection.SelectedTab));
		};
		_assetTreeSelection = uIFragment.Get<TabNavigation>("RootFolderSelection");
		_assetTreeSelection.SelectedTabChanged = delegate
		{
			OnAssetTreeSelectionChanged((AssetTreeFolder)Enum.Parse(typeof(AssetTreeFolder), _assetTreeSelection.SelectedTab));
		};
		_footer = uIFragment.Get<Group>("Footer");
		_fileSaveStatusGroup = uIFragment.Get<Group>("FileSaveStatus");
		_fileSaveStatusGroup.Visible = false;
		_validatedInfo = uIFragment.Get<Button>("ValidatedInfo");
		_validatedInfo.Activating = ToggleDiagnosticsPane;
		_errorsInfo = uIFragment.Get<Button>("ErrorsInfo");
		_errorsInfo.Activating = ToggleDiagnosticsPane;
		_warningsInfo = uIFragment.Get<Button>("WarningsInfo");
		_warningsInfo.Activating = ToggleDiagnosticsPane;
		_diagnosticsPane = uIFragment.Get<DynamicPane>("DiagnosticsPane");
		_diagnosticsPane.Visible = _isDiagnosticsPaneOpen;
		_diagnosticsPane.Anchor.Height = settings.PaneSizes[AssetEditorSettings.Panes.Diagnostics];
		_diagnosticsPane.MouseButtonReleased = delegate
		{
			UpdatePaneSize(AssetEditorSettings.Panes.Diagnostics, _diagnosticsPane.Anchor.Height.Value);
		};
		UpdateDiagnostics();
		_assetBrowserSearchInput = uIFragment.Get<TextField>("SearchInput");
		_assetBrowserSearchInput.ValueChanged = delegate
		{
			UpdateAssetTreeSearch();
		};
		_assetBrowserSearchInput.KeyDown = delegate(SDL_Keycode key)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Invalid comparison between Unknown and I4
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Invalid comparison between Unknown and I4
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			if ((int)key == 1073741905 || (int)key == 1073741906)
			{
				AssetTree assetTree = GetAssetTree(Interface.App.Settings.ActiveAssetTree);
				Desktop.FocusElement(assetTree);
				assetTree.OnKeyDown(key, 0);
			}
		};
		_assetBrowser = uIFragment.Get<DynamicPane>("AssetBrowserPane");
		_assetBrowser.Anchor.Width = settings.PaneSizes[AssetEditorSettings.Panes.AssetBrowser];
		_assetBrowser.MouseButtonReleased = delegate
		{
			UpdatePaneSize(AssetEditorSettings.Panes.AssetBrowser, _assetBrowser.Anchor.Width.Value);
		};
		_assetBrowserSpinner = uIFragment.Get<Group>("AssetBrowserSpinner");
		_exportButton = uIFragment.Get<Button>("ExportButton");
		_exportButton.Activating = delegate
		{
			ExportModal.Open();
		};
		_assetPathWarning = uIFragment.Get<Group>("AssetPathWarning");
		_assetPathWarning.Visible = settings.DisplayDefaultAssetPathWarning;
		uIFragment.Get<TextButton>("AssetPathWarningOpenSettings").Activating = delegate
		{
			Interface.SettingsModal.Open();
		};
		uIFragment.Get<TextButton>("AssetPathWarningDismiss").Activating = delegate
		{
			AssetEditorSettings assetEditorSettings = Interface.App.Settings.Clone();
			assetEditorSettings.DisplayDefaultAssetPathWarning = false;
			Interface.App.ApplySettings(assetEditorSettings);
		};
		_assetBrowser.Add(_serverAssetTree);
		_assetBrowser.Add(_commonAssetTree);
		_assetBrowser.Add(_cosmeticsAssetTree);
		Popup.Style = document.ResolveNamedValue<PopupMenuLayerStyle>(Desktop.Provider, "PopupMenuLayerStyle");
		ConfigEditor.Build();
		ConfirmationModal.Build();
		RenameModal.Build();
		CreateAssetModal.Build();
		ChangelogModal.Build();
		AutoCompleteMenu.Build();
		ConfigEditorContextPane.Build();
		ExportModal.Build();
		FilterModal.Build();
		IconMassExportModal.Build();
		_sourceEditor.Build();
		WeatherDaytimeBar.Build();
		ConfigEditorContextPane.DayTimeControls.Build();
		ToastNotifications = new ToastNotifications(Desktop, this);
		Interface.TryGetDocument("AssetEditor/AssetLoadingIndicator.ui", out var document2);
		_assetLoadingIndicator = document2.Instantiate(Desktop, null).RootElements[0];
		UpdateAssetTreeUncollapsedStateFromSettings();
		if (Backend != null)
		{
			_exportButton.Visible = Backend.IsEditingRemotely;
			BuildAssetTreeTabs();
		}
		if (base.IsMounted)
		{
			if (!IsBackendInitialized)
			{
				InitializeBackend();
			}
			FinishWork();
			SetupEditorPane();
		}
	}

	public void UpdateAssetPathWarning(bool isDefault)
	{
		_assetPathWarning.Visible = isDefault;
		Layout();
	}

	private void UpdateAssetsSourceButton()
	{
		AssetEditorBackend backend = Backend;
		AssetEditorBackend assetEditorBackend = backend;
		if (!(assetEditorBackend is LocalAssetEditorBackend))
		{
			if (assetEditorBackend is ServerAssetEditorBackend)
			{
				_assetsSourceButton.Text = Desktop.Provider.GetText("ui.assetEditor.backends.server");
			}
			else
			{
				_assetsSourceButton.Text = "???";
			}
		}
		else
		{
			_assetsSourceButton.Text = Desktop.Provider.GetText("ui.assetEditor.backends.local");
		}
	}

	private void BuildAssetTreeTabs()
	{
		List<TabNavigation.Tab> list = new List<TabNavigation.Tab>();
		AssetTreeFolder[] supportedAssetTreeFolders = Backend.SupportedAssetTreeFolders;
		AssetTreeFolder[] array = supportedAssetTreeFolders;
		for (int i = 0; i < array.Length; i++)
		{
			AssetTreeFolder assetTreeFolder = array[i];
			list.Add(new TabNavigation.Tab
			{
				Id = assetTreeFolder.ToString(),
				Text = Desktop.Provider.GetText("ui.assetEditor.assetBrowser.tabs." + assetTreeFolder.ToString().ToLowerInvariant())
			});
		}
		_assetTreeSelection.Tabs = list.ToArray();
		AssetEditorApp app = Interface.App;
		AssetEditorSettings settings = app.Settings;
		if (!supportedAssetTreeFolders.Contains(settings.ActiveAssetTree))
		{
			settings = settings.Clone();
			settings.ActiveAssetTree = supportedAssetTreeFolders[0];
			app.ApplySettings(settings);
			UpdateActiveAssetTree(doLayout: false);
		}
	}

	protected override void OnMounted()
	{
		if (!_sourceEditor.CodeEditor.IsInitialized)
		{
			_sourceEditor.CodeEditor.InitEditor();
		}
		if (!CreateAssetModal.CodeEditor.IsInitialized)
		{
			CreateAssetModal.CodeEditor.InitEditor();
		}
		if (!Interface.HasMarkupError)
		{
			if (!IsBackendInitialized)
			{
				InitializeBackend();
			}
			Backend.OnEditorOpen(isOpen: true);
			if (!CurrentAsset.Equals(AssetReference.None))
			{
				Backend.SetOpenEditorAsset(CurrentAsset);
				FetchOpenAsset();
			}
			OpenChangelogModalIfNewVersion();
		}
	}

	protected override void OnUnmounted()
	{
		_currentAssetCancellationToken?.Cancel();
	}

	private void InitializeBackend()
	{
		UpdateActiveAssetTree(doLayout: false);
		UpdateAssetTreeUncollapsedStateFromSettings();
		SetAssetTreeInitializing(isInitializing: true);
		Debug.Assert(!IsBackendInitialized);
		IsBackendInitialized = true;
		Backend.Initialize();
	}

	public void SetupBackend(AssetEditorBackend backend)
	{
		IsBackendInitialized = false;
		if (_rootContainer != null)
		{
			_exportButton.Visible = backend?.IsEditingRemotely ?? false;
			BuildAssetTreeTabs();
			UpdateAssetsSourceButton();
		}
	}

	public void FinishWork()
	{
		if (ConfigEditor.IsMounted)
		{
			ConfigEditor.SubmitPendingUpdateCommands();
		}
	}

	public void Reset()
	{
		_currentAssetCancellationToken?.Cancel();
		_serverAssetTree.UpdateFiles(new List<AssetFile>());
		_serverAssetTree.DeselectEntry();
		_serverAssetTree.DirectoriesToDisplay = null;
		_serverAssetTree.AssetTypesToDisplay = null;
		_commonAssetTree.UpdateFiles(new List<AssetFile>());
		_commonAssetTree.DeselectEntry();
		_commonAssetTree.DirectoriesToDisplay = null;
		_commonAssetTree.AssetTypesToDisplay = null;
		_cosmeticsAssetTree.UpdateFiles(new List<AssetFile>());
		_cosmeticsAssetTree.DeselectEntry();
		_cosmeticsAssetTree.DirectoriesToDisplay = null;
		_cosmeticsAssetTree.AssetTypesToDisplay = null;
		DisplayedCommonAssetTypes.Clear();
		DisplayedServerAssetTypes.Clear();
		DisplayedCosmeticAssetTypes.Clear();
		_modifiedAssetsCount = 0;
		_editorPane.Clear();
		_contextPane.Clear();
		_modifiedAssetsCountLabel.Visible = false;
		_assetBrowserSearchInput.Value = "";
		_tabs.Clear();
		WeatherDaytimeBar.ResetState();
		ConfigEditorContextPane.ResetState();
		ExportModal.ResetState();
		FilterModal.ResetState();
		Mode = EditorMode.Editor;
		_modeSelection.SelectedTab = Mode.ToString();
		IsBackendInitialized = false;
		_areAssetFilesInitialized = false;
		_areAssetTypesAndSchemasInitialized = false;
		SetFileSaveStatus(SaveStatus.Disabled, doLayout: false);
		CurrentAsset = AssetReference.None;
		TrackedAssets.Clear();
		_assetToOpenOnceAssetFilesInitialized = null;
		AssetTypeRegistry.Clear();
		Diagnostics = new Dictionary<string, AssetDiagnostics>();
		_schemas = new Dictionary<string, SchemaNode>();
		ConfigEditor.Reset();
	}

	public void CleanupWebViews()
	{
		if (_sourceEditor.CodeEditor.IsInitialized)
		{
			_sourceEditor.CodeEditor.DisposeEditor();
		}
		if (CreateAssetModal.CodeEditor.IsInitialized)
		{
			CreateAssetModal.CodeEditor.DisposeEditor();
		}
	}

	public void OnWindowFocusChanged()
	{
		if (!Interface.Engine.Window.IsFocused)
		{
			SaveAll();
		}
	}

	private void OnActivateOptionsButton()
	{
		List<PopupMenuItem> items = new List<PopupMenuItem>
		{
			new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.options.settings"), delegate
			{
				Interface.SettingsModal.Open();
			}),
			new PopupMenuItem(Desktop.Provider.GetText(ConfigEditor.DisplayUnsetProperties ? "ui.assetEditor.options.displayUnsetProperties.on" : "ui.assetEditor.options.displayUnsetProperties.off"), delegate
			{
				ConfigEditor.ToggleDisplayUnsetProperties();
			})
		};
		Popup.SetTitle(Desktop.Provider.GetText("ui.assetEditor.options.title"));
		Popup.SetItems(items);
		Popup.Open();
	}

	private bool CanEditCurrentAsset()
	{
		TrackedAsset value;
		return CurrentAsset.FilePath != null && TrackedAssets.TryGetValue(CurrentAsset.FilePath, out value) && !value.IsLoading && value.FetchError == null;
	}

	public void SetModifiedAssetsCount(int count)
	{
		if (_modifiedAssetsCount != count)
		{
			_modifiedAssetsCount = count;
			_modifiedAssetsCountLabel.Text = Desktop.Provider.FormatNumber(count);
			_modifiedAssetsCountLabel.Visible = count > 0;
			if (_modifiedAssetsCountLabel.IsMounted)
			{
				_modifiedAssetsCountLabel.Parent.Layout();
			}
		}
	}

	public HashSet<string> GetDisplayedAssetTypes()
	{
		return Interface.App.Settings.ActiveAssetTree switch
		{
			AssetTreeFolder.Common => DisplayedCommonAssetTypes, 
			AssetTreeFolder.Server => DisplayedServerAssetTypes, 
			AssetTreeFolder.Cosmetics => DisplayedCosmeticAssetTypes, 
			_ => null, 
		};
	}

	public void OnDisplayedAssetTypesChanged()
	{
		AssetTreeFolder activeAssetTree = Interface.App.Settings.ActiveAssetTree;
		AssetTree assetTree = GetAssetTree(activeAssetTree);
		HashSet<string> displayedAssetTypes = GetDisplayedAssetTypes();
		if (displayedAssetTypes.Count == 0)
		{
			assetTree.DirectoriesToDisplay = null;
			assetTree.AssetTypesToDisplay = null;
		}
		else
		{
			assetTree.DirectoriesToDisplay = new List<string>();
			assetTree.AssetTypesToDisplay = new HashSet<string>();
			foreach (string displayedAssetType in GetDisplayedAssetTypes())
			{
				if (activeAssetTree != AssetTreeFolder.Common)
				{
					assetTree.DirectoriesToDisplay.Add(AssetTypeRegistry.AssetTypes[displayedAssetType].Path);
				}
				assetTree.AssetTypesToDisplay.Add(displayedAssetType);
			}
		}
		assetTree.BuildTree();
		assetTree.Layout();
	}

	private void OnAssetTreeSelectionChanged(AssetTreeFolder assetTree)
	{
		AssetEditorApp app = Interface.App;
		AssetEditorSettings settings = app.Settings;
		settings = settings.Clone();
		settings.ActiveAssetTree = assetTree;
		app.ApplySettings(settings);
		UpdateActiveAssetTree(doLayout: true);
	}

	private void UpdateActiveAssetTree(bool doLayout)
	{
		AssetEditorSettings settings = Interface.App.Settings;
		_assetTreeSelection.SelectedTab = settings.ActiveAssetTree.ToString();
		_cosmeticsAssetTree.Visible = settings.ActiveAssetTree == AssetTreeFolder.Cosmetics;
		_commonAssetTree.Visible = settings.ActiveAssetTree == AssetTreeFolder.Common;
		_serverAssetTree.Visible = settings.ActiveAssetTree == AssetTreeFolder.Server;
		UpdateAssetTreeSearch(doLayout: false);
		if (doLayout)
		{
			_assetBrowser.Layout();
		}
	}

	private void UpdateAssetTreeSearch(bool doLayout = true)
	{
		AssetTree assetTree = GetAssetTree(Interface.App.Settings.ActiveAssetTree);
		if (!(assetTree.SearchQuery == _assetBrowserSearchInput.Value))
		{
			assetTree.SearchQuery = _assetBrowserSearchInput.Value;
			assetTree.BuildTree();
			if (doLayout)
			{
				assetTree.Layout();
			}
		}
	}

	private void CollapseAllDirectoriesInAssetTree()
	{
		AssetEditorApp app = Interface.App;
		AssetEditorSettings settings = app.Settings;
		settings.UncollapsedDirectories.Clear();
		app.SaveSettings();
		_commonAssetTree.ClearCollapsedStates();
		_commonAssetTree.BuildTree();
		_serverAssetTree.ClearCollapsedStates();
		_serverAssetTree.BuildTree();
		_cosmeticsAssetTree.ClearCollapsedStates();
		_cosmeticsAssetTree.BuildTree();
	}

	private void UpdateAssetTreeUncollapsedStateFromSettings()
	{
		_commonAssetTree.ClearCollapsedStates();
		_serverAssetTree.ClearCollapsedStates();
		_cosmeticsAssetTree.ClearCollapsedStates();
		foreach (string uncollapsedDirectory in Interface.App.Settings.UncollapsedDirectories)
		{
			if (uncollapsedDirectory.StartsWith("Common/"))
			{
				_commonAssetTree.SetUncollapsedState(uncollapsedDirectory, uncollapsed: true, bypassCallback: true);
			}
			else if (uncollapsedDirectory.StartsWith("Server/"))
			{
				_serverAssetTree.SetUncollapsedState(uncollapsedDirectory, uncollapsed: true, bypassCallback: true);
			}
			else if (uncollapsedDirectory.StartsWith("Cosmetics/"))
			{
				_cosmeticsAssetTree.SetUncollapsedState(uncollapsedDirectory, uncollapsed: true, bypassCallback: true);
			}
		}
	}

	private void OnChangeMode(EditorMode mode)
	{
		if (Mode == mode)
		{
			return;
		}
		if (CurrentAsset.FilePath != null && mode == EditorMode.Editor && AssetTypeRegistry.AssetTypes[CurrentAsset.Type].Schema == null)
		{
			_modeSelection.SelectedTab = Mode.ToString();
			_modeSelection.Layout();
			ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, Interface.GetText("ui.assetEditor.errors.assetTypeNotSetupForEditor"));
			return;
		}
		FinishWork();
		switch (mode)
		{
		case EditorMode.Editor:
			if (CheckHasUnsavedSourceEditorChanges(delegate
			{
				OnChangeMode(EditorMode.Editor);
			}))
			{
				_modeSelection.SelectedTab = Mode.ToString();
				_modeSelection.Layout();
			}
			else
			{
				Mode = EditorMode.Editor;
				SetupEditorPane();
			}
			break;
		case EditorMode.Source:
			Mode = EditorMode.Source;
			SetupEditorPane();
			break;
		}
	}

	public void SetAssetTreeInitializing(bool isInitializing)
	{
		_areAssetFilesInitialized = !isInitializing;
		AssetEditorSettings settings = Interface.App.Settings;
		_cosmeticsAssetTree.Visible = settings.ActiveAssetTree == AssetTreeFolder.Cosmetics && !isInitializing;
		_commonAssetTree.Visible = settings.ActiveAssetTree == AssetTreeFolder.Common && !isInitializing;
		_serverAssetTree.Visible = settings.ActiveAssetTree == AssetTreeFolder.Server && !isInitializing;
		_assetBrowserSpinner.Visible = isInitializing;
		_assetBrowser.Layout();
	}

	private void SetupSearchFilter(string filter)
	{
		_assetBrowserSearchInput.Value = filter + ": ";
		_assetBrowserSearchInput.ValueChanged();
		Desktop.FocusElement(_assetBrowserSearchInput);
	}

	private void OnAssetTreeCollapseStateChanged(string path, bool uncollapsed)
	{
		AssetEditorApp app = Interface.App;
		AssetEditorSettings settings = app.Settings;
		if (!uncollapsed)
		{
			settings.UncollapsedDirectories.Remove(path);
		}
		else
		{
			settings.UncollapsedDirectories.Add(path);
		}
		app.SaveSettings();
	}

	private void SelectAssetTreeEntry(AssetReference assetReference, bool bringIntoView = false, bool doLayout = true)
	{
		_commonAssetTree.DeselectEntry();
		_serverAssetTree.DeselectEntry();
		_cosmeticsAssetTree.DeselectEntry();
		AssetEditorSettings settings = Interface.App.Settings;
		if (AssetTypeRegistry.AssetTypes.TryGetValue(assetReference.Type, out var value))
		{
			if (value.AssetTree != settings.ActiveAssetTree)
			{
				settings = settings.Clone();
				settings.ActiveAssetTree = value.AssetTree;
				Interface.App.ApplySettings(settings);
				UpdateActiveAssetTree(doLayout);
			}
			GetAssetTree(value.AssetTree).SelectEntry(assetReference, bringIntoView);
		}
	}

	private void OpenChangelogModalIfNewVersion()
	{
		AssetEditorSettings settings = Interface.App.Settings;
		string lastUsedVersion = settings.LastUsedVersion;
		bool flag = false;
		if (lastUsedVersion != null)
		{
			Version version = new Version("0.1.0");
			Version version2 = new Version(lastUsedVersion);
			if (version > version2)
			{
				flag = true;
				ChangelogModal.PreviouslyUsedVersion = version2;
				Desktop.SetLayer(4, ChangelogModal);
			}
		}
		else
		{
			flag = true;
			Desktop.SetLayer(4, ChangelogModal);
		}
		if (flag)
		{
			settings = settings.Clone();
			settings.LastUsedVersion = "0.1.0";
			Interface.App.ApplySettings(settings);
		}
	}

	private void OpenCreateAssetModal()
	{
		if (CheckHasUnsavedSourceEditorChanges(OpenCreateAssetModal))
		{
			return;
		}
		string text = CurrentAsset.Type ?? AssetTypeRegistry.AssetTypes.Keys.FirstOrDefault();
		if (text != null && AssetTypeRegistry.AssetTypes[text].AssetTree == AssetTreeFolder.Common)
		{
			foreach (KeyValuePair<string, AssetTypeConfig> assetType in AssetTypeRegistry.AssetTypes)
			{
				if (assetType.Value.AssetTree == AssetTreeFolder.Common)
				{
					continue;
				}
				text = assetType.Key;
				break;
			}
		}
		if (text != null)
		{
			CreateAssetModal.Open(text);
		}
	}

	private AssetTree GetAssetTree(AssetTreeFolder assetTreeFolder)
	{
		return assetTreeFolder switch
		{
			AssetTreeFolder.Server => _serverAssetTree, 
			AssetTreeFolder.Common => _commonAssetTree, 
			AssetTreeFolder.Cosmetics => _cosmeticsAssetTree, 
			_ => throw new Exception("Invalid type: " + assetTreeFolder), 
		};
	}

	public void FocusAssetInTree(AssetReference assetReference)
	{
		AssetTypeConfig assetTypeConfig = AssetTypeRegistry.AssetTypes[assetReference.Type];
		AssetEditorSettings settings = Interface.App.Settings;
		if (assetTypeConfig.AssetTree != settings.ActiveAssetTree)
		{
			settings.ActiveAssetTree = assetTypeConfig.AssetTree;
			Interface.App.ApplySettings(settings);
			UpdateActiveAssetTree(doLayout: true);
		}
		_serverAssetTree.BringEntryIntoView(assetReference);
	}

	private void ToggleDiagnosticsPane()
	{
		_isDiagnosticsPaneOpen = !_isDiagnosticsPaneOpen;
		_diagnosticsPane.Visible = _isDiagnosticsPaneOpen;
		if (_isDiagnosticsPaneOpen)
		{
			UpdateDiagnostics();
		}
		Layout();
	}

	private void UpdateDiagnostics()
	{
		Interface.TryGetDocument("AssetEditor/DiagnosticsPaneEntry.ui", out var document);
		if (_errorCount > 0)
		{
			_errorsInfo.Visible = true;
			_errorsInfo.Find<Label>("Count").Text = Desktop.Provider.FormatNumber(_errorCount);
		}
		else
		{
			_errorsInfo.Visible = false;
		}
		if (_warningCount > 0)
		{
			_warningsInfo.Visible = true;
			_warningsInfo.Find<Label>("Count").Text = Desktop.Provider.FormatNumber(_warningCount);
		}
		else
		{
			_warningsInfo.Visible = false;
		}
		_validatedInfo.Visible = !_warningsInfo.Visible && !_errorsInfo.Visible;
		if (!_isDiagnosticsPaneOpen)
		{
			return;
		}
		_diagnosticsPane.Clear();
		foreach (KeyValuePair<string, AssetDiagnostics> entry in Diagnostics)
		{
			if (entry.Value.Errors != null)
			{
				AssetDiagnosticMessage[] errors = entry.Value.Errors;
				for (int i = 0; i < errors.Length; i++)
				{
					AssetDiagnosticMessage assetDiagnosticMessage = errors[i];
					UIFragment uIFragment = document.Instantiate(Desktop, _diagnosticsPane);
					uIFragment.Get<Label>("File").Text = entry.Key;
					uIFragment.Get<Label>("Message").Text = assetDiagnosticMessage.Property.ToString() + ": " + assetDiagnosticMessage.Message;
					uIFragment.Get<Button>("Button").Activating = delegate
					{
						OpenExistingAsset(entry.Key, bringAssetIntoAssetTreeView: true);
					};
					uIFragment.Get<Group>("Icon").Background = new PatchStyle("AssetEditor/ErrorIcon.png");
				}
			}
			if (entry.Value.Warnings == null)
			{
				continue;
			}
			AssetDiagnosticMessage[] warnings = entry.Value.Warnings;
			for (int j = 0; j < warnings.Length; j++)
			{
				AssetDiagnosticMessage assetDiagnosticMessage2 = warnings[j];
				UIFragment uIFragment2 = document.Instantiate(Desktop, _diagnosticsPane);
				uIFragment2.Get<Label>("File").Text = entry.Key;
				uIFragment2.Get<Label>("Message").Text = assetDiagnosticMessage2.Property.ToString() + ": " + assetDiagnosticMessage2.Message;
				uIFragment2.Get<Button>("Button").Activating = delegate
				{
					OpenExistingAsset(entry.Key, bringAssetIntoAssetTreeView: true);
				};
				uIFragment2.Get<Group>("Icon").Background = new PatchStyle("AssetEditor/WarningIcon.png");
			}
		}
	}

	private void AddTab(AssetReference assetReference, bool setActive = true)
	{
		bool flag = false;
		EditorTabButton editorTabButton = null;
		int num = -1;
		for (int i = 0; i < _tabs.Children.Count; i++)
		{
			EditorTabButton editorTabButton2 = (EditorTabButton)_tabs.Children[i];
			if (editorTabButton == null || editorTabButton.TimeLastActive > editorTabButton2.TimeLastActive)
			{
				editorTabButton = editorTabButton2;
			}
			if (editorTabButton2.IsActive)
			{
				num = i;
			}
			if (editorTabButton2.AssetReference.Equals(assetReference))
			{
				if (setActive)
				{
					editorTabButton2.SetActive(active: true);
				}
				flag = true;
				_tabs.ScrollChildElementIntoView(editorTabButton2);
			}
			else if (setActive)
			{
				editorTabButton2.SetActive(active: false);
			}
		}
		if (!flag)
		{
			EditorTabButton editorTabButton3 = new EditorTabButton(this, assetReference);
			if (num > -1)
			{
				_tabs.Add(editorTabButton3, num + 1);
			}
			else
			{
				_tabs.Add(editorTabButton3);
			}
			editorTabButton3.Build();
			if (setActive)
			{
				editorTabButton3.SetActive(active: true);
			}
			if (_tabs.Children.Count > 25)
			{
				_tabs.Remove(editorTabButton);
			}
			_tabs.Layout();
			if (base.IsMounted)
			{
				_tabs.ScrollChildElementIntoView(editorTabButton3);
			}
		}
	}

	public void CloseAllTabs()
	{
		if (CurrentAsset.FilePath != null)
		{
			_serverAssetTree.DeselectEntry();
			_commonAssetTree.DeselectEntry();
			_cosmeticsAssetTree.DeselectEntry();
			CurrentAsset = AssetReference.None;
			Interface.App.Editor.ClearPreview(updateUi: false);
			Backend.SetOpenEditorAsset(CurrentAsset);
			_editorPane.Clear();
			_editorPane.Layout();
		}
		_tabs.Clear();
		_tabs.Layout();
	}

	public void CloseTab(AssetReference assetReference)
	{
		bool flag = false;
		foreach (Element child in _tabs.Children)
		{
			EditorTabButton editorTabButton = (EditorTabButton)child;
			if (!editorTabButton.AssetReference.Equals(assetReference))
			{
				continue;
			}
			_tabs.Remove(editorTabButton);
			flag = true;
			break;
		}
		if (flag)
		{
			_tabs.Layout();
		}
		if (!assetReference.Equals(CurrentAsset))
		{
			return;
		}
		EditorTabButton editorTabButton2 = null;
		foreach (Element child2 in _tabs.Children)
		{
			EditorTabButton editorTabButton3 = (EditorTabButton)child2;
			if (editorTabButton2 == null || editorTabButton3.TimeLastActive > editorTabButton2.TimeLastActive)
			{
				editorTabButton2 = editorTabButton3;
			}
		}
		if (editorTabButton2 != null)
		{
			OpenExistingAsset(editorTabButton2.AssetReference);
			return;
		}
		_serverAssetTree.DeselectEntry();
		_commonAssetTree.DeselectEntry();
		_cosmeticsAssetTree.DeselectEntry();
		CurrentAsset = AssetReference.None;
		Interface.App.Editor.ClearPreview(updateUi: false);
		Backend.SetOpenEditorAsset(CurrentAsset);
		SetupEditorPane();
	}

	public void UpdateTab(AssetReference oldReference, AssetReference newReference)
	{
		bool flag = false;
		foreach (Element child in _tabs.Children)
		{
			EditorTabButton editorTabButton = (EditorTabButton)child;
			if (!editorTabButton.AssetReference.Equals(oldReference))
			{
				continue;
			}
			editorTabButton.OnAssetRenamed(newReference);
			flag = true;
			break;
		}
		if (flag)
		{
			_tabs.Layout();
		}
	}

	public void OpenExistingAsset(AssetReference assetReference, bool bringAssetIntoAssetTreeView = false)
	{
		if (!CheckHasUnsavedSourceEditorChanges(delegate
		{
			OpenExistingAsset(assetReference, bringAssetIntoAssetTreeView);
		}))
		{
			SelectAssetTreeEntry(assetReference, bringAssetIntoAssetTreeView);
			if (!CurrentAsset.Equals(assetReference))
			{
				CurrentAsset = assetReference;
				Interface.App.Editor.ClearPreview(updateUi: false);
				Backend.SetOpenEditorAsset(assetReference);
				Logger.Info<string, string>("Opening asset {0}:{1}", assetReference.Type, assetReference.FilePath);
				AddTab(assetReference);
				FetchOpenAsset();
			}
		}
	}

	public void OpenExistingAsset(string filePath, bool bringAssetIntoAssetTreeView = false)
	{
		string assetType;
		if (!_areAssetTypesAndSchemasInitialized)
		{
			_assetToOpenOnceAssetFilesInitialized = new AssetToOpen
			{
				FilePath = filePath
			};
		}
		else if (AssetTypeRegistry.TryGetAssetTypeFromPath(filePath, out assetType) && !CheckHasUnsavedSourceEditorChanges(delegate
		{
			OpenExistingAsset(filePath, bringAssetIntoAssetTreeView);
		}))
		{
			OpenExistingAsset(new AssetReference(assetType, filePath), bringAssetIntoAssetTreeView);
		}
	}

	public void OpenExistingAssetById(AssetIdReference assetIdReference, bool bringAssetIntoAssetTreeView = false)
	{
		if (CheckHasUnsavedSourceEditorChanges(delegate
		{
			OpenExistingAssetById(assetIdReference, bringAssetIntoAssetTreeView);
		}))
		{
			return;
		}
		if (_areAssetFilesInitialized)
		{
			if (Assets.TryGetPathForAssetId(assetIdReference.Type, assetIdReference.Id, out var filePath))
			{
				OpenExistingAsset(new AssetReference(assetIdReference.Type, filePath), bringAssetIntoAssetTreeView);
			}
			else
			{
				Logger.Warn<string, string>("Failed to late open asset since a path for this id could not be found: {0} ({1})", assetIdReference.Id, assetIdReference.Type);
			}
		}
		else
		{
			_assetToOpenOnceAssetFilesInitialized = new AssetToOpen
			{
				Id = assetIdReference
			};
		}
	}

	public void FetchOpenAsset()
	{
		ResetTrackedAssets();
		FetchTrackedAsset(CurrentAsset, isFirstRequest: true);
		SetupEditorPane();
	}

	private void ResetTrackedAssets()
	{
		FinishWork();
		_awaitingInitialEditorSetup = true;
		_currentAssetCancellationToken?.Cancel();
		_currentAssetCancellationToken = new CancellationTokenSource();
		TrackedAssets.Clear();
	}

	public void FetchTrackedAsset(AssetReference assetReference, bool isFirstRequest = false)
	{
		CancellationToken cancelToken = _currentAssetCancellationToken.Token;
		if (!TrackedAssets.TryGetValue(assetReference.FilePath, out var trackedAsset))
		{
			trackedAsset = new TrackedAsset(assetReference, null);
			TrackedAssets[assetReference.FilePath] = trackedAsset;
		}
		if (trackedAsset.IsLoading)
		{
			return;
		}
		trackedAsset.FetchError = null;
		trackedAsset.IsLoading = true;
		AssetTypeConfig assetTypeConfig = AssetTypeRegistry.AssetTypes[assetReference.Type];
		if (assetTypeConfig.AssetTree != AssetTreeFolder.Cosmetics && assetTypeConfig.IsJson && assetTypeConfig.Schema != null)
		{
			Backend.FetchJsonAssetWithParents(assetReference, delegate(Dictionary<string, TrackedAsset> results, FormattedMessage fetchParentsError)
			{
				if (!cancelToken.IsCancellationRequested && TrackedAssets.TryGetValue(assetReference.FilePath, out trackedAsset) && trackedAsset.IsLoading)
				{
					if (fetchParentsError == null)
					{
						foreach (KeyValuePair<string, TrackedAsset> result in results)
						{
							TrackedAssets[result.Key] = result.Value;
						}
					}
					else
					{
						trackedAsset.IsLoading = false;
						trackedAsset.FetchError = fetchParentsError;
						ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, fetchParentsError);
					}
					OnTrackedAssetUpdated(TrackedAssets[assetReference.FilePath], isFirstRequest);
				}
			}, trackUpdates: true);
			return;
		}
		Backend.FetchAsset(assetReference, delegate(object data, FormattedMessage error)
		{
			if (!cancelToken.IsCancellationRequested && TrackedAssets.TryGetValue(assetReference.FilePath, out trackedAsset) && trackedAsset.IsLoading)
			{
				trackedAsset.IsLoading = false;
				trackedAsset.FetchError = error;
				trackedAsset.Data = data;
				if (error != null)
				{
					ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, error);
				}
				OnTrackedAssetUpdated(trackedAsset, isFirstRequest);
			}
		}, trackUpdates: true);
	}

	private void OnTrackedAssetUpdated(TrackedAsset asset, bool isFirstRequest = false)
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		if (_awaitingInitialEditorSetup && !isFirstRequest)
		{
			return;
		}
		if (_awaitingInitialEditorSetup)
		{
			_awaitingInitialEditorSetup = false;
			SetupEditorPane();
		}
		else
		{
			if (!base.IsMounted)
			{
				return;
			}
			if (asset.Reference.Equals(CurrentAsset))
			{
				if (ConfigEditor.IsMounted)
				{
					ConfigEditor.UpdateJson((JObject)asset.Data);
				}
				else
				{
					SetupEditorPane();
				}
			}
			if (ConfigEditorContextPane.IsMounted)
			{
				ConfigEditorContextPane.OnTrackedAssetChanged(asset);
			}
		}
	}

	public void ReloadInheritanceStack()
	{
		AssetReference currentAsset = CurrentAsset;
		if (!AssetTypeRegistry.AssetTypes.TryGetValue(currentAsset.Type, out var value) || value.AssetTree == AssetTreeFolder.Cosmetics)
		{
			return;
		}
		_reloadInheritanceStackCancellationTokenSource?.Cancel();
		_reloadInheritanceStackCancellationTokenSource = new CancellationTokenSource();
		CancellationToken cancellationToken = _reloadInheritanceStackCancellationTokenSource.Token;
		Backend.FetchJsonAssetWithParents(currentAsset, delegate(Dictionary<string, TrackedAsset> results, FormattedMessage error)
		{
			if (currentAsset.Equals(CurrentAsset) && !cancellationToken.IsCancellationRequested)
			{
				TrackedAssets.Clear();
				if (error == null)
				{
					foreach (KeyValuePair<string, TrackedAsset> result in results)
					{
						TrackedAssets[result.Key] = result.Value;
					}
					return;
				}
				ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, Desktop.Provider.GetText("ui.assetEditor.errors.failedToFetchParent"));
			}
		});
	}

	public void FetchParents(AssetReference assetReference, JObject jObject, Action<List<TrackedAsset>, FormattedMessage> callback)
	{
		List<TrackedAsset> list = new List<TrackedAsset>
		{
			new TrackedAsset(assetReference, jObject)
		};
		FetchNext(jObject);
		void FetchNext(JObject obj)
		{
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Invalid comparison between Unknown and I4
			if (!obj.ContainsKey("Parent") || (int)obj["Parent"].Type != 8)
			{
				Interface.Engine.RunOnMainThread(Backend, delegate
				{
					callback(list, null);
				}, allowCallFromMainThread: true);
			}
			else
			{
				string text = (string)obj["Parent"];
				foreach (TrackedAsset item in list)
				{
					if (GetAssetIdFromReference(item.Reference) == text)
					{
						Interface.Engine.RunOnMainThread(Backend, delegate
						{
							callback(null, FormattedMessage.FromMessageId("ui.assetEditor.errors.recursiveParent"));
						}, allowCallFromMainThread: true);
						return;
					}
				}
				if (!Assets.TryGetPathForAssetId(assetReference.Type, text, out var filePath))
				{
					Logger.Warn("Failed to look up path for asset parent {0}", text);
					Interface.Engine.RunOnMainThread(Backend, delegate
					{
						callback(null, FormattedMessage.FromMessageId("ui.assetEditor.errors.errorOccurredFetching"));
					}, allowCallFromMainThread: true);
				}
				else
				{
					AssetReference nextAssetReference = new AssetReference(assetReference.Type, filePath);
					Backend.FetchAsset(nextAssetReference, delegate(object data, FormattedMessage error)
					{
						//IL_0059: Unknown result type (might be due to invalid IL or missing references)
						//IL_0063: Expected O, but got Unknown
						if (error != null)
						{
							callback(null, error);
						}
						else
						{
							Logger.Info("Loaded parent {0}", filePath);
							list.Add(new TrackedAsset(nextAssetReference, data));
							FetchNext((JObject)data);
						}
					});
				}
			}
		}
	}

	private void SetupEditorPane()
	{
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Invalid comparison between Unknown and I4
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Invalid comparison between Unknown and I4
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Invalid comparison between Unknown and I4
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Invalid comparison between Unknown and I4
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Expected O, but got Unknown
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Invalid comparison between Unknown and I4
		WeatherDaytimeBar.Parent?.Remove(WeatherDaytimeBar);
		ConfigEditorContextPane.SetActiveCategory(null);
		if (CurrentAsset.Equals(AssetReference.None) || !TrackedAssets.TryGetValue(CurrentAsset.FilePath, out var value))
		{
			_contextPane.Clear();
			_editorPane.Clear();
			_contentPane.Layout();
			return;
		}
		if (value.IsLoading)
		{
			_contextPane.Clear();
			_editorPane.Clear();
			_editorPane.Add(_assetLoadingIndicator);
			_contentPane.Layout();
			return;
		}
		if (value.Data == null || value.FetchError != null)
		{
			FormattedMessage formattedMessage = FormattedMessage.FromMessageId("ui.assetEditor.errors.failedToFetchAsset", new Dictionary<string, object> { { "path", CurrentAsset.FilePath } });
			if (value.FetchError != null)
			{
				formattedMessage.Children = new List<FormattedMessage>
				{
					new FormattedMessage
					{
						RawText = "\n\n"
					},
					value.FetchError
				};
			}
			ShowAssetError(formattedMessage);
			return;
		}
		if (!AssetTypeRegistry.AssetTypes.TryGetValue(CurrentAsset.Type, out var value2))
		{
			ShowAssetError(new FormattedMessage
			{
				MessageId = "ui.assetEditor.errors.unknownAssetType",
				Params = new Dictionary<string, object> { { "assetType", CurrentAsset.Type } }
			});
			return;
		}
		if ((int)value2.EditorType != 3 && (int)value2.EditorType != 2 && (int)value2.EditorType != 1)
		{
			ShowAssetError(FormattedMessage.FromMessageId("ui.assetEditor.errors.fileFormatNotSupported"));
			return;
		}
		if (Mode == EditorMode.Editor && value2.Schema == null)
		{
			Mode = EditorMode.Source;
			_modeSelection.SelectedTab = EditorMode.Source.ToString();
			_modeSelection.Layout();
		}
		Element element;
		if (Mode == EditorMode.Editor)
		{
			ConfigEditor.Setup(value2, (JObject)value.Data, CurrentAsset);
			element = ConfigEditor;
		}
		else
		{
			_sourceEditor.Setup(value.Data.ToString(), ((int)value2.EditorType != 2 && (int)value2.EditorType != 3) ? WebCodeEditor.EditorLanguage.Plaintext : WebCodeEditor.EditorLanguage.Json, CurrentAsset);
			element = _sourceEditor;
		}
		if (!element.IsMounted)
		{
			_editorPane.Clear();
			_editorPane.Add(element);
		}
		if (value2.EditorFeatures != null && value2.EditorFeatures.Contains(AssetTypeConfig.EditorFeature.WeatherDaytimeBar))
		{
			_editorPane.Add(WeatherDaytimeBar, 0);
		}
		bool visible = ConfigEditor.Categories.Count > 1 || value2.Preview != AssetTypeConfig.PreviewType.None;
		_contextPane.Visible = visible;
		ConfigEditorContextPane.Update();
		if (ConfigEditorContextPane.Parent == null)
		{
			_contextPane.Clear();
			_contextPane.Add(ConfigEditorContextPane);
		}
		_contentPane.Layout();
	}

	private void ShowAssetError(FormattedMessage message)
	{
		_contextPane.Clear();
		_editorPane.Clear();
		new Label(Desktop, _editorPane)
		{
			TextSpans = FormattedMessageConverter.GetLabelSpans(message, Interface),
			Style = new LabelStyle
			{
				Alignment = LabelStyle.LabelAlignment.Center
			},
			FlexWeight = 1
		};
		_contentPane.Layout();
	}

	private void Reload()
	{
		Backend.Initialize();
	}

	private void FocusSearch()
	{
		Desktop.FocusElement(_assetBrowserSearchInput);
		_assetBrowserSearchInput.SelectAll();
	}

	public void UndoChanges()
	{
		if (CanEditCurrentAsset())
		{
			ConfigEditor.SubmitPendingUpdateCommands();
			Backend.UndoChanges(CurrentAsset);
		}
	}

	public void RedoChanges()
	{
		if (CanEditCurrentAsset())
		{
			ConfigEditor.SubmitPendingUpdateCommands();
			Backend.RedoChanges(CurrentAsset);
		}
	}

	protected internal override void OnKeyDown(SDL_Keycode keyCode, int repeat)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Invalid comparison between Unknown and I4
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Expected I4, but got Unknown
		base.OnKeyDown(keyCode, repeat);
		if (!Desktop.IsShortcutKeyDown)
		{
			return;
		}
		if ((int)keyCode != 101)
		{
			if ((int)keyCode != 102)
			{
				switch (keyCode - 110)
				{
				case 5:
					if (Mode != EditorMode.Source)
					{
						SaveAll();
					}
					break;
				case 12:
					if (Mode != EditorMode.Source)
					{
						if (Desktop.IsShiftKeyDown && BuildInfo.Platform == Platform.MacOS)
						{
							RedoChanges();
						}
						else
						{
							UndoChanges();
						}
					}
					break;
				case 11:
					if (BuildInfo.Platform != Platform.MacOS)
					{
						RedoChanges();
					}
					break;
				case 6:
					FilterModal.Open();
					break;
				case 1:
					ConfigEditor.ToggleDisplayUnsetProperties();
					break;
				case 2:
					if (ConfigEditor.IsMounted)
					{
						ConfigEditor.FocusPropertySearch();
					}
					break;
				case 0:
					OpenCreateAssetModal();
					break;
				case 4:
					Reload();
					break;
				case 3:
				case 7:
				case 8:
				case 9:
				case 10:
					break;
				}
			}
			else
			{
				FocusSearch();
			}
		}
		else
		{
			ExportCurrentAsset();
		}
	}

	public void ExportCurrentAsset()
	{
		if (Backend != null && Backend.IsEditingRemotely && CurrentAsset.Type != null)
		{
			Backend.ExportAssets(new List<AssetReference> { CurrentAsset });
		}
	}

	public void SaveAll()
	{
		if (CurrentAsset.Type != null)
		{
			switch (Mode)
			{
			case EditorMode.Editor:
				ConfigEditor.SubmitPendingUpdateCommands();
				break;
			case EditorMode.Source:
				if (_sourceEditor.ApplyChanges())
				{
					break;
				}
				Logger.Info("Source Editor validation failed. Skip saving all...");
				return;
			}
		}
		Backend?.SaveUnsavedChanges();
	}

	private bool CheckHasUnsavedSourceEditorChanges(Action onChangesDiscarded)
	{
		if (Mode == EditorMode.Source && _sourceEditor.HasUnsavedChanges)
		{
			ConfirmationModal.Open(Interface.GetText("ui.assetEditor.sourceEditor.unsavedChangesModal.title"), Interface.GetText("ui.assetEditor.sourceEditor.unsavedChangesModal.text"), OnDiscard, null, Interface.GetText("ui.assetEditor.sourceEditor.unsavedChangesModal.discardChanges"));
			return true;
		}
		return false;
		void OnDiscard()
		{
			_sourceEditor.Discard();
			onChangesDiscarded();
		}
	}

	public bool CheckHasUnexportedChanges(bool quit, Action onConfirm)
	{
		if (Backend == null)
		{
			return false;
		}
		FinishWork();
		return false;
	}

	public void RegisterDropdownWithDataset(string dataset, DropdownBox dropdownBox, object extraValue = null)
	{
		if (!_dropdownBoxesWithDataset.TryGetValue(dataset, out var value))
		{
			List<DropdownBox> list2 = (_dropdownBoxesWithDataset[dataset] = new List<DropdownBox>());
			value = list2;
		}
		value.Add(dropdownBox);
		UpdateDropdownDataset(dataset, dropdownBox);
	}

	public void UnregisterDropdownWithDataset(string dataset, DropdownBox dropdownBox)
	{
		if (_dropdownBoxesWithDataset.TryGetValue(dataset, out var value))
		{
			value.Remove(dropdownBox);
		}
	}

	public void UpdateDropdownDataset(string dataset, DropdownBox dropdownBox, object extraValue = null)
	{
		if (!Backend.TryGetDropdownEntriesOrFetch(dataset, out var entries, extraValue))
		{
			return;
		}
		List<DropdownBox.DropdownEntryInfo> list = new List<DropdownBox.DropdownEntryInfo>();
		foreach (string item in entries)
		{
			list.Add(new DropdownBox.DropdownEntryInfo(item, item));
		}
		dropdownBox.Entries = list;
	}

	public void OnDropdownDatasetUpdated(string dataset, List<string> entries)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (!_dropdownBoxesWithDataset.TryGetValue(dataset, out var value))
		{
			return;
		}
		List<DropdownBox.DropdownEntryInfo> list = new List<DropdownBox.DropdownEntryInfo>();
		foreach (string entry in entries)
		{
			list.Add(new DropdownBox.DropdownEntryInfo(entry, entry));
		}
		foreach (DropdownBox item in value)
		{
			item.Entries = list;
			if (item.IsMounted)
			{
				item.Layout();
			}
		}
	}

	public void AttachNotifications(Element parent)
	{
		Remove(ToastNotifications);
		parent.Add(ToastNotifications);
	}

	public void ReparentNotifications()
	{
		ToastNotifications.Parent.Remove(ToastNotifications);
		Add(ToastNotifications);
		if (base.IsMounted)
		{
			ToastNotifications.Layout(_rectangleAfterPadding);
		}
	}

	public void UpdateModelPreview()
	{
		if (ConfigEditorContextPane.IsMounted)
		{
			ConfigEditorContextPane.UpdatePreview();
		}
	}

	public void SetFileSaveStatus(SaveStatus saveStatus, bool doLayout = true)
	{
		if (saveStatus != _fileSaveStatus)
		{
			_fileSaveStatus = saveStatus;
			_fileSaveStatusGroup.Visible = saveStatus != SaveStatus.Disabled;
			_fileSaveStatusGroup.Find<Label>("Label").Text = Desktop.Provider.GetText($"ui.assetEditor.fileSaveStatus.{saveStatus}");
			if (doLayout && base.IsMounted)
			{
				_footer.Layout();
			}
		}
	}

	public void SetupAssetPopup(AssetReference assetReference, List<PopupMenuItem> items)
	{
		items.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.assetBrowser.copyPath"), delegate
		{
			SDL.SDL_SetClipboardText(assetReference.FilePath);
		}));
		items.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.assetBrowser.duplicate"), delegate
		{
			CreateAssetModal.Open(assetReference.Type, assetReference.FilePath);
		}));
		items.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.assetBrowser.rename"), delegate
		{
			RenameModal.OpenForAsset(assetReference, Backend.IsEditingRemotely);
		}));
		items.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.assetBrowser.delete"), delegate
		{
			OpenDeleteAssetPrompt(assetReference);
		}));
		if (Backend is ServerAssetEditorBackend)
		{
			items.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.assetBrowser.export"), delegate
			{
				Backend.ExportAssets(new List<AssetReference> { assetReference });
			}));
			items.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.assetBrowser.exportAndDiscard"), delegate
			{
				Backend.ExportAndDiscardAssets(new List<AssetReference> { assetReference });
			}));
			items.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.assetBrowser.discard"), delegate
			{
				Backend.DiscardChanges(new TimestampedAssetReference(assetReference.FilePath, null));
			}));
		}
		items.Add(new PopupMenuItem(Desktop.Provider.GetText((BuildInfo.Platform == Platform.MacOS) ? "ui.assetEditor.assetBrowser.showInFinder" : "ui.assetEditor.assetBrowser.showInExplorer"), delegate
		{
			RevealAssetInDirectory(assetReference.FilePath);
		}));
	}

	public void OpenIpcOpenEditorConfirmationModal(string serverName)
	{
		string text = Desktop.Provider.GetText("ui.assetEditor.ipcServerConfirmation.title");
		string text2 = Desktop.Provider.GetText("ui.assetEditor.ipcServerConfirmation.description", new Dictionary<string, string> { { "serverName", serverName } });
		ConfirmationModal.Open(text, text2, delegate
		{
			Interface.App.MainMenu.Open();
		});
	}

	private void RevealAssetInDirectory(string assetPath)
	{
		string assetsPath = Interface.App.Settings.AssetsPath;
		string text = Path.Combine(assetsPath, assetPath);
		if (!OpenUtils.TryOpenFileInContainingDirectory(text, assetsPath) && !OpenUtils.TryOpenDirectoryInContainingDirectory(text, assetsPath))
		{
			Logger.Warn("Failed to open {0}", text);
		}
	}

	private void OpenDeleteAssetPrompt(AssetReference assetReference)
	{
		string text = Desktop.Provider.GetText("ui.assetEditor.deleteAssetModal.text", new Dictionary<string, string> { { "assetId", assetReference.FilePath } });
		ConfirmationModal.Open(Desktop.Provider.GetText("ui.assetEditor.deleteAssetModal.title"), text, delegate
		{
			Backend.DeleteAsset(assetReference, ConfirmationModal.ApplyChangesLocally);
		}, null, Desktop.Provider.GetText("ui.assetEditor.deleteAssetModal.confirmButton"), null, Backend.IsEditingRemotely);
	}

	public void UpdatePaneSize(AssetEditorSettings.Panes pane, int size)
	{
		AssetEditorSettings settings = Interface.App.Settings;
		settings.PaneSizes[pane] = size;
		settings.Save();
	}

	private void TryClose()
	{
		if (!CheckHasUnsavedSourceEditorChanges(TryClose))
		{
			Interface.App.Editor.CloseEditor();
		}
	}

	private SchemaNode GetSchema(string schemaReference, string rootSchemaId)
	{
		if (schemaReference.StartsWith("#"))
		{
			schemaReference = rootSchemaId + schemaReference;
		}
		schemaReference = schemaReference.TrimEnd(new char[1] { '#' });
		return _schemas[schemaReference];
	}

	public SchemaNode ResolveSchemaInCurrentContext(SchemaNode schema)
	{
		return ResolveSchema(schema, AssetTypeRegistry.AssetTypes[CurrentAsset.Type].Schema);
	}

	public SchemaNode ResolveSchema(SchemaNode schema, SchemaNode rootSchema)
	{
		if (schema.SchemaReference == null)
		{
			return schema;
		}
		SchemaNode schemaNode = GetSchema(schema.SchemaReference, rootSchema.Id).Clone();
		if (schema.Title != null)
		{
			schemaNode.Title = schema.Title;
		}
		if (schema.Description != null)
		{
			schemaNode.Description = schema.Description;
		}
		if (schema.AllowEmptyObject)
		{
			schemaNode.AllowEmptyObject = true;
		}
		if (schema.RebuildCaches != null)
		{
			schemaNode.RebuildCaches = schema.RebuildCaches;
		}
		if (!schema.IsCollapsedByDefault)
		{
			schemaNode.IsCollapsedByDefault = false;
		}
		schemaNode.RebuildCachesForChildProperties = schema.RebuildCachesForChildProperties;
		schemaNode.DefaultValue = schema.DefaultValue;
		schemaNode.InheritsProperty = schema.InheritsProperty;
		return schemaNode;
	}

	public SchemaNode GetSchemaNodeInCurrentContext(JObject value, PropertyPath path)
	{
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Invalid comparison between Unknown and I4
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Invalid comparison between Unknown and I4
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Invalid comparison between Unknown and I4
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Invalid comparison between Unknown and I4
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Invalid comparison between Unknown and I4
		SchemaNode schemaNode = ResolveSchemaInCurrentContext(AssetTypeRegistry.AssetTypes[CurrentAsset.Type].Schema);
		JToken val = (JToken)(object)value;
		TryResolveTypeSchemaInCurrentContext(val, ref schemaNode);
		if (path.Elements.Length == 0)
		{
			return schemaNode;
		}
		string[] elements = path.Elements;
		foreach (string text in elements)
		{
			switch (schemaNode.Type)
			{
			case SchemaNode.NodeType.List:
			case SchemaNode.NodeType.Map:
				schemaNode = ResolveSchemaInCurrentContext(schemaNode.Value);
				val = ((val != null && (int)val.Type != 10) ? (((int)val.Type != 2) ? (((int)val.Type != 1) ? null : val[(object)text]) : val[(object)int.Parse(text)]) : null);
				break;
			case SchemaNode.NodeType.AssetReferenceOrInline:
			case SchemaNode.NodeType.Object:
				if (schemaNode.Type == SchemaNode.NodeType.AssetReferenceOrInline)
				{
					schemaNode = ResolveSchemaInCurrentContext(schemaNode.Value);
				}
				schemaNode = ResolveSchemaInCurrentContext(schemaNode.Properties[text]);
				val = ((val == null || (int)val.Type == 10) ? null : (((int)val.Type != 1) ? null : val[(object)text]));
				TryResolveTypeSchemaInCurrentContext(val, ref schemaNode);
				break;
			}
		}
		return schemaNode;
	}

	public bool TryResolveTypeSchemaInCurrentContext(JToken value, ref SchemaNode schemaNode)
	{
		if (schemaNode.TypePropertyKey != null)
		{
			if (((value != null) ? value[(object)schemaNode.TypePropertyKey] : null) != null)
			{
				JToken val = value[(object)schemaNode.TypePropertyKey];
				return TryResolveTypeSchemaInCurrentContext((string)val, ref schemaNode);
			}
			if (schemaNode.DefaultTypeSchema != null)
			{
				return TryResolveTypeSchemaInCurrentContext(schemaNode.DefaultTypeSchema, ref schemaNode);
			}
		}
		return false;
	}

	public bool TryResolveTypeSchemaInCurrentContext(string type, ref SchemaNode schemaNode)
	{
		return TryResolveTypeSchema(type, ref schemaNode, AssetTypeRegistry.AssetTypes[CurrentAsset.Type].Schema);
	}

	public bool TryResolveTypeSchema(string type, ref SchemaNode schemaNode, SchemaNode rootSchema)
	{
		for (int i = 0; i < schemaNode.TypeSchemas.Length; i++)
		{
			SchemaNode schema = schemaNode.TypeSchemas[i];
			string text = schemaNode.Value.Enum[i];
			if (type != null && text == type)
			{
				schemaNode = ResolveSchema(schema, rootSchema);
				return true;
			}
		}
		schemaNode = null;
		return false;
	}

	private void MergeParentObject(SchemaNode schema, JObject targetObject, JObject parentObject, SchemaNode rootSchema)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Invalid comparison between Unknown and I4
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Expected O, but got Unknown
		//IL_0195: Expected O, but got Unknown
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Invalid comparison between Unknown and I4
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Invalid comparison between Unknown and I4
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Invalid comparison between Unknown and I4
		foreach (KeyValuePair<string, SchemaNode> property in schema.Properties)
		{
			string key = property.Key;
			if (targetObject.ContainsKey(key))
			{
				if (!property.Value.InheritsProperty)
				{
					continue;
				}
				JToken obj = targetObject[key];
				if (obj == null || (int)obj.Type != 1)
				{
					continue;
				}
				JToken obj2 = parentObject[key];
				if (obj2 == null || (int)obj2.Type != 1)
				{
					continue;
				}
				SchemaNode schemaNode = ResolveSchema(property.Value, rootSchema);
				if (schemaNode.TypePropertyKey != null)
				{
					JToken obj3 = targetObject[key][(object)schemaNode.TypePropertyKey];
					if (obj3 != null && (int)obj3.Type == 8)
					{
						TryResolveTypeSchema((string)targetObject[key][(object)schemaNode.TypePropertyKey], ref schemaNode, rootSchema);
					}
					else
					{
						JToken obj4 = parentObject[key][(object)schemaNode.TypePropertyKey];
						if (obj4 != null && (int)obj4.Type == 8)
						{
							targetObject[key][(object)schemaNode.TypePropertyKey] = parentObject[key][(object)schemaNode.TypePropertyKey];
							TryResolveTypeSchema((string)parentObject[key][(object)schemaNode.TypePropertyKey], ref schemaNode, rootSchema);
						}
					}
				}
				if (schemaNode.MergesProperties)
				{
					MergeParentObject(schemaNode, (JObject)targetObject[key], (JObject)parentObject[key], rootSchema);
				}
			}
			else if (property.Value.InheritsProperty && parentObject.ContainsKey(key))
			{
				targetObject[key] = parentObject[key];
			}
		}
	}

	public void ApplyAssetInheritance(SchemaNode schema, JObject targetAsset, Dictionary<string, TrackedAsset> jsonAssets, SchemaNode rootSchema)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Expected O, but got Unknown
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Expected O, but got Unknown
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Invalid comparison between Unknown and I4
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Expected O, but got Unknown
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Expected O, but got Unknown
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Expected O, but got Unknown
		if (schema.TypePropertyKey != null)
		{
			if (!schema.HasParentProperty)
			{
				return;
			}
			JToken obj = targetAsset["Parent"];
			if (obj == null || (int)obj.Type != 8)
			{
				return;
			}
			SchemaNode schemaNode = ResolveSchema(schema.TypeSchemas[0], rootSchema);
			string assetType = schemaNode.Properties["Parent"].AssetType;
			if (AssetTypeRegistry.AssetTypes.TryGetValue(assetType, out var value) && Assets.TryGetPathForAssetId(value.IdProvider ?? assetType, (string)targetAsset["Parent"], out var filePath) && jsonAssets.TryGetValue(filePath, out var value2))
			{
				JObject val = (JObject)((JToken)(JObject)value2.Data).DeepClone();
				if (assetType == "BlockType")
				{
					val = (JObject)val["BlockType"];
				}
				ApplyAssetInheritance(schema, val, jsonAssets, rootSchema);
				MergeParentObject(schema, targetAsset, val, rootSchema);
			}
			return;
		}
		if (targetAsset.ContainsKey("Parent"))
		{
			SchemaNode schemaNode2 = schema.Properties["Parent"];
			if (schemaNode2 != null && schemaNode2.IsParentProperty)
			{
				SchemaNode schemaNode3 = schema.Properties["Parent"];
				if (AssetTypeRegistry.AssetTypes.TryGetValue(schemaNode3.AssetType, out var value3) && Assets.TryGetPathForAssetId(value3.IdProvider ?? schemaNode3.AssetType, (string)targetAsset["Parent"], out var filePath2) && jsonAssets.TryGetValue(filePath2, out var value4))
				{
					JObject val2 = (JObject)((JToken)(JObject)value4.Data).DeepClone();
					if (schemaNode3.AssetType == "BlockType")
					{
						val2 = (JObject)val2["BlockType"];
					}
					ApplyAssetInheritance(schema, val2, jsonAssets, rootSchema);
					MergeParentObject(schema, targetAsset, val2, rootSchema);
				}
			}
		}
		foreach (KeyValuePair<string, JToken> item in targetAsset)
		{
			if (!schema.Properties.TryGetValue(item.Key, out var value5))
			{
				continue;
			}
			value5 = ResolveSchema(value5, rootSchema);
			JToken value6 = item.Value;
			if (value6 != null && (int)value6.Type == 1)
			{
				if (value5.Type == SchemaNode.NodeType.AssetReferenceOrInline)
				{
					value5 = ResolveSchema(value5.Value, rootSchema);
				}
				if (value5.Type == SchemaNode.NodeType.Object)
				{
					ApplyAssetInheritance(value5, (JObject)item.Value, jsonAssets, rootSchema);
				}
			}
		}
	}
}
