#define DEBUG
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Graphics;

internal class FXMemoryPool<T> where T : IFXDataStorage, new()
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public readonly int SlotSize = 32;

	private int _itemMaxCount;

	private int _slotCount;

	private T _storage;

	private MemoryPoolHelper _memoryPoolHelper;

	public ref T Storage => ref _storage;

	public int SlotCount => _slotCount;

	public int ItemMaxCount => _itemMaxCount;

	public void Initialize(int requestedMaxCount)
	{
		int memorySlotsCount = requestedMaxCount / SlotSize;
		_memoryPoolHelper = new MemoryPoolHelper(memorySlotsCount);
		_slotCount = _memoryPoolHelper.MemorySlotCount;
		_itemMaxCount = _slotCount * SlotSize;
		_storage = new T();
		_storage.Initialize(_itemMaxCount);
	}

	public void Release()
	{
		_storage.Release();
	}

	public int TakeSlots(int itemCount)
	{
		int slotCount = ComputeRequiredItemSlotCount(itemCount);
		int num = _memoryPoolHelper.TakeMemorySlots(slotCount);
		if (num < 0)
		{
			Logger.Warn("Could not find consecutive free slots for {0} items!", itemCount);
		}
		return num * SlotSize;
	}

	public void ReleaseSlots(int itemStartIndex, int itemCount)
	{
		if (itemStartIndex % SlotSize != 0)
		{
			throw new Exception($"Error detected in the item buffer management - invalid 'itemStartIndex' :{itemStartIndex}");
		}
		int slot = itemStartIndex / SlotSize;
		int slotCount = ComputeRequiredItemSlotCount(itemCount);
		_memoryPoolHelper.ReleaseMemorySlots(slot, slotCount);
	}

	public void Clear()
	{
		_memoryPoolHelper.ClearMemorySlots();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int ComputeRequiredItemSlotCount(int itemCount)
	{
		int num = itemCount / SlotSize;
		return num + ((itemCount % SlotSize != 0) ? 1 : 0);
	}
}
internal class FXMemoryPool
{
	private struct TestData
	{
		public int Input;

		public int Result;

		public int ExpectedResult;
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct TestDataPooled : IFXDataStorage
	{
		public void Initialize(int items)
		{
		}

		public void Release()
		{
		}
	}

	private const int RequestedTestsMaxCount = 10000;

	public static void UnitTest()
	{
		FXMemoryPool<TestDataPooled> fXMemoryPool = new FXMemoryPool<TestDataPooled>();
		fXMemoryPool.Initialize(10000);
		TestData[] array = new TestData[4]
		{
			new TestData
			{
				Input = 1,
				ExpectedResult = 0
			},
			new TestData
			{
				Input = 31,
				ExpectedResult = 0
			},
			new TestData
			{
				Input = 127,
				ExpectedResult = 0
			},
			new TestData
			{
				Input = 128,
				ExpectedResult = 0
			}
		};
		array[0].Result = fXMemoryPool.TakeSlots(array[0].Input);
		fXMemoryPool.ReleaseSlots(array[0].Result, array[0].Input);
		array[1].Result = fXMemoryPool.TakeSlots(array[1].Input);
		fXMemoryPool.ReleaseSlots(array[1].Result, array[1].Input);
		array[2].Result = fXMemoryPool.TakeSlots(array[2].Input);
		fXMemoryPool.ReleaseSlots(array[2].Result, array[2].Input);
		array[3].Result = fXMemoryPool.TakeSlots(array[3].Input);
		fXMemoryPool.ReleaseSlots(array[3].Result, array[3].Input);
		for (int i = 0; i < 4; i++)
		{
			Debug.Assert(array[i].Result == array[i].ExpectedResult, $"Error in test data {i}.");
		}
		array[0] = new TestData
		{
			Input = 1,
			ExpectedResult = 0
		};
		array[1] = new TestData
		{
			Input = 31,
			ExpectedResult = 32
		};
		array[2] = new TestData
		{
			Input = 127,
			ExpectedResult = 64
		};
		array[3] = new TestData
		{
			Input = 128,
			ExpectedResult = 192
		};
		array[0].Result = fXMemoryPool.TakeSlots(array[0].Input);
		array[1].Result = fXMemoryPool.TakeSlots(array[1].Input);
		array[2].Result = fXMemoryPool.TakeSlots(array[2].Input);
		array[3].Result = fXMemoryPool.TakeSlots(array[3].Input);
		fXMemoryPool.ReleaseSlots(array[0].Result, array[0].Input);
		fXMemoryPool.ReleaseSlots(array[1].Result, array[1].Input);
		fXMemoryPool.ReleaseSlots(array[2].Result, array[2].Input);
		fXMemoryPool.ReleaseSlots(array[3].Result, array[3].Input);
		for (int j = 0; j < 4; j++)
		{
			Debug.Assert(array[j].Result == array[j].ExpectedResult, $"Error in test data {j}.");
		}
		array[0] = new TestData
		{
			Input = 127,
			ExpectedResult = 0
		};
		array[1] = new TestData
		{
			Input = 128,
			ExpectedResult = 128
		};
		array[2] = new TestData
		{
			Input = 1,
			ExpectedResult = 0
		};
		array[3] = new TestData
		{
			Input = 31,
			ExpectedResult = 32
		};
		array[0].Result = fXMemoryPool.TakeSlots(array[0].Input);
		array[1].Result = fXMemoryPool.TakeSlots(array[1].Input);
		fXMemoryPool.ReleaseSlots(array[0].Result, array[0].Input);
		array[2].Result = fXMemoryPool.TakeSlots(array[2].Input);
		array[3].Result = fXMemoryPool.TakeSlots(array[3].Input);
		fXMemoryPool.ReleaseSlots(array[1].Result, array[1].Input);
		fXMemoryPool.ReleaseSlots(array[2].Result, array[2].Input);
		fXMemoryPool.ReleaseSlots(array[3].Result, array[3].Input);
		for (int k = 0; k < 4; k++)
		{
			Debug.Assert(array[k].Result == array[k].ExpectedResult, $"Error in test data {k}.");
		}
		array[0] = new TestData
		{
			Input = 127,
			ExpectedResult = 0
		};
		array[1] = new TestData
		{
			Input = 128,
			ExpectedResult = 128
		};
		array[2] = new TestData
		{
			Input = 129,
			ExpectedResult = 256
		};
		array[3] = new TestData
		{
			Input = 31,
			ExpectedResult = 0
		};
		array[0].Result = fXMemoryPool.TakeSlots(array[0].Input);
		array[1].Result = fXMemoryPool.TakeSlots(array[1].Input);
		fXMemoryPool.ReleaseSlots(array[0].Result, array[0].Input);
		array[2].Result = fXMemoryPool.TakeSlots(array[2].Input);
		array[3].Result = fXMemoryPool.TakeSlots(array[3].Input);
		fXMemoryPool.ReleaseSlots(array[1].Result, array[1].Input);
		fXMemoryPool.ReleaseSlots(array[2].Result, array[2].Input);
		fXMemoryPool.ReleaseSlots(array[3].Result, array[3].Input);
		for (int l = 0; l < 4; l++)
		{
			Debug.Assert(array[l].Result == array[l].ExpectedResult, $"Error in test data {l}.");
		}
		array[0] = new TestData
		{
			Input = 512,
			ExpectedResult = 0
		};
		array[1] = new TestData
		{
			Input = 510,
			ExpectedResult = 512
		};
		array[2] = new TestData
		{
			Input = 1020,
			ExpectedResult = 1024
		};
		array[3] = new TestData
		{
			Input = 31,
			ExpectedResult = 0
		};
		array[0].Result = fXMemoryPool.TakeSlots(array[0].Input);
		array[1].Result = fXMemoryPool.TakeSlots(array[1].Input);
		fXMemoryPool.ReleaseSlots(array[0].Result, array[0].Input);
		array[2].Result = fXMemoryPool.TakeSlots(array[2].Input);
		array[3].Result = fXMemoryPool.TakeSlots(array[3].Input);
		fXMemoryPool.ReleaseSlots(array[1].Result, array[1].Input);
		fXMemoryPool.ReleaseSlots(array[2].Result, array[2].Input);
		fXMemoryPool.ReleaseSlots(array[3].Result, array[3].Input);
		for (int m = 0; m < 4; m++)
		{
			Debug.Assert(array[m].Result == array[m].ExpectedResult, $"Error in test data {m}.");
		}
		array[0] = new TestData
		{
			Input = 1024,
			ExpectedResult = 0
		};
		array[1] = new TestData
		{
			Input = 1000,
			ExpectedResult = 1024
		};
		array[2] = new TestData
		{
			Input = 1020,
			ExpectedResult = 2048
		};
		array[3] = new TestData
		{
			Input = 31,
			ExpectedResult = 1024
		};
		array[0].Result = fXMemoryPool.TakeSlots(array[0].Input);
		array[1].Result = fXMemoryPool.TakeSlots(array[1].Input);
		array[2].Result = fXMemoryPool.TakeSlots(array[2].Input);
		fXMemoryPool.ReleaseSlots(array[1].Result, array[1].Input);
		array[3].Result = fXMemoryPool.TakeSlots(array[3].Input);
		fXMemoryPool.ReleaseSlots(array[0].Result, array[0].Input);
		fXMemoryPool.ReleaseSlots(array[2].Result, array[2].Input);
		fXMemoryPool.ReleaseSlots(array[3].Result, array[3].Input);
		for (int n = 0; n < 4; n++)
		{
			Debug.Assert(array[n].Result == array[n].ExpectedResult, $"Error in test data {n}.");
		}
		array[0] = new TestData
		{
			Input = 1024,
			ExpectedResult = 0
		};
		array[1] = new TestData
		{
			Input = 1025,
			ExpectedResult = 2048
		};
		array[2] = new TestData
		{
			Input = 2040,
			ExpectedResult = 4096
		};
		array[3] = new TestData
		{
			Input = 100,
			ExpectedResult = 1024
		};
		array[0].Result = fXMemoryPool.TakeSlots(array[0].Input);
		array[1].Result = fXMemoryPool.TakeSlots(array[1].Input);
		array[2].Result = fXMemoryPool.TakeSlots(array[2].Input);
		array[3].Result = fXMemoryPool.TakeSlots(array[3].Input);
		fXMemoryPool.ReleaseSlots(array[0].Result, array[0].Input);
		fXMemoryPool.ReleaseSlots(array[1].Result, array[1].Input);
		fXMemoryPool.ReleaseSlots(array[2].Result, array[2].Input);
		fXMemoryPool.ReleaseSlots(array[3].Result, array[3].Input);
		for (int num = 0; num < 4; num++)
		{
			Debug.Assert(array[num].Result == array[num].ExpectedResult, $"Error in test data {num}.");
		}
		array[0] = new TestData
		{
			Input = 1024,
			ExpectedResult = 0
		};
		array[1] = new TestData
		{
			Input = 1025,
			ExpectedResult = 0
		};
		array[2] = new TestData
		{
			Input = 2048,
			ExpectedResult = 0
		};
		array[3] = new TestData
		{
			Input = 2049,
			ExpectedResult = 1
		};
		for (int num2 = 0; num2 < 4; num2++)
		{
			try
			{
				array[num2].Result = 0;
				int num3 = fXMemoryPool.TakeSlots(array[num2].Input);
			}
			catch (Exception)
			{
				array[num2].Result = 1;
			}
		}
		for (int num4 = 0; num4 < 4; num4++)
		{
			Debug.Assert(array[num4].Result == array[num4].ExpectedResult, $"Error in test data {num4}.");
		}
		fXMemoryPool.Clear();
		int num5 = fXMemoryPool.SlotCount + 100;
		int slotCount = fXMemoryPool.SlotCount;
		int num6 = -1;
		for (int num7 = 0; num7 < num5; num7++)
		{
			int num8 = fXMemoryPool.TakeSlots(fXMemoryPool.SlotSize);
			if (num8 < 0)
			{
				num6 = num7;
				break;
			}
		}
		Debug.Assert(num6 == slotCount, $"Error in test data - failure came at {num6}, and was expected at {slotCount}.");
		fXMemoryPool.Clear();
		fXMemoryPool.Release();
	}
}
