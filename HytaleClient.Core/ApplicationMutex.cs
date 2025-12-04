using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace HytaleClient.Core;

internal class ApplicationMutex : IDisposable
{
	public readonly bool Acquired;

	private Mutex _mutex;

	public ApplicationMutex()
	{
		string arg = ((GuidAttribute)Assembly.GetEntryAssembly().GetCustomAttributes(typeof(GuidAttribute), inherit: false).GetValue(0)).Value.ToString();
		string name = $"Global\\{{{arg}}}";
		_mutex = new Mutex(initiallyOwned: false, name);
		Acquired = false;
		try
		{
			Acquired = _mutex.WaitOne(10, exitContext: false);
		}
		catch (AbandonedMutexException)
		{
			Acquired = true;
		}
	}

	public void Dispose()
	{
		if (Acquired)
		{
			_mutex.ReleaseMutex();
		}
	}
}
