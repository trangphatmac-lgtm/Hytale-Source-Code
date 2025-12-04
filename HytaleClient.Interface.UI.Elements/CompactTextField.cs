using System;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement]
public class CompactTextField : TextField
{
	private int _targetWidth = -1;

	[UIMarkupProperty]
	public int CollapsedWidth;

	[UIMarkupProperty]
	public int ExpandedWidth;

	[UIMarkupProperty]
	public SoundStyle ExpandSound;

	[UIMarkupProperty]
	public SoundStyle CollapseSound;

	public CompactTextField(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	protected override void Animate(float deltaTime)
	{
		base.Animate(deltaTime);
		if (_targetWidth != -1 && Anchor.Width != _targetWidth)
		{
			Anchor.Width = (int)MathHelper.Lerp(Anchor.Width.Value, _targetWidth, System.Math.Min(deltaTime * 16f, 1f));
			Layout();
		}
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		bool flag = _targetWidth == -1;
		_targetWidth = ((_isFocused || Value != "") ? ExpandedWidth : CollapsedWidth);
		if (flag)
		{
			Anchor.Width = _targetWidth;
		}
	}

	protected override void OnMouseEnter()
	{
		SDL.SDL_SetCursor(_isFocused ? Desktop.Cursors.IBeam : Desktop.Cursors.Hand);
		Layout();
	}

	protected override void OnMouseLeave()
	{
		base.OnMouseLeave();
		Layout();
	}

	protected internal override void OnFocus()
	{
		base.OnFocus();
		SDL.SDL_SetCursor(Desktop.Cursors.IBeam);
		if (Value == "" && ExpandSound != null)
		{
			Desktop.Provider.PlaySound(ExpandSound);
		}
		Layout();
	}

	protected internal override void OnBlur()
	{
		base.OnBlur();
		if (Value == "" && CollapseSound != null)
		{
			Desktop.Provider.PlaySound(CollapseSound);
		}
		Layout();
	}
}
