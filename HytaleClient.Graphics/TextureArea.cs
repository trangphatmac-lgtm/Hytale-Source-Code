using HytaleClient.Math;

namespace HytaleClient.Graphics;

public class TextureArea
{
	public Texture Texture;

	public Rectangle Rectangle;

	public int Scale;

	public TextureArea(Texture texture, int x, int y, int width, int height, int scale)
	{
		Texture = texture;
		Rectangle = new Rectangle(x, y, width, height);
		Scale = scale;
	}

	private TextureArea()
	{
	}

	public TextureArea Clone()
	{
		return new TextureArea
		{
			Texture = Texture,
			Rectangle = Rectangle,
			Scale = Scale
		};
	}
}
