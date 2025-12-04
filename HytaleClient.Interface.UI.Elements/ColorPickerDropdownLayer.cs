#define DEBUG
using System.Diagnostics;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.UI.Elements;

public class ColorPickerDropdownLayer : Element
{
	public readonly ColorPicker ColorPicker;

	private readonly ColorPickerDropdownBox _dropdown;

	public ColorPickerDropdownLayer(ColorPickerDropdownBox dropdown)
		: base(dropdown.Desktop, null)
	{
		_dropdown = dropdown;
		ColorPicker = new ColorPicker(Desktop, this);
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		ColorPickerDropdownBoxStyle style = _dropdown.Style;
		ColorPicker.Style = style.ColorPickerStyle;
		ColorPicker.Background = style.PanelBackground;
		ColorPicker.Anchor.Width = style.PanelWidth;
		ColorPicker.Anchor.Height = style.PanelHeight;
		ColorPicker.Padding = style.PanelPadding;
		int num = Desktop.UnscaleRound(_dropdown.AnchoredRectangle.X);
		int num2 = Desktop.UnscaleRound(_dropdown.AnchoredRectangle.Top);
		int num3 = Desktop.UnscaleRound(_dropdown.AnchoredRectangle.Width);
		int num4 = Desktop.UnscaleRound(_dropdown.AnchoredRectangle.Height);
		int num5 = Desktop.UnscaleRound(Desktop.ViewportRectangle.Height);
		int num6 = Desktop.UnscaleRound(Desktop.ViewportRectangle.Width);
		ColorPicker.Anchor.Top = num2 + num4 + style.PanelOffset;
		if (ColorPicker.Anchor.Top + style.PanelHeight > num5)
		{
			ColorPicker.Anchor.Top = num2 - style.PanelHeight - style.PanelOffset;
		}
		ColorPicker.Anchor.Left = num;
		if (ColorPicker.Anchor.Left + style.PanelWidth > num6)
		{
			ColorPicker.Anchor.Left = num + num3 - style.PanelWidth;
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

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		if ((long)evt.Button == 1 && !ColorPicker.AnchoredRectangle.Contains(Desktop.MousePosition))
		{
			_dropdown.CloseDropdown();
		}
	}

	protected internal override void Dismiss()
	{
		_dropdown.CloseDropdown();
	}
}
