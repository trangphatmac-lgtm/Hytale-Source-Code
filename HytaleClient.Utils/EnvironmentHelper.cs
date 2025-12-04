using System;
using System.IO;
using System.Linq;

namespace HytaleClient.Utils;

public static class EnvironmentHelper
{
	public static string ResolvePathExecutable(string exec)
	{
		if (File.Exists(exec))
		{
			return Path.GetFullPath(exec);
		}
		string environmentVariable = Environment.GetEnvironmentVariable("PATH");
		if (string.IsNullOrEmpty(environmentVariable))
		{
			return null;
		}
		return (from path in environmentVariable.Split(new char[1] { Path.PathSeparator })
			select Path.Combine(path, exec)).FirstOrDefault(File.Exists);
	}
}
