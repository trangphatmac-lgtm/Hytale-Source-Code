using System;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.AmbienceFX;

public class BlockEnvironmentStats
{
	public struct BlockStats
	{
		public float Percent;

		public int LowestAltitude;

		public int HighestAltitude;
	}

	private const int StatsArrayDefaultSize = 20;

	private const int StatsArrayGrowSize = 10;

	private const int BlocksArrayDefaultSize = 200;

	private const int BlocksArrayGrowSize = 50;

	private readonly float _inverseTotalAnalyzedBlocks;

	public int TotalStats = 0;

	public int[] BlockSoundSetIndices;

	private int[] _totalBlocks;

	private Vector3[][] _blocks;

	public BlockStats[] Stats;

	public BlockEnvironmentStats(int totalBlocks)
	{
		BlockSoundSetIndices = new int[20];
		Stats = new BlockStats[20];
		_totalBlocks = new int[20];
		_blocks = new Vector3[20][];
		for (int i = 0; i < 20; i++)
		{
			_blocks[i] = new Vector3[200];
		}
		_inverseTotalAnalyzedBlocks = 1f / (float)totalBlocks * 100f;
	}

	public void Initialize(int blockSoundSetIndex, int x, int y, int z)
	{
		if (TotalStats >= BlockSoundSetIndices.Length)
		{
			Array.Resize(ref BlockSoundSetIndices, BlockSoundSetIndices.Length + 10);
			Array.Resize(ref Stats, Stats.Length + 10);
			Array.Resize(ref _totalBlocks, _totalBlocks.Length + 10);
			Array.Resize(ref _blocks, _blocks.Length + 10);
			for (int i = TotalStats; i < _blocks.Length; i++)
			{
				_blocks[i] = new Vector3[200];
			}
		}
		BlockSoundSetIndices[TotalStats] = blockSoundSetIndex;
		_blocks[TotalStats][0] = new Vector3(x, y, z);
		ref BlockStats reference = ref Stats[TotalStats];
		reference.Percent = _inverseTotalAnalyzedBlocks;
		reference.LowestAltitude = y;
		reference.HighestAltitude = y;
		_totalBlocks[TotalStats] = 1;
		TotalStats++;
	}

	public void Add(int index, int x, int y, int z)
	{
		ref Vector3[] reference = ref _blocks[index];
		ref int reference2 = ref _totalBlocks[index];
		ref BlockStats reference3 = ref Stats[index];
		if (reference2 >= reference.Length)
		{
			Array.Resize(ref reference, reference.Length + 50);
		}
		reference[reference2] = new Vector3(x, y, z);
		reference2++;
		reference3.Percent = (float)reference2 * _inverseTotalAnalyzedBlocks;
		if (y < reference3.LowestAltitude)
		{
			reference3.LowestAltitude = y;
		}
		if (y > reference3.HighestAltitude)
		{
			reference3.HighestAltitude = y;
		}
	}

	public Vector3 GetClosestBlock(int index, Vector3 position)
	{
		ref Vector3[] reference = ref _blocks[index];
		Vector3 vector = reference[0];
		float num = Vector3.DistanceSquared(position, vector);
		ref int reference2 = ref _totalBlocks[index];
		for (int i = 1; i < reference2; i++)
		{
			ref Vector3 reference3 = ref reference[i];
			float num2 = Vector3.DistanceSquared(position, reference3);
			if (num2 < num)
			{
				num = num2;
				vector = reference3;
			}
		}
		return vector;
	}

	public string GetDebugData(int index, string blockSoundSetId)
	{
		ref BlockStats reference = ref Stats[index];
		int num = _totalBlocks[index];
		double num2 = System.Math.Round(reference.Percent * 10f) / 10.0;
		return $"[ {blockSoundSetId} = {num2}% - Count={num} - Ymin={reference.LowestAltitude} / Ymax={reference.HighestAltitude} ]";
	}
}
