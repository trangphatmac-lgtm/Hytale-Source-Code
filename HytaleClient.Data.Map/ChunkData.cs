using System;
using System.Collections.Generic;
using HytaleClient.Audio;
using HytaleClient.Data.Map.Chunk;
using HytaleClient.Utils;

namespace HytaleClient.Data.Map;

internal class ChunkData
{
	public struct InteractionStateInfo
	{
		public int BlockId;

		public ClientBlockType BlockType;

		public float StateFrameTime;

		public AudioDevice.SoundEventReference SoundEventReference;
	}

	public struct BlockHitTimer
	{
		public int BlockIndex;

		public float Timer;
	}

	public PaletteChunkData Blocks = new PaletteChunkData();

	public Dictionary<int, InteractionStateInfo> CurrentInteractionStates = new Dictionary<int, InteractionStateInfo>();

	public const int MaxBlockHitTimers = 16;

	public readonly BlockHitTimer[] BlockHitTimers = new BlockHitTimer[16];

	public ushort[] SelfLightAmounts;

	public bool SelfLightNeedsUpdate = true;

	public ushort[] BorderedLightAmounts;

	public ChunkData()
	{
		for (int i = 0; i < BlockHitTimers.Length; i++)
		{
			BlockHitTimers[i].BlockIndex = -1;
		}
	}

	public int GetBlock(int worldX, int worldY, int worldZ)
	{
		return Blocks.Get(ChunkHelper.IndexOfWorldBlockInChunk(worldX, worldY, worldZ));
	}

	public void SetBlock(int worldX, int worldY, int worldZ, int blockId)
	{
		int num = ChunkHelper.IndexOfWorldBlockInChunk(worldX, worldY, worldZ);
		Blocks.Set(num, blockId);
		for (int i = 0; i < BlockHitTimers.Length; i++)
		{
			if (BlockHitTimers[i].BlockIndex == num)
			{
				BlockHitTimers[i].BlockIndex = -1;
				BlockHitTimers[i].Timer = 0f;
				break;
			}
		}
	}

	public bool TryGetBlockHitTimer(int blockIndex, out int slotIndex, out float hitTimer)
	{
		for (int i = 0; i < BlockHitTimers.Length; i++)
		{
			if (BlockHitTimers[i].BlockIndex == blockIndex)
			{
				slotIndex = i;
				hitTimer = BlockHitTimers[i].Timer;
				return true;
			}
		}
		slotIndex = -1;
		hitTimer = 0f;
		return false;
	}

	public void SetBlockHitTimer(int blockIndex, float hitTimer)
	{
		if (hitTimer == 0f)
		{
			for (int i = 0; i < BlockHitTimers.Length; i++)
			{
				if (BlockHitTimers[i].BlockIndex == blockIndex)
				{
					BlockHitTimers[i].BlockIndex = -1;
					BlockHitTimers[i].Timer = 0f;
					break;
				}
			}
			return;
		}
		if (Blocks.Get(blockIndex) == 0)
		{
			throw new Exception("SetBlockHitTimer must never set the hitTimer > 0 for an empty block!");
		}
		int num = -1;
		for (int j = 0; j < BlockHitTimers.Length; j++)
		{
			if (BlockHitTimers[j].BlockIndex == blockIndex)
			{
				BlockHitTimers[j].BlockIndex = blockIndex;
				BlockHitTimers[j].Timer = hitTimer;
				return;
			}
			if (num == -1 && BlockHitTimers[j].BlockIndex == -1)
			{
				num = j;
			}
		}
		if (num != -1)
		{
			BlockHitTimers[num].BlockIndex = blockIndex;
			BlockHitTimers[num].Timer = hitTimer;
		}
	}
}
