using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Audio;

internal class SoundObjectMemoryPool
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private const int SoundObjectMaxCount = 2048;

	private int _soundObjectMaxCount;

	private SoundObjectBuffers _soundObjects;

	private MemoryPoolHelper _memoryPoolHelper;

	public SoundObjectBuffers SoundObjects => _soundObjects;

	public void Initialize()
	{
		_soundObjectMaxCount = 2048;
		_memoryPoolHelper = new MemoryPoolHelper(_soundObjectMaxCount);
		_soundObjects = new SoundObjectBuffers(_soundObjectMaxCount);
	}

	public void Release()
	{
	}

	public int TakeSlot()
	{
		int num = _memoryPoolHelper.ThreadSafeTakeMemorySlot(1);
		if (num < 0)
		{
			Logger.Warn("Could not find a free slot for sound object!");
		}
		return num;
	}

	public void ReleaseSlot(int slot)
	{
		_memoryPoolHelper.ThreadSafeReleaseMemorySlot(slot, 1);
	}
}
