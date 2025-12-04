using System;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.AssetEditor.Interface.Elements;

public class ResizerHandle : Element
{
	public Action MouseButtonReleased;

	public Action Resizing;

	private bool _isResizerHovered;

	public ResizerHandle(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	public override Element HitTest(Point position)
	{
		return _rectangleAfterPadding.Contains(position) ? this : null;
	}

	protected override void OnMouseEnter()
	{
		UpdateResizerState();
	}

	protected override void OnMouseLeave()
	{
		SDL.SDL_SetCursor(Desktop.Cursors.Arrow);
		_isResizerHovered = false;
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		MouseButtonReleased?.Invoke();
		Desktop.RefreshHover();
		UpdateResizerState();
	}

	protected override void OnMouseMove()
	{
		UpdateResizerState();
		if (base.CapturedMouseButton.HasValue)
		{
			Resizing?.Invoke();
		}
	}

	private void UpdateResizerState()
	{
		if (_isResizerHovered)
		{
			if (!_rectangleAfterPadding.Contains(Desktop.MousePosition) && !base.CapturedMouseButton.HasValue)
			{
				_isResizerHovered = false;
				SDL.SDL_SetCursor(Desktop.Cursors.Arrow);
			}
		}
		else if (_rectangleAfterPadding.Contains(Desktop.MousePosition) || base.CapturedMouseButton.HasValue)
		{
			_isResizerHovered = true;
			SDL.SDL_SetCursor(Desktop.Cursors.SizeWE);
		}
	}
}
