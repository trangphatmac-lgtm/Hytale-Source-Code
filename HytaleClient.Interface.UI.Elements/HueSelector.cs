using System;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.Interface.UI.Elements;

internal class HueSelector : Element
{
	public float Hue;

	public ColorPickerStyle Style;

	private TexturePatch _buttonPatch;

	private TexturePatch _buttonFillPatch;

	public Action<float> ValueChanged;

	public Action MouseButtonReleased;

	private Rectangle _buttonRectangle;

	private Texture _texture;

	private Rectangle _textureRect;

	public HueSelector(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	protected override void OnMounted()
	{
		_textureRect = new Rectangle(0, 0, 1, 400);
		byte[] array = new byte[_textureRect.Height * 4];
		for (int i = 0; i < _textureRect.Height; i++)
		{
			int num = i * 4;
			new ColorHsva((float)(_textureRect.Height - i) / (float)_textureRect.Height, 1f, 1f, 1f).ToRgba(out byte r, out byte g, out byte b, out byte _);
			array[num] = r;
			array[num + 1] = g;
			array[num + 2] = b;
			array[num + 3] = byte.MaxValue;
		}
		_texture = new Texture(Texture.TextureTypes.Texture2D);
		_texture.CreateTexture2D(_textureRect.Width, _textureRect.Height, array);
	}

	protected override void OnUnmounted()
	{
		_texture.Dispose();
		_texture = null;
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		_buttonPatch = Desktop.MakeTexturePatch(Style.ButtonBackground);
		_buttonFillPatch = Desktop.MakeTexturePatch(Style.ButtonFill);
	}

	protected override void OnMouseMove()
	{
		if (base.CapturedMouseButton == 1u)
		{
			UpdateHueFromMousePosition();
		}
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		if (base.CapturedMouseButton == 1u)
		{
			UpdateHueFromMousePosition();
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
		int height = _rectangleAfterPadding.Height;
		int num2 = System.Math.Min((int)System.Math.Round((float)height - Hue * (float)height), height - 1);
		int x = _rectangleAfterPadding.X + _rectangleAfterPadding.Width / 2 - num / 2;
		float num3 = (float)(_rectangleAfterPadding.Y + num2) - (float)num / 2f;
		_buttonRectangle = new Rectangle(x, (int)num3, num, num);
	}

	private void UpdateHueFromMousePosition()
	{
		Hue = 1f - MathHelper.Clamp((float)(Desktop.MousePosition.Y - _rectangleAfterPadding.Top) / (float)_rectangleAfterPadding.Height, 0f, 1f);
		ValueChanged?.Invoke(Hue);
		UpdateButtonRectangle();
	}

	protected override void PrepareForDrawSelf()
	{
		Desktop.Batcher2D.RequestDrawTexture(_texture, _textureRect, _rectangleAfterPadding, UInt32Color.White);
		new ColorHsva(Hue, 1f, 1f, 1f).ToRgba(out byte r, out byte g, out byte b, out byte _);
		Desktop.Batcher2D.RequestDrawTexture(_buttonFillPatch.TextureArea.Texture, _buttonFillPatch.TextureArea.Rectangle, _buttonRectangle, UInt32Color.FromRGBA(r, g, b, byte.MaxValue));
		Desktop.Batcher2D.RequestDrawPatch(_buttonPatch, _buttonRectangle, Desktop.Scale);
	}
}
