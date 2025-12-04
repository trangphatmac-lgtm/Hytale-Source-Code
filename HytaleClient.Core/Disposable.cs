#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json;

namespace HytaleClient.Core;

public abstract class Disposable : IDisposable
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public static readonly ConcurrentDictionary<WeakReference<Disposable>, byte> UndisposedDisposables = new ConcurrentDictionary<WeakReference<Disposable>, byte>();

	public static readonly ConcurrentDictionary<WeakReference<Disposable>, byte> UnfinalizedDisposables = new ConcurrentDictionary<WeakReference<Disposable>, byte>();

	protected readonly string StackTrace = Environment.StackTrace;

	protected readonly WeakReference<Disposable> _reference;

	[JsonIgnore]
	public bool Disposed { get; protected set; }

	public void Dispose()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (!Disposed)
		{
			UndisposedDisposables.TryRemove(_reference, out var _);
			DoDispose();
			Disposed = true;
		}
	}

	protected abstract void DoDispose();

	public static void LogSummary(bool unfinalized)
	{
		Dictionary<string, List<Disposable>> dictionary = new Dictionary<string, List<Disposable>>();
		ICollection<WeakReference<Disposable>> collection = (unfinalized ? UnfinalizedDisposables.Keys : UndisposedDisposables.Keys);
		foreach (WeakReference<Disposable> item in collection)
		{
			item.TryGetTarget(out var target);
			string key = ((target != null) ? target.GetType().FullName : "(Expired)");
			dictionary.TryGetValue(key, out var value);
			if (value == null)
			{
				List<Disposable> list2 = (dictionary[key] = new List<Disposable>());
				value = list2;
			}
			value.Add(target);
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("---- Summary of " + (unfinalized ? "unfinalized" : "undisposed") + " disposable references ----");
		foreach (KeyValuePair<string, List<Disposable>> item2 in from kvp in dictionary
			orderby -kvp.Value.Count, kvp.Key
			select kvp)
		{
			stringBuilder.AppendLine($"{item2.Key} : {item2.Value.Count}");
		}
		Logger.Info<StringBuilder>(stringBuilder);
	}

	protected Disposable()
	{
		_reference = new WeakReference<Disposable>(this);
		UndisposedDisposables.TryAdd(_reference, 0);
		UnfinalizedDisposables.TryAdd(_reference, 0);
	}

	~Disposable()
	{
		byte value;
		if (!Disposed && !CrashHandler.IsCrashing)
		{
			Logger.Warn<string, string>(StackTrace, "Object was not disposed! {0}", GetType().FullName);
			UndisposedDisposables.TryRemove(_reference, out value);
		}
		UnfinalizedDisposables.TryRemove(_reference, out value);
	}
}
