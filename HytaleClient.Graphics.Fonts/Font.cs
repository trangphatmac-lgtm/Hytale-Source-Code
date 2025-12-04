#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Math;
using HytaleClient.Utils;
using NLog;
using SDL2;
using Utf8Json;

namespace HytaleClient.Graphics.Fonts;

public class Font : Disposable
{
	public struct CharacterRange
	{
		public readonly ushort Start;

		public readonly ushort End;

		public CharacterRange(ushort start, ushort end)
		{
			Start = start;
			End = end;
		}
	}

	public static CharacterRange[] DefaultCharacterRanges = new CharacterRange[3]
	{
		new CharacterRange(32, 127),
		new CharacterRange(160, 255),
		new CharacterRange(1024, 1279)
	};

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public readonly string Name;

	public readonly int Height;

	public readonly int LineSkip;

	public readonly int Ascent;

	public readonly int Descent;

	public readonly int FontId;

	public readonly int BaseSize;

	public readonly int Spread;

	public readonly int ScaledownFactor;

	public readonly Dictionary<ushort, float> GlyphAdvances = new Dictionary<ushort, float>();

	public readonly Dictionary<ushort, Rectangle> GlyphAtlasRectangles = new Dictionary<ushort, Rectangle>();

	private readonly GraphicsDevice _graphics;

	private readonly IntPtr _ttfFont;

	private Point _nextGlyphLocation;

	private int _glyphLowestY;

	private readonly HashSet<ushort> _missingGlyphs = new HashSet<ushort>();

	private bool _hasWarnedAboutFullAtlas;

	private int _width;

	private int _height;

	private byte[] _initialPixels;

	private CharacterRange[] _initialCharacterRanges;

	public float FallbackGlyphAdvance { get; private set; }

	public Rectangle FallbackGlyphAtlasRectangle { get; private set; }

	public Texture TextureAtlas { get; private set; }

	public Font(GraphicsDevice graphics, string basePath, int fontId, int baseSize = 32, int sdfScaledownFactor = 8, int sdfSpread = 8, CharacterRange[] initialCharacterRanges = null)
	{
		_graphics = graphics;
		FontId = fontId;
		BaseSize = baseSize;
		Spread = sdfSpread;
		ScaledownFactor = sdfScaledownFactor;
		string text = basePath + ".ttf";
		_ttfFont = SDL_ttf.TTF_OpenFont(text, BaseSize * sdfScaledownFactor);
		if (_ttfFont == IntPtr.Zero)
		{
			Logger.Error(SDL.SDL_GetError());
			throw new Exception("Could not open font from: " + text);
		}
		Name = SDL_ttf.TTF_FontFaceFamilyName(_ttfFont);
		Height = SDL_ttf.TTF_FontHeight(_ttfFont) / sdfScaledownFactor;
		LineSkip = SDL_ttf.TTF_FontLineSkip(_ttfFont) / sdfScaledownFactor;
		Ascent = SDL_ttf.TTF_FontAscent(_ttfFont) / sdfScaledownFactor;
		Descent = SDL_ttf.TTF_FontDescent(_ttfFont) / sdfScaledownFactor;
		string text2 = basePath + "Atlas.png";
		string text3 = basePath + "Glyphs.json";
		_width = 2048;
		_height = 2048;
		if (File.Exists(text2) && File.Exists(text3))
		{
			Image image = null;
			try
			{
				image = new Image(File.ReadAllBytes(text2));
				_initialPixels = image.Pixels;
				dynamic val = JsonSerializer.Deserialize<object>(File.ReadAllBytes(text3));
				foreach (KeyValuePair<string, object> item in (IDictionary<string, object>)val)
				{
					ushort key = ushort.Parse(item.Key, CultureInfo.InvariantCulture);
					GlyphAdvances[key] = (float)((dynamic)item.Value)["Advance"];
					int num = (int)((dynamic)item.Value)["Y"];
					int num2 = (int)((dynamic)item.Value)["Height"];
					GlyphAtlasRectangles[key] = new Rectangle((int)((dynamic)item.Value)["X"], num, (int)((dynamic)item.Value)["Width"], num2);
					_glyphLowestY = System.Math.Max(_glyphLowestY, num + num2);
				}
				_nextGlyphLocation = new Point(0, _glyphLowestY);
			}
			catch (Exception ex)
			{
				if (image == null)
				{
					Logger.Error(ex, "Failed to load font atlas: " + text2);
				}
				else
				{
					Logger.Error(ex, "Failed to load glyph file: " + text3);
				}
			}
		}
		_initialCharacterRanges = initialCharacterRanges ?? DefaultCharacterRanges;
	}

	public void BuildTexture()
	{
		if (_initialPixels == null)
		{
			_initialPixels = new byte[_width * _height * 4];
			for (int i = 0; i < _initialPixels.Length; i++)
			{
				_initialPixels[i] = byte.MaxValue;
			}
		}
		TextureAtlas = new Texture(Texture.TextureTypes.Texture2D);
		TextureAtlas.CreateTexture2D(_width, _height, _initialPixels, 5, GL.LINEAR, GL.LINEAR);
		_initialPixels = null;
		HashSet<ushort> hashSet = new HashSet<ushort>();
		CharacterRange[] initialCharacterRanges = _initialCharacterRanges;
		for (int j = 0; j < initialCharacterRanges.Length; j++)
		{
			CharacterRange characterRange = initialCharacterRanges[j];
			for (ushort num = characterRange.Start; num <= characterRange.End; num++)
			{
				if (!GlyphAtlasRectangles.ContainsKey(num))
				{
					hashSet.Add(num);
				}
			}
		}
		if (hashSet.Count > 0)
		{
			AddGlyphs(hashSet);
		}
		FallbackGlyphAdvance = GlyphAdvances[63];
		FallbackGlyphAtlasRectangle = GlyphAtlasRectangles[63];
	}

	public unsafe void WriteCacheToDisk(string basePath)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		string path = basePath + "Atlas.png";
		string path2 = basePath + "Glyphs.json";
		byte[] array = new byte[TextureAtlas.Width * TextureAtlas.Height * 4];
		_graphics.GL.BindTexture(GL.TEXTURE_2D, TextureAtlas.GLTexture);
		fixed (byte* ptr = array)
		{
			_graphics.GL.GetTexImage(GL.TEXTURE_2D, 0, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr);
		}
		new Image(TextureAtlas.Width, TextureAtlas.Height, array).SavePNG(path);
		Dictionary<ushort, Dictionary<string, object>> dictionary = new Dictionary<ushort, Dictionary<string, object>>();
		foreach (ushort key in GlyphAtlasRectangles.Keys)
		{
			Rectangle rectangle = GlyphAtlasRectangles[key];
			Dictionary<string, object> value = new Dictionary<string, object>
			{
				{
					"Advance",
					GlyphAdvances[key]
				},
				{ "X", rectangle.X },
				{ "Y", rectangle.Y },
				{ "Width", rectangle.Width },
				{ "Height", rectangle.Height }
			};
			dictionary.Add(key, value);
		}
		File.WriteAllBytes(path2, JsonSerializer.Serialize<Dictionary<ushort, Dictionary<string, object>>>(dictionary));
	}

	protected override void DoDispose()
	{
		TextureAtlas?.Dispose();
		if (_ttfFont != IntPtr.Zero)
		{
			SDL_ttf.TTF_CloseFont(_ttfFont);
		}
	}

	public void BuildMissingGlyphs()
	{
		if (_missingGlyphs.Count > 0)
		{
			AddGlyphs(_missingGlyphs);
		}
		_missingGlyphs.Clear();
	}

	private unsafe void AddGlyphs(IEnumerable<ushort> characters)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		Debug.Assert(ThreadHelper.IsMainThread());
		_graphics.GL.BindTexture(GL.TEXTURE_2D, TextureAtlas.GLTexture);
		SDL_Color val = default(SDL_Color);
		val.r = 0;
		val.g = 0;
		val.b = 0;
		val.a = byte.MaxValue;
		SDL_Color val2 = val;
		int num = default(int);
		int num2 = default(int);
		int num3 = default(int);
		int num4 = default(int);
		int num5 = default(int);
		foreach (ushort character in characters)
		{
			if (SDL_ttf.TTF_GlyphIsProvided(_ttfFont, character) == 0)
			{
				GlyphAdvances[character] = FallbackGlyphAdvance;
				GlyphAtlasRectangles[character] = FallbackGlyphAtlasRectangle;
				continue;
			}
			SDL_ttf.TTF_GlyphMetrics(_ttfFont, character, ref num, ref num2, ref num3, ref num4, ref num5);
			GlyphAdvances[character] = (float)num5 / (float)ScaledownFactor;
			IntPtr intPtr = SDL_ttf.TTF_RenderGlyph_Solid(_ttfFont, character, val2);
			SDL.SDL_LockSurface(intPtr);
			SDL_Surface val3 = (SDL_Surface)Marshal.PtrToStructure(intPtr, typeof(SDL_Surface));
			byte[] array = new byte[val3.w * val3.h];
			for (int i = 0; i < val3.h; i++)
			{
				Marshal.Copy(val3.pixels + i * val3.pitch, array, i * val3.w, val3.w);
			}
			int outputWidth;
			int outputHeight;
			byte[] pixels = SignedDistanceField.Generate(array, val3.w, val3.h, ScaledownFactor, Spread, 1, out outputWidth, out outputHeight);
			SDL.SDL_UnlockSurface(intPtr);
			SDL.SDL_FreeSurface(intPtr);
			int num6 = _nextGlyphLocation.X + outputWidth;
			if (num6 >= TextureAtlas.Width)
			{
				_nextGlyphLocation.Y = _glyphLowestY;
				_nextGlyphLocation.X = 0;
			}
			int num7 = _nextGlyphLocation.Y + outputHeight;
			if (num7 >= TextureAtlas.Height)
			{
				if (!_hasWarnedAboutFullAtlas)
				{
					Logger.Warn("{0} font atlas is full, glyph can't be added. Must implement resizing.", Name);
				}
				_hasWarnedAboutFullAtlas = true;
				GlyphAdvances[character] = FallbackGlyphAdvance;
				GlyphAtlasRectangles[character] = FallbackGlyphAtlasRectangle;
				continue;
			}
			Image image = new Image($"Character {(char)character}", outputWidth, outputHeight, pixels);
			Rectangle rectangle2 = (GlyphAtlasRectangles[character] = new Rectangle(_nextGlyphLocation.X, _nextGlyphLocation.Y, outputWidth, outputHeight));
			Rectangle rectangle3 = rectangle2;
			_nextGlyphLocation.X += outputWidth;
			_glyphLowestY = _nextGlyphLocation.Y + outputHeight;
			fixed (byte* ptr = image.Pixels)
			{
				_graphics.GL.TexSubImage2D(GL.TEXTURE_2D, 0, rectangle3.X, rectangle3.Y, rectangle3.Width, rectangle3.Height, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr);
			}
		}
	}

	public float GetCharacterAdvance(ushort character)
	{
		if (!GlyphAdvances.TryGetValue(character, out var value))
		{
			if (SDL_ttf.TTF_GlyphIsProvided(_ttfFont, character) == 0)
			{
				value = (GlyphAdvances[character] = FallbackGlyphAdvance);
				GlyphAtlasRectangles[character] = FallbackGlyphAtlasRectangle;
			}
			else
			{
				int num = default(int);
				int num2 = default(int);
				int num3 = default(int);
				int num4 = default(int);
				int num5 = default(int);
				SDL_ttf.TTF_GlyphMetrics(_ttfFont, character, ref num, ref num2, ref num3, ref num4, ref num5);
				value = (GlyphAdvances[character] = (float)num5 / (float)ScaledownFactor);
				_missingGlyphs.Add(character);
			}
		}
		return value;
	}

	public float CalculateTextWidth(string text)
	{
		float num = 0f;
		foreach (ushort character in text)
		{
			num += GetCharacterAdvance(character);
		}
		return num;
	}
}
