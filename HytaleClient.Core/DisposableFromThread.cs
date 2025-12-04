#define DEBUG
using System.Diagnostics;
using System.Threading;

namespace HytaleClient.Core;

internal abstract class DisposableFromThread : Disposable
{
	private readonly Thread _thread;

	protected DisposableFromThread(Thread thread)
	{
		_thread = thread;
	}

	public new void Dispose()
	{
		Debug.Assert(Thread.CurrentThread.ManagedThreadId == _thread.ManagedThreadId);
		if (!base.Disposed)
		{
			Disposable.UndisposedDisposables.TryRemove(_reference, out var _);
			DoDispose();
			base.Disposed = true;
		}
	}
}
