using System;
using System.Diagnostics;

namespace HytaleClient.Math;

[Serializable]
[DebuggerDisplay("{DebugDisplayString,nq}")]
public struct Rectangle : IEquatable<Rectangle>
{
	public int X;

	public int Y;

	public int Width;

	public int Height;

	private static Rectangle emptyRectangle = default(Rectangle);

	public int Left => X;

	public int Right => X + Width;

	public int Top => Y;

	public int Bottom => Y + Height;

	public Point Location
	{
		get
		{
			return new Point(X, Y);
		}
		set
		{
			X = value.X;
			Y = value.Y;
		}
	}

	public Point Center => new Point(X + Width / 2, Y + Height / 2);

	public bool IsEmpty => Width == 0 && Height == 0 && X == 0 && Y == 0;

	public static Rectangle Empty => emptyRectangle;

	internal string DebugDisplayString => X + " " + Y + " " + Width + " " + Height;

	public Rectangle(int x, int y, int width, int height)
	{
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}

	public bool Contains(int x, int y)
	{
		return X <= x && x < X + Width && Y <= y && y < Y + Height;
	}

	public bool Contains(Point value)
	{
		return X <= value.X && value.X < X + Width && Y <= value.Y && value.Y < Y + Height;
	}

	public bool Contains(Rectangle value)
	{
		return X <= value.X && value.X + value.Width <= X + Width && Y <= value.Y && value.Y + value.Height <= Y + Height;
	}

	public void Contains(ref Point value, out bool result)
	{
		result = X <= value.X && value.X < X + Width && Y <= value.Y && value.Y < Y + Height;
	}

	public void Contains(ref Rectangle value, out bool result)
	{
		result = X <= value.X && value.X + value.Width <= X + Width && Y <= value.Y && value.Y + value.Height <= Y + Height;
	}

	public void Offset(Point offset)
	{
		X += offset.X;
		Y += offset.Y;
	}

	public void Offset(int offsetX, int offsetY)
	{
		X += offsetX;
		Y += offsetY;
	}

	public void Inflate(int horizontalValue, int verticalValue)
	{
		X -= horizontalValue;
		Y -= verticalValue;
		Width += horizontalValue * 2;
		Height += verticalValue * 2;
	}

	public bool Equals(Rectangle other)
	{
		return this == other;
	}

	public override bool Equals(object obj)
	{
		return obj is Rectangle && this == (Rectangle)obj;
	}

	public override string ToString()
	{
		return "{X:" + X + " Y:" + Y + " Width:" + Width + " Height:" + Height + "}";
	}

	public override int GetHashCode()
	{
		return X ^ Y ^ Width ^ Height;
	}

	public bool Intersects(Rectangle value)
	{
		return value.Left < Right && Left < value.Right && value.Top < Bottom && Top < value.Bottom;
	}

	public void Intersects(ref Rectangle value, out bool result)
	{
		result = value.Left < Right && Left < value.Right && value.Top < Bottom && Top < value.Bottom;
	}

	public static bool operator ==(Rectangle a, Rectangle b)
	{
		return a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
	}

	public static bool operator !=(Rectangle a, Rectangle b)
	{
		return !(a == b);
	}

	public static Rectangle Intersect(Rectangle value1, Rectangle value2)
	{
		Intersect(ref value1, ref value2, out var result);
		return result;
	}

	public static void Intersect(ref Rectangle value1, ref Rectangle value2, out Rectangle result)
	{
		if (value1.Intersects(value2))
		{
			int num = System.Math.Min(value1.X + value1.Width, value2.X + value2.Width);
			int num2 = System.Math.Max(value1.X, value2.X);
			int num3 = System.Math.Max(value1.Y, value2.Y);
			int num4 = System.Math.Min(value1.Y + value1.Height, value2.Y + value2.Height);
			result = new Rectangle(num2, num3, num - num2, num4 - num3);
		}
		else
		{
			result = new Rectangle(0, 0, 0, 0);
		}
	}

	public static Rectangle Union(Rectangle value1, Rectangle value2)
	{
		int num = System.Math.Min(value1.X, value2.X);
		int num2 = System.Math.Min(value1.Y, value2.Y);
		return new Rectangle(num, num2, System.Math.Max(value1.Right, value2.Right) - num, System.Math.Max(value1.Bottom, value2.Bottom) - num2);
	}

	public static void Union(ref Rectangle value1, ref Rectangle value2, out Rectangle result)
	{
		result.X = System.Math.Min(value1.X, value2.X);
		result.Y = System.Math.Min(value1.Y, value2.Y);
		result.Width = System.Math.Max(value1.Right, value2.Right) - result.X;
		result.Height = System.Math.Max(value1.Bottom, value2.Bottom) - result.Y;
	}
}
