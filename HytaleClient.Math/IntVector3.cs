using System;
using System.Diagnostics;
using HytaleClient.Protocol;

namespace HytaleClient.Math;

[DebuggerDisplay("{DebugDisplayString,nq}")]
public struct IntVector3 : IEquatable<IntVector3>
{
	private static IntVector3 zero = new IntVector3(0, 0, 0);

	private static readonly IntVector3 one = new IntVector3(1, 1, 1);

	private static readonly IntVector3 unitX = new IntVector3(1, 0, 0);

	private static readonly IntVector3 unitY = new IntVector3(0, 1, 0);

	private static readonly IntVector3 unitZ = new IntVector3(0, 0, 1);

	private static readonly IntVector3 up = new IntVector3(0, 1, 0);

	private static readonly IntVector3 down = new IntVector3(0, -1, 0);

	private static readonly IntVector3 right = new IntVector3(1, 0, 0);

	private static readonly IntVector3 left = new IntVector3(-1, 0, 0);

	private static readonly IntVector3 forward = new IntVector3(0, 0, -1);

	private static readonly IntVector3 backward = new IntVector3(0, 0, 1);

	private static readonly IntVector3 min = new IntVector3(int.MinValue, int.MinValue, int.MinValue);

	private static readonly IntVector3 max = new IntVector3(int.MaxValue, int.MaxValue, int.MaxValue);

	public int X;

	public int Y;

	public int Z;

	internal string DebugDisplayString => X + " " + Y + " " + Z;

	public static IntVector3 Zero => zero;

	public static IntVector3 One => one;

	public static IntVector3 UnitX => unitX;

	public static IntVector3 UnitY => unitY;

	public static IntVector3 UnitZ => unitZ;

	public static IntVector3 Up => up;

	public static IntVector3 Down => down;

	public static IntVector3 Right => right;

	public static IntVector3 Left => left;

	public static IntVector3 Forward => forward;

	public static IntVector3 Backward => backward;

	public static IntVector3 Min => min;

	public static IntVector3 Max => max;

	public IntVector3(int x, int y, int z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public IntVector3(Vector3 v)
	{
		X = (int)System.Math.Floor(v.X);
		Y = (int)System.Math.Floor(v.Y);
		Z = (int)System.Math.Floor(v.Z);
	}

	public IntVector3(int value)
	{
		X = value;
		Y = value;
		Z = value;
	}

	public Vector3 ToVector3()
	{
		return new Vector3(X, Y, Z);
	}

	public BlockPosition ToBlockPosition()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		return new BlockPosition(X, Y, Z);
	}

	public override bool Equals(object obj)
	{
		return obj is IntVector3 other && Equals(other);
	}

	public bool Equals(IntVector3 other)
	{
		return X == other.X && Y == other.Y && Z == other.Z;
	}

	public override int GetHashCode()
	{
		int num = -307843816;
		num = num * -1521134295 + X.GetHashCode();
		num = num * -1521134295 + Y.GetHashCode();
		return num * -1521134295 + Z.GetHashCode();
	}

	public override string ToString()
	{
		return string.Format("{0}: {1}, {2}: {3}, {4}: {5}", "X", X, "Y", Y, "Z", Z);
	}

	public IntVector3 Subtract(int x, int y, int z)
	{
		return new IntVector3(X - x, Y - y, Z - z);
	}

	public static implicit operator Vector3(IntVector3 v)
	{
		return new Vector3(v.X, v.Y, v.Z);
	}

	public static bool operator ==(IntVector3 value1, IntVector3 value2)
	{
		return value1.X == value2.X && value1.Y == value2.Y && value1.Z == value2.Z;
	}

	public static bool operator !=(IntVector3 value1, IntVector3 value2)
	{
		return !(value1 == value2);
	}

	public static IntVector3 operator +(IntVector3 value1, IntVector3 value2)
	{
		value1.X += value2.X;
		value1.Y += value2.Y;
		value1.Z += value2.Z;
		return value1;
	}

	public static IntVector3 operator -(IntVector3 value1, IntVector3 value2)
	{
		value1.X -= value2.X;
		value1.Y -= value2.Y;
		value1.Z -= value2.Z;
		return value1;
	}
}
