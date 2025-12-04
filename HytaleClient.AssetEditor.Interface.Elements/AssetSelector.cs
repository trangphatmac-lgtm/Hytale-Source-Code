#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.AssetEditor.Interface.Elements;

internal class AssetSelector : Element
{
	private const int Height = 400;

	private readonly TextField _searchInput;

	private readonly AssetEditorOverlay _assetEditorOverlay;

	private readonly AssetSelectorDropdown _dropdown;

	private readonly Group _container;

	public AssetSelector(Desktop desktop, AssetEditorOverlay assetEditorOverlay, AssetSelectorDropdown dropdown)
		: base(desktop, null)
	{
		_assetEditorOverlay = assetEditorOverlay;
		_dropdown = dropdown;
		_container = new Group(Desktop, this)
		{
			Anchor = new Anchor
			{
				Height = 400
			},
			Padding = new Padding(0),
			LayoutMode = LayoutMode.Top,
			Background = new PatchStyle(UInt32Color.FromRGBA(20, 20, 20, 230))
		};
		Group parent = new Group(Desktop, _container);
		_searchInput = new TextField(Desktop, parent)
		{
			Background = new PatchStyle(UInt32Color.Black),
			PlaceholderText = Desktop.Provider.GetText("ui.assetEditor.assetSelector.findAsset"),
			Anchor = new Anchor
			{
				Height = 30
			},
			Decoration = new InputFieldDecorationStyle
			{
				Default = new InputFieldDecorationStyleState
				{
					Icon = new InputFieldIcon
					{
						Texture = new PatchStyle("AssetEditor/SearchIcon.png")
						{
							Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 63)
						},
						Width = 16,
						Height = 16,
						Offset = 5
					}
				}
			},
			Padding = new Padding
			{
				Left = 27
			},
			PlaceholderStyle = new InputFieldStyle
			{
				TextColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 102),
				RenderItalics = true,
				FontSize = 14f
			},
			Style = new InputFieldStyle
			{
				FontSize = 14f
			},
			ValueChanged = delegate
			{
				AssetTree dropdownAssetTree2 = _assetEditorOverlay.DropdownAssetTree;
				dropdownAssetTree2.SearchQuery = _searchInput.Value;
				dropdownAssetTree2.BuildTree();
				dropdownAssetTree2.Layout();
			},
			KeyDown = delegate(SDL_Keycode key)
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0007: Invalid comparison between Unknown and I4
				//IL_0009: Unknown result type (might be due to invalid IL or missing references)
				//IL_000f: Invalid comparison between Unknown and I4
				//IL_0038: Unknown result type (might be due to invalid IL or missing references)
				if ((int)key == 1073741905 || (int)key == 1073741906)
				{
					AssetTree dropdownAssetTree = _assetEditorOverlay.DropdownAssetTree;
					Desktop.FocusElement(dropdownAssetTree);
					dropdownAssetTree.OnKeyDown(key, 0);
				}
			}
		};
	}

	private void SetupSearchFilter(string filter)
	{
		_searchInput.Value = filter + ": ";
		_searchInput.ValueChanged();
		Desktop.FocusElement(_searchInput);
	}

	protected override void OnMounted()
	{
		_searchInput.Value = "";
		AssetTree dropdownAssetTree = _assetEditorOverlay.DropdownAssetTree;
		dropdownAssetTree.FocusSearch = FocusSearch;
		dropdownAssetTree.PopupMenuEnabled = false;
		dropdownAssetTree.ShowVirtualAssets = true;
		dropdownAssetTree.AssetTypesToDisplay = new HashSet<string> { _dropdown.AssetType };
		dropdownAssetTree.SelectingDirectoryFilter = SetupSearchFilter;
		dropdownAssetTree.FileEntryActivating = delegate(AssetTree.AssetTreeEntry entry)
		{
			if (!(entry.AssetType != _dropdown.AssetType))
			{
				_dropdown.CloseDropdown(entry.Name);
			}
		};
		dropdownAssetTree.SearchQuery = "";
		if (_assetEditorOverlay.AssetTypeRegistry.AssetTypes.TryGetValue(_dropdown.AssetType, out var value))
		{
			dropdownAssetTree.DirectoriesToDisplay = new List<string> { value.Path };
			dropdownAssetTree.AssetTypesToDisplay = new HashSet<string> { value.Id };
			UpdateSelectedItem();
			dropdownAssetTree.SetUncollapsedState(value.Path, uncollapsed: true);
			List<AssetFile> assets = _assetEditorOverlay.Assets.GetAssets(value.AssetTree);
			string[] array = value.Path.Split(new char[1] { '/' });
			string rootPath = string.Join("/", array, 0, array.Length - 1);
			dropdownAssetTree.UpdateFiles(assets, rootPath);
			Desktop.FocusElement(_searchInput);
			_container.Add(dropdownAssetTree);
		}
	}

	protected override void OnUnmounted()
	{
		_container.Remove(_assetEditorOverlay.DropdownAssetTree);
	}

	public void UpdateSelectedItem()
	{
		if (_dropdown.Value != null)
		{
			if (_assetEditorOverlay.Assets.TryGetPathForAssetId(_dropdown.AssetType, _dropdown.Value, out var filePath))
			{
				_assetEditorOverlay.DropdownAssetTree.SelectEntry(new AssetReference(_dropdown.AssetType, filePath));
			}
		}
		else
		{
			_assetEditorOverlay.DropdownAssetTree.DeselectEntry();
		}
	}

	public override Element HitTest(Point position)
	{
		Debug.Assert(base.IsMounted);
		if (!_anchoredRectangle.Contains(position))
		{
			return null;
		}
		return base.HitTest(position) ?? this;
	}

	protected override void ApplyStyles()
	{
		int num = Desktop.UnscaleRound(_dropdown.AnchoredRectangle.X);
		int num2 = Desktop.UnscaleRound(_dropdown.AnchoredRectangle.Top);
		int num3 = Desktop.UnscaleRound(_dropdown.AnchoredRectangle.Width);
		int num4 = Desktop.UnscaleRound(_dropdown.AnchoredRectangle.Height);
		int num5 = Desktop.UnscaleRound(Desktop.ViewportRectangle.Height);
		int num6 = Desktop.UnscaleRound(Desktop.ViewportRectangle.Width);
		_container.Anchor.Width = num3;
		_container.Anchor.Top = num2 + num4;
		if (_container.Anchor.Top + _container.Anchor.Height > num5)
		{
			_container.Anchor.Height = System.Math.Min(num2, 400);
			_container.Anchor.Top = num2 - _container.Anchor.Height;
		}
		else
		{
			_container.Anchor.Height = System.Math.Min(num5 - _container.Anchor.Top.Value, 400);
		}
		_container.Anchor.Left = num;
		if (_container.Anchor.Left + _container.Anchor.Width > num6)
		{
			_container.Anchor.Left = num + num3 - _container.Anchor.Width;
		}
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		if (activate && !_container.AnchoredRectangle.Contains(Desktop.MousePosition))
		{
			if ((long)evt.Button == 1)
			{
				_dropdown.CloseDropdown(null);
			}
			else
			{
				_dropdown.CloseDropdown(null);
			}
		}
	}

	protected internal override void OnKeyDown(SDL_Keycode keyCode, int repeat)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		if (Desktop.IsShortcutKeyDown && (int)keyCode == 102)
		{
			FocusSearch();
		}
	}

	private void FocusSearch()
	{
		Desktop.FocusElement(_searchInput);
		_searchInput.SelectAll();
	}

	protected internal override void Dismiss()
	{
		_dropdown.CloseDropdown(null);
	}
}
