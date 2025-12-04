using System;

namespace Epic.OnlineServices.AntiCheatCommon;

public struct LogPlayerReviveOptions
{
	public IntPtr RevivedPlayerHandle { get; set; }

	public IntPtr ReviverPlayerHandle { get; set; }
}
