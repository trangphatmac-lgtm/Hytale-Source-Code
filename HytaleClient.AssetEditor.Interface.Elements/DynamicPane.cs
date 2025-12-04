#define DEBUG
using System;
using System.Diagnostics;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.AssetEditor.Interface.Elements;

[UIMarkupElement(AcceptsChildren = true)]
public class DynamicPane : Group
{
	public enum ResizeType
	{
		None,
		Start,
		End
	}

	private enum LayoutDirection
	{
		Invalid,
		Vertical,
		Horizontal
	}

	public Action MouseButtonReleased;

	[UIMarkupProperty]
	public int MinSize = 50;

	[UIMarkupProperty]
	public ResizeType ResizeAt = ResizeType.None;

	[UIMarkupProperty]
	public int ResizerSize = 1;

	[UIMarkupProperty]
	public PatchStyle ResizerBackground;

	private TexturePatch _resizerPatch;

	private Rectangle _resizerRectangle;

	private bool _isResizerHovered;

	public DynamicPane(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	protected override void OnMounted()
	{
		Debug.Assert(Parent is DynamicPaneContainer);
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		_resizerPatch = ((ResizerBackground != null && ResizeAt != 0) ? Desktop.MakeTexturePatch(ResizerBackground) : null);
	}

	private LayoutDirection GetParentLayoutDirection()
	{
		switch (Parent.LayoutMode)
		{
		case LayoutMode.Top:
		case LayoutMode.Bottom:
			return LayoutDirection.Vertical;
		case LayoutMode.Left:
		case LayoutMode.Right:
			return LayoutDirection.Horizontal;
		default:
			return LayoutDirection.Invalid;
		}
	}

	protected override void LayoutSelf()
	{
		LayoutDirection parentLayoutDirection = GetParentLayoutDirection();
		if (parentLayoutDirection == LayoutDirection.Invalid)
		{
			_resizerRectangle = Rectangle.Empty;
			return;
		}
		int num = Desktop.ScaleRound(ResizerSize);
		switch (ResizeAt)
		{
		case ResizeType.End:
			if (parentLayoutDirection == LayoutDirection.Vertical)
			{
				_resizerRectangle = new Rectangle(_anchoredRectangle.X, _anchoredRectangle.Bottom - num, _anchoredRectangle.Width, num);
				_rectangleAfterPadding.Height -= num;
				_backgroundRectangle.Height -= num;
			}
			else
			{
				_resizerRectangle = new Rectangle(_anchoredRectangle.Right - num, _anchoredRectangle.Y, num, _anchoredRectangle.Height);
				_rectangleAfterPadding.Width -= num;
				_backgroundRectangle.Width -= num;
			}
			break;
		case ResizeType.Start:
			if (parentLayoutDirection == LayoutDirection.Vertical)
			{
				_resizerRectangle = new Rectangle(_anchoredRectangle.X, _anchoredRectangle.Y, _anchoredRectangle.Width, num);
				_rectangleAfterPadding.Y += num;
				_rectangleAfterPadding.Height -= num;
				_backgroundRectangle.Y += num;
				_backgroundRectangle.Height -= num;
			}
			else
			{
				_resizerRectangle = new Rectangle(_anchoredRectangle.X, _anchoredRectangle.Y, num, _anchoredRectangle.Height);
				_rectangleAfterPadding.X += num;
				_rectangleAfterPadding.Width -= num;
				_backgroundRectangle.X += num;
				_backgroundRectangle.Width -= num;
			}
			break;
		default:
			_resizerRectangle = Rectangle.Empty;
			break;
		}
	}

	public override Element HitTest(Point position)
	{
		return (ResizeAt != 0 && _resizerRectangle.Contains(position)) ? this : base.HitTest(position);
	}

	public override void OnMouseOut()
	{
		SDL.SDL_SetCursor(Desktop.Cursors.Arrow);
		_isResizerHovered = false;
	}

	public override void OnMouseIn()
	{
		UpdateResizerState();
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
		if (!base.CapturedMouseButton.HasValue)
		{
			return;
		}
		switch (GetParentLayoutDirection())
		{
		case LayoutDirection.Invalid:
			return;
		case LayoutDirection.Vertical:
		{
			int maxHeight = GetMaxHeight();
			int num2 = ((ResizeAt == ResizeType.End) ? Desktop.UnscaleRound(Desktop.MousePosition.Y - base.AnchoredRectangle.Y) : Desktop.UnscaleRound(base.AnchoredRectangle.Bottom - Desktop.MousePosition.Y));
			if (num2 < MinSize)
			{
				num2 = MinSize;
			}
			else if (num2 > maxHeight)
			{
				num2 = maxHeight;
			}
			Anchor.Height = num2;
			break;
		}
		default:
		{
			int maxWidth = GetMaxWidth();
			int num = ((ResizeAt == ResizeType.End) ? Desktop.UnscaleRound(Desktop.MousePosition.X - base.AnchoredRectangle.X) : Desktop.UnscaleRound(base.AnchoredRectangle.Right - Desktop.MousePosition.X));
			if (num < MinSize)
			{
				num = MinSize;
			}
			else if (num > maxWidth)
			{
				num = maxWidth;
			}
			Anchor.Width = num;
			break;
		}
		}
		Parent.Layout();
	}

	private int GetMaxWidth()
	{
		int num = Desktop.UnscaleRound(Parent.AnchoredRectangle.Width);
		foreach (Element child in Parent.Children)
		{
			if (child == this || !child.IsMounted)
			{
				continue;
			}
			if (child is DynamicPane dynamicPane)
			{
				if (child.FlexWeight > 0)
				{
					num -= dynamicPane.MinSize;
				}
				else if (child.Anchor.Width.HasValue)
				{
					num -= child.Anchor.Width.Value;
				}
			}
			else if (child.Anchor.Width.HasValue)
			{
				num -= child.Anchor.Width.Value;
			}
			else if (child.FlexWeight > 0)
			{
				num -= 50;
			}
		}
		return num;
	}

	private int GetMaxHeight()
	{
		int num = Desktop.UnscaleRound(Parent.AnchoredRectangle.Height);
		foreach (Element child in Parent.Children)
		{
			if (child == this || !child.IsMounted)
			{
				continue;
			}
			if (child is DynamicPane dynamicPane)
			{
				if (child.FlexWeight > 0)
				{
					num -= dynamicPane.MinSize;
				}
				else if (child.Anchor.Height.HasValue)
				{
					num -= child.Anchor.Height.Value;
				}
			}
			else if (child.Anchor.Height.HasValue)
			{
				num -= child.Anchor.Height.Value;
			}
			else if (child.FlexWeight > 0)
			{
				num -= 50;
			}
		}
		return num;
	}

	private void UpdateResizerState()
	{
		if (_isResizerHovered)
		{
			if (!_resizerRectangle.Contains(Desktop.MousePosition) && !base.CapturedMouseButton.HasValue)
			{
				_isResizerHovered = false;
				SDL.SDL_SetCursor(Desktop.Cursors.Arrow);
			}
		}
		else if (_resizerRectangle.Contains(Desktop.MousePosition) || base.CapturedMouseButton.HasValue)
		{
			_isResizerHovered = true;
			SDL.SDL_SetCursor((GetParentLayoutDirection() == LayoutDirection.Vertical) ? Desktop.Cursors.SizeNS : Desktop.Cursors.SizeWE);
		}
	}

	protected override void PrepareForDrawSelf()
	{
		base.PrepareForDrawSelf();
		if (ResizerBackground != null)
		{
			Desktop.Batcher2D.RequestDrawPatch(_resizerPatch, _resizerRectangle, Desktop.Scale);
		}
	}
}
