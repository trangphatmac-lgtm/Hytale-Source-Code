using System;

namespace Epic.OnlineServices.AntiCheatCommon;

public struct LogPlayerSpawnOptions
{
	public IntPtr SpawnedPlayerHandle { get; set; }

	public uint TeamId { get; set; }

	public uint CharacterId { get; set; }
}
