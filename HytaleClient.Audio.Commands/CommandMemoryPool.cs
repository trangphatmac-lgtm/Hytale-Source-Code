#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Audio.Commands;

internal class CommandMemoryPool
{
	private struct TestData
	{
		public int Result;

		public int ExpectedResult;
	}

	private struct CommandField
	{
		public int Gap;

		public int Size;

		public string Name;
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public const int RequestedPriorityCommandMaxCount = 64;

	private const int RequestedCommandMaxCount = 8192;

	private int _commandMaxCount;

	private int _priorityCommandMaxCount;

	private CommandBuffers _commands;

	private MemoryPoolHelper _memoryPoolHelper;

	private MemoryPoolHelper _priorityMemoryPoolHelper;

	public CommandBuffers Commands => _commands;

	public void Initialize()
	{
		_priorityCommandMaxCount = 64;
		_priorityMemoryPoolHelper = new MemoryPoolHelper(_priorityCommandMaxCount);
		_commandMaxCount = 8192;
		_memoryPoolHelper = new MemoryPoolHelper(_commandMaxCount);
		_commands = new CommandBuffers(_priorityCommandMaxCount + _commandMaxCount);
	}

	public int TakeSlot()
	{
		int num = _memoryPoolHelper.ThreadSafeTakeMemorySlot(1);
		if (num < 0)
		{
			Logger.Warn("Could not find a free slot for basic sound command!");
		}
		return num + _priorityCommandMaxCount;
	}

	public void ReleaseSlot(int slot)
	{
		_commands.Data[slot] = default(CommandBuffers.CommandData);
		_memoryPoolHelper.ThreadSafeReleaseMemorySlot(slot - _priorityCommandMaxCount, 1);
	}

	public int TakePrioritySlot()
	{
		int num = _priorityMemoryPoolHelper.ThreadSafeTakeMemorySlot(1);
		if (num < 0)
		{
			Logger.Warn("Could not find a free slot for priority sound command!");
		}
		return num;
	}

	public void ReleasePrioritySlot(int slot)
	{
		_commands.Data[slot] = default(CommandBuffers.CommandData);
		_priorityMemoryPoolHelper.ThreadSafeReleaseMemorySlot(slot, 1);
	}

	public static bool StressTest(bool continuous, out string resultLog)
	{
		ConcurrentQueue<int> concurrentQueue = new ConcurrentQueue<int>();
		CommandMemoryPool testSystem = new CommandMemoryPool();
		testSystem.Initialize();
		int requestCancel = 0;
		int registeredCommands = 0;
		int processedCommands = 0;
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		string audioThreadLog = "";
		Thread thread = new Thread((ThreadStart)delegate
		{
			AudioDeviceThreadStart(testSystem, concurrentQueue, ref requestCancel, ref processedCommands, ref registeredCommands, cancellationTokenSource.Token, out audioThreadLog);
		})
		{
			Name = "ConsumerStressThread",
			IsBackground = true
		};
		thread.Start();
		Stopwatch stopwatch = Stopwatch.StartNew();
		float num = 1f;
		resultLog = ".CommandMemoryPool.StressTest().\n";
		string concurrentLog = "";
		ConcurrentQueue<string> concurrentQueue2 = new ConcurrentQueue<string>();
		bool flag = false;
		while (num > 0f && !flag)
		{
			Parallel.For(0, 8000, (Action<int>)delegate
			{
				int num2 = testSystem.TakeSlot();
				if (num2 >= 64)
				{
					if (testSystem.Commands.Data[num2].Volume != 0f)
					{
						concurrentLog += $" Take error at: {num2} - {testSystem.Commands.Data[num2].Volume}\n";
					}
					testSystem.Commands.Data[num2].Volume = num2;
					concurrentQueue.Enqueue(num2);
					Interlocked.Increment(ref registeredCommands);
				}
			});
			if (!continuous)
			{
				num -= (float)stopwatch.Elapsed.TotalSeconds;
				stopwatch.Restart();
			}
			else
			{
				flag = flag || concurrentLog != "" || audioThreadLog != "";
			}
		}
		Interlocked.Increment(ref requestCancel);
		cancellationTokenSource.CancelAfter(new TimeSpan(0, 0, 1));
		thread.Join();
		num -= (float)stopwatch.Elapsed.TotalSeconds;
		stopwatch.Restart();
		bool flag2 = registeredCommands == processedCommands;
		resultLog += concurrentLog;
		resultLog += audioThreadLog;
		resultLog += $"  Processed {processedCommands}/{registeredCommands} commands in {1f - num} seconds";
		if (!flag2)
		{
			resultLog += "\n  Not all commands were proccessed!";
		}
		Debug.Assert(flag2, "  Not all commands were proccessed!");
		cancellationTokenSource.Dispose();
		return flag2;
	}

	public static void AudioDeviceThreadStart(CommandMemoryPool testSystem, ConcurrentQueue<int> concurrentQueue, ref int requestCancel, ref int processedCommands, ref int registeredCommands, CancellationToken cancellationToken, out string errorLog)
	{
		errorLog = "    ...AudioThread log...\n";
		while (!cancellationToken.IsCancellationRequested && (requestCancel == 0 || (requestCancel == 1 && processedCommands != registeredCommands)))
		{
			int result;
			while (concurrentQueue.TryDequeue(out result))
			{
				if (testSystem.Commands.Data[result].Volume != (float)result)
				{
					errorLog += $"    Release error at:  {result} - {testSystem.Commands.Data[result].Volume}\n";
				}
				testSystem.Commands.Data[result].Volume = 0f;
				testSystem.ReleaseSlot(result);
				Interlocked.Increment(ref processedCommands);
			}
			Thread.Sleep(16);
		}
		if (cancellationToken.IsCancellationRequested)
		{
			errorLog += "    was forced canceled.\n";
		}
	}

	public static void UnitTest()
	{
		CommandMemoryPool testSystem = new CommandMemoryPool();
		testSystem.Initialize();
		int num = 0;
		int num2 = 127;
		TestData[] array = new TestData[num2];
		for (int j = 0; j < num2; j++)
		{
			array[j] = new TestData
			{
				ExpectedResult = j + 64
			};
			array[j].Result = testSystem.TakeSlot();
		}
		for (int k = 0; k < num2; k++)
		{
			Debug.Assert(array[k].Result == array[k].ExpectedResult, $"Error in test data {k}.");
		}
		num = num2;
		int num3 = 128;
		int[] reservedSlots = new int[num3];
		ConcurrentQueue<int> concurrentQueue = new ConcurrentQueue<int>();
		Parallel.For(0, num3, (Action<int>)delegate
		{
			concurrentQueue.Enqueue(testSystem.TakeSlot() - 64);
		});
		int num4 = 0;
		int result;
		while (concurrentQueue.TryDequeue(out result))
		{
			reservedSlots[num4] = result;
			num4++;
		}
		for (int l = 0; l < num3; l++)
		{
			Debug.Assert(reservedSlots[l] != 0, $"Error in test data {l}");
		}
		Parallel.For(0, num3, delegate(int i)
		{
			testSystem.ReleaseSlot(reservedSlots[i]);
		});
		int num5 = testSystem.TakeSlot();
		Debug.Assert(num5 == num, "Error in test data.");
		testSystem.ReleaseSlot(num5);
	}

	public static void CommandBufferUnitTest()
	{
		Debug.Assert(Test(new CommandField[1]
		{
			new CommandField
			{
				Size = 1,
				Name = "Type"
			}
		}, out var errorMessage), "Command field overlaps: " + errorMessage);
		Debug.Assert(Test(new CommandField[5]
		{
			new CommandField
			{
				Size = 1,
				Name = "Type"
			},
			new CommandField
			{
				Size = 8,
				Name = "SoundObjectReference"
			},
			new CommandField
			{
				Size = 12,
				Name = "WorldPosition"
			},
			new CommandField
			{
				Size = 12,
				Name = "WorldOrientation"
			},
			new CommandField
			{
				Size = 1,
				Name = "BoolData"
			}
		}, out errorMessage), "Command field overlaps: " + errorMessage);
		Debug.Assert(Test(new CommandField[2]
		{
			new CommandField
			{
				Size = 1,
				Name = "Type"
			},
			new CommandField
			{
				Size = 8,
				Name = "SoundObjectReference"
			}
		}, out errorMessage), "Command field overlaps: " + errorMessage);
		Debug.Assert(Test(new CommandField[4]
		{
			new CommandField
			{
				Size = 1,
				Name = "Type"
			},
			new CommandField
			{
				Size = 8,
				Name = "SoundObjectReference"
			},
			new CommandField
			{
				Size = 12,
				Name = "WorldPosition"
			},
			new CommandField
			{
				Size = 12,
				Name = "WorldOrientation"
			}
		}, out errorMessage), "Command field overlaps: " + errorMessage);
		Debug.Assert(Test(new CommandField[3]
		{
			new CommandField
			{
				Size = 1,
				Name = "Type"
			},
			new CommandField
			{
				Size = 12,
				Gap = 8,
				Name = "WorldPosition"
			},
			new CommandField
			{
				Size = 12,
				Name = "WorldOrientation"
			}
		}, out errorMessage), "Command field overlaps: " + errorMessage);
		Debug.Assert(Test(new CommandField[4]
		{
			new CommandField
			{
				Size = 1,
				Name = "Type"
			},
			new CommandField
			{
				Size = 8,
				Name = "SoundObjectReference"
			},
			new CommandField
			{
				Size = 4,
				Name = "EventId"
			},
			new CommandField
			{
				Size = 4,
				Name = "PlaybackId"
			}
		}, out errorMessage), "Command field overlaps: " + errorMessage);
		Debug.Assert(Test(new CommandField[5]
		{
			new CommandField
			{
				Size = 1,
				Name = "Type"
			},
			new CommandField
			{
				Size = 4,
				Name = "TransitionDuration"
			},
			new CommandField
			{
				Size = 1,
				Name = "FadeCurveType"
			},
			new CommandField
			{
				Size = 1,
				Name = "ActionType"
			},
			new CommandField
			{
				Size = 4,
				Gap = 6,
				Name = "PlaybackId"
			}
		}, out errorMessage), "Command field overlaps: " + errorMessage);
		Debug.Assert(Test(new CommandField[3]
		{
			new CommandField
			{
				Size = 1,
				Name = "Type"
			},
			new CommandField
			{
				Size = 4,
				Name = "Volume"
			},
			new CommandField
			{
				Size = 4,
				Name = "RTPCId"
			}
		}, out errorMessage), "Command field overlaps: " + errorMessage);
	}

	private static bool Test(CommandField[] test, out string errorMessage)
	{
		bool result = true;
		errorMessage = "";
		int num = 0;
		for (int i = 0; i < test.Length; i++)
		{
			CommandField commandField = test[i];
			int num2 = num + commandField.Gap;
			int num3 = Marshal.OffsetOf(typeof(CommandBuffers.CommandData), commandField.Name).ToInt32();
			if (num2 != num3)
			{
				result = false;
				errorMessage = commandField.Name;
				break;
			}
			num = num2 + commandField.Size;
		}
		return result;
	}
}
