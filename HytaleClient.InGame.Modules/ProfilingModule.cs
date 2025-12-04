using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using HytaleClient.Core;
using HytaleClient.Data.Map;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Batcher2D;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.Graphics.Programs;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.InGame.Modules;

internal class ProfilingModule : Module
{
	private enum Keys
	{
		Branch,
		Revision,
		Vendor,
		Renderer,
		Version,
		Vram,
		FPS,
		Resolution,
		DrawCallsCountAndTriangles,
		ShadowDrawCallsCountAndTriangles,
		AtlasSizes,
		ViewDistance,
		ChunkColumns,
		Entities,
		LoadedParticles,
		LoadedTrails,
		LoadedImmersiveScreens,
		SentPacketLength,
		ReceivedPacketLength,
		AssetCount,
		AudioPlaybacksActive,
		HeightmapAndTint,
		Light,
		Biome,
		Environment,
		Weather,
		Music,
		SoundEffect,
		ChunkPosition,
		FeetPosition,
		Collision,
		Orientation,
		TargetBlock,
		LastMoveForce,
		WishDirection,
		Disposables
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct Stat
	{
		public string Text;

		public Vector2 Position;

		public UInt32Color Color;
	}

	private const float MarginSize = 10f;

	private const float TextHeight = 12f;

	private const int MeasurableColumnWidthCpu = 9;

	private const int MeasurableColumnWidthCpuMax = 12;

	private const int MeasurableColumnWidthGpu = 9;

	private const int MeasurableColumnWidthDraws = 6;

	private const int MeasurableColumnWidthTriangles = 10;

	public bool IsDetailedProfilingEnabled = false;

	public bool IsCPUOnlyRenderingProfilesEnabled = false;

	public bool IsPartialRenderingProfilesEnabled = false;

	public Matrix _graphOrthographicProjectionMatrix;

	public Matrix _batcherOrthographicProjectionMatrix;

	private int _width;

	private int _height;

	private int _accumulatedReceivedPacketLength;

	private int _accumulatedSentPacketLength;

	private UInt32Color _backgroundColor = UInt32Color.FromRGBA(0, 0, 0, 125);

	private Font _font;

	private float _largestMeasureNameSize;

	private float _measurableTableWidth;

	private static readonly int _defaultLength = Enum.GetNames(typeof(Keys)).Length;

	public int _statsDrawCount = 0;

	private Stat[] _textStats = new Stat[_defaultLength];

	private Batcher2D _batcher2d;

	private uint _previousMusicSoundIndex = 0u;

	private uint _previousSoundEffectIndex = 0u;

	private readonly Process _process;

	private const int GraphHeight = 150;

	private const int HistoryDuration = 350;

	private const float FpsGraphMaxValue = 66.666664f;

	private const float NetworkGraphMaxValue = 400f;

	private const float GarbageCollectionMaxValue = 500f;

	private Graph.DataSet _fpsData = new Graph.DataSet(350);

	private Graph.DataSet _cpuData = new Graph.DataSet(350);

	private Graph.DataSet _networkDataSent = new Graph.DataSet(350);

	private Graph.DataSet _networkDataReceived = new Graph.DataSet(350);

	private Graph.DataSet _garbageCollectionData = new Graph.DataSet(350);

	private Graph.DataSet _processMemoryData = new Graph.DataSet(350);

	private Graph _fpsGraph;

	private Graph _cpuGraph;

	private Graph _networkSentGraph;

	private Graph _networkReceivedGraph;

	private Graph _garbageCollectionGraph;

	private Graph _processMemoryGraph;

	private float _currentScale;

	public bool IsVisible { get; private set; } = false;


	public float AccumulatedFrameTime { get; private set; }

	public int AccumulatedFrames { get; private set; }

	public float MeanFrameDuration { get; private set; }

	public int DrawnTriangles { get; private set; }

	public int DrawCallsCount { get; private set; }

	public int TotalSentPacketLength { get; private set; } = 0;


	public int TotalReceivedPacketLength { get; private set; } = 0;


	public float LastAccumulatedSentPacketLength { get; private set; }

	public float LastAccumulatedReceivedPacketLength { get; private set; }

	public void SetDrawCallStats(int drawCallCount, int drawnTriangles)
	{
		DrawCallsCount = drawCallCount;
		DrawnTriangles = drawnTriangles;
	}

	public ProfilingModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		GraphicsDevice graphics = _gameInstance.Engine.Graphics;
		_font = _gameInstance.App.Fonts.MonospaceFontFamily.RegularFont;
		_batcher2d = new Batcher2D(graphics);
		_textStats[0].Text = "Branch:   " + BuildInfo.BranchName;
		_textStats[1].Text = "Revision: " + BuildInfo.RevisionId;
		string text = graphics.GPUInfo.Vendor.ToString();
		string renderer = graphics.GPUInfo.Renderer;
		string version = graphics.GPUInfo.Version;
		int availableNow = graphics.VideoMemory.AvailableNow;
		int capacity = graphics.VideoMemory.Capacity;
		int num = capacity - availableNow;
		_textStats[2].Text = "Vendor:   " + text;
		_textStats[3].Text = "Renderer: " + renderer;
		_textStats[4].Text = "Version: " + version;
		_textStats[5].Text = $"VRAM: {num / 1024} / {capacity / 1024} MB";
		_textStats[6].Text = "";
		_textStats[17].Text = "";
		_textStats[18].Text = "";
		_textStats[8].Text = "";
		_textStats[9].Text = "";
		_textStats[12].Text = "";
		for (int i = 0; i < _textStats.Length; i++)
		{
			_textStats[i].Color = UInt32Color.White;
		}
		_textStats[27].Text = "Sound Effect: None";
		_textStats[26].Text = "Music: None";
		_statsDrawCount = _textStats.Length;
		InitializeGraphs(_font);
		_process = Process.GetCurrentProcess();
	}

	protected override void DoDispose()
	{
		DisposeGraphs();
		_batcher2d.Dispose();
	}

	public override void Initialize()
	{
		if (OptionsHelper.AutoProfiling)
		{
			IsVisible = true;
			_gameInstance.ExecuteCommand(".profiling on all");
		}
	}

	public void SetupDetailedMeasures()
	{
		Engine engine = _gameInstance.Engine;
		int num = 0;
		string text = "";
		Array.Resize(ref _textStats, _textStats.Length + (engine.Profiling.MeasureCount + 1) * 2);
		_textStats[_defaultLength].Text = "Measure name";
		_textStats[_defaultLength].Color = UInt32Color.White;
		for (int i = 0; i < engine.Profiling.MeasureCount; i++)
		{
			int num2 = _defaultLength + i + 1;
			string text2 = i.ToString().PadLeft(2) + ". " + engine.Profiling.GetMeasureName(i);
			_textStats[num2].Text = text2;
			_textStats[num2].Color = ((i % 2 == 0) ? UInt32Color.FromRGBA(192, 192, 192, byte.MaxValue) : UInt32Color.White);
			if (text2.Length > num)
			{
				num = text2.Length;
				text = text2;
			}
		}
		text += " ";
		_largestMeasureNameSize = _font.CalculateTextWidth(text) * 12f / (float)_font.BaseSize;
		string text3 = "CPU Avg".PadLeft(9) + "(Max)".PadLeft(12) + "GPU Avg".PadLeft(9) + "Draws".PadLeft(6) + "Triangles".PadLeft(10);
		_textStats[_defaultLength + engine.Profiling.MeasureCount + 1].Text = text3;
		_textStats[_defaultLength + engine.Profiling.MeasureCount + 1].Color = UInt32Color.White;
		for (int j = 0; j < engine.Profiling.MeasureCount; j++)
		{
			int num3 = _defaultLength + engine.Profiling.MeasureCount + 1 + j + 1;
			_textStats[num3].Text = "";
			_textStats[num3].Color = ((j % 2 == 0) ? UInt32Color.FromRGBA(192, 192, 192, byte.MaxValue) : UInt32Color.White);
		}
		_measurableTableWidth = _font.CalculateTextWidth(text3) * 12f / (float)_font.BaseSize;
		Resize(_gameInstance.Engine.Window.Viewport.Width, _gameInstance.Engine.Window.Viewport.Height);
	}

	public void Resize(int width, int height)
	{
		_width = width;
		_height = height;
		float viewportScale = _gameInstance.Engine.Window.ViewportScale;
		float num = 12f * viewportScale;
		float num2 = 10f * viewportScale;
		float num3 = 2f * num2;
		float num4 = (float)_width - num3;
		float num5 = num2;
		for (int i = 0; i < _defaultLength; i++)
		{
			_textStats[i].Position.X = num3;
			_textStats[i].Position.Y = num5;
			num5 += num;
		}
		float num6 = num2 + num4 - _measurableTableWidth * viewportScale - num3 - _largestMeasureNameSize * viewportScale;
		num5 = num2;
		for (int j = _defaultLength; j < _defaultLength + _gameInstance.Engine.Profiling.MeasureCount + 1; j++)
		{
			_textStats[j].Position.X = num6;
			_textStats[j].Position.Y = num5;
			num5 += num;
		}
		num6 += _largestMeasureNameSize * viewportScale;
		num5 = num2;
		for (int k = _defaultLength + _gameInstance.Engine.Profiling.MeasureCount + 1; k < _defaultLength + _gameInstance.Engine.Profiling.MeasureCount + 1 + _gameInstance.Engine.Profiling.MeasureCount + 1; k++)
		{
			_textStats[k].Position.X = num6;
			_textStats[k].Position.Y = num5;
			num5 += num;
		}
		UpdateOrthographicProjectionMatrix();
		UpdateGraphWindowScale();
	}

	private void UpdateOrthographicProjectionMatrix()
	{
		Matrix.CreateOrthographicOffCenter(0f, _width, 0f, _height, 0.1f, 1000f, out _graphOrthographicProjectionMatrix);
		Matrix.CreateOrthographicOffCenter(0f, _width, _height, 0f, 0.1f, 1000f, out _batcherOrthographicProjectionMatrix);
	}

	[Obsolete]
	public override void Tick()
	{
		if (_gameInstance.Input.ConsumeBinding(_gameInstance.App.Settings.InputBindings.ToggleProfiling))
		{
			IsVisible = !IsVisible;
		}
		if (!IsVisible)
		{
			return;
		}
		Vector3 position = _gameInstance.LocalPlayer.Position;
		_statsDrawCount = (IsDetailedProfilingEnabled ? _textStats.Length : _defaultLength);
		Vector2 viewportSize = _gameInstance.SceneRenderer.Data.ViewportSize;
		_textStats[7].Text = $"Resolution: {viewportSize.X}x{viewportSize.Y}";
		_textStats[10].Text = $"Map Atlas: {_gameInstance.MapModule.TextureAtlas.Width}x{_gameInstance.MapModule.TextureAtlas.Height}, " + $"Entity Atlas: {_gameInstance.EntityStoreModule.TextureAtlas.Width}x{_gameInstance.EntityStoreModule.TextureAtlas.Height}";
		_textStats[11].Text = $"View Distance: {_gameInstance.App.Settings.ViewDistance}, Effective: {_gameInstance.MapModule.EffectiveViewDistance:##.0}";
		_textStats[13].Text = $"Entities: {_gameInstance.EntityStoreModule.GetEntitiesCount()}";
		_textStats[14].Text = $"Particles - Systems: {_gameInstance.Engine.FXSystem.Particles.ParticleSystemCount} ({_gameInstance.Engine.FXSystem.Particles.ParticleSystemProxyCount}), " + $"Draws: - Blend: {_gameInstance.Engine.FXSystem.Particles.PreviousFrameBlendDrawCount} - Erosion: {_gameInstance.Engine.FXSystem.Particles.PreviousFrameErosionDrawCount} " + $"- Distortion: {_gameInstance.Engine.FXSystem.Particles.PreviousFrameDistortionDrawCount}";
		_textStats[15].Text = $"Trails: {_gameInstance.Engine.FXSystem.Trails.TrailCount} ({_gameInstance.Engine.FXSystem.Trails.TrailProxyCount})";
		_textStats[16].Text = $"Immersive Views: {_gameInstance.ImmersiveScreenModule.GetScreenCount()}";
		_textStats[19].Text = AssetManager.GetAssetCountInfo();
		_textStats[20].Text = $"Audio Events: {_gameInstance.Engine.Audio.PlaybackCount} ";
		int num = (int)System.Math.Floor(position.X);
		int num2 = (int)System.Math.Floor(position.Y);
		int num3 = (int)System.Math.Floor(position.Z);
		int num4 = num >> 5;
		int num5 = num2 >> 5;
		int num6 = num3 >> 5;
		int num7 = num - num4 * 32;
		int num8 = num2 - num5 * 32;
		int num9 = num3 - num6 * 32;
		ChunkColumn chunkColumn = _gameInstance.MapModule.GetChunkColumn(num4, num6);
		if (chunkColumn != null)
		{
			int num10 = (num9 << 5) + num7;
			uint num11 = chunkColumn.Tints[num10];
			_textStats[21].Text = $"Heightmap: {chunkColumn.Heights[num10]}, " + $"Tint: #{(byte)(num11 >> 16):X2}{(byte)(num11 >> 8):X2}{(byte)num11:X2}";
			Chunk chunk = chunkColumn.GetChunk(num5);
			if (chunk != null)
			{
				string text = "-";
				string text2 = "-";
				int num12 = ChunkHelper.IndexOfWorldBlockInChunk(num, num2, num3);
				if (chunk.Data.SelfLightAmounts != null)
				{
					ushort num13 = chunk.Data.SelfLightAmounts[num12];
					int num14 = num13 & 0xF;
					int num15 = (num13 >> 4) & 0xF;
					int num16 = (num13 >> 8) & 0xF;
					int num17 = (num13 >> 12) & 0xF;
					text = $"R: {num14}, G: {num15}, B: {num16}, S: {num17}";
				}
				if (chunk.Data.BorderedLightAmounts != null)
				{
					int num18 = ChunkHelper.IndexOfBlockInBorderedChunk(num12, 0, 0, 0);
					ushort num19 = chunk.Data.BorderedLightAmounts[num18];
					int num20 = num19 & 0xF;
					int num21 = (num19 >> 4) & 0xF;
					int num22 = (num19 >> 8) & 0xF;
					int num23 = (num19 >> 12) & 0xF;
					text2 = $"R: {num20}, G: {num21}, B: {num22}, S: {num23}";
				}
				_textStats[22].Text = "Light - Local: " + text + ", Global: " + text2;
			}
			else
			{
				_textStats[22].Text = "Light: -";
			}
		}
		else
		{
			_textStats[21].Text = "Heightmap: -, Tint: -";
			_textStats[22].Text = "Light: -";
		}
		if (_gameInstance.WorldMapModule.TryGetBiomeAtPosition(position, out var biomeData))
		{
			_textStats[23].Text = "Biome: " + biomeData.ZoneName + " - " + biomeData.BiomeName;
		}
		else
		{
			_textStats[23].Text = "Biome: -";
		}
		_textStats[24].Text = "Environment: " + _gameInstance.WeatherModule.CurrentEnvironment.Id;
		_textStats[25].Text = "Weather: " + _gameInstance.WeatherModule.CurrentWeather.Id;
		if (_previousMusicSoundIndex != _gameInstance.AmbienceFXModule.CurrentMusicSoundEventIndex)
		{
			if (_gameInstance.AmbienceFXModule.CurrentMusicSoundEventIndex == 0 || !_gameInstance.Engine.Audio.ResourceManager.DebugWwiseIds.TryGetValue(_gameInstance.AmbienceFXModule.CurrentMusicSoundEventIndex, out var value))
			{
				value = "None";
			}
			_textStats[26].Text = "Music: " + _gameInstance.AmbienceFXModule.AmbienceFXs[_gameInstance.AmbienceFXModule.MusicAmbienceFXIndex].Id + " - " + value;
			_previousMusicSoundIndex = _gameInstance.AmbienceFXModule.CurrentMusicSoundEventIndex;
		}
		if (_previousSoundEffectIndex != _gameInstance.AudioModule.CurrentEffectSoundEventIndex)
		{
			string text3 = "None";
			if (_gameInstance.AudioModule.CurrentEffectSoundEventIndex != 0 && _gameInstance.Engine.Audio.ResourceManager.DebugWwiseIds.TryGetValue(_gameInstance.AudioModule.CurrentEffectSoundEventIndex, out var value2))
			{
				text3 = value2;
			}
			_textStats[27].Text = "Sound Effect: " + text3;
			_previousSoundEffectIndex = _gameInstance.AudioModule.CurrentEffectSoundEventIndex;
		}
		_textStats[28].Text = $"Chunk: ({num7}, {num8}, {num9}) in ({num4}, {num5}, {num6})";
		double num24 = System.Math.Round(position.X, 3);
		double num25 = System.Math.Round(position.Y, 3);
		double num26 = System.Math.Round(position.Z, 3);
		_textStats[29].Text = $"Feet Position: ({num24:##.000}, {num25:##.000}, {num26:##.000})";
		double num27 = System.Math.Round((double)(_gameInstance.LocalPlayer.LookOrientation.X * 180f) / System.Math.PI, 4);
		double num28 = System.Math.Round((double)(_gameInstance.LocalPlayer.LookOrientation.Y * 180f) / System.Math.PI, 4);
		double num29 = System.Math.Round((double)(_gameInstance.LocalPlayer.LookOrientation.Z * 180f) / System.Math.PI, 4);
		_textStats[31].Text = $"Orientation: ({num27:##.0000}, {num28:##.0000}, {num29:##.0000})";
		int hitboxCollisionConfigIndex = _gameInstance.LocalPlayer.HitboxCollisionConfigIndex;
		_textStats[30].Text = $"Collision Setting: ({hitboxCollisionConfigIndex}) - Collided Entities: ({_gameInstance.CharacterControllerModule.MovementController.CollidedEntities.Count})";
		if (_gameInstance.InteractionModule.HasFoundTargetBlock)
		{
			HitDetection.RaycastHit targetBlockHit = _gameInstance.InteractionModule.TargetBlockHit;
			ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[targetBlockHit.BlockId];
			_textStats[32].Text = $"Target Block: {clientBlockType.Name} ({targetBlockHit.BlockPosition.X}, {targetBlockHit.BlockPosition.Y}, {targetBlockHit.BlockPosition.Z}), Hitbox: {clientBlockType.HitboxType}";
		}
		else
		{
			_textStats[32].Text = "Target Block: -, Hitbox: -";
		}
		double num30 = System.Math.Round(_gameInstance.CharacterControllerModule.MovementController.LastMoveForce.X, 3);
		double num31 = System.Math.Round(_gameInstance.CharacterControllerModule.MovementController.LastMoveForce.Y, 3);
		double num32 = System.Math.Round(_gameInstance.CharacterControllerModule.MovementController.LastMoveForce.Z, 3);
		_textStats[33].Text = $"Last Move Force: ({num30:##.0000}, {num31:##.0000}, {num32:##.0000})";
		double num33 = System.Math.Round(_gameInstance.CharacterControllerModule.MovementController.WishDirection.X, 3);
		double num34 = System.Math.Round(_gameInstance.CharacterControllerModule.MovementController.WishDirection.Y, 3);
		_textStats[34].Text = $"Wish Direction: ({num33:##.0000}, {num34:##.0000})";
		_textStats[35].Text = $"Disposable - Undisposed: {Disposable.UndisposedDisposables.Count}, " + $"Unfinalized: {Disposable.UnfinalizedDisposables.Count}";
	}

	[Obsolete]
	public override void OnNewFrame(float time)
	{
		AccumulatedFrames++;
		int num = Interlocked.Exchange(ref _gameInstance.Connection.SentPacketLength, 0);
		int num2 = Interlocked.Exchange(ref _gameInstance.Connection.ReceivedPacketLength, 0);
		_accumulatedSentPacketLength += num;
		_accumulatedReceivedPacketLength += num2;
		AccumulatedFrameTime += time;
		while (AccumulatedFrameTime >= 1f)
		{
			MeanFrameDuration = (float)System.Math.Round(AccumulatedFrameTime / (float)AccumulatedFrames * 1000f, 2);
			TotalSentPacketLength += _accumulatedSentPacketLength;
			TotalReceivedPacketLength += _accumulatedReceivedPacketLength;
			LastAccumulatedSentPacketLength = (float)_accumulatedSentPacketLength / AccumulatedFrameTime;
			LastAccumulatedReceivedPacketLength = (float)_accumulatedReceivedPacketLength / AccumulatedFrameTime;
			if (IsVisible)
			{
				_textStats[6].Text = $"Mean Frame: {MeanFrameDuration}ms ({AccumulatedFrames} frames over {AccumulatedFrameTime}s)";
				int num3 = _gameInstance.Engine.Graphics.UpdateAvailableGPUMemory();
				int capacity = _gameInstance.Engine.Graphics.VideoMemory.Capacity;
				int num4 = capacity - num3;
				_textStats[5].Text = ((num3 == 0) ? "VRAM: N/A" : $"VRAM: {num4 / 1024} / {capacity / 1024} MB");
				_textStats[17].Text = $"Sent:     {LastAccumulatedSentPacketLength / 1000f:0.000} KB/s (Total: {(float)TotalSentPacketLength / 1000f:0.000}  KB)";
				_textStats[18].Text = $"Received: {LastAccumulatedReceivedPacketLength / 1000f:0.000} KB/s (Total: {(float)TotalReceivedPacketLength / 1000f:0.000} KB)";
				_textStats[8].Text = $"Draws: {DrawCallsCount}, Triangles: {DrawnTriangles}";
				int shadowCascadeDrawCallCount = _gameInstance.SceneRenderer.GetShadowCascadeDrawCallCount(0);
				int shadowCascadeDrawCallCount2 = _gameInstance.SceneRenderer.GetShadowCascadeDrawCallCount(1);
				int shadowCascadeDrawCallCount3 = _gameInstance.SceneRenderer.GetShadowCascadeDrawCallCount(2);
				int shadowCascadeDrawCallCount4 = _gameInstance.SceneRenderer.GetShadowCascadeDrawCallCount(3);
				int shadowCascadeKiloTriangleCount = _gameInstance.SceneRenderer.GetShadowCascadeKiloTriangleCount(0);
				int shadowCascadeKiloTriangleCount2 = _gameInstance.SceneRenderer.GetShadowCascadeKiloTriangleCount(1);
				int shadowCascadeKiloTriangleCount3 = _gameInstance.SceneRenderer.GetShadowCascadeKiloTriangleCount(2);
				int shadowCascadeKiloTriangleCount4 = _gameInstance.SceneRenderer.GetShadowCascadeKiloTriangleCount(3);
				_textStats[9].Text = $"Shadow Cascades - Draws: {shadowCascadeDrawCallCount}/{shadowCascadeDrawCallCount2}/{shadowCascadeDrawCallCount3}/{shadowCascadeDrawCallCount4}, " + $"Triangles: {shadowCascadeKiloTriangleCount}K/{shadowCascadeKiloTriangleCount2}K/{shadowCascadeKiloTriangleCount3}K/{shadowCascadeKiloTriangleCount4}K";
				_textStats[12].Text = $"Chunks - Loaded: {_gameInstance.MapModule.LoadedChunksCount}, Drawable: {_gameInstance.MapModule.DrawableChunksCount} (Max: {_gameInstance.MapModule.GetMaxChunksLoaded()}), " + $"Chunk Columns: {_gameInstance.MapModule.ChunkColumnCount()}";
				if (IsDetailedProfilingEnabled)
				{
					UpdateRenderingProfiles();
				}
			}
			_accumulatedSentPacketLength -= (int)LastAccumulatedSentPacketLength;
			_accumulatedReceivedPacketLength -= (int)LastAccumulatedReceivedPacketLength;
			AccumulatedFrames -= (int)((float)AccumulatedFrames / AccumulatedFrameTime);
			AccumulatedFrameTime -= 1f;
		}
		_fpsData.RecordValue(time * 1000f);
		_cpuData.RecordValue(_gameInstance.App.CpuTime * 1000f);
		_networkDataSent.RecordValue(num);
		_networkDataReceived.RecordValue(num2);
		_garbageCollectionData.RecordValue((float)GC.GetTotalMemory(forceFullCollection: false) / 1024f / 1024f);
		_processMemoryData.RecordValue((float)_process.PrivateMemorySize64 / 1024f / 1024f);
		if (IsVisible)
		{
			UpdateGraphs();
		}
	}

	private void UpdateRenderingProfiles()
	{
		Engine engine = _gameInstance.Engine;
		for (int i = 0; i < engine.Profiling.MeasureCount; i++)
		{
			ref Profiling.MeasureInfo measureInfo = ref engine.Profiling.GetMeasureInfo(i);
			ref Profiling.CPUMeasure cPUMeasure = ref engine.Profiling.GetCPUMeasure(i);
			ref Profiling.GPUMeasure gPUMeasure = ref engine.Profiling.GetGPUMeasure(i);
			string text = "";
			if (measureInfo.AccumulatedFrameCount > 1 && measureInfo.IsEnabled)
			{
				int num = i;
				float num2 = (float)System.Math.Round(cPUMeasure.AccumulatedElapsedTime / (float)measureInfo.AccumulatedFrameCount, 4);
				float num3 = (float)System.Math.Round(cPUMeasure.MaxElapsedTime, 4);
				text = num2.ToString("N4").PadLeft(9) + (" (" + num3.ToString("N4") + ")").PadLeft(12);
				if (measureInfo.HasGpuStats)
				{
					float num4 = (float)System.Math.Round(gPUMeasure.AccumulatedElapsedTime / (float)measureInfo.AccumulatedFrameCount, 4);
					int num5 = gPUMeasure.DrawnVertices / 3;
					text = text + num4.ToString("N4").PadLeft(9) + gPUMeasure.DrawCalls.ToString().PadLeft(6) + num5.ToString().PadLeft(10);
				}
			}
			_textStats[_defaultLength + engine.Profiling.MeasureCount + 1 + i + 1].Text = text;
		}
	}

	public void PrepareForDraw()
	{
		_batcher2d.RequestDrawTexture(_gameInstance.Engine.Graphics.WhitePixelTexture, new Rectangle(0, 0, 1, 1), new Vector3(0f, 0f, 0f), _width, _height, _backgroundColor);
		int num = (IsPartialRenderingProfilesEnabled ? (_defaultLength + 1) : _statsDrawCount);
		for (int i = 0; i < num; i++)
		{
			ref Stat reference = ref _textStats[i];
			_batcher2d.RequestDrawText(_font, 12f * _currentScale, reference.Text, new Vector3(reference.Position.X, reference.Position.Y, 0f), reference.Color);
		}
		if (IsPartialRenderingProfilesEnabled)
		{
			float viewportScale = _gameInstance.Engine.Window.ViewportScale;
			float num2 = 12f * viewportScale;
			int num3 = _defaultLength + 1;
			float num4 = _textStats[num3].Position.Y;
			for (int j = _defaultLength + 1; j < _defaultLength + 1 + _gameInstance.Engine.Profiling.MeasureCount; j++)
			{
				if (_gameInstance.Engine.Profiling.GetMeasureInfo(j - _defaultLength - 1).HasGpuStats == !IsCPUOnlyRenderingProfilesEnabled)
				{
					ref Stat reference2 = ref _textStats[j];
					_batcher2d.RequestDrawText(_font, 12f * _currentScale, reference2.Text, new Vector3(reference2.Position.X, num4, 0f), reference2.Color);
					ref Stat reference3 = ref _textStats[_gameInstance.Engine.Profiling.MeasureCount + 1 + j];
					_batcher2d.RequestDrawText(_font, 12f * _currentScale, reference3.Text, new Vector3(reference3.Position.X, num4, 0f), reference3.Color);
					num4 += num2;
				}
			}
			ref Stat reference4 = ref _textStats[_defaultLength + 1 + _gameInstance.Engine.Profiling.MeasureCount];
			_batcher2d.RequestDrawText(_font, 12f * _currentScale, reference4.Text, new Vector3(reference4.Position.X, reference4.Position.Y, 0f), reference4.Color);
		}
		PrepareForDrawGraphsAxisAndLabels();
	}

	public void Draw()
	{
		Batcher2DProgram batcher2DProgram = _gameInstance.Engine.Graphics.GPUProgramStore.Batcher2DProgram;
		batcher2DProgram.MVPMatrix.SetValue(ref _batcherOrthographicProjectionMatrix);
		_batcher2d.Draw();
	}

	private void InitializeGraphs(Font font)
	{
		_fpsGraph = new Graph(_gameInstance.Engine.Graphics, _batcher2d, font, 350, "FPS/CPU");
		_fpsGraph.Color = new Vector3(1f, 0f, 0f);
		_fpsGraph.AddAxis("", 0f);
		_fpsGraph.AddAxis("15", 66.666664f);
		_fpsGraph.AddAxis("30", 33.333332f);
		_fpsGraph.AddAxis("60", 16.666666f);
		_fpsGraph.AddAxis("120", 8.333333f);
		_fpsGraph.AddAxis("400", 2.5f);
		_cpuGraph = new Graph(_gameInstance.Engine.Graphics, _batcher2d, font, 350);
		_cpuGraph.Color = new Vector3(1f, 1f, 0f);
		_networkSentGraph = new Graph(_gameInstance.Engine.Graphics, _batcher2d, font, 350, "Network Send/Receive");
		_networkSentGraph.Color = new Vector3(0f, 0.75f, 1f);
		_networkSentGraph.AddAxis("", 0f);
		_networkSentGraph.AddAxis("100 B/s", 100f);
		_networkSentGraph.AddAxis("200 B/s", 200f);
		_networkSentGraph.AddAxis("400 B/s", 400f);
		_networkReceivedGraph = new Graph(_gameInstance.Engine.Graphics, _batcher2d, font, 350);
		_networkReceivedGraph.Color = new Vector3(1f, 0.75f, 0f);
		_garbageCollectionGraph = new Graph(_gameInstance.Engine.Graphics, _batcher2d, font, 350, "Heap/Process Memory");
		_garbageCollectionGraph.Color = new Vector3(1f, 1f, 0f);
		_garbageCollectionGraph.AxisUnit = " MiB";
		_garbageCollectionGraph.AddAxis("", 0f);
		_garbageCollectionGraph.AddAxis("512 MiB", 512f);
		_garbageCollectionGraph.AddAxis("1 GiB", 1024f);
		_garbageCollectionGraph.AddAxis("2 GiB", 2048f);
		_garbageCollectionGraph.AddAxis("3 GiB", 3072f);
		_garbageCollectionGraph.AddAxis("4 GiB", 4096f);
		_processMemoryGraph = new Graph(_gameInstance.Engine.Graphics, _batcher2d, font, 350);
		_processMemoryGraph.Color = new Vector3(1f, 0f, 0f);
		UpdateGraphWindowScale();
	}

	private void DisposeGraphs()
	{
		_fpsGraph.Dispose();
		_cpuGraph.Dispose();
		_networkSentGraph.Dispose();
		_networkReceivedGraph.Dispose();
		_garbageCollectionGraph.Dispose();
		_processMemoryGraph.Dispose();
	}

	private void UpdateGraphs()
	{
		if (_currentScale != _gameInstance.Engine.Window.ViewportScale)
		{
			UpdateGraphWindowScale();
		}
		_fpsGraph.UpdateHistory(_fpsData);
		_cpuGraph.UpdateHistory(_cpuData);
		_networkSentGraph.UpdateHistory(_networkDataSent);
		_networkReceivedGraph.UpdateHistory(_networkDataReceived);
		float scale = CalculateAxisScale(MathHelper.Max(_garbageCollectionData.AverageValue, _processMemoryData.AverageValue), 500f);
		_garbageCollectionGraph.UpdateHistory(_garbageCollectionData, scale);
		_processMemoryGraph.UpdateHistory(_processMemoryData, scale);
	}

	private void UpdateGraphWindowScale()
	{
		float viewportScale = _gameInstance.Engine.Window.ViewportScale;
		_fpsGraph.Position = (_cpuGraph.Position = new Vector3(50f * viewportScale, 115f * viewportScale, -1f));
		_fpsGraph.LabelPosition = (_cpuGraph.LabelPosition = new Vector3(50f * viewportScale, (float)_gameInstance.Engine.Window.Viewport.Height - 115f * viewportScale, -1f));
		_fpsGraph.Scale = (_cpuGraph.Scale = new Vector3(1f * viewportScale, 2.25f * viewportScale, 1f));
		_fpsGraph.UpdateTextHeight(12f * viewportScale);
		_fpsGraph.UpdateAxisData(ref _graphOrthographicProjectionMatrix);
		_cpuGraph.UpdateTextHeight(12f * viewportScale);
		_cpuGraph.UpdateAxisData(ref _graphOrthographicProjectionMatrix);
		_networkSentGraph.Position = (_networkReceivedGraph.Position = new Vector3(475f * viewportScale, 115f * viewportScale, -1f));
		_networkSentGraph.LabelPosition = (_networkReceivedGraph.LabelPosition = new Vector3(475f * viewportScale, (float)_gameInstance.Engine.Window.Viewport.Height - 115f * viewportScale, -1f));
		_networkSentGraph.Scale = (_networkReceivedGraph.Scale = new Vector3(1f * viewportScale, 0.375f * viewportScale, 1f));
		_networkSentGraph.UpdateTextHeight(12f * viewportScale);
		_networkSentGraph.UpdateAxisData(ref _graphOrthographicProjectionMatrix);
		_networkReceivedGraph.UpdateTextHeight(12f * viewportScale);
		_networkReceivedGraph.UpdateAxisData(ref _graphOrthographicProjectionMatrix);
		_garbageCollectionGraph.Position = new Vector3(915f * viewportScale, 115f * viewportScale, -1f);
		_garbageCollectionGraph.LabelPosition = new Vector3(915f * viewportScale, (float)_gameInstance.Engine.Window.Viewport.Height - 115f * viewportScale, -1f);
		_garbageCollectionGraph.Scale = new Vector3(1f * viewportScale, 0.3f * viewportScale, 1f);
		_garbageCollectionGraph.UpdateTextHeight(12f * viewportScale);
		_garbageCollectionGraph.UpdateAxisData(ref _graphOrthographicProjectionMatrix);
		_processMemoryGraph.Position = _garbageCollectionGraph.Position;
		_processMemoryGraph.LabelPosition = _garbageCollectionGraph.LabelPosition;
		_processMemoryGraph.Scale = _garbageCollectionGraph.Scale;
		_processMemoryGraph.UpdateTextHeight(12f * viewportScale);
		_processMemoryGraph.UpdateAxisData(ref _graphOrthographicProjectionMatrix);
		_currentScale = viewportScale;
	}

	public void DrawGraphsData()
	{
		_fpsGraph.DrawData();
		_cpuGraph.DrawData();
		_networkSentGraph.DrawData();
		_networkReceivedGraph.DrawData();
		_processMemoryGraph.DrawData();
		_garbageCollectionGraph.DrawData();
	}

	public void PrepareForDrawGraphsAxisAndLabels()
	{
		_fpsGraph.PrepareForDrawAxisAndLabels();
		_networkSentGraph.PrepareForDrawAxisAndLabels();
		_garbageCollectionGraph.PrepareForDrawAxisAndLabels();
	}

	private static float CalculateAxisScale(float average, float max)
	{
		return (float)System.Math.Ceiling(MathHelper.Max(1f, average / max) * 2f) / 2f;
	}
}
