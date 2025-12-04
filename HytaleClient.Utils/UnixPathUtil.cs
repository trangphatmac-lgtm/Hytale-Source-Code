using System.IO;

namespace HytaleClient.Utils;

public static class UnixPathUtil
{
	public static int IndexOfExtension(string path)
	{
		if (path == null)
		{
			return -1;
		}
		int num = path.LastIndexOf('.');
		int num2 = path.LastIndexOf('/');
		return (num > num2) ? num : (-1);
	}

	public static string GetExtension(string path)
	{
		int num = IndexOfExtension(path);
		if (num > -1 && num < path.Length - 1)
		{
			return path.Substring(num);
		}
		return string.Empty;
	}

	public static string GetFileNameWithoutExtension(string path)
	{
		int num = IndexOfExtension(path);
		int num2 = path.LastIndexOf('/');
		if (num > -1 && num < path.Length - 1)
		{
			num2 = ((num2 >= 0) ? (num2 + 1) : 0);
			return path.Substring(num2, num - num2);
		}
		return (num2 > -1) ? path.Substring(num2 + 1) : path;
	}

	public static string GetFileName(string path)
	{
		int num = path.LastIndexOf('/');
		return (num > -1) ? path.Substring(num + 1) : path;
	}

	public static string ConvertToUnixPath(string path)
	{
		if (Path.DirectorySeparatorChar == '/')
		{
			return path;
		}
		return path.Replace(Path.DirectorySeparatorChar, '/');
	}
}
