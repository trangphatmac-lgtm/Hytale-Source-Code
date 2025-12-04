using HytaleClient.Graphics;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Math;

namespace HytaleClient.Interface.InGame;

internal class DebugStressComponent : InterfaceComponent
{
	private Font _font;

	public DebugStressComponent(InGameView view)
		: base(view.Interface, view.HudContainer)
	{
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		_font = Desktop.Provider.GetFontFamily("Default").RegularFont;
	}

	protected override void PrepareForDrawSelf()
	{
		Vector3 zero = Vector3.Zero;
		Rectangle destRect = new Rectangle(0, 0, 20, 10);
		UInt32Color color = UInt32Color.FromRGBA(0, 0, 0, 100);
		TextureArea whitePixel = Interface.WhitePixel;
		for (int i = 0; i < 1000; i++)
		{
			zero.X = (destRect.X = 400 + i % 20 * 40);
			zero.Y = (destRect.Y = i / 20 * 11);
			Desktop.Batcher2D.RequestDrawText(_font, 10f, "Debug", zero, UInt32Color.White);
			Desktop.Batcher2D.RequestDrawTexture(whitePixel.Texture, whitePixel.Rectangle, destRect, color);
		}
	}
}
