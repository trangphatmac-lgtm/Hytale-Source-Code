using System;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement]
public class ColorPickerDropdownBox : Element
{
	[UIMarkupProperty]
	public ColorPickerDropdownBoxStyle Style;

	public Action RightClicking;

	private readonly ColorPickerDropdownLayer _dropdownLayer;

	private readonly Element _arrow;

	private TexturePatch _overlayPatch;

	[UIMarkupProperty]
	public UInt32Color Color
	{
		get
		{
			return _dropdownLayer.ColorPicker.Value;
		}
		set
		{
			_dropdownLayer.ColorPicker.Value = value;
		}
	}

	[UIMarkupProperty]
	public ColorPicker.ColorFormat Format
	{
		get
		{
			return _dropdownLayer.ColorPicker.Format;
		}
		set
		{
			_dropdownLayer.ColorPicker.Format = value;
		}
	}

	[UIMarkupProperty]
	public bool ResetTransparencyWhenChangingColor
	{
		get
		{
			return _dropdownLayer.ColorPicker.ResetTransparencyWhenChangingColor;
		}
		set
		{
			_dropdownLayer.ColorPicker.ResetTransparencyWhenChangingColor = value;
		}
	}

	[UIMarkupProperty]
	public bool DisplayTextField
	{
		get
		{
			return _dropdownLayer.ColorPicker.DisplayTextField;
		}
		set
		{
			_dropdownLayer.ColorPicker.DisplayTextField = value;
		}
	}

	public Action ValueChanged
	{
		set
		{
			_dropdownLayer.ColorPicker.ValueChanged = value;
		}
	}

	public ColorPickerDropdownBox(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		_dropdownLayer = new ColorPickerDropdownLayer(this);
		_arrow = new Element(desktop, this);
	}

	protected override void OnUnmounted()
	{
		if (_dropdownLayer.IsMounted)
		{
			CloseDropdown();
		}
	}

	protected override void ApplyStyles()
	{
		PatchStyle patchStyle;
		if (base.CapturedMouseButton.HasValue)
		{
			Background = ((Style.Background != null) ? (Style.Background.Pressed ?? Style.Background.Hovered ?? Style.Background.Default) : null);
			_arrow.Background = ((Style.ArrowBackground != null) ? (Style.ArrowBackground.Pressed ?? Style.ArrowBackground.Hovered ?? Style.ArrowBackground.Default) : null);
			patchStyle = ((Style.Overlay != null) ? (Style.Overlay.Pressed ?? Style.Overlay.Hovered ?? Style.Overlay.Default) : null);
		}
		else if (base.IsHovered)
		{
			Background = ((Style.Background != null) ? (Style.Background.Hovered ?? Style.Background.Default) : null);
			_arrow.Background = ((Style.Background != null) ? (Style.ArrowBackground.Hovered ?? Style.ArrowBackground.Default) : null);
			patchStyle = ((Style.Overlay != null) ? (Style.Overlay.Hovered ?? Style.Overlay.Default) : null);
		}
		else
		{
			Background = Style.Background?.Default;
			_arrow.Background = Style.ArrowBackground?.Default;
			patchStyle = Style.Overlay?.Default;
		}
		base.ApplyStyles();
		_arrow.Anchor = Style.ArrowAnchor;
		_overlayPatch = ((patchStyle != null) ? Desktop.MakeTexturePatch(patchStyle) : null);
	}

	protected override void LayoutSelf()
	{
		if (_dropdownLayer.IsMounted)
		{
			_dropdownLayer.Layout();
		}
	}

	public override Element HitTest(Point position)
	{
		return _anchoredRectangle.Contains(position) ? this : null;
	}

	protected override void OnMouseEnter()
	{
		if (Style.Sounds?.MouseHover != null)
		{
			Desktop.Provider.PlaySound(Style.Sounds.MouseHover);
		}
		Layout();
	}

	protected override void OnMouseLeave()
	{
		Layout();
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		if ((long)evt.Button == 1)
		{
			if (Style.Sounds?.Activate != null)
			{
				Desktop.Provider.PlaySound(Style.Sounds.Activate);
			}
			Layout();
			Open();
		}
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		if (activate && (long)evt.Button == 3)
		{
			RightClicking?.Invoke();
		}
	}

	internal void CloseDropdown()
	{
		Desktop.SetTransientLayer(null);
		if (base.IsMounted)
		{
			Layout();
		}
	}

	public void Open()
	{
		Desktop.SetTransientLayer(_dropdownLayer);
	}

	protected override void PrepareForDrawSelf()
	{
		if (_backgroundPatch != null)
		{
			Desktop.Batcher2D.RequestDrawPatch(_backgroundPatch, _backgroundRectangle, Desktop.Scale);
		}
		Desktop.Batcher2D.RequestDrawTexture(Desktop.Provider.WhitePixel.Texture, Desktop.Provider.WhitePixel.Rectangle, _rectangleAfterPadding, Color);
		if (_overlayPatch != null)
		{
			Desktop.Batcher2D.RequestDrawPatch(_overlayPatch, _rectangleAfterPadding, Desktop.Scale);
		}
		if (OutlineSize > 0f)
		{
			TextureArea whitePixel = Desktop.Provider.WhitePixel;
			Desktop.Batcher2D.RequestDrawOutline(whitePixel.Texture, whitePixel.Rectangle, _anchoredRectangle, OutlineSize * Desktop.Scale, OutlineColor);
		}
	}
}
