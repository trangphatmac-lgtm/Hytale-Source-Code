#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Hypixel.ProtoPlus;
using HytaleClient.Audio;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Data.Characters;
using HytaleClient.Data.FX;
using HytaleClient.Data.Items;
using HytaleClient.Data.Map;
using HytaleClient.Data.Map.Chunk;
using HytaleClient.Data.UserSettings;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Map;
using HytaleClient.Graphics.Particles;
using HytaleClient.InGame.Modules.Audio;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Wwise;

namespace HytaleClient.InGame.Modules.Map;

internal class MapModule : Module
{
	public struct AtlasLocation
	{
		public Point Position;

		public Point Size;

		public int TileIndex;
	}

	private class BlockyModelTextureInfo
	{
		public string ServerPath;

		public string Hash;

		public AtlasLocation AtlasLocation;
	}

	public struct LevelOfDetailSetup
	{
		public bool Enabled;

		public float InvRange;

		public float StartDistance;

		public float ShadowStartDistance;

		public float ShadowInvRange;
	}

	public struct BlockPosition
	{
		public int X;

		public int Y;

		public int Z;
	}

	private struct ChunkPosition
	{
		public int X;

		public int Y;

		public int Z;
	}

	private struct DestroyedBlocksInfo
	{
		private int _deletedBlockCount;

		private BlockPosition[] _deletedBlockPositions;

		private byte[] _deletedBlockAge;

		private int _commitedChangeCount;

		public ushort[] _commitedChangeIds;

		private Vector3[] _commitedBlockPositionsFromCamera;

		private float[] _commitedBlockDistanceFromCamera;

		public int Count => _commitedChangeCount;

		public Vector3[] BlockPositionsFromCamera => _commitedBlockPositionsFromCamera;

		public DestroyedBlocksInfo(int capacity)
		{
			_deletedBlockCount = 0;
			_deletedBlockPositions = new BlockPosition[capacity];
			_deletedBlockAge = new byte[capacity];
			_commitedChangeCount = 0;
			_commitedChangeIds = new ushort[capacity];
			_commitedBlockPositionsFromCamera = new Vector3[capacity];
			_commitedBlockDistanceFromCamera = new float[capacity];
		}

		public void RegisterDestroyedBlock(int x, int y, int z)
		{
			if (_deletedBlockCount != _deletedBlockPositions.Length)
			{
				int deletedBlockCount = _deletedBlockCount;
				_deletedBlockPositions[deletedBlockCount].X = x;
				_deletedBlockPositions[deletedBlockCount].Y = y;
				_deletedBlockPositions[deletedBlockCount].Z = z;
				_deletedBlockAge[deletedBlockCount] = 0;
				_deletedBlockCount++;
			}
		}

		public void SafeRegisterDestroyedBlock(int x, int y, int z)
		{
			int num = Interlocked.Increment(ref _deletedBlockCount);
			if (num <= _deletedBlockPositions.Length)
			{
				int num2 = num - 1;
				_deletedBlockPositions[num2].X = x;
				_deletedBlockPositions[num2].Y = y;
				_deletedBlockPositions[num2].Z = z;
				_deletedBlockAge[num2] = 0;
			}
			else
			{
				Interlocked.Decrement(ref _deletedBlockCount);
			}
		}

		public void PrepareBlocksRemovedThisFrame(int updatedChunksCount, ChunkPosition[] updatedChunksPositions, Vector3 previousCameraPosition, Vector3 cameraPosition, BoundingFrustum cameraFrustum, float rejectNearCameraDistance = 16f)
		{
			for (int i = 0; i < _deletedBlockCount; i++)
			{
				for (int j = i + 1; j < _deletedBlockCount; j++)
				{
					if (_deletedBlockPositions[i].X == _deletedBlockPositions[j].X && _deletedBlockPositions[i].Y == _deletedBlockPositions[j].Y && _deletedBlockPositions[i].Z == _deletedBlockPositions[j].Z)
					{
						for (int k = j + 1; k < _deletedBlockCount; k++)
						{
							_deletedBlockPositions[k - 1] = _deletedBlockPositions[k];
						}
						_deletedBlockCount--;
					}
				}
			}
			_commitedChangeCount = 0;
			for (int l = 0; l < updatedChunksCount; l++)
			{
				ChunkPosition chunkPosition = updatedChunksPositions[l];
				chunkPosition.X *= 32;
				chunkPosition.Y *= 32;
				chunkPosition.Z *= 32;
				ChunkPosition chunkPosition2 = chunkPosition;
				chunkPosition2.X += 32;
				chunkPosition2.Y += 32;
				chunkPosition2.Z += 32;
				for (ushort num = 0; num < _deletedBlockCount; num++)
				{
					if (chunkPosition.X <= _deletedBlockPositions[num].X && _deletedBlockPositions[num].X < chunkPosition2.X && chunkPosition.Y <= _deletedBlockPositions[num].Y && _deletedBlockPositions[num].Y < chunkPosition2.Y && chunkPosition.Z <= _deletedBlockPositions[num].Z && _deletedBlockPositions[num].Z < chunkPosition2.Z)
					{
						_commitedChangeIds[_commitedChangeCount] = num;
						_commitedChangeCount++;
						_deletedBlockAge[num] = byte.MaxValue;
					}
				}
			}
			if (_commitedChangeCount > 0)
			{
				int num2 = 0;
				float num3 = rejectNearCameraDistance * rejectNearCameraDistance;
				Vector3 vector = new Vector3(0.5f);
				Vector3 vector2 = default(Vector3);
				BoundingBox box = default(BoundingBox);
				for (int m = 0; m < _commitedChangeCount; m++)
				{
					ushort num4 = _commitedChangeIds[m];
					vector2.X = (float)_deletedBlockPositions[num4].X - cameraPosition.X;
					vector2.Y = (float)_deletedBlockPositions[num4].Y - cameraPosition.Y;
					vector2.Z = (float)_deletedBlockPositions[num4].Z - cameraPosition.Z;
					box.Min = vector2;
					box.Max = vector2 + Vector3.One;
					bool flag = cameraFrustum.Intersects(box);
					float num5 = (vector + vector2).LengthSquared();
					bool flag2 = num5 < num3 || !flag;
					_commitedBlockDistanceFromCamera[m] = (flag2 ? 1000000f : num5);
					_commitedBlockPositionsFromCamera[m] = vector2 + cameraPosition - previousCameraPosition;
					num2 += (flag2 ? 1 : 0);
				}
				_commitedChangeCount -= num2;
				Array.Sort(_commitedBlockDistanceFromCamera, _commitedBlockPositionsFromCamera, 0, _commitedChangeCount);
			}
			ClearProcessedBlocks();
		}

		private void ClearProcessedBlocks()
		{
			for (int i = 0; i < _deletedBlockCount; i++)
			{
				if (_deletedBlockAge[i] >= 35)
				{
					for (int j = i + 1; j < _deletedBlockCount; j++)
					{
						_deletedBlockPositions[j - 1] = _deletedBlockPositions[j];
						_deletedBlockAge[j - 1] = _deletedBlockAge[j];
					}
					_deletedBlockCount--;
				}
				else
				{
					_deletedBlockAge[i]++;
				}
			}
		}
	}

	private const int InteractionStateDone = -2;

	private const int InteractionStateStarting = -1;

	private const int UnknownTextureIndex = 0;

	public const string UnknownTexturePath = "BlockTextures/Unknown.png";

	public readonly Texture TextureAtlas;

	private readonly ConcurrentDictionary<long, ChunkColumn> _chunkColumns = new ConcurrentDictionary<long, ChunkColumn>();

	private const int LightOctTreeSize = 8;

	private const int LightOctMaxDepth = 12;

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private const int NearbyChunksRadius = 1;

	private const int MaxNearChunkProgramColumns = 9;

	public const float BlockHitDuration = 0.3f;

	public ushort LoadedChunksCount;

	public ushort DrawableChunksCount;

	private MapGeometryBuilder _mapGeometryBuilder;

	private readonly SpiralIterator _spiralIterator = new SpiralIterator();

	public readonly float[] LightLevels = new float[16];

	public LevelOfDetailSetup LODSetup;

	public int MaxServerViewRadius;

	public int ChunkYMin;

	private int _chunkColumnCount;

	private const int ChunksDefaultSize = 1000;

	private const int ChunksGrowth = 1000;

	private Chunk[] _chunks = new Chunk[1000];

	private ushort _chunksCount;

	private byte[] _drawMasks = new byte[1000];

	private bool[] _undergroundHints = new bool[1000];

	private const int AnimatedChunksDefaultSize = 1000;

	private const int AnimatedChunksGrowth = 1000;

	private ushort[] _animatedChunksLocalIds = new ushort[1000];

	private ushort _animatedChunksCount;

	private ushort[] _visibleAnimatedChunkIds = new ushort[1000];

	private ushort _visibleAnimatedChunksCount;

	private const int BoundingVolumesDefaultSize = 1000;

	private const int BoundingVolumesGrowth = 1000;

	private BoundingBox[] _boundingVolumes = new BoundingBox[1000];

	private ushort _updatedChunksCount;

	private ChunkPosition[] _updatedChunksPositions = new ChunkPosition[128];

	private DestroyedBlocksInfo _destroyedBlocksInfo = new DestroyedBlocksInfo(128);

	private BitField3D _bitFieldChunksReadyForDraw;

	private readonly byte ChunkDrawTagOpaque = 1;

	private readonly byte ChunkDrawTagAlphaTested = 2;

	private readonly byte ChunkDrawTagAlphaBlended = 4;

	private readonly byte ChunkDrawTagAnimated = 8;

	private readonly Vector3 _chunkSize = new Vector3(32f);

	public ClientBlockType[] ClientBlockTypes { get; private set; }

	public bool ShouldDrawAllChunksAsNear => _chunkColumnCount <= 9;

	public bool AreNearbyChunksRendered { get; private set; }

	public float EffectiveViewDistance { get; private set; }

	public int StartChunkX { get; private set; } = int.MaxValue;


	public int StartChunkZ { get; private set; } = int.MaxValue;


	public int ViewRadius { get; private set; }

	private void SetBlockInteractionState(Chunk chunk, int blockId, int blockIndex, Vector3 position, bool isDone, bool playInteractionStateSound)
	{
		ClientBlockType blockType = ClientBlockTypes[blockId];
		ChunkData.InteractionStateInfo interactionStateInfo = default(ChunkData.InteractionStateInfo);
		interactionStateInfo.BlockId = blockId;
		interactionStateInfo.BlockType = blockType;
		interactionStateInfo.StateFrameTime = (isDone ? (-2) : (-1));
		interactionStateInfo.SoundEventReference = AudioDevice.SoundEventReference.None;
		ChunkData.InteractionStateInfo interactionInfo = interactionStateInfo;
		if (chunk.Data.CurrentInteractionStates.TryGetValue(blockIndex, out var value))
		{
			interactionInfo.SoundEventReference.SoundObjectReference = value.SoundEventReference.SoundObjectReference;
			if (value.BlockType.SoundEventIndex == interactionInfo.BlockType.SoundEventIndex)
			{
				interactionInfo.SoundEventReference.PlaybackId = value.SoundEventReference.PlaybackId;
			}
			else if (value.SoundEventReference.PlaybackId != -1)
			{
				_gameInstance.AudioModule.ActionOnEvent(ref value.SoundEventReference, (AkActionOnEventType)0);
			}
			if (interactionInfo.BlockType.BlockyAnimation != null && interactionInfo.BlockType.BlockyAnimation == value.BlockType.BlockyAnimation && interactionInfo.BlockType.Looping && value.BlockType.Looping)
			{
				interactionInfo.StateFrameTime = value.StateFrameTime;
			}
		}
		position += new Vector3(0.5f);
		if (playInteractionStateSound && interactionInfo.SoundEventReference.PlaybackId == -1)
		{
			PlayBlockInteractionStateSound(ref interactionInfo, position, isDone);
		}
		if (interactionInfo.SoundEventReference.SoundObjectReference.SoundObjectId != 0 && interactionInfo.SoundEventReference.PlaybackId == -1)
		{
			_gameInstance.AudioModule.UnregisterSoundObject(ref interactionInfo.SoundEventReference.SoundObjectReference);
		}
		chunk.Data.CurrentInteractionStates[blockIndex] = interactionInfo;
	}

	private void PlayBlockInteractionStateSound(ref ChunkData.InteractionStateInfo interactionInfo, Vector3 position, bool isDone)
	{
		if (isDone && !interactionInfo.BlockType.Looping)
		{
			return;
		}
		uint soundEventIndex = interactionInfo.BlockType.SoundEventIndex;
		if (soundEventIndex != 0)
		{
			if (interactionInfo.SoundEventReference.SoundObjectReference.SoundObjectId == 0)
			{
				_gameInstance.AudioModule.TryRegisterSoundObject(position, Vector3.Zero, ref interactionInfo.SoundEventReference.SoundObjectReference, hasUniqueEvent: true);
			}
			_gameInstance.AudioModule.PlaySoundEvent(soundEventIndex, interactionInfo.SoundEventReference.SoundObjectReference, ref interactionInfo.SoundEventReference);
		}
	}

	public void PrepareBlockTypes(Dictionary<int, BlockType> networkBlockTypes, int highestReceivedBlockId, bool atlasNeedsUpdate, ref ClientBlockType[] upcomingBlockTypes, ref Dictionary<string, AtlasLocation> upcomingBlocksImageLocations, ref Point atlasSize, out byte[][] upcomingAtlasPixelsPerLevel, CancellationToken cancellationToken)
	{
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Invalid comparison between Unknown and I4
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Invalid comparison between Unknown and I4
		//IL_0343: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Invalid comparison between Unknown and I4
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Invalid comparison between Unknown and I4
		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Invalid comparison between Unknown and I4
		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0371: Invalid comparison between Unknown and I4
		//IL_0375: Unknown result type (might be due to invalid IL or missing references)
		//IL_037b: Invalid comparison between Unknown and I4
		//IL_0389: Unknown result type (might be due to invalid IL or missing references)
		//IL_038e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_097f: Unknown result type (might be due to invalid IL or missing references)
		//IL_098b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0997: Unknown result type (might be due to invalid IL or missing references)
		//IL_03af: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b5: Invalid comparison between Unknown and I4
		//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bf: Invalid comparison between Unknown and I4
		//IL_0c49: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c4e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c9: Invalid comparison between Unknown and I4
		//IL_03ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f3: Invalid comparison between Unknown and I4
		//IL_03f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fd: Invalid comparison between Unknown and I4
		//IL_0401: Unknown result type (might be due to invalid IL or missing references)
		//IL_0407: Invalid comparison between Unknown and I4
		//IL_05a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ae: Invalid comparison between Unknown and I4
		//IL_05bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_11b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_11bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0621: Unknown result type (might be due to invalid IL or missing references)
		//IL_1253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0683: Unknown result type (might be due to invalid IL or missing references)
		//IL_07bc: Unknown result type (might be due to invalid IL or missing references)
		Debug.Assert(!ThreadHelper.IsMainThread());
		if (highestReceivedBlockId >= upcomingBlockTypes.Length)
		{
			Array.Resize(ref upcomingBlockTypes, highestReceivedBlockId + 1);
		}
		Dictionary<string, BlockyModel> dictionary = new Dictionary<string, BlockyModel>();
		Dictionary<string, BlockyAnimation> dictionary2 = new Dictionary<string, BlockyAnimation>();
		List<int> list = networkBlockTypes.Keys.ToList();
		list.Sort();
		BlockyModel blockyModel = new BlockyModel(1);
		BlockyModelNode node = BlockyModelNode.CreateMapBlockNode(CharacterPartStore.BlockNameId, 16f, 1f);
		blockyModel.AddNode(ref node);
		Dictionary<int, List<int>> dictionary3 = new Dictionary<int, List<int>>();
		for (int i = 0; i < list.Count; i++)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				upcomingBlocksImageLocations = null;
				upcomingAtlasPixelsPerLevel = null;
				return;
			}
			int num = list[i];
			BlockType val = networkBlockTypes[num];
			if (val == null)
			{
				if (num <= highestReceivedBlockId)
				{
					_gameInstance.App.DevTools.Error($"Didn't receive block type for id: {num}, {highestReceivedBlockId}");
				}
				continue;
			}
			ClientBlockType clientBlockType = new ClientBlockType();
			clientBlockType.Id = num;
			clientBlockType.Item = val.Item;
			clientBlockType.Name = val.Name;
			clientBlockType.Unknown = val.Unknown;
			if (num == 0 && clientBlockType.Name != "Empty")
			{
				throw new InvalidDataException("Block type with EmptyBlockId but has name " + clientBlockType.Name);
			}
			if (num == 1 && clientBlockType.Name != "Unknown")
			{
				throw new InvalidDataException("Block type with UnknownBlockId but has name " + clientBlockType.Name);
			}
			string name = val.Name;
			int num2 = name.IndexOf("|Filler=", StringComparison.Ordinal);
			bool flag = num2 > 0;
			if (!clientBlockType.Unknown && flag)
			{
				num2 += 8;
				int num3 = name.IndexOf("|", num2, StringComparison.Ordinal);
				string text = ((num3 != -1) ? name.Substring(num2, num3 - num2) : name.Substring(num2));
				string[] array = text.Split(new char[1] { ',' });
				bool flag2 = int.TryParse(array[0], out clientBlockType.FillerX);
				flag2 &= int.TryParse(array[1], out clientBlockType.FillerY);
				if (!(flag2 & int.TryParse(array[2], out clientBlockType.FillerZ)))
				{
					_gameInstance.App.DevTools.Error("Failed to parse filler offset: " + text + " for " + name);
				}
			}
			clientBlockType.DrawType = val.DrawType_;
			clientBlockType.RotationYaw = val.RotationYaw;
			clientBlockType.RotationPitch = val.RotationPitch;
			clientBlockType.RotationRoll = val.RotationRoll;
			clientBlockType.RandomRotation = val.RandomRotation_;
			clientBlockType.RotationYawPlacementOffset = val.RotationYawPlacementOffset;
			clientBlockType.ShouldRenderCube = (int)clientBlockType.DrawType == 2 || (int)clientBlockType.DrawType == 4 || (int)clientBlockType.DrawType == 1;
			clientBlockType.RequiresAlphaBlending = val.RequiresAlphaBlending;
			clientBlockType.VerticalFill = (byte)val.VerticalFill;
			clientBlockType.MaxFillLevel = (byte)val.MaxFillLevel;
			clientBlockType.IsOccluder = ((int)clientBlockType.DrawType == 2 || (int)clientBlockType.DrawType == 4) && !clientBlockType.RequiresAlphaBlending;
			clientBlockType.HasModel = (int)clientBlockType.DrawType == 3 || (int)clientBlockType.DrawType == 4;
			clientBlockType.Opacity = val.Opacity_;
			if (clientBlockType.ShouldRenderCube)
			{
				if ((int)clientBlockType.RotationYaw != 0 && (int)clientBlockType.RotationYaw != 1 && (int)clientBlockType.RotationYaw != 2 && (int)clientBlockType.RotationYaw != 3)
				{
					throw new Exception("Only 0°, 90°, 180° or 270° rotations around Y are supported for cube blocks");
				}
				if ((int)clientBlockType.RotationPitch != 0 && (int)clientBlockType.RotationPitch != 1 && (int)clientBlockType.RotationPitch != 2 && (int)clientBlockType.RotationPitch != 3)
				{
					throw new Exception("Only 0°, 90°, 180° or 270° rotations around Z are supported for cube blocks");
				}
				BlockTextures[] cubeTextures = val.CubeTextures;
				int num4 = ((cubeTextures == null) ? 1 : cubeTextures.Length);
				clientBlockType.CubeTextureWeights = new float[num4];
				string[] array2 = new string[num4];
				string[] array3 = new string[num4];
				string[] array4 = new string[num4];
				string[] array5 = new string[num4];
				string[] array6 = new string[num4];
				string[] array7 = new string[num4];
				if (val.CubeTextures != null && val.CubeTextures.Length != 0)
				{
					for (int j = 0; j < num4; j++)
					{
						BlockTextures val2 = val.CubeTextures[j];
						array2[j] = val2.Top;
						array3[j] = val2.Bottom;
						array4[j] = val2.Left;
						array5[j] = val2.Right;
						array6[j] = val2.Front;
						array7[j] = val2.Back;
						clientBlockType.CubeTextureWeights[j] = val2.Weight;
					}
				}
				else
				{
					array2[0] = "BlockTextures/Unknown.png";
					array3[0] = "BlockTextures/Unknown.png";
					array4[0] = "BlockTextures/Unknown.png";
					array5[0] = "BlockTextures/Unknown.png";
					array6[0] = "BlockTextures/Unknown.png";
					array7[0] = "BlockTextures/Unknown.png";
					clientBlockType.CubeTextureWeights[0] = 1f;
				}
				ClientBlockType.CubeTexture cubeTexture = CreateBlockTexture(clientBlockType, array2);
				ClientBlockType.CubeTexture cubeTexture2 = CreateBlockTexture(clientBlockType, array3);
				ClientBlockType.CubeTexture cubeTexture3 = CreateBlockTexture(clientBlockType, array4);
				ClientBlockType.CubeTexture cubeTexture4 = CreateBlockTexture(clientBlockType, array5);
				ClientBlockType.CubeTexture cubeTexture5 = CreateBlockTexture(clientBlockType, array6);
				ClientBlockType.CubeTexture cubeTexture6 = CreateBlockTexture(clientBlockType, array7);
				if ((int)clientBlockType.CollisionMaterial != 2)
				{
					int num5 = MathHelper.RotationToDegrees(clientBlockType.RotationPitch);
					for (int k = 0; k < num5 / 90; k++)
					{
						ClientBlockType.CubeTexture cubeTexture7 = cubeTexture5;
						ClientBlockType.CubeTexture cubeTexture8 = cubeTexture6;
						cubeTexture5 = cubeTexture;
						cubeTexture6 = cubeTexture2;
						cubeTexture = cubeTexture8;
						cubeTexture2 = cubeTexture7;
					}
					cubeTexture3.Rotation += num5;
					cubeTexture4.Rotation += num5;
					int num6 = MathHelper.RotationToDegrees(clientBlockType.RotationYaw);
					for (int l = 0; l < num6 / 90; l++)
					{
						ClientBlockType.CubeTexture cubeTexture9 = cubeTexture5;
						ClientBlockType.CubeTexture cubeTexture10 = cubeTexture6;
						cubeTexture5 = cubeTexture3;
						cubeTexture6 = cubeTexture4;
						cubeTexture3 = cubeTexture10;
						cubeTexture4 = cubeTexture9;
					}
					cubeTexture.Rotation -= num6;
					cubeTexture2.Rotation += num6;
					int num7 = MathHelper.RotationToDegrees(clientBlockType.RotationRoll);
					for (int m = 0; m < num7 / 90; m++)
					{
						ClientBlockType.CubeTexture cubeTexture11 = cubeTexture;
						ClientBlockType.CubeTexture cubeTexture12 = cubeTexture2;
						ClientBlockType.CubeTexture cubeTexture13 = cubeTexture3;
						ClientBlockType.CubeTexture cubeTexture14 = cubeTexture4;
						cubeTexture = cubeTexture13;
						cubeTexture2 = cubeTexture14;
						cubeTexture3 = cubeTexture12;
						cubeTexture4 = cubeTexture11;
					}
					cubeTexture5.Rotation += num7;
					cubeTexture6.Rotation += num7;
				}
				clientBlockType.CubeTextures = new ClientBlockType.CubeTexture[6] { cubeTexture, cubeTexture2, cubeTexture3, cubeTexture4, cubeTexture5, cubeTexture6 };
				ClientBlockType.CubeTexture[] cubeTextures2 = clientBlockType.CubeTextures;
				foreach (ClientBlockType.CubeTexture cubeTexture15 in cubeTextures2)
				{
					cubeTexture15.Rotation = 180 + MathHelper.WrapAngleDegrees(cubeTexture15.Rotation - 180);
				}
				byte b2 = (byte)((clientBlockType.MaxFillLevel == 0) ? 8 : clientBlockType.MaxFillLevel);
				if (clientBlockType.VerticalFill == b2)
				{
					clientBlockType.CubeSideMaskTexture = val.CubeSideMaskTexture;
					clientBlockType.TransitionTexture = val.TransitionTexture;
					clientBlockType.TransitionGroupId = val.Group;
					clientBlockType.TransitionToGroupIds = val.TransitionToGroups;
				}
				BlockTypeProtocolInitializer.ConvertShadingMode(val.CubeShadingMode, out clientBlockType.CubeShadingMode);
			}
			if (clientBlockType.HasModel)
			{
				if (val.Model == null || !_gameInstance.HashesByServerAssetPath.TryGetValue(val.Model, out clientBlockType.BlockyModelHash))
				{
					_gameInstance.App.DevTools.Error("Missing model asset: " + val.Model + " for block " + clientBlockType.Name);
					clientBlockType.BlockyModelHash = null;
				}
				ModelTexture[] modelTexture_ = val.ModelTexture_;
				clientBlockType.BlockyTextures = new ClientBlockType.BlockyTexture[System.Math.Max(1, modelTexture_.Length)];
				clientBlockType.BlockyTextureWeights = new float[System.Math.Max(1, modelTexture_.Length)];
				int num8 = 0;
				ModelTexture[] array8 = modelTexture_;
				foreach (ModelTexture val3 in array8)
				{
					clientBlockType.BlockyTextures[num8] = new ClientBlockType.BlockyTexture
					{
						Name = val3.Texture
					};
					clientBlockType.BlockyTextureWeights[num8] = val3.Weight;
					if (clientBlockType.BlockyTextures[num8].Name == null || !_gameInstance.HashesByServerAssetPath.TryGetValue(clientBlockType.BlockyTextures[num8].Name, out clientBlockType.BlockyTextures[num8].Hash))
					{
						_gameInstance.App.DevTools.Error("Missing texture asset for custom model: " + clientBlockType.BlockyTextures[num8].Name);
						clientBlockType.BlockyTextures[num8].Hash = null;
					}
					num8++;
				}
				clientBlockType.BlockyModelScale = val.ModelScale;
			}
			Matrix.CreateFromYawPitchRoll(MathHelper.RotationToRadians(clientBlockType.RotationYaw), MathHelper.RotationToRadians(clientBlockType.RotationPitch), MathHelper.RotationToRadians(clientBlockType.RotationRoll), out clientBlockType.RotationMatrix);
			Matrix.CreateScale(clientBlockType.BlockyModelScale * (1f / 32f), out clientBlockType.BlockyModelTranslatedScaleMatrix);
			Matrix.AddTranslation(ref clientBlockType.BlockyModelTranslatedScaleMatrix, 0.5f, 0f, 0.5f);
			Matrix.Multiply(ref clientBlockType.BlockyModelTranslatedScaleMatrix, ref ChunkGeometryBuilder.NegativeHalfBlockOffsetMatrix, out clientBlockType.WorldMatrix);
			Matrix.Multiply(ref clientBlockType.WorldMatrix, ref clientBlockType.RotationMatrix, out clientBlockType.WorldMatrix);
			Matrix.Multiply(ref clientBlockType.WorldMatrix, ref ChunkGeometryBuilder.PositiveHalfBlockOffsetMatrix, out clientBlockType.WorldMatrix);
			Matrix.Invert(ref clientBlockType.WorldMatrix, out clientBlockType.CubeBlockInvertMatrix);
			Matrix.AddTranslation(ref clientBlockType.CubeBlockInvertMatrix, 0f, -16f, 0f);
			if (val.ModelAnimation != null)
			{
				if (!_gameInstance.HashesByServerAssetPath.TryGetValue(val.ModelAnimation, out var value))
				{
					_gameInstance.App.DevTools.Error("Missing animation asset: " + val.ModelAnimation + " for block " + clientBlockType.Name);
				}
				else if (!dictionary2.TryGetValue(value, out clientBlockType.BlockyAnimation))
				{
					BlockyAnimation blockyAnimation = new BlockyAnimation();
					BlockyAnimationInitializer.Parse(AssetManager.GetAssetUsingHash(value), _gameInstance.EntityStoreModule.NodeNameManager, ref blockyAnimation);
					BlockyAnimation blockyAnimation3 = (dictionary2[value] = blockyAnimation);
					clientBlockType.BlockyAnimation = blockyAnimation3;
				}
			}
			clientBlockType.SelfTintColorsBySide = new int[6]
			{
				val.Tint_.Top,
				val.Tint_.Bottom,
				val.Tint_.Left,
				val.Tint_.Right,
				val.Tint_.Front,
				val.Tint_.Back
			};
			clientBlockType.BiomeTintMultipliersBySide = new float[6]
			{
				(float)val.BiomeTint.Top / 100f,
				(float)val.BiomeTint.Bottom / 100f,
				(float)val.BiomeTint.Left / 100f,
				(float)val.BiomeTint.Right / 100f,
				(float)val.BiomeTint.Front / 100f,
				(float)val.BiomeTint.Back / 100f
			};
			if (val.Light != null)
			{
				ClientItemBaseProtocolInitializer.ParseLightColor(val.Light, ref clientBlockType.LightEmitted);
			}
			clientBlockType.CollisionMaterial = val.Material_;
			clientBlockType.HitboxType = val.Hitbox;
			clientBlockType.MovementSettings = val.MovementSettings;
			clientBlockType.IsUsable = val.Flags.IsUsable;
			clientBlockType.InteractionHint = val.InteractionHint;
			clientBlockType.Gathering = val.Gathering;
			if (val.ShaderEffect.Contains((ShaderType)4))
			{
				clientBlockType.CubeShaderEffect = ClientBlockType.ClientShaderEffect.Ice;
			}
			else if (val.ShaderEffect.Contains((ShaderType)5))
			{
				clientBlockType.CubeShaderEffect = ClientBlockType.ClientShaderEffect.Water;
			}
			else if (val.ShaderEffect.Contains((ShaderType)6))
			{
				clientBlockType.CubeShaderEffect = ClientBlockType.ClientShaderEffect.Lava;
			}
			else if (val.ShaderEffect.Contains((ShaderType)7))
			{
				clientBlockType.CubeShaderEffect = ClientBlockType.ClientShaderEffect.Slime;
			}
			else
			{
				clientBlockType.CubeShaderEffect = ClientBlockType.ClientShaderEffect.None;
			}
			if (val.ShaderEffect.Contains((ShaderType)2))
			{
				clientBlockType.BlockyModelShaderEffect = ClientBlockType.ClientShaderEffect.WindAttached;
			}
			else if (val.ShaderEffect.Contains((ShaderType)1))
			{
				clientBlockType.BlockyModelShaderEffect = ClientBlockType.ClientShaderEffect.Wind;
			}
			else if (val.ShaderEffect.Contains((ShaderType)8))
			{
				clientBlockType.BlockyModelShaderEffect = ClientBlockType.ClientShaderEffect.Ripple;
			}
			else
			{
				clientBlockType.BlockyModelShaderEffect = ClientBlockType.ClientShaderEffect.None;
			}
			clientBlockType.FluidBlockId = (ushort)val.Fluid;
			clientBlockType.FluidFXIndex = val.FluidFXIndex;
			if (val.VariantOriginalId != num)
			{
				ClientBlockType clientBlockType2 = upcomingBlockTypes[val.VariantOriginalId];
				if (clientBlockType2 == null)
				{
					if (!networkBlockTypes.ContainsKey(val.VariantOriginalId))
					{
						throw new Exception("Missing original block type");
					}
					if (!dictionary3.TryGetValue(val.VariantOriginalId, out var value2))
					{
						value2 = (dictionary3[val.VariantOriginalId] = new List<int>());
					}
					value2.Add(num);
				}
				else
				{
					int capacity = clientBlockType.Name.Length - clientBlockType2.Name.Length;
					StringBuilder stringBuilder = new StringBuilder(capacity);
					string[] source = clientBlockType2.Name.Split(new char[1] { '|' });
					string[] array9 = clientBlockType.Name.Split(new char[1] { '|' });
					foreach (string value3 in array9)
					{
						if (!source.Contains(value3))
						{
							if (stringBuilder.Length > 0)
							{
								stringBuilder.Append('|');
							}
							stringBuilder.Append(value3);
						}
					}
					clientBlockType2.Variants[stringBuilder.ToString()] = num;
				}
			}
			if (dictionary3.TryGetValue(num, out var value4))
			{
				foreach (int item in value4)
				{
					ClientBlockType clientBlockType3 = upcomingBlockTypes[item];
					int capacity2 = clientBlockType3.Name.Length - clientBlockType.Name.Length;
					StringBuilder stringBuilder2 = new StringBuilder(capacity2);
					string[] source2 = clientBlockType.Name.Split(new char[1] { '|' });
					string[] array10 = clientBlockType3.Name.Split(new char[1] { '|' });
					foreach (string value5 in array10)
					{
						if (!source2.Contains(value5))
						{
							if (stringBuilder2.Length > 0)
							{
								stringBuilder2.Append('|');
							}
							stringBuilder2.Append(value5);
						}
					}
					clientBlockType.Variants[stringBuilder2.ToString()] = num;
				}
			}
			clientBlockType.BlockSoundSetIndex = val.BlockSoundSetIndex;
			clientBlockType.BlockParticleSetId = val.BlockParticleSetId;
			if (val.ParticleColor != null)
			{
				clientBlockType.ParticleColor = UInt32Color.FromRGBA((byte)val.ParticleColor.Red, (byte)val.ParticleColor.Green, (byte)val.ParticleColor.Blue, byte.MaxValue);
			}
			if (!flag && val.Particles != null)
			{
				clientBlockType.Particles = new ModelParticleSettings[val.Particles.Length];
				int num12 = 0;
				for (int num13 = 0; num13 < val.Particles.Length; num13++)
				{
					if (val.Particles[num13].SystemId != null)
					{
						ModelParticleSettings clientModelParticle = new ModelParticleSettings();
						ParticleProtocolInitializer.Initialize(val.Particles[num13], ref clientModelParticle, _gameInstance.EntityStoreModule.NodeNameManager);
						if (!clientBlockType.ParticleColor.IsTransparent && clientModelParticle.Color.IsTransparent)
						{
							clientModelParticle.Color = clientBlockType.ParticleColor;
						}
						clientBlockType.Particles[num12] = clientModelParticle;
						num12++;
					}
				}
				if (num12 != val.Particles.Length)
				{
					Array.Resize(ref clientBlockType.Particles, num12);
				}
			}
			clientBlockType.Interactions = val.Interactions;
			if (!flag)
			{
				clientBlockType.SoundEventIndex = ResourceManager.GetNetworkWwiseId(val.SoundEventIndex);
			}
			clientBlockType.Looping = val.Looping;
			clientBlockType.VariantRotation = val.VariantRotation_;
			clientBlockType.VariantOriginalId = val.VariantOriginalId;
			clientBlockType.Connections = val.Connections;
			clientBlockType.States = val.States;
			clientBlockType.StatesReverse = val.States?.ToDictionary((KeyValuePair<string, int> p) => p.Value, (KeyValuePair<string, int> p) => p.Key);
			clientBlockType.TagIndexes = val.TagIndexes;
			if ((int)clientBlockType.DrawType == 0 || (clientBlockType.ShouldRenderCube && clientBlockType.BlockyModelHash == null))
			{
				clientBlockType.OriginalBlockyModel = new BlockyModel(0);
			}
			if (clientBlockType.HasModel)
			{
				if (clientBlockType.VariantOriginalId == num)
				{
					if (clientBlockType.BlockyModelHash == null)
					{
						clientBlockType.OriginalBlockyModel = blockyModel;
					}
					else if (!dictionary.TryGetValue(clientBlockType.BlockyModelHash, out clientBlockType.OriginalBlockyModel))
					{
						try
						{
							BlockyModel blockyModel2 = new BlockyModel(BlockyModel.MaxNodeCount);
							BlockyModelInitializer.Parse(AssetManager.GetAssetUsingHash(clientBlockType.BlockyModelHash), _gameInstance.EntityStoreModule.NodeNameManager, ref blockyModel2);
							BlockyModel originalBlockyModel = (dictionary[clientBlockType.BlockyModelHash] = blockyModel2);
							clientBlockType.OriginalBlockyModel = originalBlockyModel;
						}
						catch (Exception ex)
						{
							Logger.Error(ex, "Failed to parse model " + val.Model + " for block " + clientBlockType.Name);
							clientBlockType.OriginalBlockyModel = blockyModel;
						}
					}
					clientBlockType.RenderedBlockyModel = new RenderedStaticBlockyModel(clientBlockType.OriginalBlockyModel);
				}
				else
				{
					ClientBlockType clientBlockType4 = upcomingBlockTypes[val.VariantOriginalId];
					clientBlockType.OriginalBlockyModel = clientBlockType4.OriginalBlockyModel;
					clientBlockType.RenderedBlockyModel = clientBlockType4.RenderedBlockyModel;
				}
			}
			if (clientBlockType.Particles != null)
			{
				for (int num14 = 0; num14 < clientBlockType.Particles.Length; num14++)
				{
					if (clientBlockType.RenderedBlockyModel != null)
					{
						int value6 = 0;
						if (clientBlockType.Particles[num14].TargetNodeNameId != -1)
						{
							clientBlockType.OriginalBlockyModel.NodeIndicesByNameId.TryGetValue(clientBlockType.Particles[num14].TargetNodeNameId, out value6);
						}
						else
						{
							clientBlockType.Particles[num14].TargetNodeNameId = clientBlockType.OriginalBlockyModel.AllNodes[0].NameId;
						}
						clientBlockType.Particles[num14].TargetNodeIndex = value6;
					}
				}
			}
			clientBlockType.VertexData = new ChunkGeometryData();
			if (clientBlockType.RenderedBlockyModel != null)
			{
				clientBlockType.VertexData.VerticesCount += clientBlockType.RenderedBlockyModel.AnimatedVertices.Length;
				clientBlockType.VertexData.IndicesCount += clientBlockType.RenderedBlockyModel.AnimatedIndices.Length;
			}
			if (clientBlockType.ShouldRenderCube)
			{
				clientBlockType.VertexData.VerticesCount += 24;
				clientBlockType.VertexData.IndicesCount += 36;
			}
			clientBlockType.VertexData.Vertices = new ChunkVertex[clientBlockType.VertexData.VerticesCount];
			clientBlockType.VertexData.Indices = new uint[clientBlockType.VertexData.IndicesCount];
			if (clientBlockType.RenderedBlockyModel != null)
			{
				for (int num15 = 0; num15 < clientBlockType.RenderedBlockyModel.AnimatedIndices.Length; num15++)
				{
					clientBlockType.VertexData.Indices[clientBlockType.VertexData.IndicesOffset + num15] = clientBlockType.VertexData.VerticesOffset + clientBlockType.RenderedBlockyModel.AnimatedIndices[num15];
				}
				clientBlockType.VertexData.IndicesOffset += clientBlockType.RenderedBlockyModel.AnimatedIndices.Length;
			}
			upcomingBlockTypes[num] = clientBlockType;
		}
		UShortVector2[] texCoordsByCorner = new UShortVector2[4];
		UShortVector2[] sideMaskTexCoordsByCorner = new UShortVector2[4];
		int[] cornerOcclusions = new int[4];
		ClientBlockType.ClientShaderEffect[] cornerShaderEffects = new ClientBlockType.ClientShaderEffect[4];
		uint[] array11 = new uint[1156];
		uint[] array12 = new uint[8];
		for (int num16 = 0; num16 < array12.Length; num16++)
		{
			array12[num16] = 301989887u;
		}
		int num17 = ChunkHelper.IndexOfBlockInBorderedChunk(1, 1, 1);
		int[] array13 = new int[39304];
		ushort[] array14 = new ushort[39304];
		for (int num18 = 0; num18 < 39304; num18++)
		{
			array14[num18] = 61440;
		}
		if (!_gameInstance.HashesByServerAssetPath.TryGetValue("BlockTextures/Unknown.png", out var value7))
		{
			value7 = "";
			_gameInstance.App.DevTools.Error("Missing block texture: BlockTextures/Unknown.png");
		}
		BlockyModelTextureInfo blockyModelTextureInfo = new BlockyModelTextureInfo
		{
			AtlasLocation = new AtlasLocation
			{
				Position = Point.Zero,
				Size = new Point(32, 32)
			},
			Hash = value7,
			ServerPath = "BlockTextures/Unknown.png"
		};
		if (atlasNeedsUpdate)
		{
			Dictionary<string, int> dictionary4 = new Dictionary<string, int> { { value7, 0 } };
			Dictionary<string, BlockyModelTextureInfo> dictionary5 = new Dictionary<string, BlockyModelTextureInfo>();
			upcomingBlocksImageLocations.Clear();
			for (int num19 = 0; num19 < upcomingBlockTypes.Length; num19++)
			{
				ClientBlockType clientBlockType5 = upcomingBlockTypes[num19];
				if (clientBlockType5.ShouldRenderCube)
				{
					for (int num20 = 0; num20 < clientBlockType5.CubeTextures.Length; num20++)
					{
						ClientBlockType.CubeTexture cubeTexture16 = clientBlockType5.CubeTextures[num20];
						for (int num21 = 0; num21 < cubeTexture16.Names.Length; num21++)
						{
							string text2 = cubeTexture16.Names[num21];
							int textureIndex = GetTextureIndex(dictionary4, text2);
							upcomingBlocksImageLocations[text2] = new AtlasLocation
							{
								TileIndex = textureIndex
							};
							if (textureIndex == 0 && text2 != "BlockTextures/Unknown.png")
							{
								_gameInstance.App.DevTools.Error("Missing texture asset: " + text2 + " for block " + clientBlockType5.Name);
							}
						}
					}
					if (!string.IsNullOrEmpty(clientBlockType5.CubeSideMaskTexture))
					{
						int textureIndex2 = GetTextureIndex(dictionary4, clientBlockType5.CubeSideMaskTexture);
						upcomingBlocksImageLocations[clientBlockType5.CubeSideMaskTexture] = new AtlasLocation
						{
							TileIndex = textureIndex2
						};
						if (textureIndex2 == 0)
						{
							_gameInstance.App.DevTools.Error("Missing texture asset: " + clientBlockType5.CubeSideMaskTexture + " for block " + clientBlockType5.Name);
						}
					}
					if (!string.IsNullOrEmpty(clientBlockType5.TransitionTexture))
					{
						int textureIndex3 = GetTextureIndex(dictionary4, clientBlockType5.TransitionTexture);
						upcomingBlocksImageLocations[clientBlockType5.TransitionTexture] = new AtlasLocation
						{
							TileIndex = textureIndex3
						};
						if (textureIndex3 == 0)
						{
							_gameInstance.App.DevTools.Error("Missing texture asset: " + clientBlockType5.TransitionTexture + " for block " + clientBlockType5.Name);
						}
					}
				}
				if (!clientBlockType5.HasModel)
				{
					continue;
				}
				for (int num22 = 0; num22 < clientBlockType5.BlockyTextures.Length; num22++)
				{
					string hash = clientBlockType5.BlockyTextures[num22].Hash;
					if (!string.IsNullOrEmpty(hash) && !dictionary5.TryGetValue(hash, out var value8))
					{
						value8 = new BlockyModelTextureInfo
						{
							ServerPath = clientBlockType5.BlockyTextures[num22].Name,
							Hash = hash
						};
						string assetLocalPathUsingHash = AssetManager.GetAssetLocalPathUsingHash(hash);
						if (Image.TryGetPngDimensions(assetLocalPathUsingHash, out value8.AtlasLocation.Size.X, out value8.AtlasLocation.Size.Y))
						{
							dictionary5[hash] = value8;
							continue;
						}
						_gameInstance.App.DevTools.Error("Failed to get PNG dimensions for: " + assetLocalPathUsingHash + " (" + hash + ")");
					}
				}
			}
			int num23 = (int)System.Math.Ceiling((float)dictionary4.Count / (float)(TextureAtlas.Width / 32)) * 32;
			atlasSize.Y = 32;
			while (atlasSize.Y < num23)
			{
				atlasSize.Y <<= 1;
			}
			Point position = new Point(0, num23);
			List<BlockyModelTextureInfo> list3 = new List<BlockyModelTextureInfo>(dictionary5.Values);
			list3.Sort((BlockyModelTextureInfo a, BlockyModelTextureInfo b) => b.AtlasLocation.Size.Y.CompareTo(a.AtlasLocation.Size.Y));
			foreach (BlockyModelTextureInfo item2 in list3)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					upcomingAtlasPixelsPerLevel = null;
					return;
				}
				int x = item2.AtlasLocation.Size.X;
				int y = item2.AtlasLocation.Size.Y;
				if (x % 32 != 0 || y % 32 != 0 || x < 32 || y < 32)
				{
					_gameInstance.App.DevTools.Warn($"Texture width/height must be a multiple of 32 and at least 32x32: {item2.ServerPath} ({x}x{y})");
				}
				if (position.X + x > atlasSize.X)
				{
					position.X = 0;
					position.Y = num23;
				}
				while (position.Y + y > atlasSize.Y)
				{
					atlasSize.Y <<= 1;
				}
				item2.AtlasLocation.Position = position;
				num23 = System.Math.Max(num23, position.Y + y);
				position.X += x;
			}
			byte[] array15 = new byte[atlasSize.X * atlasSize.Y * 4];
			position = Point.Zero;
			string[] array16 = dictionary4.Keys.ToArray();
			foreach (string text3 in array16)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					upcomingAtlasPixelsPerLevel = null;
					return;
				}
				try
				{
					Image image = new Image(AssetManager.GetAssetUsingHash(text3));
					if (image.Width != 32 || image.Height != 32)
					{
						_gameInstance.App.DevTools.Warn($"Invalid block texture size, must be {32}: {AssetManager.GetAssetLocalPathUsingHash(text3)} ({image.Width}x{image.Height})");
					}
					for (int num25 = 0; num25 < image.Height; num25++)
					{
						int dstOffset = ((position.Y + num25) * atlasSize.X + position.X) * 4;
						Buffer.BlockCopy(image.Pixels, num25 * image.Width * 4, array15, dstOffset, image.Width * 4);
					}
					upcomingBlocksImageLocations[text3] = new AtlasLocation
					{
						Position = position,
						Size = new Point(image.Width, image.Height)
					};
				}
				catch (Exception ex2)
				{
					Logger.Error(ex2, "Block texture not found: " + AssetManager.GetAssetLocalPathUsingHash(text3));
				}
				position.X += 32;
				if (position.X >= atlasSize.X)
				{
					position.X = 0;
					position.Y += 32;
				}
			}
			foreach (BlockyModelTextureInfo item3 in list3)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					upcomingAtlasPixelsPerLevel = null;
					return;
				}
				if (!upcomingBlocksImageLocations.TryGetValue(item3.Hash, out var value9))
				{
					upcomingBlocksImageLocations[item3.Hash] = item3.AtlasLocation;
					Image image2 = null;
					try
					{
						image2 = new Image(AssetManager.GetAssetUsingHash(item3.Hash));
						for (int num26 = 0; num26 < image2.Height; num26++)
						{
							int dstOffset2 = ((item3.AtlasLocation.Position.Y + num26) * atlasSize.X + item3.AtlasLocation.Position.X) * 4;
							Buffer.BlockCopy(image2.Pixels, num26 * image2.Width * 4, array15, dstOffset2, image2.Width * 4);
						}
					}
					catch (Exception ex3)
					{
						if (image2 != null)
						{
							throw new Exception("Invalid texture! " + $"TextureInfo: {item3.ServerPath}, {item3.Hash}, ({item3.AtlasLocation.Size.X}, {item3.AtlasLocation.Size.Y}), " + $"Image: ({image2.Width}, {image2.Height}), {image2.Pixels.Length / 4}, " + $"AtlasPosition: ({item3.AtlasLocation.Position.X} {item3.AtlasLocation.Position.Y}), " + $"TextureAtlas: ({atlasSize.X} {atlasSize.Y})", ex3);
						}
						Logger.Error(ex3, "Failed to load block texture: " + AssetManager.GetAssetLocalPathUsingHash(item3.Hash));
					}
				}
				else
				{
					item3.AtlasLocation = value9;
				}
			}
			upcomingAtlasPixelsPerLevel = Texture.BuildMipmapPixels(array15, TextureAtlas.Width, TextureAtlas.MipmapLevelCount);
			for (int num27 = 0; num27 < upcomingBlockTypes.Length; num27++)
			{
				ClientBlockType clientBlockType6 = upcomingBlockTypes[num27];
				if (clientBlockType6 == null || cancellationToken.IsCancellationRequested)
				{
					break;
				}
				FinishBlockTypeModelPreparation(clientBlockType6, upcomingBlocksImageLocations, blockyModelTextureInfo.AtlasLocation, atlasSize);
				int alphaTestedLowLODIndicesOffset = 0;
				int? seed = 0;
				int num28 = 32;
				uint num29 = (uint)clientBlockType6.SelfTintColorsBySide[0];
				if (!ChunkGeometryBuilder.NoTint.Equals(ChunkGeometryBuilder.ForceTint))
				{
				}
				array11[0] = num29;
				array11[1] = num29;
				array11[34] = num29;
				array11[35] = num29;
				array13[num17] = clientBlockType6.Id;
				clientBlockType6.VertexData.VerticesOffset = 0u;
				clientBlockType6.VertexData.IndicesOffset = 0;
				ChunkGeometryBuilder.CreateBlockGeometry(upcomingBlockTypes, LightLevels, clientBlockType6, num17, num28, Vector3.Zero, 0, 0, 0, ref seed, byte.MaxValue, Matrix.Identity, clientBlockType6.RotationMatrix, clientBlockType6.CubeBlockInvertMatrix, texCoordsByCorner, sideMaskTexCoordsByCorner, cornerOcclusions, cornerShaderEffects, num29, array13, array14, array11, array12, atlasSize.X, atlasSize.Y, clientBlockType6.VertexData, clientBlockType6.VertexData, alphaTestedLowLODIndicesOffset, ref alphaTestedLowLODIndicesOffset, isAnimated: true);
			}
			return;
		}
		upcomingAtlasPixelsPerLevel = null;
		foreach (int key in networkBlockTypes.Keys)
		{
			ClientBlockType clientBlockType7 = upcomingBlockTypes[key];
			if (clientBlockType7 == null || cancellationToken.IsCancellationRequested)
			{
				break;
			}
			FinishBlockTypeModelPreparation(clientBlockType7, upcomingBlocksImageLocations, blockyModelTextureInfo.AtlasLocation, atlasSize);
			int alphaTestedLowLODIndicesOffset2 = 0;
			int? seed2 = 0;
			int num30 = 32;
			uint biomeTintColor = (array11[35] = (array11[34] = (array11[1] = (array11[0] = (uint)clientBlockType7.SelfTintColorsBySide[0]))));
			array13[num17] = clientBlockType7.Id;
			clientBlockType7.VertexData.VerticesOffset = 0u;
			clientBlockType7.VertexData.IndicesOffset = 0;
			ChunkGeometryBuilder.CreateBlockGeometry(upcomingBlockTypes, LightLevels, clientBlockType7, num17, num30, Vector3.Zero, 0, 0, 0, ref seed2, byte.MaxValue, Matrix.Identity, clientBlockType7.RotationMatrix, clientBlockType7.CubeBlockInvertMatrix, texCoordsByCorner, sideMaskTexCoordsByCorner, cornerOcclusions, cornerShaderEffects, biomeTintColor, array13, array14, array11, array12, atlasSize.X, atlasSize.Y, clientBlockType7.VertexData, clientBlockType7.VertexData, alphaTestedLowLODIndicesOffset2, ref alphaTestedLowLODIndicesOffset2, isAnimated: true);
		}
	}

	private void FinishBlockTypeModelPreparation(ClientBlockType blockType, Dictionary<string, AtlasLocation> upcomingBlocksImageLocations, AtlasLocation unknownBlockAtlasLocation, Point atlasSize)
	{
		blockType.FinalBlockyModel = blockType.OriginalBlockyModel.Clone();
		if (blockType.HasModel)
		{
			if (blockType.BlockyTextures[0].Hash == null || !upcomingBlocksImageLocations.TryGetValue(blockType.BlockyTextures[0].Hash, out var value))
			{
				value = unknownBlockAtlasLocation;
			}
			blockType.FinalBlockyModel.OffsetUVs(value.Position);
			blockType.RenderedBlockyModel.PrepareUVs(blockType.OriginalBlockyModel, value.Size, atlasSize);
			blockType.RenderedBlockyModelTextureOrigins = new Vector2[blockType.BlockyTextures.Length];
			for (int i = 0; i < blockType.BlockyTextures.Length; i++)
			{
				if (blockType.BlockyTextures[i].Hash == null || !upcomingBlocksImageLocations.TryGetValue(blockType.BlockyTextures[i].Hash, out var value2))
				{
					value2 = unknownBlockAtlasLocation;
				}
				blockType.RenderedBlockyModelTextureOrigins[i] = new Vector2((float)value2.Position.X / (float)atlasSize.X, (float)value2.Position.Y / (float)atlasSize.Y);
			}
		}
		if (blockType.ShouldRenderCube)
		{
			for (int j = 0; j < blockType.CubeTextures.Length; j++)
			{
				ClientBlockType.CubeTexture cubeTexture = blockType.CubeTextures[j];
				for (int k = 0; k < cubeTexture.Names.Length; k++)
				{
					cubeTexture.TileLinearPositionsInAtlas[k] = upcomingBlocksImageLocations[cubeTexture.Names[k]].TileIndex;
				}
			}
			if (!string.IsNullOrEmpty(blockType.CubeSideMaskTexture))
			{
				blockType.CubeSideMaskTextureAtlasIndex = upcomingBlocksImageLocations[blockType.CubeSideMaskTexture].TileIndex;
			}
			else
			{
				blockType.CubeSideMaskTextureAtlasIndex = -1;
			}
			if (!string.IsNullOrEmpty(blockType.TransitionTexture))
			{
				blockType.TransitionTextureAtlasIndex = upcomingBlocksImageLocations[blockType.TransitionTexture].TileIndex;
			}
			else
			{
				blockType.TransitionTextureAtlasIndex = -1;
			}
			blockType.FinalBlockyModel.AddMapBlockNode(blockType, CharacterPartStore.BlockNameId, CharacterPartStore.SideMaskNameId, atlasSize.X);
		}
		blockType.FinalBlockyModel.SetAtlasIndex(0);
	}

	public void SetupBlockTypes(ClientBlockType[] blockTypes, bool rebuildAllChunks = true)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		DoWithMapGeometryBuilderPaused(rebuildAllChunks, delegate
		{
			ClientBlockTypes = blockTypes;
		});
	}

	private ClientBlockType.CubeTexture CreateBlockTexture(ClientBlockType blockType, string[] networkBlockTextures)
	{
		ClientBlockType.CubeTexture cubeTexture = new ClientBlockType.CubeTexture();
		cubeTexture.Names = networkBlockTextures;
		cubeTexture.TileLinearPositionsInAtlas = new int[System.Math.Max(1, networkBlockTextures.Length)];
		return cubeTexture;
	}

	private int GetTextureIndex(Dictionary<string, int> textures, string texturePath)
	{
		if (texturePath == null || !_gameInstance.HashesByServerAssetPath.TryGetValue(texturePath, out var value))
		{
			return 0;
		}
		if (!textures.TryGetValue(value, out var value2))
		{
			value2 = textures.Count;
			textures.Add(value, value2);
		}
		return value2;
	}

	public int GetClientBlockIdFromName(string blockName)
	{
		blockName = blockName.ToLower();
		return Array.IndexOf<ClientBlockType>(ClientBlockTypes, Array.Find(ClientBlockTypes, (ClientBlockType p) => p.Name.ToLower() == blockName));
	}

	public ClientBlockType GetClientBlockTypeFromName(string blockName)
	{
		blockName = blockName.ToLower();
		return Array.Find(ClientBlockTypes, (ClientBlockType p) => p.Name.ToLower() == blockName);
	}

	public Vector3 GetBlockEnvironmentTint(int blockX, int blockY, int blockZ, ClientBlockType blockType)
	{
		Vector3 result = new Vector3(255f);
		int num = blockType.SelfTintColorsBySide[0];
		byte b = (byte)(num >> 16);
		byte b2 = (byte)(num >> 8);
		byte b3 = (byte)num;
		int num2 = blockX >> 5;
		int num3 = blockZ >> 5;
		int num4 = blockX - num2 * 32;
		int num5 = blockZ - num3 * 32;
		ChunkColumn chunkColumn = GetChunkColumn(num2, num3);
		if (chunkColumn == null)
		{
			return result;
		}
		uint num6 = chunkColumn.Tints[(num5 << 5) + num4];
		byte b4 = (byte)(num6 >> 16);
		byte b5 = (byte)(num6 >> 8);
		byte b6 = (byte)num6;
		float num7 = blockType.BiomeTintMultipliersBySide[0];
		result.X = (uint)((float)(int)b + (float)(b4 - b) * num7);
		result.Y = (uint)((float)(int)b2 + (float)(b5 - b2) * num7);
		result.Z = (uint)((float)(int)b3 + (float)(b6 - b3) * num7);
		return result;
	}

	public Vector3 GetBlockFluidTint(int blockX, int blockY, int blockZ, ClientBlockType blockType)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected I4, but got Unknown
		Vector3 result = new Vector3(255f);
		if (blockType.FluidFXIndex == 0)
		{
			return result;
		}
		FluidFX val = _gameInstance.ServerSettings.FluidFXs[blockType.FluidFXIndex];
		FluidFog fogMode = val.FogMode;
		FluidFog val2 = fogMode;
		switch ((int)val2)
		{
		case 2:
		{
			int num = blockX >> 5;
			int num2 = blockZ >> 5;
			int num3 = blockX - num * 32;
			int num4 = blockZ - num2 * 32;
			ChunkColumn chunkColumn = GetChunkColumn(num, num2);
			if (chunkColumn == null)
			{
				return result;
			}
			ushort environmentId = ChunkHelper.GetEnvironmentId(chunkColumn.Environments, num3, num4, blockY);
			int waterTint = _gameInstance.ServerSettings.Environments[environmentId].WaterTint;
			if (waterTint == -1)
			{
				Vector4 lightColorAtBlockPosition = GetLightColorAtBlockPosition(blockX, blockY, blockZ);
				result.X = _gameInstance.WeatherModule.WaterTintColor.X * 255f * lightColorAtBlockPosition.X;
				result.Y = _gameInstance.WeatherModule.WaterTintColor.Y * 255f * lightColorAtBlockPosition.Y;
				result.Z = _gameInstance.WeatherModule.WaterTintColor.Z * 255f * lightColorAtBlockPosition.Z;
				break;
			}
			float num5 = (float)(int)(byte)(waterTint >> 16) / 255f;
			float num6 = (float)(int)(byte)(waterTint >> 8) / 255f;
			float num7 = (float)(int)(byte)waterTint / 255f;
			uint num8 = chunkColumn.Tints[(num4 << 5) + num3];
			byte b = (byte)(num8 >> 16);
			byte b2 = (byte)(num8 >> 8);
			byte b3 = (byte)num8;
			int num9 = blockType.SelfTintColorsBySide[0];
			byte b4 = (byte)(num9 >> 16);
			byte b5 = (byte)(num9 >> 8);
			byte b6 = (byte)num9;
			float num10 = blockType.BiomeTintMultipliersBySide[0];
			uint num11 = (uint)((float)(int)b4 + (float)(b - b4) * num10);
			uint num12 = (uint)((float)(int)b5 + (float)(b2 - b5) * num10);
			uint num13 = (uint)((float)(int)b6 + (float)(b3 - b6) * num10);
			result.X = (uint)((float)num11 * num5);
			result.Y = (uint)((float)num12 * num6);
			result.Z = (uint)((float)num13 * num7);
			break;
		}
		case 1:
		{
			Vector4 lightColorAtBlockPosition = GetLightColorAtBlockPosition(blockX, blockY, blockZ);
			result.X = (float)(int)(byte)val.FogColor.Red * lightColorAtBlockPosition.X;
			result.Y = (float)(int)(byte)val.FogColor.Green * lightColorAtBlockPosition.Y;
			result.Z = (float)(int)(byte)val.FogColor.Blue * lightColorAtBlockPosition.Z;
			break;
		}
		case 0:
			result.X = (int)(byte)val.FogColor.Red;
			result.Y = (int)(byte)val.FogColor.Green;
			result.Z = (int)(byte)val.FogColor.Blue;
			break;
		}
		return result;
	}

	public ChunkColumn GetChunkColumn(long indexChunk)
	{
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(MapModule).FullName);
		}
		_chunkColumns.TryGetValue(indexChunk, out var value);
		if (value == null || value.Tints == null || value.Heights == null || value.Environments == null)
		{
			return null;
		}
		return value;
	}

	public ChunkColumn GetChunkColumn(int worldChunkX, int worldChunkZ)
	{
		return GetChunkColumn(ChunkHelper.IndexOfChunkColumn(worldChunkX, worldChunkZ));
	}

	public ChunkColumn GetOrCreateChunkColumn(int worldChunkX, int worldChunkZ)
	{
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(MapModule).FullName);
		}
		return _chunkColumns.GetOrAdd(ChunkHelper.IndexOfChunkColumn(worldChunkX, worldChunkZ), (long key) => new ChunkColumn(worldChunkX, worldChunkZ));
	}

	public Chunk GetChunk(int worldChunkX, int worldChunkY, int worldChunkZ)
	{
		return GetChunkColumn(worldChunkX, worldChunkZ)?.GetChunk(worldChunkY);
	}

	public BlockPosition GetBaseBlock(BlockPosition position)
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		int block = GetBlock(position.X, position.Y, position.Z, -1);
		if (block == -1)
		{
			return position;
		}
		ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[block];
		if (clientBlockType.FillerX != 0 || clientBlockType.FillerY != 0 || clientBlockType.FillerZ != 0)
		{
			return new BlockPosition(position.X - clientBlockType.FillerX, position.Y - clientBlockType.FillerY, position.Z - clientBlockType.FillerZ);
		}
		return position;
	}

	public int GetBlock(Vector3 position, int undefinedBlockId)
	{
		return GetBlock((int)System.Math.Floor(position.X), (int)System.Math.Floor(position.Y), (int)System.Math.Floor(position.Z), undefinedBlockId);
	}

	public int GetBlock(int worldX, int worldY, int worldZ, int undefinedBlockId)
	{
		if (worldY < 0 || worldY >= ChunkHelper.Height)
		{
			return 0;
		}
		int worldChunkX = worldX >> 5;
		int worldChunkY = worldY >> 5;
		int worldChunkZ = worldZ >> 5;
		return GetChunk(worldChunkX, worldChunkY, worldChunkZ)?.Data.GetBlock(worldX, worldY, worldZ) ?? undefinedBlockId;
	}

	public void SetBlock(int worldX, int worldY, int worldZ, int newBlockId, bool playInteractionStateSound)
	{
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Invalid comparison between Unknown and I4
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Invalid comparison between Unknown and I4
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0391: Invalid comparison between Unknown and I4
		Debug.Assert(_gameInstance.IsOnPacketHandlerThread);
		int num = worldX >> 5;
		int num2 = worldY >> 5;
		int num3 = worldZ >> 5;
		ChunkColumn chunkColumn = GetChunkColumn(num, num3);
		if (chunkColumn == null)
		{
			return;
		}
		lock (chunkColumn.DisposeLock)
		{
			if (chunkColumn.Disposed)
			{
				return;
			}
			Chunk chunk = chunkColumn.GetChunk(num2);
			if (chunk == null)
			{
				return;
			}
			int blockId = newBlockId;
			if (blockId >= ClientBlockTypes.Length)
			{
				blockId = 1;
				_gameInstance.App.DevTools.Error($"Invalid block set in chunk ({num}, {num2}, {num3}) at ({worldX} {worldY} {worldZ}) to {newBlockId} (max value {ClientBlockTypes.Length - 1})");
			}
			chunk.Data.SetBlock(worldX, worldY, worldZ, blockId);
			if (blockId == 0)
			{
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					RegisterDestroyedBlock(worldX, worldY, worldZ);
					int key = ChunkHelper.IndexOfWorldBlockInChunk(worldX, worldY, worldZ);
					if (chunk.Data.CurrentInteractionStates.ContainsKey(key))
					{
						ChunkData.InteractionStateInfo interactionStateInfo = chunk.Data.CurrentInteractionStates[key];
						if (interactionStateInfo.SoundEventReference.SoundObjectReference.SoundObjectId != 0)
						{
							_gameInstance.AudioModule.ActionOnEvent(ref interactionStateInfo.SoundEventReference, (AkActionOnEventType)0);
							_gameInstance.AudioModule.UnregisterSoundObject(ref interactionStateInfo.SoundEventReference.SoundObjectReference);
						}
						chunk.Data.CurrentInteractionStates.Remove(key);
					}
				});
			}
			ClientBlockType clientBlockType = ClientBlockTypes[blockId];
			if (clientBlockType.BlockyAnimation != null)
			{
				_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
				{
					HandleBlockInteractionState(worldX, worldY, worldZ, blockId, playInteractionStateSound);
				});
			}
			int num4 = worldX - num * 32;
			int num5 = worldZ - num3 * 32;
			int num6 = (num5 << 5) + num4;
			ushort num7 = chunkColumn.Heights[num6];
			int val = worldY;
			if (worldY > num7 && (int)clientBlockType.Opacity != 3)
			{
				chunkColumn.Heights[num6] = (ushort)worldY;
			}
			else if (worldY == num7 && (int)clientBlockType.Opacity == 3)
			{
				int num8 = worldY >> 5;
				Chunk chunk2 = chunk;
				for (int num9 = worldY; num9 >= 0; num9--)
				{
					int num10 = num9 >> 5;
					if (num8 != num10)
					{
						num8 = num10;
						chunk2 = chunkColumn.GetChunk(num10);
					}
					int block = chunk2.Data.GetBlock(worldX, num9, worldZ);
					if ((int)ClientBlockTypes[block].Opacity != 3)
					{
						val = (ushort)num9;
						chunkColumn.Heights[num6] = (ushort)num9;
						break;
					}
				}
			}
			int num11 = System.Math.Min(num7, val) >> 5;
			int num12 = System.Math.Max(num7, val) >> 5;
			for (int i = num11; i <= num12; i++)
			{
				Chunk chunk3 = chunkColumn.GetChunk(i);
				if (chunk3 != null)
				{
					chunk3.Data.SelfLightNeedsUpdate = true;
				}
			}
			for (int j = -1; j <= 1; j++)
			{
				for (int k = -1; k <= 1; k++)
				{
					int worldChunkX = worldX + k * 16 >> 5;
					int worldChunkZ = worldZ + j * 16 >> 5;
					ChunkColumn chunkColumn2 = GetChunkColumn(worldChunkX, worldChunkZ);
					if (chunkColumn2 == null)
					{
						continue;
					}
					for (int l = num11 - 1; l <= num12 + 1; l++)
					{
						RenderedChunk renderedChunk = chunkColumn2.GetChunk(l)?.Rendered;
						if (renderedChunk != null)
						{
							renderedChunk.GeometryNeedsUpdate = true;
						}
					}
				}
			}
		}
	}

	private void HandleBlockInteractionState(int worldX, int worldY, int worldZ, int blockId, bool playInteractionStateSound)
	{
		int worldChunkX = worldX >> 5;
		int worldChunkY = worldY >> 5;
		int worldChunkZ = worldZ >> 5;
		Chunk chunk = _gameInstance.MapModule.GetChunk(worldChunkX, worldChunkY, worldChunkZ);
		if (chunk != null)
		{
			int blockIndex = ChunkHelper.IndexOfWorldBlockInChunk(worldX, worldY, worldZ);
			SetBlockInteractionState(chunk, blockId, blockIndex, new Vector3(worldX, worldY, worldZ), isDone: false, playInteractionStateSound);
		}
	}

	public void SetBlockHitTimer(int worldX, int worldY, int worldZ, float hitTimer)
	{
		int num = worldX >> 5;
		int num2 = worldY >> 5;
		int num3 = worldZ >> 5;
		ChunkColumn chunkColumn = GetChunkColumn(num, num3);
		if (chunkColumn == null)
		{
			return;
		}
		lock (chunkColumn.DisposeLock)
		{
			if (chunkColumn.Disposed)
			{
				return;
			}
			Chunk chunk = chunkColumn.GetChunk(num2);
			if (chunk == null)
			{
				return;
			}
			chunk.Data.SetBlockHitTimer(ChunkHelper.IndexOfWorldBlockInChunk(worldX, worldY, worldZ), hitTimer);
			if (chunk.Rendered != null)
			{
				chunk.Rendered.GeometryNeedsUpdate = true;
			}
		}
		int num4 = worldX - num * 32;
		ChunkColumn chunkColumn2 = null;
		switch (num4)
		{
		case 0:
			chunkColumn2 = GetChunkColumn(num - 1, num3);
			break;
		case 31:
			chunkColumn2 = GetChunkColumn(num + 1, num3);
			break;
		}
		if (chunkColumn2 != null)
		{
			lock (chunkColumn2.DisposeLock)
			{
				if (!chunkColumn2.Disposed)
				{
					Chunk chunk2 = chunkColumn2.GetChunk(num2);
					if (chunk2?.Rendered != null)
					{
						chunk2.Rendered.GeometryNeedsUpdate = true;
					}
				}
			}
		}
		int num5 = worldY - num3 * 32;
		Chunk chunk3 = null;
		switch (num5)
		{
		case 0:
			chunk3 = chunkColumn.GetChunk(num2 - 1);
			break;
		case 31:
			chunk3 = chunkColumn.GetChunk(num2 + 1);
			break;
		}
		if (chunk3?.Rendered != null)
		{
			chunk3.Rendered.GeometryNeedsUpdate = true;
		}
		int num6 = worldZ - num3 * 32;
		ChunkColumn chunkColumn3 = null;
		switch (num6)
		{
		case 0:
			chunkColumn3 = GetChunkColumn(num, num3 - 1);
			break;
		case 31:
			chunkColumn3 = GetChunkColumn(num, num3 + 1);
			break;
		}
		if (chunkColumn3 == null)
		{
			return;
		}
		lock (chunkColumn3.DisposeLock)
		{
			if (!chunkColumn3.Disposed)
			{
				Chunk chunk4 = chunkColumn3.GetChunk(num2);
				if (chunk4?.Rendered != null)
				{
					chunk4.Rendered.GeometryNeedsUpdate = true;
				}
			}
		}
	}

	public Vector4 GetLightColorAtBlockPosition(int blockX, int blockY, int blockZ)
	{
		Vector4 result = new Vector4(_gameInstance.WeatherModule.SunlightColor * _gameInstance.WeatherModule.SunLight, 1f);
		if (blockY >= 0 && blockY < ChunkHelper.Height)
		{
			int worldChunkX = blockX >> 5;
			int worldChunkY = blockY >> 5;
			int worldChunkZ = blockZ >> 5;
			Chunk chunk = GetChunk(worldChunkX, worldChunkY, worldChunkZ);
			if (chunk != null && chunk.Data.BorderedLightAmounts != null)
			{
				int x = (blockX & 0x1F) + 1;
				int y = (blockY & 0x1F) + 1;
				int z = (blockZ & 0x1F) + 1;
				int num = ChunkHelper.IndexOfBlockInBorderedChunk(x, y, z);
				ushort num2 = chunk.Data.BorderedLightAmounts[num];
				Vector3 vector = new Vector3(LightLevels[num2 & 0xF], LightLevels[(num2 >> 4) & 0xF], LightLevels[(num2 >> 8) & 0xF]);
				int num3 = (num2 >> 12) & 0xF;
				result *= LightLevels[num3];
				result.X = System.Math.Max(vector.X * 2f, result.X);
				result.Y = System.Math.Max(vector.Y * 2f, result.Y);
				result.Z = System.Math.Max(vector.Z * 2f, result.Z);
			}
		}
		return result;
	}

	public void SetChunkBlocks(int worldChunkX, int worldChunkY, int worldChunkZ, byte[] data, int maxValidBlockTypeId, byte[] localLight, byte[] globalLight)
	{
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Expected I4, but got Unknown
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		Debug.Assert(_gameInstance.IsOnPacketHandlerThread);
		ChunkColumn orCreateChunkColumn = GetOrCreateChunkColumn(worldChunkX, worldChunkZ);
		Chunk chunk = orCreateChunkColumn.GetChunk(worldChunkY);
		if (chunk == null)
		{
			chunk = orCreateChunkColumn.CreateChunk(worldChunkY);
		}
		ushort[] array = ((localLight != null && localLight.Length != 0) ? DecodeLocalLightArray(localLight) : null);
		if (array != null)
		{
			chunk.Data.SelfLightNeedsUpdate = false;
			chunk.Data.SelfLightAmounts = array;
		}
		else
		{
			chunk.Data.SelfLightNeedsUpdate = true;
		}
		ushort[] array2 = ((globalLight != null && globalLight.Length != 0) ? DecodeGlobalLightArray(globalLight) : null);
		if (array2 != null)
		{
			chunk.Data.BorderedLightAmounts = array2;
		}
		IChunkData chunkData = EmptyPaletteChunkData.Instance;
		if (data != null)
		{
			using MemoryStream input = new MemoryStream(data, 0, data.Length, writable: false, publiclyVisible: true);
			using BinaryReader binaryReader = new BinaryReader(input);
			PaletteType val = (PaletteType)binaryReader.ReadByte();
			PaletteType val2 = val;
			PaletteType val3 = val2;
			switch (val3 - 1)
			{
			case 0:
				chunkData = new HalfBytePaletteChunkData();
				break;
			case 1:
				chunkData = new BytePaletteChunkData();
				break;
			case 2:
				chunkData = new ShortPaletteChunkData();
				break;
			}
			chunkData.Deserialize(binaryReader, maxValidBlockTypeId, val);
		}
		chunk.Data.Blocks.SetChunkSection(chunkData);
		MarkAdjacentChunksDirty(worldChunkX, worldChunkY, worldChunkZ);
	}

	private ushort[] DecodeLocalLightArray(byte[] arr)
	{
		using MemoryStream input = new MemoryStream(arr);
		using BinaryReader binaryReader = new BinaryReader(input);
		if (!binaryReader.ReadBoolean())
		{
			return null;
		}
		ushort[] array = _mapGeometryBuilder?.DequeueSelfLightAmountArray() ?? new ushort[32768];
		DeserializeOctree(binaryReader, array, 0, 0);
		return array;
	}

	private static void DeserializeOctree(BinaryReader from, ushort[] selfLightAmount, int depth, int index)
	{
		int num = from.ReadByte();
		for (int i = 0; i < 8; i++)
		{
			int num2 = 12 - depth;
			int num3 = index | (i << num2);
			if (((num >> i) & 1) == 1)
			{
				DeserializeOctree(from, selfLightAmount, depth + 3, num3);
				continue;
			}
			ushort num4 = from.ReadUInt16();
			int num5 = index + (i + 1 << num2);
			for (int j = num3; j < num5; j++)
			{
				selfLightAmount[j] = num4;
			}
		}
	}

	private ushort[] DecodeGlobalLightArray(byte[] arr)
	{
		using MemoryStream input = new MemoryStream(arr);
		using BinaryReader binaryReader = new BinaryReader(input);
		if (!binaryReader.ReadBoolean())
		{
			return null;
		}
		ushort[] array = _mapGeometryBuilder?.DequeueBorderedLightAmountArray() ?? new ushort[39304];
		DeserializeBorderedOctree(binaryReader, array, 0, 0);
		return array;
	}

	private static void DeserializeBorderedOctree(BinaryReader from, ushort[] borderedLightAmount, int depth, int index)
	{
		int num = from.ReadByte();
		for (int i = 0; i < 8; i++)
		{
			int num2 = 12 - depth;
			int num3 = index | (i << num2);
			if (((num >> i) & 1) == 1)
			{
				DeserializeBorderedOctree(from, borderedLightAmount, depth + 3, num3);
				continue;
			}
			ushort num4 = from.ReadUInt16();
			int num5 = index + (i + 1 << num2);
			for (int j = num3; j < num5; j++)
			{
				borderedLightAmount[ChunkHelper.IndexOfBlockInBorderedChunk(j, 0, 0, 0)] = num4;
			}
		}
	}

	private void MarkAdjacentChunksDirty(int worldChunkX, int worldChunkY, int worldChunkZ)
	{
		for (int i = -1; i < 2; i++)
		{
			for (int j = -1; j < 2; j++)
			{
				ChunkColumn chunkColumn = GetChunkColumn(worldChunkX + j, worldChunkZ + i);
				if (chunkColumn == null)
				{
					continue;
				}
				lock (chunkColumn.DisposeLock)
				{
					if (!chunkColumn.Disposed)
					{
						RenderedChunk renderedChunk = chunkColumn.GetChunk(worldChunkY - 1)?.Rendered;
						if (renderedChunk != null)
						{
							renderedChunk.GeometryNeedsUpdate = true;
						}
						RenderedChunk renderedChunk2 = chunkColumn.GetChunk(worldChunkY)?.Rendered;
						if (renderedChunk2 != null)
						{
							renderedChunk2.GeometryNeedsUpdate = true;
						}
						RenderedChunk renderedChunk3 = chunkColumn.GetChunk(worldChunkY + 1)?.Rendered;
						if (renderedChunk3 != null)
						{
							renderedChunk3.GeometryNeedsUpdate = true;
						}
					}
				}
			}
		}
	}

	public void SetChunkColumnHeights(int worldChunkX, int worldChunkZ, ushort[] heightData)
	{
		Debug.Assert(_gameInstance.IsOnPacketHandlerThread);
		ChunkColumn orCreateChunkColumn = GetOrCreateChunkColumn(worldChunkX, worldChunkZ);
		lock (orCreateChunkColumn.DisposeLock)
		{
			if (orCreateChunkColumn.Disposed)
			{
				return;
			}
			orCreateChunkColumn.Heights = heightData;
			for (int i = 0; i < ChunkHelper.ChunksPerColumn; i++)
			{
				Chunk chunk = orCreateChunkColumn.GetChunk(i);
				if (chunk != null)
				{
					chunk.Data.SelfLightNeedsUpdate = true;
				}
			}
		}
		for (int j = -1; j < 2; j++)
		{
			for (int k = -1; k < 2; k++)
			{
				ChunkColumn chunkColumn = GetChunkColumn(worldChunkX + k, worldChunkZ + j);
				if (chunkColumn == null)
				{
					continue;
				}
				lock (chunkColumn.DisposeLock)
				{
					if (chunkColumn.Disposed)
					{
						continue;
					}
					for (int l = 0; l < ChunkHelper.ChunksPerColumn; l++)
					{
						RenderedChunk renderedChunk = chunkColumn.GetChunk(l)?.Rendered;
						if (renderedChunk != null)
						{
							renderedChunk.GeometryNeedsUpdate = true;
						}
					}
				}
			}
		}
	}

	public void SetChunkColumnTints(int worldChunkX, int worldChunkZ, uint[] tintData)
	{
		ChunkColumn orCreateChunkColumn = GetOrCreateChunkColumn(worldChunkX, worldChunkZ);
		lock (orCreateChunkColumn.DisposeLock)
		{
			if (orCreateChunkColumn.Disposed)
			{
				return;
			}
			orCreateChunkColumn.Tints = tintData;
			for (int i = 0; i < ChunkHelper.ChunksPerColumn; i++)
			{
				Chunk chunk = orCreateChunkColumn.GetChunk(i);
				if (chunk != null)
				{
					chunk.Data.SelfLightNeedsUpdate = true;
				}
			}
		}
		for (int j = -1; j < 2; j++)
		{
			for (int k = -1; k < 2; k++)
			{
				ChunkColumn chunkColumn = GetChunkColumn(worldChunkX + k, worldChunkZ + j);
				if (chunkColumn == null)
				{
					continue;
				}
				lock (chunkColumn.DisposeLock)
				{
					if (chunkColumn.Disposed)
					{
						continue;
					}
					for (int l = 0; l < ChunkHelper.ChunksPerColumn; l++)
					{
						RenderedChunk renderedChunk = chunkColumn.GetChunk(l)?.Rendered;
						if (renderedChunk != null)
						{
							renderedChunk.GeometryNeedsUpdate = true;
						}
					}
				}
			}
		}
	}

	public void SetChunkColumnEnvironments(int worldChunkX, int worldChunkZ, ushort[][] environmentData)
	{
		ChunkColumn orCreateChunkColumn = GetOrCreateChunkColumn(worldChunkX, worldChunkZ);
		lock (orCreateChunkColumn.DisposeLock)
		{
			if (orCreateChunkColumn.Disposed)
			{
				return;
			}
			orCreateChunkColumn.Environments = environmentData;
			for (int i = 0; i < ChunkHelper.ChunksPerColumn; i++)
			{
				Chunk chunk = orCreateChunkColumn.GetChunk(i);
				if (chunk != null)
				{
					chunk.Data.SelfLightNeedsUpdate = true;
				}
			}
		}
		for (int j = -1; j < 2; j++)
		{
			for (int k = -1; k < 2; k++)
			{
				ChunkColumn chunkColumn = GetChunkColumn(worldChunkX + k, worldChunkZ + j);
				if (chunkColumn == null)
				{
					continue;
				}
				lock (chunkColumn.DisposeLock)
				{
					if (chunkColumn.Disposed)
					{
						continue;
					}
					for (int l = 0; l < ChunkHelper.ChunksPerColumn; l++)
					{
						RenderedChunk renderedChunk = chunkColumn.GetChunk(l)?.Rendered;
						if (renderedChunk != null)
						{
							renderedChunk.GeometryNeedsUpdate = true;
						}
					}
				}
			}
		}
	}

	public void UnloadChunkColumn(int worldChunkX, int worldChunkZ)
	{
		Debug.Assert(_gameInstance.IsOnPacketHandlerThread);
		if (_chunkColumns.TryRemove(ChunkHelper.IndexOfChunkColumn(worldChunkX, worldChunkZ), out var chunkColumn))
		{
			_gameInstance.Engine.RunOnMainThread(_gameInstance, delegate
			{
				DisposeChunkColumn(chunkColumn);
			});
		}
	}

	public void DisposeChunkColumn(ChunkColumn chunkColumn)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		lock (chunkColumn.DisposeLock)
		{
			for (int i = 0; i < ChunkHelper.ChunksPerColumn; i++)
			{
				Chunk chunk = chunkColumn.GetChunk(i);
				if (chunk == null)
				{
					continue;
				}
				lock (chunk.DisposeLock)
				{
					if (chunk.Rendered?.UpdateTask != null)
					{
						_mapGeometryBuilder?.EnqueueChunkUpdateTask(chunk.Rendered.UpdateTask);
						chunk.Rendered.UpdateTask = null;
					}
					if (chunk.Data.SelfLightAmounts != null)
					{
						_mapGeometryBuilder?.EnqueueSelfLightAmountArray(chunk.Data.SelfLightAmounts);
						chunk.Data.SelfLightAmounts = null;
					}
					if (chunk.Data.BorderedLightAmounts != null)
					{
						_mapGeometryBuilder?.EnqueueBorderedLightAmountArray(chunk.Data.BorderedLightAmounts);
						chunk.Data.BorderedLightAmounts = null;
					}
					foreach (ChunkData.InteractionStateInfo value in chunk.Data.CurrentInteractionStates.Values)
					{
						if (value.SoundEventReference.SoundObjectReference.SoundObjectId != 0)
						{
							AudioDevice.SoundEventReference soundEventReference = value.SoundEventReference;
							AudioDevice.SoundObjectReference soundObjectReference = value.SoundEventReference.SoundObjectReference;
							_gameInstance.AudioModule.ActionOnEvent(ref soundEventReference, (AkActionOnEventType)0);
							_gameInstance.AudioModule.UnregisterSoundObject(ref soundObjectReference);
						}
					}
					chunk.Data.CurrentInteractionStates = null;
					if (chunk.Rendered?.SoundObjects != null)
					{
						AudioModule audioModule = _gameInstance.AudioModule;
						for (int j = 0; j < chunk.Rendered.SoundObjects.Length; j++)
						{
							ref RenderedChunk.MapSoundObject reference = ref chunk.Rendered.SoundObjects[j];
							if (reference.SoundEventReference.SoundObjectReference.SlotId != -1)
							{
								audioModule.ActionOnEvent(ref reference.SoundEventReference, (AkActionOnEventType)0);
								audioModule.UnregisterSoundObject(ref reference.SoundEventReference.SoundObjectReference);
							}
						}
						chunk.Rendered.SoundObjects = null;
					}
					chunk.Dispose();
				}
			}
			chunkColumn.Dispose();
		}
	}

	public int ChunkColumnCount()
	{
		return _chunkColumns.Count;
	}

	public List<long> GetAllChunkColumnKeys()
	{
		return _chunkColumns.Keys.ToList();
	}

	public void SafeRegisterDestroyedBlock(int x, int y, int z)
	{
		_destroyedBlocksInfo.SafeRegisterDestroyedBlock(x, y, z);
	}

	public void RegisterDestroyedBlock(int x, int y, int z)
	{
		_destroyedBlocksInfo.RegisterDestroyedBlock(x, y, z);
	}

	public void GetBlocksRemovedThisFrame(Vector3 previousCameraPosition, Vector3 cameraPosition, BoundingFrustum cameraFrustum, float rejectNearCameraDistance, out int blocksCount, out Vector3[] blocksPositionFromCamera)
	{
		_destroyedBlocksInfo.PrepareBlocksRemovedThisFrame(_updatedChunksCount, _updatedChunksPositions, previousCameraPosition, cameraPosition, cameraFrustum, rejectNearCameraDistance);
		blocksCount = _destroyedBlocksInfo.Count;
		blocksPositionFromCamera = _destroyedBlocksInfo.BlockPositionsFromCamera;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsChunkReadyForDraw(int chunkX, int chunkY, int chunkZ)
	{
		return _bitFieldChunksReadyForDraw.IsBitOn(chunkX, chunkY, chunkZ);
	}

	private void SetupBitField3DForFrustum(BoundingFrustum viewFrustum, ref Vector3 cameraPosition)
	{
		Vector3[] array = new Vector3[4];
		viewFrustum.GetFarCorners(array);
		int num = (int)System.Math.Floor(cameraPosition.X);
		int num2 = (int)System.Math.Floor(cameraPosition.X);
		int num3 = (int)System.Math.Floor(cameraPosition.Z);
		int num4 = (int)System.Math.Floor(cameraPosition.Z);
		for (int i = 0; i < 4; i++)
		{
			num = (((int)array[i].X > num) ? num : ((int)array[i].X));
			num2 = (((int)array[i].X < num2) ? num2 : ((int)array[i].X));
			num3 = (((int)array[i].Z > num3) ? num3 : ((int)array[i].Z));
			num4 = (((int)array[i].Z < num4) ? num4 : ((int)array[i].Z));
		}
		num >>= 5;
		num2 >>= 5;
		num3 >>= 5;
		num4 >>= 5;
		int chunksPerColumn = ChunkHelper.ChunksPerColumn;
		int num5 = 0;
		int num6 = chunksPerColumn;
		_bitFieldChunksReadyForDraw.Setup(num - 1, num5 - 1, num3 - 1, num2 + 1, num6 + 1, num4 + 1);
	}

	private void SetupBitField3D(Vector3 playerPosition)
	{
		int num = (int)System.Math.Floor(playerPosition.X) >> 5;
		int num2 = (int)System.Math.Floor(playerPosition.Y) >> 5;
		int num3 = (int)System.Math.Floor(playerPosition.Z) >> 5;
		int minX = num - (ViewRadius + 1);
		int maxX = num + (ViewRadius + 1);
		int minZ = num3 - (ViewRadius + 1);
		int maxZ = num3 + (ViewRadius + 1);
		int chunksPerColumn = ChunkHelper.ChunksPerColumn;
		int minY = 0;
		int maxY = chunksPerColumn;
		_bitFieldChunksReadyForDraw.Setup(minX, minY, minZ, maxX, maxY, maxZ);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool IsNear(float distance)
	{
		return distance < 64f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool ShouldUseAnimatedBlocks(float distance)
	{
		return distance < LODSetup.StartDistance;
	}

	public int GetMaxChunksLoaded()
	{
		int num = ViewRadius * 2 + 1;
		return num * num * ChunkHelper.ChunksPerColumn;
	}

	public MapModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		ViewRadius = System.Math.Min(MaxServerViewRadius, _gameInstance.App.Settings.ViewDistance) / 32;
		TextureAtlas = new Texture(Texture.TextureTypes.Texture2D);
		TextureAtlas.CreateTexture2D(2048, 32, null, 5, GL.NEAREST_MIPMAP_NEAREST);
		_bitFieldChunksReadyForDraw.Initialize(8192);
		LODSetup.Enabled = true;
		LODSetup.InvRange = 1f / 32f;
		LODSetup.StartDistance = 160f;
		LODSetup.ShadowStartDistance = 48f;
		LODSetup.ShadowInvRange = 1f / 32f;
		ComputeLightLevels(0.05f);
	}

	protected override void DoDispose()
	{
		ClearAllColumns();
		if (_mapGeometryBuilder != null)
		{
			_mapGeometryBuilder.Dispose();
			_mapGeometryBuilder = null;
		}
		TextureAtlas.Dispose();
	}

	public void BeginFrame()
	{
		_visibleAnimatedChunksCount = 0;
	}

	public void ClearAllColumns()
	{
		_mapGeometryBuilder?.Suspend();
		foreach (ChunkColumn value in _chunkColumns.Values)
		{
			DisposeChunkColumn(value);
		}
		_chunkColumns.Clear();
		_mapGeometryBuilder?.Resume();
	}

	public override void Initialize()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_mapGeometryBuilder = new MapGeometryBuilder(_gameInstance);
	}

	public void SetClientBlock(int x, int y, int z, int blockId)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Expected O, but got Unknown
		int block = _gameInstance.MapModule.GetBlock(x, y, z, 1);
		if (block != blockId)
		{
			_gameInstance.InjectPacket((ProtoPacket)new ServerSetBlock(x, y, z, blockId, true));
			BlockParticleEvent val = (BlockParticleEvent)((block != 0 && blockId == 0) ? 7 : 8);
			ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[(blockId != 0) ? blockId : block];
			if (clientBlockType.BlockParticleSetId != null)
			{
				_gameInstance.InjectPacket((ProtoPacket)new SpawnBlockParticleSystem(blockId, val, new Position((double)((float)x + 0.5f), (double)((float)y + 0.5f), (double)((float)z + 0.5f))));
			}
		}
	}

	private void ComputeLightLevels(float ambientLight)
	{
		for (int i = 0; i < 16; i++)
		{
			LightLevels[i] = ambientLight + (float)System.Math.Pow((float)i / 15f, 1.5) * (1f - ambientLight);
		}
	}

	public void SetAmbientLight(float ambientLight)
	{
		DoWithMapGeometryBuilderPaused(discardAllRenderedChunks: true, delegate
		{
			ComputeLightLevels(ambientLight);
		});
	}

	private void UpdateAnimatedBlocks(Chunk chunk, RenderedChunk.ChunkUpdateTask chunkUpdateTask, float frameTime)
	{
		if (chunk.Rendered.AnimatedBlocks != null)
		{
			for (int i = 0; i < chunk.Rendered.AnimatedBlocks.Length; i++)
			{
				chunk.Rendered.AnimatedBlocks[i].Renderer.Dispose();
			}
			chunk.Rendered.AnimatedBlocks = null;
		}
		if (chunkUpdateTask?.AnimatedBlocks == null)
		{
			return;
		}
		chunk.Rendered.AnimatedBlocks = chunkUpdateTask.AnimatedBlocks;
		chunkUpdateTask.AnimatedBlocks = null;
		for (int j = 0; j < chunk.Rendered.AnimatedBlocks.Length; j++)
		{
			ref RenderedChunk.AnimatedBlock reference = ref chunk.Rendered.AnimatedBlocks[j];
			reference.Renderer.CreateGPUData(_gameInstance.Engine.Graphics);
			int num = chunk.Data.Blocks.Get(reference.Index);
			ClientBlockType clientBlockType = ClientBlockTypes[num];
			if (clientBlockType.BlockyAnimation != null)
			{
				if (!chunk.Data.CurrentInteractionStates.ContainsKey(reference.Index))
				{
					SetBlockInteractionState(chunk, num, reference.Index, reference.Position, isDone: true, playInteractionStateSound: true);
				}
				ChunkData.InteractionStateInfo value = chunk.Data.CurrentInteractionStates[reference.Index];
				if (value.StateFrameTime == -1f)
				{
					value.StateFrameTime = frameTime;
					chunk.Data.CurrentInteractionStates[reference.Index] = value;
				}
				float startTime = ((value.StateFrameTime == -2f && !clientBlockType.Looping) ? ((float)clientBlockType.BlockyAnimation.Duration) : ((frameTime - value.StateFrameTime) * 60f));
				reference.Renderer.SetSlotAnimationNoBlending(0, clientBlockType.BlockyAnimation, clientBlockType.Looping, 1f, startTime);
			}
			else
			{
				reference.Renderer.SetSlotAnimationNoBlending(0, reference.Animation, isLooping: true, 1f, frameTime * 60f + reference.AnimationTimeOffset);
			}
		}
	}

	public void UpdateSounds(Chunk chunk, RenderedChunk.ChunkUpdateTask chunkUpdateTask)
	{
		int num = ((chunkUpdateTask?.SoundObjects != null) ? chunkUpdateTask.SoundObjects.Length : 0);
		int num2 = ((chunk.Rendered.SoundObjects != null) ? chunk.Rendered.SoundObjects.Length : 0);
		int num3 = 0;
		Dictionary<int, ChunkData.InteractionStateInfo> currentInteractionStates = chunk.Data.CurrentInteractionStates;
		AudioModule audioModule = _gameInstance.AudioModule;
		for (int i = 0; i < num; i++)
		{
			ref RenderedChunk.MapSoundObject reference = ref chunkUpdateTask.SoundObjects[i];
			if (num3 < num2)
			{
				ref RenderedChunk.MapSoundObject reference2 = ref chunk.Rendered.SoundObjects[num3];
				while (reference.BlockIndex > reference2.BlockIndex && num3 < num2)
				{
					audioModule.ActionOnEvent(ref reference2.SoundEventReference, (AkActionOnEventType)0);
					audioModule.UnregisterSoundObject(ref reference2.SoundEventReference.SoundObjectReference);
					num3++;
					if (num3 < num2)
					{
						reference2 = chunk.Rendered.SoundObjects[num3];
					}
				}
				if (num3 < num2 && reference.BlockIndex == reference2.BlockIndex)
				{
					if (reference.SoundEventIndex == reference2.SoundEventIndex)
					{
						reference.SoundEventReference = reference2.SoundEventReference;
						num3++;
						continue;
					}
					reference.SoundEventReference.SoundObjectReference = reference2.SoundEventReference.SoundObjectReference;
					audioModule.ActionOnEvent(ref reference2.SoundEventReference, (AkActionOnEventType)0);
					num3++;
				}
			}
			if (reference.SoundEventReference.SoundObjectReference.SlotId != -1 || audioModule.TryRegisterSoundObject(reference.Position, Vector3.Zero, ref reference.SoundEventReference.SoundObjectReference, hasUniqueEvent: true))
			{
				audioModule.PlaySoundEvent(reference.SoundEventIndex, reference.SoundEventReference.SoundObjectReference, ref reference.SoundEventReference);
			}
		}
		for (int j = num3; j < num2; j++)
		{
			ref RenderedChunk.MapSoundObject reference3 = ref chunk.Rendered.SoundObjects[j];
			if (reference3.SoundEventReference.SoundObjectReference.SlotId != -1)
			{
				_gameInstance.AudioModule.ActionOnEvent(ref reference3.SoundEventReference, (AkActionOnEventType)0);
				_gameInstance.AudioModule.UnregisterSoundObject(ref reference3.SoundEventReference.SoundObjectReference);
			}
		}
		if (chunkUpdateTask != null)
		{
			chunk.Rendered.SoundObjects = chunkUpdateTask.SoundObjects;
			chunkUpdateTask.SoundObjects = null;
		}
		else
		{
			chunk.Rendered.SoundObjects = null;
		}
	}

	public void UpdateParticles(Chunk chunk, RenderedChunk.ChunkUpdateTask chunkUpdateTask)
	{
		int num = ((chunkUpdateTask?.MapParticles != null) ? chunkUpdateTask.MapParticles.Length : 0);
		int num2 = ((chunk.Rendered.MapParticles != null) ? chunk.Rendered.MapParticles.Length : 0);
		int num3 = 0;
		for (int i = 0; i < num; i++)
		{
			ref RenderedChunk.MapParticle reference = ref chunkUpdateTask.MapParticles[i];
			if (num3 < num2)
			{
				ref RenderedChunk.MapParticle reference2 = ref chunk.Rendered.MapParticles[num3];
				while (reference.BlockIndex > reference2.BlockIndex && num3 < num2)
				{
					if (reference2.ParticleSystemProxy != null)
					{
						reference2.ParticleSystemProxy.Expire();
						reference2.ParticleSystemProxy = null;
					}
					num3++;
					if (num3 < num2)
					{
						reference2 = chunk.Rendered.MapParticles[num3];
					}
				}
				if (num3 < num2)
				{
					if (reference.BlockIndex == reference2.BlockIndex && reference.ParticleSystemId == reference2.ParticleSystemId)
					{
						reference.ParticleSystemProxy = reference2.ParticleSystemProxy;
						num3++;
						continue;
					}
					if (reference.BlockIndex == reference2.BlockIndex && reference.ParticleSystemId != reference2.ParticleSystemId && reference2.ParticleSystemProxy != null)
					{
						reference2.ParticleSystemProxy.Expire();
						reference2.ParticleSystemProxy = null;
					}
				}
			}
			if (_gameInstance.ParticleSystemStoreModule.TrySpawnSystem(reference.ParticleSystemId, out reference.ParticleSystemProxy, isLocalPlayer: false, isTracked: true))
			{
				reference.ParticleSystemProxy.Position = reference.Position;
				reference.ParticleSystemProxy.Rotation = reference.RotationOffset;
				if (!reference.Color.IsTransparent)
				{
					reference.ParticleSystemProxy.DefaultColor = reference.Color;
				}
				reference.ParticleSystemProxy.Scale = reference.Scale;
			}
		}
		for (int j = num3; j < num2; j++)
		{
			if (chunk.Rendered.MapParticles[j].ParticleSystemProxy != null)
			{
				chunk.Rendered.MapParticles[j].ParticleSystemProxy.Expire();
				chunk.Rendered.MapParticles[j].ParticleSystemProxy = null;
			}
		}
		if (chunkUpdateTask != null)
		{
			chunk.Rendered.MapParticles = chunkUpdateTask.MapParticles;
			chunkUpdateTask.MapParticles = null;
		}
		else
		{
			chunk.Rendered.MapParticles = null;
		}
	}

	public void ResetParticleSystems()
	{
		Vector3 position = _gameInstance.LocalPlayer.Position;
		int chunkX = (int)position.X >> 5;
		int chunkZ = (int)position.Z >> 5;
		_spiralIterator.Initialize(chunkX, chunkZ, ViewRadius);
		foreach (long item in _spiralIterator)
		{
			int num = ChunkHelper.XOfChunkColumnIndex(item);
			int num2 = ChunkHelper.ZOfChunkColumnIndex(item);
			ChunkColumn chunkColumn = GetChunkColumn(item);
			if (chunkColumn == null)
			{
				continue;
			}
			for (int i = 0; i < ChunkHelper.ChunksPerColumn; i++)
			{
				Chunk chunk = chunkColumn.GetChunk(i);
				if (chunk == null || chunk.Rendered == null || chunk.Rendered.MapParticles == null)
				{
					continue;
				}
				for (int j = 0; j < chunk.Rendered.MapParticles.Length; j++)
				{
					ref RenderedChunk.MapParticle reference = ref chunk.Rendered.MapParticles[j];
					if (reference.ParticleSystemProxy != null)
					{
						reference.ParticleSystemProxy.Expire(instant: true);
						reference.ParticleSystemProxy = null;
					}
					if (_gameInstance.ParticleSystemStoreModule.TrySpawnSystem(reference.ParticleSystemId, out reference.ParticleSystemProxy, isLocalPlayer: false, isTracked: true))
					{
						reference.ParticleSystemProxy.Position = reference.Position;
						reference.ParticleSystemProxy.Rotation = reference.RotationOffset;
						if (!reference.Color.IsTransparent)
						{
							reference.ParticleSystemProxy.DefaultColor = reference.Color;
						}
						reference.ParticleSystemProxy.Scale = reference.Scale;
					}
				}
			}
		}
	}

	public void PrepareChunks(float frameTime)
	{
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(MapModule).FullName);
		}
		AreNearbyChunksRendered = true;
		int num = System.Math.Min(MaxServerViewRadius, _gameInstance.App.Settings.ViewDistance) / 32;
		bool flag = ViewRadius != num;
		ViewRadius = num;
		Vector3 position = _gameInstance.LocalPlayer.Position;
		int num2 = (int)position.X >> 5;
		int num3 = (int)position.Z >> 5;
		if (num2 != StartChunkX || num3 != StartChunkZ)
		{
			StartChunkX = num2;
			StartChunkZ = num3;
			flag = true;
		}
		Settings settings = _gameInstance.App.Settings;
		float num4 = settings.ViewDistance * settings.ViewDistance;
		float num5 = 0f;
		Vector2 vector = default(Vector2);
		int num6 = 2;
		int num7 = 2;
		ushort num8 = 0;
		ushort num9 = 0;
		ushort num10 = 0;
		_chunkColumnCount = 0;
		_animatedChunksCount = 0;
		SetupBitField3D(position);
		_updatedChunksCount = 0;
		_spiralIterator.Initialize(StartChunkX, StartChunkZ, num);
		Vector3 vector2 = default(Vector3);
		BoundingBox boundingBox = default(BoundingBox);
		foreach (long item in _spiralIterator)
		{
			int num11 = ChunkHelper.XOfChunkColumnIndex(item);
			int num12 = ChunkHelper.ZOfChunkColumnIndex(item);
			bool flag2 = System.Math.Abs(num11 - StartChunkX) <= 1 && System.Math.Abs(num12 - StartChunkZ) <= 1;
			ChunkColumn chunkColumn = GetChunkColumn(item);
			if (chunkColumn == null)
			{
				if (flag2)
				{
					AreNearbyChunksRendered = false;
				}
				continue;
			}
			_chunkColumnCount++;
			vector2.X = num11 * 32;
			vector2.Z = num12 * 32;
			vector.X = ((float)num11 + 0.5f) * 32f - _gameInstance.LocalPlayer.Position.X;
			vector.Y = ((float)num12 + 0.5f) * 32f - _gameInstance.LocalPlayer.Position.Z;
			float num13 = vector.LengthSquared();
			for (int i = 0; i < ChunkHelper.ChunksPerColumn; i++)
			{
				Chunk chunk = chunkColumn.GetChunk(i);
				if (chunk == null)
				{
					if (flag2)
					{
						AreNearbyChunksRendered = false;
					}
					continue;
				}
				num8++;
				if (chunk.Rendered == null)
				{
					if (num6 <= 0)
					{
						if (flag2)
						{
							AreNearbyChunksRendered = false;
						}
						continue;
					}
					num6--;
					chunk.Initialize(_gameInstance.Engine.Graphics);
				}
				if (chunk.Rendered.GeometryNeedsUpdate && (chunk.Rendered.RebuildState == RenderedChunk.ChunkRebuildState.Waiting || chunk.Rendered.RebuildState == RenderedChunk.ChunkRebuildState.ReadyForRebuild) && HasReceivedAllAdjacentChunks(num11, i, num12, StartChunkX, StartChunkZ))
				{
					chunk.Rendered.GeometryNeedsUpdate = false;
					chunk.Rendered.RebuildState = RenderedChunk.ChunkRebuildState.ReadyForRebuild;
					flag = true;
				}
				if (chunk.Rendered.RebuildState == RenderedChunk.ChunkRebuildState.UpdateReady)
				{
					RenderedChunk.ChunkUpdateTask updateTask;
					lock (chunk.DisposeLock)
					{
						updateTask = chunk.Rendered.UpdateTask;
						chunk.Rendered.UpdateTask = null;
					}
					if (num7 > 0 || updateTask == null)
					{
						UpdateChunkBufferData(chunk, updateTask);
						UpdateAnimatedBlocks(chunk, updateTask, frameTime);
						if (updateTask?.MapParticles != null || chunk.Rendered.MapParticles != null)
						{
							UpdateParticles(chunk, updateTask);
						}
						if (updateTask?.SoundObjects != null || chunk.Rendered.SoundObjects != null)
						{
							UpdateSounds(chunk, updateTask);
						}
						chunk.Rendered.BufferUpdateCount++;
						chunk.Rendered.RebuildState = RenderedChunk.ChunkRebuildState.Waiting;
						if (_updatedChunksCount < _updatedChunksPositions.Length)
						{
							_updatedChunksPositions[_updatedChunksCount].X = chunk.X;
							_updatedChunksPositions[_updatedChunksCount].Y = chunk.Y;
							_updatedChunksPositions[_updatedChunksCount].Z = chunk.Z;
							_updatedChunksCount++;
						}
						if (updateTask != null)
						{
							num7--;
							_mapGeometryBuilder.EnqueueChunkUpdateTask(updateTask);
						}
					}
					else
					{
						lock (chunk.DisposeLock)
						{
							chunk.Rendered.UpdateTask = updateTask;
						}
					}
				}
				if (chunk.Rendered.BufferUpdateCount == 0)
				{
					if (flag2)
					{
						AreNearbyChunksRendered = false;
					}
					num4 = MathHelper.Min(num4, num13);
				}
				else
				{
					num5 = num13;
				}
				if (chunk.Rendered.BufferUpdateCount > 0)
				{
					_bitFieldChunksReadyForDraw.SwitchBitOn(chunk.X, chunk.Y, chunk.Z);
					num9++;
				}
				byte b = 0;
				b |= (byte)((chunk.Rendered.OpaqueIndicesCount > 0) ? ChunkDrawTagOpaque : 0);
				b |= (byte)((chunk.Rendered.AlphaTestedIndicesCount > 0) ? ChunkDrawTagAlphaTested : 0);
				b |= (byte)((chunk.Rendered.AlphaBlendedIndicesCount > 0) ? ChunkDrawTagAlphaBlended : 0);
				b |= (byte)((chunk.Rendered.AnimatedBlocks != null) ? ChunkDrawTagAnimated : 0);
				if (b != 0)
				{
					ArrayUtils.GrowArrayIfNecessary(ref _chunks, num10 + 1, 1000);
					ArrayUtils.GrowArrayIfNecessary(ref _drawMasks, num10 + 1, 1000);
					ArrayUtils.GrowArrayIfNecessary(ref _undergroundHints, num10 + 1, 1000);
					ArrayUtils.GrowArrayIfNecessary(ref _boundingVolumes, num10 + 1, 1000);
					ushort num14 = num10;
					_chunks[num14] = chunk;
					num10++;
					_drawMasks[num14] = b;
					_undergroundHints[num14] = chunk.IsUnderground;
					if (chunk.Rendered.AnimatedBlocks != null)
					{
						ArrayUtils.GrowArrayIfNecessary(ref _animatedChunksLocalIds, _animatedChunksCount + 1, 1000);
						ushort animatedChunksCount = _animatedChunksCount;
						_animatedChunksLocalIds[animatedChunksCount] = num14;
						_animatedChunksCount++;
					}
					vector2.Y = i * 32;
					boundingBox.Min = vector2;
					boundingBox.Max = vector2 + _chunkSize;
					_boundingVolumes[num14] = boundingBox;
				}
			}
		}
		_chunksCount = num10;
		LoadedChunksCount = num8;
		DrawableChunksCount = num9;
		EffectiveViewDistance = MathHelper.Min((float)System.Math.Sqrt(num4), (float)System.Math.Sqrt(num5));
		if (flag)
		{
			int startChunkY = MathHelper.Clamp((int)_gameInstance.LocalPlayer.Position.Y >> 5, 0, ChunkHelper.ChunksPerColumn - 1);
			_mapGeometryBuilder.RestartSpiral(ChunkHelper.WorldToChunk(position), num2, startChunkY, num3, ViewRadius);
		}
	}

	public void Update(float deltaTime)
	{
		_mapGeometryBuilder.HandleDisposeRequests();
		for (int i = 0; i < _animatedChunksCount; i++)
		{
			ushort num = _animatedChunksLocalIds[i];
			Chunk chunk = _chunks[num];
			for (int j = 0; j < chunk.Rendered.AnimatedBlocks.Length; j++)
			{
				int index = chunk.Rendered.AnimatedBlocks[j].Index;
				chunk.Rendered.AnimatedBlocks[j].Renderer.AdvancePlayback(deltaTime * 60f);
				if (chunk.Rendered.AnimatedBlocks[j].IsBeingHit && chunk.Data.TryGetBlockHitTimer(index, out var slotIndex, out var hitTimer))
				{
					if (deltaTime >= hitTimer)
					{
						int worldX = chunk.X * 32 + index % 32;
						int worldY = chunk.Y * 32 + index / 32 / 32;
						int worldZ = chunk.Z * 32 + index / 32 % 32;
						SetBlockHitTimer(worldX, worldY, worldZ, 0f);
					}
					else
					{
						chunk.Data.BlockHitTimers[slotIndex].Timer = hitTimer - deltaTime;
					}
				}
			}
		}
	}

	public void DoWithMapGeometryBuilderPaused(bool discardAllRenderedChunks, Action actionBeforeResume)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (_mapGeometryBuilder != null)
		{
			_mapGeometryBuilder.Suspend();
			if (discardAllRenderedChunks)
			{
				foreach (ChunkColumn value in _chunkColumns.Values)
				{
					lock (value.DisposeLock)
					{
						if (!value.Disposed)
						{
							value.DiscardRenderedChunks();
						}
					}
				}
				_mapGeometryBuilder.EnsureEnoughChunkUpdateTasks();
				Logger.Warn("All rendered chunks were discarded.");
			}
		}
		actionBeforeResume?.Invoke();
		_mapGeometryBuilder?.Resume();
	}

	public int GetChunkUpdateTaskQueueCount()
	{
		return _mapGeometryBuilder.GetChunkUpdateTaskQueueCount();
	}

	private bool HasReceivedAllAdjacentChunks(int chunkX, int chunkY, int chunkZ, int startChunkX, int startChunkZ)
	{
		if (System.Math.Abs(chunkX - startChunkX) >= ViewRadius || System.Math.Abs(chunkZ - startChunkZ) >= ViewRadius)
		{
			return true;
		}
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				ChunkColumn chunkColumn = GetChunkColumn(chunkX + (i - 1), chunkZ + (j - 1));
				if (chunkColumn == null)
				{
					return false;
				}
				for (int k = 0; k < 3; k++)
				{
					int num = chunkY + (k - 1);
					if (num >= 0 && num < ChunkHelper.ChunksPerColumn && chunkColumn.GetChunk(num)?.Rendered == null)
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	private unsafe void UpdateChunkBufferData(Chunk chunk, RenderedChunk.ChunkUpdateTask chunkUpdateTask)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		GLFunctions gL = _gameInstance.Engine.Graphics.GL;
		RenderedChunk rendered = chunk.Rendered;
		gL.BindVertexArray(rendered.OpaqueVertexArray);
		gL.BindBuffer(rendered.OpaqueVertexArray, GL.ARRAY_BUFFER, rendered.OpaqueVerticesBuffer);
		gL.BindBuffer(rendered.OpaqueVertexArray, GL.ELEMENT_ARRAY_BUFFER, rendered.OpaqueIndicesBuffer);
		rendered.OpaqueIndicesCount = chunkUpdateTask?.OpaqueData.IndicesCount ?? 0;
		if (rendered.OpaqueIndicesCount > 0)
		{
			fixed (ChunkVertex* ptr = chunkUpdateTask.OpaqueData.Vertices)
			{
				gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(chunkUpdateTask.OpaqueData.VerticesCount * ChunkVertex.Size), (IntPtr)ptr, GL.STATIC_DRAW);
			}
			fixed (uint* ptr2 = chunkUpdateTask.OpaqueData.Indices)
			{
				gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(rendered.OpaqueIndicesCount * 4), (IntPtr)ptr2, GL.STATIC_DRAW);
			}
		}
		else
		{
			gL.BufferData(GL.ARRAY_BUFFER, IntPtr.Zero, IntPtr.Zero, GL.STATIC_DRAW);
			gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, IntPtr.Zero, IntPtr.Zero, GL.STATIC_DRAW);
		}
		gL.BindVertexArray(rendered.AlphaBlendedVertexArray);
		gL.BindBuffer(rendered.AlphaBlendedVertexArray, GL.ARRAY_BUFFER, rendered.AlphaBlendedVerticesBuffer);
		gL.BindBuffer(rendered.AlphaBlendedVertexArray, GL.ELEMENT_ARRAY_BUFFER, rendered.AlphaBlendedIndicesBuffer);
		rendered.AlphaBlendedIndicesCount = chunkUpdateTask?.AlphaBlendedData.IndicesCount ?? 0;
		if (rendered.AlphaBlendedIndicesCount > 0)
		{
			fixed (ChunkVertex* ptr3 = chunkUpdateTask.AlphaBlendedData.Vertices)
			{
				gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(chunkUpdateTask.AlphaBlendedData.VerticesCount * ChunkVertex.Size), (IntPtr)ptr3, GL.STATIC_DRAW);
			}
			fixed (uint* ptr4 = chunkUpdateTask.AlphaBlendedData.Indices)
			{
				gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(rendered.AlphaBlendedIndicesCount * 4), (IntPtr)ptr4, GL.STATIC_DRAW);
			}
		}
		else
		{
			gL.BufferData(GL.ARRAY_BUFFER, IntPtr.Zero, IntPtr.Zero, GL.STATIC_DRAW);
			gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, IntPtr.Zero, IntPtr.Zero, GL.STATIC_DRAW);
		}
		gL.BindVertexArray(rendered.AlphaTestedVertexArray);
		gL.BindBuffer(rendered.AlphaTestedVertexArray, GL.ARRAY_BUFFER, rendered.AlphaTestedVerticesBuffer);
		gL.BindBuffer(rendered.AlphaTestedVertexArray, GL.ELEMENT_ARRAY_BUFFER, rendered.AlphaTestedIndicesBuffer);
		rendered.AlphaTestedAnimatedLowLODIndicesCount = chunkUpdateTask?.AlphaTestedAnimatedLowLODIndicesCount ?? 0;
		rendered.AlphaTestedLowLODIndicesCount = chunkUpdateTask?.AlphaTestedLowLODIndicesCount ?? 0;
		rendered.AlphaTestedHighLODIndicesCount = chunkUpdateTask?.AlphaTestedHighLODIndicesCount ?? 0;
		rendered.AlphaTestedAnimatedHighLODIndicesCount = chunkUpdateTask?.AlphaTestedAnimatedHighLODIndicesCount ?? 0;
		if (rendered.AlphaTestedIndicesCount > 0)
		{
			fixed (ChunkVertex* ptr5 = chunkUpdateTask.AlphaTestedData.Vertices)
			{
				gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(chunkUpdateTask.AlphaTestedData.VerticesCount * ChunkVertex.Size), (IntPtr)ptr5, GL.STATIC_DRAW);
			}
			fixed (uint* ptr6 = chunkUpdateTask.AlphaTestedData.Indices)
			{
				gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(rendered.AlphaTestedIndicesCount * 4), (IntPtr)ptr6, GL.STATIC_DRAW);
			}
		}
		else
		{
			gL.BufferData(GL.ARRAY_BUFFER, IntPtr.Zero, IntPtr.Zero, GL.STATIC_DRAW);
			gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, IntPtr.Zero, IntPtr.Zero, GL.STATIC_DRAW);
		}
		chunk.SolidPlaneMinY = chunkUpdateTask?.SolidPlaneMinY ?? 0;
		chunk.IsUnderground = chunkUpdateTask?.IsUnderground ?? false;
	}

	public void ProcessFrustumCulling(SceneView sceneView)
	{
		ArrayUtils.GrowArrayIfNecessary(ref sceneView.ChunksFrustumCullingResults, _chunksCount, 1000);
		Vector3 cameraPosition = _gameInstance.SceneRenderer.Data.CameraPosition;
		if (sceneView.UseKDopForCulling)
		{
			for (int i = 0; i < _chunksCount; i++)
			{
				BoundingBox volume = _boundingVolumes[i];
				volume.Max -= cameraPosition;
				volume.Min -= cameraPosition;
				sceneView.ChunksFrustumCullingResults[i] = sceneView.KDopFrustum.Intersects(volume);
			}
		}
		else
		{
			for (int j = 0; j < _chunksCount; j++)
			{
				sceneView.Frustum.Intersects(ref _boundingVolumes[j], out sceneView.ChunksFrustumCullingResults[j]);
			}
		}
	}

	public void GatherRenderableChunks(SceneView cameraSceneView)
	{
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(MapModule).FullName);
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		cameraSceneView.PrepareForIncomingChunks(_chunksCount);
		for (int i = 0; i < _chunksCount; i++)
		{
			if (!cameraSceneView.ChunksFrustumCullingResults[i])
			{
				continue;
			}
			byte b = _drawMasks[i];
			num += (((b & ChunkDrawTagOpaque) > 0) ? 1 : 0);
			num2 += (((b & ChunkDrawTagAlphaTested) > 0) ? 1 : 0);
			num3 += (((b & ChunkDrawTagAlphaBlended) > 0) ? 1 : 0);
			Vector3 center = _boundingVolumes[i].GetCenter();
			cameraSceneView.RegisterChunk(i, center);
			if ((b & ChunkDrawTagAnimated) > 0)
			{
				float distance = (center - cameraSceneView.Position).Length();
				if (ShouldUseAnimatedBlocks(distance))
				{
					ArrayUtils.GrowArrayIfNecessary(ref _visibleAnimatedChunkIds, _visibleAnimatedChunksCount + 1, 1000);
					_visibleAnimatedChunkIds[_visibleAnimatedChunksCount] = (ushort)i;
					_visibleAnimatedChunksCount++;
				}
			}
		}
		_gameInstance.SceneRenderer.PrepareForIncomingMapChunkDrawTasks(num, num2, num3);
	}

	public void PrepareChunksForDraw(SceneView cameraSceneView)
	{
		SceneRenderer sceneRenderer = _gameInstance.SceneRenderer;
		Vector3 vector = new Vector3(16f);
		bool shouldDrawAllChunksAsNear = ShouldDrawAllChunksAsNear;
		Vector3 vector2 = default(Vector3);
		for (int i = 0; i < cameraSceneView.ChunksCount; i++)
		{
			int sortedChunkId = cameraSceneView.GetSortedChunkId(i);
			Chunk chunk = _chunks[sortedChunkId];
			byte b = _drawMasks[sortedChunkId];
			vector2.X = (float)chunk.X * 32f;
			vector2.Y = (float)chunk.Y * 32f;
			vector2.Z = (float)chunk.Z * 32f;
			Vector3 position = vector2 - cameraSceneView.Position;
			Matrix.CreateTranslation(ref position, out var result);
			if (chunk.SolidPlaneMinY > 0 && position.Y <= -32f)
			{
				sceneRenderer.PrepareForIncomingChunkOccluderPlane(1);
				sceneRenderer.RegisterChunkOccluderPlane(position, chunk.SolidPlaneMinY);
			}
			Vector3 vector3 = vector2 + vector;
			float num = (vector3 - cameraSceneView.Position).Length();
			float value = (num - LODSetup.StartDistance) * LODSetup.InvRange;
			value = 1f - MathHelper.Clamp(value, 0f, 1f);
			value = (LODSetup.Enabled ? value : 1f);
			bool flag = IsNear(num);
			RenderedChunk rendered = chunk.Rendered;
			bool isNear = flag || shouldDrawAllChunksAsNear;
			if ((b & ChunkDrawTagOpaque) > 0)
			{
				sceneRenderer.RegisterMapChunkOpaqueDrawTask(ref result, rendered.OpaqueVertexArray, rendered.OpaqueIndicesCount, isNear);
			}
			if ((b & ChunkDrawTagAlphaTested) > 0)
			{
				bool useAnimatedBlocks = ShouldUseAnimatedBlocks(num);
				rendered.GetAlphaTestedData(useAnimatedBlocks, value, out var dataCount, out var dataOffset);
				sceneRenderer.RegisterMapChunkAlphaTestedDrawTask(ref result, rendered.AlphaTestedVertexArray, dataOffset, dataCount, isNear);
			}
			if ((b & ChunkDrawTagAlphaBlended) > 0)
			{
				sceneRenderer.RegisterMapChunkAlphaBlendedDrawTask(ref result, rendered.AlphaBlendedVertexArray, rendered.AlphaBlendedIndicesCount, isNear);
			}
		}
	}

	public void GatherRenderableAnimatedBlocks(SceneView cameraSceneView, SceneView sunSceneView, bool cullUndergroundShadowCasters = true)
	{
		SceneRenderer sceneRenderer = _gameInstance.SceneRenderer;
		AnimationSystem animationSystem = _gameInstance.Engine.AnimationSystem;
		FXSystem fXSystem = _gameInstance.Engine.FXSystem;
		Vector3 vector = default(Vector3);
		for (int i = 0; i < _visibleAnimatedChunksCount; i++)
		{
			ushort num = _visibleAnimatedChunkIds[i];
			Chunk chunk = _chunks[num];
			vector.X = (float)chunk.X * 32f;
			vector.Y = (float)chunk.Y * 32f;
			vector.Z = (float)chunk.Z * 32f;
			Vector3 position = vector - cameraSceneView.Position;
			Matrix.CreateTranslation(ref position, out var result);
			RenderedChunk rendered = chunk.Rendered;
			int num2 = rendered.AnimatedBlocks.Length;
			sceneRenderer.PrepareForIncomingMapBlockAnimatedDrawTasks(num2);
			sceneRenderer.PrepareForIncomingMapBlockAnimatedSunShadowCasterDrawTasks(num2);
			animationSystem.PrepareForIncomingTasks(num2);
			for (int j = 0; j < num2; j++)
			{
				ref RenderedChunk.AnimatedBlock reference = ref rendered.AnimatedBlocks[j];
				BoundingBox boundingBox = reference.BoundingBox;
				bool flag = cameraSceneView.Frustum.Intersects(boundingBox);
				boundingBox.Min -= cameraSceneView.Position;
				boundingBox.Max -= cameraSceneView.Position;
				bool flag2 = sunSceneView != null && (sunSceneView.UseKDopForCulling ? sunSceneView.KDopFrustum.Intersects(boundingBox) : sunSceneView.Frustum.Intersects(boundingBox));
				if (!(flag || flag2))
				{
					continue;
				}
				animationSystem.RegisterAnimationTask(reference.Renderer, skipUpdate: false);
				float hitTimer = 0f;
				if (reference.IsBeingHit)
				{
					chunk.Data.TryGetBlockHitTimer(reference.Index, out var _, out hitTimer);
				}
				animationSystem.ProcessHitBlockAnimation(hitTimer, ref reference.Matrix, out var animatedMatrix);
				Matrix.Multiply(ref animatedMatrix, ref result, out var result2);
				ref AnimatedBlockRenderer renderer = ref reference.Renderer;
				if (flag)
				{
					sceneRenderer.RegisterMapBlockAnimatedDrawTask(ref result2, renderer.VertexArray, renderer.IndicesCount, animationSystem.NodeBuffer, renderer.NodeBufferOffset, renderer.NodeCount);
				}
				bool flag3 = _undergroundHints[num];
				bool flag4 = cullUndergroundShadowCasters && flag3;
				if (flag2 && !flag4)
				{
					sceneRenderer.RegisterMapBlockAnimatedSunShadowCasterDrawTask(ref boundingBox, ref result2, renderer.VertexArray, renderer.IndicesCount, animationSystem.NodeBuffer, renderer.NodeBufferOffset, renderer.NodeCount);
				}
				if (!flag || reference.MapParticleIndices == null)
				{
					continue;
				}
				int num3 = reference.MapParticleIndices.Length;
				fXSystem.Particles.PrepareForIncomingAnimatedBlockParticlesTasks(num3);
				for (int k = 0; k < num3; k++)
				{
					RenderedChunk.MapParticle mapParticle = rendered.MapParticles[reference.MapParticleIndices[k]];
					if (mapParticle.ParticleSystemProxy != null)
					{
						fXSystem.Particles.RegisterFXAnimatedBlockParticlesTask(reference.Renderer, mapParticle);
					}
				}
			}
		}
	}

	public void GatherRenderableChunksForShadowMap(SceneView sunSceneView, bool cullUndergroundShadowCasters, int maxChunks = 100)
	{
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(MapModule).FullName);
		}
		int num = System.Math.Min(_chunksCount, maxChunks);
		int num2 = 0;
		sunSceneView.PrepareForIncomingChunks(num);
		int num3 = 0;
		for (int i = 0; i < _chunksCount; i++)
		{
			if (num3 >= num)
			{
				break;
			}
			if (sunSceneView.ChunksFrustumCullingResults[i])
			{
				bool flag = _undergroundHints[i];
				if (!(cullUndergroundShadowCasters && flag))
				{
					byte b = _drawMasks[i];
					num2 += (((b & ChunkDrawTagOpaque) > 0) ? 1 : 0);
					num2 += (((b & ChunkDrawTagAlphaTested) > 0) ? 1 : 0);
					Vector3 center = _boundingVolumes[i].GetCenter();
					sunSceneView.RegisterChunk(i, center);
					num3++;
				}
			}
		}
		_gameInstance.SceneRenderer.PrepareForIncomingMapChunkSunShadowCasterDrawTasks(num2);
	}

	public void PrepareForSunShadowMapDraw(SceneView sunSceneView, Vector3 cameraPosition)
	{
		SceneRenderer sceneRenderer = _gameInstance.SceneRenderer;
		Vector3 vector = new Vector3(16f);
		Vector3 position = default(Vector3);
		for (int i = 0; i < sunSceneView.ChunksCount; i++)
		{
			int sortedChunkId = sunSceneView.GetSortedChunkId(i);
			Chunk chunk = _chunks[sortedChunkId];
			position.X = (float)chunk.X * 32f;
			position.Y = (float)chunk.Y * 32f;
			position.Z = (float)chunk.Z * 32f;
			position -= cameraPosition;
			Matrix.CreateTranslation(ref position, out var result);
			byte b = _drawMasks[sortedChunkId];
			RenderedChunk rendered = chunk.Rendered;
			if ((b & ChunkDrawTagOpaque) > 0)
			{
				sceneRenderer.RegisterMapChunkSunShadowCasterDrawTask(ref result, rendered.OpaqueVertexArray, rendered.OpaqueIndicesCount, IntPtr.Zero);
			}
			if ((b & ChunkDrawTagAlphaTested) > 0)
			{
				float num = (position + vector).Length();
				float value = (num - LODSetup.ShadowStartDistance) * LODSetup.ShadowInvRange;
				value = 1f - MathHelper.Clamp(value, 0f, 1f);
				value *= value;
				bool useAnimatedBlocks = ShouldUseAnimatedBlocks(num);
				rendered.GetAlphaTestedData(useAnimatedBlocks, value, out var dataCount, out var dataOffset);
				sceneRenderer.RegisterMapChunkSunShadowCasterDrawTask(ref result, rendered.AlphaTestedVertexArray, dataCount, dataOffset);
			}
		}
	}
}
