using System;
using System.Collections.Generic;
using HytaleClient.AssetEditor.Data;

namespace HytaleClient.AssetEditor.Utils;

public class AssetFileComparer : IComparer<AssetFile>
{
	public static readonly AssetFileComparer Instance = new AssetFileComparer(ignoreLowerCase: false);

	public static readonly AssetFileComparer IgnoreCaseInstance = new AssetFileComparer(ignoreLowerCase: true);

	private readonly bool _ignoreCase;

	public AssetFileComparer(bool ignoreLowerCase)
	{
		_ignoreCase = ignoreLowerCase;
	}

	public int Compare(AssetFile a, AssetFile b)
	{
		int num = System.Math.Max(a.PathElements.Length, b.PathElements.Length);
		for (int i = 0; i < num; i++)
		{
			if (i >= a.PathElements.Length)
			{
				return -1;
			}
			if (i >= b.PathElements.Length)
			{
				return 1;
			}
			bool flag = i == a.PathElements.Length - 1 && !a.IsDirectory;
			bool flag2 = i == b.PathElements.Length - 1 && !b.IsDirectory;
			if (flag && !flag2)
			{
				return 1;
			}
			if (flag2 && !flag)
			{
				return -1;
			}
			int num2 = string.Compare(a.PathElements[i], b.PathElements[i], _ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
			if (num2 != 0)
			{
				return num2;
			}
		}
		return string.Compare(a.Path, b.Path, _ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
	}
}
