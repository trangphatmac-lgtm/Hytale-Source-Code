using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HytaleClient.Audio;
using HytaleClient.Common.Collections;
using HytaleClient.Common.Memory;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Graphics.Programs;
using HytaleClient.Interface.CoherentUI;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using HytaleClient.Utils;
using NLog;
using SDL2;

namespace HytaleClient.Interface;

internal abstract class BaseInterface : Disposable, IUIProvider
{
	public enum InterfaceFadeState
	{
		FadedIn,
		FadingIn,
		FadingOut,
		FadedOut
	}

	public static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public readonly Engine Engine;

	public readonly CoUIManager CoUiManager;

	public readonly Desktop Desktop;

	private readonly FontManager _fonts;

	private readonly string _resourcesPath;

	private readonly bool _isDevModeEnabled;

	public const int CustomUIErrorLayerKey = 1;

	public const int PopupLayerKey = 2;

	public const int OverlayLayerKey = 3;

	public const int ModalLayerKey = 4;

	public const int ConsoleLayerKey = 5;

	public bool HasMarkupError;

	public bool HasLoaded;

	private Action _onFadeComplete;

	private float _fadeTimer;

	private float _fadeDuration;

	private float _flashTimer = -1f;

	private const float FlashDuration = 0.35f;

	private readonly Desktop _markupErrorDesktop;

	private readonly MarkupErrorOverlay _markupErrorLayer;

	private float _watchDelayTimer;

	private const float WatchDelayDuration = 0.25f;

	private readonly Dictionary<string, Document> _documentsLibrary = new Dictionary<string, Document>();

	private string _language;

	private Dictionary<string, string> _clientTexts = new Dictionary<string, string>();

	private Dictionary<string, string> _serverTexts = new Dictionary<string, string>();

	public const string NameWhitePixel = "Special:WhitePixel";

	public const string NameMissing = "Special:Missing";

	private Texture _atlas;

	private Dictionary<string, TextureArea> _atlasTextureAreas = new Dictionary<string, TextureArea>();

	public InterfaceFadeState FadeState { get; private set; }

	public TextureArea WhitePixel { get; private set; }

	public TextureArea MissingTexture { get; private set; }

	public Point TextureAtlasSize => new Point(_atlas.Width, _atlas.Height);

	protected BaseInterface(Engine engine, FontManager fonts, CoUIManager coUiManager, string resourcesPath, bool isDevModeEnabled)
	{
		Engine = engine;
		CoUiManager = coUiManager;
		_fonts = fonts;
		_resourcesPath = resourcesPath;
		_isDevModeEnabled = isDevModeEnabled;
		Desktop = new Desktop(this, Engine.Graphics, Engine.Graphics.Batcher2D);
		if (_isDevModeEnabled)
		{
			_markupErrorDesktop = new Desktop(this, Engine.Graphics, Engine.Graphics.Batcher2D);
			_markupErrorLayer = new MarkupErrorOverlay(_markupErrorDesktop, null, "UI – Markup Error");
			FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(_resourcesPath)
			{
				IncludeSubdirectories = true
			};
			fileSystemWatcher.Changed += OnFilesChanged;
			fileSystemWatcher.Deleted += OnFilesChanged;
			fileSystemWatcher.Created += OnFilesChanged;
			fileSystemWatcher.EnableRaisingEvents = true;
		}
		SetupViewports();
	}

	private void OnFilesChanged(object sender, FileSystemEventArgs e)
	{
		_watchDelayTimer = 0.25f;
	}

	public void LoadAndBuild()
	{
		if (_isDevModeEnabled && HasMarkupError)
		{
			_markupErrorLayer.Clear();
			_markupErrorDesktop.ClearAllLayers();
			HasMarkupError = false;
		}
		Desktop.ClearInput();
		LoadTextures(Desktop.Scale > 1f);
		try
		{
			LoadDocuments();
		}
		catch (TextParser.TextParserException ex)
		{
			if (!_isDevModeEnabled)
			{
				throw ex;
			}
			DisplayError(ex.RawMessage, ex.Span);
			return;
		}
		Build();
		Desktop.Layout();
		HasLoaded = true;
		Logger.Info("Interface loaded.");
		void DisplayError(string message, TextParserSpan span)
		{
			_markupErrorLayer.Setup(message, span);
			_markupErrorDesktop.SetLayer(0, _markupErrorLayer);
			HasMarkupError = true;
		}
	}

	protected abstract void Build();

	protected override void DoDispose()
	{
		Desktop.ClearAllLayers();
		if (_isDevModeEnabled)
		{
			_markupErrorDesktop.ClearAllLayers();
		}
		DisposeTextures();
	}

	protected virtual float GetScale()
	{
		return Engine.Window.ViewportScale;
	}

	public void OnWindowSizeChanged()
	{
		float scale = GetScale();
		bool flag = scale > 1f;
		bool flag2 = Desktop.Scale > 1f;
		if (flag != flag2)
		{
			LoadTextures(flag);
		}
		SetupViewports();
	}

	private void SetupViewports()
	{
		float scale = GetScale();
		Desktop.SetViewport(Engine.Window.Viewport, scale);
		if (_isDevModeEnabled)
		{
			_markupErrorDesktop.SetViewport(Engine.Window.Viewport, scale);
		}
	}

	public void CancelOnFadeComplete()
	{
		_onFadeComplete = null;
	}

	public void FadeIn(Action onComplete = null, bool longFade = false)
	{
		if (_onFadeComplete != null)
		{
			throw new InvalidOperationException("Cannot start a fade in while a fade completion callback is pending.");
		}
		Desktop.ClearInput(clearFocus: false);
		FadeState = InterfaceFadeState.FadingIn;
		_fadeTimer = 0f;
		_fadeDuration = (longFade ? 1f : 0.15f);
		_onFadeComplete = onComplete;
	}

	public void FadeOut(Action onComplete = null)
	{
		if (_onFadeComplete != null)
		{
			throw new InvalidOperationException("Cannot start a fade out while a fade completion callback is pending.");
		}
		Desktop.ClearInput(clearFocus: false);
		FadeState = InterfaceFadeState.FadingOut;
		_fadeTimer = 0f;
		_fadeDuration = 0.15f;
		_onFadeComplete = onComplete;
	}

	public void ClearFlash()
	{
		_flashTimer = -1f;
	}

	public void Flash()
	{
		_flashTimer = 0f;
	}

	protected virtual void SetDrawOutlines(bool draw)
	{
		Desktop.DrawOutlines = !Desktop.DrawOutlines;
	}

	public FontFamily GetFontFamily(string name)
	{
		return _fonts.GetFontFamilyByName(name);
	}

	public unsafe void OnUserInput(SDL_Event evt)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Expected I4, but got Unknown
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Invalid comparison between Unknown and I4
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Expected I4, but got Unknown
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		if (_isDevModeEnabled)
		{
			if ((int)evt.type == 768 && (int)evt.key.keysym.sym == 1073741889 && Desktop.IsShortcutKeyDown)
			{
				SetDrawOutlines(!Desktop.DrawOutlines);
			}
			if (HasMarkupError)
			{
				return;
			}
		}
		if (FadeState != 0)
		{
			return;
		}
		SDL_EventType type = evt.type;
		SDL_EventType val = type;
		switch (val - 768)
		{
		case 0:
			Desktop.OnKeyDown(evt.key.keysym.sym, evt.key.repeat);
			return;
		case 1:
			Desktop.OnKeyUp(evt.key.keysym.sym);
			return;
		case 3:
		{
			NativeArray<byte> val2 = default(NativeArray<byte>);
			val2._002Ector(256, (Allocator)1, (AllocOptions)0);
			byte* ptr;
			for (ptr = &evt.text.text.FixedElementField; *ptr != 0; ptr++)
			{
			}
			int num = (int)(ptr - &evt.text.text.FixedElementField);
			NativeArray<byte>.Copy((void*)(&evt.text.text.FixedElementField), 0, val2, 0, num);
			string @string = Encoding.UTF8.GetString((byte*)val2.GetBuffer(), num);
			Desktop.OnTextInput(@string);
			return;
		}
		case 2:
			return;
		}
		switch (val - 1024)
		{
		case 1:
			Desktop.OnMouseDown(evt.button.button, evt.button.clicks);
			break;
		case 2:
			Desktop.OnMouseUp(evt.button.button, evt.button.clicks);
			break;
		case 0:
			Desktop.OnMouseMove(Engine.Window.TransformSDLToViewportCoords(evt.motion.x, evt.motion.y));
			break;
		case 3:
			if (Desktop.IsShiftKeyDown)
			{
				Desktop.OnMouseWheel(new Point(evt.wheel.y, evt.wheel.x));
			}
			else
			{
				Desktop.OnMouseWheel(new Point(evt.wheel.x, evt.wheel.y));
			}
			break;
		}
	}

	public void Update(float deltaTime)
	{
		if (_isDevModeEnabled && _watchDelayTimer > 0f)
		{
			_watchDelayTimer = System.Math.Max(0f, _watchDelayTimer - deltaTime);
			if (_watchDelayTimer == 0f)
			{
				LoadClientTexts();
				LoadAndBuild();
			}
		}
		if (FadeState != 0 && FadeState != InterfaceFadeState.FadedOut)
		{
			_fadeTimer += deltaTime;
			if (_fadeTimer >= _fadeDuration)
			{
				if (FadeState == InterfaceFadeState.FadingIn)
				{
					FadeState = InterfaceFadeState.FadedIn;
				}
				else
				{
					FadeState = InterfaceFadeState.FadedOut;
				}
				if (_onFadeComplete != null)
				{
					Engine.RunOnMainThread(Engine, _onFadeComplete, allowCallFromMainThread: true);
				}
				_onFadeComplete = null;
			}
		}
		if (_flashTimer >= 0f)
		{
			_flashTimer += deltaTime;
			if (_flashTimer >= 0.35f)
			{
				_flashTimer = -1f;
			}
		}
		Desktop.Update(deltaTime);
	}

	public void PrepareForDraw()
	{
		Desktop.PrepareForDraw();
		if (FadeState != 0)
		{
			float num = 1f;
			float num2 = System.Math.Min(1f, _fadeTimer / _fadeDuration);
			if (FadeState == InterfaceFadeState.FadingIn)
			{
				num = 1f - num2 * num2;
			}
			else if (FadeState == InterfaceFadeState.FadingOut)
			{
				num = num2 * num2;
			}
			Desktop.Batcher2D.RequestDrawTexture(Desktop.Graphics.WhitePixelTexture, new Rectangle(0, 0, 1, 1), Engine.Window.Viewport, UInt32Color.FromRGBA(0, 0, 0, (byte)(255f * num)));
		}
		if (_flashTimer > 0f)
		{
			float num3 = 1f - _flashTimer / 0.35f;
			Desktop.Batcher2D.RequestDrawTexture(Desktop.Graphics.WhitePixelTexture, new Rectangle(0, 0, 1, 1), Engine.Window.Viewport, UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(255f * num3)));
		}
		if (_isDevModeEnabled && HasMarkupError)
		{
			_markupErrorDesktop.PrepareForDraw();
		}
	}

	public void Draw()
	{
		GLFunctions gL = Engine.Graphics.GL;
		Matrix matrix = Matrix.CreateTranslation(0f, 0f, 5f) * Matrix.CreateOrthographicOffCenter(0f, Engine.Window.Viewport.Width, Engine.Window.Viewport.Height, 0f, 0.1f, 100f);
		Batcher2DProgram batcher2DProgram = Engine.Graphics.GPUProgramStore.Batcher2DProgram;
		gL.UseProgram(batcher2DProgram);
		batcher2DProgram.MVPMatrix.SetValue(ref matrix);
		gL.BlendFunc(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA);
		if (_fonts.TextureArray2D != null)
		{
			gL.ActiveTexture(GL.TEXTURE2);
			gL.BindTexture(GL.TEXTURE_2D_ARRAY, _fonts.TextureArray2D.GLTexture);
			gL.ActiveTexture(GL.TEXTURE0);
		}
		Desktop.Batcher2D.Draw();
		gL.BlendFunc(GL.ONE, GL.ONE_MINUS_SRC_ALPHA);
		gL.UseProgram(Engine.Graphics.GPUProgramStore.BasicProgram);
	}

	private void LoadDocuments()
	{
		_documentsLibrary.Clear();
		foreach (string item in Directory.EnumerateFiles(_resourcesPath, "*.ui", SearchOption.AllDirectories))
		{
			string text = item.Substring(_resourcesPath.Length + 1).Replace("\\", "/");
			Document value = DocumentParser.Parse(File.ReadAllText(item), text);
			_documentsLibrary.Add(text, value);
		}
		foreach (KeyValuePair<string, Document> item2 in _documentsLibrary)
		{
			item2.Value.ResolveProperties(Desktop.Provider);
		}
	}

	public bool TryGetDocument(string path, out Document document)
	{
		return _documentsLibrary.TryGetValue(path, out document);
	}

	private void LoadClientTexts()
	{
		_clientTexts = Language.LoadLanguage(_language);
	}

	public void SetLanguageAndLoad(string language)
	{
		_language = language;
		LoadClientTexts();
	}

	public string GetText(string key, Dictionary<string, string> parameters = null, bool returnFallback = true)
	{
		if (!_clientTexts.TryGetValue(key, out var value) && !_serverTexts.TryGetValue(key, out value))
		{
			return returnFallback ? key : null;
		}
		if (parameters != null)
		{
			foreach (KeyValuePair<string, string> parameter in parameters)
			{
				value = value.Replace("{" + parameter.Key + "}", parameter.Value);
			}
		}
		return value;
	}

	public string GetServerText(string key, Dictionary<string, string> parameters = null, bool returnFallback = true)
	{
		if (!_serverTexts.TryGetValue(key, out var value))
		{
			return returnFallback ? key : null;
		}
		if (parameters != null)
		{
			foreach (KeyValuePair<string, string> parameter in parameters)
			{
				value = value.Replace("{" + parameter.Key + "}", parameter.Value);
			}
		}
		return value;
	}

	public void SetServerMessages(Dictionary<string, string> dict)
	{
		_serverTexts = dict;
	}

	public void AddServerMessages(Dictionary<string, string> dict)
	{
		foreach (KeyValuePair<string, string> item in dict)
		{
			_serverTexts[item.Key] = item.Value;
		}
	}

	public void RemoveServerMessages(ICollection<string> keys)
	{
		foreach (string key in keys)
		{
			_serverTexts.Remove(key);
		}
	}

	public string FormatNumber(int value)
	{
		return value.ToString("N0");
	}

	public string FormatNumber(float value)
	{
		return value.ToString("N");
	}

	public string FormatNumber(double value)
	{
		return value.ToString("N");
	}

	public string FormatRelativeTime(DateTime dateTime)
	{
		TimeSpan timeSpan = DateTime.Now.Subtract(dateTime);
		if (timeSpan <= TimeSpan.FromSeconds(1.0))
		{
			return GetText("ui.general.relativeTime.now");
		}
		if (timeSpan <= TimeSpan.FromSeconds(60.0))
		{
			return GetText("ui.general.relativeTime.lessThanAMinute");
		}
		if (timeSpan <= TimeSpan.FromMinutes(60.0))
		{
			return GetText("ui.general.relativeTime.minute" + ((timeSpan.Minutes > 1) ? "s" : ""), new Dictionary<string, string> { 
			{
				"minutes, number",
				timeSpan.Minutes.ToString()
			} });
		}
		if (timeSpan <= TimeSpan.FromHours(24.0))
		{
			return GetText("ui.general.relativeTime.hour" + ((timeSpan.Hours > 1) ? "s" : ""), new Dictionary<string, string> { 
			{
				"hours, number",
				timeSpan.Hours.ToString()
			} });
		}
		if (timeSpan <= TimeSpan.FromDays(30.0))
		{
			return GetText("ui.general.relativeTime.day" + ((timeSpan.Days > 1) ? "s" : ""), new Dictionary<string, string> { 
			{
				"days, number",
				timeSpan.Days.ToString()
			} });
		}
		if (timeSpan <= TimeSpan.FromDays(365.0))
		{
			return GetText("ui.general.relativeTime.month" + ((timeSpan.Days > 30) ? "s" : ""), new Dictionary<string, string> { 
			{
				"months, number",
				(timeSpan.Days / 30).ToString()
			} });
		}
		return GetText("ui.general.relativeTime.year" + ((timeSpan.Days > 365) ? "s" : ""), new Dictionary<string, string> { 
		{
			"years, number",
			(timeSpan.Days / 365).ToString()
		} });
	}

	public void PlaySound(SoundStyle sound)
	{
		if (Engine.Audio != null)
		{
			if (!Engine.Audio.ResourceManager.WwiseEventIds.TryGetValue(sound.SoundPath.Value, out var value))
			{
				Logger.Warn("Unknown UI sound: {0}", sound.SoundPath.Value);
			}
			else
			{
				Engine.Audio.PostEvent(value, AudioDevice.PlayerSoundObjectReference);
			}
		}
	}

	protected virtual void LoadTextures(bool use2x)
	{
		_atlas?.Dispose();
		_atlasTextureAreas.Clear();
		List<string> list = new List<string>();
		foreach (string item2 in Directory.EnumerateFiles(_resourcesPath, "*.png", SearchOption.AllDirectories))
		{
			string item = item2.Substring(_resourcesPath.Length + 1);
			bool flag = item2.EndsWith("@2x.png");
			if (!use2x && flag)
			{
				string path = item2.Replace("@2x.png", ".png");
				if (File.Exists(path))
				{
					continue;
				}
			}
			else if (use2x && !flag)
			{
				string path2 = item2.Replace(".png", "@2x.png");
				if (File.Exists(path2))
				{
					continue;
				}
			}
			list.Add(item);
		}
		int num = (use2x ? 8192 : 4096);
		_atlas = new Texture(Texture.TextureTypes.Texture2D);
		_atlas.CreateTexture2D(num, num, null, 0, GL.LINEAR_MIPMAP_LINEAR, GL.LINEAR);
		List<Image> list2 = new List<Image>();
		list2.Add(MakeWhitePixelImage("Special:WhitePixel"));
		list2.Add(MakeMissingImage("Special:Missing"));
		foreach (string item3 in list)
		{
			list2.Add(new Image(item3, File.ReadAllBytes(Path.Combine(_resourcesPath, item3))));
		}
		list2.Sort(delegate(Image a, Image b)
		{
			int height = b.Height;
			return height.CompareTo(a.Height);
		});
		Dictionary<Image, Point> imageLocations;
		byte[] atlasPixels = Image.Pack(num, list2, out imageLocations, doPadding: true);
		_atlas.UpdateTexture2DMipMaps(Texture.BuildMipmapPixels(atlasPixels, num, _atlas.MipmapLevelCount));
		foreach (KeyValuePair<Image, Point> item4 in imageLocations)
		{
			Image key = item4.Key;
			Point value = item4.Value;
			string text = key.Name.Replace("\\", "/");
			int num2 = ((!text.EndsWith("@2x.png")) ? 1 : 2);
			if (num2 == 2)
			{
				text = text.Replace("@2x.png", ".png");
			}
			_atlasTextureAreas.Add(text, new TextureArea(_atlas, value.X, value.Y, key.Width, key.Height, num2));
		}
		WhitePixel = MakeTextureArea("Special:WhitePixel");
		MissingTexture = MakeTextureArea("Special:Missing");
	}

	private void DisposeTextures()
	{
		_atlas?.Dispose();
		_atlasTextureAreas.Clear();
	}

	public TextureArea MakeTextureArea(string path)
	{
		if (_atlasTextureAreas.TryGetValue(path, out var value))
		{
			return value.Clone();
		}
		return MissingTexture.Clone();
	}

	public static Image MakeWhitePixelImage(string name)
	{
		return new Image(name, 1, 1, new byte[4] { 255, 255, 255, 255 });
	}

	public static Image MakeMissingImage(string name)
	{
		byte[] array = new byte[4096];
		for (int i = 0; i < 32; i++)
		{
			for (int j = 0; j < 32; j++)
			{
				array[(i * 32 + j) * 4] = byte.MaxValue;
				if (System.Math.Abs(j - i) > 2 && System.Math.Abs(31 - j - i) > 2)
				{
					array[(i * 32 + j) * 4 + 1] = byte.MaxValue;
					array[(i * 32 + j) * 4 + 2] = byte.MaxValue;
				}
				array[(i * 32 + j) * 4 + 3] = byte.MaxValue;
			}
		}
		return new Image(name, 32, 32, array);
	}
}
