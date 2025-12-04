#define DEBUG
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HytaleClient.Data.Audio;
using HytaleClient.Utils;

namespace HytaleClient.Audio;

internal class ResourceManager
{
	public readonly ConcurrentDictionary<string, string> FilePathsByFileName = new ConcurrentDictionary<string, string>();

	public Dictionary<string, uint> WwiseEventIds = new Dictionary<string, uint>();

	public Dictionary<string, uint> WwiseGameParameterIds = new Dictionary<string, uint>();

	public Dictionary<uint, string> DebugWwiseIds = new Dictionary<uint, string>();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint GetNetworkWwiseId(int value)
	{
		return (uint)value ^ 0x80000000u;
	}

	public void SetupWwiseIds(Dictionary<string, WwiseResource> upcomingWwiseIds)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		WwiseEventIds.Clear();
		WwiseGameParameterIds.Clear();
		DebugWwiseIds.Clear();
		foreach (KeyValuePair<string, WwiseResource> upcomingWwiseId in upcomingWwiseIds)
		{
			uint id = upcomingWwiseId.Value.Id;
			if (upcomingWwiseId.Value.Type == WwiseResource.WwiseResourceType.Event)
			{
				WwiseEventIds[upcomingWwiseId.Key] = id;
			}
			else
			{
				WwiseGameParameterIds[upcomingWwiseId.Key] = id;
			}
			DebugWwiseIds[id] = upcomingWwiseId.Key;
		}
	}
}
