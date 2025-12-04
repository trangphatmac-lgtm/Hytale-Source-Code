using System;

namespace Epic.OnlineServices.AntiCheatCommon;

public struct LogPlayerUseAbilityOptions
{
	public IntPtr PlayerHandle { get; set; }

	public uint AbilityId { get; set; }

	public uint AbilityDurationMs { get; set; }

	public uint AbilityCooldownMs { get; set; }
}
