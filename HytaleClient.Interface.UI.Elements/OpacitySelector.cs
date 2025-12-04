using System;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.UI.Elements;

internal class OpacitySelector : Element
{
	public float Opacity;

	public ColorPickerStyle Style;

	private TexturePatch _buttonPatch;

	private TexturePatch _buttonFillPatch;

	public Action MouseButtonReleased;

	private Rectangle _buttonRectangle;

	private readonly Label _label;

	public OpacitySelector(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		_label = new Label(Desktop, this)
		{
			Visible = false,
			Background = new PatchStyle(UInt32Color.FromRGBA(0, 0, 0, 150)),
			Style = new LabelStyle
			{
				FontSize = 13f,
				Alignment = LabelStyle.LabelAlignment.Center
			},
			Anchor = new Anchor
			{
				Width = 38,
				Height = 24,
				Top = -30
			}
		};
	}

	protected override void OnUnmounted()
	{
		_label.Visible = false;
	}

	protected override void ApplyStyles()
	{
		Background = Style.OpacitySelectorBackground;
		base.ApplyStyles();
		_buttonPatch = Desktop.MakeTexturePatch(Style.ButtonBackground);
		_buttonFillPatch = Desktop.MakeTexturePatch(Style.ButtonFill);
	}

	protected override void OnMouseEnter()
	{
		_label.Visible = true;
		Layout();
	}

	protected override void OnMouseLeave()
	{
		_label.Visible = false;
	}

	protected override void OnMouseMove()
	{
		if (base.CapturedMouseButton == 1u)
		{
			UpdateOpacityFromMousePosition();
		}
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		if (base.CapturedMouseButton == 1u)
		{
			UpdateOpacityFromMousePosition();
		}
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		MouseButtonReleased?.Invoke();
	}

	protected override void LayoutSelf()
	{
		UpdateButtonRectangle();
	}

	public override Element HitTest(Point position)
	{
		if (!base.Visible || (!_rectangleAfterPadding.Contains(position) && !_buttonRectangle.Contains(position)))
		{
			return null;
		}
		return this;
	}

	private void UpdateButtonRectangle()
	{
		int num = Desktop.ScaleRound(16f);
		int width = _rectangleAfterPadding.Width;
		float num2 = (float)_rectangleAfterPadding.X + Opacity * (float)width - (float)num / 2f;
		int y = _rectangleAfterPadding.Y + _rectangleAfterPadding.Height / 2 - num / 2;
		_buttonRectangle = new Rectangle((int)num2, y, num, num);
		_label.Text = Desktop.Provider.FormatNumber((int)(Opacity * 100f)) + "%";
		_label.Anchor.Left = Desktop.UnscaleRound(Opacity * (float)width) - _label.Anchor.Width / 2;
		_label.Layout();
	}

	private void UpdateOpacityFromMousePosition()
	{
		Opacity = MathHelper.Clamp((float)(Desktop.MousePosition.X - _rectangleAfterPadding.Left) / (float)_rectangleAfterPadding.Width, 0f, 1f);
		UpdateButtonRectangle();
	}

	protected override void PrepareForDrawSelf()
	{
		base.PrepareForDrawSelf();
		byte b = (byte)(255f * (1f - Opacity));
		Desktop.Batcher2D.RequestDrawTexture(_buttonFillPatch.TextureArea.Texture, _buttonFillPatch.TextureArea.Rectangle, _buttonRectangle, UInt32Color.FromRGBA(b, b, b, byte.MaxValue));
		Desktop.Batcher2D.RequestDrawPatch(_buttonPatch, _buttonRectangle, Desktop.Scale);
	}
}
