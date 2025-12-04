using System;
using System.Collections.Generic;

namespace HytaleClient.Utils;

public class ComparisonUtils
{
	public static bool CompareKeyValuePairLists<A, B>(IReadOnlyList<KeyValuePair<string, A>> list1, IReadOnlyList<KeyValuePair<string, B>> list2) where A : class where B : class
	{
		if (list1.Count != list2.Count)
		{
			return false;
		}
		for (int i = 0; i < list1.Count; i++)
		{
			if (!list1[i].Key.Equals(list2[i].Key) || !list1[i].Value.Equals(list2[i].Value))
			{
				return false;
			}
		}
		return true;
	}

	public static Comparison<T> Compose<T>(params Comparison<T>[] comparisons) where T : class
	{
		return delegate(T o1, T o2)
		{
			int num = 0;
			int num2 = 0;
			while (num2 == 0 && num < comparisons.Length)
			{
				num2 = comparisons[num++](o1, o2);
			}
			return num2;
		};
	}
}
