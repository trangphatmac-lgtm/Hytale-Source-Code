#define DEBUG
using System.Diagnostics;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;

namespace HytaleClient.Interface.UI.Styles;

[UIMarkupData]
public class PatchStyle
{
	private TextureArea _textureArea;

	private UIPath _texturePath;

	public Rectangle? Area;

	public UInt32Color Color = UInt32Color.White;

	public Anchor? Anchor;

	public int HorizontalBorder;

	public int VerticalBorder;

	internal TextureArea TextureArea
	{
		get
		{
			return _textureArea;
		}
		set
		{
			Debug.Assert(_texturePath == null);
			_textureArea = value;
		}
	}

	public UIPath TexturePath
	{
		get
		{
			return _texturePath;
		}
		set
		{
			Debug.Assert(_textureArea == null);
			_texturePath = value;
		}
	}

	public int Border
	{
		set
		{
			HorizontalBorder = (VerticalBorder = value);
		}
	}

	public PatchStyle()
	{
	}

	public PatchStyle(TextureArea textureArea)
	{
		_textureArea = textureArea;
	}

	public PatchStyle(string texturePath)
	{
		_texturePath = new UIPath(texturePath);
	}

	public PatchStyle(UInt32Color color)
	{
		Color = color;
	}

	public PatchStyle(uint rgba)
		: this(UInt32Color.FromRGBA(rgba))
	{
	}

	public PatchStyle Clone()
	{
		return new PatchStyle
		{
			_texturePath = _texturePath,
			_textureArea = _textureArea,
			Anchor = Anchor,
			Area = Area,
			Color = Color,
			HorizontalBorder = HorizontalBorder,
			VerticalBorder = VerticalBorder
		};
	}
}
