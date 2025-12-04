using System;
using System.IO;
using System.Reflection;
using NLog;
using SDL2;

namespace HytaleClient.Utils;

internal static class BuildInfo
{
	private static readonly Logger Logger;

	public static readonly Platform Platform;

	public static readonly string Architecture;

	public static readonly string Configuration;

	public static readonly string Version;

	public static readonly string RevisionId;

	public static readonly string BranchName;

	static BuildInfo()
	{
		Logger = LogManager.GetCurrentClassLogger();
		Architecture = ((IntPtr.Size == 8) ? "x64" : "x86");
		Configuration = "Debug";
		string text = SDL.SDL_GetPlatform();
		switch (text)
		{
		case "Windows":
			Platform = Platform.Windows;
			break;
		case "Mac OS X":
			Platform = Platform.MacOS;
			break;
		case "Linux":
			Platform = Platform.Linux;
			break;
		default:
			Logger.Warn("The platform {0} is not supported! Defaulting to Windows.", text);
			Platform = Platform.Windows;
			break;
		}
		Version version = Assembly.GetExecutingAssembly().GetName().Version;
		string arg = "0-dev";
		Version = $"{version.Major}.{version.Minor}.{arg}";
		if (RevisionId != null)
		{
			return;
		}
		string text2 = Path.GetFullPath(Path.Combine(Paths.App, "..", "..", "..", "..", ".git"));
		string gitPath = text2;
		if (File.Exists(text2) && (File.GetAttributes(text2) & FileAttributes.Directory) == 0)
		{
			string text3 = File.ReadAllLines(text2)[0];
			if (!text3.StartsWith("gitdir: "))
			{
				throw new Exception("Can't handle work-tree. Missing gitdir");
			}
			text2 = text3.Substring("gitdir: ".Length);
			gitPath = Path.GetFullPath(Path.Combine(text2, "..", ".."));
		}
		string path = Path.Combine(text2, "HEAD");
		string text4 = File.ReadAllLines(path)[0];
		if (text4.StartsWith("ref: "))
		{
			string text5 = text4.Substring("ref: ".Length);
			BranchName = text5.Substring(text5.LastIndexOf("/", StringComparison.Ordinal) + 1);
			RevisionId = GetShaFromBranch(gitPath, text5, BranchName);
		}
		else
		{
			RevisionId = text4;
			BranchName = "(detached)";
		}
	}

	private static string GetShaFromBranch(string gitPath, string headRef, string branchName)
	{
		string path = Path.Combine(gitPath, "refs", "heads", branchName);
		if (File.Exists(path))
		{
			return File.ReadAllLines(path)[0];
		}
		string[] array = File.ReadAllLines(Path.Combine(gitPath, "packed-refs"));
		foreach (string text in array)
		{
			if (text.Contains(headRef))
			{
				return text.Split(new char[1] { ' ' })[0];
			}
		}
		return "(unknown)";
	}

	public static void PrintAll()
	{
		Logger.Info("HytaleClient v{0} ({1} {2} {3} — {4} - .NET {5})", new object[6]
		{
			Version,
			Platform,
			Architecture,
			Configuration,
			Environment.OSVersion.VersionString,
			Environment.Version
		});
		Logger.Info<string, string>("Branch: {0}, Revision: {1}", BranchName, RevisionId);
	}
}
