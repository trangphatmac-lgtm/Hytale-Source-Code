#define DEBUG
using System.Diagnostics;
using System.IO;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Utils;

namespace HytaleClient.Core;

internal class FontManager : Disposable
{
	private int _fontCount = 0;

	public Texture TextureArray2D { get; private set; }

	public FontFamily DefaultFontFamily { get; private set; }

	public FontFamily SecondaryFontFamily { get; private set; }

	public FontFamily MonospaceFontFamily { get; private set; }

	internal void LoadFonts(GraphicsDevice graphics)
	{
		Font regularFont = new Font(graphics, Path.Combine(Paths.SharedData, "Fonts/NotoSans-Regular"), _fontCount++);
		Font boldFont = new Font(graphics, Path.Combine(Paths.SharedData, "Fonts/NotoSans-Bold"), _fontCount++);
		Font regularFont2 = new Font(graphics, Path.Combine(Paths.SharedData, "Fonts/PenumbraSerifStd-Semibold"), _fontCount++);
		Font boldFont2 = new Font(graphics, Path.Combine(Paths.SharedData, "Fonts/PenumbraSerifStd-Bold"), _fontCount++);
		Font regularFont3 = new Font(graphics, Path.Combine(Paths.SharedData, "Fonts/NotoMono-Regular"), _fontCount++);
		DefaultFontFamily = new FontFamily(regularFont, boldFont);
		SecondaryFontFamily = new FontFamily(regularFont2, boldFont2);
		MonospaceFontFamily = new FontFamily(regularFont3, null);
	}

	internal void BuildFontTextures()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		DefaultFontFamily.RegularFont.BuildTexture();
		DefaultFontFamily.BoldFont.BuildTexture();
		SecondaryFontFamily.RegularFont.BuildTexture();
		SecondaryFontFamily.BoldFont.BuildTexture();
		MonospaceFontFamily.RegularFont.BuildTexture();
		TextureArray2D = new Texture(Texture.TextureTypes.Texture2DArray);
		TextureArray2D.CreateTexture2DArray(2048, 2048, _fontCount, null, GL.LINEAR, GL.LINEAR, GL.REPEAT, GL.REPEAT);
		TextureArray2D.UpdateTexture2DArrayLayer(DefaultFontFamily.RegularFont.TextureAtlas, DefaultFontFamily.RegularFont.FontId);
		TextureArray2D.UpdateTexture2DArrayLayer(DefaultFontFamily.BoldFont.TextureAtlas, DefaultFontFamily.BoldFont.FontId);
		TextureArray2D.UpdateTexture2DArrayLayer(SecondaryFontFamily.RegularFont.TextureAtlas, SecondaryFontFamily.RegularFont.FontId);
		TextureArray2D.UpdateTexture2DArrayLayer(SecondaryFontFamily.BoldFont.TextureAtlas, SecondaryFontFamily.BoldFont.FontId);
		TextureArray2D.UpdateTexture2DArrayLayer(MonospaceFontFamily.RegularFont.TextureAtlas, MonospaceFontFamily.RegularFont.FontId);
	}

	internal void BuildMissingGlyphs()
	{
		DefaultFontFamily?.RegularFont.BuildMissingGlyphs();
		DefaultFontFamily?.BoldFont.BuildMissingGlyphs();
		SecondaryFontFamily?.RegularFont.BuildMissingGlyphs();
		SecondaryFontFamily?.BoldFont.BuildMissingGlyphs();
		MonospaceFontFamily?.RegularFont.BuildMissingGlyphs();
	}

	protected override void DoDispose()
	{
		TextureArray2D?.Dispose();
		DefaultFontFamily?.RegularFont.Dispose();
		DefaultFontFamily?.BoldFont.Dispose();
		SecondaryFontFamily?.RegularFont.Dispose();
		SecondaryFontFamily?.BoldFont.Dispose();
		MonospaceFontFamily?.RegularFont.Dispose();
	}

	public FontFamily GetFontFamilyByName(string name)
	{
		return name switch
		{
			"Default" => DefaultFontFamily, 
			"Secondary" => SecondaryFontFamily, 
			"Mono" => MonospaceFontFamily, 
			_ => null, 
		};
	}
}
