using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using HytaleClient.Protocol;
using SDL2;

namespace HytaleClient.AssetEditor.Interface.Elements;

internal class AssetTree : Element
{
	public class AssetTreeEntry
	{
		public readonly AssetTreeEntryType Type;

		public readonly string Name;

		public readonly string Path;

		public readonly string AssetType;

		public readonly int Indention;

		public readonly bool IsCollapsed;

		public AssetTreeEntry(string name, string path, int indention, AssetTreeEntryType type, string assetType, bool isCollapsed)
		{
			Name = name;
			Path = path;
			Indention = indention;
			Type = type;
			AssetType = assetType;
			IsCollapsed = isCollapsed;
		}
	}

	public enum AssetTreeEntryType
	{
		Type,
		Folder,
		File
	}

	private static readonly Regex DirectoryFilterRegex = new Regex("^(:?.*):");

	private readonly AssetEditorOverlay _assetEditorOverlay;

	private TextTooltipLayer _tooltip;

	private FontFamily _fontFamily;

	private int _rowHeight = 30;

	private int _hoveredEntryIndex = -1;

	private int _focusedEntryIndex = -1;

	private int _activeEntryIndex = -1;

	private AssetReference _activeEntry;

	public Action<AssetTreeEntry> FileEntryActivating;

	public Action<string> SelectingDirectoryFilter;

	public Action<string, bool> CollapseStateChanged;

	public Action FocusSearch;

	private readonly Dictionary<string, TexturePatch> _iconPatches = new Dictionary<string, TexturePatch>();

	private TexturePatch _missingIconPatch;

	private TexturePatch _folderIconPatch;

	private TexturePatch _uncollapseIconPatch;

	private TexturePatch _collapseIconPatch;

	private AssetTreeEntry[] _entries = new AssetTreeEntry[0];

	private List<AssetFile> _assetFiles = new List<AssetFile>();

	private readonly HashSet<string> _uncollapsedEntries = new HashSet<string>();

	public string SearchQuery = "";

	public List<string> DirectoriesToDisplay;

	public HashSet<string> AssetTypesToDisplay;

	public bool PopupMenuEnabled = true;

	public bool ShowVirtualAssets;

	private string _rootPath;

	public ScrollbarStyle ScrollbarStyle
	{
		get
		{
			return _scrollbarStyle;
		}
		set
		{
			_scrollbarStyle = value;
		}
	}

	public TextTooltipStyle TooltipStyle
	{
		set
		{
			_tooltip.Style = value;
		}
	}

	public AssetTree(AssetEditorOverlay assetEditorOverlay, string rootPath, Element parent)
		: base(assetEditorOverlay.Desktop, parent)
	{
		_assetEditorOverlay = assetEditorOverlay;
		_rootPath = rootPath;
		_scrollbarStyle.Size = 8;
		_scrollbarStyle.Spacing = 0;
		FlexWeight = 1;
		_tooltip = new TextTooltipLayer(Desktop)
		{
			ShowDelay = 1.5f
		};
	}

	protected override void OnUnmounted()
	{
		if (Desktop.FocusedElement == this)
		{
			Desktop.FocusElement(null);
		}
		_focusedEntryIndex = -1;
		_hoveredEntryIndex = -1;
		_tooltip.Stop();
	}

	private void PrepareFilters(out string[] searchKeywords, out HashSet<string> directoryEntriesoDisplay, out HashSet<string> directoriesToDisplay)
	{
		List<string> list = ((DirectoriesToDisplay != null) ? new List<string>(DirectoriesToDisplay) : new List<string>());
		string text = SearchQuery.Trim();
		string text2 = null;
		Match match = DirectoryFilterRegex.Match(text);
		if (match.Success)
		{
			text = text.Replace(match.Groups[0].Value, "");
			string text3 = match.Groups[1].Value.Trim();
			if (!text3.StartsWith("/"))
			{
				text3 = text3.ToLowerInvariant();
				foreach (AssetTypeConfig value in _assetEditorOverlay.AssetTypeRegistry.AssetTypes.Values)
				{
					if (value.Id.ToLowerInvariant() != text3)
					{
						continue;
					}
					text2 = value.Path;
					break;
				}
			}
			else
			{
				text2 = text3.TrimStart(new char[1] { '/' });
			}
		}
		if (text2 != null && (list.Count == 0 || AssetPathUtils.IsAnyDirectory(text2, list)))
		{
			list.Add(text2);
		}
		if (list.Count > 0)
		{
			directoryEntriesoDisplay = new HashSet<string>();
			directoriesToDisplay = new HashSet<string>();
			foreach (string item in list)
			{
				directoriesToDisplay.Add(item);
				string[] array = item.Split(new char[1] { '/' });
				string text4 = "";
				string[] array2 = array;
				foreach (string text5 in array2)
				{
					if (text4 != "")
					{
						text4 += "/";
					}
					text4 += text5;
					directoryEntriesoDisplay.Add(text4);
				}
			}
		}
		else
		{
			directoryEntriesoDisplay = null;
			directoriesToDisplay = null;
		}
		searchKeywords = ((text != "") ? (from k in text.ToLowerInvariant().Split(new char[1] { ' ' })
			select k.Trim()).ToArray() : null);
	}

	public void BuildTree()
	{
		_activeEntryIndex = -1;
		PrepareFilters(out var searchKeywords, out var directoryEntriesoDisplay, out var directoriesToDisplay);
		List<AssetTreeEntry> list = new List<AssetTreeEntry>();
		int num = -_rootPath.Split(new char[1] { '/' }).Length;
		bool flag = false;
		string value = "";
		List<AssetTreeEntry> list2 = new List<AssetTreeEntry>();
		int num2 = -1;
		foreach (AssetFile assetFile in _assetFiles)
		{
			if (directoryEntriesoDisplay != null)
			{
				if (assetFile.IsDirectory && directoryEntriesoDisplay.Contains(assetFile.Path))
				{
					if (directoriesToDisplay.Contains(assetFile.Path))
					{
						num2 = assetFile.PathElements.Length;
					}
				}
				else
				{
					if (num2 == -1)
					{
						continue;
					}
					if (assetFile.PathElements.Length <= num2)
					{
						num2 = -1;
						continue;
					}
				}
			}
			if (searchKeywords != null && !assetFile.IsDirectory)
			{
				string text = assetFile.DisplayName.ToLowerInvariant();
				bool flag2 = true;
				string[] array = searchKeywords;
				foreach (string value2 in array)
				{
					if (!text.Contains(value2))
					{
						flag2 = false;
						break;
					}
				}
				if (!flag2)
				{
					continue;
				}
			}
			AssetTreeEntry assetTreeEntry;
			if (assetFile.IsDirectory)
			{
				if (flag && assetFile.Path.StartsWith(value))
				{
					continue;
				}
				flag = searchKeywords == null && !_uncollapsedEntries.Contains(assetFile.Path);
				if (assetFile.AssetType != null && _assetEditorOverlay.AssetTypeRegistry.AssetTypes.TryGetValue(assetFile.AssetType, out var value3))
				{
					value = ((value3.AssetTree != AssetTreeFolder.Cosmetics) ? (assetFile.Path + "/") : (assetFile.Path + "#"));
					if (!ShowVirtualAssets && value3.IsVirtual)
					{
						flag = true;
						continue;
					}
				}
				else
				{
					value = assetFile.Path + "/";
				}
				assetTreeEntry = ((assetFile.AssetType == null) ? new AssetTreeEntry(assetFile.DisplayName, assetFile.Path, num + assetFile.PathElements.Length - 1, AssetTreeEntryType.Folder, null, flag) : new AssetTreeEntry(assetFile.DisplayName, assetFile.Path, num + assetFile.PathElements.Length - 1, AssetTreeEntryType.Type, assetFile.AssetType, flag));
			}
			else
			{
				if (flag)
				{
					if (assetFile.Path.StartsWith(value))
					{
						continue;
					}
					flag = false;
				}
				if (AssetTypesToDisplay != null && !AssetTypesToDisplay.Contains(assetFile.AssetType))
				{
					continue;
				}
				assetTreeEntry = new AssetTreeEntry(assetFile.DisplayName, assetFile.Path, num + assetFile.PathElements.Length - 1, AssetTreeEntryType.File, assetFile.AssetType, isCollapsed: false);
			}
			if (searchKeywords != null)
			{
				if (assetFile.IsDirectory)
				{
					list2.Add(assetTreeEntry);
				}
				else
				{
					if (list2.Count > 0)
					{
						foreach (AssetTreeEntry item in list2)
						{
							if (assetTreeEntry.Path.StartsWith(item.Path))
							{
								list.Add(item);
							}
						}
						list2.Clear();
					}
					list.Add(assetTreeEntry);
				}
			}
			else
			{
				list.Add(assetTreeEntry);
			}
			if (assetTreeEntry.Type == AssetTreeEntryType.File && assetTreeEntry.Path == _activeEntry.FilePath)
			{
				_activeEntryIndex = list.Count - 1;
			}
		}
		_entries = list.ToArray();
	}

	public void UpdateFiles(List<AssetFile> files, string rootPath = null)
	{
		if (rootPath != null)
		{
			_rootPath = rootPath;
		}
		_assetFiles = files;
		BuildTree();
	}

	private void ToggleCollapsedState(string path)
	{
		bool flag = _uncollapsedEntries.Contains(path);
		if (!flag)
		{
			_uncollapsedEntries.Add(path);
		}
		else
		{
			_uncollapsedEntries.Remove(path);
		}
		CollapseStateChanged?.Invoke(path, !flag);
	}

	public void SetUncollapsedState(string path, bool uncollapsed, bool bypassCallback = false)
	{
		if (uncollapsed)
		{
			_uncollapsedEntries.Add(path);
			if (!bypassCallback)
			{
				CollapseStateChanged?.Invoke(path, arg2: true);
			}
		}
		else
		{
			_uncollapsedEntries.Remove(path);
			if (!bypassCallback)
			{
				CollapseStateChanged?.Invoke(path, arg2: false);
			}
		}
	}

	public void ClearCollapsedStates()
	{
		_uncollapsedEntries.Clear();
	}

	public void SelectEntry(AssetReference assetReference, bool bringIntoView = false)
	{
		_activeEntry = assetReference;
		_activeEntryIndex = -1;
		for (int i = 0; i < _entries.Length; i++)
		{
			AssetTreeEntry assetTreeEntry = _entries[i];
			if (assetTreeEntry.Path == _activeEntry.FilePath)
			{
				_activeEntryIndex = i;
				break;
			}
		}
		if (bringIntoView)
		{
			if (_activeEntryIndex != -1)
			{
				ScrollEntryIntoView(_activeEntryIndex);
			}
			else
			{
				BringEntryIntoView(assetReference);
			}
		}
	}

	public void BringEntryIntoView(AssetReference assetReference)
	{
		if (SearchQuery != "")
		{
			return;
		}
		AssetFile assetFile = null;
		foreach (AssetFile assetFile2 in _assetFiles)
		{
			if (assetFile2.Path != assetReference.FilePath)
			{
				continue;
			}
			assetFile = assetFile2;
			break;
		}
		if (assetFile == null)
		{
			return;
		}
		string text = "";
		string[] pathElements = assetFile.PathElements;
		foreach (string text2 in pathElements)
		{
			if (text != "")
			{
				text += "/";
			}
			text += text2;
			SetUncollapsedState(text, uncollapsed: true);
		}
		BuildTree();
		Layout();
		ScrollEntryIntoView(GetEntryIndex(assetFile.Path));
	}

	private int GetEntryIndex(string filePath)
	{
		for (int i = 0; i < _entries.Length; i++)
		{
			AssetTreeEntry assetTreeEntry = _entries[i];
			if (assetTreeEntry.Type == AssetTreeEntryType.File && assetTreeEntry.Path == filePath)
			{
				return i;
			}
		}
		return -1;
	}

	public void DeselectEntry()
	{
		_activeEntry = AssetReference.None;
		_activeEntryIndex = -1;
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		_fontFamily = Desktop.Provider.GetFontFamily("Default");
		_iconPatches.Clear();
		foreach (AssetTypeConfig value in _assetEditorOverlay.AssetTypeRegistry.AssetTypes.Values)
		{
			TexturePatch texturePatch = Desktop.MakeTexturePatch(value.Icon);
			if (!value.IsColoredIcon)
			{
				texturePatch.Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 125);
			}
			_iconPatches.Add(value.Id, texturePatch);
		}
		_missingIconPatch = Desktop.MakeTexturePatch(new PatchStyle(Desktop.Provider.MissingTexture));
		_folderIconPatch = Desktop.MakeTexturePatch(new PatchStyle("AssetEditor/AssetIcons/Folder.png"));
		_collapseIconPatch = Desktop.MakeTexturePatch(new PatchStyle("Common/CaretUncollapsed.png"));
		_uncollapseIconPatch = Desktop.MakeTexturePatch(new PatchStyle("Common/CaretCollapsed.png"));
	}

	protected override void LayoutSelf()
	{
		ContentHeight = Desktop.UnscaleRound(_entries.Length * Desktop.ScaleRound(_rowHeight));
	}

	public override Element HitTest(Point position)
	{
		if (!base.Visible || !_rectangleAfterPadding.Contains(position))
		{
			return null;
		}
		return this;
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		RefreshHoveredEntry();
		if (_hoveredEntryIndex == -1)
		{
			return;
		}
		switch ((uint)evt.Button)
		{
		case 1u:
		{
			_focusedEntryIndex = _hoveredEntryIndex;
			AssetTreeEntry assetTreeEntry = _entries[_hoveredEntryIndex];
			switch (assetTreeEntry.Type)
			{
			case AssetTreeEntryType.Type:
			case AssetTreeEntryType.Folder:
				if (!(SearchQuery != ""))
				{
					ToggleCollapsedState(assetTreeEntry.Path);
					BuildTree();
					Layout();
				}
				break;
			case AssetTreeEntryType.File:
				FileEntryActivating?.Invoke(assetTreeEntry);
				break;
			}
			break;
		}
		case 3u:
			if (PopupMenuEnabled)
			{
				OpenPopupMenu(_entries[_hoveredEntryIndex]);
			}
			break;
		}
	}

	private void OpenPopupMenu(AssetTreeEntry entry)
	{
		PopupMenuLayer popup = _assetEditorOverlay.Popup;
		if (popup.IsMounted)
		{
			popup.Close();
		}
		popup.SetTitle(null);
		switch (entry.Type)
		{
		case AssetTreeEntryType.Type:
			popup.SetItems(new List<PopupMenuItem>
			{
				new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.assetBrowser.create", new Dictionary<string, string> { { "assetType", entry.Name } }), delegate
				{
					OpenCreateAssetModal(entry.AssetType);
				}),
				new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.assetBrowser.findAsset"), delegate
				{
					popup.Close();
					SelectingDirectoryFilter?.Invoke(entry.AssetType);
				})
			});
			break;
		case AssetTreeEntryType.File:
		{
			List<PopupMenuItem> items = new List<PopupMenuItem>();
			_assetEditorOverlay.SetupAssetPopup(new AssetReference(entry.AssetType, entry.Path), items);
			popup.SetItems(items);
			break;
		}
		case AssetTreeEntryType.Folder:
		{
			List<PopupMenuItem> list = new List<PopupMenuItem>();
			bool flag = false;
			if (_assetEditorOverlay.AssetTypeRegistry.TryGetAssetTypesFromDirectoryPath(entry.Path + "/", out var assetTypes))
			{
				foreach (string assetType in assetTypes)
				{
					if (_assetEditorOverlay.AssetTypeRegistry.AssetTypes.TryGetValue(assetType, out var assetTypeConfig))
					{
						list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.assetBrowser.createInFolder", new Dictionary<string, string> { { "assetType", assetTypeConfig.Name } }), delegate
						{
							OpenCreateAssetModal(assetType, null, entry.Path.Replace(assetTypeConfig.Path + "/", ""));
						}));
						if (entry.Path.StartsWith(assetTypeConfig.Path + "/"))
						{
							flag = true;
						}
					}
				}
			}
			list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.assetBrowser.findInFolder"), delegate
			{
				popup.Close();
				SelectingDirectoryFilter?.Invoke("/" + entry.Path);
			}));
			if (flag)
			{
				list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.assetBrowser.rename"), delegate
				{
					OpenRenameDirectoryModal(entry.Path);
				}));
				list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.assetBrowser.delete"), delegate
				{
					OpenDeleteDirectoryPrompt(entry.Path);
				}));
			}
			popup.SetItems(list);
			break;
		}
		}
		popup.Open();
	}

	private void OpenDeleteDirectoryPrompt(string path)
	{
		string text = Desktop.Provider.GetText("ui.assetEditor.deleteDirectoryModal.text", new Dictionary<string, string> { { "path", path } });
		_assetEditorOverlay.ConfirmationModal.Open(Desktop.Provider.GetText("ui.assetEditor.deleteDirectoryModal.title"), text, delegate
		{
			_assetEditorOverlay.Backend.DeleteDirectory(path, _assetEditorOverlay.ConfirmationModal.ApplyChangesLocally, OnDeleted);
		}, null, Desktop.Provider.GetText("ui.assetEditor.deleteDirectoryModal.confirmButton"), null, _assetEditorOverlay.Backend.IsEditingRemotely);
		void OnDeleted(string _, FormattedMessage error)
		{
			if (error != null)
			{
				_assetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)2, error);
			}
			else
			{
				_assetEditorOverlay.ToastNotifications.AddNotification((AssetEditorPopupNotificationType)1, Desktop.Provider.GetText("ui.assetEditor.messages.directoryDeleted"));
			}
		}
	}

	private void OpenCreateAssetModal(string assetType, string assetToCopyPath = null, string path = null)
	{
		_assetEditorOverlay.CreateAssetModal.Open(assetType, assetToCopyPath, path);
	}

	private void OpenRenameDirectoryModal(string path)
	{
		_assetEditorOverlay.RenameModal.OpenForDirectory(path, _assetEditorOverlay.Backend.IsEditingRemotely);
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		Desktop.FocusElement(this);
	}

	protected internal override void OnKeyDown(SDL_Keycode keyCode, int repeat)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Invalid comparison between Unknown and I4
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected I4, but got Unknown
		if (Desktop.IsShortcutKeyDown && (int)keyCode == 102)
		{
			FocusSearch();
		}
		else if ((int)keyCode != 13)
		{
			switch (keyCode - 1073741903)
			{
			case 2:
				if (_focusedEntryIndex == -1 && _entries.Length > 1)
				{
					_focusedEntryIndex = 1;
				}
				else
				{
					_focusedEntryIndex++;
					if (_focusedEntryIndex >= _entries.Length)
					{
						_focusedEntryIndex = 0;
					}
				}
				ScrollEntryIntoView(_focusedEntryIndex);
				break;
			case 3:
				_focusedEntryIndex--;
				if (_focusedEntryIndex < 0)
				{
					_focusedEntryIndex = _entries.Length - 1;
				}
				ScrollEntryIntoView(_focusedEntryIndex);
				break;
			case 0:
				if (_focusedEntryIndex != -1 && SearchQuery == "")
				{
					AssetTreeEntry assetTreeEntry2 = _entries[_focusedEntryIndex];
					if (assetTreeEntry2.Type != AssetTreeEntryType.File)
					{
						SetUncollapsedState(assetTreeEntry2.Path, uncollapsed: true);
						BuildTree();
						Layout();
					}
				}
				break;
			case 1:
				if (_focusedEntryIndex != -1 && SearchQuery == "")
				{
					AssetTreeEntry assetTreeEntry = _entries[_focusedEntryIndex];
					if (assetTreeEntry.Type != AssetTreeEntryType.File)
					{
						SetUncollapsedState(assetTreeEntry.Path, uncollapsed: false);
						BuildTree();
						Layout();
					}
				}
				break;
			}
		}
		else if (_focusedEntryIndex != -1)
		{
			AssetTreeEntry assetTreeEntry3 = _entries[_focusedEntryIndex];
			if (assetTreeEntry3.Type == AssetTreeEntryType.File)
			{
				FileEntryActivating?.Invoke(assetTreeEntry3);
			}
		}
	}

	protected internal override void OnBlur()
	{
		_focusedEntryIndex = -1;
	}

	protected override void OnMouseMove()
	{
		RefreshHoveredEntry();
	}

	protected override void OnMouseEnter()
	{
		RefreshHoveredEntry();
	}

	protected override void OnMouseLeave()
	{
		RefreshHoveredEntry();
	}

	private void RefreshHoveredEntry()
	{
		if (!_viewRectangle.Contains(Desktop.MousePosition))
		{
			_tooltip.Stop();
			_hoveredEntryIndex = -1;
			return;
		}
		int num = Desktop.ScaleRound(_rowHeight);
		int num2 = (int)((float)(Desktop.MousePosition.Y - _rectangleAfterPadding.Y + _scaledScrollOffset.Y) / (float)num);
		int hoveredEntryIndex = _hoveredEntryIndex;
		_hoveredEntryIndex = ((num2 < _entries.Length) ? num2 : (-1));
		if (_hoveredEntryIndex == hoveredEntryIndex)
		{
			return;
		}
		if (_hoveredEntryIndex > -1)
		{
			AssetTreeEntry assetTreeEntry = _entries[_hoveredEntryIndex];
			if (assetTreeEntry.Type == AssetTreeEntryType.File)
			{
				_tooltip.TextSpans = new List<Label.LabelSpan>
				{
					new Label.LabelSpan
					{
						Text = _assetEditorOverlay.AssetTypeRegistry.AssetTypes[assetTreeEntry.AssetType].Name + ": ",
						IsBold = true
					},
					new Label.LabelSpan
					{
						Text = assetTreeEntry.Path
					}
				};
			}
			else
			{
				_tooltip.Text = assetTreeEntry.Path;
			}
			_tooltip.Start(resetTimer: true);
			_tooltip.Layout();
		}
		else
		{
			_tooltip.Stop();
		}
	}

	private void ScrollEntryIntoView(int entryIndex)
	{
		int num = Desktop.ScaleRound(_rowHeight);
		if (num * (entryIndex + 1) > _anchoredRectangle.Height + _scaledScrollOffset.Y)
		{
			SetScroll(0, num * (entryIndex + 1) - _anchoredRectangle.Height);
		}
		else if (num * entryIndex < _scaledScrollOffset.Y)
		{
			SetScroll(0, num * entryIndex);
		}
	}

	protected override void PrepareForDrawSelf()
	{
		base.PrepareForDrawSelf();
		TextureArea whitePixel = Desktop.Provider.WhitePixel;
		UInt32Color color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 64);
		UInt32Color color2 = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 140);
		UInt32Color color3 = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 40);
		UInt32Color colorOverride = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 125);
		UInt32Color color4 = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 30);
		int num = Desktop.ScaleRound(5f);
		bool flag = SearchQuery.Trim() != "";
		int num2 = Desktop.ScaleRound(_rowHeight);
		int height = (int)MathHelper.Max(Desktop.ScaleRound(1f), 1f);
		int num3 = Desktop.ScaleRound(11f);
		int num4 = Desktop.ScaleRound(7f);
		int num5 = (num2 - num3) / 2;
		int num6 = Desktop.ScaleRound(20f);
		int num7 = Desktop.ScaleRound(43f);
		float num8 = 13f * Desktop.Scale;
		Desktop.Batcher2D.PushScissor(_rectangleAfterPadding);
		for (int i = 0; i < _entries.Length; i++)
		{
			AssetTreeEntry assetTreeEntry = _entries[i];
			int num9 = _rectangleAfterPadding.Y + num2 * i - _scaledScrollOffset.Y;
			int num10 = _rectangleAfterPadding.X + Desktop.ScaleRound(5 + 10 * (assetTreeEntry.Indention - 1));
			if (num9 > _rectangleAfterPadding.Bottom)
			{
				continue;
			}
			int num11 = num9 + num2;
			if (num11 < _rectangleAfterPadding.Top)
			{
				continue;
			}
			if (i > 0 && _entries[i - 1].Indention > assetTreeEntry.Indention)
			{
				Desktop.Batcher2D.RequestDrawTexture(whitePixel.Texture, whitePixel.Rectangle, new Rectangle(num10 + num6, num9, _rectangleAfterPadding.Width, height), color4);
			}
			if (i == _activeEntryIndex)
			{
				Desktop.Batcher2D.RequestDrawTexture(whitePixel.Texture, whitePixel.Rectangle, new Rectangle(_rectangleAfterPadding.X, num9, _rectangleAfterPadding.Width, num2), color2);
			}
			else if (i == _hoveredEntryIndex)
			{
				Desktop.Batcher2D.RequestDrawTexture(whitePixel.Texture, whitePixel.Rectangle, new Rectangle(_rectangleAfterPadding.X, num9, _rectangleAfterPadding.Width, num2), color);
			}
			if (i == _focusedEntryIndex)
			{
				Desktop.Batcher2D.RequestDrawOutline(whitePixel.Texture, whitePixel.Rectangle, new Rectangle(_rectangleAfterPadding.X - 10, num9, _rectangleAfterPadding.Width + 20, num2), 2f, color3);
			}
			Rectangle destRect = new Rectangle(num10 + num6, num9 + num, num2 - num * 2, num2 - num * 2);
			if (assetTreeEntry.Type == AssetTreeEntryType.Type)
			{
				Desktop.Batcher2D.RequestDrawPatch(_folderIconPatch, destRect, Desktop.Scale, colorOverride);
			}
			else if (assetTreeEntry.Type == AssetTreeEntryType.File)
			{
				if (_iconPatches.TryGetValue(assetTreeEntry.AssetType, out var value))
				{
					Desktop.Batcher2D.RequestDrawPatch(value, destRect, Desktop.Scale, value.Color);
				}
				else
				{
					Desktop.Batcher2D.RequestDrawPatch(_missingIconPatch, destRect, Desktop.Scale, colorOverride);
				}
			}
			else
			{
				Desktop.Batcher2D.RequestDrawPatch(_folderIconPatch, destRect, Desktop.Scale, colorOverride);
			}
			if (assetTreeEntry.Type != AssetTreeEntryType.File && !flag)
			{
				Rectangle destRect2 = new Rectangle(num10 + num4, num9 + num5, num3, num3);
				Desktop.Batcher2D.RequestDrawPatch(assetTreeEntry.IsCollapsed ? _uncollapseIconPatch : _collapseIconPatch, destRect2, Desktop.Scale, colorOverride);
			}
			Font font = ((assetTreeEntry.Type == AssetTreeEntryType.Type) ? _fontFamily.BoldFont : _fontFamily.RegularFont);
			float num12 = num8 / (float)font.BaseSize;
			float num13 = (float)num9 + (float)num2 / 2f;
			float y = num13 - (float)(int)((float)font.Height * num12 / 2f);
			Desktop.Batcher2D.RequestDrawText(font, num8, assetTreeEntry.Name, new Vector3(num10 + num7, y, 0f), (assetTreeEntry.Type == AssetTreeEntryType.File) ? UInt32Color.White : UInt32Color.FromRGBA(200, 200, 200, byte.MaxValue));
		}
		Desktop.Batcher2D.PopScissor();
	}
}
