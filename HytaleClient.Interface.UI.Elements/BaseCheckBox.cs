using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.Interface.UI.Elements;

public abstract class BaseCheckBox<CheckBoxStyleType, CheckBoxStyleStateType> : InputElement<bool> where CheckBoxStyleType : BaseCheckBoxStyle<CheckBoxStyleStateType>, new() where CheckBoxStyleStateType : CheckBoxStyleState, new()
{
	[UIMarkupProperty]
	public bool Disabled;

	[UIMarkupProperty]
	public CheckBoxStyleType Style;

	private TexturePatch _checkmarkPatch;

	public CheckBoxStyleState StyleState => Value ? Style.Checked : Style.Unchecked;

	public BaseCheckBox(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		CheckBoxStyleStateType val = (Value ? Style.Checked : Style.Unchecked);
		if (Disabled)
		{
			_checkmarkPatch = Desktop.MakeTexturePatch(val.DisabledBackground ?? val.DefaultBackground);
		}
		else if (base.CapturedMouseButton == 1u)
		{
			_checkmarkPatch = Desktop.MakeTexturePatch(val.PressedBackground ?? val.HoveredBackground ?? val.DefaultBackground);
		}
		else if (base.IsHovered)
		{
			_checkmarkPatch = Desktop.MakeTexturePatch(val.HoveredBackground ?? val.DefaultBackground);
		}
		else
		{
			_checkmarkPatch = Desktop.MakeTexturePatch(val.DefaultBackground);
		}
	}

	public override Element HitTest(Point position)
	{
		return _anchoredRectangle.Contains(position) ? this : null;
	}

	protected override void OnMouseEnter()
	{
		if (!Disabled && StyleState.HoveredSound != null)
		{
			Desktop.Provider.PlaySound(StyleState.HoveredSound);
		}
		Layout();
		SDL.SDL_SetCursor(Desktop.Cursors.Hand);
	}

	protected override void OnMouseLeave()
	{
		Layout();
		SDL.SDL_SetCursor(Desktop.Cursors.Arrow);
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		if (!Disabled && (long)evt.Button == 1)
		{
			Layout();
		}
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		base.OnMouseButtonUp(evt, activate);
		if (!Disabled)
		{
			if (activate && (long)evt.Button == 1)
			{
				Value = !Value;
				Layout();
				ValueChanged?.Invoke();
			}
			if (StyleState.ChangedSound != null)
			{
				Desktop.Provider.PlaySound(StyleState.ChangedSound);
			}
		}
	}

	protected override void PrepareForDrawSelf()
	{
		base.PrepareForDrawSelf();
		Desktop.Batcher2D.RequestDrawPatch(_checkmarkPatch, _rectangleAfterPadding, Desktop.Scale);
	}
}
