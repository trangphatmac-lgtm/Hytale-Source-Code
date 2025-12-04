using System;
using System.Collections.Generic;
using System.IO;
using HytaleClient.AssetEditor.Data;
using HytaleClient.AssetEditor.Utils;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.AssetEditor.Interface.Editor;

internal class EditorTabButton : Button
{
	public AssetReference AssetReference;

	private string _path;

	private string _name;

	private readonly AssetEditorOverlay _assetEditorOverlay;

	private Button _closeButton;

	private Label _nameLabel;

	private Label _pathLabel;

	private Group _activeHighlight;

	public bool IsActive { get; private set; }

	public DateTime TimeLastActive { get; private set; }

	public EditorTabButton(AssetEditorOverlay assetEditorOverlay, AssetReference assetReference)
		: base(assetEditorOverlay.Desktop, null)
	{
		AssetReference = assetReference;
		_assetEditorOverlay = assetEditorOverlay;
		_path = ((_assetEditorOverlay.AssetTypeRegistry.AssetTypes[assetReference.Type].AssetTree == AssetTreeFolder.Cosmetics) ? AssetPathUtils.GetPathWithoutAssetId(AssetReference.FilePath) : Path.GetDirectoryName(AssetReference.FilePath));
		_name = _assetEditorOverlay.GetAssetIdFromReference(AssetReference);
	}

	public void Build()
	{
		Clear();
		AssetTypeConfig assetTypeConfig = _assetEditorOverlay.AssetTypeRegistry.AssetTypes[AssetReference.Type];
		base.TooltipText = assetTypeConfig.Name;
		Desktop.Provider.TryGetDocument("AssetEditor/TabButton.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		Group group = uIFragment.Get<Group>("Icon");
		group.Background = assetTypeConfig.Icon.Clone();
		group.Background.Color = (assetTypeConfig.IsColoredIcon ? UInt32Color.White : UInt32Color.FromRGBA(160, 160, 160, byte.MaxValue));
		_pathLabel = uIFragment.Get<Label>("Directory");
		_pathLabel.Text = _path;
		_nameLabel = uIFragment.Get<Label>("Name");
		_nameLabel.Text = _name;
		_closeButton = uIFragment.Get<Button>("CloseButton");
		_closeButton.Activating = delegate
		{
			_assetEditorOverlay.CloseTab(AssetReference);
		};
		_activeHighlight = uIFragment.Get<Group>("ActiveTabHighlight");
	}

	protected override void OnMouseEnter()
	{
		base.OnMouseEnter();
		_closeButton.Disabled = false;
		_closeButton.Layout();
	}

	protected override void OnMouseLeave()
	{
		base.OnMouseLeave();
		_closeButton.Disabled = true;
		_closeButton.Layout();
	}

	public override Point ComputeScaledMinSize(int? maxWidth, int? maxHeight)
	{
		FontFamily fontFamily = Desktop.Provider.GetFontFamily(_nameLabel.Style.FontName.Value);
		Font font = (_nameLabel.Style.RenderBold ? fontFamily.BoldFont : fontFamily.RegularFont);
		float num = font.CalculateTextWidth(_name) * _nameLabel.Style.FontSize / (float)font.BaseSize;
		_pathLabel.Anchor.Width = System.Math.Max((int)num, 150);
		return base.ComputeScaledMinSize(maxWidth, maxHeight);
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		FontFamily fontFamily = Desktop.Provider.GetFontFamily(_pathLabel.Style.FontName.Value);
		Font font = (_nameLabel.Style.RenderBold ? fontFamily.BoldFont : fontFamily.RegularFont);
		string text = "";
		float num = (float)_pathLabel.Anchor.Width.Value - font.GetCharacterAdvance(8230) * _pathLabel.Style.FontSize / (float)font.BaseSize;
		for (int num2 = _path.Length - 1; num2 >= 0; num2--)
		{
			float num3 = font.GetCharacterAdvance(_path[num2]) * _pathLabel.Style.FontSize / (float)font.BaseSize;
			num -= num3;
			if (num <= 0f)
			{
				break;
			}
			text = _path[num2] + text;
			num -= _pathLabel.Style.LetterSpacing;
		}
		_pathLabel.Text = "…" + text;
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		base.OnMouseButtonDown(evt);
		if (!Disabled)
		{
			switch ((uint)evt.Button)
			{
			case 3u:
				OpenContextPopup();
				break;
			case 2u:
				_assetEditorOverlay.CloseTab(AssetReference.None);
				break;
			}
		}
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		base.OnMouseButtonUp(evt, activate);
		if (!Disabled && activate && (long)evt.Button == 1)
		{
			_assetEditorOverlay.OpenExistingAsset(AssetReference, evt.Clicks == 2);
		}
	}

	public void SetActive(bool active)
	{
		if (active)
		{
			TimeLastActive = DateTime.Now;
		}
		if (active != IsActive)
		{
			IsActive = active;
			Background = (IsActive ? new PatchStyle(UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 150)) : null);
			_nameLabel.Style.TextColor = UInt32Color.FromHexString(IsActive ? "#111111" : "#575656");
			_activeHighlight.Visible = IsActive;
			if (base.IsMounted)
			{
				Layout();
			}
		}
	}

	private void OpenContextPopup()
	{
		PopupMenuLayer popup = _assetEditorOverlay.Popup;
		List<PopupMenuItem> items = new List<PopupMenuItem>
		{
			new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.tabs.popup.close"), delegate
			{
				_assetEditorOverlay.CloseTab(AssetReference);
			}),
			new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.tabs.popup.closeAll"), delegate
			{
				_assetEditorOverlay.CloseAllTabs();
			}),
			new PopupMenuItem(Desktop.Provider.GetText("ui.assetEditor.tabs.popup.copyId"), delegate
			{
				SDL.SDL_SetClipboardText(_name);
			})
		};
		_assetEditorOverlay.SetupAssetPopup(AssetReference, items);
		popup.SetTitle(null);
		popup.SetItems(items);
		popup.Open();
	}

	public void OnAssetRenamed(AssetReference assetReference)
	{
		AssetReference = assetReference;
		_path = ((_assetEditorOverlay.AssetTypeRegistry.AssetTypes[assetReference.Type].AssetTree == AssetTreeFolder.Cosmetics) ? AssetPathUtils.GetPathWithoutAssetId(AssetReference.FilePath) : Path.GetDirectoryName(AssetReference.FilePath));
		_name = _assetEditorOverlay.GetAssetIdFromReference(AssetReference);
		_pathLabel.Text = _path;
		_nameLabel.Text = _name;
	}
}
