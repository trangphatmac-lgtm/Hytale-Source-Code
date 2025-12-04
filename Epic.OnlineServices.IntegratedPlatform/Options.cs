using System;

namespace Epic.OnlineServices.IntegratedPlatform;

public struct Options
{
	public Utf8String Type { get; set; }

	public IntegratedPlatformManagementFlags Flags { get; set; }

	public IntPtr InitOptions { get; set; }

	internal void Set(ref OptionsInternal other)
	{
		Type = other.Type;
		Flags = other.Flags;
		InitOptions = other.InitOptions;
	}
}
