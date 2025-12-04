using System;

namespace Epic.OnlineServices.Version;

public sealed class VersionInterface
{
	public static readonly Utf8String CompanyName = "Epic Games, Inc.";

	public static readonly Utf8String CopyrightString = "Copyright Epic Games, Inc. All Rights Reserved.";

	public const int MajorVersion = 1;

	public const int MinorVersion = 17;

	public const int PatchVersion = 0;

	public static readonly Utf8String ProductIdentifier = "Epic Online Services SDK";

	public static readonly Utf8String ProductName = "Epic Online Services SDK";

	public static Utf8String GetVersion()
	{
		IntPtr from = Bindings.EOS_GetVersion();
		Helper.Get(from, out Utf8String to);
		return to;
	}
}
