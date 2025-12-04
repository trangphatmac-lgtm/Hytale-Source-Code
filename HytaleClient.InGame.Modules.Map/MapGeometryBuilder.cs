#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using HytaleClient.Core;
using HytaleClient.Data.Map;
using HytaleClient.Data.Map.Chunk;
using HytaleClient.Graphics.Map;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.InGame.Modules.Map;

internal class MapGeometryBuilder : Disposable
{
	private const int BytesPerBlock = 4;

	private const int BytesPerBlockLine = 128;

	private const int SliceBlockCount = 1024;

	private const int BorderedSliceBlockCount = 1156;

	private readonly int[] _borderedChunkBlocks = new int[39304];

	private readonly Dictionary<int, float> _borderedChunkHitTimers = new Dictionary<int, float>();

	private readonly int[] _missingChunkLine = new int[32];

	private readonly int[] _emptyChunkLine = new int[32];

	private readonly ushort[] _borderedChunkLightAmounts = new ushort[39304];

	private const int BytesPerTint = 4;

	private const int BytesPerTintLine = 128;

	private readonly uint[] _blendedBlockCornerBiomeTints = new uint[1156];

	private readonly ushort[][] _borderedColumnEnvironmentIds = new ushort[1156][];

	private readonly GameInstance _gameInstance;

	private readonly ChunkGeometryBuilder _chunkGeometryBuilder;

	private CancellationTokenSource _threadCancellationTokenSource;

	private CancellationToken _threadCancellationToken;

	private Thread _thread;

	private readonly SpiralIterator _spiralIterator = new SpiralIterator();

	private readonly object _spiralIteratorLock = new object();

	private readonly AutoResetEvent _restartSpiralEvent = new AutoResetEvent(initialState: false);

	private static readonly int[] RebuildOffsets = new int[3] { 0, -1, 1 };

	private float[] _nearestChunkDistances = new float[27];

	private IntVector3[] _nearestChunks = new IntVector3[27];

	private int _startChunkX = int.MaxValue;

	private int _startChunkY = int.MaxValue;

	private int _startChunkZ = int.MaxValue;

	private int _spiralRadius;

	private Stopwatch _disposeRequestsStopwatch = new Stopwatch();

	private FastIntQueue _floodFillQueue = new FastIntQueue(1024);

	private const int BytesPerLightBlock = 2;

	private const int BytesPerLightBlockLine = 64;

	private const int XPlusOne = 1;

	private const int XMinusOne = -1;

	private const int ZPlusOne = 32;

	private const int ZMinusOne = -32;

	private const int YPlusOne = 1024;

	private const int YMinusOne = -1024;

	private readonly PaletteChunkData[] adjacentChunkBlocks = new PaletteChunkData[27];

	private readonly ushort[][] adjacentChunkLightAmounts = new ushort[27][];

	private readonly ConcurrentQueue<ushort[]> _selfLightAmountQueue = new ConcurrentQueue<ushort[]>();

	private int _selfLightAmountAllocatedTotal = 0;

	private readonly ConcurrentQueue<ushort[]> _borderedLightAmountQueue = new ConcurrentQueue<ushort[]>();

	private int _borderedLightAmountAllocatedTotal = 0;

	public bool IsSolidBorderedChunk(int[] borderedChunkData)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[borderedChunkData[ChunkHelper.IndexOfBlockInBorderedChunk(1, 1, 1)]];
		DrawType drawType = clientBlockType.DrawType;
		bool requiresAlphaBlending = clientBlockType.RequiresAlphaBlending;
		for (int i = 0; i < 39304; i++)
		{
			int num = borderedChunkData[i];
			if (num != int.MaxValue && (_gameInstance.MapModule.ClientBlockTypes[num].DrawType != drawType || _gameInstance.MapModule.ClientBlockTypes[num].RequiresAlphaBlending != requiresAlphaBlending))
			{
				return false;
			}
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ClearBlock(int index)
	{
		_borderedChunkBlocks[index] = int.MaxValue;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ClearLine(int targetOffset)
	{
		Buffer.BlockCopy(_missingChunkLine, 0, _borderedChunkBlocks, targetOffset * 4, 128);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ClearScatteredLine(int index, int offset)
	{
		for (int i = 0; i < 32; i++)
		{
			_borderedChunkBlocks[index] = int.MaxValue;
			index += offset;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ClearMultipleLines(int targetOffset, int offset)
	{
		for (int i = 0; i < 32; i++)
		{
			Buffer.BlockCopy(_missingChunkLine, 0, _borderedChunkBlocks, targetOffset * 4, 128);
			targetOffset += offset;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void EmptyBlock(int index)
	{
		_borderedChunkBlocks[index] = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void EmptyLine(int targetOffset)
	{
		Buffer.BlockCopy(_emptyChunkLine, 0, _borderedChunkBlocks, targetOffset * 4, 128);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void EmptyScatteredLine(int index, int offset)
	{
		for (int i = 0; i < 32; i++)
		{
			_borderedChunkBlocks[index] = 0;
			index += offset;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void EmptyMultipleLines(int targetOffset, int offset)
	{
		for (int i = 0; i < 32; i++)
		{
			Buffer.BlockCopy(_emptyChunkLine, 0, _borderedChunkBlocks, targetOffset * 4, 128);
			targetOffset += offset;
		}
	}

	private void SetupBorderedChunkData(int chunkX, int chunkY, int chunkZ, ChunkData chunkData, uint[] columnTints, ushort[][] environmentIds)
	{
		_borderedChunkHitTimers.Clear();
		int num = ChunkHelper.IndexOfBlockInBorderedChunk(1, 1, 1);
		for (int i = 0; i < 32; i++)
		{
			for (int j = 0; j < 32; j++)
			{
				for (int k = 0; k < 32; k++)
				{
					_borderedChunkBlocks[num + k] = chunkData.Blocks.Get(k, i, j);
				}
				num += 34;
			}
			num += 68;
		}
		ChunkData.BlockHitTimer[] blockHitTimers = chunkData.BlockHitTimers;
		for (int l = 0; l < blockHitTimers.Length; l++)
		{
			ChunkData.BlockHitTimer blockHitTimer = blockHitTimers[l];
			if (blockHitTimer.BlockIndex != -1)
			{
				int num2 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer.BlockIndex, 0, 0, 0);
				if (num2 != -1)
				{
					_borderedChunkHitTimers.Add(num2, blockHitTimer.Timer);
				}
			}
		}
		int num3 = 0;
		int num4 = ChunkHelper.IndexInBorderedChunkColumn(1, 1);
		for (int m = 0; m < 32; m++)
		{
			for (int n = 0; n < 32; n++)
			{
				int num5 = environmentIds[num3 + n].Length;
				_borderedColumnEnvironmentIds[num4 + n] = new ushort[num5];
				int count = 2 * num5;
				Buffer.BlockCopy(environmentIds[num3 + n], 0, _borderedColumnEnvironmentIds[num4 + n], 0, count);
			}
			Buffer.BlockCopy(columnTints, num3 * 4, _blendedBlockCornerBiomeTints, num4 * 4, 128);
			num3 += 32;
			num4 += 34;
		}
		ChunkColumn chunkColumn = _gameInstance.MapModule.GetChunkColumn(chunkX - 1, chunkZ - 1);
		if (chunkColumn != null)
		{
			int num6 = ChunkHelper.IndexInChunkColumn(31, 31);
			int num7 = ChunkHelper.IndexInBorderedChunkColumn(0, 0);
			_borderedColumnEnvironmentIds[num7] = chunkColumn.Environments[num6];
			_blendedBlockCornerBiomeTints[num7] = chunkColumn.Tints[num6];
		}
		ChunkColumn chunkColumn2 = _gameInstance.MapModule.GetChunkColumn(chunkX + 1, chunkZ - 1);
		if (chunkColumn2 != null)
		{
			int num8 = ChunkHelper.IndexInChunkColumn(0, 31);
			int num9 = ChunkHelper.IndexInBorderedChunkColumn(33, 0);
			_borderedColumnEnvironmentIds[num9] = chunkColumn2.Environments[num8];
			_blendedBlockCornerBiomeTints[num9] = chunkColumn2.Tints[num8];
		}
		ChunkColumn chunkColumn3 = _gameInstance.MapModule.GetChunkColumn(chunkX - 1, chunkZ + 1);
		if (chunkColumn3 != null)
		{
			int num10 = ChunkHelper.IndexInChunkColumn(31, 0);
			int num11 = ChunkHelper.IndexInBorderedChunkColumn(0, 33);
			_borderedColumnEnvironmentIds[num11] = chunkColumn3.Environments[num10];
			_blendedBlockCornerBiomeTints[num11] = chunkColumn3.Tints[num10];
		}
		ChunkColumn chunkColumn4 = _gameInstance.MapModule.GetChunkColumn(chunkX + 1, chunkZ + 1);
		if (chunkColumn4 != null)
		{
			int num12 = ChunkHelper.IndexInChunkColumn(0, 0);
			int num13 = ChunkHelper.IndexInBorderedChunkColumn(33, 33);
			_borderedColumnEnvironmentIds[num13] = chunkColumn4.Environments[num12];
			_blendedBlockCornerBiomeTints[num13] = chunkColumn4.Tints[num12];
		}
		ChunkColumn chunkColumn5 = _gameInstance.MapModule.GetChunkColumn(chunkX, chunkZ - 1);
		if (chunkColumn5 != null)
		{
			int num14 = ChunkHelper.IndexInChunkColumn(0, 31);
			int num15 = ChunkHelper.IndexInBorderedChunkColumn(1, 0);
			for (int num16 = 0; num16 < 32; num16++)
			{
				int num17 = chunkColumn5.Environments[num14 + num16].Length;
				_borderedColumnEnvironmentIds[num15 + num16] = new ushort[num17];
				int count2 = 2 * num17;
				Buffer.BlockCopy(chunkColumn5.Environments[num14 + num16], 0, _borderedColumnEnvironmentIds[num15 + num16], 0, count2);
			}
			Buffer.BlockCopy(chunkColumn5.Tints, num14 * 4, _blendedBlockCornerBiomeTints, num15 * 4, 128);
		}
		ChunkColumn chunkColumn6 = _gameInstance.MapModule.GetChunkColumn(chunkX, chunkZ + 1);
		if (chunkColumn6 != null)
		{
			int num18 = ChunkHelper.IndexInChunkColumn(0, 0);
			int num19 = ChunkHelper.IndexInBorderedChunkColumn(1, 33);
			for (int num20 = 0; num20 < 32; num20++)
			{
				int num21 = chunkColumn6.Environments[num18 + num20].Length;
				_borderedColumnEnvironmentIds[num19 + num20] = new ushort[num21];
				int count3 = 2 * num21;
				Buffer.BlockCopy(chunkColumn6.Environments[num18 + num20], 0, _borderedColumnEnvironmentIds[num19 + num20], 0, count3);
			}
			Buffer.BlockCopy(chunkColumn6.Tints, num18 * 4, _blendedBlockCornerBiomeTints, num19 * 4, 128);
		}
		ChunkColumn chunkColumn7 = _gameInstance.MapModule.GetChunkColumn(chunkX - 1, chunkZ);
		if (chunkColumn7 != null)
		{
			int num22 = ChunkHelper.IndexInChunkColumn(31, 0);
			int num23 = ChunkHelper.IndexInBorderedChunkColumn(0, 1);
			for (int num24 = 0; num24 < 32; num24++)
			{
				_blendedBlockCornerBiomeTints[num23] = chunkColumn7.Tints[num22];
				_borderedColumnEnvironmentIds[num23] = chunkColumn7.Environments[num22];
				num22 += 32;
				num23 += 34;
			}
		}
		ChunkColumn chunkColumn8 = _gameInstance.MapModule.GetChunkColumn(chunkX + 1, chunkZ);
		if (chunkColumn8 != null)
		{
			int num25 = ChunkHelper.IndexInChunkColumn(0, 0);
			int num26 = ChunkHelper.IndexInBorderedChunkColumn(33, 1);
			for (int num27 = 0; num27 < 32; num27++)
			{
				_blendedBlockCornerBiomeTints[num26] = chunkColumn8.Tints[num25];
				_borderedColumnEnvironmentIds[num26] = chunkColumn8.Environments[num25];
				num25 += 32;
				num26 += 34;
			}
		}
		for (int num28 = 0; num28 < 33; num28++)
		{
			for (int num29 = 0; num29 < 33; num29++)
			{
				int num30 = ChunkHelper.IndexInBorderedChunkColumn(num28, num29);
				int num31 = ChunkHelper.IndexInBorderedChunkColumn(num28 + 1, num29);
				int num32 = ChunkHelper.IndexInBorderedChunkColumn(num28, num29 + 1);
				int num33 = ChunkHelper.IndexInBorderedChunkColumn(num28 + 1, num29 + 1);
				uint num34 = _blendedBlockCornerBiomeTints[num30];
				uint num35 = (byte)(num34 >> 16);
				uint num36 = (byte)(num34 >> 8);
				uint num37 = (byte)num34;
				num34 = _blendedBlockCornerBiomeTints[num31];
				num35 += (byte)(num34 >> 16);
				num36 += (byte)(num34 >> 8);
				num37 += (byte)num34;
				num34 = _blendedBlockCornerBiomeTints[num32];
				num35 += (byte)(num34 >> 16);
				num36 += (byte)(num34 >> 8);
				num37 += (byte)num34;
				num34 = _blendedBlockCornerBiomeTints[num33];
				num35 += (byte)(num34 >> 16);
				num36 += (byte)(num34 >> 8);
				num37 += (byte)num34;
				num35 = (byte)((float)num35 * 0.25f);
				num36 = (byte)((float)num36 * 0.25f);
				num37 = (byte)((float)num37 * 0.25f);
				_blendedBlockCornerBiomeTints[num30] = (num35 << 16) | (num36 << 8) | num37;
			}
		}
		Chunk chunk = _gameInstance.MapModule.GetChunk(chunkX - 1, chunkY - 1, chunkZ - 1);
		int slotIndex;
		if (chunk != null)
		{
			lock (chunk.DisposeLock)
			{
				if (!chunk.Disposed)
				{
					int num38 = ChunkHelper.IndexOfBlockInChunk(31, 31, 31);
					_borderedChunkBlocks[0] = chunk.Data.Blocks.Get(num38);
					if (chunk.Data.TryGetBlockHitTimer(num38, out slotIndex, out var hitTimer))
					{
						_borderedChunkHitTimers.Add(0, hitTimer);
					}
				}
				else
				{
					ClearBlock(0);
				}
			}
		}
		else
		{
			ClearBlock(0);
		}
		int num39 = ChunkHelper.IndexOfBlockInBorderedChunk(1, 0, 0);
		Chunk chunk2 = _gameInstance.MapModule.GetChunk(chunkX, chunkY - 1, chunkZ - 1);
		if (chunk2 != null)
		{
			lock (chunk2.DisposeLock)
			{
				if (!chunk2.Disposed)
				{
					int num40 = ChunkHelper.IndexOfBlockInChunk(0, 31, 31);
					for (int num41 = 0; num41 < 32; num41++)
					{
						_borderedChunkBlocks[num39 + num41] = chunk2.Data.Blocks.Get(num40 + num41);
					}
					ChunkData.BlockHitTimer[] blockHitTimers2 = chunk2.Data.BlockHitTimers;
					for (int num42 = 0; num42 < blockHitTimers2.Length; num42++)
					{
						ChunkData.BlockHitTimer blockHitTimer2 = blockHitTimers2[num42];
						if (blockHitTimer2.BlockIndex != -1)
						{
							int num43 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer2.BlockIndex, 0, -1, -1);
							if (num43 != -1)
							{
								_borderedChunkHitTimers.Add(num43, blockHitTimer2.Timer);
							}
						}
					}
				}
				else
				{
					ClearLine(num39);
				}
			}
		}
		else
		{
			ClearLine(num39);
		}
		Chunk chunk3 = _gameInstance.MapModule.GetChunk(chunkX + 1, chunkY - 1, chunkZ - 1);
		int num44 = ChunkHelper.IndexOfBlockInBorderedChunk(33, 0, 0);
		if (chunk3 != null)
		{
			lock (chunk3.DisposeLock)
			{
				if (!chunk3.Disposed)
				{
					int num45 = ChunkHelper.IndexOfBlockInChunk(0, 31, 31);
					_borderedChunkBlocks[num44] = chunk3.Data.Blocks.Get(num45);
					if (chunk3.Data.TryGetBlockHitTimer(num45, out slotIndex, out var hitTimer2))
					{
						_borderedChunkHitTimers.Add(num44, hitTimer2);
					}
				}
				else
				{
					ClearBlock(num44);
				}
			}
		}
		else
		{
			ClearBlock(num44);
		}
		Chunk chunk4 = _gameInstance.MapModule.GetChunk(chunkX - 1, chunkY - 1, chunkZ);
		int num46 = ChunkHelper.IndexOfBlockInBorderedChunk(0, 0, 1);
		if (chunk4 != null)
		{
			lock (chunk4.DisposeLock)
			{
				if (!chunk4.Disposed)
				{
					int num47 = ChunkHelper.IndexOfBlockInChunk(31, 31, 0);
					for (int num48 = 0; num48 < 32; num48++)
					{
						_borderedChunkBlocks[num46] = chunk4.Data.Blocks.Get(num47);
						num47 += 32;
						num46 += 34;
					}
					ChunkData.BlockHitTimer[] blockHitTimers3 = chunk4.Data.BlockHitTimers;
					for (int num49 = 0; num49 < blockHitTimers3.Length; num49++)
					{
						ChunkData.BlockHitTimer blockHitTimer3 = blockHitTimers3[num49];
						if (blockHitTimer3.BlockIndex != -1)
						{
							int num50 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer3.BlockIndex, -1, -1, 0);
							if (num50 != -1)
							{
								_borderedChunkHitTimers.Add(num50, blockHitTimer3.Timer);
							}
						}
					}
				}
				else
				{
					ClearScatteredLine(num46, 34);
				}
			}
		}
		else
		{
			ClearScatteredLine(num46, 34);
		}
		Chunk chunk5 = _gameInstance.MapModule.GetChunk(chunkX + 1, chunkY - 1, chunkZ);
		int num51 = ChunkHelper.IndexOfBlockInBorderedChunk(33, 0, 1);
		if (chunk5 != null)
		{
			lock (chunk5.DisposeLock)
			{
				if (!chunk5.Disposed)
				{
					int num52 = ChunkHelper.IndexOfBlockInChunk(0, 31, 0);
					for (int num53 = 0; num53 < 32; num53++)
					{
						_borderedChunkBlocks[num51] = chunk5.Data.Blocks.Get(num52);
						num52 += 32;
						num51 += 34;
					}
					ChunkData.BlockHitTimer[] blockHitTimers4 = chunk5.Data.BlockHitTimers;
					for (int num54 = 0; num54 < blockHitTimers4.Length; num54++)
					{
						ChunkData.BlockHitTimer blockHitTimer4 = blockHitTimers4[num54];
						if (blockHitTimer4.BlockIndex != -1)
						{
							int num55 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer4.BlockIndex, 1, -1, 0);
							if (num55 != -1)
							{
								_borderedChunkHitTimers.Add(num55, blockHitTimer4.Timer);
							}
						}
					}
				}
				else
				{
					ClearScatteredLine(num51, 34);
				}
			}
		}
		else
		{
			ClearScatteredLine(num51, 34);
		}
		Chunk chunk6 = _gameInstance.MapModule.GetChunk(chunkX, chunkY - 1, chunkZ);
		int num56 = ChunkHelper.IndexOfBlockInBorderedChunk(1, 0, 1);
		if (chunk6 != null)
		{
			lock (chunk6.DisposeLock)
			{
				if (!chunk6.Disposed)
				{
					int num57 = ChunkHelper.IndexOfBlockInChunk(0, 31, 0);
					for (int num58 = 0; num58 < 32; num58++)
					{
						for (int num59 = 0; num59 < 32; num59++)
						{
							_borderedChunkBlocks[num56 + num59] = chunk6.Data.Blocks.Get(num57 + num59);
						}
						num57 += 32;
						num56 += 34;
					}
					ChunkData.BlockHitTimer[] blockHitTimers5 = chunk6.Data.BlockHitTimers;
					for (int num60 = 0; num60 < blockHitTimers5.Length; num60++)
					{
						ChunkData.BlockHitTimer blockHitTimer5 = blockHitTimers5[num60];
						if (blockHitTimer5.BlockIndex != -1)
						{
							int num61 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer5.BlockIndex, 0, -1, 0);
							if (num61 != -1)
							{
								_borderedChunkHitTimers.Add(num61, blockHitTimer5.Timer);
							}
						}
					}
				}
				else
				{
					ClearMultipleLines(num56, 34);
				}
			}
		}
		else
		{
			ClearMultipleLines(num56, 34);
		}
		Chunk chunk7 = _gameInstance.MapModule.GetChunk(chunkX - 1, chunkY - 1, chunkZ + 1);
		int num62 = ChunkHelper.IndexOfBlockInBorderedChunk(0, 0, 33);
		if (chunk7 != null)
		{
			lock (chunk7.DisposeLock)
			{
				if (!chunk7.Disposed)
				{
					int blockIdx = ChunkHelper.IndexOfBlockInChunk(31, 31, 0);
					_borderedChunkBlocks[num62] = chunk7.Data.Blocks.Get(blockIdx);
					ChunkData.BlockHitTimer[] blockHitTimers6 = chunk7.Data.BlockHitTimers;
					for (int num63 = 0; num63 < blockHitTimers6.Length; num63++)
					{
						ChunkData.BlockHitTimer blockHitTimer6 = blockHitTimers6[num63];
						if (blockHitTimer6.BlockIndex != -1)
						{
							int num64 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer6.BlockIndex, -1, -1, 1);
							if (num64 != -1)
							{
								_borderedChunkHitTimers.Add(num64, blockHitTimer6.Timer);
							}
						}
					}
				}
				else
				{
					ClearBlock(num62);
				}
			}
		}
		else
		{
			ClearBlock(num62);
		}
		int num65 = ChunkHelper.IndexOfBlockInBorderedChunk(1, 0, 33);
		Chunk chunk8 = _gameInstance.MapModule.GetChunk(chunkX, chunkY - 1, chunkZ + 1);
		if (chunk8 != null)
		{
			lock (chunk8.DisposeLock)
			{
				if (!chunk8.Disposed)
				{
					int num66 = ChunkHelper.IndexOfBlockInChunk(0, 31, 0);
					for (int num67 = 0; num67 < 32; num67++)
					{
						_borderedChunkBlocks[num65 + num67] = chunk8.Data.Blocks.Get(num66 + num67);
					}
					ChunkData.BlockHitTimer[] blockHitTimers7 = chunk8.Data.BlockHitTimers;
					for (int num68 = 0; num68 < blockHitTimers7.Length; num68++)
					{
						ChunkData.BlockHitTimer blockHitTimer7 = blockHitTimers7[num68];
						if (blockHitTimer7.BlockIndex != -1)
						{
							int num69 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer7.BlockIndex, 0, -1, 1);
							if (num69 != -1)
							{
								_borderedChunkHitTimers.Add(num69, blockHitTimer7.Timer);
							}
						}
					}
				}
				else
				{
					ClearLine(num65);
				}
			}
		}
		else
		{
			ClearLine(num65);
		}
		Chunk chunk9 = _gameInstance.MapModule.GetChunk(chunkX + 1, chunkY - 1, chunkZ + 1);
		int num70 = ChunkHelper.IndexOfBlockInBorderedChunk(33, 0, 33);
		if (chunk9 != null)
		{
			lock (chunk9.DisposeLock)
			{
				if (!chunk9.Disposed)
				{
					int num71 = ChunkHelper.IndexOfBlockInChunk(0, 31, 0);
					_borderedChunkBlocks[num70] = chunk9.Data.Blocks.Get(num71);
					if (chunk9.Data.TryGetBlockHitTimer(num71, out slotIndex, out var hitTimer3))
					{
						_borderedChunkHitTimers.Add(num70, hitTimer3);
					}
				}
				else
				{
					ClearBlock(num70);
				}
			}
		}
		else
		{
			ClearBlock(num70);
		}
		Chunk chunk10 = _gameInstance.MapModule.GetChunk(chunkX - 1, chunkY, chunkZ - 1);
		int num72 = ChunkHelper.IndexOfBlockInBorderedChunk(0, 1, 0);
		if (chunk10 != null)
		{
			lock (chunk10.DisposeLock)
			{
				if (!chunk10.Disposed)
				{
					int num73 = ChunkHelper.IndexOfBlockInChunk(31, 0, 31);
					for (int num74 = 0; num74 < 32; num74++)
					{
						_borderedChunkBlocks[num72] = chunk10.Data.Blocks.Get(num73);
						num73 += 1024;
						num72 += 1156;
					}
					ChunkData.BlockHitTimer[] blockHitTimers8 = chunk10.Data.BlockHitTimers;
					for (int num75 = 0; num75 < blockHitTimers8.Length; num75++)
					{
						ChunkData.BlockHitTimer blockHitTimer8 = blockHitTimers8[num75];
						if (blockHitTimer8.BlockIndex != -1)
						{
							int num76 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer8.BlockIndex, -1, 0, -1);
							if (num76 != -1)
							{
								_borderedChunkHitTimers.Add(num76, blockHitTimer8.Timer);
							}
						}
					}
				}
				else
				{
					ClearScatteredLine(num72, 1156);
				}
			}
		}
		else
		{
			ClearScatteredLine(num72, 1156);
		}
		Chunk chunk11 = _gameInstance.MapModule.GetChunk(chunkX, chunkY, chunkZ - 1);
		int num77 = ChunkHelper.IndexOfBlockInBorderedChunk(1, 1, 0);
		if (chunk11 != null)
		{
			lock (chunk11.DisposeLock)
			{
				if (!chunk11.Disposed)
				{
					int num78 = ChunkHelper.IndexOfBlockInChunk(0, 0, 31);
					for (int num79 = 0; num79 < 32; num79++)
					{
						for (int num80 = 0; num80 < 32; num80++)
						{
							_borderedChunkBlocks[num77 + num80] = chunk11.Data.Blocks.Get(num78 + num80);
						}
						num78 += 1024;
						num77 += 1156;
					}
					ChunkData.BlockHitTimer[] blockHitTimers9 = chunk11.Data.BlockHitTimers;
					for (int num81 = 0; num81 < blockHitTimers9.Length; num81++)
					{
						ChunkData.BlockHitTimer blockHitTimer9 = blockHitTimers9[num81];
						if (blockHitTimer9.BlockIndex != -1)
						{
							int num82 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer9.BlockIndex, 0, 0, -1);
							if (num82 != -1)
							{
								_borderedChunkHitTimers.Add(num82, blockHitTimer9.Timer);
							}
						}
					}
				}
				else
				{
					ClearMultipleLines(num77, 1156);
				}
			}
		}
		else
		{
			ClearMultipleLines(num77, 1156);
		}
		Chunk chunk12 = _gameInstance.MapModule.GetChunk(chunkX + 1, chunkY, chunkZ - 1);
		int num83 = ChunkHelper.IndexOfBlockInBorderedChunk(33, 1, 0);
		if (chunk12 != null)
		{
			lock (chunk12.DisposeLock)
			{
				if (!chunk12.Disposed)
				{
					int num84 = ChunkHelper.IndexOfBlockInChunk(0, 0, 31);
					for (int num85 = 0; num85 < 32; num85++)
					{
						_borderedChunkBlocks[num83] = chunk12.Data.Blocks.Get(num84);
						num84 += 1024;
						num83 += 1156;
					}
					ChunkData.BlockHitTimer[] blockHitTimers10 = chunk12.Data.BlockHitTimers;
					for (int num86 = 0; num86 < blockHitTimers10.Length; num86++)
					{
						ChunkData.BlockHitTimer blockHitTimer10 = blockHitTimers10[num86];
						if (blockHitTimer10.BlockIndex != -1)
						{
							int num87 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer10.BlockIndex, 1, 0, -1);
							if (num87 != -1)
							{
								_borderedChunkHitTimers.Add(num87, blockHitTimer10.Timer);
							}
						}
					}
				}
				else
				{
					ClearScatteredLine(num83, 1156);
				}
			}
		}
		else
		{
			ClearScatteredLine(num83, 1156);
		}
		Chunk chunk13 = _gameInstance.MapModule.GetChunk(chunkX - 1, chunkY, chunkZ);
		int num88 = ChunkHelper.IndexOfBlockInBorderedChunk(0, 1, 1);
		if (chunk13 != null)
		{
			lock (chunk13.DisposeLock)
			{
				if (!chunk13.Disposed)
				{
					int num89 = ChunkHelper.IndexOfBlockInChunk(31, 0, 0);
					for (int num90 = 0; num90 < 32; num90++)
					{
						for (int num91 = 0; num91 < 32; num91++)
						{
							_borderedChunkBlocks[num88] = chunk13.Data.Blocks.Get(num89);
							num89 += 32;
							num88 += 34;
						}
						num88 += 68;
					}
					ChunkData.BlockHitTimer[] blockHitTimers11 = chunk13.Data.BlockHitTimers;
					for (int num92 = 0; num92 < blockHitTimers11.Length; num92++)
					{
						ChunkData.BlockHitTimer blockHitTimer11 = blockHitTimers11[num92];
						if (blockHitTimer11.BlockIndex != -1)
						{
							int num93 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer11.BlockIndex, -1, 0, 0);
							if (num93 != -1)
							{
								_borderedChunkHitTimers.Add(num93, blockHitTimer11.Timer);
							}
						}
					}
				}
				else
				{
					for (int num94 = 0; num94 < 32; num94++)
					{
						ClearScatteredLine(num88, 34);
						num88 += 68;
					}
				}
			}
		}
		else
		{
			for (int num95 = 0; num95 < 32; num95++)
			{
				ClearScatteredLine(num88, 34);
				num88 += 68;
			}
		}
		Chunk chunk14 = _gameInstance.MapModule.GetChunk(chunkX + 1, chunkY, chunkZ);
		int num96 = ChunkHelper.IndexOfBlockInBorderedChunk(33, 1, 1);
		if (chunk14 != null)
		{
			lock (chunk14.DisposeLock)
			{
				if (!chunk14.Disposed)
				{
					int num97 = ChunkHelper.IndexOfBlockInChunk(0, 0, 0);
					for (int num98 = 0; num98 < 32; num98++)
					{
						for (int num99 = 0; num99 < 32; num99++)
						{
							_borderedChunkBlocks[num96] = chunk14.Data.Blocks.Get(num97);
							num97 += 32;
							num96 += 34;
						}
						num96 += 68;
					}
					ChunkData.BlockHitTimer[] blockHitTimers12 = chunk14.Data.BlockHitTimers;
					for (int num100 = 0; num100 < blockHitTimers12.Length; num100++)
					{
						ChunkData.BlockHitTimer blockHitTimer12 = blockHitTimers12[num100];
						if (blockHitTimer12.BlockIndex != -1)
						{
							int num101 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer12.BlockIndex, 1, 0, 0);
							if (num101 != -1)
							{
								_borderedChunkHitTimers.Add(num101, blockHitTimer12.Timer);
							}
						}
					}
				}
				else
				{
					for (int num102 = 0; num102 < 32; num102++)
					{
						ClearScatteredLine(num96, 34);
						num96 += 68;
					}
				}
			}
		}
		else
		{
			for (int num103 = 0; num103 < 32; num103++)
			{
				ClearScatteredLine(num96, 34);
				num96 += 68;
			}
		}
		Chunk chunk15 = _gameInstance.MapModule.GetChunk(chunkX - 1, chunkY, chunkZ + 1);
		int num104 = ChunkHelper.IndexOfBlockInBorderedChunk(0, 1, 33);
		if (chunk15 != null)
		{
			lock (chunk15.DisposeLock)
			{
				if (!chunk15.Disposed)
				{
					int num105 = ChunkHelper.IndexOfBlockInChunk(31, 0, 0);
					for (int num106 = 0; num106 < 32; num106++)
					{
						_borderedChunkBlocks[num104] = chunk15.Data.Blocks.Get(num105);
						num105 += 1024;
						num104 += 1156;
					}
					ChunkData.BlockHitTimer[] blockHitTimers13 = chunk15.Data.BlockHitTimers;
					for (int num107 = 0; num107 < blockHitTimers13.Length; num107++)
					{
						ChunkData.BlockHitTimer blockHitTimer13 = blockHitTimers13[num107];
						if (blockHitTimer13.BlockIndex != -1)
						{
							int num108 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer13.BlockIndex, -1, 0, 1);
							if (num108 != -1)
							{
								_borderedChunkHitTimers.Add(num108, blockHitTimer13.Timer);
							}
						}
					}
				}
				else
				{
					ClearScatteredLine(num104, 1156);
				}
			}
		}
		else
		{
			ClearScatteredLine(num104, 1156);
		}
		Chunk chunk16 = _gameInstance.MapModule.GetChunk(chunkX, chunkY, chunkZ + 1);
		int num109 = ChunkHelper.IndexOfBlockInBorderedChunk(1, 1, 33);
		if (chunk16 != null)
		{
			lock (chunk16.DisposeLock)
			{
				if (!chunk16.Disposed)
				{
					int num110 = ChunkHelper.IndexOfBlockInChunk(0, 0, 0);
					for (int num111 = 0; num111 < 32; num111++)
					{
						for (int num112 = 0; num112 < 32; num112++)
						{
							_borderedChunkBlocks[num109 + num112] = chunk16.Data.Blocks.Get(num110 + num112);
						}
						num110 += 1024;
						num109 += 1156;
					}
					ChunkData.BlockHitTimer[] blockHitTimers14 = chunk16.Data.BlockHitTimers;
					for (int num113 = 0; num113 < blockHitTimers14.Length; num113++)
					{
						ChunkData.BlockHitTimer blockHitTimer14 = blockHitTimers14[num113];
						if (blockHitTimer14.BlockIndex != -1)
						{
							int num114 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer14.BlockIndex, 0, 0, 1);
							if (num114 != -1)
							{
								_borderedChunkHitTimers.Add(num114, blockHitTimer14.Timer);
							}
						}
					}
				}
				else
				{
					ClearMultipleLines(num109, 1156);
				}
			}
		}
		else
		{
			ClearMultipleLines(num109, 1156);
		}
		Chunk chunk17 = _gameInstance.MapModule.GetChunk(chunkX + 1, chunkY, chunkZ + 1);
		int num115 = ChunkHelper.IndexOfBlockInBorderedChunk(33, 1, 33);
		if (chunk17 != null)
		{
			lock (chunk17.DisposeLock)
			{
				if (!chunk17.Disposed)
				{
					int num116 = ChunkHelper.IndexOfBlockInChunk(0, 0, 0);
					for (int num117 = 0; num117 < 32; num117++)
					{
						_borderedChunkBlocks[num115] = chunk17.Data.Blocks.Get(num116);
						num116 += 1024;
						num115 += 1156;
					}
					ChunkData.BlockHitTimer[] blockHitTimers15 = chunk17.Data.BlockHitTimers;
					for (int num118 = 0; num118 < blockHitTimers15.Length; num118++)
					{
						ChunkData.BlockHitTimer blockHitTimer15 = blockHitTimers15[num118];
						if (blockHitTimer15.BlockIndex != -1)
						{
							int num119 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer15.BlockIndex, 1, 0, 1);
							if (num119 != -1)
							{
								_borderedChunkHitTimers.Add(num119, blockHitTimer15.Timer);
							}
						}
					}
				}
				else
				{
					ClearScatteredLine(num115, 1156);
				}
			}
		}
		else
		{
			ClearScatteredLine(num115, 1156);
		}
		Chunk chunk18 = _gameInstance.MapModule.GetChunk(chunkX - 1, chunkY + 1, chunkZ - 1);
		int num120 = ChunkHelper.IndexOfBlockInBorderedChunk(0, 33, 0);
		if (chunk18 != null)
		{
			lock (chunk18.DisposeLock)
			{
				if (!chunk18.Disposed)
				{
					int num121 = ChunkHelper.IndexOfBlockInChunk(31, 0, 31);
					_borderedChunkBlocks[num120] = chunk18.Data.Blocks.Get(num121);
					if (chunk18.Data.TryGetBlockHitTimer(num121, out slotIndex, out var hitTimer4))
					{
						_borderedChunkHitTimers.Add(num120, hitTimer4);
					}
				}
				else
				{
					ClearBlock(num120);
				}
			}
		}
		else if (chunkY + 1 >= ChunkHelper.ChunksPerColumn)
		{
			EmptyBlock(num120);
		}
		else
		{
			ClearBlock(num120);
		}
		Chunk chunk19 = _gameInstance.MapModule.GetChunk(chunkX, chunkY + 1, chunkZ - 1);
		int num122 = ChunkHelper.IndexOfBlockInBorderedChunk(1, 33, 0);
		if (chunk19 != null)
		{
			lock (chunk19.DisposeLock)
			{
				if (!chunk19.Disposed)
				{
					int num123 = ChunkHelper.IndexOfBlockInChunk(0, 0, 31);
					for (int num124 = 0; num124 < 32; num124++)
					{
						_borderedChunkBlocks[num122 + num124] = chunk19.Data.Blocks.Get(num123 + num124);
					}
					ChunkData.BlockHitTimer[] blockHitTimers16 = chunk19.Data.BlockHitTimers;
					for (int num125 = 0; num125 < blockHitTimers16.Length; num125++)
					{
						ChunkData.BlockHitTimer blockHitTimer16 = blockHitTimers16[num125];
						if (blockHitTimer16.BlockIndex != -1)
						{
							int num126 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer16.BlockIndex, 0, 1, -1);
							if (num126 != -1)
							{
								_borderedChunkHitTimers.Add(num126, blockHitTimer16.Timer);
							}
						}
					}
				}
				else
				{
					ClearLine(num122);
				}
			}
		}
		else if (chunkY + 1 >= ChunkHelper.ChunksPerColumn)
		{
			EmptyLine(num122);
		}
		else
		{
			ClearLine(num122);
		}
		Chunk chunk20 = _gameInstance.MapModule.GetChunk(chunkX + 1, chunkY + 1, chunkZ - 1);
		int num127 = ChunkHelper.IndexOfBlockInBorderedChunk(33, 33, 0);
		if (chunk20 != null)
		{
			lock (chunk20.DisposeLock)
			{
				if (!chunk20.Disposed)
				{
					int num128 = ChunkHelper.IndexOfBlockInChunk(0, 0, 31);
					_borderedChunkBlocks[num127] = chunk20.Data.Blocks.Get(num128);
					if (chunk20.Data.TryGetBlockHitTimer(num128, out slotIndex, out var hitTimer5))
					{
						_borderedChunkHitTimers.Add(num127, hitTimer5);
					}
				}
				else
				{
					ClearBlock(num127);
				}
			}
		}
		else if (chunkY + 1 >= ChunkHelper.ChunksPerColumn)
		{
			EmptyBlock(num127);
		}
		else
		{
			ClearBlock(num127);
		}
		Chunk chunk21 = _gameInstance.MapModule.GetChunk(chunkX - 1, chunkY + 1, chunkZ);
		int num129 = ChunkHelper.IndexOfBlockInBorderedChunk(0, 33, 1);
		if (chunk21 != null)
		{
			lock (chunk21.DisposeLock)
			{
				if (!chunk21.Disposed)
				{
					int num130 = ChunkHelper.IndexOfBlockInChunk(31, 0, 0);
					for (int num131 = 0; num131 < 32; num131++)
					{
						_borderedChunkBlocks[num129] = chunk21.Data.Blocks.Get(num130);
						num130 += 32;
						num129 += 34;
					}
					ChunkData.BlockHitTimer[] blockHitTimers17 = chunk21.Data.BlockHitTimers;
					for (int num132 = 0; num132 < blockHitTimers17.Length; num132++)
					{
						ChunkData.BlockHitTimer blockHitTimer17 = blockHitTimers17[num132];
						if (blockHitTimer17.BlockIndex != -1)
						{
							int num133 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer17.BlockIndex, -1, 1, 0);
							if (num133 != -1)
							{
								_borderedChunkHitTimers.Add(num133, blockHitTimer17.Timer);
							}
						}
					}
				}
				else
				{
					ClearScatteredLine(num129, 34);
				}
			}
		}
		else if (chunkY + 1 >= ChunkHelper.ChunksPerColumn)
		{
			EmptyScatteredLine(num129, 34);
		}
		else
		{
			ClearScatteredLine(num129, 34);
		}
		Chunk chunk22 = _gameInstance.MapModule.GetChunk(chunkX, chunkY + 1, chunkZ);
		int num134 = ChunkHelper.IndexOfBlockInBorderedChunk(1, 33, 1);
		if (chunk22 != null)
		{
			lock (chunk22.DisposeLock)
			{
				if (!chunk22.Disposed)
				{
					int num135 = ChunkHelper.IndexOfBlockInChunk(0, 0, 0);
					for (int num136 = 0; num136 < 32; num136++)
					{
						for (int num137 = 0; num137 < 32; num137++)
						{
							_borderedChunkBlocks[num134 + num137] = chunk22.Data.Blocks.Get(num135 + num137);
						}
						num135 += 32;
						num134 += 34;
					}
					ChunkData.BlockHitTimer[] blockHitTimers18 = chunk22.Data.BlockHitTimers;
					for (int num138 = 0; num138 < blockHitTimers18.Length; num138++)
					{
						ChunkData.BlockHitTimer blockHitTimer18 = blockHitTimers18[num138];
						if (blockHitTimer18.BlockIndex != -1)
						{
							int num139 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer18.BlockIndex, 0, 1, 0);
							if (num139 != -1)
							{
								_borderedChunkHitTimers.Add(num139, blockHitTimer18.Timer);
							}
						}
					}
				}
				else
				{
					ClearMultipleLines(num134, 34);
				}
			}
		}
		else if (chunkY + 1 >= ChunkHelper.ChunksPerColumn)
		{
			EmptyMultipleLines(num134, 34);
		}
		else
		{
			ClearMultipleLines(num134, 34);
		}
		Chunk chunk23 = _gameInstance.MapModule.GetChunk(chunkX + 1, chunkY + 1, chunkZ);
		int num140 = ChunkHelper.IndexOfBlockInBorderedChunk(33, 33, 1);
		if (chunk23 != null)
		{
			lock (chunk23.DisposeLock)
			{
				if (!chunk23.Disposed)
				{
					int num141 = ChunkHelper.IndexOfBlockInChunk(0, 0, 0);
					for (int num142 = 0; num142 < 32; num142++)
					{
						_borderedChunkBlocks[num140] = chunk23.Data.Blocks.Get(num141);
						num141 += 32;
						num140 += 34;
					}
					ChunkData.BlockHitTimer[] blockHitTimers19 = chunk23.Data.BlockHitTimers;
					for (int num143 = 0; num143 < blockHitTimers19.Length; num143++)
					{
						ChunkData.BlockHitTimer blockHitTimer19 = blockHitTimers19[num143];
						if (blockHitTimer19.BlockIndex != -1)
						{
							int num144 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer19.BlockIndex, 1, 1, 0);
							if (num144 != -1)
							{
								_borderedChunkHitTimers.Add(num144, blockHitTimer19.Timer);
							}
						}
					}
				}
				else
				{
					ClearScatteredLine(num140, 34);
				}
			}
		}
		else if (chunkY + 1 >= ChunkHelper.ChunksPerColumn)
		{
			EmptyScatteredLine(num140, 34);
		}
		else
		{
			ClearScatteredLine(num140, 34);
		}
		Chunk chunk24 = _gameInstance.MapModule.GetChunk(chunkX - 1, chunkY + 1, chunkZ + 1);
		int num145 = ChunkHelper.IndexOfBlockInBorderedChunk(0, 33, 33);
		if (chunk24 != null)
		{
			lock (chunk24.DisposeLock)
			{
				if (!chunk24.Disposed)
				{
					int num146 = ChunkHelper.IndexOfBlockInChunk(31, 0, 0);
					_borderedChunkBlocks[num145] = chunk24.Data.Blocks.Get(num146);
					if (chunk24.Data.TryGetBlockHitTimer(num146, out slotIndex, out var hitTimer6))
					{
						_borderedChunkHitTimers.Add(num145, hitTimer6);
					}
				}
				else
				{
					ClearBlock(num145);
				}
			}
		}
		else if (chunkY + 1 >= ChunkHelper.ChunksPerColumn)
		{
			EmptyBlock(num145);
		}
		else
		{
			ClearBlock(num145);
		}
		Chunk chunk25 = _gameInstance.MapModule.GetChunk(chunkX, chunkY + 1, chunkZ + 1);
		int num147 = ChunkHelper.IndexOfBlockInBorderedChunk(1, 33, 33);
		if (chunk25 != null)
		{
			lock (chunk25.DisposeLock)
			{
				if (!chunk25.Disposed)
				{
					int num148 = ChunkHelper.IndexOfBlockInChunk(0, 0, 0);
					for (int num149 = 0; num149 < 32; num149++)
					{
						_borderedChunkBlocks[num147 + num149] = chunk25.Data.Blocks.Get(num148 + num149);
					}
					ChunkData.BlockHitTimer[] blockHitTimers20 = chunk25.Data.BlockHitTimers;
					for (int num150 = 0; num150 < blockHitTimers20.Length; num150++)
					{
						ChunkData.BlockHitTimer blockHitTimer20 = blockHitTimers20[num150];
						if (blockHitTimer20.BlockIndex != -1)
						{
							int num151 = ChunkHelper.IndexOfBlockInBorderedChunk(blockHitTimer20.BlockIndex, 0, 1, 1);
							if (num151 != -1)
							{
								_borderedChunkHitTimers.Add(num151, blockHitTimer20.Timer);
							}
						}
					}
				}
				else
				{
					ClearLine(num147);
				}
			}
		}
		else if (chunkY + 1 >= ChunkHelper.ChunksPerColumn)
		{
			EmptyLine(num147);
		}
		else
		{
			ClearLine(num147);
		}
		Chunk chunk26 = _gameInstance.MapModule.GetChunk(chunkX + 1, chunkY + 1, chunkZ + 1);
		int num152 = ChunkHelper.IndexOfBlockInBorderedChunk(33, 33, 33);
		if (chunk26 != null)
		{
			lock (chunk26.DisposeLock)
			{
				if (!chunk26.Disposed)
				{
					int num153 = ChunkHelper.IndexOfBlockInChunk(0, 0, 0);
					_borderedChunkBlocks[num152] = chunk26.Data.Blocks.Get(num153);
					if (chunk26.Data.TryGetBlockHitTimer(num153, out slotIndex, out var hitTimer7))
					{
						_borderedChunkHitTimers.Add(num152, hitTimer7);
					}
				}
				else
				{
					ClearBlock(num152);
				}
				return;
			}
		}
		if (chunkY + 1 >= ChunkHelper.ChunksPerColumn)
		{
			EmptyBlock(num152);
		}
		else
		{
			ClearBlock(num152);
		}
	}

	public MapGeometryBuilder(GameInstance gameInstance)
	{
		_gameInstance = gameInstance;
		_chunkGeometryBuilder = new ChunkGeometryBuilder();
		for (int i = 0; i < _missingChunkLine.Length; i++)
		{
			_missingChunkLine[i] = int.MaxValue;
		}
		for (int j = 0; j < _emptyChunkLine.Length; j++)
		{
			_emptyChunkLine[j] = 0;
		}
		AllocateLightData();
		Resume();
	}

	protected override void DoDispose()
	{
		Suspend();
		Disposable result;
		while (_chunkGeometryBuilder.DisposeRequests.TryDequeue(out result))
		{
			result.Dispose();
		}
	}

	public void Suspend()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_threadCancellationTokenSource.Cancel();
		_restartSpiralEvent.Set();
		_thread.Join();
		_thread = null;
		_threadCancellationTokenSource = null;
	}

	public void Resume()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_chunkGeometryBuilder.SetBlockTypes(_gameInstance.MapModule.ClientBlockTypes);
		_chunkGeometryBuilder.SetBlockHitboxes(_gameInstance.ServerSettings.BlockHitboxes);
		_chunkGeometryBuilder.SetLightLevels(_gameInstance.MapModule.LightLevels);
		_chunkGeometryBuilder.SetEnvironments(_gameInstance.ServerSettings.Environments);
		_chunkGeometryBuilder.SetAtlasSizes(_gameInstance.AtlasSizes);
		_threadCancellationTokenSource = new CancellationTokenSource();
		_threadCancellationToken = _threadCancellationTokenSource.Token;
		_thread = new Thread(ThreadStart)
		{
			Name = "BackgroundMapGeometryBuilder",
			IsBackground = true
		};
		_thread.Start();
	}

	public void HandleDisposeRequests()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_disposeRequestsStopwatch.Restart();
		Disposable result;
		while (_chunkGeometryBuilder.DisposeRequests.TryDequeue(out result))
		{
			result.Dispose();
			if ((float)_disposeRequestsStopwatch.ElapsedMilliseconds > 0.1f)
			{
				break;
			}
		}
	}

	public void EnsureEnoughChunkUpdateTasks()
	{
		_chunkGeometryBuilder.EnsureEnoughChunkUpdateTasks();
	}

	public int GetChunkUpdateTaskQueueCount()
	{
		return _chunkGeometryBuilder.ChunkUpdateTaskQueue.Count;
	}

	public void EnqueueChunkUpdateTask(RenderedChunk.ChunkUpdateTask chunkUpdateTask)
	{
		if (chunkUpdateTask.AnimatedBlocks != null)
		{
			for (int i = 0; i < chunkUpdateTask.AnimatedBlocks.Length; i++)
			{
				_chunkGeometryBuilder.DisposeRequests.Enqueue(chunkUpdateTask.AnimatedBlocks[i].Renderer);
			}
			chunkUpdateTask.AnimatedBlocks = null;
		}
		_chunkGeometryBuilder.ChunkUpdateTaskQueue.Add(chunkUpdateTask);
	}

	public void RestartSpiral(Vector3 positionInChunk, int startChunkX, int startChunkY, int startChunkZ, int spiralRadius)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		lock (_spiralIteratorLock)
		{
			_startChunkX = startChunkX;
			_startChunkY = startChunkY;
			_startChunkZ = startChunkZ;
			_spiralRadius = spiralRadius;
			OrderNearestChunks(positionInChunk, startChunkX, startChunkY, startChunkZ);
		}
		_restartSpiralEvent.Set();
	}

	private void OrderNearestChunks(Vector3 positionInChunk, int startChunkX, int startChunkY, int startChunkZ)
	{
		for (int i = 0; i < RebuildOffsets.Length; i++)
		{
			int num = RebuildOffsets[i];
			for (int j = 0; j < RebuildOffsets.Length; j++)
			{
				int num2 = RebuildOffsets[j];
				for (int k = 0; k < RebuildOffsets.Length; k++)
				{
					int num3 = RebuildOffsets[k];
					int num4 = (i * 3 + j) * 3 + k;
					_nearestChunks[num4] = new IntVector3(startChunkX + num2, startChunkY + num3, startChunkZ + num);
					Vector3 value = new Vector3(15.5f + (float)(32 * num2), 15.5f + (float)(32 * num3), 15.5f + (float)(32 * num));
					float num5 = Vector3.DistanceSquared(positionInChunk, value);
					_nearestChunkDistances[num4] = num5;
				}
			}
		}
		_nearestChunkDistances[0] = 0f;
		Array.Sort(_nearestChunkDistances, _nearestChunks);
	}

	private void ThreadStart()
	{
		while (true)
		{
			_restartSpiralEvent.WaitOne();
			while (true)
			{
				IL_0013:
				if (_threadCancellationToken.IsCancellationRequested)
				{
					return;
				}
				int chunkYMin = _gameInstance.MapModule.ChunkYMin;
				int startChunkY;
				lock (_spiralIteratorLock)
				{
					int startChunkX = _startChunkX;
					startChunkY = _startChunkY;
					int startChunkZ = _startChunkZ;
					_spiralIterator.Initialize(_startChunkX, _startChunkZ, _spiralRadius);
				}
				for (int i = 0; i < _nearestChunks.Length; i++)
				{
					IntVector3 intVector = _nearestChunks[i];
					ChunkColumn chunkColumn = _gameInstance.MapModule.GetChunkColumn(ChunkHelper.IndexOfChunkColumn(intVector.X, intVector.Z));
					if (chunkColumn == null || intVector.Y < chunkYMin)
					{
						continue;
					}
					Chunk chunk = chunkColumn.GetChunk(intVector.Y);
					if (chunk != null && chunk.Rendered != null && chunk.Rendered.RebuildState == RenderedChunk.ChunkRebuildState.ReadyForRebuild)
					{
						RebuildChunk(chunkColumn, chunk);
						if (_restartSpiralEvent.WaitOne(0))
						{
							goto IL_0013;
						}
					}
				}
				foreach (long item in _spiralIterator)
				{
					ChunkColumn chunkColumn2 = _gameInstance.MapModule.GetChunkColumn(item);
					if (chunkColumn2 == null)
					{
						continue;
					}
					for (int j = 0; j < ChunkHelper.ChunksPerColumn; j++)
					{
						if (_threadCancellationToken.IsCancellationRequested)
						{
							return;
						}
						int num = startChunkY - j;
						if (num < 0)
						{
							num = startChunkY - num;
						}
						if (num < chunkYMin)
						{
							continue;
						}
						Chunk chunk2 = chunkColumn2.GetChunk(num);
						if (chunk2 != null && chunk2.Rendered != null && chunk2.Rendered.RebuildState == RenderedChunk.ChunkRebuildState.ReadyForRebuild)
						{
							RebuildChunk(chunkColumn2, chunk2);
							if (_restartSpiralEvent.WaitOne(0))
							{
								goto IL_0013;
							}
						}
					}
				}
				break;
			}
		}
	}

	private void RebuildChunk(ChunkColumn chunkColumn, Chunk chunk)
	{
		lock (chunk.DisposeLock)
		{
			if (chunk.Disposed)
			{
				return;
			}
			chunk.Rendered.RebuildState = RenderedChunk.ChunkRebuildState.Rebuilding;
			if (chunk.Data.Blocks.IsSolidAir())
			{
				chunk.Rendered.UpdateTask = null;
				chunk.Rendered.RebuildState = RenderedChunk.ChunkRebuildState.UpdateReady;
				return;
			}
			SetupBorderedChunkData(chunk.X, chunk.Y, chunk.Z, chunk.Data, chunkColumn.Tints, chunkColumn.Environments);
			if (IsSolidBorderedChunk(_borderedChunkBlocks))
			{
				chunk.Rendered.UpdateTask = null;
				chunk.Rendered.RebuildState = RenderedChunk.ChunkRebuildState.UpdateReady;
				return;
			}
		}
		CalculateLighting(chunk);
		lock (chunk.DisposeLock)
		{
			if (chunk.Disposed)
			{
				return;
			}
			Buffer.BlockCopy(chunk.Data.BorderedLightAmounts, 0, _borderedChunkLightAmounts, 0, chunk.Data.BorderedLightAmounts.Length * 2);
		}
		RenderedChunk.ChunkUpdateTask chunkUpdateTask = _chunkGeometryBuilder.BuildGeometry(chunk.X, chunk.Y, chunk.Z, chunkColumn, _borderedChunkBlocks, _borderedChunkHitTimers, _borderedChunkLightAmounts, _blendedBlockCornerBiomeTints, _borderedColumnEnvironmentIds, _gameInstance.MapModule.TextureAtlas.Width, _gameInstance.MapModule.TextureAtlas.Height, _threadCancellationToken);
		lock (chunk.DisposeLock)
		{
			if (chunk.Disposed)
			{
				if (chunkUpdateTask != null)
				{
					EnqueueChunkUpdateTask(chunkUpdateTask);
				}
			}
			else if (_threadCancellationToken.IsCancellationRequested)
			{
				chunk.Rendered.RebuildState = RenderedChunk.ChunkRebuildState.ReadyForRebuild;
			}
			else
			{
				chunk.Rendered.UpdateTask = chunkUpdateTask;
				chunk.Rendered.RebuildState = RenderedChunk.ChunkRebuildState.UpdateReady;
			}
		}
	}

	private void AllocateLightData()
	{
		for (int i = 0; i < 27; i++)
		{
			adjacentChunkLightAmounts[i] = new ushort[32768];
		}
		int maxChunksLoaded = _gameInstance.MapModule.GetMaxChunksLoaded();
		for (int j = 0; j < maxChunksLoaded; j++)
		{
			_selfLightAmountQueue.Enqueue(new ushort[32768]);
			_borderedLightAmountQueue.Enqueue(new ushort[39304]);
		}
		Interlocked.Add(ref _selfLightAmountAllocatedTotal, maxChunksLoaded);
		Interlocked.Add(ref _borderedLightAmountAllocatedTotal, maxChunksLoaded);
	}

	public void EnqueueSelfLightAmountArray(ushort[] selfLightAmountData)
	{
		if (_selfLightAmountAllocatedTotal < _gameInstance.MapModule.GetMaxChunksLoaded())
		{
			Array.Clear(selfLightAmountData, 0, selfLightAmountData.Length);
			_selfLightAmountQueue.Enqueue(selfLightAmountData);
		}
		else
		{
			Interlocked.Decrement(ref _selfLightAmountAllocatedTotal);
		}
	}

	public ushort[] DequeueSelfLightAmountArray()
	{
		if (!_selfLightAmountQueue.TryDequeue(out var result))
		{
			result = new ushort[32768];
			Interlocked.Increment(ref _selfLightAmountAllocatedTotal);
		}
		return result;
	}

	public void EnqueueBorderedLightAmountArray(ushort[] borderedLightAmountData)
	{
		if (_borderedLightAmountAllocatedTotal < _gameInstance.MapModule.GetMaxChunksLoaded())
		{
			Array.Clear(borderedLightAmountData, 0, borderedLightAmountData.Length);
			_borderedLightAmountQueue.Enqueue(borderedLightAmountData);
		}
		else
		{
			Interlocked.Decrement(ref _borderedLightAmountAllocatedTotal);
		}
	}

	public ushort[] DequeueBorderedLightAmountArray()
	{
		if (!_borderedLightAmountQueue.TryDequeue(out var result))
		{
			result = new ushort[39304];
			Interlocked.Increment(ref _borderedLightAmountAllocatedTotal);
		}
		return result;
	}

	private void CalculateLighting(Chunk centerChunk)
	{
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Invalid comparison between Unknown and I4
		//IL_0388: Unknown result type (might be due to invalid IL or missing references)
		//IL_038e: Invalid comparison between Unknown and I4
		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a3: Invalid comparison between Unknown and I4
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ad: Invalid comparison between Unknown and I4
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				ChunkColumn chunkColumn = _gameInstance.MapModule.GetChunkColumn(centerChunk.X + (i - 1), centerChunk.Z + (j - 1));
				if (chunkColumn == null)
				{
					for (int k = 0; k < 3; k++)
					{
						int num = (k * 3 + j) * 3 + i;
						adjacentChunkBlocks[num] = null;
						ushort[] array = adjacentChunkLightAmounts[num];
						for (int l = 0; l < array.Length; l++)
						{
							array[l] = 61440;
						}
					}
					continue;
				}
				lock (chunkColumn.DisposeLock)
				{
					for (int m = 0; m < 3; m++)
					{
						Chunk chunk = null;
						if (!chunkColumn.Disposed)
						{
							chunk = chunkColumn.GetChunk(centerChunk.Y + (m - 1));
						}
						int num2 = (m * 3 + j) * 3 + i;
						bool flag = false;
						if (chunk != null)
						{
							lock (chunk.DisposeLock)
							{
								if (chunk.Rendered != null)
								{
									flag = true;
									if (chunk.Data.SelfLightNeedsUpdate)
									{
										chunk.Data.SelfLightNeedsUpdate = false;
										if (chunk.Data.SelfLightAmounts == null)
										{
											chunk.Data.SelfLightAmounts = DequeueSelfLightAmountArray();
										}
										int num3 = (centerChunk.Y + (m - 1)) * 32;
										for (int n = 0; n < 32; n++)
										{
											for (int num4 = 0; num4 < 32; num4++)
											{
												ushort num5 = chunkColumn.Heights[(n << 5) + num4];
												for (int num6 = 0; num6 < 32; num6++)
												{
													int num7 = ((num3 + num6 >= num5) ? 15 : 0);
													int blockIdx = (num6 * 32 + n) * 32 + num4;
													ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[chunk.Data.Blocks.Get(blockIdx)];
													ushort num8 = (ushort)(clientBlockType.LightEmitted.R | (clientBlockType.LightEmitted.G << 4) | (clientBlockType.LightEmitted.B << 8) | (num7 << 12));
													chunk.Data.SelfLightAmounts[(num6 * 32 + n) * 32 + num4] = num8;
												}
											}
										}
										for (int num9 = 0; num9 < 32; num9++)
										{
											for (int num10 = 0; num10 < 32; num10++)
											{
												for (int num11 = 0; num11 < 32; num11++)
												{
													int num12 = (num9 * 32 + num10) * 32 + num11;
													ClientBlockType clientBlockType2 = _gameInstance.MapModule.ClientBlockTypes[chunk.Data.Blocks.Get(num12)];
													if ((int)clientBlockType2.Opacity == 0)
													{
														continue;
													}
													for (int num13 = 0; num13 < 4; num13++)
													{
														_floodFillQueue.Push(num12);
														while (_floodFillQueue.Count > 0)
														{
															int num14 = _floodFillQueue.Pop();
															ushort num15 = chunk.Data.SelfLightAmounts[num14];
															int num16 = (num15 >> 4 * num13) & 0xF;
															if (num16 <= 1)
															{
																continue;
															}
															ClientBlockType clientBlockType3 = _gameInstance.MapModule.ClientBlockTypes[chunk.Data.Blocks.Get(num14)];
															if ((int)clientBlockType3.Opacity == 0)
															{
																continue;
															}
															if ((int)clientBlockType3.Opacity == 2 || (int)clientBlockType3.Opacity == 1)
															{
																num16--;
																if (num16 <= 1)
																{
																	continue;
																}
															}
															int num17 = num14 % 32;
															int num18 = num14 / 32 % 32;
															int num19 = num14 / 1024;
															if (num17 < 31)
															{
																FloodIntoBlock(num14 + 1, chunk.Data.Blocks.Get(num14 + 1), chunk.Data.SelfLightAmounts, num13, num16);
															}
															if (num17 > 0)
															{
																FloodIntoBlock(num14 + -1, chunk.Data.Blocks.Get(num14 + -1), chunk.Data.SelfLightAmounts, num13, num16);
															}
															if (num19 < 31)
															{
																FloodIntoBlock(num14 + 1024, chunk.Data.Blocks.Get(num14 + 1024), chunk.Data.SelfLightAmounts, num13, num16);
															}
															if (num19 > 0)
															{
																FloodIntoBlock(num14 + -1024, chunk.Data.Blocks.Get(num14 + -1024), chunk.Data.SelfLightAmounts, num13, num16);
															}
															if (num18 < 31)
															{
																FloodIntoBlock(num14 + 32, chunk.Data.Blocks.Get(num14 + 32), chunk.Data.SelfLightAmounts, num13, num16);
															}
															if (num18 > 0)
															{
																FloodIntoBlock(num14 + -32, chunk.Data.Blocks.Get(num14 + -32), chunk.Data.SelfLightAmounts, num13, num16);
															}
														}
													}
												}
											}
										}
									}
									adjacentChunkBlocks[num2] = chunk.Data.Blocks;
									Buffer.BlockCopy(chunk.Data.SelfLightAmounts, 0, adjacentChunkLightAmounts[num2], 0, chunk.Data.SelfLightAmounts.Length * 2);
								}
							}
						}
						if (!flag)
						{
							adjacentChunkBlocks[num2] = null;
							ushort[] array2 = adjacentChunkLightAmounts[num2];
							for (int num20 = 0; num20 < array2.Length; num20++)
							{
								array2[num20] = 61440;
							}
						}
					}
				}
			}
		}
		for (int num21 = 0; num21 < 2; num21++)
		{
			for (int num22 = 0; num22 < 3; num22++)
			{
				for (int num23 = 0; num23 < 3; num23++)
				{
					for (int num24 = 0; num24 < 3; num24++)
					{
						int num25 = (num24 * 3 + num23) * 3 + num22;
						if (num23 < 2)
						{
							int targetChunkOffset = (num24 * 3 + (num23 + 1)) * 3 + num22;
							for (int num26 = 0; num26 < 32; num26++)
							{
								for (int num27 = 0; num27 < 32; num27++)
								{
									int num28 = (num27 * 32 + 32 - 1) * 32 + num26;
									ushort sourceLightAmount = adjacentChunkLightAmounts[num25][num28];
									int targetBlockIndex = num27 * 32 * 32 + num26;
									FloodIntoChunk(sourceLightAmount, targetChunkOffset, targetBlockIndex);
								}
							}
						}
						if (num23 > 0)
						{
							int targetChunkOffset2 = (num24 * 3 + (num23 - 1)) * 3 + num22;
							for (int num29 = 0; num29 < 32; num29++)
							{
								for (int num30 = 0; num30 < 32; num30++)
								{
									int num31 = num30 * 32 * 32 + num29;
									ushort sourceLightAmount2 = adjacentChunkLightAmounts[num25][num31];
									int targetBlockIndex2 = (num30 * 32 + 32 - 1) * 32 + num29;
									FloodIntoChunk(sourceLightAmount2, targetChunkOffset2, targetBlockIndex2);
								}
							}
						}
						if (num22 < 2)
						{
							int targetChunkOffset3 = (num24 * 3 + num23) * 3 + num22 + 1;
							for (int num32 = 0; num32 < 32; num32++)
							{
								for (int num33 = 0; num33 < 32; num33++)
								{
									int num34 = (num32 * 32 + num33) * 32 + 32 - 1;
									ushort sourceLightAmount3 = adjacentChunkLightAmounts[num25][num34];
									int targetBlockIndex3 = (num32 * 32 + num33) * 32;
									FloodIntoChunk(sourceLightAmount3, targetChunkOffset3, targetBlockIndex3);
								}
							}
						}
						if (num22 > 0)
						{
							int targetChunkOffset4 = (num24 * 3 + num23) * 3 + num22 - 1;
							for (int num35 = 0; num35 < 32; num35++)
							{
								for (int num36 = 0; num36 < 32; num36++)
								{
									int num37 = (num35 * 32 + num36) * 32;
									ushort sourceLightAmount4 = adjacentChunkLightAmounts[num25][num37];
									int targetBlockIndex4 = (num35 * 32 + num36) * 32 + 32 - 1;
									FloodIntoChunk(sourceLightAmount4, targetChunkOffset4, targetBlockIndex4);
								}
							}
						}
						if (num24 < 2)
						{
							int targetChunkOffset5 = ((num24 + 1) * 3 + num23) * 3 + num22;
							for (int num38 = 0; num38 < 32; num38++)
							{
								for (int num39 = 0; num39 < 32; num39++)
								{
									int num40 = (992 + num39) * 32 + num38;
									ushort sourceLightAmount5 = adjacentChunkLightAmounts[num25][num40];
									int targetBlockIndex5 = num39 * 32 + num38;
									FloodIntoChunk(sourceLightAmount5, targetChunkOffset5, targetBlockIndex5);
								}
							}
						}
						if (num24 <= 0)
						{
							continue;
						}
						int targetChunkOffset6 = ((num24 - 1) * 3 + num23) * 3 + num22;
						for (int num41 = 0; num41 < 32; num41++)
						{
							for (int num42 = 0; num42 < 32; num42++)
							{
								int num43 = num42 * 32 + num41;
								ushort sourceLightAmount6 = adjacentChunkLightAmounts[num25][num43];
								int targetBlockIndex6 = (992 + num42) * 32 + num41;
								FloodIntoChunk(sourceLightAmount6, targetChunkOffset6, targetBlockIndex6);
							}
						}
					}
				}
			}
		}
		if (centerChunk.Data.BorderedLightAmounts == null)
		{
			centerChunk.Data.BorderedLightAmounts = DequeueBorderedLightAmountArray();
		}
		ushort[] borderedLightAmounts = centerChunk.Data.BorderedLightAmounts;
		ushort[] src = adjacentChunkLightAmounts[GetAdjacentChunkIndex(0, 0, 0)];
		for (int num44 = 0; num44 < 32; num44++)
		{
			for (int num45 = 0; num45 < 32; num45++)
			{
				Buffer.BlockCopy(src, (num44 * 32 + num45) * 32 * 2, borderedLightAmounts, (((1 + num44) * 34 + (1 + num45)) * 34 + 1) * 2, 64);
			}
		}
		ushort[] src2 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(0, -1, 0)];
		ushort[] src3 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(0, 1, 0)];
		ushort[] src4 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(0, 0, -1)];
		ushort[] src5 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(0, 0, 1)];
		ushort[] array3 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(1, 0, 0)];
		ushort[] array4 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(-1, 0, 0)];
		for (int num46 = 0; num46 < 32; num46++)
		{
			Buffer.BlockCopy(src2, (992 + num46) * 32 * 2, borderedLightAmounts, ((num46 + 1) * 34 + 1) * 2, 64);
			Buffer.BlockCopy(src3, num46 * 32 * 2, borderedLightAmounts, ((1123 + num46) * 34 + 1) * 2, 64);
			Buffer.BlockCopy(src4, (num46 * 32 + 32 - 1) * 32 * 2, borderedLightAmounts, ((num46 + 1) * 34 * 34 + 1) * 2, 64);
			Buffer.BlockCopy(src5, num46 * 32 * 32 * 2, borderedLightAmounts, (((num46 + 1) * 34 + 34 - 1) * 34 + 1) * 2, 64);
		}
		for (int num47 = 0; num47 < 32; num47++)
		{
			for (int num48 = 0; num48 < 32; num48++)
			{
				borderedLightAmounts[((num47 + 1) * 34 + num48 + 1) * 34] = array4[(num47 * 32 + num48) * 32 + 32 - 1];
				borderedLightAmounts[((num47 + 1) * 34 + num48 + 1) * 34 + 34 - 1] = array3[(num47 * 32 + num48) * 32];
			}
		}
		ushort[] array5 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(-1, -1, -1)];
		int num49 = 0;
		borderedLightAmounts[num49] = array5[ChunkHelper.IndexOfBlockInChunk(31, 31, 31)];
		ushort[] src6 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(0, -1, -1)];
		int srcOffset = ChunkHelper.IndexOfBlockInChunk(0, 31, 31) * 2;
		int dstOffset = ChunkHelper.IndexOfBlockInBorderedChunk(1, 0, 0) * 2;
		Buffer.BlockCopy(src6, srcOffset, borderedLightAmounts, dstOffset, 64);
		ushort[] array6 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(1, -1, -1)];
		int num50 = ChunkHelper.IndexOfBlockInBorderedChunk(33, 0, 0);
		borderedLightAmounts[num50] = array6[ChunkHelper.IndexOfBlockInChunk(0, 31, 31)];
		ushort[] array7 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(-1, -1, 0)];
		int num51 = ChunkHelper.IndexOfBlockInChunk(31, 31, 0);
		int num52 = ChunkHelper.IndexOfBlockInBorderedChunk(0, 0, 1);
		for (int num53 = 0; num53 < 32; num53++)
		{
			borderedLightAmounts[num52] = array7[num51];
			num51 += 32;
			num52 += 34;
		}
		ushort[] array8 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(1, -1, 0)];
		int num54 = ChunkHelper.IndexOfBlockInChunk(0, 31, 0);
		int num55 = ChunkHelper.IndexOfBlockInBorderedChunk(33, 0, 1);
		for (int num56 = 0; num56 < 32; num56++)
		{
			borderedLightAmounts[num55] = array8[num54];
			num54 += 32;
			num55 += 34;
		}
		ushort[] array9 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(-1, -1, 1)];
		int num57 = ChunkHelper.IndexOfBlockInBorderedChunk(0, 0, 33);
		borderedLightAmounts[num57] = array9[ChunkHelper.IndexOfBlockInChunk(31, 31, 0)];
		ushort[] src7 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(0, -1, 1)];
		int srcOffset2 = ChunkHelper.IndexOfBlockInChunk(0, 31, 0) * 2;
		int dstOffset2 = ChunkHelper.IndexOfBlockInBorderedChunk(1, 0, 33) * 2;
		Buffer.BlockCopy(src7, srcOffset2, borderedLightAmounts, dstOffset2, 64);
		ushort[] array10 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(1, -1, 1)];
		int num58 = ChunkHelper.IndexOfBlockInBorderedChunk(33, 0, 33);
		borderedLightAmounts[num58] = array10[ChunkHelper.IndexOfBlockInChunk(0, 31, 0)];
		ushort[] array11 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(-1, 0, -1)];
		int num59 = ChunkHelper.IndexOfBlockInChunk(31, 0, 31);
		int num60 = ChunkHelper.IndexOfBlockInBorderedChunk(0, 1, 0);
		for (int num61 = 0; num61 < 32; num61++)
		{
			borderedLightAmounts[num60] = array11[num59];
			num59 += 1024;
			num60 += 1156;
		}
		ushort[] array12 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(1, 0, -1)];
		int num62 = ChunkHelper.IndexOfBlockInChunk(0, 0, 31);
		int num63 = ChunkHelper.IndexOfBlockInBorderedChunk(33, 1, 0);
		for (int num64 = 0; num64 < 32; num64++)
		{
			borderedLightAmounts[num63] = array12[num62];
			num62 += 1024;
			num63 += 1156;
		}
		ushort[] array13 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(-1, 0, 1)];
		int num65 = ChunkHelper.IndexOfBlockInChunk(31, 0, 0);
		int num66 = ChunkHelper.IndexOfBlockInBorderedChunk(0, 1, 33);
		for (int num67 = 0; num67 < 32; num67++)
		{
			borderedLightAmounts[num66] = array13[num65];
			num65 += 1024;
			num66 += 1156;
		}
		ushort[] array14 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(1, 0, 1)];
		int num68 = ChunkHelper.IndexOfBlockInChunk(0, 0, 0);
		int num69 = ChunkHelper.IndexOfBlockInBorderedChunk(33, 1, 33);
		for (int num70 = 0; num70 < 32; num70++)
		{
			borderedLightAmounts[num69] = array14[num68];
			num68 += 1024;
			num69 += 1156;
		}
		ushort[] array15 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(-1, 1, -1)];
		int num71 = ChunkHelper.IndexOfBlockInBorderedChunk(0, 33, 0);
		borderedLightAmounts[num71] = array15[ChunkHelper.IndexOfBlockInChunk(31, 0, 31)];
		ushort[] src8 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(0, 1, -1)];
		int srcOffset3 = ChunkHelper.IndexOfBlockInChunk(0, 0, 31) * 2;
		int dstOffset3 = ChunkHelper.IndexOfBlockInBorderedChunk(1, 33, 0) * 2;
		Buffer.BlockCopy(src8, srcOffset3, borderedLightAmounts, dstOffset3, 64);
		ushort[] array16 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(1, 1, -1)];
		int num72 = ChunkHelper.IndexOfBlockInBorderedChunk(33, 33, 0);
		borderedLightAmounts[num72] = array16[ChunkHelper.IndexOfBlockInChunk(0, 0, 31)];
		ushort[] array17 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(-1, 1, 0)];
		int num73 = ChunkHelper.IndexOfBlockInChunk(31, 0, 0);
		int num74 = ChunkHelper.IndexOfBlockInBorderedChunk(0, 33, 1);
		for (int num75 = 0; num75 < 32; num75++)
		{
			borderedLightAmounts[num74] = array17[num73];
			num73 += 32;
			num74 += 34;
		}
		ushort[] array18 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(1, 1, 0)];
		int num76 = ChunkHelper.IndexOfBlockInChunk(0, 0, 0);
		int num77 = ChunkHelper.IndexOfBlockInBorderedChunk(33, 33, 1);
		for (int num78 = 0; num78 < 32; num78++)
		{
			borderedLightAmounts[num77] = array18[num76];
			num76 += 32;
			num77 += 34;
		}
		ushort[] array19 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(-1, 1, 1)];
		int num79 = ChunkHelper.IndexOfBlockInBorderedChunk(0, 33, 33);
		borderedLightAmounts[num79] = array19[ChunkHelper.IndexOfBlockInChunk(31, 0, 0)];
		ushort[] src9 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(0, 1, 1)];
		int srcOffset4 = ChunkHelper.IndexOfBlockInChunk(0, 0, 0) * 2;
		int dstOffset4 = ChunkHelper.IndexOfBlockInBorderedChunk(1, 33, 33) * 2;
		Buffer.BlockCopy(src9, srcOffset4, borderedLightAmounts, dstOffset4, 64);
		ushort[] array20 = adjacentChunkLightAmounts[GetAdjacentChunkIndex(1, 1, 1)];
		int num80 = ChunkHelper.IndexOfBlockInBorderedChunk(33, 33, 33);
		borderedLightAmounts[num80] = array20[ChunkHelper.IndexOfBlockInChunk(0, 0, 0)];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetAdjacentChunkIndex(int x, int y, int z)
	{
		return ((y + 1) * 3 + z + 1) * 3 + x + 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void FloodIntoBlock(int blockIndex, int blockId, ushort[] lightAmounts, int channel, int channelLightAmount)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Invalid comparison between Unknown and I4
		if ((int)_gameInstance.MapModule.ClientBlockTypes[blockId].Opacity == 2)
		{
			channelLightAmount--;
		}
		ushort num = lightAmounts[blockIndex];
		int num2 = (num >> 4 * channel) & 0xF;
		if (num2 < channelLightAmount - 1)
		{
			num2 = channelLightAmount - 1;
			lightAmounts[blockIndex] = (ushort)((num & ~(15 << 4 * channel)) | (num2 << 4 * channel));
			_floodFillQueue.Push(blockIndex);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void FloodIntoChunk(int sourceLightAmount, int targetChunkOffset, int targetBlockIndex)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Invalid comparison between Unknown and I4
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Invalid comparison between Unknown and I4
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Invalid comparison between Unknown and I4
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Invalid comparison between Unknown and I4
		PaletteChunkData paletteChunkData = adjacentChunkBlocks[targetChunkOffset];
		if (paletteChunkData == null)
		{
			return;
		}
		int num = paletteChunkData.Get(targetBlockIndex);
		ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[num];
		if ((int)clientBlockType.Opacity == 0)
		{
			return;
		}
		ushort[] array = adjacentChunkLightAmounts[targetChunkOffset];
		for (int i = 0; i < 4; i++)
		{
			int channelLightAmount = (sourceLightAmount >> 4 * i) & 0xF;
			FloodIntoBlock(targetBlockIndex, num, array, i, channelLightAmount);
			while (_floodFillQueue.Count > 0)
			{
				int num2 = _floodFillQueue.Pop();
				ushort num3 = array[num2];
				int num4 = (num3 >> 4 * i) & 0xF;
				if (num4 <= 1)
				{
					continue;
				}
				ClientBlockType clientBlockType2 = _gameInstance.MapModule.ClientBlockTypes[paletteChunkData.Get(num2)];
				if ((int)clientBlockType2.Opacity == 0)
				{
					continue;
				}
				if ((int)clientBlockType2.Opacity == 2 || (int)clientBlockType2.Opacity == 1)
				{
					num4--;
					if (num4 <= 1)
					{
						continue;
					}
				}
				int num5 = num2 % 32;
				int num6 = num2 / 32 % 32;
				int num7 = num2 / 1024;
				if (num5 < 31)
				{
					FloodIntoBlock(num2 + 1, paletteChunkData.Get(num2 + 1), array, i, num4);
				}
				if (num5 > 0)
				{
					FloodIntoBlock(num2 + -1, paletteChunkData.Get(num2 + -1), array, i, num4);
				}
				if (num7 < 31)
				{
					FloodIntoBlock(num2 + 1024, paletteChunkData.Get(num2 + 1024), array, i, num4);
				}
				if (num7 > 0)
				{
					FloodIntoBlock(num2 + -1024, paletteChunkData.Get(num2 + -1024), array, i, num4);
				}
				if (num6 < 31)
				{
					FloodIntoBlock(num2 + 32, paletteChunkData.Get(num2 + 32), array, i, num4);
				}
				if (num6 > 0)
				{
					FloodIntoBlock(num2 + -32, paletteChunkData.Get(num2 + -32), array, i, num4);
				}
			}
		}
	}
}
