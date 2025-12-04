#define DEBUG
using System;
using System.Diagnostics;

namespace HytaleClient.AssetEditor.Interface.Config;

public struct PropertyPath : IEquatable<PropertyPath>
{
	public static readonly PropertyPath Root = new PropertyPath(new string[0], 0);

	public readonly string[] Elements;

	private readonly int _stringLength;

	public int ElementCount => Elements.Length;

	public string LastElement => Elements[Elements.Length - 1];

	private PropertyPath(string[] elements, int length)
	{
		Elements = elements;
		_stringLength = length;
	}

	private PropertyPath(string[] elements)
	{
		Elements = elements;
		int num = ((elements.Length != 0) ? (elements.Length - 1) : 0);
		foreach (string text in elements)
		{
			num += text.Length;
		}
		_stringLength = num;
	}

	public PropertyPath GetParent()
	{
		string[] array = new string[Elements.Length - 1];
		Array.Copy(Elements, 0, array, 0, array.Length);
		int num = _stringLength - Elements[Elements.Length - 1].Length;
		if (array.Length != 0)
		{
			num--;
		}
		return new PropertyPath(array, num);
	}

	public PropertyPath GetChild(string key)
	{
		Debug.Assert(!key.Contains("."));
		string[] array = new string[Elements.Length + 1];
		Array.Copy(Elements, 0, array, 0, Elements.Length);
		array[Elements.Length] = key;
		int num = _stringLength + key.Length;
		if (array.Length > 1)
		{
			num++;
		}
		return new PropertyPath(array, num);
	}

	public bool IsDescendantOf(PropertyPath ancestor)
	{
		if (ancestor._stringLength >= _stringLength || ancestor.ElementCount >= ElementCount)
		{
			return false;
		}
		for (int i = 0; i < ancestor.Elements.Length; i++)
		{
			if (Elements[i] != ancestor.Elements[i])
			{
				return false;
			}
		}
		return true;
	}

	public bool Equals(PropertyPath other)
	{
		if (_stringLength != other._stringLength)
		{
			return false;
		}
		if (Elements.Length != other.Elements.Length)
		{
			return false;
		}
		for (int i = 0; i < Elements.Length; i++)
		{
			if (Elements[i] != other.Elements[i])
			{
				return false;
			}
		}
		return true;
	}

	public override bool Equals(object obj)
	{
		return obj is PropertyPath other && Equals(other);
	}

	public override int GetHashCode()
	{
		int num = 17;
		string[] elements = Elements;
		for (int i = 0; i < elements.Length; i++)
		{
			num = num * 23 + (elements[i]?.GetHashCode() ?? 0);
		}
		return num;
	}

	public override string ToString()
	{
		return string.Join(".", Elements);
	}

	public static PropertyPath FromString(string path)
	{
		return new PropertyPath(path.Split(new char[1] { '.' }, StringSplitOptions.RemoveEmptyEntries));
	}

	public static PropertyPath FromElements(string[] pathElements)
	{
		return new PropertyPath(pathElements);
	}
}
