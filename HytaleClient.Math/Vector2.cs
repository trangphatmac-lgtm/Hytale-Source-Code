using System;
using System.Diagnostics;
using Coherent.UI.Binding;

namespace HytaleClient.Math;

[Serializable]
[CoherentType]
[DebuggerDisplay("{DebugDisplayString,nq}")]
public struct Vector2 : IEquatable<Vector2>
{
	[CoherentProperty("x")]
	public float X;

	[CoherentProperty("y")]
	public float Y;

	private static readonly Vector2 zeroVector = new Vector2(0f, 0f);

	private static readonly Vector2 unitVector = new Vector2(1f, 1f);

	private static readonly Vector2 unitXVector = new Vector2(1f, 0f);

	private static readonly Vector2 unitYVector = new Vector2(0f, 1f);

	private static readonly Vector2 nan = new Vector2(float.NaN, float.NaN);

	public static Vector2 Zero => zeroVector;

	public static Vector2 One => unitVector;

	public static Vector2 UnitX => unitXVector;

	public static Vector2 UnitY => unitYVector;

	internal string DebugDisplayString => X + " " + Y;

	public static Vector2 NaN => nan;

	public Vector2(float x, float y)
	{
		X = x;
		Y = y;
	}

	public Vector2(float value)
	{
		X = value;
		Y = value;
	}

	public override bool Equals(object obj)
	{
		return obj is Vector2 && Equals((Vector2)obj);
	}

	public bool Equals(Vector2 other)
	{
		return X == other.X && Y == other.Y;
	}

	public override int GetHashCode()
	{
		return X.GetHashCode() + Y.GetHashCode();
	}

	public float Length()
	{
		return (float)System.Math.Sqrt(X * X + Y * Y);
	}

	public float LengthSquared()
	{
		return X * X + Y * Y;
	}

	public void Normalize()
	{
		float num = 1f / (float)System.Math.Sqrt(X * X + Y * Y);
		X *= num;
		Y *= num;
	}

	public override string ToString()
	{
		return "{X:" + X + " Y:" + Y + "}";
	}

	public static Vector2 Add(Vector2 value1, Vector2 value2)
	{
		value1.X += value2.X;
		value1.Y += value2.Y;
		return value1;
	}

	public static void Add(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
	{
		result.X = value1.X + value2.X;
		result.Y = value1.Y + value2.Y;
	}

	public static Vector2 Barycentric(Vector2 value1, Vector2 value2, Vector2 value3, float amount1, float amount2)
	{
		return new Vector2(MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2), MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2));
	}

	public static void Barycentric(ref Vector2 value1, ref Vector2 value2, ref Vector2 value3, float amount1, float amount2, out Vector2 result)
	{
		result.X = MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2);
		result.Y = MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2);
	}

	public static Vector2 CatmullRom(Vector2 value1, Vector2 value2, Vector2 value3, Vector2 value4, float amount)
	{
		return new Vector2(MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount), MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount));
	}

	public static void CatmullRom(ref Vector2 value1, ref Vector2 value2, ref Vector2 value3, ref Vector2 value4, float amount, out Vector2 result)
	{
		result.X = MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount);
		result.Y = MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount);
	}

	public static Vector2 Clamp(Vector2 value1, Vector2 min, Vector2 max)
	{
		return new Vector2(MathHelper.Clamp(value1.X, min.X, max.X), MathHelper.Clamp(value1.Y, min.Y, max.Y));
	}

	public static void Clamp(ref Vector2 value1, ref Vector2 min, ref Vector2 max, out Vector2 result)
	{
		result.X = MathHelper.Clamp(value1.X, min.X, max.X);
		result.Y = MathHelper.Clamp(value1.Y, min.Y, max.Y);
	}

	public static float Distance(Vector2 value1, Vector2 value2)
	{
		float num = value1.X - value2.X;
		float num2 = value1.Y - value2.Y;
		return (float)System.Math.Sqrt(num * num + num2 * num2);
	}

	public static void Distance(ref Vector2 value1, ref Vector2 value2, out float result)
	{
		float num = value1.X - value2.X;
		float num2 = value1.Y - value2.Y;
		result = (float)System.Math.Sqrt(num * num + num2 * num2);
	}

	public static float DistanceSquared(Vector2 value1, Vector2 value2)
	{
		float num = value1.X - value2.X;
		float num2 = value1.Y - value2.Y;
		return num * num + num2 * num2;
	}

	public static void DistanceSquared(ref Vector2 value1, ref Vector2 value2, out float result)
	{
		float num = value1.X - value2.X;
		float num2 = value1.Y - value2.Y;
		result = num * num + num2 * num2;
	}

	public static Vector2 Divide(Vector2 value1, Vector2 value2)
	{
		value1.X /= value2.X;
		value1.Y /= value2.Y;
		return value1;
	}

	public static void Divide(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
	{
		result.X = value1.X / value2.X;
		result.Y = value1.Y / value2.Y;
	}

	public static Vector2 Divide(Vector2 value1, float divider)
	{
		float num = 1f / divider;
		value1.X *= num;
		value1.Y *= num;
		return value1;
	}

	public static void Divide(ref Vector2 value1, float divider, out Vector2 result)
	{
		float num = 1f / divider;
		result.X = value1.X * num;
		result.Y = value1.Y * num;
	}

	public static float Dot(Vector2 value1, Vector2 value2)
	{
		return value1.X * value2.X + value1.Y * value2.Y;
	}

	public static void Dot(ref Vector2 value1, ref Vector2 value2, out float result)
	{
		result = value1.X * value2.X + value1.Y * value2.Y;
	}

	public static Vector2 Hermite(Vector2 value1, Vector2 tangent1, Vector2 value2, Vector2 tangent2, float amount)
	{
		Vector2 result = default(Vector2);
		Hermite(ref value1, ref tangent1, ref value2, ref tangent2, amount, out result);
		return result;
	}

	public static void Hermite(ref Vector2 value1, ref Vector2 tangent1, ref Vector2 value2, ref Vector2 tangent2, float amount, out Vector2 result)
	{
		result.X = MathHelper.Hermite(value1.X, tangent1.X, value2.X, tangent2.X, amount);
		result.Y = MathHelper.Hermite(value1.Y, tangent1.Y, value2.Y, tangent2.Y, amount);
	}

	public static Vector2 Lerp(Vector2 value1, Vector2 value2, float amount)
	{
		return new Vector2(MathHelper.Lerp(value1.X, value2.X, amount), MathHelper.Lerp(value1.Y, value2.Y, amount));
	}

	public static void Lerp(ref Vector2 value1, ref Vector2 value2, float amount, out Vector2 result)
	{
		result.X = MathHelper.Lerp(value1.X, value2.X, amount);
		result.Y = MathHelper.Lerp(value1.Y, value2.Y, amount);
	}

	public static Vector2 Max(Vector2 value1, Vector2 value2)
	{
		return new Vector2((value1.X > value2.X) ? value1.X : value2.X, (value1.Y > value2.Y) ? value1.Y : value2.Y);
	}

	public static void Max(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
	{
		result.X = ((value1.X > value2.X) ? value1.X : value2.X);
		result.Y = ((value1.Y > value2.Y) ? value1.Y : value2.Y);
	}

	public static Vector2 Min(Vector2 value1, Vector2 value2)
	{
		return new Vector2((value1.X < value2.X) ? value1.X : value2.X, (value1.Y < value2.Y) ? value1.Y : value2.Y);
	}

	public static void Min(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
	{
		result.X = ((value1.X < value2.X) ? value1.X : value2.X);
		result.Y = ((value1.Y < value2.Y) ? value1.Y : value2.Y);
	}

	public static Vector2 Multiply(Vector2 value1, Vector2 value2)
	{
		value1.X *= value2.X;
		value1.Y *= value2.Y;
		return value1;
	}

	public static Vector2 Multiply(Vector2 value1, float scaleFactor)
	{
		value1.X *= scaleFactor;
		value1.Y *= scaleFactor;
		return value1;
	}

	public static void Multiply(ref Vector2 value1, float scaleFactor, out Vector2 result)
	{
		result.X = value1.X * scaleFactor;
		result.Y = value1.Y * scaleFactor;
	}

	public static void Multiply(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
	{
		result.X = value1.X * value2.X;
		result.Y = value1.Y * value2.Y;
	}

	public static Vector2 Negate(Vector2 value)
	{
		value.X = 0f - value.X;
		value.Y = 0f - value.Y;
		return value;
	}

	public static void Negate(ref Vector2 value, out Vector2 result)
	{
		result.X = 0f - value.X;
		result.Y = 0f - value.Y;
	}

	public static Vector2 Normalize(Vector2 value)
	{
		float num = 1f / (float)System.Math.Sqrt(value.X * value.X + value.Y * value.Y);
		value.X *= num;
		value.Y *= num;
		return value;
	}

	public static void Normalize(ref Vector2 value, out Vector2 result)
	{
		float num = 1f / (float)System.Math.Sqrt(value.X * value.X + value.Y * value.Y);
		result.X = value.X * num;
		result.Y = value.Y * num;
	}

	public static Vector2 Reflect(Vector2 vector, Vector2 normal)
	{
		float num = 2f * (vector.X * normal.X + vector.Y * normal.Y);
		Vector2 result = default(Vector2);
		result.X = vector.X - normal.X * num;
		result.Y = vector.Y - normal.Y * num;
		return result;
	}

	public static void Reflect(ref Vector2 vector, ref Vector2 normal, out Vector2 result)
	{
		float num = 2f * (vector.X * normal.X + vector.Y * normal.Y);
		result.X = vector.X - normal.X * num;
		result.Y = vector.Y - normal.Y * num;
	}

	public static Vector2 SmoothStep(Vector2 value1, Vector2 value2, float amount)
	{
		return new Vector2(MathHelper.SmoothStep(value1.X, value2.X, amount), MathHelper.SmoothStep(value1.Y, value2.Y, amount));
	}

	public static void SmoothStep(ref Vector2 value1, ref Vector2 value2, float amount, out Vector2 result)
	{
		result.X = MathHelper.SmoothStep(value1.X, value2.X, amount);
		result.Y = MathHelper.SmoothStep(value1.Y, value2.Y, amount);
	}

	public static Vector2 Subtract(Vector2 value1, Vector2 value2)
	{
		value1.X -= value2.X;
		value1.Y -= value2.Y;
		return value1;
	}

	public static void Subtract(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
	{
		result.X = value1.X - value2.X;
		result.Y = value1.Y - value2.Y;
	}

	public static Vector2 Transform(Vector2 position, Matrix matrix)
	{
		return new Vector2(position.X * matrix.M11 + position.Y * matrix.M21 + matrix.M41, position.X * matrix.M12 + position.Y * matrix.M22 + matrix.M42);
	}

	public static void Transform(ref Vector2 position, ref Matrix matrix, out Vector2 result)
	{
		float x = position.X * matrix.M11 + position.Y * matrix.M21 + matrix.M41;
		float y = position.X * matrix.M12 + position.Y * matrix.M22 + matrix.M42;
		result.X = x;
		result.Y = y;
	}

	public static Vector2 Transform(Vector2 value, Quaternion rotation)
	{
		Transform(ref value, ref rotation, out value);
		return value;
	}

	public static void Transform(ref Vector2 value, ref Quaternion rotation, out Vector2 result)
	{
		float num = 2f * (0f - rotation.Z * value.Y);
		float num2 = 2f * (rotation.Z * value.X);
		float num3 = 2f * (rotation.X * value.Y - rotation.Y * value.X);
		result.X = value.X + num * rotation.W + (rotation.Y * num3 - rotation.Z * num2);
		result.Y = value.Y + num2 * rotation.W + (rotation.Z * num - rotation.X * num3);
	}

	public static void Transform(Vector2[] sourceArray, ref Matrix matrix, Vector2[] destinationArray)
	{
		Transform(sourceArray, 0, ref matrix, destinationArray, 0, sourceArray.Length);
	}

	public static void Transform(Vector2[] sourceArray, int sourceIndex, ref Matrix matrix, Vector2[] destinationArray, int destinationIndex, int length)
	{
		for (int i = 0; i < length; i++)
		{
			Vector2 vector = sourceArray[sourceIndex + i];
			Vector2 vector2 = destinationArray[destinationIndex + i];
			vector2.X = vector.X * matrix.M11 + vector.Y * matrix.M21 + matrix.M41;
			vector2.Y = vector.X * matrix.M12 + vector.Y * matrix.M22 + matrix.M42;
			destinationArray[destinationIndex + i] = vector2;
		}
	}

	public static void Transform(Vector2[] sourceArray, ref Quaternion rotation, Vector2[] destinationArray)
	{
		Transform(sourceArray, 0, ref rotation, destinationArray, 0, sourceArray.Length);
	}

	public static void Transform(Vector2[] sourceArray, int sourceIndex, ref Quaternion rotation, Vector2[] destinationArray, int destinationIndex, int length)
	{
		for (int i = 0; i < length; i++)
		{
			Vector2 value = sourceArray[sourceIndex + i];
			Transform(ref value, ref rotation, out var result);
			destinationArray[destinationIndex + i] = result;
		}
	}

	public static Vector2 TransformNormal(Vector2 normal, Matrix matrix)
	{
		return new Vector2(normal.X * matrix.M11 + normal.Y * matrix.M21, normal.X * matrix.M12 + normal.Y * matrix.M22);
	}

	public static void TransformNormal(ref Vector2 normal, ref Matrix matrix, out Vector2 result)
	{
		float x = normal.X * matrix.M11 + normal.Y * matrix.M21;
		float y = normal.X * matrix.M12 + normal.Y * matrix.M22;
		result.X = x;
		result.Y = y;
	}

	public static void TransformNormal(Vector2[] sourceArray, ref Matrix matrix, Vector2[] destinationArray)
	{
		TransformNormal(sourceArray, 0, ref matrix, destinationArray, 0, sourceArray.Length);
	}

	public static void TransformNormal(Vector2[] sourceArray, int sourceIndex, ref Matrix matrix, Vector2[] destinationArray, int destinationIndex, int length)
	{
		Vector2 vector2 = default(Vector2);
		for (int i = 0; i < length; i++)
		{
			Vector2 vector = sourceArray[sourceIndex + i];
			vector2.X = vector.X * matrix.M11 + vector.Y * matrix.M21;
			vector2.Y = vector.X * matrix.M12 + vector.Y * matrix.M22;
			destinationArray[destinationIndex + i] = vector2;
		}
	}

	public static Vector2 operator -(Vector2 value)
	{
		value.X = 0f - value.X;
		value.Y = 0f - value.Y;
		return value;
	}

	public static bool operator ==(Vector2 value1, Vector2 value2)
	{
		return value1.X == value2.X && value1.Y == value2.Y;
	}

	public static bool operator !=(Vector2 value1, Vector2 value2)
	{
		return !(value1 == value2);
	}

	public static Vector2 operator +(Vector2 value1, Vector2 value2)
	{
		value1.X += value2.X;
		value1.Y += value2.Y;
		return value1;
	}

	public static Vector2 operator -(Vector2 value1, Vector2 value2)
	{
		value1.X -= value2.X;
		value1.Y -= value2.Y;
		return value1;
	}

	public static Vector2 operator *(Vector2 value1, Vector2 value2)
	{
		value1.X *= value2.X;
		value1.Y *= value2.Y;
		return value1;
	}

	public static Vector2 operator *(Vector2 value, float scaleFactor)
	{
		value.X *= scaleFactor;
		value.Y *= scaleFactor;
		return value;
	}

	public static Vector2 operator *(float scaleFactor, Vector2 value)
	{
		value.X *= scaleFactor;
		value.Y *= scaleFactor;
		return value;
	}

	public static Vector2 operator /(Vector2 value1, Vector2 value2)
	{
		value1.X /= value2.X;
		value1.Y /= value2.Y;
		return value1;
	}

	public static Vector2 operator /(Vector2 value1, float divider)
	{
		float num = 1f / divider;
		value1.X *= num;
		value1.Y *= num;
		return value1;
	}

	public static Vector2 Spline(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
	{
		Vector2 result = default(Vector2);
		Spline(ref t, ref p0, ref p1, ref p2, ref p3, out result);
		return result;
	}

	public static void Spline(ref float t, ref Vector2 p0, ref Vector2 p1, ref Vector2 p2, ref Vector2 p3, out Vector2 result)
	{
		result.X = MathHelper.Spline(t, p0.X, p1.X, p2.X, p3.X);
		result.Y = MathHelper.Spline(t, p0.Y, p1.Y, p2.Y, p3.Y);
	}

	public static Vector2 Slerp(Vector2 p0, Vector2 p1, float t)
	{
		Vector2 result = default(Vector2);
		Slerp(ref p0, ref p1, ref t, out result);
		return result;
	}

	public static void Slerp(ref Vector2 p0, ref Vector2 p1, ref float t, out Vector2 result)
	{
		result.X = MathHelper.Slerp(p0.X, p1.X, t);
		result.Y = MathHelper.Slerp(p0.Y, p1.Y, t);
	}

	public bool IsNaN()
	{
		return float.IsNaN(X) && float.IsNaN(Y);
	}
}
