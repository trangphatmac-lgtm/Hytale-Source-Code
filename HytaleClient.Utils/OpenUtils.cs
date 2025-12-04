using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace HytaleClient.Utils;

public static class OpenUtils
{
	[DllImport("shell32.dll", SetLastError = true)]
	public static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, [In][MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);

	[DllImport("shell32.dll", SetLastError = true)]
	public static extern void SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr bindingContext, out IntPtr pidl, uint sfgaoIn, out uint psfgaoOut);

	private static bool TryRevealPathInDirectory_Windows(string filePath)
	{
		string directoryName = Path.GetDirectoryName(filePath);
		SHParseDisplayName(directoryName, IntPtr.Zero, out var pidl, 0u, out var psfgaoOut);
		if (pidl == IntPtr.Zero)
		{
			return false;
		}
		SHParseDisplayName(filePath, IntPtr.Zero, out var pidl2, 0u, out psfgaoOut);
		IntPtr[] array = ((!(pidl2 == IntPtr.Zero)) ? new IntPtr[1] { pidl2 } : new IntPtr[0]);
		SHOpenFolderAndSelectItems(pidl, (uint)array.Length, array, 0u);
		Marshal.FreeCoTaskMem(pidl);
		if (pidl2 != IntPtr.Zero)
		{
			Marshal.FreeCoTaskMem(pidl2);
		}
		return true;
	}

	private static bool TryRevealPathInDirectory_Mac(string filePath)
	{
		Process.Start("open", "--reveal " + filePath);
		return true;
	}

	private static bool TryRevealPathInDirectory_Linux(string filePath)
	{
		Process.Start("nautilus", "--select " + filePath);
		return true;
	}

	public static bool TryOpenFileInContainingDirectory(string filePath, string rootPathForValidation)
	{
		filePath = Path.GetFullPath(filePath);
		rootPathForValidation = Path.GetFullPath(rootPathForValidation);
		if (!File.Exists(filePath))
		{
			return false;
		}
		string text = filePath;
		string text2 = rootPathForValidation;
		char directorySeparatorChar = Path.DirectorySeparatorChar;
		if (!text.StartsWith(text2 + directorySeparatorChar))
		{
			return false;
		}
		return BuildInfo.Platform switch
		{
			Platform.Windows => TryRevealPathInDirectory_Windows(filePath), 
			Platform.MacOS => TryRevealPathInDirectory_Mac(filePath), 
			Platform.Linux => TryRevealPathInDirectory_Linux(filePath), 
			_ => false, 
		};
	}

	public static bool TryOpenDirectoryInContainingDirectory(string filePath, string rootPathForValidation)
	{
		filePath = Path.GetFullPath(filePath);
		rootPathForValidation = Path.GetFullPath(rootPathForValidation);
		string text = filePath;
		char directorySeparatorChar = Path.DirectorySeparatorChar;
		if (!text.EndsWith(directorySeparatorChar.ToString()))
		{
			string text2 = filePath;
			directorySeparatorChar = Path.DirectorySeparatorChar;
			filePath = text2 + directorySeparatorChar;
		}
		if (!Directory.Exists(filePath))
		{
			return false;
		}
		string text3 = filePath;
		string text4 = rootPathForValidation;
		directorySeparatorChar = Path.DirectorySeparatorChar;
		if (!text3.StartsWith(text4 + directorySeparatorChar))
		{
			return false;
		}
		return BuildInfo.Platform switch
		{
			Platform.Windows => TryRevealPathInDirectory_Windows(filePath), 
			Platform.MacOS => TryRevealPathInDirectory_Mac(filePath), 
			Platform.Linux => TryRevealPathInDirectory_Linux(filePath), 
			_ => false, 
		};
	}

	public static void OpenTrustedUrlInDefaultBrowser(string url)
	{
		switch (BuildInfo.Platform)
		{
		case Platform.Windows:
			Process.Start(new ProcessStartInfo
			{
				FileName = url,
				UseShellExecute = true
			});
			break;
		case Platform.MacOS:
			Process.Start("open", url);
			break;
		case Platform.Linux:
			Process.Start("xdg-open", url);
			break;
		}
	}
}
