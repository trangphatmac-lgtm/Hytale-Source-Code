#define DEBUG
using System;
using System.Diagnostics;
using System.Threading;

namespace HytaleClient.Utils;

public struct MemoryPoolHelper
{
	private struct TestData
	{
		public int InputInt0;

		public int InputInt1;

		public int ResultInt;

		public int ExpectedResultInt;

		public ulong InputULong0;

		public ulong InputULong1;

		public ulong ResultULong;

		public ulong ExpectedResultULong;
	}

	private ulong[] _usedSlotsBlocks;

	public int MemorySlotCount => _usedSlotsBlocks.Length * 64;

	public MemoryPoolHelper(int memorySlotsCount)
	{
		int num = memorySlotsCount / 64;
		num += ((memorySlotsCount % 64 != 0) ? 1 : 0);
		_usedSlotsBlocks = new ulong[num];
	}

	public void ClearMemorySlots()
	{
		for (int i = 0; i < _usedSlotsBlocks.Length; i++)
		{
			_usedSlotsBlocks[i] = 0uL;
		}
	}

	public void ReleaseMemorySlots(int slot, int slotCount)
	{
		int num = slot / 64;
		int num2 = slot % 64;
		ulong bitfield = _usedSlotsBlocks[num];
		int num3 = num2;
		BitUtils.SwitchOffConsecutiveBits(num2, slotCount, ref bitfield);
		_usedSlotsBlocks[num] = bitfield;
	}

	public void ThreadSafeReleaseMemorySlot(int slot, int slotCount)
	{
		int num = slot / 64;
		int bitId = slot % 64;
		ulong num2;
		ulong bitfield;
		do
		{
			num2 = _usedSlotsBlocks[num];
			bitfield = num2;
			BitUtils.SwitchOffConsecutiveBits(bitId, slotCount, ref bitfield);
		}
		while (num2 != InterlockedCompareExchange(ref _usedSlotsBlocks[num], bitfield, num2));
	}

	public int TakeMemorySlots(int slotCount)
	{
		if (slotCount > 64)
		{
			throw new Exception("Cannot allocate more than 64 contiguous MemorySlot slots.");
		}
		int result = -1;
		if (slotCount > 32)
		{
			for (int i = 0; i < _usedSlotsBlocks.Length; i++)
			{
				ulong num = _usedSlotsBlocks[i];
				if (num == 0)
				{
					result = i * 64;
					num = 4294967295uL;
					BitUtils.SwitchOnConsecutiveBits(32, slotCount - 32, ref num);
					_usedSlotsBlocks[i] = num;
					break;
				}
			}
		}
		else
		{
			for (int j = 0; j < _usedSlotsBlocks.Length; j++)
			{
				ulong bitfield = _usedSlotsBlocks[j];
				int num2 = -1;
				if (bitfield == ulong.MaxValue)
				{
					continue;
				}
				uint bitfield2 = (uint)(bitfield & 0xFFFFFFFFu);
				uint bitfield3 = (uint)((bitfield & 0xFFFFFFFF00000000uL) >> 32);
				uint num3 = BitUtils.CountBitsOn(bitfield2);
				uint num4 = BitUtils.CountBitsOn(bitfield3);
				if (32 - num3 >= slotCount)
				{
					num2 = BitUtils.FindFirstContiguousBitsOff(bitfield2, slotCount);
					if (num2 >= 0)
					{
						num2 = num2;
						result = j * 64 + num2;
						BitUtils.SwitchOnConsecutiveBits(num2, slotCount, ref bitfield);
						_usedSlotsBlocks[j] = bitfield;
						break;
					}
				}
				else if (32 - num4 >= slotCount)
				{
					num2 = BitUtils.FindFirstContiguousBitsOff(bitfield3, slotCount);
					if (num2 >= 0)
					{
						num2 += 32;
						result = j * 64 + num2;
						BitUtils.SwitchOnConsecutiveBits(num2, slotCount, ref bitfield);
						_usedSlotsBlocks[j] = bitfield;
						break;
					}
				}
			}
		}
		return result;
	}

	public int ThreadSafeTakeMemorySlot(int slotCount)
	{
		if (slotCount > 64)
		{
			throw new Exception("Cannot allocate more than 32 contiguous MemorySlot slots.");
		}
		int result = -1;
		if (slotCount > 32)
		{
			bool flag = false;
			do
			{
				for (int i = 0; i < _usedSlotsBlocks.Length; i++)
				{
					ulong num = _usedSlotsBlocks[i];
					if (num == 0)
					{
						ulong num2 = num;
						result = i * 64;
						num2 = 4294967295uL;
						BitUtils.SwitchOnConsecutiveBits(32, slotCount - 32, ref num2);
						flag = num != InterlockedCompareExchange(ref _usedSlotsBlocks[i], num2, num);
						break;
					}
				}
			}
			while (flag);
		}
		else
		{
			bool flag2 = false;
			do
			{
				for (int j = 0; j < _usedSlotsBlocks.Length; j++)
				{
					ulong num3 = _usedSlotsBlocks[j];
					int num4 = -1;
					if (num3 == ulong.MaxValue)
					{
						continue;
					}
					ulong bitfield = num3;
					uint bitfield2 = (uint)(bitfield & 0xFFFFFFFFu);
					uint bitfield3 = (uint)((bitfield & 0xFFFFFFFF00000000uL) >> 32);
					uint num5 = BitUtils.CountBitsOn(bitfield2);
					uint num6 = BitUtils.CountBitsOn(bitfield3);
					if (32 - num5 >= slotCount)
					{
						num4 = BitUtils.FindFirstContiguousBitsOff(bitfield2, slotCount);
						if (num4 >= 0)
						{
							num4 = num4;
							result = j * 64 + num4;
							BitUtils.SwitchOnConsecutiveBits(num4, slotCount, ref bitfield);
							flag2 = num3 != InterlockedCompareExchange(ref _usedSlotsBlocks[j], bitfield, num3);
							break;
						}
					}
					else if (32 - num6 >= slotCount)
					{
						bitfield = num3;
						num4 = BitUtils.FindFirstContiguousBitsOff(bitfield3, slotCount);
						if (num4 >= 0)
						{
							num4 += 32;
							result = j * 64 + num4;
							BitUtils.SwitchOnConsecutiveBits(num4, slotCount, ref bitfield);
							flag2 = num3 != InterlockedCompareExchange(ref _usedSlotsBlocks[j], bitfield, num3);
							break;
						}
					}
				}
			}
			while (flag2);
		}
		return result;
	}

	private unsafe static ulong InterlockedCompareExchange(ref ulong location, ulong value, ulong comparand)
	{
		fixed (ulong* ptr = &location)
		{
			return (ulong)Interlocked.CompareExchange(ref *(long*)ptr, (long)value, (long)comparand);
		}
	}

	public static void UnitTest()
	{
		TestData[] array = new TestData[4];
		MemoryPoolHelper memoryPoolHelper = new MemoryPoolHelper(1024);
		array[0] = new TestData
		{
			InputInt0 = 1,
			ExpectedResultInt = 0
		};
		array[1] = new TestData
		{
			InputInt0 = 1,
			ExpectedResultInt = 0
		};
		array[2] = new TestData
		{
			InputInt0 = 4,
			ExpectedResultInt = 0
		};
		array[3] = new TestData
		{
			InputInt0 = 4,
			ExpectedResultInt = 0
		};
		array[0].ResultInt = memoryPoolHelper.TakeMemorySlots(array[0].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[0].ResultInt, array[0].InputInt0);
		array[1].ResultInt = memoryPoolHelper.TakeMemorySlots(array[1].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[1].ResultInt, array[1].InputInt0);
		array[2].ResultInt = memoryPoolHelper.TakeMemorySlots(array[2].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[2].ResultInt, array[2].InputInt0);
		array[3].ResultInt = memoryPoolHelper.TakeMemorySlots(array[3].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[3].ResultInt, array[3].InputInt0);
		for (int i = 0; i < 4; i++)
		{
			Debug.Assert(array[i].ResultInt == array[i].ExpectedResultInt, $"Error in test data {i}.");
		}
		array[0] = new TestData
		{
			InputInt0 = 1,
			ExpectedResultInt = 0
		};
		array[1] = new TestData
		{
			InputInt0 = 1,
			ExpectedResultInt = 1
		};
		array[2] = new TestData
		{
			InputInt0 = 4,
			ExpectedResultInt = 2
		};
		array[3] = new TestData
		{
			InputInt0 = 4,
			ExpectedResultInt = 6
		};
		array[0].ResultInt = memoryPoolHelper.TakeMemorySlots(array[0].InputInt0);
		array[1].ResultInt = memoryPoolHelper.TakeMemorySlots(array[1].InputInt0);
		array[2].ResultInt = memoryPoolHelper.TakeMemorySlots(array[2].InputInt0);
		array[3].ResultInt = memoryPoolHelper.TakeMemorySlots(array[3].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[0].ResultInt, array[0].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[1].ResultInt, array[1].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[2].ResultInt, array[2].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[3].ResultInt, array[3].InputInt0);
		for (int j = 0; j < 4; j++)
		{
			Debug.Assert(array[j].ResultInt == array[j].ExpectedResultInt, $"Error in test data {j}.");
		}
		array[0] = new TestData
		{
			InputInt0 = 4,
			ExpectedResultInt = 0
		};
		array[1] = new TestData
		{
			InputInt0 = 4,
			ExpectedResultInt = 4
		};
		array[2] = new TestData
		{
			InputInt0 = 1,
			ExpectedResultInt = 0
		};
		array[3] = new TestData
		{
			InputInt0 = 1,
			ExpectedResultInt = 1
		};
		array[0].ResultInt = memoryPoolHelper.TakeMemorySlots(array[0].InputInt0);
		array[1].ResultInt = memoryPoolHelper.TakeMemorySlots(array[1].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[0].ResultInt, array[0].InputInt0);
		array[2].ResultInt = memoryPoolHelper.TakeMemorySlots(array[2].InputInt0);
		array[3].ResultInt = memoryPoolHelper.TakeMemorySlots(array[3].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[1].ResultInt, array[1].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[2].ResultInt, array[2].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[3].ResultInt, array[3].InputInt0);
		for (int k = 0; k < 4; k++)
		{
			Debug.Assert(array[k].ResultInt == array[k].ExpectedResultInt, $"Error in test data {k}.");
		}
		array[0] = new TestData
		{
			InputInt0 = 4,
			ExpectedResultInt = 0
		};
		array[1] = new TestData
		{
			InputInt0 = 4,
			ExpectedResultInt = 4
		};
		array[2] = new TestData
		{
			InputInt0 = 5,
			ExpectedResultInt = 8
		};
		array[3] = new TestData
		{
			InputInt0 = 1,
			ExpectedResultInt = 0
		};
		array[0].ResultInt = memoryPoolHelper.TakeMemorySlots(array[0].InputInt0);
		array[1].ResultInt = memoryPoolHelper.TakeMemorySlots(array[1].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[0].ResultInt, array[0].InputInt0);
		array[2].ResultInt = memoryPoolHelper.TakeMemorySlots(array[2].InputInt0);
		array[3].ResultInt = memoryPoolHelper.TakeMemorySlots(array[3].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[1].ResultInt, array[1].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[2].ResultInt, array[2].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[3].ResultInt, array[3].InputInt0);
		for (int l = 0; l < 4; l++)
		{
			Debug.Assert(array[l].ResultInt == array[l].ExpectedResultInt, $"Error in test data {l}.");
		}
		array[0] = new TestData
		{
			InputInt0 = 16,
			ExpectedResultInt = 0
		};
		array[1] = new TestData
		{
			InputInt0 = 16,
			ExpectedResultInt = 16
		};
		array[2] = new TestData
		{
			InputInt0 = 32,
			ExpectedResultInt = 32
		};
		array[3] = new TestData
		{
			InputInt0 = 1,
			ExpectedResultInt = 0
		};
		array[0].ResultInt = memoryPoolHelper.TakeMemorySlots(array[0].InputInt0);
		array[1].ResultInt = memoryPoolHelper.TakeMemorySlots(array[1].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[0].ResultInt, array[0].InputInt0);
		array[2].ResultInt = memoryPoolHelper.TakeMemorySlots(array[2].InputInt0);
		array[3].ResultInt = memoryPoolHelper.TakeMemorySlots(array[3].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[1].ResultInt, array[1].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[2].ResultInt, array[2].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[3].ResultInt, array[3].InputInt0);
		for (int m = 0; m < 4; m++)
		{
			Debug.Assert(array[m].ResultInt == array[m].ExpectedResultInt, $"Error in test data {m}.");
		}
		array[0] = new TestData
		{
			InputInt0 = 32,
			ExpectedResultInt = 0
		};
		array[1] = new TestData
		{
			InputInt0 = 32,
			ExpectedResultInt = 32
		};
		array[2] = new TestData
		{
			InputInt0 = 32,
			ExpectedResultInt = 64
		};
		array[3] = new TestData
		{
			InputInt0 = 1,
			ExpectedResultInt = 32
		};
		array[0].ResultInt = memoryPoolHelper.TakeMemorySlots(array[0].InputInt0);
		array[1].ResultInt = memoryPoolHelper.TakeMemorySlots(array[1].InputInt0);
		array[2].ResultInt = memoryPoolHelper.TakeMemorySlots(array[2].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[1].ResultInt, array[1].InputInt0);
		array[3].ResultInt = memoryPoolHelper.TakeMemorySlots(array[3].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[0].ResultInt, array[0].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[2].ResultInt, array[2].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[3].ResultInt, array[3].InputInt0);
		for (int n = 0; n < 4; n++)
		{
			Debug.Assert(array[n].ResultInt == array[n].ExpectedResultInt, $"Error in test data {n}.");
		}
		array[0] = new TestData
		{
			InputInt0 = 32,
			ExpectedResultInt = 0
		};
		array[1] = new TestData
		{
			InputInt0 = 33,
			ExpectedResultInt = 64
		};
		array[2] = new TestData
		{
			InputInt0 = 64,
			ExpectedResultInt = 128
		};
		array[3] = new TestData
		{
			InputInt0 = 4,
			ExpectedResultInt = 32
		};
		array[0].ResultInt = memoryPoolHelper.TakeMemorySlots(array[0].InputInt0);
		array[1].ResultInt = memoryPoolHelper.TakeMemorySlots(array[1].InputInt0);
		array[2].ResultInt = memoryPoolHelper.TakeMemorySlots(array[2].InputInt0);
		array[3].ResultInt = memoryPoolHelper.TakeMemorySlots(array[3].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[0].ResultInt, array[0].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[1].ResultInt, array[1].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[2].ResultInt, array[2].InputInt0);
		memoryPoolHelper.ReleaseMemorySlots(array[3].ResultInt, array[3].InputInt0);
		for (int num = 0; num < 4; num++)
		{
			Debug.Assert(array[num].ResultInt == array[num].ExpectedResultInt, $"Error in test data {num}.");
		}
		array[0] = new TestData
		{
			InputInt0 = 32,
			ExpectedResultInt = 0
		};
		array[1] = new TestData
		{
			InputInt0 = 33,
			ExpectedResultInt = 0
		};
		array[2] = new TestData
		{
			InputInt0 = 64,
			ExpectedResultInt = 0
		};
		array[3] = new TestData
		{
			InputInt0 = 65,
			ExpectedResultInt = 1
		};
		for (int num2 = 0; num2 < 4; num2++)
		{
			try
			{
				array[num2].ResultInt = 0;
				int num3 = memoryPoolHelper.TakeMemorySlots(array[num2].InputInt0);
			}
			catch (Exception)
			{
				array[num2].ResultInt = 1;
			}
		}
		for (int num4 = 0; num4 < 4; num4++)
		{
			Debug.Assert(array[num4].ResultInt == array[num4].ExpectedResultInt, $"Error in test data {num4}.");
		}
	}
}
