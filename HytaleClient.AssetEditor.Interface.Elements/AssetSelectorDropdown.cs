using System;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.AssetEditor.Interface.Elements;

internal class AssetSelectorDropdown : Element
{
	private readonly AssetSelector _assetSelector;

	private readonly Label _label;

	private readonly Group _arrow;

	public FileDropdownBoxStyle Style;

	private string _assetType;

	public Action ValueChanged;

	private string _value;

	public string AssetType
	{
		get
		{
			return _assetType;
		}
		set
		{
			_assetType = value;
			if (_assetSelector.IsMounted)
			{
				_assetSelector.UpdateSelectedItem();
			}
		}
	}

	public string Value
	{
		get
		{
			return _value;
		}
		set
		{
			_value = value;
			_label.Text = value;
			if (_assetSelector.IsMounted)
			{
				_assetSelector.UpdateSelectedItem();
			}
		}
	}

	public AssetSelectorDropdown(Desktop desktop, Element parent, AssetEditorOverlay overlay)
		: base(desktop, parent)
	{
		_layoutMode = LayoutMode.Left;
		_assetSelector = new AssetSelector(desktop, overlay, this);
		_label = new Label(Desktop, this)
		{
			Style = new LabelStyle
			{
				VerticalAlignment = LabelStyle.LabelAlignment.Center
			},
			FlexWeight = 1
		};
		_arrow = new Group(Desktop, this);
	}

	public override Element HitTest(Point position)
	{
		return _anchoredRectangle.Contains(position) ? this : null;
	}

	protected override void OnUnmounted()
	{
		if (_assetSelector.IsMounted)
		{
			CloseDropdown(null);
		}
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		if (activate && (long)evt.Button == 1)
		{
			Desktop.SetTransientLayer(_assetSelector);
		}
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		Layout();
	}

	protected override void OnMouseEnter()
	{
		Layout();
	}

	protected override void OnMouseLeave()
	{
		Layout();
	}

	protected internal void CloseDropdown(string newSelectedValue)
	{
		Desktop.SetTransientLayer(null);
		if (newSelectedValue != null)
		{
			Value = newSelectedValue;
			ValueChanged?.Invoke();
		}
		if (base.IsMounted)
		{
			Layout();
		}
	}

	protected override void ApplyStyles()
	{
		if (_assetSelector.IsMounted)
		{
			Background = Style.PressedBackground ?? Style.HoveredBackground ?? Style.DefaultBackground;
			_arrow.Background = new PatchStyle
			{
				TexturePath = (Style.PressedArrowTexturePath ?? Style.HoveredArrowTexturePath ?? Style.DefaultArrowTexturePath)
			};
		}
		else if (base.IsHovered)
		{
			Background = Style.HoveredBackground ?? Style.DefaultBackground;
			_arrow.Background = new PatchStyle
			{
				TexturePath = (Style.HoveredArrowTexturePath ?? Style.DefaultArrowTexturePath)
			};
		}
		else
		{
			Background = Style.DefaultBackground;
			_arrow.Background = new PatchStyle
			{
				TexturePath = Style.DefaultArrowTexturePath
			};
		}
		base.ApplyStyles();
		_arrow.Anchor.Width = Style.ArrowWidth;
		_arrow.Anchor.Height = Style.ArrowHeight;
		_arrow.Anchor.Right = Style.HorizontalPadding;
		_label.Style = Style.LabelStyle ?? new LabelStyle();
		_label.Anchor.Left = (_label.Anchor.Right = Style.HorizontalPadding);
	}

	protected override void LayoutSelf()
	{
		if (_assetSelector.IsMounted)
		{
			_assetSelector.Layout();
		}
	}

	public void Open()
	{
		Desktop.SetTransientLayer(_assetSelector);
	}
}
