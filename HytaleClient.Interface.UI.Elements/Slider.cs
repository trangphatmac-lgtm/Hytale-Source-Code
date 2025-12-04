using System;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupElement]
public class Slider : InputElement<int>
{
	[UIMarkupProperty]
	public int Min;

	[UIMarkupProperty]
	public int Max;

	[UIMarkupProperty]
	public int Step = 1;

	private int _value;

	[UIMarkupProperty]
	public SliderStyle Style = SliderStyle.MakeDefault();

	public Action MouseButtonReleased;

	private readonly Group _fill;

	private readonly Group _handle;

	private Rectangle _hitboxRectangle;

	private bool _isDragging;

	private TexturePatch _paddedBackgroundPatch;

	public override int Value
	{
		get
		{
			return MathHelper.Clamp(_value, Min, Max);
		}
		set
		{
			_value = value;
		}
	}

	public Slider(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		_fill = new Group(Desktop, this)
		{
			Anchor = new Anchor
			{
				Left = 0
			}
		};
		_handle = new Group(Desktop, this)
		{
			Anchor = new Anchor
			{
				Left = 0
			}
		};
	}

	protected override void ApplyStyles()
	{
		_paddedBackgroundPatch = ((Style.Background != null) ? Desktop.MakeTexturePatch(Style.Background) : null);
		_fill.Background = Style.Fill;
		_handle.Anchor.Width = Style.HandleWidth;
		_handle.Anchor.Height = Style.HandleHeight;
		_handle.Background = Style.Handle;
	}

	protected override void LayoutSelf()
	{
		int num = Desktop.UnscaleRound(_rectangleAfterPadding.Width);
		int num2 = (int)System.Math.Round((float)(Value - Min) * (float)num / (float)(Max - Min), MidpointRounding.AwayFromZero);
		_fill.Anchor.Width = num2;
		_handle.Anchor.Left = (int)((float)num2 - (float)Style.HandleWidth / 2f);
	}

	protected override void AfterChildrenLayout()
	{
		UpdateHitboxRectangle();
	}

	private void UpdateHitboxRectangle()
	{
		int num = _anchoredRectangle.Width;
		if (_anchoredRectangle.X > _handle.AnchoredRectangle.X)
		{
			num += _anchoredRectangle.X - _handle.AnchoredRectangle.X;
		}
		else if (_handle.AnchoredRectangle.Right > _anchoredRectangle.Right)
		{
			num += _handle.AnchoredRectangle.Right - _anchoredRectangle.Right;
		}
		_hitboxRectangle = new Rectangle(System.Math.Min(_anchoredRectangle.Left, _handle.AnchoredRectangle.Left), System.Math.Min(_anchoredRectangle.Top, _handle.AnchoredRectangle.Top), num, System.Math.Max(_anchoredRectangle.Height, _handle.AnchoredRectangle.Height));
	}

	protected override void ApplyParentScroll(Point scaledParentScroll)
	{
		base.ApplyParentScroll(scaledParentScroll);
		UpdateHitboxRectangle();
	}

	public override Element HitTest(Point position)
	{
		return _hitboxRectangle.Contains(position) ? this : null;
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		if ((long)evt.Button == 1)
		{
			_isDragging = true;
			SetValueFromMouseX(Desktop.MousePosition.X);
		}
	}

	protected override void OnMouseEnter()
	{
		if (Style.Sounds?.MouseHover != null)
		{
			Desktop.Provider.PlaySound(Style.Sounds?.MouseHover);
		}
	}

	protected override void OnMouseMove()
	{
		if (_isDragging)
		{
			SetValueFromMouseX(Desktop.MousePosition.X);
		}
	}

	private void SetValueFromMouseX(int x)
	{
		int num = Desktop.UnscaleRound(_rectangleAfterPadding.Width);
		int num2 = MathHelper.Clamp(Desktop.UnscaleRound(x - _rectangleAfterPadding.X), 0, num);
		int num3 = MathHelper.Clamp(Min + (int)(System.Math.Round((float)num2 * (float)(Max - Min) / (float)num / (float)Step) * (double)Step), Min, Max);
		if (Value != num3)
		{
			Value = num3;
			Layout();
			ValueChanged?.Invoke();
		}
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		if ((long)evt.Button == 1)
		{
			if (Style.Sounds?.Activate != null)
			{
				Desktop.Provider.PlaySound(Style.Sounds?.Activate);
			}
			_isDragging = false;
			MouseButtonReleased?.Invoke();
		}
	}

	protected override void PrepareForDrawSelf()
	{
		base.PrepareForDrawSelf();
		if (_paddedBackgroundPatch != null)
		{
			Desktop.Batcher2D.RequestDrawPatch(_paddedBackgroundPatch, _rectangleAfterPadding, Desktop.Scale);
		}
	}
}
