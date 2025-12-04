using System;

namespace Epic.OnlineServices.AntiCheatCommon;

public struct SetClientDetailsOptions
{
	public IntPtr ClientHandle { get; set; }

	public AntiCheatCommonClientFlags ClientFlags { get; set; }

	public AntiCheatCommonClientInput ClientInputMethod { get; set; }
}
