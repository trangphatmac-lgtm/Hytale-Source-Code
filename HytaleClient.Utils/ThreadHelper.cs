using System.Threading;

namespace HytaleClient.Utils;

internal static class ThreadHelper
{
	private static readonly int MainThreadId;

	static ThreadHelper()
	{
		Thread currentThread = Thread.CurrentThread;
		currentThread.Name = "MainThread";
		MainThreadId = currentThread.ManagedThreadId;
	}

	public static void Initialize()
	{
	}

	public static bool IsMainThread()
	{
		return Thread.CurrentThread.ManagedThreadId == MainThreadId;
	}

	public static bool IsOnThread(Thread thread)
	{
		return Thread.CurrentThread.ManagedThreadId == thread.ManagedThreadId;
	}
}
