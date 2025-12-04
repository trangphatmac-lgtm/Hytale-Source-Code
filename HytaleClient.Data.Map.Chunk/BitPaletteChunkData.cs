#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Map.Chunk;

public class BitPaletteChunkData : IChunkData
{
	private Dictionary<int, short> _externalToInternal = new Dictionary<int, short>(16);

	private Dictionary<short, ushort> _typeCounts = new Dictionary<short, ushort>(16);

	private uint _bitMask;

	private int _promotionIndex;

	private int _demoteSize;

	private int _bitsPerBlock;

	private int[] _internalToExternal;

	private uint[] _data;

	private const int WordSize = 32;

	private static readonly byte[] promotionTable = new byte[14]
	{
		0, 4, 5, 6, 7, 8, 9, 10, 11, 12,
		13, 14, 15, 16
	};

	public BitPaletteChunkData(PaletteType paletteType)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Invalid comparison between Unknown and I4
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		Debug.Assert((int)paletteType > 0, "Attempted to instantiate an empty BitPalette. Use EmptyPaletteChunkData.Instance instead");
		_promotionIndex = PaletteTypeToPromotionIndex(paletteType);
		Resize(promotionTable[paletteType], retainData: false);
		_externalToInternal[0] = 0;
		_internalToExternal[0] = 0;
		_typeCounts[0] = 32768;
	}

	public BitPaletteChunkData(BinaryReader reader, int maxValidBlockTypeId, PaletteType paletteType)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		Deserialize(reader, maxValidBlockTypeId, paletteType);
	}

	private static int PaletteTypeToPromotionIndex(PaletteType type)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected I4, but got Unknown
		return (type - 1) switch
		{
			0 => 1, 
			1 => 5, 
			2 => 13, 
			_ => 1, 
		};
	}

	private void Resize(int bitsPerBlock, bool retainData = true)
	{
		Debug.Assert(bitsPerBlock > 0 && bitsPerBlock <= 16);
		int num = 1 << bitsPerBlock;
		_bitMask = (uint)(num - 1);
		uint[] array = new uint[32768 * bitsPerBlock / 32];
		int[] array2 = new int[num];
		if (retainData)
		{
			if (array2.Length > _internalToExternal.Length)
			{
				Buffer.BlockCopy(_internalToExternal, 0, array2, 0, _internalToExternal.Length * 4);
				for (int i = 0; i < 32768; i++)
				{
					int internalId = GetInternalId(i, _bitsPerBlock, _data);
					SetInternalId(i, internalId, bitsPerBlock, array);
				}
			}
			else
			{
				Dictionary<int, short> dictionary = new Dictionary<int, short>(16);
				Dictionary<short, ushort> dictionary2 = new Dictionary<short, ushort>(16);
				Dictionary<short, short> dictionary3 = new Dictionary<short, short>();
				short num2 = 0;
				foreach (KeyValuePair<int, short> item in _externalToInternal)
				{
					int key = item.Key;
					short value = item.Value;
					dictionary[key] = num2;
					dictionary2[num2] = _typeCounts[value];
					array2[num2] = _internalToExternal[value];
					dictionary3[value] = num2;
					num2++;
				}
				_externalToInternal = dictionary;
				_typeCounts = dictionary2;
				for (int j = 0; j < 32768; j++)
				{
					num2 = GetInternalId(j, _bitsPerBlock, _data);
					SetInternalId(j, dictionary3[num2], bitsPerBlock, array);
				}
			}
		}
		else
		{
			_externalToInternal.Clear();
			_typeCounts.Clear();
		}
		_data = array;
		_internalToExternal = array2;
		int num3 = _promotionIndex - 1;
		if (num3 <= 0)
		{
			_demoteSize = 0;
		}
		else
		{
			_demoteSize = (1 << (int)promotionTable[num3]) - 2;
		}
		_bitsPerBlock = bitsPerBlock;
	}

	private static void CopyData(uint[] src, int srcBits, uint[] dest, int destBits)
	{
	}

	public override int Get(int blockIdx)
	{
		int internalId = GetInternalId(blockIdx);
		return _internalToExternal[internalId];
	}

	private short GetInternalId(int blockIdx)
	{
		int num = blockIdx * _bitsPerBlock;
		int num2 = num / 32;
		uint num3 = _data[num2];
		int num4 = num % 32;
		int num5 = (num + _bitsPerBlock - 1) / 32;
		if (num2 == num5)
		{
			return (short)((num3 >> num4) & _bitMask);
		}
		int num6 = 32 - num4;
		return (short)(((num3 >> num4) | (_data[num5] << num6)) & _bitMask);
	}

	private static short GetInternalId(int blockIdx, int bitsPerBlock, uint[] src)
	{
		uint num = (uint)((1 << bitsPerBlock) - 1);
		int num2 = blockIdx * bitsPerBlock;
		int num3 = num2 / 32;
		uint num4 = src[num3];
		int num5 = num2 % 32;
		int num6 = (num2 + bitsPerBlock - 1) / 32;
		if (num3 == num6)
		{
			return (short)((num4 >> num5) & num);
		}
		int num7 = 32 - num5;
		return (short)(((num4 >> num5) | (src[num6] << num7)) & num);
	}

	private void SetInternalId(int blockIdx, int blockId)
	{
		int num = blockIdx * _bitsPerBlock;
		int num2 = num / 32;
		uint num3 = _data[num2];
		int num4 = num % 32;
		uint num5 = _bitMask << num4;
		num3 &= ~num5;
		uint num6 = (uint)(blockId << num4);
		num3 |= num6;
		_data[num2] = num3;
		int num7 = (num + _bitsPerBlock - 1) / 32;
		if (num2 != num7)
		{
			int num8 = 32 - num4;
			int num9 = _bitsPerBlock - num8;
			_data[num7] = (_data[num7] >> num9 << num9) | (uint)(blockId >> num8);
		}
	}

	private static void SetInternalId(int blockIdx, int blockId, int bitsPerBlock, uint[] dst)
	{
		uint num = (uint)((1 << bitsPerBlock) - 1);
		int num2 = blockIdx * bitsPerBlock;
		int num3 = num2 / 32;
		uint num4 = dst[num3];
		int num5 = num2 % 32;
		uint num6 = num << num5;
		num4 &= ~num6;
		uint num7 = (uint)(blockId << num5);
		num4 |= num7;
		dst[num3] = num4;
		int num8 = (num2 + bitsPerBlock - 1) / 32;
		if (num3 != num8)
		{
			int num9 = 32 - num5;
			int num10 = bitsPerBlock - num9;
			dst[num8] = (dst[num8] >> num10 << num10) | (uint)(blockId >> num9);
		}
	}

	public override BlockSetResult Set(int blockIdx, int blockId)
	{
		short internalId = GetInternalId(blockIdx);
		if (_externalToInternal.TryGetValue(blockId, out var value))
		{
			if (value != internalId)
			{
				bool flag = DecrementBlockCount(internalId);
				IncrementBlockCount(value);
				SetInternalId(blockIdx, value);
				if (flag)
				{
					return BlockSetResult.BLOCK_ADDED_OR_REMOVED;
				}
				return BlockSetResult.BLOCK_CHANGED;
			}
			return BlockSetResult.BLOCK_UNCHANGED;
		}
		int num = NextInternalId(internalId);
		if (!IsValidInternalId(num))
		{
			return BlockSetResult.REQUIRES_PROMOTE;
		}
		DecrementBlockCount(internalId);
		short num2 = (short)num;
		CreateBlockId(num2, blockId);
		SetInternalId(blockIdx, num2);
		return BlockSetResult.BLOCK_ADDED_OR_REMOVED;
	}

	public override bool Contains(int blockId)
	{
		return _externalToInternal.ContainsKey(blockId);
	}

	public override int BlockCount()
	{
		return _typeCounts.Count;
	}

	public override int Count(int blockId)
	{
		if (_externalToInternal.TryGetValue(blockId, out var value))
		{
			return _typeCounts[value];
		}
		return 0;
	}

	public override HashSet<int> Blocks()
	{
		return new HashSet<int>(_externalToInternal.Keys);
	}

	public override Dictionary<int, ushort> BlockCounts()
	{
		Dictionary<int, ushort> dictionary = new Dictionary<int, ushort>();
		foreach (KeyValuePair<short, ushort> typeCount in _typeCounts)
		{
			short key = typeCount.Key;
			ushort value = typeCount.Value;
			int key2 = _internalToExternal[key];
			dictionary[key2] = value;
		}
		return dictionary;
	}

	public override bool ShouldDemote()
	{
		if (_demoteSize == 0)
		{
			if (!_externalToInternal.ContainsKey(0))
			{
				return false;
			}
			return BlockCount() == 1;
		}
		return BlockCount() <= _demoteSize;
	}

	public override IChunkData Demote()
	{
		Debug.Assert(_promotionIndex - 1 >= 0, "Unable to demote: Invalid palette size.");
		_promotionIndex--;
		if (_promotionIndex == 0)
		{
			return EmptyPaletteChunkData.Instance;
		}
		Resize(promotionTable[_promotionIndex]);
		return this;
	}

	public override IChunkData Promote()
	{
		Debug.Assert(_promotionIndex + 1 < promotionTable.Length, "Unable to promote: Invalid palette size.");
		_promotionIndex++;
		Resize(promotionTable[_promotionIndex]);
		return this;
	}

	private void CreateBlockId(short internalId, int blockId)
	{
		_internalToExternal[internalId] = blockId;
		_externalToInternal[blockId] = internalId;
		_typeCounts[internalId] = 1;
	}

	private bool DecrementBlockCount(short internalId)
	{
		ushort num = _typeCounts[internalId];
		if (num == 1)
		{
			_typeCounts.Remove(internalId);
			_externalToInternal.Remove(_internalToExternal[internalId]);
			_internalToExternal[internalId] = 0;
			return true;
		}
		num = (_typeCounts[internalId] = (ushort)(num - 1));
		return false;
	}

	private void IncrementBlockCount(short internalId)
	{
		ushort num = _typeCounts[internalId];
		_typeCounts[internalId] = (ushort)(num + 1);
	}

	private int NextInternalId(short oldInternalId)
	{
		if (_typeCounts[oldInternalId] == 1)
		{
			return oldInternalId;
		}
		if (!_externalToInternal.TryGetValue(0, out var value))
		{
			value = -1;
		}
		int i;
		for (i = 0; i < _internalToExternal.Length && (_internalToExternal[i] > 0 || i == value); i++)
		{
		}
		return i;
	}

	private bool IsValidInternalId(int internalId)
	{
		return (internalId & _bitMask) == internalId;
	}

	public override void Deserialize(BinaryReader reader, int maxValidBlockTypeId, PaletteType paletteType)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected I4, but got Unknown
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		int bitsPerBlock = 0;
		bool flag = false;
		int num = 1;
		int num2 = 32768;
		switch (paletteType - 1)
		{
		case 0:
			bitsPerBlock = 4;
			flag = true;
			num2 = 16384;
			break;
		case 1:
			bitsPerBlock = 8;
			flag = true;
			break;
		case 2:
			bitsPerBlock = 16;
			num = 2;
			break;
		}
		_promotionIndex = PaletteTypeToPromotionIndex(paletteType);
		Resize(bitsPerBlock, retainData: false);
		ushort num3 = reader.ReadUInt16();
		for (int i = 0; i < num3; i++)
		{
			short num4 = (flag ? reader.ReadByte() : reader.ReadInt16());
			int num5 = reader.ReadInt32();
			ushort value = reader.ReadUInt16();
			if (num5 > maxValidBlockTypeId)
			{
				num5 = 1;
			}
			_internalToExternal[num4] = num5;
			_externalToInternal[num5] = num4;
			_typeCounts[num4] = value;
		}
		MemoryStream memoryStream = (MemoryStream)reader.BaseStream;
		Buffer.BlockCopy(memoryStream.GetBuffer(), (int)memoryStream.Position, _data, 0, num2 * num);
		memoryStream.Position += num2 * num;
	}
}
