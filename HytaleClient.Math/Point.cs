using System;
using System.Diagnostics;

namespace HytaleClient.Math;

[Serializable]
[DebuggerDisplay("{DebugDisplayString,nq}")]
public struct Point : IEquatable<Point>
{
	public int X;

	public int Y;

	private static readonly Point zeroPoint = default(Point);

	public static Point Zero => zeroPoint;

	internal string DebugDisplayString => X + " " + Y;

	public Point(int x, int y)
	{
		X = x;
		Y = y;
	}

	public bool Equals(Point other)
	{
		return X == other.X && Y == other.Y;
	}

	public override bool Equals(object obj)
	{
		return obj is Point && Equals((Point)obj);
	}

	public override int GetHashCode()
	{
		return X ^ Y;
	}

	public override string ToString()
	{
		return "{X:" + X + " Y:" + Y + "}";
	}

	public static Point operator +(Point value1, Point value2)
	{
		return new Point(value1.X + value2.X, value1.Y + value2.Y);
	}

	public static Point operator -(Point value1, Point value2)
	{
		return new Point(value1.X - value2.X, value1.Y - value2.Y);
	}

	public static Point operator *(Point value1, Point value2)
	{
		return new Point(value1.X * value2.X, value1.Y * value2.Y);
	}

	public static Point operator /(Point value1, Point value2)
	{
		return new Point(value1.X / value2.X, value1.Y / value2.Y);
	}

	public static bool operator ==(Point a, Point b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(Point a, Point b)
	{
		return !a.Equals(b);
	}
}
