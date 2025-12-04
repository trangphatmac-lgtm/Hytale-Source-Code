using System;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.Interface.UI.Elements;

internal class HsvPicker : Element
{
	private Texture _texture;

	private Rectangle _textureRect;

	private byte[] _data;

	private bool _isTextureDirty;

	public ColorPickerStyle Style;

	private TexturePatch _buttonPatch;

	private TexturePatch _buttonFillPatch;

	private Rectangle _buttonRectangle;

	public Action ValueChanged;

	public Action MouseButtonReleased;

	public bool IsShortColor;

	public float Hue { get; private set; }

	public float Saturation { get; private set; }

	public float Value { get; private set; }

	public HsvPicker(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	protected override void ApplyStyles()
	{
		_buttonPatch = Desktop.MakeTexturePatch(Style.ButtonBackground);
		_buttonFillPatch = Desktop.MakeTexturePatch(Style.ButtonFill);
	}

	protected override void OnMounted()
	{
		_textureRect = new Rectangle(0, 0, 400, 400);
		_data = new byte[_textureRect.Width * _textureRect.Height * 4];
		_texture = new Texture(Texture.TextureTypes.Texture2D);
		_texture.CreateTexture2D(_textureRect.Width, _textureRect.Height, _data);
		_isTextureDirty = true;
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		_texture.Dispose();
		_texture = null;
		Desktop.UnregisterAnimationCallback(Animate);
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		if (base.CapturedMouseButton == 1u)
		{
			UpdateColorFromMousePosition();
			ValueChanged?.Invoke();
		}
	}

	protected override void OnMouseMove()
	{
		if (base.CapturedMouseButton == 1u)
		{
			UpdateColorFromMousePosition();
			ValueChanged?.Invoke();
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
		int height = _rectangleAfterPadding.Height;
		int num2 = System.Math.Min((int)System.Math.Round(Saturation * (float)width), width - 1);
		int num3 = System.Math.Min((int)System.Math.Round(Value * (float)height), height - 1);
		float num4 = (float)(_rectangleAfterPadding.X + num2) - (float)num / 2f;
		float num5 = (float)(_rectangleAfterPadding.Y + (height - num3)) - (float)num / 2f;
		_buttonRectangle = new Rectangle((int)num4, (int)num5, num, num);
	}

	private void UpdateColorFromMousePosition()
	{
		Saturation = MathHelper.Clamp((float)(Desktop.MousePosition.X - _rectangleAfterPadding.Left) / (float)_rectangleAfterPadding.Width, 0f, 1f);
		Value = 1f - MathHelper.Clamp((float)(Desktop.MousePosition.Y - _rectangleAfterPadding.Top) / (float)_rectangleAfterPadding.Height, 0f, 1f);
		_isTextureDirty = true;
		UpdateButtonRectangle();
	}

	private void Animate(float dt)
	{
		if (_isTextureDirty)
		{
			_isTextureDirty = false;
			UpdateTexture();
		}
	}

	public void SetHue(float hue)
	{
		Hue = hue;
		_isTextureDirty = true;
	}

	public void SetColor(float hue, float saturation, float value)
	{
		Hue = hue;
		Saturation = saturation;
		Value = value;
		_isTextureDirty = true;
	}

	public ColorRgba GetColorRgba()
	{
		new ColorHsva(Hue, Saturation, Value, 1f).ToRgba(out byte r, out byte g, out byte b, out byte _);
		return new ColorRgba(r, g, b);
	}

	private void UpdateTexture()
	{
		int width = _texture.Width;
		int height = _texture.Height;
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				new ColorHsva(Hue, (float)j / (float)width, (float)i / (float)height, 1f).ToRgba(out byte r, out byte g, out byte b, out byte _);
				int num = ((height - i - 1) * width + j) * 4;
				if (IsShortColor)
				{
					_data[num] = (byte)(System.Math.Round((float)(int)r / 255f * 15f) / 15.0 * 255.0);
					_data[num + 1] = (byte)(System.Math.Round((float)(int)g / 255f * 15f) / 15.0 * 255.0);
					_data[num + 2] = (byte)(System.Math.Round((float)(int)b / 255f * 15f) / 15.0 * 255.0);
					_data[num + 3] = byte.MaxValue;
				}
				else
				{
					_data[num] = r;
					_data[num + 1] = g;
					_data[num + 2] = b;
					_data[num + 3] = byte.MaxValue;
				}
			}
		}
		_texture.UpdateTexture2D(_data);
	}

	protected override void PrepareForDrawSelf()
	{
		Desktop.Batcher2D.RequestDrawTexture(_texture, _textureRect, _rectangleAfterPadding, UInt32Color.White);
		new ColorHsva(Hue, Saturation, Value, 1f).ToRgba(out byte r, out byte g, out byte b, out byte _);
		Desktop.Batcher2D.RequestDrawTexture(_buttonFillPatch.TextureArea.Texture, _buttonFillPatch.TextureArea.Rectangle, _buttonRectangle, UInt32Color.FromRGBA(r, g, b, byte.MaxValue));
		Desktop.Batcher2D.RequestDrawPatch(_buttonPatch, _buttonRectangle, Desktop.Scale);
	}
}
