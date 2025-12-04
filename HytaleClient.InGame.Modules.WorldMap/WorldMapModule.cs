#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Hypixel.ProtoPlus;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using SDL2;

namespace HytaleClient.InGame.Modules.WorldMap;

internal class WorldMapModule : Module
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct MarkerDrawTask
	{
		public QuadRenderer Renderer;

		public Matrix MVPMatrix;

		public Vector3 Color;
	}

	public class ClientBiomeData
	{
		public int ZoneId;

		public string ZoneName;

		public string BiomeName;

		public byte[] Color;
	}

	public class MapMarker
	{
		public string Id;

		public string Name;

		public string MarkerImage;

		public float X;

		public float Y;

		public float Z;

		public float Yaw;

		public float Pitch;

		public float Roll;

		public float LerpX;

		public float LerpZ;

		public float LerpYaw;
	}

	public class Texture
	{
		public Rectangle Rectangle;

		public QuadRenderer QuadRenderer;

		private bool[] _opaquePixels;

		public Texture(Rectangle rectangle)
		{
			Rectangle = rectangle;
		}

		public void Init(GraphicsDevice graphics, HytaleClient.Graphics.Texture atlas)
		{
			QuadRenderer = new QuadRenderer(graphics, graphics.GPUProgramStore.BasicProgram.AttribPosition, graphics.GPUProgramStore.BasicProgram.AttribTexCoords);
			QuadRenderer.UpdateUVs((float)(Rectangle.Width + Rectangle.X) / (float)atlas.Width, (float)(Rectangle.Height + Rectangle.Y) / (float)atlas.Height, (float)Rectangle.X / (float)atlas.Width, (float)Rectangle.Y / (float)atlas.Height);
		}

		public void GenerateHitDetectionMap(byte[] pixels)
		{
			_opaquePixels = new bool[Rectangle.Width * Rectangle.Height];
			for (int i = 0; i < _opaquePixels.Length; i++)
			{
				_opaquePixels[i] = pixels[i * 4 + 3] == byte.MaxValue;
			}
		}

		public bool IsPointOpaque(int x, int y)
		{
			return _opaquePixels[y * Rectangle.Width + x];
		}
	}

	public enum MarkerSelectionType
	{
		None,
		LocalPlayer,
		ServerMarker,
		Coordinates
	}

	public struct MarkerSelection
	{
		private static readonly MarkerSelection _noSelection = new MarkerSelection
		{
			Type = MarkerSelectionType.None
		};

		public MarkerSelectionType Type;

		public MapMarker MapMarker;

		public Point Coordinates;

		public static MarkerSelection None => _noSelection;

		public bool Equals(MarkerSelection other)
		{
			return other.Type == Type && other.MapMarker == MapMarker && other.Coordinates == Coordinates;
		}
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private const string MapMaskTexture = "UI/WorldMap/MapMask.png";

	private const float HighlightAnimationWidth = 128f;

	private const float HighlightAnimationHeight = 128f;

	private const int HighlightAnimationFPS = 15;

	private const int HighlightAnimationLastFrame = 10;

	private const float DefaultScale = 16f;

	private const float MinScale = 2f;

	private const float MaxScale = 64f;

	private readonly Font _font;

	private readonly QuadRenderer _mapRenderer;

	private readonly GLTexture _maskTexture;

	private readonly HytaleClient.Graphics.Texture _textureAtlas;

	private readonly Vector3 _localPlayerColor = new Vector3(0f, 209f, 255f) / 255f;

	private readonly Vector3 _selectionColor = new Vector3(242f, 206f, 5f) / 255f;

	private readonly Vector3 _selectionHoverColor = new Vector3(255f, 225f, 59f) / 255f;

	private readonly Vector3 _hoverColor = new Vector3(182f, 215f, 255f) / 255f;

	private GLTexture _mapTexture;

	private GLTexture _mapTextureUpcoming;

	private Texture _playerMarkerTexture;

	private Texture _coordinateMarkerTexture;

	private Texture _highlightAnimationTexture;

	private Dictionary<string, Texture> _textures = new Dictionary<string, Texture>();

	private TextRenderer _hoveredMarkerNameRenderer;

	private readonly RenderTarget _renderTarget;

	private MarkerSelection _selectedMarker;

	private MarkerSelection _hoveredMarker;

	private MarkerSelection _contextMenuMarker;

	private Matrix _tempMatrix;

	private Matrix _projectionMatrix;

	private Matrix _mapMatrix;

	private Matrix _hoveredMarkerNameMatrix;

	private const int MarkerDrawTasksDefaultSize = 20;

	private const int MarkerDrawTasksGrowth = 5;

	private MarkerDrawTask[] _mapMarkerDrawTasks = new MarkerDrawTask[20];

	private int _mapMarkerDrawTaskCount;

	private readonly Dictionary<ushort, ClientBiomeData> _biomes = new Dictionary<ushort, ClientBiomeData>();

	private readonly Dictionary<string, MapMarker> _markers = new Dictionary<string, MapMarker>();

	private readonly Dictionary<long, Image> _images = new Dictionary<long, Image>();

	private float _scale = 16f;

	private bool _isMovingOffset;

	private float _offsetChunksX;

	private float _offsetChunksZ;

	private int _minChunkX = int.MaxValue;

	private int _minChunkZ = int.MaxValue;

	private int _maxChunkX = int.MinValue;

	private int _maxChunkZ = int.MinValue;

	private HashSet<long> _imagesToUpdate = new HashSet<long>();

	private HashSet<long> _imagesBeingUpdated = new HashSet<long>();

	private readonly List<long> _tempImagesKeys = new List<long>();

	private bool _mapTextureNeedsUpdate = false;

	private bool _mapTextureIsUpdating = false;

	private bool _mapTextureNeedsTransfer = false;

	private int _mapChunkImageWidth;

	private int _mapChunkImageHeight;

	private int _mapTextureChunkWidth;

	private int _mapTextureChunkHeight;

	private int _mapTextureMinChunkX;

	private int _mapTextureMinChunkZ;

	private int _mapTextureCenterChunkX;

	private int _mapTextureCenterChunkZ;

	private int _previousMouseX;

	private int _previousMouseY;

	private float _highlightAnimationFrame;

	private volatile ushort _hoveredBiomeId;

	private float _maxHeightShading = 2f;

	private float _maxBorderSize = 1.25f;

	private float _maxBorderShading = 3f;

	private float _maxBorderSaturation = 0.3f;

	private const float ImageIncreaseScaleSize = 1.5f;

	private const int ImageGridSpacing = 0;

	private const int ChannelCount = 4;

	private Image _image;

	private readonly Thread _worldMapThread;

	private readonly CancellationTokenSource _updaterThreadCancellationTokenSource;

	private readonly CancellationToken _updaterThreadCancellationToken;

	private readonly BlockingCollection<Action> _worldMapThreadActionQueue = new BlockingCollection<Action>();

	public bool MapNeedsDrawing { get; private set; }

	public bool IsWorldMapEnabled { get; private set; } = true;


	public bool AllowTeleportToCoordinates { get; private set; } = true;


	public bool AllowTeleportToMarkers { get; private set; } = true;


	private float _windowScale => (float)_gameInstance.Engine.Window.Viewport.Height / 1080f;

	public WorldMapModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		_font = _gameInstance.App.Fonts.DefaultFontFamily.RegularFont;
		BasicProgram basicProgram = _gameInstance.Engine.Graphics.GPUProgramStore.BasicProgram;
		_mapRenderer = new QuadRenderer(_gameInstance.Engine.Graphics, basicProgram.AttribPosition, basicProgram.AttribTexCoords);
		_textureAtlas = new HytaleClient.Graphics.Texture(HytaleClient.Graphics.Texture.TextureTypes.Texture2D);
		_textureAtlas.CreateTexture2D(2048, 32, null, 0, GL.LINEAR_MIPMAP_LINEAR, GL.LINEAR);
		GLFunctions gL = _gameInstance.Engine.Graphics.GL;
		_mapTexture = gL.GenTexture();
		gL.BindTexture(GL.TEXTURE_2D, _mapTexture);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, 33071);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, 33071);
		_maskTexture = gL.GenTexture();
		gL.BindTexture(GL.TEXTURE_2D, _maskTexture);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, 33071);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, 33071);
		int width = _gameInstance.Engine.Window.Viewport.Width;
		int height = _gameInstance.Engine.Window.Viewport.Height;
		_renderTarget = new RenderTarget(width, height, "WorldMap");
		_renderTarget.AddTexture(RenderTarget.Target.Depth, GL.DEPTH24_STENCIL8, GL.DEPTH_STENCIL, GL.UNSIGNED_INT_24_8, GL.NEAREST, GL.NEAREST);
		_renderTarget.AddTexture(RenderTarget.Target.Color0, GL.RGBA8, GL.RGBA, GL.UNSIGNED_BYTE, GL.LINEAR, GL.LINEAR);
		_renderTarget.FinalizeSetup();
		Resize(width, height);
		_updaterThreadCancellationTokenSource = new CancellationTokenSource();
		_updaterThreadCancellationToken = _updaterThreadCancellationTokenSource.Token;
		_worldMapThread = new Thread(RunWorldMapThread)
		{
			Name = "WorldMap",
			IsBackground = true
		};
		_worldMapThread.Start();
	}

	protected override void DoDispose()
	{
		_mapRenderer.Dispose();
		_textureAtlas.Dispose();
		_renderTarget.Dispose();
		_hoveredMarkerNameRenderer?.Dispose();
		_gameInstance.Engine.Graphics.GL.DeleteTexture(_mapTexture);
		_gameInstance.Engine.Graphics.GL.DeleteTexture(_maskTexture);
		foreach (Texture value in _textures.Values)
		{
			value.QuadRenderer.Dispose();
		}
		_updaterThreadCancellationTokenSource.Cancel();
		_worldMapThread.Join();
		_updaterThreadCancellationTokenSource.Dispose();
	}

	public void PrepareTextureAtlas(out Dictionary<string, Texture> upcomingTextures, out byte[][] upcomingAtlasPixelsPerLevel, CancellationToken cancellationToken)
	{
		upcomingTextures = new Dictionary<string, Texture>();
		List<Image> list = new List<Image>();
		Dictionary<Image, string> dictionary = new Dictionary<Image, string>();
		foreach (KeyValuePair<string, string> item in _gameInstance.HashesByServerAssetPath)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				upcomingAtlasPixelsPerLevel = null;
				return;
			}
			string key = item.Key;
			if (key.EndsWith(".png") && key.StartsWith("UI/WorldMap/"))
			{
				try
				{
					Image image = new Image(AssetManager.GetAssetUsingHash(item.Value));
					list.Add(image);
					dictionary[image] = key;
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "Failed to load worldmap texture: " + AssetManager.GetAssetLocalPathUsingHash(item.Value));
				}
			}
		}
		list.Sort(delegate(Image a, Image b)
		{
			int height = b.Height;
			return height.CompareTo(a.Height);
		});
		Dictionary<Image, Point> imageLocations;
		byte[] atlasPixels = Image.Pack(_textureAtlas.Width, list, out imageLocations, doPadding: false, cancellationToken);
		if (imageLocations == null)
		{
			upcomingAtlasPixelsPerLevel = null;
			return;
		}
		foreach (KeyValuePair<Image, Point> item2 in imageLocations)
		{
			Point value = item2.Value;
			Image key2 = item2.Key;
			string text = dictionary[key2];
			Texture texture = new Texture(new Rectangle(value.X, value.Y, key2.Width, key2.Height));
			upcomingTextures[text] = texture;
			if (text.StartsWith("UI/WorldMap/MapMarkers/"))
			{
				texture.GenerateHitDetectionMap(key2.Pixels);
			}
		}
		upcomingAtlasPixelsPerLevel = HytaleClient.Graphics.Texture.BuildMipmapPixels(atlasPixels, _textureAtlas.Width, _textureAtlas.MipmapLevelCount);
	}

	public void BuildTextureAtlas(Dictionary<string, Texture> textures, byte[][] atlasPixelsPerLevel)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		foreach (string key in _markers.Keys)
		{
			MapMarker mapMarker = _markers[key];
			if (!textures.ContainsKey("UI/WorldMap/MapMarkers/" + mapMarker.MarkerImage))
			{
				_gameInstance.App.DevTools.Error("World map marker type image '" + mapMarker.MarkerImage + "' was removed!");
			}
		}
		_textureAtlas.UpdateTexture2DMipMaps(atlasPixelsPerLevel);
		if (_textures != null)
		{
			foreach (Texture value in _textures.Values)
			{
				value.QuadRenderer.Dispose();
			}
		}
		_textures = textures;
		foreach (Texture value2 in textures.Values)
		{
			value2.Init(_gameInstance.Engine.Graphics, _textureAtlas);
		}
		if (!_textures.TryGetValue("UI/WorldMap/MapMarkers/Player.png", out _playerMarkerTexture))
		{
			_playerMarkerTexture = new Texture(default(Rectangle));
			_playerMarkerTexture.Init(_gameInstance.Engine.Graphics, _textureAtlas);
			Logger.Error("No player map marker texture has been provided (\"UI/WorldMap/MapMarkers/Player.png\").");
		}
		if (!_textures.TryGetValue("UI/WorldMap/MapMarkers/Coordinate.png", out _coordinateMarkerTexture))
		{
			_coordinateMarkerTexture = new Texture(default(Rectangle));
			_coordinateMarkerTexture.Init(_gameInstance.Engine.Graphics, _textureAtlas);
			Logger.Error("No coordinate map marker texture has been provided (\"UI/WorldMap/MapMarkers/Coordinate.png\").");
		}
		if (!_textures.TryGetValue("UI/WorldMap/LocationHighlightAnimation.png", out _highlightAnimationTexture))
		{
			_highlightAnimationTexture = new Texture(default(Rectangle));
			_highlightAnimationTexture.Init(_gameInstance.Engine.Graphics, _textureAtlas);
			Logger.Error("No location highlight animation texture has been provided (\"UI/WorldMap/LocationHighlightAnimation.png\").");
		}
		UpdateMaskTexture();
	}

	public void Resize(int width, int height)
	{
		_projectionMatrix = Matrix.CreateTranslation(0f, 0f, -1f) * Matrix.CreateOrthographicOffCenter((float)(-width) / 2f, (float)width / 2f, (float)(-height) / 2f, (float)height / 2f, 0.1f, 1000f);
		_renderTarget.Resize(width, height);
		ClampOffset();
	}

	public void SetVisible(bool visible)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		MapNeedsDrawing = visible;
		_contextMenuMarker = MarkerSelection.None;
		_hoveredMarker = MarkerSelection.None;
		_hoveredMarkerNameRenderer?.Dispose();
		_hoveredMarkerNameRenderer = null;
		_highlightAnimationFrame = 0f;
		if (visible)
		{
			CenterMapOnPlayer();
		}
		_gameInstance.Connection.SendPacket((ProtoPacket)new UpdateWorldMapVisible(MapNeedsDrawing));
	}

	private void CenterMapOnPlayer()
	{
		_offsetChunksX = _gameInstance.LocalPlayer.Position.X / 32f;
		_offsetChunksZ = _gameInstance.LocalPlayer.Position.Z / 32f;
		ClampOffset();
	}

	public bool TryGetBiomeAtPosition(Vector3 position, out ClientBiomeData biomeData)
	{
		biomeData = null;
		int num = (int)position.X;
		int num2 = (int)position.Z;
		if (_images.TryGetValue(ChunkHelper.IndexOfChunkColumn(num >> 5, num2 >> 5), out var value))
		{
			int num3 = num & 0x1F;
			int num4 = num2 & 0x1F;
			int x = (int)((float)value.Width * ((float)num3 / 32f));
			int z = (int)((float)value.Height * ((float)num4 / 32f));
			int num5 = IndexPixel(x, z, value.Width, value.Height);
			int chunkPixel = value.PixelToBiomeId[num5];
			ushort key = PixelToBiomeId(chunkPixel);
			return _biomes.TryGetValue(key, out biomeData);
		}
		return false;
	}

	public void UpdateMapSettings(Dictionary<short, BiomeData> biomes, bool isEnabled, bool allowTeleportToCoordinates, bool allowTeleportToMarkers)
	{
		Debug.Assert(ThreadHelper.IsOnThread(_worldMapThread));
		_gameInstance.Engine.RunOnMainThread(this, delegate
		{
			IsWorldMapEnabled = isEnabled;
			AllowTeleportToCoordinates = allowTeleportToCoordinates;
			AllowTeleportToMarkers = allowTeleportToMarkers;
			_gameInstance.App.Interface.InGameView.InputBindingsComponent.OnWorldMapSettingsUpdated();
		});
		_biomes.Clear();
		foreach (KeyValuePair<short, BiomeData> biome in biomes)
		{
			int biomeColor = biome.Value.BiomeColor;
			_biomes[(ushort)biome.Key] = new ClientBiomeData
			{
				ZoneId = biome.Value.ZoneId,
				ZoneName = biome.Value.ZoneName,
				BiomeName = biome.Value.BiomeName,
				Color = new byte[3]
				{
					(byte)((uint)(biomeColor >> 16) & 0xFFu),
					(byte)((uint)(biomeColor >> 8) & 0xFFu),
					(byte)((uint)biomeColor & 0xFFu)
				}
			};
		}
		_imagesToUpdate.UnionWith(_images.Keys);
		_mapTextureNeedsUpdate = true;
	}

	public void SetMapChunk(int chunkX, int chunkZ, Image image)
	{
		Debug.Assert(ThreadHelper.IsOnThread(_worldMapThread));
		long num = ChunkHelper.IndexOfChunkColumn(chunkX, chunkZ);
		if (image != null)
		{
			_images[num] = image;
		}
		else
		{
			_images.Remove(num);
		}
		_imagesToUpdate.Add(num);
		_imagesToUpdate.Add(ChunkHelper.IndexOfChunkColumn(chunkX - 1, chunkZ));
		_imagesToUpdate.Add(ChunkHelper.IndexOfChunkColumn(chunkX, chunkZ - 1));
		_mapTextureNeedsUpdate = true;
	}

	public void AddMapMarker(MapMarker mapMarker)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (!_textures.ContainsKey("UI/WorldMap/MapMarkers/" + mapMarker.MarkerImage))
		{
			_gameInstance.App.DevTools.Error("World map marker type image '" + mapMarker.MarkerImage + "' doesn't exist!");
		}
		if (_markers.TryGetValue(mapMarker.Id, out var value))
		{
			mapMarker.LerpX = value.LerpX;
			mapMarker.LerpZ = value.LerpZ;
			mapMarker.LerpYaw = value.LerpYaw;
			_markers[mapMarker.Id] = mapMarker;
			_gameInstance.App.Interface.InGameView.CompassComponent.OnWorldMapMarkerUpdated(mapMarker);
		}
		else
		{
			mapMarker.LerpX = mapMarker.X;
			mapMarker.LerpZ = mapMarker.Z;
			mapMarker.LerpYaw = mapMarker.Yaw;
			_markers[mapMarker.Id] = mapMarker;
			_gameInstance.App.Interface.InGameView.CompassComponent.OnWorldMapMarkerAdded(mapMarker);
		}
	}

	public void RemoveMapMarker(string[] ids)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		foreach (string key in ids)
		{
			_markers.TryGetValue(key, out var value);
			if (value == _selectedMarker.MapMarker)
			{
				_selectedMarker = MarkerSelection.None;
			}
			_markers.Remove(key);
			_gameInstance.App.Interface.InGameView.CompassComponent.OnWorldMapMarkerRemoved(value);
		}
	}

	public unsafe void UpdateMaskTexture()
	{
		if (_gameInstance.HashesByServerAssetPath.TryGetValue("UI/WorldMap/MapMask.png", out var value))
		{
			try
			{
				Image image = new Image(AssetManager.GetAssetUsingHash(value));
				GLFunctions gL = _gameInstance.Engine.Graphics.GL;
				gL.BindTexture(GL.TEXTURE_2D, _maskTexture);
				fixed (byte* ptr = image.Pixels)
				{
					gL.TexImage2D(GL.TEXTURE_2D, 0, 6408, image.Width, image.Height, 0, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr);
					return;
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to load worldmap mask texture: UI/WorldMap/MapMask.png");
				return;
			}
		}
		Logger.Error("Asset doesn't exist: UI/WorldMap/MapMask.png");
	}

	private unsafe void SendDataToGPU()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		Debug.Assert(_image.Width > 0 && _image.Height > 0);
		Debug.Assert(_mapTexture != GLTexture.None);
		GLFunctions gL = _gameInstance.Engine.Graphics.GL;
		gL.BindTexture(GL.TEXTURE_2D, _mapTexture);
		fixed (byte* ptr = _image.Pixels)
		{
			gL.TexImage2D(GL.TEXTURE_2D, 0, 6408, _image.Width, _image.Height, 0, GL.BGRA, GL.UNSIGNED_BYTE, (IntPtr)ptr);
		}
	}

	public void Update(float deltaTime)
	{
		bool mapNeedsDrawing = MapNeedsDrawing;
		if (_mapTextureNeedsTransfer && mapNeedsDrawing)
		{
			SendDataToGPU();
			_mapTextureNeedsTransfer = false;
		}
		if (_mapTextureNeedsUpdate && !_mapTextureIsUpdating && IsWorldMapEnabled)
		{
			_mapTextureNeedsUpdate = false;
			_mapTextureIsUpdating = true;
			RunOnWorldMapThread(delegate
			{
				HashSet<long> imagesBeingUpdated = _imagesBeingUpdated;
				_imagesBeingUpdated = _imagesToUpdate;
				_imagesToUpdate = imagesBeingUpdated;
				_imagesToUpdate.Clear();
				_tempImagesKeys.Clear();
				_tempImagesKeys.AddRange(_images.Keys);
				RunUpdateTextureTask(_imagesBeingUpdated, _tempImagesKeys);
			});
		}
		if (!MapNeedsDrawing)
		{
			return;
		}
		if (!_isMovingOffset)
		{
			ClampOffset(smooth: true);
		}
		HandleKeyNavigation(deltaTime);
		_highlightAnimationFrame += deltaTime * 15f;
		foreach (MapMarker value in _markers.Values)
		{
			value.LerpX = MathHelper.Lerp(value.LerpX, value.X, deltaTime);
			value.LerpZ = MathHelper.Lerp(value.LerpZ, value.Z, deltaTime);
			if (!float.IsNaN(value.Yaw))
			{
				value.LerpYaw = MathHelper.WrapAngle(MathHelper.LerpAngle(value.LerpYaw, value.Yaw, 2f * deltaTime));
			}
			else
			{
				value.LerpYaw = 0f;
			}
		}
	}

	public void PrepareMapForDraw()
	{
		if (!MapNeedsDrawing)
		{
			throw new Exception("PrepareMapForDraw called when it was not required. Please check with MapNeedsDrawing first before calling this.");
		}
		int num = _mapTextureMinChunkZ + _mapTextureChunkHeight;
		float windowScale = _windowScale;
		Matrix.CreateTranslation(((float)_mapTextureMinChunkX - _offsetChunksX) * _scale * windowScale, ((float)(-num) + _offsetChunksZ) * _scale * windowScale, 0f, out _tempMatrix);
		Matrix.Multiply(ref _tempMatrix, ref _projectionMatrix, out _mapMatrix);
		float xScale = (float)_mapTextureChunkWidth * _scale * windowScale;
		float yScale = (float)_mapTextureChunkHeight * _scale * windowScale;
		Matrix.CreateScale(xScale, yScale, 1f, out _tempMatrix);
		Matrix.Multiply(ref _tempMatrix, ref _mapMatrix, out _mapMatrix);
		int num2 = 2;
		num2 += _markers.Count;
		if (_selectedMarker.Type == MarkerSelectionType.Coordinates)
		{
			num2 += 2;
		}
		else if (_selectedMarker.Type == MarkerSelectionType.ServerMarker)
		{
			num2++;
		}
		if (_contextMenuMarker.Type == MarkerSelectionType.Coordinates && !_contextMenuMarker.Equals(_selectedMarker))
		{
			num2++;
		}
		if (num2 >= _mapMarkerDrawTasks.Length)
		{
			Array.Resize(ref _mapMarkerDrawTasks, num2 + 5);
		}
		_mapMarkerDrawTaskCount = 0;
		foreach (MapMarker value2 in _markers.Values)
		{
			if (_textures.TryGetValue("UI/WorldMap/MapMarkers/" + value2.MarkerImage, out var value))
			{
				bool flag = value2 == _selectedMarker.MapMarker;
				if (flag)
				{
					PrepareHighlightAnimationForDraw(_mapMarkerDrawTaskCount, value2.LerpX, value2.LerpZ);
					_mapMarkerDrawTaskCount++;
				}
				PrepareMarkerForDraw(_mapMarkerDrawTaskCount, value, value2.LerpX, value2.LerpZ, float.IsNaN(value2.LerpYaw) ? 0f : value2.LerpYaw, value2 == _hoveredMarker.MapMarker, flag);
				_mapMarkerDrawTaskCount++;
			}
		}
		if (_selectedMarker.Type == MarkerSelectionType.Coordinates)
		{
			PrepareHighlightAnimationForDraw(_mapMarkerDrawTaskCount, _selectedMarker.Coordinates.X, _selectedMarker.Coordinates.Y);
			_mapMarkerDrawTaskCount++;
			PrepareMarkerForDraw(_mapMarkerDrawTaskCount, _coordinateMarkerTexture, _selectedMarker.Coordinates.X, _selectedMarker.Coordinates.Y, 0f, _hoveredMarker.Type == MarkerSelectionType.Coordinates, selected: true);
			_mapMarkerDrawTaskCount++;
		}
		if (_contextMenuMarker.Type == MarkerSelectionType.Coordinates && !_contextMenuMarker.Equals(_selectedMarker))
		{
			PrepareMarkerForDraw(_mapMarkerDrawTaskCount, _coordinateMarkerTexture, _contextMenuMarker.Coordinates.X, _contextMenuMarker.Coordinates.Y, 0f, _hoveredMarker.Type == MarkerSelectionType.Coordinates);
			_mapMarkerDrawTaskCount++;
		}
		PrepareHighlightAnimationForDraw(_mapMarkerDrawTaskCount, _gameInstance.LocalPlayer.Position.X, _gameInstance.LocalPlayer.Position.Z);
		_mapMarkerDrawTaskCount++;
		PrepareMarkerForDraw(_mapMarkerDrawTaskCount, _playerMarkerTexture, _gameInstance.LocalPlayer.Position.X, _gameInstance.LocalPlayer.Position.Z, _gameInstance.LocalPlayer.LookOrientation.Yaw, _hoveredMarker.Type == MarkerSelectionType.LocalPlayer, _selectedMarker.Type == MarkerSelectionType.LocalPlayer, localPlayer: true);
		_mapMarkerDrawTaskCount++;
		if (_hoveredMarkerNameRenderer != null)
		{
			float num3 = 20f * windowScale;
			float scale = 14f / (float)_gameInstance.App.Fonts.DefaultFontFamily.RegularFont.BaseSize * windowScale;
			Point worldPosition = GetWorldPosition(_hoveredMarker);
			Matrix.CreateTranslation(0f - _hoveredMarkerNameRenderer.GetHorizontalOffset(TextRenderer.TextAlignment.Center), 0f - _hoveredMarkerNameRenderer.GetVerticalOffset(TextRenderer.TextVerticalAlignment.Middle), 0f, out _hoveredMarkerNameMatrix);
			Matrix.CreateScale(scale, out _tempMatrix);
			Matrix.Multiply(ref _hoveredMarkerNameMatrix, ref _tempMatrix, out _hoveredMarkerNameMatrix);
			Matrix.AddTranslation(ref _hoveredMarkerNameMatrix, ((float)worldPosition.X / 32f - _offsetChunksX) * _scale * windowScale, ((float)(-worldPosition.Y) / 32f + _offsetChunksZ) * _scale * windowScale - num3, 0f);
			Matrix.Multiply(ref _hoveredMarkerNameMatrix, ref _projectionMatrix, out _hoveredMarkerNameMatrix);
		}
	}

	private void PrepareHighlightAnimationForDraw(int drawTaskIndex, float blockX, float blockZ)
	{
		float windowScale = _windowScale;
		float scaledWidth = 128f * windowScale * 0.5f;
		float scaledHeight = 128f * windowScale * 0.5f;
		BuildMatrix(out _mapMarkerDrawTasks[drawTaskIndex].MVPMatrix, blockX, blockZ, scaledWidth, scaledHeight);
		_mapMarkerDrawTasks[drawTaskIndex].Color = _gameInstance.Engine.Graphics.WhiteColor;
		int num = (int)_highlightAnimationFrame % 10;
		float num2 = (float)_highlightAnimationTexture.Rectangle.Width / 128f;
		float num3 = (float)_highlightAnimationTexture.Rectangle.X + (float)num % num2 * 128f;
		float num4 = (float)_highlightAnimationTexture.Rectangle.Y + (float)(int)System.Math.Floor((float)num / num2) * 128f;
		_highlightAnimationTexture.QuadRenderer.UpdateUVs((num3 + 128f) / (float)_textureAtlas.Width, (num4 + 128f) / (float)_textureAtlas.Height, num3 / (float)_textureAtlas.Width, num4 / (float)_textureAtlas.Height);
		_mapMarkerDrawTasks[drawTaskIndex].Renderer = _highlightAnimationTexture.QuadRenderer;
	}

	private void PrepareMarkerForDraw(int drawTaskIndex, Texture marker, float blockX, float blockZ, float yaw = 0f, bool hover = false, bool selected = false, bool localPlayer = false)
	{
		float windowScale = _windowScale;
		float scaledWidth = (float)marker.Rectangle.Width * windowScale;
		float scaledHeight = (float)marker.Rectangle.Height * windowScale;
		BuildMatrix(out _mapMarkerDrawTasks[drawTaskIndex].MVPMatrix, blockX, blockZ, scaledWidth, scaledHeight, yaw);
		if (localPlayer)
		{
			_mapMarkerDrawTasks[drawTaskIndex].Color = _localPlayerColor;
		}
		else if (selected)
		{
			_mapMarkerDrawTasks[drawTaskIndex].Color = (hover ? _selectionHoverColor : _selectionColor);
		}
		else if (hover)
		{
			_mapMarkerDrawTasks[drawTaskIndex].Color = _hoverColor;
		}
		else
		{
			_mapMarkerDrawTasks[drawTaskIndex].Color = _gameInstance.Engine.Graphics.WhiteColor;
		}
		_mapMarkerDrawTasks[drawTaskIndex].Renderer = marker.QuadRenderer;
	}

	private void BuildMatrix(out Matrix matrix, float blockX, float blockZ, float scaledWidth, float scaledHeight, float yaw = 0f)
	{
		float windowScale = _windowScale;
		Matrix.CreateTranslation((blockX / 32f - _offsetChunksX) * _scale * windowScale, ((0f - blockZ) / 32f + _offsetChunksZ) * _scale * windowScale, 0f, out _tempMatrix);
		Matrix.Multiply(ref _tempMatrix, ref _projectionMatrix, out matrix);
		if (yaw != 0f)
		{
			Matrix.CreateRotationZ(yaw, out _tempMatrix);
			Matrix.Multiply(ref _tempMatrix, ref matrix, out matrix);
		}
		Matrix.CreateTranslation(0f - scaledWidth / 2f, 0f - scaledHeight / 2f, 0f, out _tempMatrix);
		Matrix.Multiply(ref _tempMatrix, ref matrix, out matrix);
		Matrix.CreateScale(scaledWidth, scaledHeight, 1f, out _tempMatrix);
		Matrix.Multiply(ref _tempMatrix, ref matrix, out matrix);
	}

	public void DrawMap()
	{
		if (!MapNeedsDrawing)
		{
			throw new Exception("DrawMap called when it was not required. Please check with MapNeedsDrawing first before calling this.");
		}
		GraphicsDevice graphics = _gameInstance.Engine.Graphics;
		GLFunctions gL = graphics.GL;
		BasicProgram basicProgram = graphics.GPUProgramStore.BasicProgram;
		basicProgram.AssertInUse();
		gL.BindTexture(GL.TEXTURE_2D, graphics.WhitePixelTexture.GLTexture);
		basicProgram.MVPMatrix.SetValue(ref graphics.ScreenMatrix);
		basicProgram.Color.SetValue(graphics.BlackColor);
		basicProgram.Opacity.SetValue(0.5f);
		graphics.ScreenQuadRenderer.Draw();
		basicProgram.Opacity.SetValue(1f);
		_renderTarget.Bind(clear: true, setupViewport: true);
		gL.BindTexture(GL.TEXTURE_2D, _mapTexture);
		basicProgram.MVPMatrix.SetValue(ref _mapMatrix);
		basicProgram.Color.SetValue(graphics.WhiteColor);
		_mapRenderer.Draw();
		gL.BindTexture(GL.TEXTURE_2D, _textureAtlas.GLTexture);
		for (int i = 0; i < _mapMarkerDrawTaskCount; i++)
		{
			basicProgram.MVPMatrix.SetValue(ref _mapMarkerDrawTasks[i].MVPMatrix);
			basicProgram.Color.SetValue(_mapMarkerDrawTasks[i].Color);
			_mapMarkerDrawTasks[i].Renderer.Draw();
		}
		if (_hoveredMarkerNameRenderer != null)
		{
			TextProgram textProgram = graphics.GPUProgramStore.TextProgram;
			gL.UseProgram(textProgram);
			gL.BindTexture(GL.TEXTURE_2D, _font.TextureAtlas.GLTexture);
			textProgram.FillThreshold.SetValue(0f);
			textProgram.FillBlurThreshold.SetValue(0.2f);
			textProgram.OutlineThreshold.SetValue(0f);
			textProgram.OutlineBlurThreshold.SetValue(0f);
			textProgram.OutlineOffset.SetValue(Vector2.Zero);
			textProgram.FogParams.SetValue(Vector4.Zero);
			textProgram.Opacity.SetValue(1f);
			textProgram.MVPMatrix.SetValue(ref _hoveredMarkerNameMatrix);
			_hoveredMarkerNameRenderer.Draw();
		}
		_renderTarget.Unbind();
		RenderTarget.BindHardwareFramebuffer();
		WorldMapProgram worldMapProgram = graphics.GPUProgramStore.WorldMapProgram;
		gL.UseProgram(worldMapProgram);
		gL.ActiveTexture(GL.TEXTURE1);
		gL.BindTexture(GL.TEXTURE_2D, _maskTexture);
		gL.ActiveTexture(GL.TEXTURE0);
		gL.BindTexture(GL.TEXTURE_2D, _renderTarget.GetTexture(RenderTarget.Target.Color0));
		worldMapProgram.MVPMatrix.SetValue(ref graphics.ScreenMatrix);
		graphics.ScreenQuadRenderer.Draw();
	}

	private void MoveOffset(float x, float y)
	{
		if (_contextMenuMarker.Type != 0)
		{
			HideContextMenu();
		}
		float windowScale = _windowScale;
		_offsetChunksX += (0f - x) / (_scale * windowScale);
		_offsetChunksZ += (0f - y) / (_scale * windowScale);
		ClampOffset();
	}

	private void Zoom(float zoom)
	{
		if (_contextMenuMarker.Type != 0)
		{
			HideContextMenu();
		}
		_scale += zoom;
		_scale = MathHelper.Clamp(_scale, 2f, 64f);
		ClampOffset();
	}

	private void ClampOffset(bool smooth = false)
	{
		if (_minChunkX >= _maxChunkX || _minChunkZ >= _maxChunkZ)
		{
			return;
		}
		float num = (_isMovingOffset ? (-32f) : 0f);
		float windowScale = _windowScale;
		float num2 = _gameInstance.LocalPlayer.Position.X / 32f;
		if (num2 < (float)_minChunkX || num2 > (float)_maxChunkX)
		{
			_offsetChunksX = num2;
		}
		else
		{
			int num3 = _maxChunkX - _minChunkX;
			float num4 = ((float)num3 + num) / (_scale * windowScale);
			if (smooth)
			{
				float value = MathHelper.Clamp(_offsetChunksX, (float)_minChunkX + num4, (float)_maxChunkX - num4);
				_offsetChunksX = MathHelper.Lerp(_offsetChunksX, value, 0.3f);
			}
			else
			{
				_offsetChunksX = MathHelper.Clamp(_offsetChunksX, (float)_minChunkX + num4, (float)_maxChunkX - num4);
			}
		}
		float num5 = _gameInstance.LocalPlayer.Position.Z / 32f;
		if (num5 < (float)_minChunkZ || num5 > (float)_maxChunkZ)
		{
			_offsetChunksZ = num5;
			return;
		}
		int num6 = _maxChunkZ - _minChunkZ;
		float num7 = ((float)num6 + num) / (_scale * _windowScale);
		if (smooth)
		{
			float value2 = MathHelper.Clamp(_offsetChunksZ, (float)_minChunkZ + num7, (float)_maxChunkZ - num7);
			_offsetChunksZ = MathHelper.Lerp(_offsetChunksZ, value2, 0.3f);
		}
		else
		{
			_offsetChunksZ = MathHelper.Clamp(_offsetChunksZ, (float)_minChunkZ + num7, (float)_maxChunkZ - num7);
		}
	}

	private void HideContextMenu()
	{
		_contextMenuMarker = MarkerSelection.None;
		_gameInstance.App.Interface.TriggerEvent("worldMap.hideContextMenu");
	}

	private Point GetWorldPosition(MarkerSelection marker)
	{
		return marker.Type switch
		{
			MarkerSelectionType.LocalPlayer => new Point((int)System.Math.Floor(_gameInstance.LocalPlayer.Position.X), (int)System.Math.Floor(_gameInstance.LocalPlayer.Position.Z)), 
			MarkerSelectionType.ServerMarker => new Point((int)System.Math.Floor(marker.MapMarker.X), (int)System.Math.Floor(marker.MapMarker.Z)), 
			MarkerSelectionType.Coordinates => marker.Coordinates, 
			_ => Point.Zero, 
		};
	}

	private Point ScreenToBlockPosition(int mouseX, int mouseY)
	{
		float windowScale = _windowScale;
		return new Point((int)((float)mouseX / (_scale * windowScale) * 32f + _offsetChunksX * 32f), (int)((float)mouseY / (_scale * windowScale) * 32f + _offsetChunksZ * 32f));
	}

	private bool IsMouseAtWorldPosition(int screenX, int screenY, int blockX, int blockZ, Texture texture, float yaw = 0f)
	{
		float windowScale = _windowScale;
		float num = ((float)blockX / 32f - _offsetChunksX) * _scale * windowScale;
		float num2 = ((float)blockZ / 32f - _offsetChunksZ) * _scale * windowScale;
		if (yaw != 0f)
		{
			MathHelper.RotateAroundPoint(ref screenX, ref screenY, yaw, (int)num, (int)num2);
		}
		float num3 = (float)texture.Rectangle.Width * windowScale;
		float num4 = (float)texture.Rectangle.Height * windowScale;
		Rectangle rectangle = new Rectangle((int)(num - num3 / 2f), (int)(num2 - num4 / 2f), (int)num3, (int)num4);
		if (!rectangle.Contains(screenX, screenY))
		{
			return false;
		}
		return texture.IsPointOpaque((int)((float)(screenX - rectangle.X) / windowScale), (int)((float)(screenY - rectangle.Y) / windowScale));
	}

	private MarkerSelection GetMarkerAtMousePosition(int screenX, int screenY)
	{
		if (IsMouseAtWorldPosition(screenX, screenY, (int)_gameInstance.LocalPlayer.Position.X, (int)_gameInstance.LocalPlayer.Position.Z, _playerMarkerTexture, _gameInstance.LocalPlayer.LookOrientation.Yaw))
		{
			MarkerSelection result = default(MarkerSelection);
			result.Type = MarkerSelectionType.LocalPlayer;
			return result;
		}
		if (_selectedMarker.Type == MarkerSelectionType.Coordinates && IsMouseAtWorldPosition(screenX, screenY, _selectedMarker.Coordinates.X, _selectedMarker.Coordinates.Y, _coordinateMarkerTexture))
		{
			return _selectedMarker;
		}
		MapMarker mapMarker = null;
		foreach (MapMarker value2 in _markers.Values)
		{
			if (_textures.TryGetValue("UI/WorldMap/MapMarkers/" + value2.MarkerImage, out var value) && IsMouseAtWorldPosition(screenX, screenY, (int)System.Math.Floor(value2.LerpX), (int)System.Math.Floor(value2.LerpZ), value, value2.LerpYaw))
			{
				mapMarker = value2;
			}
		}
		if (mapMarker != null)
		{
			MarkerSelection result = default(MarkerSelection);
			result.Type = MarkerSelectionType.ServerMarker;
			result.MapMarker = mapMarker;
			return result;
		}
		return MarkerSelection.None;
	}

	private void SetHighlightedBiome(ushort biomeId)
	{
		Debug.Assert(ThreadHelper.IsOnThread(_worldMapThread));
		if (_hoveredBiomeId == biomeId)
		{
			return;
		}
		_hoveredBiomeId = biomeId;
		if (biomeId != ushort.MaxValue && _biomes.TryGetValue(biomeId, out var biome))
		{
			_gameInstance.Engine.RunOnMainThread(this, delegate
			{
				_gameInstance.App.Interface.TriggerEvent("worldMap.setHighlightedBiome", biome.ZoneName, biome.BiomeName);
			});
		}
		else if (biomeId != ushort.MaxValue)
		{
			_gameInstance.Engine.RunOnMainThread(this, delegate
			{
				_gameInstance.App.Interface.TriggerEvent("worldMap.setHighlightedBiome");
			});
		}
		else
		{
			_gameInstance.Engine.RunOnMainThread(this, delegate
			{
				_gameInstance.App.Interface.TriggerEvent("worldMap.setHighlightedBiome", "Error: Unknown Zone!!", "Error: Unknown Biome!!");
			});
		}
	}

	public void OnSelectContextMarker()
	{
		if (_contextMenuMarker.Type != 0)
		{
			_selectedMarker = _contextMenuMarker;
			HideContextMenu();
		}
	}

	public void OnDeselectContextMarker()
	{
		if (_contextMenuMarker.Equals(_selectedMarker))
		{
			_selectedMarker = MarkerSelection.None;
			HideContextMenu();
		}
	}

	public void OnTeleportToContextMarker()
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		switch (_contextMenuMarker.Type)
		{
		case MarkerSelectionType.Coordinates:
			_gameInstance.Chat.SendCommand("tp", _contextMenuMarker.Coordinates.X, "_2", _contextMenuMarker.Coordinates.Y);
			break;
		case MarkerSelectionType.ServerMarker:
			_gameInstance.Connection.SendPacket((ProtoPacket)new TeleportToWorldMapMarker(_contextMenuMarker.MapMarker.Id));
			break;
		default:
			_gameInstance.Chat.Error($"Teleport is not supported for selected marker type {_contextMenuMarker.Type}");
			return;
		}
		HideContextMenu();
	}

	private void HandleKeyNavigation(float dt)
	{
		if (_gameInstance.Input.IsKeyHeld((SDL_Scancode)81))
		{
			MoveOffset(0f, dt * -100f);
		}
		else if (_gameInstance.Input.IsKeyHeld((SDL_Scancode)82))
		{
			MoveOffset(0f, dt * 100f);
		}
		if (_gameInstance.Input.IsKeyHeld((SDL_Scancode)80))
		{
			MoveOffset(dt * 100f, 0f);
		}
		else if (_gameInstance.Input.IsKeyHeld((SDL_Scancode)79))
		{
			MoveOffset(dt * -100f, 0f);
		}
	}

	public void OnUserInput(SDL_Event evt)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Invalid comparison between Unknown and I4
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected I4, but got Unknown
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Expected I4, but got Unknown
		//IL_0422: Unknown result type (might be due to invalid IL or missing references)
		//IL_0423: Unknown result type (might be due to invalid IL or missing references)
		//IL_042d: Unknown result type (might be due to invalid IL or missing references)
		//IL_042e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		SDL_EventType type = evt.type;
		SDL_EventType val = type;
		if ((int)val != 768)
		{
			switch (val - 1024)
			{
			case 3:
				if (evt.wheel.y != 0)
				{
					Zoom((evt.wheel.y < 0) ? (-1f) : 1f);
				}
				break;
			case 1:
				switch ((Input.MouseButton)evt.button.button)
				{
				case Input.MouseButton.SDL_BUTTON_LEFT:
				{
					_isMovingOffset = true;
					Point point3 = _gameInstance.Engine.Window.TransformSDLToViewportCoords(evt.button.x, evt.button.y);
					_previousMouseX = point3.X;
					_previousMouseY = point3.Y;
					break;
				}
				case Input.MouseButton.SDL_BUTTON_RIGHT:
				{
					Window window2 = _gameInstance.Engine.Window;
					Point point2 = window2.TransformSDLToViewportCoords(evt.button.x, evt.button.y);
					float num7 = (float)window2.Viewport.Width / 2f;
					float num8 = (float)window2.Viewport.Height / 2f;
					int num9 = (int)((float)point2.X - num7);
					int num10 = (int)((float)point2.Y - num8);
					MarkerSelection contextMenuMarker = GetMarkerAtMousePosition(num9, num10);
					if (contextMenuMarker.Type == MarkerSelectionType.None)
					{
						MarkerSelection markerSelection = default(MarkerSelection);
						markerSelection.Type = MarkerSelectionType.Coordinates;
						markerSelection.Coordinates = ScreenToBlockPosition(num9, num10);
						contextMenuMarker = markerSelection;
						_gameInstance.App.Interface.InGameView.MapPage.OnMarkerPlaced();
					}
					if (contextMenuMarker.Type != MarkerSelectionType.LocalPlayer)
					{
						_contextMenuMarker = contextMenuMarker;
						_gameInstance.App.Interface.InGameView.MapPage.OnShowContextMenu(_contextMenuMarker, _selectedMarker.Equals(_contextMenuMarker));
					}
					break;
				}
				}
				break;
			case 2:
				_isMovingOffset = false;
				break;
			case 0:
			{
				Window window = _gameInstance.Engine.Window;
				Point point = window.TransformSDLToViewportCoords(evt.motion.x, evt.motion.y);
				float num = (float)window.Viewport.Width / 2f;
				float num2 = (float)window.Viewport.Height / 2f;
				int num3 = (int)((float)point.X - num);
				int num4 = (int)((float)point.Y - num2);
				int num5 = point.X - _previousMouseX;
				int num6 = point.Y - _previousMouseY;
				if (_isMovingOffset && _contextMenuMarker.Type != 0)
				{
					if (System.Math.Abs(num5) > 2 || System.Math.Abs(num5) > 2)
					{
						MoveOffset(num5, num6);
					}
				}
				else if (_isMovingOffset)
				{
					MoveOffset(num5, num6);
				}
				else
				{
					Point blockPosition = ScreenToBlockPosition(num3, num4);
					RunOnWorldMapThread(delegate
					{
						if (_images.TryGetValue(ChunkHelper.IndexOfChunkColumn(blockPosition.X >> 5, blockPosition.Y >> 5), out var value))
						{
							int num11 = blockPosition.X & 0x1F;
							int num12 = blockPosition.Y & 0x1F;
							int x = (int)((float)value.Width * ((float)num11 / 32f));
							int z = (int)((float)value.Height * ((float)num12 / 32f));
							int num13 = IndexPixel(x, z, value.Width, value.Height);
							int chunkPixel = value.PixelToBiomeId[num13];
							ushort highlightedBiome = PixelToBiomeId(chunkPixel);
							SetHighlightedBiome(highlightedBiome);
						}
						else
						{
							SetHighlightedBiome(ushort.MaxValue);
						}
					});
				}
				MarkerSelection markerAtMousePosition = GetMarkerAtMousePosition(num3, num4);
				if (!_hoveredMarker.Equals(markerAtMousePosition))
				{
					string text = markerAtMousePosition.Type switch
					{
						MarkerSelectionType.ServerMarker => markerAtMousePosition.MapMarker.Name, 
						MarkerSelectionType.LocalPlayer => "You", 
						_ => null, 
					};
					if (text != null)
					{
						if (_hoveredMarkerNameRenderer != null)
						{
							_hoveredMarkerNameRenderer.Text = text;
						}
						else
						{
							_hoveredMarkerNameRenderer = new TextRenderer(_gameInstance.Engine.Graphics, _gameInstance.App.Fonts.DefaultFontFamily.RegularFont, text);
						}
					}
					else
					{
						_hoveredMarkerNameRenderer?.Dispose();
						_hoveredMarkerNameRenderer = null;
					}
				}
				_hoveredMarker = markerAtMousePosition;
				_previousMouseX = point.X;
				_previousMouseY = point.Y;
				break;
			}
			}
			return;
		}
		SDL_Keycode sym = evt.key.keysym.sym;
		SDL_Keycode val2 = sym;
		if ((int)val2 != 32)
		{
			switch (val2 - 43)
			{
			case 0:
				Zoom(0.4f);
				break;
			case 2:
				Zoom(-0.4f);
				break;
			case 6:
				_maxHeightShading -= 0.25f;
				RunOnWorldMapThread(delegate
				{
					Logger.Info("MaxHeightShading: {0}", _maxHeightShading);
					_imagesToUpdate.UnionWith(_images.Keys);
					_mapTextureNeedsUpdate = true;
				});
				break;
			case 7:
				_maxHeightShading += 0.25f;
				RunOnWorldMapThread(delegate
				{
					Logger.Info("MaxHeightShading: {0}", _maxHeightShading);
					_imagesToUpdate.UnionWith(_images.Keys);
					_mapTextureNeedsUpdate = true;
				});
				break;
			case 8:
				_maxBorderShading -= 0.25f;
				RunOnWorldMapThread(delegate
				{
					Logger.Info("MaxBorderShading: {0}", _maxBorderShading);
					_imagesToUpdate.UnionWith(_images.Keys);
					_mapTextureNeedsUpdate = true;
				});
				break;
			case 9:
				_maxBorderShading += 0.25f;
				RunOnWorldMapThread(delegate
				{
					Logger.Info("MaxBorderShading: {0}", _maxBorderShading);
					_imagesToUpdate.UnionWith(_images.Keys);
					_mapTextureNeedsUpdate = true;
				});
				break;
			case 10:
				_maxBorderSize -= 0.25f;
				RunOnWorldMapThread(delegate
				{
					Logger.Info("MaxBorderize: {0}", _maxBorderSize);
					_imagesToUpdate.UnionWith(_images.Keys);
					_mapTextureNeedsUpdate = true;
				});
				break;
			case 11:
				_maxBorderSize += 0.25f;
				RunOnWorldMapThread(delegate
				{
					Logger.Info("MaxBorderSize: {0}", _maxBorderSize);
					_imagesToUpdate.UnionWith(_images.Keys);
					_mapTextureNeedsUpdate = true;
				});
				break;
			case 12:
				_maxBorderSaturation -= 0.05f;
				RunOnWorldMapThread(delegate
				{
					Logger.Info("MaxBorderSaturation: {0}", _maxBorderSaturation);
					_imagesToUpdate.UnionWith(_images.Keys);
					_mapTextureNeedsUpdate = true;
				});
				break;
			case 13:
				_maxBorderSaturation += 0.05f;
				RunOnWorldMapThread(delegate
				{
					Logger.Info("MaxBorderSaturation: {0}", _maxBorderSaturation);
					_imagesToUpdate.UnionWith(_images.Keys);
					_mapTextureNeedsUpdate = true;
				});
				break;
			case 1:
			case 3:
			case 4:
			case 5:
				break;
			}
		}
		else
		{
			CenterMapOnPlayer();
		}
	}

	public void RunOnWorldMapThread(Action action)
	{
		Debug.Assert(!ThreadHelper.IsOnThread(_worldMapThread));
		_worldMapThreadActionQueue.Add(action, _updaterThreadCancellationToken);
	}

	private void RunWorldMapThread()
	{
		while (true)
		{
			CancellationToken updaterThreadCancellationToken = _updaterThreadCancellationToken;
			if (!updaterThreadCancellationToken.IsCancellationRequested)
			{
				Action action;
				try
				{
					action = _worldMapThreadActionQueue.Take(_updaterThreadCancellationToken);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				action();
				continue;
			}
			break;
		}
	}

	private void RunUpdateTextureTask(HashSet<long> imagesToUpdate, List<long> imagesKeys)
	{
		if (_images.Count == 0)
		{
			_mapTextureIsUpdating = false;
			return;
		}
		int minChunkX = int.MaxValue;
		int minChunkZ = int.MaxValue;
		int maxChunkX = int.MinValue;
		int maxChunkZ = int.MinValue;
		int chunkImageWidth = 0;
		int chunkImageHeight = 0;
		foreach (KeyValuePair<long, Image> image in _images)
		{
			int num = ChunkHelper.XOfChunkColumnIndex(image.Key);
			int num2 = ChunkHelper.ZOfChunkColumnIndex(image.Key);
			if (num < minChunkX)
			{
				minChunkX = num;
			}
			if (num2 < minChunkZ)
			{
				minChunkZ = num2;
			}
			if (num > maxChunkX)
			{
				maxChunkX = num;
			}
			if (num2 > maxChunkZ)
			{
				maxChunkZ = num2;
			}
			int width = image.Value.Width;
			int height = image.Value.Height;
			if (width > chunkImageWidth)
			{
				chunkImageWidth = width;
			}
			if (height > chunkImageHeight)
			{
				chunkImageHeight = height;
			}
		}
		int num3 = maxChunkX - minChunkX + 1;
		int num4 = maxChunkZ - minChunkZ + 1;
		if (num3 <= 0 && num4 <= 0)
		{
			Logger.Warn("No size!");
			_mapTextureIsUpdating = false;
			return;
		}
		int num5 = _mapTextureChunkWidth * _mapChunkImageWidth;
		int num6 = _mapTextureChunkHeight * _mapChunkImageHeight;
		int num7 = _mapTextureChunkWidth / 2 * _mapChunkImageWidth;
		int num8 = _mapTextureChunkHeight / 2 * _mapChunkImageHeight;
		int textureChunkWidth = _mapTextureChunkWidth;
		int textureChunkHeight = _mapTextureChunkHeight;
		int textureMinChunkX = _mapTextureMinChunkX;
		int textureMinChunkZ = _mapTextureMinChunkZ;
		int textureCenterChunkX = _mapTextureCenterChunkX;
		int textureCenterChunkZ = _mapTextureCenterChunkZ;
		if (minChunkX < _mapTextureMinChunkX || minChunkZ < _mapTextureMinChunkZ || maxChunkX + 1 >= _mapTextureMinChunkX + _mapTextureChunkWidth || maxChunkZ + 1 >= _mapTextureMinChunkZ + _mapTextureChunkHeight || _mapChunkImageWidth != chunkImageWidth || _mapChunkImageHeight != chunkImageHeight)
		{
			int num9 = (int)((float)num3 * 1.5f);
			int num10 = (int)((float)num4 * 1.5f);
			textureChunkWidth = num9 + num9 % 2;
			textureChunkHeight = num10 + num10 % 2;
			num5 = textureChunkWidth * chunkImageWidth;
			num6 = textureChunkHeight * chunkImageHeight;
			int maxTextureSize = _gameInstance.Engine.Graphics.MaxTextureSize;
			if (num5 > maxTextureSize || num6 > maxTextureSize)
			{
				Logger.Warn("Requested texture size is too big! WorldMapModule need to be designed w/ around that limitation!");
				_mapTextureIsUpdating = false;
				return;
			}
			num7 = textureChunkWidth / 2 * chunkImageWidth;
			num8 = textureChunkHeight / 2 * chunkImageHeight;
			textureCenterChunkX = minChunkX + num3 / 2;
			textureCenterChunkZ = minChunkZ + num4 / 2;
			textureMinChunkX = textureCenterChunkX - textureChunkWidth / 2;
			textureMinChunkZ = textureCenterChunkZ - textureChunkHeight / 2;
			_image = new Image(num5, num6, new byte[num5 * num6 * 4]);
			imagesToUpdate.UnionWith(imagesKeys);
		}
		foreach (long item in imagesToUpdate)
		{
			CancellationToken updaterThreadCancellationToken = _updaterThreadCancellationToken;
			if (updaterThreadCancellationToken.IsCancellationRequested)
			{
				return;
			}
			int num11 = ChunkHelper.XOfChunkColumnIndex(item);
			int num12 = ChunkHelper.ZOfChunkColumnIndex(item);
			if (!_images.TryGetValue(item, out var value))
			{
				continue;
			}
			float num13 = (float)chunkImageWidth / (float)value.Width;
			float num14 = (float)chunkImageHeight / (float)value.Height;
			int num15 = num7 - (textureCenterChunkX - num11) * chunkImageWidth;
			int num16 = num8 - (textureCenterChunkZ - num12) * chunkImageHeight;
			for (int i = 0; i < value.Width; i++)
			{
				for (int j = 0; j < value.Height; j++)
				{
					int num17 = IndexPixel(i, j, value.Width, value.Height);
					int chunkPixel = value.PixelToBiomeId[num17];
					ushort num18 = PixelToBiomeId(chunkPixel);
					if (num18 != ushort.MaxValue && _biomes.TryGetValue(num18, out var value2))
					{
						float num19 = PixelHeight(chunkPixel);
						float num20 = PixelBorder(chunkPixel);
						float num21 = 0f;
						Image value3;
						if (i + 1 < value.Width)
						{
							int num22 = IndexPixel(i + 1, j, value.Width, value.Height);
							float num23 = PixelHeight(value.PixelToBiomeId[num22]);
							num21 += num23 - num19;
						}
						else if (_images.TryGetValue(ChunkHelper.IndexOfChunkColumn(num11 + 1, num12), out value3))
						{
							int num24 = IndexPixel(0, j, value3.Width, value3.Height);
							float num25 = PixelHeight(value3.PixelToBiomeId[num24]);
							num21 += num25 - num19;
						}
						Image value4;
						if (j + 1 < value.Height)
						{
							int num26 = IndexPixel(i, j + 1, value.Width, value.Height);
							float num27 = PixelHeight(value.PixelToBiomeId[num26]);
							num21 += num27 - num19;
						}
						else if (_images.TryGetValue(ChunkHelper.IndexOfChunkColumn(num11, num12 + 1), out value4))
						{
							int num28 = IndexPixel(i, 0, value4.Width, value4.Height);
							float num29 = PixelHeight(value4.PixelToBiomeId[num28]);
							num21 += num29 - num19;
						}
						float num30 = num21 / 2f;
						float percent = num30 * _maxHeightShading;
						ColorRgba rgba = new ColorRgba(value2.Color[0], value2.Color[1], value2.Color[2]);
						rgba.Darken(percent);
						if (num20 < 100f)
						{
							ColorHsla colorHsla = ColorHsla.FromRgba(rgba);
							colorHsla.Saturate((100f - num20) * _maxBorderSaturation / 100f);
							colorHsla.ToRgb(out rgba.R, out rgba.G, out rgba.B);
							rgba.Darken((100f - num20) / (0f - _maxBorderShading));
						}
						for (int k = 0; (float)k < num13; k++)
						{
							for (int l = 0; (float)l < num14; l++)
							{
								int num31 = i + num15;
								int num32 = j + num16;
								int num33 = num32 * _image.Width + num31;
								_image.Pixels[num33 * 4] = rgba.B;
								_image.Pixels[num33 * 4 + 1] = rgba.G;
								_image.Pixels[num33 * 4 + 2] = rgba.R;
								_image.Pixels[num33 * 4 + 3] = byte.MaxValue;
							}
						}
						continue;
					}
					for (int m = 0; (float)m < num13; m++)
					{
						for (int n = 0; (float)n < num14; n++)
						{
							int num34 = i + num15;
							int num35 = j + num16;
							int num36 = num35 * _image.Width + num34;
							_image.Pixels[num36 * 4] = 0;
							_image.Pixels[num36 * 4 + 1] = 0;
							_image.Pixels[num36 * 4 + 2] = 0;
							_image.Pixels[num36 * 4 + 3] = byte.MaxValue;
						}
					}
				}
			}
		}
		_gameInstance.Engine.RunOnMainThread(this, delegate
		{
			_mapTextureNeedsTransfer = true;
			_minChunkX = minChunkX;
			_minChunkZ = minChunkZ;
			_maxChunkX = maxChunkX;
			_maxChunkZ = maxChunkZ;
			_mapChunkImageWidth = chunkImageWidth;
			_mapChunkImageHeight = chunkImageHeight;
			_mapTextureChunkWidth = textureChunkWidth;
			_mapTextureChunkHeight = textureChunkHeight;
			_mapTextureMinChunkX = textureMinChunkX;
			_mapTextureMinChunkZ = textureMinChunkZ;
			_mapTextureCenterChunkX = textureCenterChunkX;
			_mapTextureCenterChunkZ = textureCenterChunkZ;
			_mapTextureIsUpdating = false;
		}, allowCallFromMainThread: false, distributed: true);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ushort PixelToBiomeId(int chunkPixel)
	{
		return (ushort)(chunkPixel >> 16);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private float PixelBorder(int chunkPixel)
	{
		int num = (chunkPixel >> 10) & 0x3F;
		float value = (float)num * (_maxBorderSize / 63f);
		return MathHelper.Min(value, 1f) * 100f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private float PixelHeight(int chunkPixel)
	{
		return (float)(chunkPixel & 0x3FF) / 255f * 100f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int IndexPixel(int x, int z, int width, int height)
	{
		return z * width + x;
	}
}
