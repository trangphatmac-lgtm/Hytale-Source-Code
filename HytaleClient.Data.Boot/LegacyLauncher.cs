using System;
using System.IO;
using HytaleClient.Utils;

namespace HytaleClient.Data.Boot;

[Obsolete]
public static class LegacyLauncher
{
	public static string GetHomeDirectory()
	{
		return Path.Combine(BuildInfo.Platform switch
		{
			Platform.Windows => Environment.ExpandEnvironmentVariables("%APPDATA%"), 
			Platform.Linux => Environment.ExpandEnvironmentVariables("%HOME%/.config"), 
			Platform.MacOS => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support"), 
			_ => throw new Exception($"Don't know how to find the user data directory for platform {BuildInfo.Platform}"), 
		}, "hytale-launcher");
	}
}
