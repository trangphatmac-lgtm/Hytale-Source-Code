#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Hypixel.ProtoPlus;
using HytaleClient.Application.Services;
using HytaleClient.Application.Services.Api;
using HytaleClient.Audio;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Data.Characters;
using HytaleClient.Graphics;
using HytaleClient.Graphics.BlockyModels;
using HytaleClient.Graphics.Programs;
using HytaleClient.Interface.MainMenu.Pages;
using HytaleClient.Math;
using HytaleClient.Networking;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SDL2;
using Wwise;

namespace HytaleClient.Application;

internal class AppMainMenu
{
	private class CharacterOnScreen : Disposable
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public const int IdleAnimationSlotIndex = 0;

		public const int EmoteAnimationSlotIndex = 1;

		public const float EmoteBlendingDuration = 12f;

		private const float DefaultCharacterModelAngle = -(float)System.Math.PI / 8f;

		private float _characterModelAngle;

		private float _lerpCharacterModelAngle;

		private bool _isMouseDragging;

		private int _dragStartMouseX;

		private float _dragStartCharacterModelAngle;

		public Rectangle Viewport;

		public ModelRenderer CharacterRenderer;

		public Vector3 Translation = new Vector3(0f, -5.3f, -16f);

		private Matrix _projectionMatrix;

		public float Scale;

		private readonly App _app;

		public CharacterOnScreen(App app, Rectangle viewport, float initialModelAngle = -(float)System.Math.PI / 8f, float scale = 1f)
		{
			_app = app;
			Viewport = viewport;
			Scale = scale;
			SetRotation(initialModelAngle);
		}

		protected override void DoDispose()
		{
			CharacterRenderer?.Dispose();
		}

		public void InitializeRendering(ClientPlayerSkin skin, CharacterPartStore characterPartStore)
		{
			float aspectRatio = (float)Viewport.Width / (float)Viewport.Height;
			_app.Engine.Graphics.CreatePerspectiveMatrix((float)System.Math.PI / 4f, aspectRatio, 0.1f, 1000f, out _projectionMatrix);
			Dictionary<string, CharacterPartTintColor> gradients = characterPartStore.GradientSets["Skin"].Gradients;
			if (!gradients.TryGetValue(skin.SkinTone, out var value))
			{
				value = gradients.First().Value;
			}
			BlockyModel andCloneModel = characterPartStore.GetAndCloneModel(characterPartStore.GetBodyModelPath(skin.BodyType));
			andCloneModel.OffsetUVs(characterPartStore.ImageLocations[(skin.BodyType == CharacterBodyType.Masculine) ? "Characters/Player_Textures/Masculine_Greyscale.png" : "Characters/Player_Textures/Feminine_Greyscale.png"]);
			andCloneModel.SetGradientId(value.GradientId);
			foreach (CharacterAttachment characterAttachment in characterPartStore.GetCharacterAttachments(skin))
			{
				if (characterAttachment.Model == null)
				{
					Logger.Warn("Model is not assigned");
					continue;
				}
				BlockyModel blockyModel = characterPartStore.GetAndCloneModel(characterAttachment.Model);
				if (blockyModel == null)
				{
					Logger.Warn("Tried to clone model which is not loaded or does not exist: {0}", characterAttachment.Model);
					continue;
				}
				if (!characterPartStore.ImageLocations.TryGetValue(characterAttachment.Texture, out var value2))
				{
					Logger.Warn("Tried to get model texture which is not loaded or does not exist: {0}", characterAttachment.Texture);
					continue;
				}
				if (characterAttachment.IsUsingBaseNodeOnly)
				{
					BlockyModelNode node = blockyModel.AllNodes[0].Clone();
					BlockyModelNode node2 = blockyModel.AllNodes[1].Clone();
					blockyModel = new BlockyModel(2);
					blockyModel.AddNode(ref node);
					blockyModel.AddNode(ref node2, 0);
				}
				blockyModel.GradientId = characterAttachment.GradientId;
				BlockyModel attachment = blockyModel;
				NodeNameManager characterNodeNameManager = characterPartStore.CharacterNodeNameManager;
				Point? uvOffset = value2;
				andCloneModel.Attach(attachment, characterNodeNameManager, null, uvOffset);
			}
			float startTime = CharacterRenderer?.GetSlotAnimationTime(0) ?? 0f;
			BlockyAnimation blockyAnimation = null;
			float startTime2 = 0f;
			if (CharacterRenderer?.GetSlotAnimation(1) != null)
			{
				startTime2 = CharacterRenderer.GetSlotAnimationTime(1);
				blockyAnimation = CharacterRenderer.GetSlotAnimation(1);
			}
			CharacterRenderer?.Dispose();
			CharacterRenderer = new ModelRenderer(andCloneModel, characterPartStore.AtlasSizes, _app.Engine.Graphics, 0u, selfManageNodeBuffer: true);
			CharacterRenderer.SetSlotAnimation(0, characterPartStore.IdleAnimation, isLooping: true, 1f, startTime);
			if (blockyAnimation != null)
			{
				CharacterRenderer.SetSlotAnimation(1, blockyAnimation, isLooping: false, 1f, startTime2);
			}
		}

		public bool StartDraggingIfApplicable(SDL_Event evt)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			Point value = _app.Engine.Window.TransformSDLToViewportCoords(evt.button.x, evt.button.y);
			if (evt.button.button != 1 || !Viewport.Contains(value))
			{
				return false;
			}
			_isMouseDragging = true;
			_dragStartMouseX = evt.button.x;
			_dragStartCharacterModelAngle = _characterModelAngle;
			return true;
		}

		public bool StopDraggingIfApplicable(SDL_Event evt)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			if (evt.button.button != 1 || !_isMouseDragging)
			{
				return false;
			}
			_isMouseDragging = false;
			return true;
		}

		public void StopDragging()
		{
			_isMouseDragging = false;
		}

		public bool DragIfApplicable(SDL_Event evt)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			if (!_isMouseDragging)
			{
				return false;
			}
			_characterModelAngle = _dragStartCharacterModelAngle + (float)(evt.motion.x - _dragStartMouseX) / 64f;
			return true;
		}

		public void SetRotation(float rotation = -(float)System.Math.PI / 8f)
		{
			_characterModelAngle = (_lerpCharacterModelAngle = rotation);
		}

		public void Draw(float deltaTime)
		{
			GLFunctions gL = _app.Engine.Graphics.GL;
			BlockyModelProgram blockyModelForwardProgram = _app.Engine.Graphics.GPUProgramStore.BlockyModelForwardProgram;
			blockyModelForwardProgram.AssertInUse();
			gL.AssertEnabled(GL.DEPTH_TEST);
			if (CharacterRenderer.GetSlotAnimation(1) != null && !CharacterRenderer.IsSlotPlayingAnimation(1))
			{
				CharacterRenderer.SetSlotAnimation(1, null, isLooping: true, 1f, 0f, 12f);
			}
			CharacterRenderer.AdvancePlayback(deltaTime * 60f);
			CharacterRenderer.UpdatePose();
			CharacterRenderer.SendDataToGPU();
			_lerpCharacterModelAngle = MathHelper.Lerp(_lerpCharacterModelAngle, _characterModelAngle, MathHelper.Min(1f, 10f * deltaTime));
			Matrix matrix = Matrix.CreateRotationY(_lerpCharacterModelAngle);
			Matrix matrix2 = Matrix.CreateScale(1f / 12f * Scale);
			Matrix matrix3 = matrix * matrix2 * Matrix.CreateTranslation(Translation);
			blockyModelForwardProgram.ViewProjectionMatrix.SetValue(ref _projectionMatrix);
			blockyModelForwardProgram.ModelMatrix.SetValue(ref matrix3);
			blockyModelForwardProgram.NodeBlock.SetBuffer(CharacterRenderer.NodeBuffer);
			CharacterRenderer.Draw();
		}
	}

	public enum MainMenuPage
	{
		Home,
		Servers,
		Minigames,
		Adventure,
		WorldOptions,
		MyAvatar,
		Settings,
		SharedSinglePlayer
	}

	public class World
	{
		public string Path;

		public string LastWriteTime;

		public WorldOptions Options;

		public bool HasPreviewImage;

		public string LastPreviewImageWriteTime;
	}

	public class WorldOptions
	{
		public string Name;

		public bool FlatWorld;

		public bool NpcSpawning;

		public GameMode GameMode;
	}

	public class RenderCharacterPartPreviewCommand
	{
		public CharacterPartId Id;

		public PlayerSkinProperty Property;

		public bool Selected;

		public ColorRgba? BackgroundColor;
	}

	public class AddCharacterOnScreenEvent
	{
		public Rectangle Viewport;

		public string Id;

		public float InitialModelAngle;

		public float Scale;
	}

	public enum ServerListTab
	{
		Internet,
		Recent,
		Favorites,
		Friends,
		Lan
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private const string MainMenuWWiseId = "MENU_INIT";

	private const int MusicFadeDuration = 5000;

	private readonly App _app;

	public SceneRenderer SceneRenderer;

	public PostEffectRenderer PostEffectRenderer;

	public GLTexture _defaultBackgroundTexture;

	public GLTexture _defaultBlurredBackgroundTexture;

	public GLTexture _characterCreatorBackgroundTexture;

	public QuadRenderer _backgroundRenderer;

	private int[] _mainMenuMusicFileIndices;

	private int _musicTrackIndex;

	private bool _musicIsFadingOut;

	private int _musicPlaybackId = -1;

	public readonly List<World> Worlds = new List<World>();

	private const int LanDiscoveryBroadcastPort = 5510;

	private const int LanDiscoveryPort = 5511;

	private static readonly byte[] LanDiscoveryReplyHeader = Encoding.ASCII.GetBytes("HYTALE_DISCOVER_REPLY");

	private static readonly byte[] LanDiscoveryRequestHeader = Encoding.ASCII.GetBytes("HYTALE_DISCOVER_REQUEST");

	private readonly object _newlyDiscoveredLanServersLock = new object();

	private readonly HashSet<Server> _newlyDiscoveredLanServers = new HashSet<Server>();

	private readonly object _lanDiscoverySocketLock = new object();

	private UdpClient _lanDiscoverySocket;

	private float _lanDiscoverTimer;

	private const int CharacterAssetPreviewWidth = 92;

	private const int CharacterAssetPreviewHeight = 149;

	private bool _useMSAAForAssetPreview = false;

	private RenderTarget _characterAssetPreviewRenderTarget;

	private RenderTarget _characterAssetFinalPreviewRenderTarget;

	private CharacterOnScreen _characterOnScreen;

	private Texture _characterAssetPreviewSelectionBackgroundTexture;

	private QuadRenderer _characterAssetPreviewBackgroundQuadRenderer;

	private readonly Random _characterRandom = new Random();

	private readonly DropOutStack<ClientPlayerSkin> _skinUndoStack = new DropOutStack<ClientPlayerSkin>(100);

	private readonly DropOutStack<ClientPlayerSkin> _skinRedoStack = new DropOutStack<ClientPlayerSkin>(100);

	private readonly Dictionary<string, CharacterOnScreen> CharactersOnScreen = new Dictionary<string, CharacterOnScreen>();

	private CancellationTokenSource _reloadCharacterAssetsCancelTokenSource;

	private CancellationTokenSource _serverFetchCancellationToken;

	private CancellationTokenSource _serverFetchDetailsCancellationToken;

	public MainMenuPage CurrentPage { get; private set; }

	public Server[] LanServers { get; private set; } = new Server[0];


	public ClientPlayerSkin EditedSkin { get; private set; }

	public ServerListTab ActiveServerListTab { get; private set; }

	public bool IsFetchingList { get; private set; }

	public AppMainMenu(App app)
	{
		_app = app;
		GatherWorldList();
	}

	internal void SetPageToReturnTo(MainMenuPage page)
	{
		Debug.Assert(_app.Stage != App.AppStage.MainMenu);
		CurrentPage = page;
	}

	private void InitializeMainMenuMusic()
	{
		if (_app.Engine.Audio.ResourceManager.WwiseEventIds.TryGetValue("MENU_INIT", out var value))
		{
			_musicPlaybackId = _app.Engine.Audio.PostEvent(value, AudioDevice.PlayerSoundObjectReference);
		}
		else
		{
			Logger.Warn("Could not load UI music: {0}", "MENU_INIT");
		}
	}

	public void StopMusic()
	{
		_app.Engine.Audio.ActionOnEvent(_musicPlaybackId, (AkActionOnEventType)0, 5000, (AkCurveInterpolation)0);
	}

	public unsafe void Open(MainMenuPage page)
	{
		if (_app.Stage != App.AppStage.MainMenu)
		{
			App.AppStage stage = _app.Stage;
			App.AppStage appStage = stage;
			if (appStage != App.AppStage.Startup && (uint)(appStage - 3) > 2u)
			{
				Debug.Assert(condition: false);
			}
			_app.SetSingleplayerWorldName(null);
			_app.SetStage(App.AppStage.MainMenu);
			Engine engine = _app.Engine;
			int width = engine.Window.Viewport.Width;
			int height = engine.Window.Viewport.Height;
			engine.Graphics.RTStore.Resize(width, height);
			engine.Profiling.Initialize(1);
			SceneRenderer = new SceneRenderer(engine.Graphics, engine.Profiling, width, height);
			PostEffectProgram mainMenuPostEffectProgram = engine.Graphics.GPUProgramStore.MainMenuPostEffectProgram;
			PostEffectRenderer = new PostEffectRenderer(engine.Graphics, engine.Profiling, mainMenuPostEffectProgram);
			_backgroundRenderer = new QuadRenderer(engine.Graphics, engine.Graphics.GPUProgramStore.BasicProgram.AttribPosition, engine.Graphics.GPUProgramStore.BasicProgram.AttribTexCoords);
			GLFunctions gL = engine.Graphics.GL;
			gL.Enable(GL.BLEND);
			gL.Enable(GL.CULL_FACE);
			Image image = new Image(File.ReadAllBytes(Path.Combine(Paths.GameData, "Backgrounds", _app.Interface.MainMenuBackgroundImagePath)));
			_defaultBackgroundTexture = gL.GenTexture();
			gL.BindTexture(GL.TEXTURE_2D, _defaultBackgroundTexture);
			gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, 9729);
			gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, 9729);
			gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, 33071);
			gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, 33071);
			fixed (byte* ptr = image.Pixels)
			{
				gL.TexImage2D(GL.TEXTURE_2D, 0, 6408, image.Width, image.Height, 0, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr);
			}
			Image image2 = new Image(File.ReadAllBytes(Path.Combine(Paths.GameData, "Backgrounds", _app.Interface.MainMenuBackgroundImagePath.Replace(".png", "Blurred.png"))));
			_defaultBlurredBackgroundTexture = gL.GenTexture();
			gL.BindTexture(GL.TEXTURE_2D, _defaultBlurredBackgroundTexture);
			gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, 9729);
			gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, 9729);
			gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, 33071);
			gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, 33071);
			fixed (byte* ptr2 = image2.Pixels)
			{
				gL.TexImage2D(GL.TEXTURE_2D, 0, 6408, image2.Width, image2.Height, 0, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr2);
			}
			Image image3 = new Image(File.ReadAllBytes(Path.Combine(Paths.GameData, "Backgrounds", "CharacterCreator.png")));
			_characterCreatorBackgroundTexture = gL.GenTexture();
			gL.BindTexture(GL.TEXTURE_2D, _characterCreatorBackgroundTexture);
			gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, 9729);
			gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, 9729);
			gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, 33071);
			gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, 33071);
			fixed (byte* ptr3 = image3.Pixels)
			{
				gL.TexImage2D(GL.TEXTURE_2D, 0, 6408, image3.Width, image3.Height, 0, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr3);
			}
			SetupCharacter();
			SetupCharacterAssetPreviews();
			SetupLanDiscovery();
			InitializeMainMenuMusic();
			_app.Interface.FadeIn();
		}
		CurrentPage = page;
		_app.Interface.DevToolsLayer.DevTools.ResetGameInfoState();
		_app.Interface.MainMenuView.OnPageChanged();
	}

	internal void CleanUp()
	{
		DisposeCharacter();
		DisposeCharacterAssetPreviews();
		DisposeLanDiscovery();
		_backgroundRenderer.Dispose();
		_backgroundRenderer = null;
		GLFunctions gL = _app.Engine.Graphics.GL;
		gL.DeleteTexture(_characterCreatorBackgroundTexture);
		gL.DeleteTexture(_defaultBlurredBackgroundTexture);
		gL.DeleteTexture(_defaultBackgroundTexture);
		_characterCreatorBackgroundTexture = GLTexture.None;
		_defaultBlurredBackgroundTexture = GLTexture.None;
		_defaultBackgroundTexture = GLTexture.None;
		SceneRenderer.Dispose();
		SceneRenderer = null;
		PostEffectRenderer.Dispose();
		PostEffectRenderer = null;
	}

	internal void OnUserInput(SDL_Event @event)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		if ((int)@event.type == 768 && Input.EventMatchesBinding(@event, _app.Settings.InputBindings.OpenAssetEditor) && _app.Interface.Desktop.FocusedElement == null)
		{
			OpenCosmeticEditor();
		}
		OnCharacterRotate(@event);
	}

	internal void OnNewFrame(float deltaTime)
	{
		UpdateLanDiscovery(deltaTime);
		_app.Interface.PrepareForDraw();
		Engine engine = _app.Engine;
		RenderTargetStore rTStore = engine.Graphics.RTStore;
		GLFunctions gL = engine.Graphics.GL;
		gL.Viewport(engine.Window.Viewport);
		int width = engine.Window.Viewport.Width;
		int height = engine.Window.Viewport.Height;
		rTStore.BeginFrame();
		SceneRenderer.BeginDraw();
		PostEffectRenderer.UseTemporalAA(enable: false);
		rTStore.SceneColor.Bind(clear: false, setupViewport: true);
		gL.ClearColor(0f, 0f, 0f, 1f);
		gL.Clear((GL)17664u);
		gL.Disable(GL.DEPTH_TEST);
		gL.Disable(GL.BLEND);
		BasicProgram basicProgram = engine.Graphics.GPUProgramStore.BasicProgram;
		gL.UseProgram(basicProgram);
		gL.BindTexture(GL.TEXTURE_2D, CurrentPage switch
		{
			MainMenuPage.Home => _defaultBackgroundTexture, 
			MainMenuPage.MyAvatar => _characterCreatorBackgroundTexture, 
			_ => _defaultBlurredBackgroundTexture, 
		});
		basicProgram.Opacity.SetValue(1f);
		basicProgram.Color.SetValue(engine.Graphics.WhiteColor);
		basicProgram.MVPMatrix.SetValue(ref engine.Graphics.ScreenMatrix);
		_backgroundRenderer.Draw();
		if (CurrentPage == MainMenuPage.Home || CurrentPage == MainMenuPage.MyAvatar)
		{
			gL.Enable(GL.DEPTH_TEST);
			gL.UseProgram(engine.Graphics.GPUProgramStore.BlockyModelForwardProgram);
			gL.ActiveTexture(GL.TEXTURE3);
			gL.BindTexture(GL.TEXTURE_2D, _app.CharacterPartStore.CharacterGradientAtlas.GLTexture);
			gL.ActiveTexture(GL.TEXTURE0);
			gL.BindTexture(GL.TEXTURE_2D, _app.CharacterPartStore.TextureAtlas.GLTexture);
			DrawCharacters(deltaTime);
			gL.Disable(GL.DEPTH_TEST);
		}
		rTStore.SceneColor.Unbind();
		gL.Viewport(engine.Window.Viewport);
		PostEffectRenderer.Draw(rTStore.SceneColor.GetTexture(RenderTarget.Target.Color0), GLTexture.None, width, height, 1f);
		gL.Enable(GL.BLEND);
		gL.BlendFunc(GL.ONE, GL.ONE_MINUS_SRC_ALPHA);
		_app.Interface.Draw();
		gL.BlendFunc(GL.SRC_ALPHA, GL.ONE_MINUS_SRC_ALPHA);
	}

	public bool TryUpdateSingleplayerWorldOptions(string path, WorldOptions options, out string error)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		string path2 = Path.Combine(Paths.Saves, path, "worlds", "default", "config.bson");
		try
		{
			JObject val = JObject.Parse(File.ReadAllText(path2));
			val["DisplayName"] = JToken.op_Implicit(options.Name);
			val["GameMode"] = JToken.op_Implicit(((object)(GameMode)(ref options.GameMode)).ToString());
			val["IsSpawningNPC"] = JToken.op_Implicit(options.NpcSpawning);
			string contents = JsonConvert.SerializeObject((object)val, (Formatting)1, new JsonSerializerSettings
			{
				FloatFormatHandling = (FloatFormatHandling)1
			});
			File.WriteAllText(path2, contents);
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
		Logger.Info("Updated options for world \"{0}\"", path);
		error = null;
		return true;
	}

	public bool TryDeleteSingleplayerWorld(string worldDirectoryName, out string error)
	{
		try
		{
			Logger.Info("Deleting singleplayer world \"{0}\"", worldDirectoryName);
			string path = Path.Combine(Paths.Saves, worldDirectoryName);
			if (!Path.GetFullPath(path).StartsWith(Path.GetFullPath(Path.Combine(Paths.Saves))))
			{
				Logger.Warn("Failed to delete world {0} path is outside of Saves directory!", worldDirectoryName);
				error = "Path is outsides of Saves directory";
				return false;
			}
			Directory.Delete(path, recursive: true);
			GatherWorldList();
			error = null;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Error<Exception>(ex);
			error = ex.Message;
			return false;
		}
	}

	public void OpenSingleplayerWorldFolder(string worldDirectoryName)
	{
		Process.Start(Path.Combine(Paths.Saves, worldDirectoryName));
	}

	public void GatherWorldList()
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		Worlds.Clear();
		if (Directory.Exists(Paths.Saves))
		{
			string[] directories = Directory.GetDirectories(Paths.Saves);
			foreach (string text in directories)
			{
				string text2 = text.Substring(Paths.Saves.Length + 1);
				string name = text2;
				GameMode result = (GameMode)0;
				bool npcSpawning = true;
				string path = Path.Combine(text, "worlds", "default", "config.bson");
				if (File.Exists(path))
				{
					JObject val = JObject.Parse(File.ReadAllText(path));
					if (val["DisplayName"] != null)
					{
						name = Extensions.Value<string>((IEnumerable<JToken>)val["DisplayName"]);
					}
					if (val["GameMode"] != null)
					{
						Enum.TryParse<GameMode>(Extensions.Value<string>((IEnumerable<JToken>)val["GameMode"]), out result);
					}
					if (val["IsSpawningNPC"] != null)
					{
						npcSpawning = Extensions.Value<bool>((IEnumerable<JToken>)val["IsSpawningNPC"]);
					}
				}
				List<World> worlds = Worlds;
				World world = new World();
				char directorySeparatorChar = Path.DirectorySeparatorChar;
				world.Path = text2 + directorySeparatorChar;
				world.Options = new WorldOptions
				{
					Name = name,
					GameMode = result,
					NpcSpawning = npcSpawning
				};
				world.HasPreviewImage = File.Exists(Path.Combine(Paths.Saves, text, "preview.png"));
				world.LastPreviewImageWriteTime = File.GetLastWriteTime(Path.Combine(Paths.Saves, text, "preview.png")).ToString("o");
				world.LastWriteTime = File.GetLastWriteTime(Path.Combine(Paths.Saves, text)).ToString("o");
				worlds.Add(world);
			}
		}
		Worlds.Sort((World a, World b) => string.Compare(b.LastWriteTime, a.LastWriteTime, StringComparison.Ordinal));
	}

	public void JoinSingleplayerWorld(string worldDirectoryName)
	{
		if (CanConnectToServer("join world " + worldDirectoryName, out var _))
		{
			Logger.Info("Connecting to singleplayer world \"{0}\"...", worldDirectoryName);
			_app.Interface.FadeOut(delegate
			{
				_app.GameLoading.Open(worldDirectoryName);
				_app.Interface.FadeIn();
			});
		}
	}

	public bool TryCreateSingleplayerWorld(WorldOptions options, out string error)
	{
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		if (!CanConnectToServer("create world " + options.Name, out error))
		{
			return false;
		}
		string path = new Regex("[+:\\/\\\\*?\"<>|]+").Replace(options.Name, string.Empty).Trim();
		string text = Paths.EnsureUniqueDirname(Path.Combine(Paths.Saves, path));
		Logger.Info("Creating new singleplayer world in \"{0}\"...", text);
		JObject val = new JObject();
		val["DisplayName"] = JToken.op_Implicit(options.Name);
		val["GameMode"] = JToken.op_Implicit(((object)(GameMode)(ref options.GameMode)).ToString());
		val["IsSpawningNPC"] = JToken.op_Implicit(options.NpcSpawning);
		if (options.FlatWorld)
		{
			val["WorldGen"] = JObject.Parse(File.ReadAllText(Path.Combine(Paths.GameData, "DefaultFlatWorldConfig.json")))["WorldGen"];
		}
		try
		{
			Directory.CreateDirectory(Path.Combine(text, "worlds", "default"));
			File.WriteAllText(Path.Combine(text, "worlds", "default", "config.bson"), ((object)val).ToString());
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to create world");
			error = ex.Message;
			return false;
		}
		JoinSingleplayerWorld(Path.GetFileName(text));
		return true;
	}

	public void SetupLanDiscovery()
	{
		lock (_lanDiscoverySocketLock)
		{
			_lanDiscoverySocket = new UdpClient
			{
				ExclusiveAddressUse = false,
				EnableBroadcast = true
			};
			_lanDiscoverySocket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
			_lanDiscoverySocket.Client.Bind(new IPEndPoint(IPAddress.Any, 5511));
			_lanDiscoverySocket.BeginReceive(LanDiscoveryReceive, null);
		}
		ProbeLanServers();
	}

	public void DisposeLanDiscovery()
	{
		lock (_lanDiscoverySocketLock)
		{
			_lanDiscoverySocket.Client.Close();
			_lanDiscoverySocket.Close();
			_lanDiscoverySocket = null;
		}
	}

	public void UpdateLanDiscovery(float elapsedTime)
	{
		_lanDiscoverTimer += elapsedTime;
		if (_lanDiscoverTimer < 5f)
		{
			return;
		}
		_lanDiscoverTimer = 0f;
		ProbeLanServers();
		lock (_newlyDiscoveredLanServersLock)
		{
			if (_newlyDiscoveredLanServers.RemoveWhere((Server details) => details.Updated.ElapsedMilliseconds >= 10000) > 0)
			{
				LanServers = _newlyDiscoveredLanServers.ToArray();
				if (ActiveServerListTab == ServerListTab.Lan)
				{
					_app.Interface.MainMenuView.ServersPage.BuildServerList();
				}
			}
		}
	}

	private void ProbeLanServers()
	{
		lock (_lanDiscoverySocketLock)
		{
			if (_lanDiscoverySocket == null)
			{
				return;
			}
			try
			{
				IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, 5510);
				_lanDiscoverySocket.Send(LanDiscoveryRequestHeader, LanDiscoveryRequestHeader.Length, endPoint);
			}
			catch (SocketException)
			{
			}
		}
	}

	private void LanDiscoveryReceive(IAsyncResult result)
	{
		lock (_lanDiscoverySocketLock)
		{
			if (_lanDiscoverySocket == null)
			{
				return;
			}
			try
			{
				IPEndPoint remoteEP = null;
				byte[] bytes = _lanDiscoverySocket.EndReceive(result, ref remoteEP);
				HandleLanDiscoveryData(bytes, remoteEP);
			}
			catch (ObjectDisposedException)
			{
			}
			catch (Exception ex2)
			{
				Logger.Error<Exception>(ex2);
			}
			try
			{
				_lanDiscoverySocket.BeginReceive(LanDiscoveryReceive, null);
			}
			catch (ObjectDisposedException)
			{
			}
		}
	}

	private void HandleLanDiscoveryData(byte[] bytes, IPEndPoint endPoint)
	{
		if (LanDiscoveryReplyHeader.Length > bytes.Length || LanDiscoveryReplyHeader.Where((byte t, int i) => bytes[i] != t).Any())
		{
			return;
		}
		byte b = bytes[LanDiscoveryReplyHeader.Length];
		if (b != 0)
		{
			Logger.Warn<byte, IPEndPoint>("Received LAN Discovery packet with incompatible version: {0} from {1}", b, endPoint);
			return;
		}
		using MemoryStream input = new MemoryStream(bytes, LanDiscoveryReplyHeader.Length + 1, bytes.Length - LanDiscoveryReplyHeader.Length - 1);
		using BinaryReader binaryReader = new BinaryReader(input);
		IPAddress iPAddress = new IPAddress(binaryReader.ReadBytes(binaryReader.ReadByte()));
		ushort num = binaryReader.ReadUInt16();
		string @string = Encoding.UTF8.GetString(binaryReader.ReadBytes(binaryReader.ReadUInt16()));
		uint onlinePlayers = binaryReader.ReadUInt32();
		uint maxPlayers = binaryReader.ReadUInt32();
		if (object.Equals(iPAddress, IPAddress.Any))
		{
			iPAddress = endPoint.Address;
		}
		Server server = new Server
		{
			IsLan = true,
			Host = $"{iPAddress}:{num}",
			Name = @string,
			MaxPlayers = (int)maxPlayers,
			OnlinePlayers = (int)onlinePlayers
		};
		lock (_newlyDiscoveredLanServersLock)
		{
			bool flag = _newlyDiscoveredLanServers.Add(server);
			bool flag2 = false;
			if (!flag)
			{
				foreach (Server newlyDiscoveredLanServer in _newlyDiscoveredLanServers)
				{
					if (!newlyDiscoveredLanServer.Equals(server))
					{
						continue;
					}
					if (newlyDiscoveredLanServer.MaxPlayers != server.MaxPlayers)
					{
						flag2 = true;
					}
					if (newlyDiscoveredLanServer.OnlinePlayers != server.OnlinePlayers)
					{
						flag2 = true;
					}
					newlyDiscoveredLanServer.MaxPlayers = server.MaxPlayers;
					newlyDiscoveredLanServer.OnlinePlayers = server.OnlinePlayers;
					newlyDiscoveredLanServer.Updated.Restart();
					break;
				}
			}
			if (!flag2 && !flag)
			{
				return;
			}
			Logger.Info<IPAddress, ushort>("Discovered LAN server: {0}:{1}", iPAddress, num);
			Server[] lanServers = _newlyDiscoveredLanServers.ToArray();
			_app.Engine.RunOnMainThread(_app.Engine, delegate
			{
				LanServers = lanServers;
				if (ActiveServerListTab == ServerListTab.Lan)
				{
					_app.Interface.MainMenuView.ServersPage.BuildServerList();
				}
			});
		}
	}

	public void QueueForSharedSinglePlayerWorld(Guid worldId)
	{
		if (CanConnectToServer($"queue for {worldId}", out var _) && ServicesConnected($"queue for {worldId}"))
		{
			_app.HytaleServices.JoinSharedSinglePlayerWorld(worldId);
		}
	}

	public void CreateSharedSinglePlayerWorld(string name)
	{
		if (ServicesConnected("create world " + name))
		{
			_app.HytaleServices.CreateSharedSinglePlayerWorld(name);
		}
	}

	private bool ServicesConnected(string attemptedAction)
	{
		if (_app.Interface.ServiceState != HytaleServices.ServiceState.Connected)
		{
			Logger.Warn("Tried to {0} but not authenticated with services!", attemptedAction);
			return false;
		}
		return true;
	}

	private void SetupCharacterAssetPreviews()
	{
		BasicProgram basicProgram = _app.Engine.Graphics.GPUProgramStore.BasicProgram;
		Image image = new Image(File.ReadAllBytes(Path.Combine(Paths.GameData, "Interface/MainMenu/MyAvatar/PartBackgroundSelected@2x.png")));
		_characterAssetPreviewSelectionBackgroundTexture = new Texture(Texture.TextureTypes.Texture2D);
		_characterAssetPreviewSelectionBackgroundTexture.CreateTexture2D(image.Width, image.Height, image.Pixels);
		_characterAssetPreviewBackgroundQuadRenderer = new QuadRenderer(_app.Engine.Graphics, basicProgram.AttribPosition, basicProgram.AttribTexCoords);
		int sampleCount = ((!_useMSAAForAssetPreview) ? 1 : 4);
		_characterAssetPreviewRenderTarget = new RenderTarget(92, 149, "_characterAssetPreviewRenderTarget");
		_characterAssetPreviewRenderTarget.AddTexture(RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8, GL.NEAREST, GL.NEAREST, GL.CLAMP_TO_EDGE, requestCompareModeForDepth: false, requestMipMapChain: false, sampleCount);
		_characterAssetPreviewRenderTarget.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR, GL.CLAMP_TO_EDGE, requestCompareModeForDepth: false, requestMipMapChain: false, sampleCount);
		_characterAssetPreviewRenderTarget.FinalizeSetup();
		_characterAssetFinalPreviewRenderTarget = new RenderTarget(92, 149, "_characterAssetFinalPreviewRenderTarget");
		_characterAssetFinalPreviewRenderTarget.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		_characterAssetFinalPreviewRenderTarget.FinalizeSetup();
		_characterOnScreen = new CharacterOnScreen(_app, new Rectangle(0, 0, 92, 149));
	}

	private void DisposeCharacterAssetPreviews()
	{
		_characterOnScreen.Dispose();
		_characterAssetFinalPreviewRenderTarget.Dispose();
		_characterAssetPreviewRenderTarget.Dispose();
		_characterAssetPreviewSelectionBackgroundTexture.Dispose();
		_characterAssetPreviewBackgroundQuadRenderer.Dispose();
	}

	public void RenderAssetPreviews(RenderCharacterPartPreviewCommand[] events)
	{
		GLFunctions gL = _app.Engine.Graphics.GL;
		gL.Enable(GL.DEPTH_TEST);
		gL.Disable(GL.BLEND);
		foreach (RenderCharacterPartPreviewCommand renderCharacterPartPreviewCommand in events)
		{
			if (renderCharacterPartPreviewCommand.BackgroundColor.HasValue)
			{
				ColorRgba value = renderCharacterPartPreviewCommand.BackgroundColor.Value;
				gL.ClearColor((float)(int)value.R / 255f, (float)(int)value.G / 255f, (float)(int)value.B / 255f, (float)(int)value.A / 255f);
			}
			else
			{
				gL.ClearColor(0.18431373f, 0.22745098f, 0.30980393f, 1f);
			}
			_characterAssetPreviewRenderTarget.Bind(clear: true, setupViewport: true);
			if (renderCharacterPartPreviewCommand.Selected)
			{
				gL.Disable(GL.DEPTH_TEST);
				BasicProgram basicProgram = _app.Engine.Graphics.GPUProgramStore.BasicProgram;
				gL.UseProgram(basicProgram);
				basicProgram.Opacity.SetValue(1f);
				gL.BindTexture(GL.TEXTURE_2D, _characterAssetPreviewSelectionBackgroundTexture.GLTexture);
				basicProgram.Color.SetValue(_app.Engine.Graphics.WhiteColor);
				_characterAssetPreviewBackgroundQuadRenderer.Draw();
				gL.Enable(GL.DEPTH_TEST);
			}
			ClientPlayerSkin clientPlayerSkin = new ClientPlayerSkin
			{
				BodyType = EditedSkin.BodyType,
				SkinTone = EditedSkin.SkinTone,
				Haircut = EditedSkin.Haircut,
				FacialHair = EditedSkin.FacialHair,
				Eyes = EditedSkin.Eyes,
				Face = EditedSkin.Face,
				Eyebrows = EditedSkin.Eyebrows
			};
			CharacterPartStore characterPartStore = _app.CharacterPartStore;
			switch (renderCharacterPartPreviewCommand.Property)
			{
			case PlayerSkinProperty.Haircut:
				clientPlayerSkin.Haircut = renderCharacterPartPreviewCommand.Id;
				_characterOnScreen.Translation = new Vector3(0f, -8.3f, -8f);
				break;
			case PlayerSkinProperty.FacialHair:
				clientPlayerSkin.FacialHair = renderCharacterPartPreviewCommand.Id;
				_characterOnScreen.Translation = new Vector3(0f, -6.7f, -8f);
				break;
			case PlayerSkinProperty.Eyes:
				clientPlayerSkin.Eyes = renderCharacterPartPreviewCommand.Id;
				_characterOnScreen.Translation = new Vector3(-0.2f, -8.3f, -6f);
				break;
			case PlayerSkinProperty.Face:
				clientPlayerSkin.Face = renderCharacterPartPreviewCommand.Id.PartId;
				_characterOnScreen.Translation = new Vector3(-0.2f, -8.3f, -6f);
				break;
			case PlayerSkinProperty.Eyebrows:
				clientPlayerSkin.Eyebrows = renderCharacterPartPreviewCommand.Id;
				_characterOnScreen.Translation = new Vector3(-0.2f, -8.3f, -6.5f);
				break;
			case PlayerSkinProperty.HeadAccessory:
				clientPlayerSkin.HeadAccessory = renderCharacterPartPreviewCommand.Id;
				_characterOnScreen.Translation = new Vector3(0f, -8.3f, -8f);
				break;
			case PlayerSkinProperty.FaceAccessory:
				clientPlayerSkin.FaceAccessory = renderCharacterPartPreviewCommand.Id;
				_characterOnScreen.Translation = new Vector3(0f, -8.3f, -8f);
				break;
			case PlayerSkinProperty.EarAccessory:
				clientPlayerSkin.EarAccessory = renderCharacterPartPreviewCommand.Id;
				_characterOnScreen.Translation = new Vector3(-1.2f, -7.9f, -3.6f);
				break;
			case PlayerSkinProperty.Overtop:
				clientPlayerSkin.Overtop = renderCharacterPartPreviewCommand.Id;
				_characterOnScreen.Translation = new Vector3(0f, -4.9f, -8f);
				break;
			case PlayerSkinProperty.Undertop:
				clientPlayerSkin.Undertop = renderCharacterPartPreviewCommand.Id;
				_characterOnScreen.Translation = new Vector3(0f, -4.9f, -8f);
				break;
			case PlayerSkinProperty.Gloves:
				clientPlayerSkin.Gloves = renderCharacterPartPreviewCommand.Id;
				_characterOnScreen.Translation = new Vector3(0f, -4.9f, -8f);
				break;
			case PlayerSkinProperty.Pants:
				clientPlayerSkin.Pants = renderCharacterPartPreviewCommand.Id;
				_characterOnScreen.Translation = new Vector3(0f, -2.5f, -8f);
				break;
			case PlayerSkinProperty.Overpants:
				clientPlayerSkin.Overpants = renderCharacterPartPreviewCommand.Id;
				_characterOnScreen.Translation = new Vector3(0f, -2.5f, -8f);
				break;
			case PlayerSkinProperty.Shoes:
				clientPlayerSkin.Shoes = renderCharacterPartPreviewCommand.Id;
				_characterOnScreen.Translation = new Vector3(0f, -2.2f, -8f);
				break;
			}
			gL.ActiveTexture(GL.TEXTURE3);
			gL.BindTexture(GL.TEXTURE_2D, characterPartStore.CharacterGradientAtlas.GLTexture);
			gL.ActiveTexture(GL.TEXTURE0);
			gL.BindTexture(GL.TEXTURE_2D, characterPartStore.TextureAtlas.GLTexture);
			gL.UseProgram(_app.Engine.Graphics.GPUProgramStore.BlockyModelForwardProgram);
			_characterOnScreen.SetRotation((float)((renderCharacterPartPreviewCommand.Property != PlayerSkinProperty.EarAccessory) ? 1 : (-1)) * (float)System.Math.PI / 8f);
			try
			{
				_characterOnScreen.InitializeRendering(clientPlayerSkin, characterPartStore);
			}
			catch
			{
				break;
			}
			_characterOnScreen.Draw(0f);
			_characterAssetPreviewRenderTarget.Unbind();
			if (!_useMSAAForAssetPreview)
			{
				PostEffectRenderer.Draw(_characterAssetPreviewRenderTarget.GetTexture(RenderTarget.Target.Color0), GLTexture.None, _characterAssetPreviewRenderTarget.Width, _characterAssetPreviewRenderTarget.Height, 1f, _characterAssetFinalPreviewRenderTarget);
			}
			else
			{
				_characterAssetPreviewRenderTarget.ResolveTo(_characterAssetFinalPreviewRenderTarget, GL.COLOR_ATTACHMENT0, GL.COLOR_ATTACHMENT0, GL.NEAREST, bindSource: false, rebindSourceAfter: false);
			}
			Texture texture = new Texture(Texture.TextureTypes.Texture2D);
			byte[] pixels = _characterAssetFinalPreviewRenderTarget.ReadPixels(1, GL.RGBA, _useMSAAForAssetPreview);
			texture.CreateTexture2D(_characterAssetFinalPreviewRenderTarget.Width, _characterAssetFinalPreviewRenderTarget.Height, pixels);
			_app.Interface.MainMenuView.MyAvatarPage.OnPreviewRendered(renderCharacterPartPreviewCommand.Id, texture);
		}
	}

	private string CharacterAssetPreview_BuildAssetId(string assetId, string colorId, string variantId)
	{
		if (variantId != null)
		{
			return assetId + "." + colorId + "." + variantId;
		}
		return assetId + "." + colorId;
	}

	private void SetupCharacter()
	{
		JObject meta;
		CharacterPartStore partStore;
		if (!_app.AuthManager.Settings.IsInsecure)
		{
			JToken obj = _app.AuthManager.Metadata["playerOptions"];
			meta = (JObject)(object)((obj is JObject) ? obj : null);
			if (meta != null)
			{
				Logger.Info<JObject>("Starting with meta {0}", meta);
				partStore = _app.CharacterPartStore;
				EditedSkin = new ClientPlayerSkin();
				JToken val = meta["bodyType"];
				if (val != null)
				{
					EditedSkin.BodyType = (CharacterBodyType)Enum.Parse(typeof(CharacterBodyType), (string)meta["bodyType"]);
				}
				else
				{
					EditedSkin.BodyType = ((_characterRandom.Next(2) == 0) ? CharacterBodyType.Masculine : CharacterBodyType.Feminine);
				}
				Dictionary<string, CharacterPartTintColor> gradients = partStore.GradientSets["Skin"].Gradients;
				if (gradients.ContainsKey((string)meta["skinTone"]))
				{
					EditedSkin.SkinTone = (string)meta["skinTone"];
				}
				else
				{
					EditedSkin.SkinTone = gradients.ElementAt(_characterRandom.Next(gradients.Count)).Key;
				}
				if (meta.ContainsKey("face") && partStore.TryGetCharacterPart(PlayerSkinProperty.Face, (string)meta["face"], out var characterPart))
				{
					EditedSkin.Face = characterPart.Id;
				}
				else
				{
					EditedSkin.Face = partStore.GetDefaultPartFor(EditedSkin.BodyType, partStore.Faces).Id;
				}
				EditedSkin.Eyes = GetCharacterPartId("eyes", PlayerSkinProperty.Eyes, partStore.GetDefaultPartIdFor(EditedSkin.BodyType, partStore.Eyes));
				EditedSkin.Haircut = GetCharacterPartId("haircut", PlayerSkinProperty.Haircut);
				EditedSkin.FacialHair = GetCharacterPartId("facialHair", PlayerSkinProperty.FacialHair);
				EditedSkin.Eyebrows = GetCharacterPartId("eyebrows", PlayerSkinProperty.Eyebrows);
				EditedSkin.Pants = GetCharacterPartId("pants", PlayerSkinProperty.Pants);
				EditedSkin.Overpants = GetCharacterPartId("overpants", PlayerSkinProperty.Overpants);
				EditedSkin.Undertop = GetCharacterPartId("undertop", PlayerSkinProperty.Undertop);
				EditedSkin.Overtop = GetCharacterPartId("overtop", PlayerSkinProperty.Overtop);
				EditedSkin.Shoes = GetCharacterPartId("shoes", PlayerSkinProperty.Shoes);
				EditedSkin.HeadAccessory = GetCharacterPartId("headAccessory", PlayerSkinProperty.HeadAccessory);
				EditedSkin.FaceAccessory = GetCharacterPartId("faceAccessory", PlayerSkinProperty.FaceAccessory);
				EditedSkin.EarAccessory = GetCharacterPartId("earAccessory", PlayerSkinProperty.EarAccessory);
				EditedSkin.SkinFeature = GetCharacterPartId("skinFeature", PlayerSkinProperty.SkinFeature);
				EditedSkin.Gloves = GetCharacterPartId("gloves", PlayerSkinProperty.Gloves);
				_app.SetPlayerSkin(new ClientPlayerSkin(EditedSkin));
				Logger.Info<ClientPlayerSkin>("Got live skin {0}", EditedSkin);
			}
		}
		if (EditedSkin == null)
		{
			EditedSkin = GetNakedCharacter(null);
			_app.SetPlayerSkin(new ClientPlayerSkin(EditedSkin));
		}
		CharacterPartId GetCharacterPartId(string jsonKey, PlayerSkinProperty property, CharacterPartId defaultValue = null)
		{
			if (meta.ContainsKey(jsonKey))
			{
				string text = (string)meta[jsonKey];
				if (text == null)
				{
					return defaultValue;
				}
				CharacterPartId characterPartId = CharacterPartId.FromString(text);
				if (!partStore.TryGetCharacterPart(property, characterPartId.PartId, out var characterPart2))
				{
					return defaultValue;
				}
				if (characterPartId.VariantId != null)
				{
					if (characterPart2.Variants == null || !characterPart2.Variants.TryGetValue(characterPartId.VariantId, out var value))
					{
						return defaultValue;
					}
					if (value.Textures != null && value.Textures.ContainsKey(characterPartId.ColorId))
					{
						return characterPartId;
					}
				}
				else if (characterPart2.Textures != null && characterPart2.Textures.ContainsKey(characterPartId.ColorId))
				{
					return characterPartId;
				}
				if (characterPart2.GradientSet != null && partStore.GradientSets.TryGetValue(characterPart2.GradientSet, out var value2) && value2.Gradients.ContainsKey(characterPartId.ColorId))
				{
					return characterPartId;
				}
			}
			return defaultValue;
		}
	}

	private void DisposeCharacter()
	{
		foreach (CharacterOnScreen value in CharactersOnScreen.Values)
		{
			value.Dispose();
		}
		CharactersOnScreen.Clear();
	}

	public bool HasUnsavedSkinChanges()
	{
		return !EditedSkin.Equals(_app.PlayerSkin);
	}

	public void SaveCharacter()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Expected O, but got Unknown
		Logger.Info("Saving character...");
		JObject val = new JObject();
		val.Add("bodyType", JToken.op_Implicit(EditedSkin.BodyType.ToString()));
		val.Add("skinTone", JToken.op_Implicit(EditedSkin.SkinTone));
		val.Add("face", JToken.op_Implicit(EditedSkin.Face));
		val.Add("haircut", JToken.op_Implicit(EditedSkin.Haircut?.ToString()));
		val.Add("facialHair", JToken.op_Implicit(EditedSkin.FacialHair?.ToString()));
		val.Add("eyebrows", JToken.op_Implicit(EditedSkin.Eyebrows?.ToString()));
		val.Add("eyes", JToken.op_Implicit(EditedSkin.Eyes?.ToString()));
		val.Add("pants", JToken.op_Implicit(EditedSkin.Pants?.ToString()));
		val.Add("overpants", JToken.op_Implicit(EditedSkin.Overpants?.ToString()));
		val.Add("undertop", JToken.op_Implicit(EditedSkin.Undertop?.ToString()));
		val.Add("overtop", JToken.op_Implicit(EditedSkin.Overtop?.ToString()));
		val.Add("shoes", JToken.op_Implicit(EditedSkin.Shoes?.ToString()));
		val.Add("headAccessory", JToken.op_Implicit(EditedSkin.HeadAccessory?.ToString()));
		val.Add("faceAccessory", JToken.op_Implicit(EditedSkin.FaceAccessory?.ToString()));
		val.Add("earAccessory", JToken.op_Implicit(EditedSkin.EarAccessory?.ToString()));
		val.Add("skinFeature", JToken.op_Implicit(EditedSkin.SkinFeature?.ToString()));
		val.Add("gloves", JToken.op_Implicit(EditedSkin.Gloves?.ToString()));
		JObject metadata = val;
		_app.SetPlayerSkin(new ClientPlayerSkin(EditedSkin));
		if (_app.AuthManager.Settings.IsInsecure)
		{
			return;
		}
		_app.HytaleServices.SetPlayerOptions(metadata, delegate(Exception exception)
		{
			if (exception != null)
			{
				_app.Engine.RunOnMainThread(_app.Engine, delegate
				{
					if (_app.Stage == App.AppStage.MainMenu && CurrentPage == MainMenuPage.MyAvatar)
					{
						_app.Interface.MainMenuView.MyAvatarPage.OnFailedToSync(exception);
					}
				}, allowCallFromMainThread: true);
			}
		});
	}

	public void MakeEditedSkinNaked()
	{
		UpdateEditedSkin(delegate
		{
			EditedSkin = GetNakedCharacter(EditedSkin);
		});
	}

	public void AddCharacterOnScreen(AddCharacterOnScreenEvent evt)
	{
		if (!CharactersOnScreen.TryGetValue(evt.Id, out var value))
		{
			CharacterOnScreen characterOnScreen2 = (CharactersOnScreen[evt.Id] = new CharacterOnScreen(_app, evt.Viewport, evt.InitialModelAngle, evt.Scale));
			value = characterOnScreen2;
		}
		else
		{
			value.Viewport = evt.Viewport;
			value.Scale = evt.Scale;
		}
		value.InitializeRendering(new ClientPlayerSkin(EditedSkin), _app.CharacterPartStore);
	}

	public void RemoveCharacterFromScreen(string id)
	{
		if (CharactersOnScreen.TryGetValue(id, out var value))
		{
			value.Dispose();
			CharactersOnScreen.Remove(id);
		}
	}

	public void ClearSkinEditHistory()
	{
		_skinUndoStack.Clear();
		_skinRedoStack.Clear();
	}

	public void CancelCharacter()
	{
		EditedSkin = new ClientPlayerSkin(_app.PlayerSkin);
		UpdateCharacterSkinsOnScreen();
	}

	public void PlayCharacterEmote(string emoteId)
	{
		CharacterPartStore characterPartStore = _app.CharacterPartStore;
		Emote emote = characterPartStore.Emotes.Find((Emote x) => x.Id == emoteId);
		if (emote != null)
		{
			BlockyAnimation blockyAnimation = new BlockyAnimation();
			BlockyAnimationInitializer.Parse(AssetManager.GetBuiltInAsset("Common/" + emote.Animation), _app.CharacterPartStore.CharacterNodeNameManager, ref blockyAnimation);
			CharactersOnScreen.First().Value.CharacterRenderer.SetSlotAnimation(1, blockyAnimation, isLooping: false, 1f, 0f, 12f);
		}
	}

	public void ReloadCharacterAssets()
	{
		_reloadCharacterAssetsCancelTokenSource?.Cancel();
		_reloadCharacterAssetsCancelTokenSource = new CancellationTokenSource();
		CancellationToken cancelToken = _reloadCharacterAssetsCancelTokenSource.Token;
		CharacterPartStore upcomingCharacterPartStore = new CharacterPartStore(_app.Engine.Graphics.GL);
		ThreadPool.QueueUserWorkItem(delegate
		{
			bool textureAtlasNeedsUpdate = true;
			upcomingCharacterPartStore.LoadAssets(new HashSet<string>(), ref textureAtlasNeedsUpdate, cancelToken);
			upcomingCharacterPartStore.PrepareGradientAtlas(out var upcomingHairGradientAtlasPixels);
			upcomingCharacterPartStore.LoadModelData(_app.Engine, new HashSet<string>(), textureAtlasNeedsUpdate);
			_app.Engine.RunOnMainThread(_app.Engine, delegate
			{
				if (!cancelToken.IsCancellationRequested)
				{
					_app.ReplaceCharacterPartStore(upcomingCharacterPartStore);
					_app.CharacterPartStore.BuildGradientTexture(upcomingHairGradientAtlasPixels);
					if (_app.Stage == App.AppStage.MainMenu && CurrentPage == MainMenuPage.MyAvatar)
					{
						UpdateCharacterSkinsOnScreen();
						_app.Interface.MainMenuView.MyAvatarPage.OnCharacterChanged();
						_app.Interface.MainMenuView.MyAvatarPage.OnAssetsReloaded();
					}
				}
			});
		});
	}

	public void UndoCharacterSkinChange()
	{
		ClientPlayerSkin clientPlayerSkin = _skinUndoStack.Pop();
		if (clientPlayerSkin != null)
		{
			_skinRedoStack.Push(new ClientPlayerSkin(EditedSkin));
			EditedSkin = clientPlayerSkin;
			UpdateCharacterSkinsOnScreen();
			_app.Interface.MainMenuView.MyAvatarPage.OnCharacterChanged();
			_app.Interface.MainMenuView.MyAvatarPage.OnSetCanUndoRedoSelection(_skinUndoStack.Count > 0, _skinRedoStack.Count > 0);
		}
	}

	public void RedoCharacterSkinChange()
	{
		ClientPlayerSkin clientPlayerSkin = _skinRedoStack.Pop();
		if (clientPlayerSkin != null)
		{
			_skinUndoStack.Push(new ClientPlayerSkin(EditedSkin));
			EditedSkin = clientPlayerSkin;
			UpdateCharacterSkinsOnScreen();
			_app.Interface.MainMenuView.MyAvatarPage.OnCharacterChanged();
			_app.Interface.MainMenuView.MyAvatarPage.OnSetCanUndoRedoSelection(_skinUndoStack.Count > 0, _skinRedoStack.Count > 0);
		}
	}

	public void RandomizeCharacter(HashSet<PlayerSkinProperty> lockedProperties)
	{
		CharacterPartStore partStore = _app.CharacterPartStore;
		UpdateEditedSkin(delegate
		{
			if (!lockedProperties.Contains(PlayerSkinProperty.BodyType))
			{
				EditedSkin.BodyType = ((_characterRandom.NextDouble() > 0.5) ? CharacterBodyType.Masculine : CharacterBodyType.Feminine);
			}
			if (!lockedProperties.Contains(PlayerSkinProperty.SkinTone))
			{
				CharacterPartGradientSet characterPartGradientSet = partStore.GradientSets["Skin"];
				EditedSkin.SkinTone = characterPartGradientSet.Gradients.ElementAt(_characterRandom.Next(characterPartGradientSet.Gradients.Count)).Key;
			}
			if (!lockedProperties.Contains(PlayerSkinProperty.Face))
			{
				EditedSkin.Face = GetRandomCharacterAsset(partStore.Faces, allowNull: false).PartId;
			}
			if (!lockedProperties.Contains(PlayerSkinProperty.Haircut))
			{
				EditedSkin.Haircut = GetRandomCharacterAsset(partStore.Haircuts);
			}
			if (!lockedProperties.Contains(PlayerSkinProperty.Pants))
			{
				EditedSkin.Pants = GetRandomCharacterAsset(partStore.Pants);
			}
			if (!lockedProperties.Contains(PlayerSkinProperty.Overpants))
			{
				EditedSkin.Overpants = GetRandomCharacterAsset(partStore.Overpants);
			}
			if (!lockedProperties.Contains(PlayerSkinProperty.Undertop))
			{
				EditedSkin.Undertop = GetRandomCharacterAsset(partStore.Undertops);
			}
			if (!lockedProperties.Contains(PlayerSkinProperty.Overtop))
			{
				EditedSkin.Overtop = GetRandomCharacterAsset(partStore.Overtops);
			}
			if (!lockedProperties.Contains(PlayerSkinProperty.Shoes))
			{
				EditedSkin.Shoes = GetRandomCharacterAsset(partStore.Shoes);
			}
			if (!lockedProperties.Contains(PlayerSkinProperty.Eyebrows))
			{
				EditedSkin.Eyebrows = GetRandomCharacterAsset(partStore.Eyebrows, allowNull: true, EditedSkin.Haircut?.ColorId, EditedSkin.FacialHair?.ColorId);
			}
			if (!lockedProperties.Contains(PlayerSkinProperty.Eyes))
			{
				EditedSkin.Eyes = GetRandomCharacterAsset(partStore.Eyes, allowNull: false);
			}
			if (!lockedProperties.Contains(PlayerSkinProperty.FacialHair))
			{
				if (_characterRandom.Next(10) > 4 && EditedSkin.BodyType == CharacterBodyType.Masculine)
				{
					EditedSkin.FacialHair = GetRandomCharacterAsset(partStore.FacialHair, allowNull: true, EditedSkin.Haircut?.ColorId, EditedSkin.Eyebrows?.ColorId);
				}
				else
				{
					EditedSkin.FacialHair = null;
				}
			}
			if (!lockedProperties.Contains(PlayerSkinProperty.HeadAccessory))
			{
				if (_characterRandom.Next(10) > 8)
				{
					EditedSkin.HeadAccessory = GetRandomCharacterAsset(partStore.HeadAccessory);
				}
				else
				{
					EditedSkin.HeadAccessory = null;
				}
			}
			if (!lockedProperties.Contains(PlayerSkinProperty.FaceAccessory))
			{
				if (_characterRandom.Next(10) > 8)
				{
					EditedSkin.FaceAccessory = GetRandomCharacterAsset(partStore.FaceAccessory);
				}
				else
				{
					EditedSkin.FaceAccessory = null;
				}
			}
			if (!lockedProperties.Contains(PlayerSkinProperty.EarAccessory))
			{
				if (_characterRandom.Next(10) > 8)
				{
					EditedSkin.EarAccessory = GetRandomCharacterAsset(partStore.EarAccessory);
				}
				else
				{
					EditedSkin.EarAccessory = null;
				}
			}
			if (!lockedProperties.Contains(PlayerSkinProperty.SkinFeature))
			{
				if (_characterRandom.Next(10) > 8)
				{
					EditedSkin.SkinFeature = GetRandomCharacterAsset(partStore.SkinFeatures);
				}
				else
				{
					EditedSkin.SkinFeature = null;
				}
			}
		});
	}

	public void SetCharacterAsset(PlayerSkinProperty property, CharacterPartId id, bool updateInterface = true)
	{
		CharacterPartStore partStore = _app.CharacterPartStore;
		UpdateEditedSkin(delegate
		{
			switch (property)
			{
			case PlayerSkinProperty.BodyType:
				EditedSkin.BodyType = (CharacterBodyType)Enum.Parse(typeof(CharacterBodyType), id.PartId);
				EditedSkin.Eyes = partStore.GetDefaultPartIdFor(EditedSkin.BodyType, partStore.Eyes);
				if (EditedSkin.Eyebrows != null)
				{
					CharacterPart defaultPartFor = partStore.GetDefaultPartFor(EditedSkin.BodyType, partStore.Eyebrows);
					List<string> colorOptions = _app.CharacterPartStore.GetColorOptions(defaultPartFor, EditedSkin.Eyebrows?.VariantId);
					string colorId = (colorOptions.Contains(EditedSkin.Eyebrows.ColorId) ? EditedSkin.Eyebrows.ColorId : colorOptions.First());
					EditedSkin.Eyebrows = new CharacterPartId(defaultPartFor.Id, EditedSkin.Eyebrows?.VariantId, colorId);
				}
				_app.Interface.MainMenuView.MyAvatarPage.OnCharacterChanged();
				break;
			case PlayerSkinProperty.SkinTone:
				EditedSkin.SkinTone = id.PartId;
				break;
			case PlayerSkinProperty.FacialHair:
				EditedSkin.FacialHair = id;
				break;
			case PlayerSkinProperty.Eyes:
				EditedSkin.Eyes = id;
				break;
			case PlayerSkinProperty.Face:
				EditedSkin.Face = id.PartId;
				break;
			case PlayerSkinProperty.Gloves:
				EditedSkin.Gloves = id;
				break;
			case PlayerSkinProperty.SkinFeature:
				EditedSkin.SkinFeature = id;
				break;
			case PlayerSkinProperty.Eyebrows:
				EditedSkin.Eyebrows = id;
				break;
			case PlayerSkinProperty.Haircut:
				EditedSkin.Haircut = id;
				break;
			case PlayerSkinProperty.Pants:
				EditedSkin.Pants = id;
				break;
			case PlayerSkinProperty.Overpants:
				EditedSkin.Overpants = id;
				break;
			case PlayerSkinProperty.Undertop:
				EditedSkin.Undertop = id;
				break;
			case PlayerSkinProperty.Overtop:
				EditedSkin.Overtop = id;
				break;
			case PlayerSkinProperty.Shoes:
				EditedSkin.Shoes = id;
				break;
			case PlayerSkinProperty.HeadAccessory:
				EditedSkin.HeadAccessory = id;
				break;
			case PlayerSkinProperty.FaceAccessory:
				EditedSkin.FaceAccessory = id;
				break;
			case PlayerSkinProperty.EarAccessory:
				EditedSkin.EarAccessory = id;
				break;
			}
		}, updateInterface);
	}

	public JObject GetSkinJson()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Expected O, but got Unknown
		CharacterPartStore partStore = _app.CharacterPartStore;
		JArray attachments = new JArray();
		JObject val = new JObject
		{
			["Name"] = JToken.op_Implicit("MyModel"),
			["Parent"] = JToken.op_Implicit("Player"),
			["Model"] = JToken.op_Implicit(partStore.GetBodyModelPath(EditedSkin.BodyType).Replace("Common/", string.Empty)),
			["Texture"] = JToken.op_Implicit((EditedSkin.BodyType == CharacterBodyType.Feminine) ? "Characters/Player_Textures/Feminine_Greyscale.png" : "Characters/Player_Textures/Masculine_Greyscale.png"),
			["GradientSet"] = JToken.op_Implicit(EditedSkin.SkinTone)
		};
		partStore.GetCharacterAttachments(EditedSkin).ForEach(delegate(CharacterAttachment attachment)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Expected O, but got Unknown
			JObject val2 = new JObject();
			val2.Add("Model", JToken.op_Implicit(attachment.Model.Replace("Common/", "")));
			val2.Add("Texture", JToken.op_Implicit(attachment.Texture.Replace("Common/", "")));
			JObject val3 = val2;
			if (attachment.GradientId != 0 && partStore.TryGetGradientByIndex(attachment.GradientId, out var gradientSetId, out var gradientId))
			{
				val3["GradientSet"] = JToken.op_Implicit(gradientSetId);
				val3["GradientId"] = JToken.op_Implicit(gradientId);
			}
			attachments.Add((JToken)(object)val3);
		});
		val["Attachments"] = (JToken)(object)attachments;
		return val;
	}

	private ClientPlayerSkin GetNakedCharacter(ClientPlayerSkin character)
	{
		if (character != null)
		{
			return new ClientPlayerSkin
			{
				BodyType = character.BodyType,
				SkinTone = character.SkinTone,
				Eyes = character.Eyes,
				Eyebrows = character.Eyebrows,
				Face = character.Face
			};
		}
		CharacterPartStore characterPartStore = _app.CharacterPartStore;
		CharacterBodyType bodyType = ((_characterRandom.NextDouble() > 0.5) ? CharacterBodyType.Masculine : CharacterBodyType.Feminine);
		Dictionary<string, CharacterPartTintColor> gradients = characterPartStore.GradientSets["Skin"].Gradients;
		return new ClientPlayerSkin
		{
			BodyType = bodyType,
			SkinTone = gradients.ElementAt(_characterRandom.Next(gradients.Count)).Key,
			Face = characterPartStore.Faces.ElementAt(_characterRandom.Next(characterPartStore.Faces.Count)).Id,
			Eyes = characterPartStore.GetDefaultPartIdFor(bodyType, characterPartStore.Eyes),
			Eyebrows = characterPartStore.GetDefaultPartIdFor(bodyType, characterPartStore.Eyebrows)
		};
	}

	private CharacterPartId GetRandomCharacterAsset<T>(List<T> assets, bool allowNull = true, string matchColor = null, string matchColor2 = null) where T : CharacterPart
	{
		int num = _characterRandom.Next(assets.Count + (allowNull ? 1 : 0));
		if (num == assets.Count)
		{
			return null;
		}
		T val = assets[num];
		string variantId = null;
		if (val.Variants != null)
		{
			variantId = val.Variants.Keys.ElementAt(_characterRandom.Next(val.Variants.Count));
		}
		List<string> colorOptions = _app.CharacterPartStore.GetColorOptions(val, variantId);
		string text = null;
		if (matchColor != null)
		{
			if (colorOptions.Contains(matchColor))
			{
				text = matchColor;
			}
			else if (matchColor2 != null && colorOptions.Contains(matchColor2))
			{
				text = matchColor2;
			}
		}
		if (text == null)
		{
			text = colorOptions.ElementAt(_characterRandom.Next(colorOptions.Count));
		}
		return new CharacterPartId(val.Id, variantId, text);
	}

	private void UpdateEditedSkin(Action setter, bool updateInterface = true)
	{
		ClientPlayerSkin clientPlayerSkin = new ClientPlayerSkin(EditedSkin);
		setter();
		if (!EditedSkin.Equals(clientPlayerSkin))
		{
			if (updateInterface)
			{
				_app.Interface.MainMenuView.MyAvatarPage.OnCharacterChanged();
			}
			UpdateCharacterSkinsOnScreen();
			if (_skinRedoStack.Count > 0)
			{
				_skinRedoStack.Clear();
			}
			ClientPlayerSkin clientPlayerSkin2 = _skinUndoStack.Peek();
			if (clientPlayerSkin2 == null || !clientPlayerSkin2.Equals(clientPlayerSkin))
			{
				_skinUndoStack.Push(clientPlayerSkin);
				_app.Interface.MainMenuView.MyAvatarPage.OnSetCanUndoRedoSelection(_skinUndoStack.Count > 0, _skinRedoStack.Count > 0);
			}
		}
	}

	public void UpdateCharacterSkinsOnScreen()
	{
		foreach (CharacterOnScreen value in CharactersOnScreen.Values)
		{
			value.InitializeRendering(EditedSkin, _app.CharacterPartStore);
		}
	}

	public void OpenAssetIdInCosmeticEditor(string assetType, string assetId)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		IpcClient ipc = _app.Ipc;
		JObject val = new JObject();
		val.Add("Cosmetics", JToken.op_Implicit(true));
		val.Add("AssetType", JToken.op_Implicit(assetType));
		val.Add("AssetId", JToken.op_Implicit(assetId));
		ipc.SendCommand("OpenEditor", val);
	}

	public void OpenCosmeticEditor()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		IpcClient ipc = _app.Ipc;
		JObject val = new JObject();
		val.Add("Cosmetics", JToken.op_Implicit(true));
		ipc.SendCommand("OpenEditor", val);
	}

	public void OnCharacterRotate(SDL_Event evt)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected I4, but got Unknown
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		if (CurrentPage != 0 && CurrentPage != MainMenuPage.MyAvatar)
		{
			return;
		}
		SDL_EventType type = evt.type;
		SDL_EventType val = type;
		switch (val - 1024)
		{
		case 1:
		{
			foreach (CharacterOnScreen value in CharactersOnScreen.Values)
			{
				if (value.StartDraggingIfApplicable(evt))
				{
					break;
				}
			}
			break;
		}
		case 2:
		{
			foreach (CharacterOnScreen value2 in CharactersOnScreen.Values)
			{
				if (value2.StopDraggingIfApplicable(evt))
				{
					break;
				}
			}
			break;
		}
		case 0:
		{
			foreach (CharacterOnScreen value3 in CharactersOnScreen.Values)
			{
				if (value3.DragIfApplicable(evt))
				{
					break;
				}
			}
			break;
		}
		}
	}

	public void ResetCharacters()
	{
		foreach (CharacterOnScreen value in CharactersOnScreen.Values)
		{
			value.SetRotation();
		}
	}

	private void DrawCharacters(float deltaTime)
	{
		GLFunctions gL = _app.Engine.Graphics.GL;
		Rectangle viewport = _app.Engine.Window.Viewport;
		foreach (CharacterOnScreen value in CharactersOnScreen.Values)
		{
			gL.Viewport(viewport.X + value.Viewport.X, viewport.Y + viewport.Height - (value.Viewport.Y + value.Viewport.Height), value.Viewport.Width, value.Viewport.Height);
			value.Draw(deltaTime);
		}
	}

	public void QueueForMinigame(string joinKey)
	{
		if (CanConnectToServer("queue for " + joinKey, out var _))
		{
			_app.HytaleServices.JoinGameQueue(joinKey);
		}
	}

	public bool CanConnectToServer(string attemptedAction, out string reason)
	{
		if (_app.HytaleServices.QueueTicket.Ticket != null)
		{
			Logger.Warn<string, string>("Tried to {0} but already queued for {1}", attemptedAction, _app.HytaleServices.QueueTicket.Ticket);
			reason = "Already queued for " + _app.HytaleServices.QueueTicket.Ticket + ".";
			return false;
		}
		reason = null;
		return true;
	}

	public void TryConnectToServer(Server server)
	{
		if (server == null)
		{
			Logger.Warn("Error whilst connecting to server because server object is null");
			return;
		}
		AddServerToRecentServers(server.UUID);
		if (!CanConnectToServer("connect to " + server.Host, out var _))
		{
			return;
		}
		Logger.Info<string, string>("Connecting to multiplayer server \"{0}\" at {1}", server.Name, server.Host);
		if (!HostnameHelper.TryParseHostname(server.Host, 5520, out var hostname, out var port, out var error))
		{
			Logger.Warn("Invalid address: {0}", error);
			return;
		}
		_app.Interface.FadeOut(delegate
		{
			_app.GameLoading.Open(hostname, port);
			_app.Interface.FadeIn();
		});
	}

	private CancellationToken GetNewFetchCancelToken()
	{
		_serverFetchCancellationToken?.Cancel();
		_serverFetchCancellationToken = new CancellationTokenSource();
		return _serverFetchCancellationToken.Token;
	}

	public void FetchAndShowPublicServers(string name = null, string[] tags = null)
	{
		CancelFetchServerDetails();
		ServersPage serversPage = _app.Interface.MainMenuView.ServersPage;
		if (ActiveServerListTab != 0)
		{
			ActiveServerListTab = ServerListTab.Internet;
			serversPage.OnActiveTabChanged(cleanTags: false);
		}
		else if (tags == null)
		{
			serversPage.ClearTags();
		}
		IsFetchingList = true;
		serversPage.BuildServerList();
		CancellationToken cancelToken = GetNewFetchCancelToken();
		HytaleServicesApiClient.FetchPublicServerQuery fetchPublicServerQuery = default(HytaleServicesApiClient.FetchPublicServerQuery);
		fetchPublicServerQuery.Name = name;
		fetchPublicServerQuery.Tags = tags;
		HytaleServicesApiClient.FetchPublicServerQuery query = fetchPublicServerQuery;
		_app.HytaleServicesApi.FetchPublicServers(query).ContinueWith(delegate(Task<Server[]> t)
		{
			if (t.IsFaulted)
			{
				Logger.Error((Exception)t.Exception, "Failed to fetch public servers from API");
			}
			_app.Engine.RunOnMainThread(_app.Engine, delegate
			{
				if (!cancelToken.IsCancellationRequested)
				{
					IsFetchingList = false;
					_app.Interface.MainMenuView.ServersPage.OnServersReceived(t.IsFaulted ? null : t.Result);
				}
			});
		});
	}

	public void FetchAndShowFavoriteServers()
	{
		CancelFetchServerDetails();
		ServersPage serversPage = _app.Interface.MainMenuView.ServersPage;
		if (ActiveServerListTab != ServerListTab.Favorites)
		{
			ActiveServerListTab = ServerListTab.Favorites;
			serversPage.OnActiveTabChanged();
		}
		IsFetchingList = true;
		serversPage.BuildServerList();
		CancellationToken cancelToken = GetNewFetchCancelToken();
		_app.HytaleServicesApi.FetchFavoriteServers(_app.AuthManager.Settings.Uuid).ContinueWith(delegate(Task<Server[]> t)
		{
			if (t.IsFaulted)
			{
				Logger.Error((Exception)t.Exception, "Failed to fetch public servers from API");
			}
			_app.Engine.RunOnMainThread(_app.Engine, delegate
			{
				if (!cancelToken.IsCancellationRequested)
				{
					IsFetchingList = false;
					_app.Interface.MainMenuView.ServersPage.OnServersReceived(t.IsFaulted ? null : t.Result);
				}
			});
		});
	}

	public void FetchAndShowRecentServers()
	{
		CancelFetchServerDetails();
		ServersPage serversPage = _app.Interface.MainMenuView.ServersPage;
		if (ActiveServerListTab != ServerListTab.Recent)
		{
			ActiveServerListTab = ServerListTab.Recent;
			serversPage.OnActiveTabChanged();
		}
		IsFetchingList = true;
		serversPage.BuildServerList();
		CancellationToken cancelToken = GetNewFetchCancelToken();
		_app.HytaleServicesApi.FetchRecentServers(_app.AuthManager.Settings.Uuid).ContinueWith(delegate(Task<Server[]> t)
		{
			if (t.IsFaulted)
			{
				Logger.Error((Exception)t.Exception, "Failed to fetch public servers from API");
			}
			_app.Engine.RunOnMainThread(_app.Engine, delegate
			{
				if (!cancelToken.IsCancellationRequested)
				{
					IsFetchingList = false;
					_app.Interface.MainMenuView.ServersPage.OnServersReceived(t.IsFaulted ? null : t.Result);
				}
			});
		});
	}

	public void AddServerToRecentServers(Guid serverUuid)
	{
		_app.HytaleServicesApi.AddServerToRecents(serverUuid, _app.AuthManager.Settings.Uuid).ContinueWith(delegate(Task t)
		{
			if (t.IsFaulted)
			{
				Logger.Error((Exception)t.Exception, "Failed to add server to recents");
			}
		});
	}

	public void CancelFetchServerDetails()
	{
		_serverFetchDetailsCancellationToken?.Cancel();
		_serverFetchDetailsCancellationToken = null;
	}

	public void FetchServerDetails(Guid serverUuid)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_serverFetchDetailsCancellationToken?.Cancel();
		_serverFetchDetailsCancellationToken = new CancellationTokenSource();
		CancellationToken token = _serverFetchCancellationToken.Token;
		_app.Interface.MainMenuView.ServersPage.SetSelectedServerDetails(null);
		Task.Run(delegate
		{
			Server server = null;
			try
			{
				server = _app.HytaleServicesApi.FetchServerDetails(serverUuid).GetAwaiter().GetResult();
				if (token.IsCancellationRequested)
				{
					return;
				}
				Server[] result = _app.HytaleServicesApi.FetchFavoriteServers(_app.AuthManager.Settings.Uuid).GetAwaiter().GetResult();
				Server[] array = result;
				foreach (Server server2 in array)
				{
					if (server2.UUID.Equals(server.UUID))
					{
						server.IsFavorite = true;
						break;
					}
				}
			}
			catch (Exception ex)
			{
				Logger logger = Logger;
				Guid guid = serverUuid;
				logger.Error(ex, "Failed to fetch details for server " + guid);
			}
			_app.Engine.RunOnMainThread(_app.Engine, delegate
			{
				if (!token.IsCancellationRequested)
				{
					_app.Interface.MainMenuView.ServersPage.SetSelectedServerDetails(server);
				}
			});
		});
	}

	public void AddServerToFavorites(Guid serverUuid)
	{
		_app.HytaleServicesApi.AddServerToFavorites(serverUuid, _app.AuthManager.Settings.Uuid).ContinueWith(delegate(Task t)
		{
			if (t.IsFaulted)
			{
				Logger.Error((Exception)t.Exception, "Failed to add server to favorites");
			}
			_app.Engine.RunOnMainThread(_app.Engine, delegate
			{
				if (t.IsFaulted)
				{
					_app.Interface.MainMenuView.ServersPage.OnFailedToToggleFavoriteServer(t.Exception.Message);
				}
			});
		});
	}

	public void RemoveServerFromFavorites(Guid serverUuid)
	{
		_app.HytaleServicesApi.RemoveServerFromFavorites(serverUuid, _app.AuthManager.Settings.Uuid).ContinueWith(delegate(Task t)
		{
			if (t.IsFaulted)
			{
				Logger.Error((Exception)t.Exception, "Failed to remove server from favorites");
			}
			_app.Engine.RunOnMainThread(_app.Engine, delegate
			{
				if (t.IsFaulted)
				{
					_app.Interface.MainMenuView.ServersPage.OnFailedToToggleFavoriteServer(t.Exception.Message);
				}
			});
		});
	}

	public void RebootServer(string connectionAddress)
	{
		if (!HostnameHelper.TryParseHostname(connectionAddress, 5520, out var host, out var port, out var error))
		{
			Logger.Warn("Failed to parse hostname: {0}", error);
			return;
		}
		ConnectionToServer connection = null;
		connection = new ConnectionToServer(_app.Engine, host, port, delegate(Exception exception)
		{
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Expected O, but got Unknown
			if (exception == null)
			{
				Logger.Info("Connected to server: {0}", connectionAddress);
				connection.SendPacketImmediate((ProtoPacket)new Connect("f4c63561b2d2f5120b4c81ad1b8544e396088277d88f650aea892b6f0cb113f", 1643968234458L, (ConnectionMode)5, Language.SystemLanguage));
				connection.Close();
			}
		}, delegate(Exception exception)
		{
			Logger.Info("Disconnected from server: {0}", connectionAddress);
			Logger.Error<Exception>(exception);
		});
	}

	public void ShowFriendsServers()
	{
		ServersPage serversPage = _app.Interface.MainMenuView.ServersPage;
		if (ActiveServerListTab != ServerListTab.Friends)
		{
			ActiveServerListTab = ServerListTab.Friends;
			serversPage.OnActiveTabChanged();
		}
		serversPage.OnServersReceived(null);
	}
}
