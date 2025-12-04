#define DEBUG
using System;
using System.Diagnostics;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.Interface.UI.Elements;

public abstract class BaseButton<ButtonStyleType, ButtonStyleStateType> : Element where ButtonStyleType : BaseButtonStyle<ButtonStyleStateType>, new() where ButtonStyleStateType : class, new()
{
	[UIMarkupProperty]
	public bool Disabled;

	[UIMarkupProperty]
	public ButtonStyleType Style = new ButtonStyleType();

	protected ButtonStyleStateType _styleState;

	protected TexturePatch _stateBackgroundPatch;

	protected bool _isFocused;

	public Action Activating;

	public Action DoubleClicking;

	public Action RightClicking;

	public Action MouseEntered;

	public Action MouseExited;

	public BaseButton(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	protected override void OnUnmounted()
	{
		if (_isFocused)
		{
			Desktop.FocusElement(null);
		}
	}

	public override Point ComputeScaledMinSize(int? maxWidth, int? maxHeight)
	{
		ApplyStyles();
		return base.ComputeScaledMinSize(maxWidth, maxHeight);
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		if (Disabled && Style.Disabled != null)
		{
			_styleState = Style.Disabled;
		}
		else if (!Disabled && base.CapturedMouseButton == 1u && Style.Pressed != null)
		{
			_styleState = Style.Pressed;
		}
		else if (!Disabled && base.IsHovered && Style.Hovered != null)
		{
			_styleState = Style.Hovered;
		}
		else
		{
			_styleState = Style.Default;
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

	protected override void OnMouseEnter()
	{
		if (!Disabled)
		{
			Layout();
		}
		MouseEntered?.Invoke();
		if (!Disabled)
		{
			SDL.SDL_SetCursor(Desktop.Cursors.Hand);
			if (Style.Sounds?.MouseHover != null)
			{
				Desktop.Provider.PlaySound(Style.Sounds.MouseHover);
			}
		}
	}

	protected override void OnMouseLeave()
	{
		if (!Disabled)
		{
			Layout();
		}
		MouseExited?.Invoke();
		SDL.SDL_SetCursor(Desktop.Cursors.Arrow);
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		if (!Disabled && (long)evt.Button == 1)
		{
			Desktop.FocusElement(this);
			Layout();
		}
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		activate = activate && !Disabled;
		if (!Disabled)
		{
			Layout();
		}
		if (!activate)
		{
			return;
		}
		switch ((uint)evt.Button)
		{
		case 1u:
			if (Style.Sounds?.Activate != null)
			{
				Desktop.Provider.PlaySound(Style.Sounds?.Activate);
			}
			if (DoubleClicking != null && evt.Clicks == 2 && _isFocused)
			{
				DoubleClicking();
			}
			else
			{
				Activating?.Invoke();
			}
			break;
		case 3u:
			if (Style.Sounds?.Context != null)
			{
				Desktop.Provider.PlaySound(Style.Sounds?.Context);
			}
			RightClicking?.Invoke();
			break;
		}
	}

	protected internal override void OnBlur()
	{
		_isFocused = false;
	}

	protected internal override void OnFocus()
	{
		_isFocused = true;
	}

	protected override void PrepareForDrawSelf()
	{
		base.PrepareForDrawSelf();
		if (_stateBackgroundPatch != null)
		{
			Desktop.Batcher2D.RequestDrawPatch(_stateBackgroundPatch, _anchoredRectangle, Desktop.Scale);
		}
	}
}
