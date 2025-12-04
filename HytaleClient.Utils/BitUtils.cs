#define DEBUG
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HytaleClient.Utils;

public static class BitUtils
{
	private struct TestData
	{
		public byte InputByte;

		public int InputInt0;

		public int InputInt1;

		public uint InputUInt0;

		public ulong InputULong0;

		public byte ResultByte;

		public int ResultInt;

		public uint ResultUInt;

		public ulong ResultULong;

		public byte ExpectedResultByte;

		public int ExpectedResultInt;

		public uint ExpectedResultUInt;

		public ulong ExpectedResultULong;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FindFirstContiguousBitsOff(uint bitfield, int requestedConsecutiveBits)
	{
		int result = -1;
		uint num = 0u;
		int num2 = 0;
		uint num3 = bitfield;
		for (int i = 0; i < 32; i++)
		{
			if (num >= requestedConsecutiveBits)
			{
				break;
			}
			if ((num3 & 1) == 1)
			{
				num = 0u;
				num2 = i + 1;
			}
			else
			{
				num++;
			}
			num3 >>= 1;
		}
		if (num > requestedConsecutiveBits)
		{
			throw new Exception("This should never happen : 'consecutiveBits > requestedConsecutiveBits'.");
		}
		if (num == requestedConsecutiveBits)
		{
			result = num2;
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SwitchOnConsecutiveBits(int bitId, int consecutiveBits, ref ulong bitfield)
	{
		for (int i = bitId; i < bitId + consecutiveBits; i++)
		{
			bitfield |= (ulong)(1L << i);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SwitchOffConsecutiveBits(int bitId, int consecutiveBits, ref ulong bitfield)
	{
		for (int i = bitId; i < bitId + consecutiveBits; i++)
		{
			bitfield &= (ulong)(~(1L << i));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint FindFirstBitOff(uint bitfield)
	{
		bitfield = ~bitfield;
		return CountBitsOn((uint)((bitfield & (0L - (long)bitfield)) - 1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint FindFirstBitOn(uint bitfield)
	{
		return CountBitsOn((uint)((bitfield & (0L - (long)bitfield)) - 1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SwitchOnBit(int bitId, ref uint bitfield)
	{
		uint num = (uint)(1 << bitId % 32);
		bitfield |= num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SwitchOnBit(int bitId, ref byte bitfield)
	{
		bitfield |= (byte)(1 << bitId);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SwitchOffBit(int bitId, ref uint bitfield)
	{
		uint num = (uint)(1 << bitId % 32);
		bitfield &= ~num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SwitchOffBit(int bitId, ref byte bitfield)
	{
		bitfield &= (byte)(~(1 << bitId));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsBitOn(int bitId, uint bitfield)
	{
		uint num = (uint)(1 << bitId % 32);
		return (bitfield & num) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsBitOn(int bitId, byte bitfield)
	{
		return (bitfield & (1 << bitId)) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint CountBitsOn(uint bitfield)
	{
		uint num = bitfield;
		num -= (num >> 1) & 0x55555555;
		num = (num & 0x33333333) + ((num >> 2) & 0x33333333);
		return ((num + (num >> 4)) & 0xF0F0F0F) * 16843009 >> 24;
	}

	public static void UnitTest()
	{
		TestData[] array = new TestData[4]
		{
			new TestData
			{
				InputInt0 = 0,
				ExpectedResultInt = 0
			},
			new TestData
			{
				InputInt0 = 1,
				ExpectedResultInt = 1
			},
			new TestData
			{
				InputInt0 = 31,
				ExpectedResultInt = 5
			},
			new TestData
			{
				InputInt0 = 95,
				ExpectedResultInt = 6
			}
		};
		array[0].ResultInt = (int)CountBitsOn((uint)array[0].InputInt0);
		array[1].ResultInt = (int)CountBitsOn((uint)array[1].InputInt0);
		array[2].ResultInt = (int)CountBitsOn((uint)array[2].InputInt0);
		array[3].ResultInt = (int)CountBitsOn((uint)array[3].InputInt0);
		for (int i = 0; i < 4; i++)
		{
			Debug.Assert(array[i].ResultInt == array[i].ExpectedResultInt, $"Error in test data {i}.");
		}
		array[0] = new TestData
		{
			InputUInt0 = 0u,
			ExpectedResultInt = 0
		};
		array[1] = new TestData
		{
			InputUInt0 = 1u,
			ExpectedResultInt = 1
		};
		array[2] = new TestData
		{
			InputUInt0 = 95u,
			ExpectedResultInt = 5
		};
		array[3] = new TestData
		{
			InputUInt0 = uint.MaxValue,
			ExpectedResultInt = 32
		};
		array[0].ResultInt = (int)FindFirstBitOff(array[0].InputUInt0);
		array[1].ResultInt = (int)FindFirstBitOff(array[1].InputUInt0);
		array[2].ResultInt = (int)FindFirstBitOff(array[2].InputUInt0);
		array[3].ResultInt = (int)FindFirstBitOff(array[3].InputUInt0);
		for (int j = 0; j < 4; j++)
		{
			Debug.Assert(array[j].ResultInt == array[j].ExpectedResultInt, $"Error in test data {j}.");
		}
		array[0] = new TestData
		{
			InputUInt0 = 0u,
			ExpectedResultInt = 32
		};
		array[1] = new TestData
		{
			InputUInt0 = 1u,
			ExpectedResultInt = 0
		};
		array[2] = new TestData
		{
			InputUInt0 = 80u,
			ExpectedResultInt = 4
		};
		array[3] = new TestData
		{
			InputUInt0 = 16773376u,
			ExpectedResultInt = 8
		};
		array[0].ResultInt = (int)FindFirstBitOn(array[0].InputUInt0);
		array[1].ResultInt = (int)FindFirstBitOn(array[1].InputUInt0);
		array[2].ResultInt = (int)FindFirstBitOn(array[2].InputUInt0);
		array[3].ResultInt = (int)FindFirstBitOn(array[3].InputUInt0);
		for (int k = 0; k < 4; k++)
		{
			Debug.Assert(array[k].ResultInt == array[k].ExpectedResultInt, $"Error in test data {k}.");
		}
		array[0] = new TestData
		{
			InputInt0 = 0,
			InputUInt0 = 0u,
			ExpectedResultInt = 0
		};
		array[1] = new TestData
		{
			InputInt0 = 0,
			InputUInt0 = 1u,
			ExpectedResultInt = 1
		};
		array[2] = new TestData
		{
			InputInt0 = 5,
			InputUInt0 = 95u,
			ExpectedResultInt = 0
		};
		array[3] = new TestData
		{
			InputInt0 = 1,
			InputUInt0 = uint.MaxValue,
			ExpectedResultInt = 1
		};
		array[0].ResultInt = (IsBitOn(array[0].InputInt0, array[0].InputUInt0) ? 1 : 0);
		array[1].ResultInt = (IsBitOn(array[1].InputInt0, array[1].InputUInt0) ? 1 : 0);
		array[2].ResultInt = (IsBitOn(array[2].InputInt0, array[2].InputUInt0) ? 1 : 0);
		array[3].ResultInt = (IsBitOn(array[3].InputInt0, array[3].InputUInt0) ? 1 : 0);
		for (int l = 0; l < 4; l++)
		{
			Debug.Assert(array[l].ResultInt == array[l].ExpectedResultInt, $"Error in test data {l}.");
		}
		array[0] = new TestData
		{
			InputInt0 = 0,
			InputByte = 14,
			ExpectedResultInt = 0
		};
		array[1] = new TestData
		{
			InputInt0 = 0,
			InputByte = 15,
			ExpectedResultInt = 1
		};
		array[2] = new TestData
		{
			InputInt0 = 2,
			InputByte = 11,
			ExpectedResultInt = 0
		};
		array[3] = new TestData
		{
			InputInt0 = 2,
			InputByte = 15,
			ExpectedResultInt = 1
		};
		array[0].ResultInt = (IsBitOn(array[0].InputInt0, array[0].InputByte) ? 1 : 0);
		array[1].ResultInt = (IsBitOn(array[1].InputInt0, array[1].InputByte) ? 1 : 0);
		array[2].ResultInt = (IsBitOn(array[2].InputInt0, array[2].InputByte) ? 1 : 0);
		array[3].ResultInt = (IsBitOn(array[3].InputInt0, array[3].InputByte) ? 1 : 0);
		for (int m = 0; m < 4; m++)
		{
			Debug.Assert(array[m].ResultInt == array[m].ExpectedResultInt, $"Error in test data {m}.");
		}
		array[0] = new TestData
		{
			InputInt0 = 0,
			InputUInt0 = 0u,
			ExpectedResultUInt = 1u
		};
		array[1] = new TestData
		{
			InputInt0 = 1,
			InputUInt0 = 1u,
			ExpectedResultUInt = 3u
		};
		array[2] = new TestData
		{
			InputInt0 = 8,
			InputUInt0 = 95u,
			ExpectedResultUInt = 351u
		};
		array[3] = new TestData
		{
			InputInt0 = 1,
			InputUInt0 = uint.MaxValue,
			ExpectedResultUInt = uint.MaxValue
		};
		uint bitfield = array[0].InputUInt0;
		SwitchOnBit(array[0].InputInt0, ref bitfield);
		array[0].ResultUInt = bitfield;
		bitfield = array[1].InputUInt0;
		SwitchOnBit(array[1].InputInt0, ref bitfield);
		array[1].ResultUInt = bitfield;
		bitfield = array[2].InputUInt0;
		SwitchOnBit(array[2].InputInt0, ref bitfield);
		array[2].ResultUInt = bitfield;
		bitfield = array[3].InputUInt0;
		SwitchOnBit(array[3].InputInt0, ref bitfield);
		array[3].ResultUInt = bitfield;
		for (int n = 0; n < 4; n++)
		{
			Debug.Assert(array[n].ResultUInt == array[n].ExpectedResultUInt, $"Error in test data {n}.");
		}
		array[0] = new TestData
		{
			InputInt0 = 0,
			InputByte = 0,
			ExpectedResultByte = 1
		};
		array[1] = new TestData
		{
			InputInt0 = 1,
			InputByte = 2,
			ExpectedResultByte = 2
		};
		array[2] = new TestData
		{
			InputInt0 = 2,
			InputByte = 0,
			ExpectedResultByte = 4
		};
		array[3] = new TestData
		{
			InputInt0 = 3,
			InputByte = 8,
			ExpectedResultByte = 8
		};
		byte bitfield2 = array[0].InputByte;
		SwitchOnBit(array[0].InputInt0, ref bitfield2);
		array[0].ResultByte = bitfield2;
		bitfield2 = array[1].InputByte;
		SwitchOnBit(array[1].InputInt0, ref bitfield2);
		array[1].ResultByte = bitfield2;
		bitfield2 = array[2].InputByte;
		SwitchOnBit(array[2].InputInt0, ref bitfield2);
		array[2].ResultByte = bitfield2;
		bitfield2 = array[3].InputByte;
		SwitchOnBit(array[3].InputInt0, ref bitfield2);
		array[3].ResultByte = bitfield2;
		for (int num = 0; num < 4; num++)
		{
			Debug.Assert(array[num].ResultByte == array[num].ExpectedResultByte, $"Error in test data {num}.");
		}
		array[0] = new TestData
		{
			InputInt0 = 0,
			InputUInt0 = 1u,
			ExpectedResultUInt = 0u
		};
		array[1] = new TestData
		{
			InputInt0 = 1,
			InputUInt0 = 3u,
			ExpectedResultUInt = 1u
		};
		array[2] = new TestData
		{
			InputInt0 = 8,
			InputUInt0 = 351u,
			ExpectedResultUInt = 95u
		};
		array[3] = new TestData
		{
			InputInt0 = 1,
			InputUInt0 = 0u,
			ExpectedResultUInt = 0u
		};
		uint bitfield3 = array[0].InputUInt0;
		SwitchOffBit(array[0].InputInt0, ref bitfield3);
		array[0].ResultUInt = bitfield3;
		bitfield3 = array[1].InputUInt0;
		SwitchOffBit(array[1].InputInt0, ref bitfield3);
		array[1].ResultUInt = bitfield3;
		bitfield3 = array[2].InputUInt0;
		SwitchOffBit(array[2].InputInt0, ref bitfield3);
		array[2].ResultUInt = bitfield3;
		bitfield3 = array[3].InputUInt0;
		SwitchOffBit(array[3].InputInt0, ref bitfield3);
		array[3].ResultUInt = bitfield3;
		for (int num2 = 0; num2 < 4; num2++)
		{
			Debug.Assert(array[num2].ResultUInt == array[num2].ExpectedResultUInt, $"Error in test data {num2}.");
		}
		array[0] = new TestData
		{
			InputInt0 = 0,
			InputByte = 0,
			ExpectedResultByte = 0
		};
		array[1] = new TestData
		{
			InputInt0 = 1,
			InputByte = 2,
			ExpectedResultByte = 0
		};
		array[2] = new TestData
		{
			InputInt0 = 2,
			InputByte = 11,
			ExpectedResultByte = 11
		};
		array[3] = new TestData
		{
			InputInt0 = 3,
			InputByte = 15,
			ExpectedResultByte = 7
		};
		byte bitfield4 = array[0].InputByte;
		SwitchOffBit(array[0].InputInt0, ref bitfield4);
		array[0].ResultByte = bitfield4;
		bitfield4 = array[1].InputByte;
		SwitchOffBit(array[1].InputInt0, ref bitfield4);
		array[1].ResultByte = bitfield4;
		bitfield4 = array[2].InputByte;
		SwitchOffBit(array[2].InputInt0, ref bitfield4);
		array[2].ResultByte = bitfield4;
		bitfield4 = array[3].InputByte;
		SwitchOffBit(array[3].InputInt0, ref bitfield4);
		array[3].ResultByte = bitfield4;
		for (int num3 = 0; num3 < 4; num3++)
		{
			Debug.Assert(array[num3].ResultByte == array[num3].ExpectedResultByte, $"Error in test data {num3}.");
		}
		array[0] = new TestData
		{
			InputInt0 = 0,
			InputInt1 = 1,
			InputULong0 = 0uL,
			ExpectedResultULong = 1uL
		};
		array[1] = new TestData
		{
			InputInt0 = 2,
			InputInt1 = 5,
			InputULong0 = 0uL,
			ExpectedResultULong = 124uL
		};
		array[2] = new TestData
		{
			InputInt0 = 2,
			InputInt1 = 20,
			InputULong0 = 0uL,
			ExpectedResultULong = 4194300uL
		};
		array[3] = new TestData
		{
			InputInt0 = 4,
			InputInt1 = 4,
			InputULong0 = 17293822569102704640uL,
			ExpectedResultULong = 17293822569102704880uL
		};
		ulong bitfield5 = array[0].InputULong0;
		SwitchOnConsecutiveBits(array[0].InputInt0, array[0].InputInt1, ref bitfield5);
		array[0].ResultULong = bitfield5;
		bitfield5 = array[1].InputULong0;
		SwitchOnConsecutiveBits(array[1].InputInt0, array[1].InputInt1, ref bitfield5);
		array[1].ResultULong = bitfield5;
		bitfield5 = array[2].InputULong0;
		SwitchOnConsecutiveBits(array[2].InputInt0, array[2].InputInt1, ref bitfield5);
		array[2].ResultULong = bitfield5;
		bitfield5 = array[3].InputULong0;
		SwitchOnConsecutiveBits(array[3].InputInt0, array[3].InputInt1, ref bitfield5);
		array[3].ResultULong = bitfield5;
		for (int num4 = 0; num4 < 4; num4++)
		{
			Debug.Assert(array[num4].ResultULong == array[num4].ExpectedResultULong, $"Error in test data {num4}.");
		}
		array[0] = new TestData
		{
			InputInt0 = 0,
			InputInt1 = 1,
			InputULong0 = 1uL,
			ExpectedResultULong = 0uL
		};
		array[1] = new TestData
		{
			InputInt0 = 2,
			InputInt1 = 5,
			InputULong0 = 124uL,
			ExpectedResultULong = 0uL
		};
		array[2] = new TestData
		{
			InputInt0 = 2,
			InputInt1 = 20,
			InputULong0 = 4194300uL,
			ExpectedResultULong = 0uL
		};
		array[3] = new TestData
		{
			InputInt0 = 4,
			InputInt1 = 4,
			InputULong0 = 17293822569102704880uL,
			ExpectedResultULong = 17293822569102704640uL
		};
		ulong bitfield6 = array[0].InputULong0;
		SwitchOffConsecutiveBits(array[0].InputInt0, array[0].InputInt1, ref bitfield6);
		array[0].ResultULong = bitfield6;
		bitfield6 = array[1].InputULong0;
		SwitchOffConsecutiveBits(array[1].InputInt0, array[1].InputInt1, ref bitfield6);
		array[1].ResultULong = bitfield6;
		bitfield6 = array[2].InputULong0;
		SwitchOffConsecutiveBits(array[2].InputInt0, array[2].InputInt1, ref bitfield6);
		array[2].ResultULong = bitfield6;
		bitfield6 = array[3].InputULong0;
		SwitchOffConsecutiveBits(array[3].InputInt0, array[3].InputInt1, ref bitfield6);
		array[3].ResultULong = bitfield6;
		for (int num5 = 0; num5 < 4; num5++)
		{
			Debug.Assert(array[num5].ResultULong == array[num5].ExpectedResultULong, $"Error in test data {num5}.");
		}
		array[0] = new TestData
		{
			InputInt0 = 0,
			InputInt1 = 6,
			ExpectedResultInt = 0
		};
		array[1] = new TestData
		{
			InputInt0 = 1,
			InputInt1 = 6,
			ExpectedResultInt = 1
		};
		array[2] = new TestData
		{
			InputInt0 = 8,
			InputInt1 = 8,
			ExpectedResultInt = 4
		};
		array[3] = new TestData
		{
			InputInt0 = 11184810,
			InputInt1 = 8,
			ExpectedResultInt = 24
		};
		array[0].ResultInt = FindFirstContiguousBitsOff((uint)array[0].InputInt0, array[0].InputInt1);
		array[1].ResultInt = FindFirstContiguousBitsOff((uint)array[1].InputInt0, array[1].InputInt1);
		array[2].ResultInt = FindFirstContiguousBitsOff((uint)array[2].InputInt0, array[2].InputInt1);
		array[3].ResultInt = FindFirstContiguousBitsOff((uint)array[3].InputInt0, array[3].InputInt1);
		for (int num6 = 0; num6 < 4; num6++)
		{
			Debug.Assert(array[num6].ResultInt == array[num6].ExpectedResultInt, $"Error in test data {num6}.");
		}
	}
}
