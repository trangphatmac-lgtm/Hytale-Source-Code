#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using HytaleClient.Audio;
using HytaleClient.Core;
using HytaleClient.Data.Map;
using HytaleClient.Data.Weather;
using HytaleClient.Graphics.Particles;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.Graphics.Map;

internal class ChunkGeometryBuilder
{
	public class AdjacentBlockOffsets
	{
		public int Main;

		public int Vertical;

		public int Horizontal;

		public Vector3 SideMaskOffset;

		public Vector3 Normal;

		public uint PackedNormal;
	}

	private const float MaskOffsetEpsilon = 0.0001f;

	public static readonly int[][] AdjacentBlockSignsByCorner = new int[4][]
	{
		new int[2] { 1, 1 },
		new int[2] { -1, 1 },
		new int[2] { -1, -1 },
		new int[2] { 1, -1 }
	};

	public static readonly AdjacentBlockOffsets[] AdjacentBlockOffsetsBySide = new AdjacentBlockOffsets[6]
	{
		new AdjacentBlockOffsets
		{
			Main = ChunkHelper.IndexOfBlockInBorderedChunk(0, 1, 0),
			Horizontal = ChunkHelper.IndexOfBlockInBorderedChunk(1, 0, 0),
			Vertical = ChunkHelper.IndexOfBlockInBorderedChunk(0, 0, -1),
			SideMaskOffset = new Vector3(0f, 0.0001f, 0f),
			Normal = new Vector3(0f, 1f, 0f),
			PackedNormal = VertexCompression.NormalizedXYZToUint(0f, 1f, 0f)
		},
		new AdjacentBlockOffsets
		{
			Main = ChunkHelper.IndexOfBlockInBorderedChunk(0, -1, 0),
			Horizontal = ChunkHelper.IndexOfBlockInBorderedChunk(-1, 0, 0),
			Vertical = ChunkHelper.IndexOfBlockInBorderedChunk(0, 0, -1),
			SideMaskOffset = new Vector3(0f, -0.0001f, 0f),
			Normal = new Vector3(0f, -1f, 0f),
			PackedNormal = VertexCompression.NormalizedXYZToUint(0f, -1f, 0f)
		},
		new AdjacentBlockOffsets
		{
			Main = ChunkHelper.IndexOfBlockInBorderedChunk(-1, 0, 0),
			Horizontal = ChunkHelper.IndexOfBlockInBorderedChunk(0, 0, 1),
			Vertical = ChunkHelper.IndexOfBlockInBorderedChunk(0, 1, 0),
			SideMaskOffset = new Vector3(-0.0001f, 0f, 0f),
			Normal = new Vector3(-1f, 0f, 0f),
			PackedNormal = VertexCompression.NormalizedXYZToUint(-1f, 0f, 0f)
		},
		new AdjacentBlockOffsets
		{
			Main = ChunkHelper.IndexOfBlockInBorderedChunk(1, 0, 0),
			Horizontal = ChunkHelper.IndexOfBlockInBorderedChunk(0, 0, -1),
			Vertical = ChunkHelper.IndexOfBlockInBorderedChunk(0, 1, 0),
			SideMaskOffset = new Vector3(0.0001f, 0f, 0f),
			Normal = new Vector3(1f, 0f, 0f),
			PackedNormal = VertexCompression.NormalizedXYZToUint(1f, 0f, 0f)
		},
		new AdjacentBlockOffsets
		{
			Main = ChunkHelper.IndexOfBlockInBorderedChunk(0, 0, 1),
			Horizontal = ChunkHelper.IndexOfBlockInBorderedChunk(1, 0, 0),
			Vertical = ChunkHelper.IndexOfBlockInBorderedChunk(0, 1, 0),
			SideMaskOffset = new Vector3(0f, 0f, 0.0001f),
			Normal = new Vector3(0f, 0f, 1f),
			PackedNormal = VertexCompression.NormalizedXYZToUint(0f, 0f, 1f)
		},
		new AdjacentBlockOffsets
		{
			Main = ChunkHelper.IndexOfBlockInBorderedChunk(0, 0, -1),
			Horizontal = ChunkHelper.IndexOfBlockInBorderedChunk(-1, 0, 0),
			Vertical = ChunkHelper.IndexOfBlockInBorderedChunk(0, 1, 0),
			SideMaskOffset = new Vector3(0f, 0f, -0.0001f),
			Normal = new Vector3(0f, 0f, -1f),
			PackedNormal = VertexCompression.NormalizedXYZToUint(0f, 0f, -1f)
		}
	};

	public const int BlockSize = 32;

	public const float BlockScale = 1f / 32f;

	public const float TextureBleedOffset = 0.04f;

	public static readonly UShortVector2 NoSideMaskUV = new UShortVector2
	{
		X = 0,
		Y = 0
	};

	public static Matrix PositiveHalfBlockOffsetMatrix = Matrix.CreateTranslation(0.5f, 0.5f, 0.5f);

	public static Matrix NegativeHalfBlockOffsetMatrix = Matrix.CreateTranslation(-0.5f, -0.5f, -0.5f);

	private static readonly Vector3 BackBottomLeft = new Vector3(0f, 0f, 0f);

	private static readonly Vector3 BackBottomRight = new Vector3(1f, 0f, 0f);

	private static readonly Vector3 BackTopLeft = new Vector3(0f, 1f, 0f);

	private static readonly Vector3 BackTopRight = new Vector3(1f, 1f, 0f);

	private static readonly Vector3 FrontBottomLeft = new Vector3(0f, 0f, 1f);

	private static readonly Vector3 FrontBottomRight = new Vector3(1f, 0f, 1f);

	private static readonly Vector3 FrontTopLeft = new Vector3(0f, 1f, 1f);

	private static readonly Vector3 FrontTopRight = new Vector3(1f, 1f, 1f);

	public static readonly Vector3[][] CornersPerSide = new Vector3[6][]
	{
		new Vector3[4] { BackTopRight, BackTopLeft, FrontTopLeft, FrontTopRight },
		new Vector3[4] { BackBottomLeft, BackBottomRight, FrontBottomRight, FrontBottomLeft },
		new Vector3[4] { FrontTopLeft, BackTopLeft, BackBottomLeft, FrontBottomLeft },
		new Vector3[4] { BackTopRight, FrontTopRight, FrontBottomRight, BackBottomRight },
		new Vector3[4] { FrontTopRight, FrontTopLeft, FrontBottomLeft, FrontBottomRight },
		new Vector3[4] { BackTopLeft, BackTopRight, BackBottomRight, BackBottomLeft }
	};

	public BlockingCollection<RenderedChunk.ChunkUpdateTask> ChunkUpdateTaskQueue = new BlockingCollection<RenderedChunk.ChunkUpdateTask>();

	public ConcurrentQueue<Disposable> DisposeRequests = new ConcurrentQueue<Disposable>();

	private ClientBlockType[] _clientBlockTypes;

	private BlockHitbox[] _blockHitboxes;

	private ClientWorldEnvironment[] _environments;

	private float[] _lightLevels;

	private Point[] _atlasSizes;

	private bool _LODEnabled = true;

	private const float FluidShaderHeightThreshold = 0.125f;

	private byte[] _solidBlockHeight = new byte[1024];

	private readonly byte[] _chunkVisibleSideFlags = new byte[32768];

	private readonly UShortVector2[] _texCoordsByCorner = new UShortVector2[4];

	private readonly UShortVector2[] _sideMaskTexCoordsByCorner = new UShortVector2[4];

	private readonly int[] _cornerOcclusions = new int[4];

	private readonly ushort[] _environmentTracker = new ushort[3468];

	private readonly uint[] _cornerEnvironmentWaterTints = new uint[8];

	private readonly ClientBlockType.ClientShaderEffect[] _cornerShaderEffects = new ClientBlockType.ClientShaderEffect[4];

	private Matrix _tempBlockRotationMatrix;

	private Matrix _tempBlockWorldMatrix;

	private Matrix _tempCubeBlockWorldInvertMatrix;

	public static ShortVector3 NoTint = default(ShortVector3);

	public static ShortVector3 ForceTint = NoTint;

	public ChunkGeometryBuilder()
	{
		EnsureEnoughChunkUpdateTasks();
	}

	public void EnsureEnoughChunkUpdateTasks()
	{
		int num = 5 - ChunkUpdateTaskQueue.Count;
		for (int i = 0; i < num; i++)
		{
			ChunkUpdateTaskQueue.Add(new RenderedChunk.ChunkUpdateTask
			{
				OpaqueData = new ChunkGeometryData
				{
					Vertices = new ChunkVertex[60000],
					Indices = new uint[90000]
				},
				AlphaBlendedData = new ChunkGeometryData
				{
					Vertices = new ChunkVertex[60000],
					Indices = new uint[90000]
				},
				AlphaTestedData = new ChunkGeometryData
				{
					Vertices = new ChunkVertex[60000],
					Indices = new uint[90000]
				}
			});
		}
	}

	public void SetBlockTypes(ClientBlockType[] clientBlockTypes)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_clientBlockTypes = clientBlockTypes;
	}

	public void SetBlockHitboxes(BlockHitbox[] blockHitboxes)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_blockHitboxes = blockHitboxes;
	}

	public void SetLightLevels(float[] lightLevels)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_lightLevels = lightLevels;
	}

	public void SetEnvironments(ClientWorldEnvironment[] environments)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_environments = environments;
	}

	public void SetAtlasSizes(Point[] atlasSizes)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_atlasSizes = atlasSizes;
	}

	public void SetLODEnabled(bool LODEnabled)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_LODEnabled = LODEnabled;
	}

	public RenderedChunk.ChunkUpdateTask BuildGeometry(int chunkX, int chunkY, int chunkZ, ChunkColumn chunkColumn, int[] borderedChunkBlocks, Dictionary<int, float> borderedChunkBlockHitTimers, ushort[] borderedChunkLightAmounts, uint[] borderedColumnTints, ushort[][] borderedColumnEnvironmentIds, int atlasTextureWidth, int atlasTextureHeight, CancellationToken cancellationToken)
	{
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Invalid comparison between Unknown and I4
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Invalid comparison between Unknown and I4
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Invalid comparison between Unknown and I4
		//IL_099a: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a0: Invalid comparison between Unknown and I4
		//IL_12fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_1303: Invalid comparison between Unknown and I4
		//IL_1347: Unknown result type (might be due to invalid IL or missing references)
		//IL_134d: Invalid comparison between Unknown and I4
		//IL_1393: Unknown result type (might be due to invalid IL or missing references)
		//IL_1399: Invalid comparison between Unknown and I4
		//IL_139d: Unknown result type (might be due to invalid IL or missing references)
		//IL_13a3: Invalid comparison between Unknown and I4
		//IL_13e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_13ee: Invalid comparison between Unknown and I4
		//IL_1927: Unknown result type (might be due to invalid IL or missing references)
		//IL_1933: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ddc: Unknown result type (might be due to invalid IL or missing references)
		//IL_1de8: Unknown result type (might be due to invalid IL or missing references)
		RenderedChunk.ChunkUpdateTask chunkUpdateTask;
		try
		{
			chunkUpdateTask = ChunkUpdateTaskQueue.Take(cancellationToken);
		}
		catch (OperationCanceledException)
		{
			return null;
		}
		bool flag = borderedChunkBlockHitTimers.Count > 0;
		int num = 1191;
		int num2 = 0;
		chunkUpdateTask.OpaqueData.VerticesCount = 0;
		chunkUpdateTask.OpaqueData.IndicesCount = 0;
		chunkUpdateTask.AlphaBlendedData.VerticesCount = 0;
		chunkUpdateTask.AlphaBlendedData.IndicesCount = 0;
		chunkUpdateTask.AlphaTestedData.VerticesCount = 0;
		chunkUpdateTask.AlphaTestedData.IndicesCount = 0;
		chunkUpdateTask.AlphaTestedAnimatedLowLODIndicesCount = 0;
		chunkUpdateTask.AlphaTestedLowLODIndicesCount = 0;
		chunkUpdateTask.AlphaTestedHighLODIndicesCount = 0;
		chunkUpdateTask.AlphaTestedAnimatedHighLODIndicesCount = 0;
		chunkUpdateTask.SolidPlaneMinY = 0;
		chunkUpdateTask.IsUnderground = false;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		Array.Clear(_solidBlockHeight, 0, _solidBlockHeight.Length);
		for (int i = 0; i < 32; i++)
		{
			for (int j = 0; j < 32; j++)
			{
				for (int k = 0; k < 32; k++)
				{
					ClientBlockType clientBlockType = _clientBlockTypes[borderedChunkBlocks[num]];
					byte b = (byte)((clientBlockType.MaxFillLevel == 0) ? 8 : clientBlockType.MaxFillLevel);
					if ((int)clientBlockType.DrawType == 0)
					{
						num++;
						num2++;
						continue;
					}
					if ((int)clientBlockType.DrawType == 2 && (int)clientBlockType.Opacity == 0)
					{
						int num6 = k + 32 * j;
						_solidBlockHeight[num6] = (byte)(i + 1);
					}
					bool flag2 = flag && borderedChunkBlockHitTimers.ContainsKey(num);
					if (clientBlockType.IsAnimated() || flag2)
					{
						num3++;
					}
					int num7 = 0;
					if (clientBlockType.ShouldRenderCube)
					{
						ChunkGeometryData chunkGeometryData = (flag2 ? chunkUpdateTask.AlphaTestedData : ((!clientBlockType.RequiresAlphaBlending) ? chunkUpdateTask.OpaqueData : chunkUpdateTask.AlphaBlendedData));
						for (int l = 0; l < 6; l++)
						{
							int num8 = num + AdjacentBlockOffsetsBySide[l].Main;
							int num9 = borderedChunkBlocks[num8];
							if (num9 == int.MaxValue)
							{
								continue;
							}
							ClientBlockType clientBlockType2 = _clientBlockTypes[num9];
							byte b2 = (byte)((clientBlockType2.MaxFillLevel == 0) ? 8 : clientBlockType2.MaxFillLevel);
							ClientBlockType adjacentTopClientBlockType = null;
							int num10 = num8 + AdjacentBlockOffsetsBySide[0].Main;
							if (num10 < borderedChunkBlocks.Length && borderedChunkBlocks[num10] != int.MaxValue)
							{
								adjacentTopClientBlockType = _clientBlockTypes[borderedChunkBlocks[num10]];
							}
							bool flag3 = false;
							if (num9 == 0 || (!clientBlockType2.ShouldRenderCube && clientBlockType2.VerticalFill == b2) || (clientBlockType2.RequiresAlphaBlending && !clientBlockType.RequiresAlphaBlending) || (flag && borderedChunkBlockHitTimers.ContainsKey(num8)))
							{
								flag3 = true;
							}
							else if (l == 0)
							{
								if (clientBlockType.VerticalFill != b)
								{
									flag3 = true;
								}
								else if (clientBlockType.RequiresAlphaBlending)
								{
									int num11 = num + AdjacentBlockOffsetsBySide[0].Main;
									int num12 = borderedChunkBlocks[num11];
									if (num12 == int.MaxValue || !_clientBlockTypes[num12].ShouldRenderCube)
									{
										flag3 = true;
									}
								}
							}
							if (l != 0 && clientBlockType2.VerticalFill < b2)
							{
								if (clientBlockType.VerticalFill == b)
								{
									flag3 = true;
								}
								else if (l == 1)
								{
									flag3 = true;
								}
								else
								{
									if ((num7 & 1) == 0)
									{
										num7 |= 1;
										chunkGeometryData.VerticesCount += 4;
										chunkGeometryData.IndicesCount += 6;
										if (flag2)
										{
											chunkUpdateTask.AlphaTestedAnimatedLowLODIndicesCount += 6;
										}
									}
									flag3 = clientBlockType2.VerticalFill < clientBlockType.VerticalFill || clientBlockType2.RequiresAlphaBlending;
								}
							}
							if (flag3)
							{
								int num13 = 1 << l;
								if ((num7 & num13) == 0)
								{
									num7 |= num13;
									chunkGeometryData.VerticesCount += 4;
									chunkGeometryData.IndicesCount += 6;
									if (flag2)
									{
										chunkUpdateTask.AlphaTestedAnimatedLowLODIndicesCount += 6;
									}
								}
							}
							if (l >= 2 && ((uint)num7 & (true ? 1u : 0u)) != 0 && ShouldAddTransition(clientBlockType, clientBlockType2, adjacentTopClientBlockType))
							{
								chunkUpdateTask.AlphaTestedData.VerticesCount += 4;
								chunkUpdateTask.AlphaTestedData.IndicesCount += 6;
								chunkUpdateTask.AlphaTestedLowLODIndicesCount += 6;
							}
						}
					}
					_chunkVisibleSideFlags[num2] = (byte)num7;
					if (clientBlockType.Particles != null && (!clientBlockType.ShouldRenderCube || clientBlockType.RequiresAlphaBlending || num7 > 0))
					{
						for (int m = 0; m < clientBlockType.Particles.Length; m++)
						{
							if (clientBlockType.Particles[m].SystemId != null)
							{
								num4++;
							}
						}
					}
					if (ShouldRegisterSound(clientBlockType))
					{
						num5++;
					}
					if (clientBlockType.RenderedBlockyModel != null && (!clientBlockType.ShouldRenderCube || clientBlockType.RequiresAlphaBlending || num7 > 0))
					{
						chunkUpdateTask.AlphaTestedData.VerticesCount += clientBlockType.RenderedBlockyModel.StaticVertices.Length;
						chunkUpdateTask.AlphaTestedData.IndicesCount += clientBlockType.RenderedBlockyModel.StaticIndices.Length;
						if (clientBlockType.IsAnimated() || flag2)
						{
							int num14 = (_LODEnabled ? clientBlockType.RenderedBlockyModel.LowLODIndicesCount : clientBlockType.RenderedBlockyModel.StaticIndices.Length);
							chunkUpdateTask.AlphaTestedAnimatedLowLODIndicesCount += num14;
							chunkUpdateTask.AlphaTestedAnimatedHighLODIndicesCount += clientBlockType.RenderedBlockyModel.StaticIndices.Length - num14;
						}
						else
						{
							int num15 = (_LODEnabled ? clientBlockType.RenderedBlockyModel.LowLODIndicesCount : clientBlockType.RenderedBlockyModel.StaticIndices.Length);
							chunkUpdateTask.AlphaTestedLowLODIndicesCount += num15;
							chunkUpdateTask.AlphaTestedHighLODIndicesCount += clientBlockType.RenderedBlockyModel.StaticIndices.Length - num15;
						}
					}
					num++;
					num2++;
				}
				num += 2;
			}
			num += 68;
		}
		int num16 = 1000;
		for (int n = 0; n < _solidBlockHeight.Length; n++)
		{
			num16 = System.Math.Min(num16, _solidBlockHeight[n]);
			if (_solidBlockHeight[n] == 0)
			{
				num16 = 0;
				break;
			}
		}
		if (num16 > 0)
		{
			chunkUpdateTask.SolidPlaneMinY = num16;
		}
		if (chunkUpdateTask.OpaqueData.IndicesCount == 0 && chunkUpdateTask.AlphaTestedData.IndicesCount == 0 && chunkUpdateTask.AlphaBlendedData.IndicesCount == 0)
		{
			ChunkUpdateTaskQueue.Add(chunkUpdateTask);
			return null;
		}
		if (chunkUpdateTask.OpaqueData.VerticesCount > chunkUpdateTask.OpaqueData.Vertices.Length)
		{
			Array.Resize(ref chunkUpdateTask.OpaqueData.Vertices, chunkUpdateTask.OpaqueData.VerticesCount);
		}
		if (chunkUpdateTask.OpaqueData.IndicesCount > chunkUpdateTask.OpaqueData.Indices.Length)
		{
			Array.Resize(ref chunkUpdateTask.OpaqueData.Indices, chunkUpdateTask.OpaqueData.IndicesCount);
		}
		if (chunkUpdateTask.AlphaBlendedData.VerticesCount > chunkUpdateTask.AlphaBlendedData.Vertices.Length)
		{
			Array.Resize(ref chunkUpdateTask.AlphaBlendedData.Vertices, chunkUpdateTask.AlphaBlendedData.VerticesCount);
		}
		if (chunkUpdateTask.AlphaBlendedData.IndicesCount > chunkUpdateTask.AlphaBlendedData.Indices.Length)
		{
			Array.Resize(ref chunkUpdateTask.AlphaBlendedData.Indices, chunkUpdateTask.AlphaBlendedData.IndicesCount);
		}
		if (chunkUpdateTask.AlphaTestedData.VerticesCount > chunkUpdateTask.AlphaTestedData.Vertices.Length)
		{
			Array.Resize(ref chunkUpdateTask.AlphaTestedData.Vertices, chunkUpdateTask.AlphaTestedData.VerticesCount);
		}
		if (chunkUpdateTask.AlphaTestedData.IndicesCount > chunkUpdateTask.AlphaTestedData.Indices.Length)
		{
			Array.Resize(ref chunkUpdateTask.AlphaTestedData.Indices, chunkUpdateTask.AlphaTestedData.IndicesCount);
		}
		if (num3 > 0)
		{
			chunkUpdateTask.AnimatedBlocks = new RenderedChunk.AnimatedBlock[num3];
		}
		if (num4 > 0)
		{
			chunkUpdateTask.MapParticles = new RenderedChunk.MapParticle[num4];
		}
		if (num5 > 0)
		{
			chunkUpdateTask.SoundObjects = new RenderedChunk.MapSoundObject[num5];
		}
		num = 1191;
		num2 = 0;
		chunkUpdateTask.OpaqueData.VerticesOffset = 0u;
		chunkUpdateTask.OpaqueData.IndicesOffset = 0;
		chunkUpdateTask.AlphaBlendedData.VerticesOffset = 0u;
		chunkUpdateTask.AlphaBlendedData.IndicesOffset = 0;
		chunkUpdateTask.AlphaTestedData.VerticesOffset = 0u;
		chunkUpdateTask.AlphaTestedData.IndicesOffset = 0;
		int alphaTestedLowLODIndicesOffset = 0;
		int num17 = 0;
		int num18 = 0;
		int num19 = 0;
		int num20 = 0;
		int num21 = 0;
		int num22 = 0;
		Array.Clear(_environmentTracker, 0, _environmentTracker.Length);
		for (int num23 = 0; num23 < 32; num23++)
		{
			for (int num24 = 0; num24 < 32; num24++)
			{
				for (int num25 = 0; num25 < 32; num25++)
				{
					ClientBlockType clientBlockType3 = _clientBlockTypes[borderedChunkBlocks[num]];
					if ((int)clientBlockType3.DrawType == 0)
					{
						num++;
						num2++;
						continue;
					}
					bool flag4 = flag && borderedChunkBlockHitTimers.ContainsKey(num);
					byte b3 = _chunkVisibleSideFlags[num2];
					BoundingBox boundingBox = _blockHitboxes[clientBlockType3.HitboxType].BoundingBox;
					float hitboxHeight = boundingBox.Max.Y * 32f;
					if (clientBlockType3.CubeShaderEffect == ClientBlockType.ClientShaderEffect.Water)
					{
						int num26 = chunkY * 32 + num23;
						int num27 = ChunkHelper.IndexInBorderedChunkColumn(num25 + 1, num24 + 1);
						int num28 = num27 * 3;
						if (num26 >= _environmentTracker[num28])
						{
							ChunkHelper.GetEnvironmentId(borderedColumnEnvironmentIds[num27], num26, _environmentTracker, num28);
						}
						ushort num29 = _environmentTracker[num28 + 2];
						int waterTint = _environments[num29].WaterTint;
						byte b4 = (byte)(waterTint >> 16);
						byte b5 = (byte)(waterTint >> 8);
						byte b6 = (byte)waterTint;
						int num30 = num25 + 1;
						int num31 = num23 + 1;
						int num32 = num24 + 1;
						for (int num33 = 0; num33 < _cornerEnvironmentWaterTints.Length; num33++)
						{
							int num34 = 1;
							int num35 = num33 % 4;
							ClientBlockType.ClientShaderEffect clientShaderEffect = ((waterTint != -1) ? ClientBlockType.ClientShaderEffect.WaterEnvironmentColor : ClientBlockType.ClientShaderEffect.Water);
							bool flag5 = clientShaderEffect == ClientBlockType.ClientShaderEffect.WaterEnvironmentColor;
							bool flag6 = !flag5;
							uint num36 = b4;
							uint num37 = b5;
							uint num38 = b6;
							int num39 = 0;
							int num40 = ((num33 < 4) ? 1 : (-1));
							int num41 = 0;
							switch (num35)
							{
							case 0:
								num39 = 1;
								num41 = -1;
								break;
							case 1:
								num39 = -1;
								num41 = -1;
								break;
							case 2:
								num39 = -1;
								num41 = 1;
								break;
							case 3:
								num39 = 1;
								num41 = 1;
								break;
							}
							int x = num30 + num39;
							int y = num31;
							int z = num32;
							int num42 = ChunkHelper.IndexOfBlockInBorderedChunk(x, y, z);
							int num43 = borderedChunkBlocks[num42];
							if (num43 != int.MaxValue && _clientBlockTypes[num43].CubeShaderEffect == ClientBlockType.ClientShaderEffect.Water)
							{
								int num44 = ChunkHelper.IndexInBorderedChunkColumn(x, z);
								num28 = num44 * 3;
								if (num26 >= _environmentTracker[num28])
								{
									ChunkHelper.GetEnvironmentId(borderedColumnEnvironmentIds[num44], num26, _environmentTracker, num28);
								}
								ushort num45 = _environmentTracker[num28 + 2];
								int waterTint2 = _environments[num45].WaterTint;
								if (waterTint2 != -1)
								{
									num36 += (byte)(waterTint2 >> 16);
									num37 += (byte)(waterTint2 >> 8);
									num38 += (byte)waterTint2;
									flag5 = true;
								}
								else
								{
									num36 += 255;
									num37 += 255;
									num38 += 255;
									flag6 = true;
								}
								num34++;
							}
							x = num30;
							y = num31;
							z = num32 + num41;
							int num46 = ChunkHelper.IndexOfBlockInBorderedChunk(x, y, z);
							int num47 = borderedChunkBlocks[num46];
							if (num47 != int.MaxValue && _clientBlockTypes[num47].CubeShaderEffect == ClientBlockType.ClientShaderEffect.Water)
							{
								int num48 = ChunkHelper.IndexInBorderedChunkColumn(x, z);
								num28 = num48 * 3;
								if (num26 >= _environmentTracker[num28])
								{
									ChunkHelper.GetEnvironmentId(borderedColumnEnvironmentIds[num48], num26, _environmentTracker, num28);
								}
								ushort num49 = _environmentTracker[num28 + 2];
								int waterTint3 = _environments[num49].WaterTint;
								if (waterTint3 != -1)
								{
									num36 += (byte)(waterTint3 >> 16);
									num37 += (byte)(waterTint3 >> 8);
									num38 += (byte)waterTint3;
									flag5 = true;
								}
								else
								{
									num36 += 255;
									num37 += 255;
									num38 += 255;
									flag6 = true;
								}
								num34++;
							}
							x = num30 + num39;
							y = num31;
							z = num32 + num41;
							int num50 = ChunkHelper.IndexOfBlockInBorderedChunk(x, y, z);
							int num51 = borderedChunkBlocks[num50];
							if (num51 != int.MaxValue && _clientBlockTypes[num51].CubeShaderEffect == ClientBlockType.ClientShaderEffect.Water)
							{
								int num52 = ChunkHelper.IndexInBorderedChunkColumn(x, z);
								num28 = num52 * 3;
								if (num26 >= _environmentTracker[num28])
								{
									ChunkHelper.GetEnvironmentId(borderedColumnEnvironmentIds[num52], num26, _environmentTracker, num28);
								}
								ushort num53 = _environmentTracker[num28 + 2];
								int waterTint4 = _environments[num53].WaterTint;
								if (waterTint4 != -1)
								{
									num36 += (byte)(waterTint4 >> 16);
									num37 += (byte)(waterTint4 >> 8);
									num38 += (byte)waterTint4;
									flag5 = true;
								}
								else
								{
									num36 += 255;
									num37 += 255;
									num38 += 255;
									flag6 = true;
								}
								num34++;
							}
							x = num30 + num39;
							y = num31 + num40;
							z = num32;
							int num54 = ChunkHelper.IndexOfBlockInBorderedChunk(x, y, z);
							int num55 = borderedChunkBlocks[num54];
							if (num55 != int.MaxValue && _clientBlockTypes[num55].CubeShaderEffect == ClientBlockType.ClientShaderEffect.Water)
							{
								int num56 = ChunkHelper.IndexInBorderedChunkColumn(x, z);
								num28 = num56 * 3;
								if (num26 + num40 >= _environmentTracker[num28])
								{
									ChunkHelper.GetEnvironmentId(borderedColumnEnvironmentIds[num56], num26 + num40, _environmentTracker, num28);
								}
								ushort num57 = _environmentTracker[num28 + 2];
								int waterTint5 = _environments[num57].WaterTint;
								if (waterTint5 != -1)
								{
									num36 += (byte)(waterTint5 >> 16);
									num37 += (byte)(waterTint5 >> 8);
									num38 += (byte)waterTint5;
									flag5 = true;
								}
								else
								{
									num36 += 255;
									num37 += 255;
									num38 += 255;
									flag6 = true;
								}
								num34++;
							}
							x = num30;
							y = num31 + num40;
							z = num32 + num41;
							int num58 = ChunkHelper.IndexOfBlockInBorderedChunk(x, y, z);
							int num59 = borderedChunkBlocks[num58];
							if (num59 != int.MaxValue && _clientBlockTypes[num59].CubeShaderEffect == ClientBlockType.ClientShaderEffect.Water)
							{
								int num60 = ChunkHelper.IndexInBorderedChunkColumn(x, z);
								num28 = num60 * 3;
								if (num26 + num40 >= _environmentTracker[num28])
								{
									ChunkHelper.GetEnvironmentId(borderedColumnEnvironmentIds[num60], num26 + num40, _environmentTracker, num28);
								}
								ushort num61 = _environmentTracker[num28 + 2];
								int waterTint6 = _environments[num61].WaterTint;
								if (waterTint6 != -1)
								{
									num36 += (byte)(waterTint6 >> 16);
									num37 += (byte)(waterTint6 >> 8);
									num38 += (byte)waterTint6;
									flag5 = true;
								}
								else
								{
									num36 += 255;
									num37 += 255;
									num38 += 255;
									flag6 = true;
								}
								num34++;
							}
							int num62 = ChunkHelper.IndexOfBlockInBorderedChunk(num30 + num39, num31 + num40, num32 + num41);
							int num63 = borderedChunkBlocks[num62];
							if (num63 != int.MaxValue && _clientBlockTypes[num63].CubeShaderEffect == ClientBlockType.ClientShaderEffect.Water)
							{
								int num64 = ChunkHelper.IndexInBorderedChunkColumn(num30 + num39, num32 + num41);
								num28 = num64 * 3;
								if (num26 + num40 >= _environmentTracker[num28])
								{
									ChunkHelper.GetEnvironmentId(borderedColumnEnvironmentIds[num64], num26 + num40, _environmentTracker, num28);
								}
								ushort num65 = _environmentTracker[num28 + 2];
								int waterTint7 = _environments[num65].WaterTint;
								if (waterTint7 != -1)
								{
									num36 += (byte)(waterTint7 >> 16);
									num37 += (byte)(waterTint7 >> 8);
									num38 += (byte)waterTint7;
									flag5 = true;
								}
								else
								{
									num36 += 255;
									num37 += 255;
									num38 += 255;
									flag6 = true;
								}
								num34++;
							}
							x = num30;
							y = num31 + num40;
							z = num32;
							int num66 = ChunkHelper.IndexOfBlockInBorderedChunk(x, y, z);
							int num67 = borderedChunkBlocks[num66];
							if (num67 != int.MaxValue && _clientBlockTypes[num67].CubeShaderEffect == ClientBlockType.ClientShaderEffect.Water)
							{
								int num68 = ChunkHelper.IndexInBorderedChunkColumn(x, z);
								num28 = num68 * 3;
								if (num26 + num40 >= _environmentTracker[num28])
								{
									ChunkHelper.GetEnvironmentId(borderedColumnEnvironmentIds[num68], num26 + num40, _environmentTracker, num28);
								}
								ushort num69 = _environmentTracker[num28 + 2];
								int waterTint8 = _environments[num69].WaterTint;
								if (waterTint8 != -1)
								{
									num36 += (byte)(waterTint8 >> 16);
									num37 += (byte)(waterTint8 >> 8);
									num38 += (byte)waterTint8;
									flag5 = true;
								}
								else
								{
									num36 += 255;
									num37 += 255;
									num38 += 255;
									flag6 = true;
								}
								num34++;
							}
							if (flag6 && flag5)
							{
								clientShaderEffect = ClientBlockType.ClientShaderEffect.WaterEnvironmentTransition;
							}
							num36 = (uint)(num36 / num34);
							num37 = (uint)(num37 / num34);
							num38 = (uint)(num38 / num34);
							_cornerEnvironmentWaterTints[num33] = (num36 << 16) | (num37 << 8) | num38 | (uint)((int)clientShaderEffect << 24);
						}
					}
					int? seed = null;
					int num70 = (num24 << 5) + num25;
					uint biomeTintColor = chunkColumn.Tints[num70];
					float num71 = 0f;
					float num72 = 0f;
					float num73 = 0f;
					if ((int)clientBlockType3.RandomRotation == 4)
					{
						seed = MathHelper.Hash(num25, num23, num24);
						int num74 = System.Math.Abs(seed.Value + MathHelper.HashOne);
						num71 = (float)(num74 % 4) * ((float)System.Math.PI / 2f);
					}
					else if ((int)clientBlockType3.RandomRotation == 3)
					{
						seed = MathHelper.Hash(num25, num24);
						int num74 = System.Math.Abs(seed.Value + MathHelper.HashFive);
						num71 = MathHelper.ToRadians(num74 % 360);
					}
					else
					{
						if ((int)clientBlockType3.RandomRotation == 2 || (int)clientBlockType3.RandomRotation == 1)
						{
							seed = MathHelper.Hash(num25, num23, num24);
							int num74 = System.Math.Abs(seed.Value + MathHelper.HashTwo);
							num71 = MathHelper.ToRadians(num74 % 360);
						}
						if ((int)clientBlockType3.RandomRotation == 1)
						{
							if (!seed.HasValue)
							{
								seed = MathHelper.Hash(num25, num23, num24);
							}
							int num74 = System.Math.Abs(seed.Value + MathHelper.HashThree);
							num72 = MathHelper.ToRadians(num74 % 360);
							num74 = System.Math.Abs(seed.Value + MathHelper.HashFour);
							num73 = MathHelper.ToRadians(num74 % 360);
						}
					}
					if (num71 != 0f || num72 != 0f || num73 != 0f)
					{
						Matrix.CreateFromYawPitchRoll(num71, num72, num73, out _tempBlockRotationMatrix);
						Matrix.Multiply(ref _tempBlockRotationMatrix, ref clientBlockType3.RotationMatrix, out _tempBlockRotationMatrix);
						Matrix.Multiply(ref clientBlockType3.BlockyModelTranslatedScaleMatrix, ref NegativeHalfBlockOffsetMatrix, out _tempBlockWorldMatrix);
						Matrix.Multiply(ref _tempBlockWorldMatrix, ref _tempBlockRotationMatrix, out _tempBlockWorldMatrix);
						Matrix.Multiply(ref _tempBlockWorldMatrix, ref PositiveHalfBlockOffsetMatrix, out _tempBlockWorldMatrix);
						Matrix.Invert(ref _tempBlockWorldMatrix, out _tempCubeBlockWorldInvertMatrix);
						Matrix.AddTranslation(ref _tempCubeBlockWorldInvertMatrix, 0f, -16f, 0f);
					}
					else
					{
						_tempBlockWorldMatrix = clientBlockType3.WorldMatrix;
						_tempBlockRotationMatrix = clientBlockType3.RotationMatrix;
						_tempCubeBlockWorldInvertMatrix = clientBlockType3.CubeBlockInvertMatrix;
					}
					Matrix.AddTranslation(ref _tempBlockWorldMatrix, num25, num23, num24);
					if (clientBlockType3.IsAnimated() || flag4)
					{
						float val = System.Math.Max(System.Math.Abs(boundingBox.Min.X), boundingBox.Max.X);
						float val2 = System.Math.Max(System.Math.Abs(boundingBox.Min.Z), boundingBox.Max.Z);
						float num75 = System.Math.Max(val, val2);
						Vector3 position = new Vector3(chunkX * 32 + num25 - clientBlockType3.FillerX, chunkY * 32 + num23 - clientBlockType3.FillerY, chunkZ * 32 + num24 - clientBlockType3.FillerZ);
						BoundingBox boundingBox2 = new BoundingBox(new Vector3(position.X - num75, position.Y - boundingBox.Min.Y, position.Z - num75), new Vector3(position.X + num75, position.Y + boundingBox.Max.Y, position.Z + num75));
						ChunkGeometryData chunkGeometryData2 = new ChunkGeometryData();
						if (clientBlockType3.RenderedBlockyModel != null)
						{
							chunkGeometryData2.VerticesCount += clientBlockType3.RenderedBlockyModel.AnimatedVertices.Length;
							chunkGeometryData2.IndicesCount += clientBlockType3.RenderedBlockyModel.AnimatedIndices.Length;
						}
						if (clientBlockType3.ShouldRenderCube)
						{
							chunkGeometryData2.VerticesCount += 24;
							chunkGeometryData2.IndicesCount += 36;
						}
						chunkGeometryData2.Vertices = new ChunkVertex[chunkGeometryData2.VerticesCount];
						chunkGeometryData2.Indices = new uint[chunkGeometryData2.IndicesCount];
						if (clientBlockType3.RenderedBlockyModel != null)
						{
							for (int num76 = 0; num76 < clientBlockType3.RenderedBlockyModel.AnimatedIndices.Length; num76++)
							{
								chunkGeometryData2.Indices[chunkGeometryData2.IndicesOffset + num76] = chunkGeometryData2.VerticesOffset + clientBlockType3.RenderedBlockyModel.AnimatedIndices[num76];
							}
							chunkGeometryData2.IndicesOffset += clientBlockType3.RenderedBlockyModel.AnimatedIndices.Length;
						}
						CreateBlockGeometry(_clientBlockTypes, _lightLevels, clientBlockType3, num, hitboxHeight, Vector3.Zero, num25, num23, num24, ref seed, byte.MaxValue, Matrix.Identity, _tempBlockRotationMatrix, _tempCubeBlockWorldInvertMatrix, _texCoordsByCorner, _sideMaskTexCoordsByCorner, _cornerOcclusions, _cornerShaderEffects, biomeTintColor, borderedChunkBlocks, borderedChunkLightAmounts, borderedColumnTints, _cornerEnvironmentWaterTints, atlasTextureWidth, atlasTextureHeight, chunkGeometryData2, chunkGeometryData2, chunkUpdateTask.AlphaTestedAnimatedLowLODIndicesCount, ref alphaTestedLowLODIndicesOffset, isAnimated: true);
						float animationTimeOffset = 0f;
						if (clientBlockType3.BlockyAnimation != null)
						{
							if (!seed.HasValue)
							{
								seed = MathHelper.Hash(num25, num23, num24);
							}
							animationTimeOffset = System.Math.Abs(seed.Value) % clientBlockType3.BlockyAnimation.Duration;
						}
						chunkUpdateTask.AnimatedBlocks[num19] = new RenderedChunk.AnimatedBlock
						{
							Index = num2,
							IsBeingHit = flag4,
							Position = position,
							BoundingBox = boundingBox2,
							Matrix = _tempBlockWorldMatrix,
							Renderer = new AnimatedBlockRenderer(clientBlockType3.FinalBlockyModel, _atlasSizes, chunkGeometryData2),
							Animation = clientBlockType3.BlockyAnimation,
							AnimationTimeOffset = animationTimeOffset
						};
						if (clientBlockType3.Particles != null)
						{
							chunkUpdateTask.AnimatedBlocks[num19].MapParticleIndices = new int[clientBlockType3.Particles.Length];
							Vector3 position2 = new Vector3(chunkX * 32 + num25, chunkY * 32 + num23, chunkZ * 32 + num24);
							Quaternion rotation = Quaternion.CreateFromYawPitchRoll(MathHelper.RotationToRadians(clientBlockType3.RotationYaw), MathHelper.RotationToRadians(clientBlockType3.RotationPitch), 0f);
							Quaternion quaternion = Quaternion.CreateFromYawPitchRoll(num71, num72, num73);
							rotation *= quaternion;
							Vector3 vector = Vector3.Transform(new Vector3(0f, -0.5f, 0f), rotation);
							position2 += new Vector3(0.5f, 0.5f, 0.5f) + vector;
							for (int num77 = 0; num77 < clientBlockType3.Particles.Length; num77++)
							{
								ref ModelParticleSettings reference = ref clientBlockType3.Particles[num77];
								chunkUpdateTask.MapParticles[num20] = new RenderedChunk.MapParticle
								{
									BlockIndex = num2,
									Position = position2,
									Rotation = rotation,
									PositionOffset = reference.PositionOffset,
									RotationOffset = reference.RotationOffset,
									TargetNodeIndex = reference.TargetNodeIndex,
									ParticleSystemId = reference.SystemId,
									Color = reference.Color,
									BlockScale = clientBlockType3.BlockyModelScale,
									Scale = clientBlockType3.BlockyModelScale * reference.Scale
								};
								chunkUpdateTask.AnimatedBlocks[num19].MapParticleIndices[num77] = num20;
								num20++;
							}
						}
						num19++;
					}
					if (clientBlockType3.RenderedBlockyModel != null && (!clientBlockType3.ShouldRenderCube || clientBlockType3.RequiresAlphaBlending || b3 > 0))
					{
						if (clientBlockType3.IsAnimated() || flag4)
						{
							int num78 = (_LODEnabled ? clientBlockType3.RenderedBlockyModel.LowLODIndicesCount : clientBlockType3.RenderedBlockyModel.StaticIndices.Length);
							int indicesOffset = chunkUpdateTask.AlphaTestedData.IndicesOffset;
							for (int num79 = 0; num79 < num78; num79++)
							{
								chunkUpdateTask.AlphaTestedData.Indices[indicesOffset + num79] = chunkUpdateTask.AlphaTestedData.VerticesOffset + clientBlockType3.RenderedBlockyModel.StaticIndices[num79];
							}
							chunkUpdateTask.AlphaTestedData.IndicesOffset += num78;
							int num80 = chunkUpdateTask.AlphaTestedAnimatedLowLODIndicesCount + chunkUpdateTask.AlphaTestedLowLODIndicesCount + chunkUpdateTask.AlphaTestedHighLODIndicesCount + num18;
							for (int num81 = 0; num81 < clientBlockType3.RenderedBlockyModel.StaticIndices.Length - num78; num81++)
							{
								chunkUpdateTask.AlphaTestedData.Indices[num80 + num81] = chunkUpdateTask.AlphaTestedData.VerticesOffset + clientBlockType3.RenderedBlockyModel.StaticIndices[num78 + num81];
							}
							num18 += clientBlockType3.RenderedBlockyModel.StaticIndices.Length - num78;
						}
						else
						{
							int num82 = (_LODEnabled ? clientBlockType3.RenderedBlockyModel.LowLODIndicesCount : clientBlockType3.RenderedBlockyModel.StaticIndices.Length);
							int num83 = chunkUpdateTask.AlphaTestedAnimatedLowLODIndicesCount + alphaTestedLowLODIndicesOffset;
							for (int num84 = 0; num84 < num82; num84++)
							{
								chunkUpdateTask.AlphaTestedData.Indices[num83 + num84] = chunkUpdateTask.AlphaTestedData.VerticesOffset + clientBlockType3.RenderedBlockyModel.StaticIndices[num84];
							}
							alphaTestedLowLODIndicesOffset += num82;
							int num85 = chunkUpdateTask.AlphaTestedAnimatedLowLODIndicesCount + chunkUpdateTask.AlphaTestedLowLODIndicesCount + num17;
							for (int num86 = 0; num86 < clientBlockType3.RenderedBlockyModel.StaticIndices.Length - num82; num86++)
							{
								chunkUpdateTask.AlphaTestedData.Indices[num85 + num86] = chunkUpdateTask.AlphaTestedData.VerticesOffset + clientBlockType3.RenderedBlockyModel.StaticIndices[num82 + num86];
							}
							num17 += clientBlockType3.RenderedBlockyModel.StaticIndices.Length - num82;
						}
					}
					if (ShouldRegisterSound(clientBlockType3))
					{
						Vector3 position3 = new Vector3(chunkX * 32 + num25, chunkY * 32 + num23, chunkZ * 32 + num24);
						chunkUpdateTask.SoundObjects[num21] = new RenderedChunk.MapSoundObject
						{
							BlockIndex = num2,
							SoundEventReference = AudioDevice.SoundEventReference.None,
							SoundEventIndex = clientBlockType3.SoundEventIndex,
							Position = position3
						};
						num21++;
					}
					if (clientBlockType3.Particles != null && !flag4 && ((clientBlockType3.RenderedBlockyModel != null && !clientBlockType3.IsAnimated()) || (clientBlockType3.ShouldRenderCube && (clientBlockType3.RequiresAlphaBlending || b3 > 0))))
					{
						Vector3 vector2 = new Vector3(chunkX * 32 + num25, chunkY * 32 + num23, chunkZ * 32 + num24);
						Quaternion quaternion2 = Quaternion.CreateFromYawPitchRoll(MathHelper.RotationToRadians(clientBlockType3.RotationYaw), MathHelper.RotationToRadians(clientBlockType3.RotationPitch), 0f);
						Quaternion quaternion3 = Quaternion.CreateFromYawPitchRoll(num71, num72, num73);
						quaternion2 *= quaternion3;
						Vector3 vector3 = Vector3.Transform(new Vector3(0f, -0.5f, 0f), quaternion2);
						vector2 += new Vector3(0.5f, 0.5f, 0.5f) + vector3;
						for (int num87 = 0; num87 < clientBlockType3.Particles.Length; num87++)
						{
							ref ModelParticleSettings reference2 = ref clientBlockType3.Particles[num87];
							Vector3 zero = Vector3.Zero;
							Quaternion identity = Quaternion.Identity;
							if (clientBlockType3.RenderedBlockyModel != null)
							{
								zero = Vector3.Transform(clientBlockType3.RenderedBlockyModel.NodeParentTransforms[reference2.TargetNodeIndex].Position * (1f / 32f) * clientBlockType3.BlockyModelScale, quaternion2) + Vector3.Transform(reference2.PositionOffset * clientBlockType3.BlockyModelScale, quaternion2 * clientBlockType3.RenderedBlockyModel.NodeParentTransforms[reference2.TargetNodeIndex].Orientation);
								identity = clientBlockType3.RenderedBlockyModel.NodeParentTransforms[reference2.TargetNodeIndex].Orientation * reference2.RotationOffset;
							}
							else
							{
								zero = Vector3.Transform(Vector3.Up * 0.5f * clientBlockType3.BlockyModelScale, quaternion2) + Vector3.Transform(reference2.PositionOffset * clientBlockType3.BlockyModelScale, quaternion2);
								identity = reference2.RotationOffset;
							}
							chunkUpdateTask.MapParticles[num20] = new RenderedChunk.MapParticle
							{
								BlockIndex = num2,
								Position = vector2 + zero,
								RotationOffset = quaternion2 * identity,
								TargetNodeIndex = reference2.TargetNodeIndex,
								ParticleSystemId = reference2.SystemId,
								Color = reference2.Color,
								Scale = clientBlockType3.BlockyModelScale * reference2.Scale
							};
							num20++;
						}
					}
					ChunkGeometryData cubeVertexData = (flag4 ? chunkUpdateTask.AlphaTestedData : ((!clientBlockType3.RequiresAlphaBlending) ? chunkUpdateTask.OpaqueData : chunkUpdateTask.AlphaBlendedData));
					ushort num88 = borderedChunkLightAmounts[num];
					int val3 = (num88 >> 12) & 0xF;
					num22 = System.Math.Max(val3, num22);
					CreateBlockGeometry(_clientBlockTypes, _lightLevels, clientBlockType3, num, hitboxHeight, new Vector3(num25, num23, num24), num25, num23, num24, ref seed, b3, _tempBlockWorldMatrix, _tempBlockRotationMatrix, Matrix.Identity, _texCoordsByCorner, _sideMaskTexCoordsByCorner, _cornerOcclusions, _cornerShaderEffects, biomeTintColor, borderedChunkBlocks, borderedChunkLightAmounts, borderedColumnTints, _cornerEnvironmentWaterTints, atlasTextureWidth, atlasTextureHeight, cubeVertexData, chunkUpdateTask.AlphaTestedData, chunkUpdateTask.AlphaTestedAnimatedLowLODIndicesCount, ref alphaTestedLowLODIndicesOffset, isAnimated: false);
					num++;
					num2++;
				}
				num += 2;
			}
			num += 68;
		}
		chunkUpdateTask.IsUnderground = num22 <= 2;
		if (chunkUpdateTask.OpaqueData.VerticesOffset != chunkUpdateTask.OpaqueData.VerticesCount)
		{
			throw new Exception("Invalid opaque vertex count");
		}
		if (chunkUpdateTask.OpaqueData.IndicesOffset != chunkUpdateTask.OpaqueData.IndicesCount)
		{
			throw new Exception("Invalid opaque index count");
		}
		if (chunkUpdateTask.AlphaBlendedData.VerticesOffset != chunkUpdateTask.AlphaBlendedData.VerticesCount)
		{
			throw new Exception("Invalid alpha-blended vertex count");
		}
		if (chunkUpdateTask.AlphaBlendedData.IndicesOffset != chunkUpdateTask.AlphaBlendedData.IndicesCount)
		{
			throw new Exception("Invalid alpha-blended index count");
		}
		if (chunkUpdateTask.AlphaTestedData.VerticesOffset != chunkUpdateTask.AlphaTestedData.VerticesCount)
		{
			throw new Exception("Invalid alpha-tested vertex count");
		}
		if (chunkUpdateTask.AlphaTestedData.IndicesOffset != chunkUpdateTask.AlphaTestedAnimatedLowLODIndicesCount)
		{
			throw new Exception("Invalid alpha-tested low LOD animated indices count");
		}
		if (alphaTestedLowLODIndicesOffset != chunkUpdateTask.AlphaTestedLowLODIndicesCount)
		{
			throw new Exception("Invalid alpha-tested low LOD indices count");
		}
		if (num17 != chunkUpdateTask.AlphaTestedHighLODIndicesCount)
		{
			throw new Exception("Invalid alpha-tested high LOD indices count");
		}
		if (num18 != chunkUpdateTask.AlphaTestedAnimatedHighLODIndicesCount)
		{
			throw new Exception("Invalid alpha-tested high LOD animated indices count");
		}
		return chunkUpdateTask;
	}

	public static void CreateBlockGeometry(ClientBlockType[] clientBlockTypes, float[] lightLevels, ClientBlockType blockType, int borderedBlockIndex, float hitboxHeight, Vector3 cubeOffset, int chunkX, int chunkY, int chunkZ, ref int? seed, byte visibleSideFlags, Matrix blockyModelMatrix, Matrix blockModelRotationMatrix, Matrix cubeMatrix, UShortVector2[] texCoordsByCorner, UShortVector2[] sideMaskTexCoordsByCorner, int[] cornerOcclusions, ClientBlockType.ClientShaderEffect[] cornerShaderEffects, uint biomeTintColor, int[] borderedChunkBlocks, ushort[] borderedLightAmounts, uint[] borderedColumnTints, uint[] environmentTints, int atlasTextureWidth, int atlasTextureHeight, ChunkGeometryData cubeVertexData, ChunkGeometryData alphaTestedVertexData, int alphaTestedAnimatedLowLODIndicesStart, ref int alphaTestedLowLODIndicesOffset, bool isAnimated)
	{
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Invalid comparison between Unknown and I4
		byte b = (byte)(biomeTintColor >> 16);
		byte b2 = (byte)(biomeTintColor >> 8);
		byte b3 = (byte)biomeTintColor;
		if (!NoTint.Equals(ForceTint))
		{
			b = (byte)ForceTint.X;
			b2 = (byte)ForceTint.Y;
			b3 = (byte)ForceTint.Z;
		}
		ShortVector3 shortVector = default(ShortVector3);
		if (blockType.RenderedBlockyModel != null && (!blockType.ShouldRenderCube || blockType.RequiresAlphaBlending || visibleSideFlags > 0))
		{
			int num = blockType.SelfTintColorsBySide[0];
			byte b4 = (byte)(num >> 16);
			byte b5 = (byte)(num >> 8);
			byte b6 = (byte)num;
			float num2 = blockType.BiomeTintMultipliersBySide[0];
			uint num3 = (uint)((float)(int)b4 + (float)(b - b4) * num2);
			uint num4 = (uint)((float)(int)b5 + (float)(b2 - b5) * num2);
			uint num5 = (uint)((float)(int)b6 + (float)(b3 - b6) * num2);
			uint num6 = num3 | (num4 << 8) | (num5 << 16);
			uint useBillboard = (blockType.RenderedBlockyModel.UsesBillboardLOD ? 1u : 0u);
			uint num7 = ((blockType.RenderedBlockyModel.HasOnlyQuads || (int)blockType.CollisionMaterial == 1) ? 32u : 0u);
			ushort num8 = borderedLightAmounts[borderedBlockIndex];
			uint num9 = 0u;
			for (int i = 0; i < 4; i++)
			{
				int num10 = (num8 >> i * 4) & 0xF;
				float num11 = lightLevels[num10];
				num9 |= (uint)(num11 * 255f) << i * 8;
			}
			int num12 = 0;
			if (blockType.BlockyTextureWeights.Length > 1)
			{
				if (!seed.HasValue)
				{
					seed = MathHelper.Hash(chunkX, chunkY, chunkZ);
				}
				num12 = CalculateBlockTextureIndex(blockType.BlockyTextureWeights, seed.Value);
			}
			Vector2 vector = blockType.RenderedBlockyModelTextureOrigins[num12];
			ushort num13 = VertexCompression.CompressBlockLocalPosition(chunkX, chunkY, chunkZ);
			StaticBlockyModelVertex[] array = (isAnimated ? blockType.RenderedBlockyModel.AnimatedVertices : blockType.RenderedBlockyModel.StaticVertices);
			for (int j = 0; j < array.Length / 4; j++)
			{
				int num14 = j * 4;
				long num15 = alphaTestedVertexData.VerticesOffset + num14;
				Vector3.Transform(ref array[num14].Position, ref blockyModelMatrix, out var result);
				Vector3.Transform(ref array[num14 + 1].Position, ref blockyModelMatrix, out var result2);
				Vector3.Transform(ref array[num14 + 2].Position, ref blockyModelMatrix, out var result3);
				Vector3.Transform(ref array[num14 + 3].Position, ref blockyModelMatrix, out var result4);
				alphaTestedVertexData.Vertices[num15].PositionPacked = VertexCompression.Vector3PositionToShortVector3(result);
				alphaTestedVertexData.Vertices[num15 + 1].PositionPacked = VertexCompression.Vector3PositionToShortVector3(result2);
				alphaTestedVertexData.Vertices[num15 + 2].PositionPacked = VertexCompression.Vector3PositionToShortVector3(result3);
				alphaTestedVertexData.Vertices[num15 + 3].PositionPacked = VertexCompression.Vector3PositionToShortVector3(result4);
				ushort doubleSidedAndBlockId = (ushort)(((1 & array[num14].DoubleSided) << 15) | num13);
				alphaTestedVertexData.Vertices[num15].DoubleSidedAndBlockId = doubleSidedAndBlockId;
				alphaTestedVertexData.Vertices[num15 + 1].DoubleSidedAndBlockId = doubleSidedAndBlockId;
				alphaTestedVertexData.Vertices[num15 + 2].DoubleSidedAndBlockId = doubleSidedAndBlockId;
				alphaTestedVertexData.Vertices[num15 + 3].DoubleSidedAndBlockId = doubleSidedAndBlockId;
				Vector3 vector2 = ((!isAnimated) ? Vector3.TransformNormal(array[num14].Normal, blockModelRotationMatrix) : array[num14].Normal);
				uint normalAndNodeIndex = VertexCompression.NormalizedXYZToUint(vector2.X, vector2.Y, vector2.Z) | (uint)(array[num14].NodeIndex << 24);
				alphaTestedVertexData.Vertices[num15].NormalAndNodeIndex = normalAndNodeIndex;
				alphaTestedVertexData.Vertices[num15].TextureCoordinates.X = VertexCompression.NormalizedTexCoordToUshort(array[num14].TextureCoordinates.X + vector.X);
				alphaTestedVertexData.Vertices[num15].TextureCoordinates.Y = VertexCompression.NormalizedTexCoordToUshort(array[num14].TextureCoordinates.Y + vector.Y);
				alphaTestedVertexData.Vertices[num15].MaskTextureCoordinates = NoSideMaskUV;
				alphaTestedVertexData.Vertices[num15].GlowColorAndSunlight = num9;
				alphaTestedVertexData.Vertices[num15].UseBillboard = useBillboard;
				uint num16 = ((blockType.BlockyModelShaderEffect == ClientBlockType.ClientShaderEffect.WindAttached) ? ((uint)(MathHelper.Clamp(array[num14].Position.Y / hitboxHeight, 0f, 1f) * 14f)) : ((uint)blockType.BlockyModelShaderEffect));
				alphaTestedVertexData.Vertices[num15].TintColorAndEffectAndShadingMode = num6 | ((num7 | num16) << 24) | ((uint)array[num14].ShadingMode << 30);
				num14++;
				num15++;
				alphaTestedVertexData.Vertices[num15].NormalAndNodeIndex = normalAndNodeIndex;
				alphaTestedVertexData.Vertices[num15].TextureCoordinates.X = VertexCompression.NormalizedTexCoordToUshort(array[num14].TextureCoordinates.X + vector.X);
				alphaTestedVertexData.Vertices[num15].TextureCoordinates.Y = VertexCompression.NormalizedTexCoordToUshort(array[num14].TextureCoordinates.Y + vector.Y);
				alphaTestedVertexData.Vertices[num15].MaskTextureCoordinates = NoSideMaskUV;
				alphaTestedVertexData.Vertices[num15].GlowColorAndSunlight = num9;
				alphaTestedVertexData.Vertices[num15].UseBillboard = useBillboard;
				num16 = ((blockType.BlockyModelShaderEffect == ClientBlockType.ClientShaderEffect.WindAttached) ? ((uint)(MathHelper.Clamp(array[num14].Position.Y / hitboxHeight, 0f, 1f) * 14f)) : ((uint)blockType.BlockyModelShaderEffect));
				alphaTestedVertexData.Vertices[num15].TintColorAndEffectAndShadingMode = num6 | ((num7 | num16) << 24) | ((uint)array[num14].ShadingMode << 30);
				num14++;
				num15++;
				alphaTestedVertexData.Vertices[num15].NormalAndNodeIndex = normalAndNodeIndex;
				alphaTestedVertexData.Vertices[num15].TextureCoordinates.X = VertexCompression.NormalizedTexCoordToUshort(array[num14].TextureCoordinates.X + vector.X);
				alphaTestedVertexData.Vertices[num15].TextureCoordinates.Y = VertexCompression.NormalizedTexCoordToUshort(array[num14].TextureCoordinates.Y + vector.Y);
				alphaTestedVertexData.Vertices[num15].MaskTextureCoordinates = NoSideMaskUV;
				alphaTestedVertexData.Vertices[num15].GlowColorAndSunlight = num9;
				alphaTestedVertexData.Vertices[num15].UseBillboard = useBillboard;
				num16 = ((blockType.BlockyModelShaderEffect == ClientBlockType.ClientShaderEffect.WindAttached) ? ((uint)(MathHelper.Clamp(array[num14].Position.Y / hitboxHeight, 0f, 1f) * 14f)) : ((uint)blockType.BlockyModelShaderEffect));
				alphaTestedVertexData.Vertices[num15].TintColorAndEffectAndShadingMode = num6 | ((num7 | num16) << 24) | ((uint)array[num14].ShadingMode << 30);
				num14++;
				num15++;
				alphaTestedVertexData.Vertices[num15].NormalAndNodeIndex = normalAndNodeIndex;
				alphaTestedVertexData.Vertices[num15].TextureCoordinates.X = VertexCompression.NormalizedTexCoordToUshort(array[num14].TextureCoordinates.X + vector.X);
				alphaTestedVertexData.Vertices[num15].TextureCoordinates.Y = VertexCompression.NormalizedTexCoordToUshort(array[num14].TextureCoordinates.Y + vector.Y);
				alphaTestedVertexData.Vertices[num15].MaskTextureCoordinates = NoSideMaskUV;
				alphaTestedVertexData.Vertices[num15].GlowColorAndSunlight = num9;
				alphaTestedVertexData.Vertices[num15].UseBillboard = useBillboard;
				num16 = ((blockType.BlockyModelShaderEffect == ClientBlockType.ClientShaderEffect.WindAttached) ? ((uint)(MathHelper.Clamp(array[num14].Position.Y / hitboxHeight, 0f, 1f) * 14f)) : ((uint)blockType.BlockyModelShaderEffect));
				alphaTestedVertexData.Vertices[num15].TintColorAndEffectAndShadingMode = num6 | ((num7 | num16) << 24) | ((uint)array[num14].ShadingMode << 30);
			}
			alphaTestedVertexData.VerticesOffset += (uint)array.Length;
		}
		if (!blockType.ShouldRenderCube || visibleSideFlags <= 0)
		{
			return;
		}
		ushort num17 = 0;
		ushort num18 = 0;
		ushort num19 = 0;
		ushort num20 = 0;
		uint num21 = 0u;
		bool flag = false;
		uint num22 = (blockType.RequiresAlphaBlending ? 1u : 0u);
		bool flag2 = (visibleSideFlags & 1) != 0;
		byte b7 = (byte)((blockType.MaxFillLevel == 0) ? 8 : blockType.MaxFillLevel);
		float num23 = (float)(int)blockType.VerticalFill / (float)(int)b7;
		int num24 = 0;
		if (blockType.CubeTextureWeights.Length > 1)
		{
			if (!seed.HasValue)
			{
				seed = MathHelper.Hash(chunkX, chunkY, chunkZ);
			}
			num24 = CalculateBlockTextureIndex(blockType.CubeTextureWeights, seed.Value);
		}
		for (int k = 0; k < 6; k++)
		{
			AdjacentBlockOffsets adjacentBlockOffsets = AdjacentBlockOffsetsBySide[k];
			int num25 = borderedBlockIndex + adjacentBlockOffsets.Main;
			if (k == 0 && blockType.VerticalFill < b7)
			{
				num25 = borderedBlockIndex;
			}
			byte b8 = (byte)(1 << k);
			if ((visibleSideFlags & b8) != 0)
			{
				ClientBlockType.CubeTexture cubeTexture = blockType.CubeTextures[k];
				int num26 = atlasTextureWidth / 32;
				int num27 = cubeTexture.TileLinearPositionsInAtlas[num24];
				float num28 = num27 % num26 * 32;
				float num29 = num27 / num26 * 32;
				float u = (num28 + 0.04f) / (float)atlasTextureWidth;
				float u2 = (num28 + 32f - 0.04f) / (float)atlasTextureWidth;
				float u3 = (num29 + ((k >= 2) ? (32f * (1f - num23)) : 0f) + 0.04f) / (float)atlasTextureHeight;
				float u4 = (num29 + 32f - 0.04f) / (float)atlasTextureHeight;
				ushort x = VertexCompression.NormalizedTexCoordToUshort(u);
				ushort x2 = VertexCompression.NormalizedTexCoordToUshort(u2);
				ushort y = VertexCompression.NormalizedTexCoordToUshort(u3);
				ushort y2 = VertexCompression.NormalizedTexCoordToUshort(u4);
				switch (cubeTexture.Rotation)
				{
				case 0:
					texCoordsByCorner[0].X = x2;
					texCoordsByCorner[1].X = x;
					texCoordsByCorner[2].X = x;
					texCoordsByCorner[3].X = x2;
					texCoordsByCorner[0].Y = y;
					texCoordsByCorner[1].Y = y;
					texCoordsByCorner[2].Y = y2;
					texCoordsByCorner[3].Y = y2;
					break;
				case 90:
					texCoordsByCorner[0].X = x;
					texCoordsByCorner[1].X = x;
					texCoordsByCorner[2].X = x2;
					texCoordsByCorner[3].X = x2;
					texCoordsByCorner[0].Y = y;
					texCoordsByCorner[1].Y = y2;
					texCoordsByCorner[2].Y = y2;
					texCoordsByCorner[3].Y = y;
					break;
				case 180:
					texCoordsByCorner[0].X = x;
					texCoordsByCorner[1].X = x2;
					texCoordsByCorner[2].X = x2;
					texCoordsByCorner[3].X = x;
					texCoordsByCorner[0].Y = y2;
					texCoordsByCorner[1].Y = y2;
					texCoordsByCorner[2].Y = y;
					texCoordsByCorner[3].Y = y;
					break;
				case 270:
					texCoordsByCorner[0].X = x2;
					texCoordsByCorner[1].X = x2;
					texCoordsByCorner[2].X = x;
					texCoordsByCorner[3].X = x;
					texCoordsByCorner[0].Y = y2;
					texCoordsByCorner[1].Y = y;
					texCoordsByCorner[2].Y = y;
					texCoordsByCorner[3].Y = y2;
					break;
				default:
					throw new Exception($"Unsupported Z rotation value for texture: {cubeTexture.Rotation}°");
				}
				bool flag3 = k >= 2 && blockType.CubeSideMaskTextureAtlasIndex != -1;
				if (flag3)
				{
					int num30 = blockType.CubeSideMaskTextureAtlasIndex * 32;
					float num31 = num30 % atlasTextureWidth;
					float num32 = num30 / atlasTextureWidth * 32;
					float u5 = (num31 + 0.04f) / (float)atlasTextureWidth;
					float u6 = (num31 + 32f - 0.04f) / (float)atlasTextureWidth;
					float u7 = (num32 + 0.04f) / (float)atlasTextureHeight;
					float u8 = (num32 + 32f - 0.04f) / (float)atlasTextureHeight;
					ushort x3 = VertexCompression.NormalizedTexCoordToUshort(u5);
					ushort x4 = VertexCompression.NormalizedTexCoordToUshort(u6);
					ushort y3 = VertexCompression.NormalizedTexCoordToUshort(u7);
					ushort y4 = VertexCompression.NormalizedTexCoordToUshort(u8);
					sideMaskTexCoordsByCorner[0].X = x4;
					sideMaskTexCoordsByCorner[0].Y = y3;
					sideMaskTexCoordsByCorner[1].X = x3;
					sideMaskTexCoordsByCorner[1].Y = y3;
					sideMaskTexCoordsByCorner[2].X = x3;
					sideMaskTexCoordsByCorner[2].Y = y4;
					sideMaskTexCoordsByCorner[3].X = x4;
					sideMaskTexCoordsByCorner[3].Y = y4;
				}
				else
				{
					sideMaskTexCoordsByCorner[0] = NoSideMaskUV;
					sideMaskTexCoordsByCorner[1] = NoSideMaskUV;
					sideMaskTexCoordsByCorner[2] = NoSideMaskUV;
					sideMaskTexCoordsByCorner[3] = NoSideMaskUV;
				}
				int num33 = ((!flag3) ? k : 0);
				float num34 = blockType.BiomeTintMultipliersBySide[num33];
				for (int l = 0; l < 4; l++)
				{
					uint num35 = 0u;
					uint num36 = 255u;
					uint num37 = 255u;
					uint num38 = 255u;
					int num39 = borderedChunkBlocks[num25];
					bool flag4 = num39 != int.MaxValue && clientBlockTypes[num39].IsOccluder;
					if (!flag4)
					{
						num20 = borderedLightAmounts[num25];
					}
					int num40 = num25 + adjacentBlockOffsets.Horizontal * AdjacentBlockSignsByCorner[l][0];
					int num41 = borderedChunkBlocks[num40];
					bool flag5 = num41 != int.MaxValue && clientBlockTypes[num41].IsOccluder;
					if (!flag5)
					{
						num17 = borderedLightAmounts[num40];
					}
					int num42 = num25 + adjacentBlockOffsets.Vertical * AdjacentBlockSignsByCorner[l][1];
					int num43 = borderedChunkBlocks[num42];
					bool flag6 = num43 != int.MaxValue && clientBlockTypes[num43].IsOccluder;
					if (!flag6)
					{
						num18 = borderedLightAmounts[num42];
					}
					int num44 = num40 + adjacentBlockOffsets.Vertical * AdjacentBlockSignsByCorner[l][1];
					int num45 = borderedChunkBlocks[num44];
					bool flag7 = num45 != int.MaxValue && clientBlockTypes[num45].IsOccluder;
					if (!flag7)
					{
						num19 = borderedLightAmounts[num44];
					}
					for (int m = 0; m < 4; m++)
					{
						float num46 = 0f;
						int num47 = 0;
						if (!flag4)
						{
							int num48 = (num20 >> m * 4) & 0xF;
							num46 += lightLevels[num48];
							num47++;
						}
						if (!flag5)
						{
							int num49 = (num17 >> m * 4) & 0xF;
							num46 += lightLevels[num49];
							num47++;
						}
						if (!flag6)
						{
							int num50 = (num18 >> m * 4) & 0xF;
							num46 += lightLevels[num50];
							num47++;
						}
						if (!flag7)
						{
							int num51 = (num19 >> m * 4) & 0xF;
							num46 += lightLevels[num51];
							num47++;
						}
						if (num47 != 0)
						{
							num46 /= (float)num47;
						}
						if ((flag5 && flag6) || (flag5 && flag4) || (flag6 && flag4))
						{
							num46 *= 0.7f;
							cornerOcclusions[l] = 2;
						}
						else if (flag5 || flag6 || flag7 || flag4)
						{
							num46 *= 0.85f;
							cornerOcclusions[l] = 1;
						}
						else
						{
							cornerOcclusions[l] = 0;
						}
						num35 |= (uint)(num46 * 255f) << m * 8;
					}
					uint num52 = 0u;
					Vector3 vector3 = CornersPerSide[k][l];
					ClientBlockType.ClientShaderEffect clientShaderEffect = blockType.CubeShaderEffect;
					if ((clientShaderEffect == ClientBlockType.ClientShaderEffect.Water || clientShaderEffect == ClientBlockType.ClientShaderEffect.Lava) && vector3.Y > 0f && (float)(int)blockType.VerticalFill > (float)(int)blockType.MaxFillLevel * 0.125f)
					{
						num52 = 32u;
						if (blockType.VerticalFill == blockType.MaxFillLevel)
						{
							int z = (int)System.Math.Floor(vector3.Z + 0.5f);
							int z2 = (int)System.Math.Floor(vector3.Z - 0.5f);
							int x5 = (int)System.Math.Floor(vector3.X + 0.5f);
							int x6 = (int)System.Math.Floor(vector3.X - 0.5f);
							int num53 = borderedBlockIndex + ChunkHelper.IndexOfBlockInBorderedChunk(x5, 1, z);
							int num54 = borderedChunkBlocks[num53];
							int num55 = borderedBlockIndex + ChunkHelper.IndexOfBlockInBorderedChunk(x6, 1, z);
							int num56 = borderedChunkBlocks[num55];
							int num57 = borderedBlockIndex + ChunkHelper.IndexOfBlockInBorderedChunk(x5, 1, z2);
							int num58 = borderedChunkBlocks[num57];
							int num59 = borderedBlockIndex + ChunkHelper.IndexOfBlockInBorderedChunk(x6, 1, z2);
							int num60 = borderedChunkBlocks[num59];
							if ((num54 != int.MaxValue && clientBlockTypes[num54].ShouldRenderCube) || (num56 != int.MaxValue && clientBlockTypes[num56].ShouldRenderCube) || (num58 != int.MaxValue && clientBlockTypes[num58].ShouldRenderCube) || (num60 != int.MaxValue && clientBlockTypes[num60].ShouldRenderCube))
							{
								num52 = 0u;
							}
						}
					}
					Vector3 position = new Vector3(cubeOffset.X + vector3.X, cubeOffset.Y + vector3.Y * num23, cubeOffset.Z + vector3.Z);
					Vector3.Transform(ref position, ref cubeMatrix, out position);
					shortVector = VertexCompression.Vector3PositionToShortVector3(position);
					Vector3 vector4 = Vector3.Normalize(Vector3.TransformNormal(adjacentBlockOffsets.Normal, cubeMatrix));
					uint normalAndNodeIndex2 = VertexCompression.NormalizedXYZToUint(vector4.X, vector4.Y, vector4.Z) | (uint)(blockType.FinalBlockyModel.NodeCount - 1 << 24);
					if (blockType.RenderedBlockyModel == null)
					{
						int num61 = blockType.SelfTintColorsBySide[num33];
						byte b9 = (byte)(num61 >> 16);
						byte b10 = (byte)(num61 >> 8);
						byte b11 = (byte)num61;
						int num62 = 0;
						int num63 = 0;
						switch (k)
						{
						case 0:
							switch (l)
							{
							case 0:
								num62 = 1;
								num63 = 0;
								break;
							case 1:
								num62 = 0;
								num63 = 0;
								break;
							case 2:
								num62 = 0;
								num63 = 1;
								break;
							case 3:
								num62 = 1;
								num63 = 1;
								break;
							}
							break;
						case 1:
							switch (l)
							{
							case 0:
								num62 = 0;
								num63 = 0;
								break;
							case 1:
								num62 = 1;
								num63 = 0;
								break;
							case 2:
								num62 = 1;
								num63 = 1;
								break;
							case 3:
								num62 = 0;
								num63 = 1;
								break;
							}
							break;
						case 4:
							switch (l)
							{
							case 0:
							case 3:
								num62 = 1;
								num63 = 1;
								break;
							case 1:
							case 2:
								num62 = 0;
								num63 = 1;
								break;
							}
							break;
						case 5:
							switch (l)
							{
							case 0:
							case 3:
								num62 = 0;
								num63 = 0;
								break;
							case 1:
							case 2:
								num62 = 1;
								num63 = 0;
								break;
							}
							break;
						case 3:
							switch (l)
							{
							case 0:
							case 3:
								num62 = 1;
								num63 = 0;
								break;
							case 1:
							case 2:
								num62 = 1;
								num63 = 1;
								break;
							}
							break;
						case 2:
							switch (l)
							{
							case 0:
							case 3:
								num62 = 0;
								num63 = 1;
								break;
							case 1:
							case 2:
								num62 = 0;
								num63 = 0;
								break;
							}
							break;
						}
						uint num64 = borderedColumnTints[ChunkHelper.IndexInBorderedChunkColumn(chunkX + num62, chunkZ + num63)];
						byte b12 = (byte)(num64 >> 16);
						byte b13 = (byte)(num64 >> 8);
						byte b14 = (byte)num64;
						if (!NoTint.Equals(ForceTint))
						{
							b12 = (byte)ForceTint.X;
							b13 = (byte)ForceTint.Y;
							b14 = (byte)ForceTint.Z;
						}
						num36 = (uint)((float)(int)b9 + (float)(b12 - b9) * num34);
						num37 = (uint)((float)(int)b10 + (float)(b13 - b10) * num34);
						num38 = (uint)((float)(int)b11 + (float)(b14 - b11) * num34);
					}
					if (clientShaderEffect == ClientBlockType.ClientShaderEffect.Water)
					{
						int num65 = l;
						switch (k)
						{
						case 1:
							switch (l)
							{
							case 0:
								num65 = 5;
								break;
							case 1:
								num65 = 4;
								break;
							case 3:
								num65 = 6;
								break;
							case 2:
								num65 = 7;
								break;
							}
							break;
						case 4:
							switch (l)
							{
							case 0:
								num65 = 3;
								break;
							case 1:
								num65 = 2;
								break;
							case 3:
								num65 = 7;
								break;
							case 2:
								num65 = 6;
								break;
							}
							break;
						case 5:
							switch (l)
							{
							case 0:
								num65 = 1;
								break;
							case 1:
								num65 = 0;
								break;
							case 3:
								num65 = 5;
								break;
							case 2:
								num65 = 4;
								break;
							}
							break;
						case 3:
							switch (l)
							{
							case 0:
								num65 = 0;
								break;
							case 1:
								num65 = 3;
								break;
							case 3:
								num65 = 4;
								break;
							case 2:
								num65 = 7;
								break;
							}
							break;
						case 2:
							switch (l)
							{
							case 0:
								num65 = 2;
								break;
							case 1:
								num65 = 1;
								break;
							case 3:
								num65 = 6;
								break;
							case 2:
								num65 = 5;
								break;
							}
							break;
						}
						uint num66 = environmentTints[num65];
						float num67 = (float)(int)(byte)(num66 >> 16) / 255f;
						float num68 = (float)(int)(byte)(num66 >> 8) / 255f;
						float num69 = (float)(int)(byte)num66 / 255f;
						if (!NoTint.Equals(ForceTint))
						{
							num67 = (float)(int)(byte)ForceTint.X / 255f;
							num68 = (float)(int)(byte)ForceTint.Y / 255f;
							num69 = (float)(int)(byte)ForceTint.Z / 255f;
						}
						clientShaderEffect = (cornerShaderEffects[l] = (ClientBlockType.ClientShaderEffect)(byte)(num66 >> 24));
						num36 = (uint)((float)num36 * num67);
						num37 = (uint)((float)num37 * num68);
						num38 = (uint)((float)num38 * num69);
					}
					uint num70 = num36 | (num37 << 8) | (num38 << 16) | (uint)((int)clientShaderEffect << 24);
					cubeVertexData.Vertices[cubeVertexData.VerticesOffset + l] = new ChunkVertex
					{
						PositionPacked = shortVector,
						DoubleSidedAndBlockId = (ushort)(((1 & num22) << 15) | VertexCompression.CompressBlockLocalPosition(chunkX, chunkY, chunkZ)),
						NormalAndNodeIndex = normalAndNodeIndex2,
						TextureCoordinates = texCoordsByCorner[l],
						MaskTextureCoordinates = sideMaskTexCoordsByCorner[l],
						TintColorAndEffectAndShadingMode = (num70 | (num52 << 24) | ((uint)blockType.CubeShadingMode << 30)),
						GlowColorAndSunlight = num35,
						UseBillboard = 0u
					};
				}
				bool flag8 = cornerOcclusions[0] + cornerOcclusions[2] > cornerOcclusions[1] + cornerOcclusions[3] || cornerShaderEffects[0] != cornerShaderEffects[2];
				cubeVertexData.Indices[cubeVertexData.IndicesOffset] = cubeVertexData.VerticesOffset;
				cubeVertexData.Indices[cubeVertexData.IndicesOffset + 1] = cubeVertexData.VerticesOffset + 1;
				cubeVertexData.Indices[cubeVertexData.IndicesOffset + 2] = cubeVertexData.VerticesOffset + (uint)(flag8 ? 3 : 2);
				cubeVertexData.Indices[cubeVertexData.IndicesOffset + 3] = cubeVertexData.VerticesOffset + (uint)(flag8 ? 1 : 0);
				cubeVertexData.Indices[cubeVertexData.IndicesOffset + 4] = cubeVertexData.VerticesOffset + 2;
				cubeVertexData.Indices[cubeVertexData.IndicesOffset + 5] = cubeVertexData.VerticesOffset + 3;
				if (k == 0)
				{
					num21 = cubeVertexData.VerticesOffset;
					flag = flag8;
				}
				cubeVertexData.VerticesOffset += 4u;
				cubeVertexData.IndicesOffset += 6;
			}
			int num71 = borderedChunkBlocks[num25];
			if (!(!isAnimated && k >= 2 && flag2) || num71 == int.MaxValue)
			{
				continue;
			}
			ClientBlockType clientBlockType = clientBlockTypes[num71];
			ClientBlockType adjacentTopClientBlockType = null;
			int num72 = num25 + AdjacentBlockOffsetsBySide[0].Main;
			if (num72 < borderedChunkBlocks.Length && borderedChunkBlocks[num72] != int.MaxValue)
			{
				adjacentTopClientBlockType = clientBlockTypes[borderedChunkBlocks[num72]];
			}
			if (!ShouldAddTransition(blockType, clientBlockType, adjacentTopClientBlockType))
			{
				continue;
			}
			int num73 = clientBlockType.TransitionTextureAtlasIndex * 32;
			float num74 = num73 % atlasTextureWidth;
			float num75 = num73 / atlasTextureWidth * 32;
			float u9 = (num74 + 0.04f) / (float)atlasTextureWidth;
			float u10 = (num74 + 32f - 0.04f) / (float)atlasTextureWidth;
			float u11 = (num75 + 0.04f) / (float)atlasTextureHeight;
			float u12 = (num75 + 32f - 0.04f) / (float)atlasTextureHeight;
			ushort x7 = VertexCompression.NormalizedTexCoordToUshort(u9);
			ushort x8 = VertexCompression.NormalizedTexCoordToUshort(u10);
			ushort y5 = VertexCompression.NormalizedTexCoordToUshort(u11);
			ushort y6 = VertexCompression.NormalizedTexCoordToUshort(u12);
			switch (k)
			{
			case 2:
				texCoordsByCorner[0].X = x8;
				texCoordsByCorner[0].Y = y6;
				texCoordsByCorner[1].X = x8;
				texCoordsByCorner[1].Y = y5;
				texCoordsByCorner[2].X = x7;
				texCoordsByCorner[2].Y = y5;
				texCoordsByCorner[3].X = x7;
				texCoordsByCorner[3].Y = y6;
				break;
			case 3:
				texCoordsByCorner[0].X = x7;
				texCoordsByCorner[0].Y = y5;
				texCoordsByCorner[1].X = x7;
				texCoordsByCorner[1].Y = y6;
				texCoordsByCorner[2].X = x8;
				texCoordsByCorner[2].Y = y6;
				texCoordsByCorner[3].X = x8;
				texCoordsByCorner[3].Y = y5;
				break;
			case 5:
				texCoordsByCorner[0].X = x8;
				texCoordsByCorner[0].Y = y5;
				texCoordsByCorner[1].X = x7;
				texCoordsByCorner[1].Y = y5;
				texCoordsByCorner[2].X = x7;
				texCoordsByCorner[2].Y = y6;
				texCoordsByCorner[3].X = x8;
				texCoordsByCorner[3].Y = y6;
				break;
			case 4:
				texCoordsByCorner[0].X = x7;
				texCoordsByCorner[0].Y = y6;
				texCoordsByCorner[1].X = x8;
				texCoordsByCorner[1].Y = y6;
				texCoordsByCorner[2].X = x8;
				texCoordsByCorner[2].Y = y5;
				texCoordsByCorner[3].X = x7;
				texCoordsByCorner[3].Y = y5;
				break;
			}
			int num76 = clientBlockType.SelfTintColorsBySide[0];
			byte b15 = (byte)(num76 >> 16);
			byte b16 = (byte)(num76 >> 8);
			byte b17 = (byte)num76;
			float num77 = clientBlockType.BiomeTintMultipliersBySide[0];
			int num78 = 0;
			int num79 = 0;
			for (int n = 0; n < 4; n++)
			{
				switch (k)
				{
				case 4:
					switch (n)
					{
					case 0:
					case 3:
						num78 = 1;
						num79 = 1;
						break;
					case 1:
					case 2:
						num78 = 0;
						num79 = 1;
						break;
					}
					break;
				case 5:
					switch (n)
					{
					case 0:
					case 3:
						num78 = 1;
						num79 = 0;
						break;
					case 1:
					case 2:
						num78 = 0;
						num79 = 0;
						break;
					}
					break;
				case 3:
					switch (n)
					{
					case 0:
					case 1:
						num78 = 1;
						num79 = 0;
						break;
					case 2:
					case 3:
						num78 = 1;
						num79 = 1;
						break;
					}
					break;
				case 2:
					switch (n)
					{
					case 0:
					case 1:
						num78 = 0;
						num79 = 0;
						break;
					case 2:
					case 3:
						num78 = 0;
						num79 = 1;
						break;
					}
					break;
				}
				uint num80 = borderedColumnTints[ChunkHelper.IndexInBorderedChunkColumn(chunkX + num78, chunkZ + num79)];
				byte b18 = (byte)(num80 >> 16);
				byte b19 = (byte)(num80 >> 8);
				byte b20 = (byte)num80;
				uint num81 = (uint)((float)(int)b15 + (float)(b18 - b15) * num77);
				uint num82 = (uint)((float)(int)b16 + (float)(b19 - b16) * num77);
				uint num83 = (uint)((float)(int)b17 + (float)(b20 - b17) * num77);
				uint num84 = num81 | (num82 << 8) | (num83 << 16);
				num84 |= (uint)((int)clientBlockType.CubeShaderEffect << 24) | ((uint)clientBlockType.CubeShadingMode << 30);
				ChunkVertex chunkVertex = cubeVertexData.Vertices[num21 + n];
				alphaTestedVertexData.Vertices[alphaTestedVertexData.VerticesOffset + n] = new ChunkVertex
				{
					PositionPacked = chunkVertex.PositionPacked,
					DoubleSidedAndBlockId = chunkVertex.DoubleSidedAndBlockId,
					NormalAndNodeIndex = chunkVertex.NormalAndNodeIndex,
					TextureCoordinates = texCoordsByCorner[n],
					MaskTextureCoordinates = NoSideMaskUV,
					TintColorAndEffectAndShadingMode = num84,
					GlowColorAndSunlight = chunkVertex.GlowColorAndSunlight,
					UseBillboard = chunkVertex.UseBillboard
				};
			}
			int num85 = alphaTestedAnimatedLowLODIndicesStart + alphaTestedLowLODIndicesOffset;
			alphaTestedVertexData.Indices[num85] = alphaTestedVertexData.VerticesOffset;
			alphaTestedVertexData.Indices[num85 + 1] = alphaTestedVertexData.VerticesOffset + 1;
			alphaTestedVertexData.Indices[num85 + 2] = alphaTestedVertexData.VerticesOffset + (uint)(flag ? 3 : 2);
			alphaTestedVertexData.Indices[num85 + 3] = alphaTestedVertexData.VerticesOffset + (uint)(flag ? 1 : 0);
			alphaTestedVertexData.Indices[num85 + 4] = alphaTestedVertexData.VerticesOffset + 2;
			alphaTestedVertexData.Indices[num85 + 5] = alphaTestedVertexData.VerticesOffset + 3;
			alphaTestedVertexData.VerticesOffset += 4u;
			alphaTestedLowLODIndicesOffset += 6;
		}
	}

	private static bool ShouldAddTransition(ClientBlockType clientBlockType, ClientBlockType adjacentClientBlockType, ClientBlockType adjacentTopClientBlockType)
	{
		if (clientBlockType.TransitionGroupId == -1 || adjacentClientBlockType.TransitionTextureAtlasIndex == -1 || adjacentClientBlockType.TransitionToGroupIds == null)
		{
			return false;
		}
		if (clientBlockType == adjacentClientBlockType || (adjacentTopClientBlockType != null && adjacentTopClientBlockType.ShouldRenderCube && !adjacentTopClientBlockType.RequiresAlphaBlending))
		{
			return false;
		}
		for (int i = 0; i < adjacentClientBlockType.TransitionToGroupIds.Length; i++)
		{
			int num = adjacentClientBlockType.TransitionToGroupIds[i];
			if (num == -1 || num != clientBlockType.TransitionGroupId)
			{
				continue;
			}
			if (adjacentClientBlockType.TransitionGroupId == clientBlockType.TransitionGroupId && clientBlockType.TransitionToGroupIds != null)
			{
				for (int j = 0; j < clientBlockType.TransitionToGroupIds.Length; j++)
				{
					if (clientBlockType.TransitionToGroupIds[j] == clientBlockType.TransitionGroupId)
					{
						return adjacentClientBlockType.Id > clientBlockType.Id;
					}
				}
			}
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool ShouldRegisterSound(ClientBlockType blockType)
	{
		int result;
		if (blockType.SoundEventIndex != 0)
		{
			Dictionary<InteractionType, int> interactions = blockType.Interactions;
			result = (((interactions != null && interactions.Count == 0) || !blockType.IsAnimated()) ? 1 : 0);
		}
		else
		{
			result = 0;
		}
		return (byte)result != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int CalculateBlockTextureIndex(float[] weights, int seed)
	{
		if (weights.Length <= 1)
		{
			return 0;
		}
		double val = (double)(seed & 0x7FFFFFFF) / 2147483647.0;
		double num = System.Math.Min(val, 0.9999);
		for (int i = 0; i < weights.Length; i++)
		{
			if ((num -= (double)weights[i]) <= 0.0001)
			{
				return i;
			}
		}
		return 0;
	}
}
