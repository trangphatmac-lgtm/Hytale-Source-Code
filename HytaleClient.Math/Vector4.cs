using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Coherent.UI.Binding;

namespace HytaleClient.Math;

[Serializable]
[CoherentType]
[DebuggerDisplay("{DebugDisplayString,nq}")]
public struct Vector4 : IEquatable<Vector4>
{
	[CoherentProperty("x")]
	public float X;

	[CoherentProperty("y")]
	public float Y;

	[CoherentProperty("z")]
	public float Z;

	[CoherentProperty("w")]
	public float W;

	private static Vector4 zero = default(Vector4);

	private static readonly Vector4 unit = new Vector4(1f, 1f, 1f, 1f);

	private static readonly Vector4 unitX = new Vector4(1f, 0f, 0f, 0f);

	private static readonly Vector4 unitY = new Vector4(0f, 1f, 0f, 0f);

	private static readonly Vector4 unitZ = new Vector4(0f, 0f, 1f, 0f);

	private static readonly Vector4 unitW = new Vector4(0f, 0f, 0f, 1f);

	public static Vector4 Zero => zero;

	public static Vector4 One => unit;

	public static Vector4 UnitX => unitX;

	public static Vector4 UnitY => unitY;

	public static Vector4 UnitZ => unitZ;

	public static Vector4 UnitW => unitW;

	internal string DebugDisplayString => X + " " + Y + " " + Z + " " + W;

	public Vector4(float x, float y, float z, float w)
	{
		X = x;
		Y = y;
		Z = z;
		W = w;
	}

	public Vector4(Vector2 value, float z, float w)
	{
		X = value.X;
		Y = value.Y;
		Z = z;
		W = w;
	}

	public Vector4(Vector3 value, float w)
	{
		X = value.X;
		Y = value.Y;
		Z = value.Z;
		W = w;
	}

	public Vector4(float value)
	{
		X = value;
		Y = value;
		Z = value;
		W = value;
	}

	public override bool Equals(object obj)
	{
		return obj is Vector4 && Equals((Vector4)obj);
	}

	public bool Equals(Vector4 other)
	{
		return X == other.X && Y == other.Y && Z == other.Z && W == other.W;
	}

	public override int GetHashCode()
	{
		return (int)(W + X + Y + Y);
	}

	public float Length()
	{
		DistanceSquared(ref this, ref zero, out var result);
		return (float)System.Math.Sqrt(result);
	}

	public float LengthSquared()
	{
		DistanceSquared(ref this, ref zero, out var result);
		return result;
	}

	public void Normalize()
	{
		Normalize(ref this, out this);
	}

	public override string ToString()
	{
		return "{X:" + X + " Y:" + Y + " Z:" + Z + " W:" + W + "}";
	}

	public static Vector4 Add(Vector4 value1, Vector4 value2)
	{
		value1.W += value2.W;
		value1.X += value2.X;
		value1.Y += value2.Y;
		value1.Z += value2.Z;
		return value1;
	}

	public static void Add(ref Vector4 value1, ref Vector4 value2, out Vector4 result)
	{
		result.W = value1.W + value2.W;
		result.X = value1.X + value2.X;
		result.Y = value1.Y + value2.Y;
		result.Z = value1.Z + value2.Z;
	}

	public static Vector4 Barycentric(Vector4 value1, Vector4 value2, Vector4 value3, float amount1, float amount2)
	{
		return new Vector4(MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2), MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2), MathHelper.Barycentric(value1.Z, value2.Z, value3.Z, amount1, amount2), MathHelper.Barycentric(value1.W, value2.W, value3.W, amount1, amount2));
	}

	public static void Barycentric(ref Vector4 value1, ref Vector4 value2, ref Vector4 value3, float amount1, float amount2, out Vector4 result)
	{
		result.X = MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2);
		result.Y = MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2);
		result.Z = MathHelper.Barycentric(value1.Z, value2.Z, value3.Z, amount1, amount2);
		result.W = MathHelper.Barycentric(value1.W, value2.W, value3.W, amount1, amount2);
	}

	public static Vector4 CatmullRom(Vector4 value1, Vector4 value2, Vector4 value3, Vector4 value4, float amount)
	{
		return new Vector4(MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount), MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount), MathHelper.CatmullRom(value1.Z, value2.Z, value3.Z, value4.Z, amount), MathHelper.CatmullRom(value1.W, value2.W, value3.W, value4.W, amount));
	}

	public static void CatmullRom(ref Vector4 value1, ref Vector4 value2, ref Vector4 value3, ref Vector4 value4, float amount, out Vector4 result)
	{
		result.X = MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount);
		result.Y = MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount);
		result.Z = MathHelper.CatmullRom(value1.Z, value2.Z, value3.Z, value4.Z, amount);
		result.W = MathHelper.CatmullRom(value1.W, value2.W, value3.W, value4.W, amount);
	}

	public static Vector4 Clamp(Vector4 value1, Vector4 min, Vector4 max)
	{
		return new Vector4(MathHelper.Clamp(value1.X, min.X, max.X), MathHelper.Clamp(value1.Y, min.Y, max.Y), MathHelper.Clamp(value1.Z, min.Z, max.Z), MathHelper.Clamp(value1.W, min.W, max.W));
	}

	public static void Clamp(ref Vector4 value1, ref Vector4 min, ref Vector4 max, out Vector4 result)
	{
		result.X = MathHelper.Clamp(value1.X, min.X, max.X);
		result.Y = MathHelper.Clamp(value1.Y, min.Y, max.Y);
		result.Z = MathHelper.Clamp(value1.Z, min.Z, max.Z);
		result.W = MathHelper.Clamp(value1.W, min.W, max.W);
	}

	public static float Distance(Vector4 value1, Vector4 value2)
	{
		return (float)System.Math.Sqrt(DistanceSquared(value1, value2));
	}

	public static void Distance(ref Vector4 value1, ref Vector4 value2, out float result)
	{
		result = (float)System.Math.Sqrt(DistanceSquared(value1, value2));
	}

	public static float DistanceSquared(Vector4 value1, Vector4 value2)
	{
		return (value1.W - value2.W) * (value1.W - value2.W) + (value1.X - value2.X) * (value1.X - value2.X) + (value1.Y - value2.Y) * (value1.Y - value2.Y) + (value1.Z - value2.Z) * (value1.Z - value2.Z);
	}

	public static void DistanceSquared(ref Vector4 value1, ref Vector4 value2, out float result)
	{
		result = (value1.W - value2.W) * (value1.W - value2.W) + (value1.X - value2.X) * (value1.X - value2.X) + (value1.Y - value2.Y) * (value1.Y - value2.Y) + (value1.Z - value2.Z) * (value1.Z - value2.Z);
	}

	public static Vector4 Divide(Vector4 value1, Vector4 value2)
	{
		value1.W /= value2.W;
		value1.X /= value2.X;
		value1.Y /= value2.Y;
		value1.Z /= value2.Z;
		return value1;
	}

	public static Vector4 Divide(Vector4 value1, float divider)
	{
		float num = 1f / divider;
		value1.W *= num;
		value1.X *= num;
		value1.Y *= num;
		value1.Z *= num;
		return value1;
	}

	public static void Divide(ref Vector4 value1, float divider, out Vector4 result)
	{
		float num = 1f / divider;
		result.W = value1.W * num;
		result.X = value1.X * num;
		result.Y = value1.Y * num;
		result.Z = value1.Z * num;
	}

	public static void Divide(ref Vector4 value1, ref Vector4 value2, out Vector4 result)
	{
		result.W = value1.W / value2.W;
		result.X = value1.X / value2.X;
		result.Y = value1.Y / value2.Y;
		result.Z = value1.Z / value2.Z;
	}

	public static float Dot(Vector4 vector1, Vector4 vector2)
	{
		return vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z + vector1.W * vector2.W;
	}

	public static void Dot(ref Vector4 vector1, ref Vector4 vector2, out float result)
	{
		result = vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z + vector1.W * vector2.W;
	}

	public static Vector4 Hermite(Vector4 value1, Vector4 tangent1, Vector4 value2, Vector4 tangent2, float amount)
	{
		return new Vector4(MathHelper.Hermite(value1.X, tangent1.X, value2.X, tangent2.X, amount), MathHelper.Hermite(value1.Y, tangent1.Y, value2.Y, tangent2.Y, amount), MathHelper.Hermite(value1.Z, tangent1.Z, value2.Z, tangent2.Z, amount), MathHelper.Hermite(value1.W, tangent1.W, value2.W, tangent2.W, amount));
	}

	public static void Hermite(ref Vector4 value1, ref Vector4 tangent1, ref Vector4 value2, ref Vector4 tangent2, float amount, out Vector4 result)
	{
		result.W = MathHelper.Hermite(value1.W, tangent1.W, value2.W, tangent2.W, amount);
		result.X = MathHelper.Hermite(value1.X, tangent1.X, value2.X, tangent2.X, amount);
		result.Y = MathHelper.Hermite(value1.Y, tangent1.Y, value2.Y, tangent2.Y, amount);
		result.Z = MathHelper.Hermite(value1.Z, tangent1.Z, value2.Z, tangent2.Z, amount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Lerp(Vector4 value1, Vector4 value2, float amount)
	{
		return new Vector4(MathHelper.Lerp(value1.X, value2.X, amount), MathHelper.Lerp(value1.Y, value2.Y, amount), MathHelper.Lerp(value1.Z, value2.Z, amount), MathHelper.Lerp(value1.W, value2.W, amount));
	}

	public static void Lerp(ref Vector4 value1, ref Vector4 value2, float amount, out Vector4 result)
	{
		result.X = MathHelper.Lerp(value1.X, value2.X, amount);
		result.Y = MathHelper.Lerp(value1.Y, value2.Y, amount);
		result.Z = MathHelper.Lerp(value1.Z, value2.Z, amount);
		result.W = MathHelper.Lerp(value1.W, value2.W, amount);
	}

	public static Vector4 Max(Vector4 value1, Vector4 value2)
	{
		return new Vector4(MathHelper.Max(value1.X, value2.X), MathHelper.Max(value1.Y, value2.Y), MathHelper.Max(value1.Z, value2.Z), MathHelper.Max(value1.W, value2.W));
	}

	public static void Max(ref Vector4 value1, ref Vector4 value2, out Vector4 result)
	{
		result.X = MathHelper.Max(value1.X, value2.X);
		result.Y = MathHelper.Max(value1.Y, value2.Y);
		result.Z = MathHelper.Max(value1.Z, value2.Z);
		result.W = MathHelper.Max(value1.W, value2.W);
	}

	public static Vector4 Min(Vector4 value1, Vector4 value2)
	{
		return new Vector4(MathHelper.Min(value1.X, value2.X), MathHelper.Min(value1.Y, value2.Y), MathHelper.Min(value1.Z, value2.Z), MathHelper.Min(value1.W, value2.W));
	}

	public static void Min(ref Vector4 value1, ref Vector4 value2, out Vector4 result)
	{
		result.X = MathHelper.Min(value1.X, value2.X);
		result.Y = MathHelper.Min(value1.Y, value2.Y);
		result.Z = MathHelper.Min(value1.Z, value2.Z);
		result.W = MathHelper.Min(value1.W, value2.W);
	}

	public static Vector4 Multiply(Vector4 value1, Vector4 value2)
	{
		value1.W *= value2.W;
		value1.X *= value2.X;
		value1.Y *= value2.Y;
		value1.Z *= value2.Z;
		return value1;
	}

	public static Vector4 Multiply(Vector4 value1, float scaleFactor)
	{
		value1.W *= scaleFactor;
		value1.X *= scaleFactor;
		value1.Y *= scaleFactor;
		value1.Z *= scaleFactor;
		return value1;
	}

	public static void Multiply(ref Vector4 value1, float scaleFactor, out Vector4 result)
	{
		result.W = value1.W * scaleFactor;
		result.X = value1.X * scaleFactor;
		result.Y = value1.Y * scaleFactor;
		result.Z = value1.Z * scaleFactor;
	}

	public static void Multiply(ref Vector4 value1, ref Vector4 value2, out Vector4 result)
	{
		result.W = value1.W * value2.W;
		result.X = value1.X * value2.X;
		result.Y = value1.Y * value2.Y;
		result.Z = value1.Z * value2.Z;
	}

	public static Vector4 Negate(Vector4 value)
	{
		value = new Vector4(0f - value.X, 0f - value.Y, 0f - value.Z, 0f - value.W);
		return value;
	}

	public static void Negate(ref Vector4 value, out Vector4 result)
	{
		result.X = 0f - value.X;
		result.Y = 0f - value.Y;
		result.Z = 0f - value.Z;
		result.W = 0f - value.W;
	}

	public static Vector4 Normalize(Vector4 vector)
	{
		Normalize(ref vector, out vector);
		return vector;
	}

	public static void Normalize(ref Vector4 vector, out Vector4 result)
	{
		DistanceSquared(ref vector, ref zero, out var result2);
		result2 = 1f / (float)System.Math.Sqrt(result2);
		result.W = vector.W * result2;
		result.X = vector.X * result2;
		result.Y = vector.Y * result2;
		result.Z = vector.Z * result2;
	}

	public static Vector4 SmoothStep(Vector4 value1, Vector4 value2, float amount)
	{
		return new Vector4(MathHelper.SmoothStep(value1.X, value2.X, amount), MathHelper.SmoothStep(value1.Y, value2.Y, amount), MathHelper.SmoothStep(value1.Z, value2.Z, amount), MathHelper.SmoothStep(value1.W, value2.W, amount));
	}

	public static void SmoothStep(ref Vector4 value1, ref Vector4 value2, float amount, out Vector4 result)
	{
		result.X = MathHelper.SmoothStep(value1.X, value2.X, amount);
		result.Y = MathHelper.SmoothStep(value1.Y, value2.Y, amount);
		result.Z = MathHelper.SmoothStep(value1.Z, value2.Z, amount);
		result.W = MathHelper.SmoothStep(value1.W, value2.W, amount);
	}

	public static Vector4 Subtract(Vector4 value1, Vector4 value2)
	{
		value1.W -= value2.W;
		value1.X -= value2.X;
		value1.Y -= value2.Y;
		value1.Z -= value2.Z;
		return value1;
	}

	public static void Subtract(ref Vector4 value1, ref Vector4 value2, out Vector4 result)
	{
		result.W = value1.W - value2.W;
		result.X = value1.X - value2.X;
		result.Y = value1.Y - value2.Y;
		result.Z = value1.Z - value2.Z;
	}

	public static Vector4 Transform(Vector2 position, Matrix matrix)
	{
		Transform(ref position, ref matrix, out var result);
		return result;
	}

	public static Vector4 Transform(Vector3 position, Matrix matrix)
	{
		Transform(ref position, ref matrix, out var result);
		return result;
	}

	public static Vector4 Transform(Vector4 vector, Matrix matrix)
	{
		Transform(ref vector, ref matrix, out vector);
		return vector;
	}

	public static void Transform(ref Vector2 position, ref Matrix matrix, out Vector4 result)
	{
		result = new Vector4(position.X * matrix.M11 + position.Y * matrix.M21 + matrix.M41, position.X * matrix.M12 + position.Y * matrix.M22 + matrix.M42, position.X * matrix.M13 + position.Y * matrix.M23 + matrix.M43, position.X * matrix.M14 + position.Y * matrix.M24 + matrix.M44);
	}

	public static void Transform(ref Vector3 position, ref Matrix matrix, out Vector4 result)
	{
		float x = position.X * matrix.M11 + position.Y * matrix.M21 + position.Z * matrix.M31 + matrix.M41;
		float y = position.X * matrix.M12 + position.Y * matrix.M22 + position.Z * matrix.M32 + matrix.M42;
		float z = position.X * matrix.M13 + position.Y * matrix.M23 + position.Z * matrix.M33 + matrix.M43;
		float w = position.X * matrix.M14 + position.Y * matrix.M24 + position.Z * matrix.M34 + matrix.M44;
		result.X = x;
		result.Y = y;
		result.Z = z;
		result.W = w;
	}

	public static void Transform(ref Vector4 vector, ref Matrix matrix, out Vector4 result)
	{
		float x = vector.X * matrix.M11 + vector.Y * matrix.M21 + vector.Z * matrix.M31 + vector.W * matrix.M41;
		float y = vector.X * matrix.M12 + vector.Y * matrix.M22 + vector.Z * matrix.M32 + vector.W * matrix.M42;
		float z = vector.X * matrix.M13 + vector.Y * matrix.M23 + vector.Z * matrix.M33 + vector.W * matrix.M43;
		float w = vector.X * matrix.M14 + vector.Y * matrix.M24 + vector.Z * matrix.M34 + vector.W * matrix.M44;
		result.X = x;
		result.Y = y;
		result.Z = z;
		result.W = w;
	}

	public static void Transform(Vector4[] sourceArray, ref Matrix matrix, Vector4[] destinationArray)
	{
		if (sourceArray == null)
		{
			throw new ArgumentNullException("sourceArray");
		}
		if (destinationArray == null)
		{
			throw new ArgumentNullException("destinationArray");
		}
		if (destinationArray.Length < sourceArray.Length)
		{
			throw new ArgumentException("destinationArray is too small to contain the result.");
		}
		for (int i = 0; i < sourceArray.Length; i++)
		{
			Transform(ref sourceArray[i], ref matrix, out destinationArray[i]);
		}
	}

	public static void Transform(Vector4[] sourceArray, int sourceIndex, ref Matrix matrix, Vector4[] destinationArray, int destinationIndex, int length)
	{
		if (sourceArray == null)
		{
			throw new ArgumentNullException("sourceArray");
		}
		if (destinationArray == null)
		{
			throw new ArgumentNullException("destinationArray");
		}
		if (destinationIndex + length > destinationArray.Length)
		{
			throw new ArgumentException("destinationArray is too small to contain the result.");
		}
		if (sourceIndex + length > sourceArray.Length)
		{
			throw new ArgumentException("The combination of sourceIndex and length was greater than sourceArray.Length.");
		}
		for (int i = 0; i < length; i++)
		{
			Transform(ref sourceArray[i + sourceIndex], ref matrix, out destinationArray[i + destinationIndex]);
		}
	}

	public static Vector4 Transform(Vector2 value, Quaternion rotation)
	{
		Transform(ref value, ref rotation, out var result);
		return result;
	}

	public static Vector4 Transform(Vector3 value, Quaternion rotation)
	{
		Transform(ref value, ref rotation, out var result);
		return result;
	}

	public static Vector4 Transform(Vector4 value, Quaternion rotation)
	{
		Transform(ref value, ref rotation, out var result);
		return result;
	}

	public static void Transform(ref Vector2 value, ref Quaternion rotation, out Vector4 result)
	{
		double num = rotation.X + rotation.X;
		double num2 = rotation.Y + rotation.Y;
		double num3 = rotation.Z + rotation.Z;
		double num4 = (double)rotation.W * num;
		double num5 = (double)rotation.W * num2;
		double num6 = (double)rotation.W * num3;
		double num7 = (double)rotation.X * num;
		double num8 = (double)rotation.X * num2;
		double num9 = (double)rotation.X * num3;
		double num10 = (double)rotation.Y * num2;
		double num11 = (double)rotation.Y * num3;
		double num12 = (double)rotation.Z * num3;
		result.X = (float)((double)value.X * (1.0 - num10 - num12) + (double)value.Y * (num8 - num6));
		result.Y = (float)((double)value.X * (num8 + num6) + (double)value.Y * (1.0 - num7 - num12));
		result.Z = (float)((double)value.X * (num9 - num5) + (double)value.Y * (num11 + num4));
		result.W = 1f;
	}

	public static void Transform(ref Vector3 value, ref Quaternion rotation, out Vector4 result)
	{
		double num = rotation.X + rotation.X;
		double num2 = rotation.Y + rotation.Y;
		double num3 = rotation.Z + rotation.Z;
		double num4 = (double)rotation.W * num;
		double num5 = (double)rotation.W * num2;
		double num6 = (double)rotation.W * num3;
		double num7 = (double)rotation.X * num;
		double num8 = (double)rotation.X * num2;
		double num9 = (double)rotation.X * num3;
		double num10 = (double)rotation.Y * num2;
		double num11 = (double)rotation.Y * num3;
		double num12 = (double)rotation.Z * num3;
		result.X = (float)((double)value.X * (1.0 - num10 - num12) + (double)value.Y * (num8 - num6) + (double)value.Z * (num9 + num5));
		result.Y = (float)((double)value.X * (num8 + num6) + (double)value.Y * (1.0 - num7 - num12) + (double)value.Z * (num11 - num4));
		result.Z = (float)((double)value.X * (num9 - num5) + (double)value.Y * (num11 + num4) + (double)value.Z * (1.0 - num7 - num10));
		result.W = 1f;
	}

	public static void Transform(ref Vector4 value, ref Quaternion rotation, out Vector4 result)
	{
		double num = rotation.X + rotation.X;
		double num2 = rotation.Y + rotation.Y;
		double num3 = rotation.Z + rotation.Z;
		double num4 = (double)rotation.W * num;
		double num5 = (double)rotation.W * num2;
		double num6 = (double)rotation.W * num3;
		double num7 = (double)rotation.X * num;
		double num8 = (double)rotation.X * num2;
		double num9 = (double)rotation.X * num3;
		double num10 = (double)rotation.Y * num2;
		double num11 = (double)rotation.Y * num3;
		double num12 = (double)rotation.Z * num3;
		result.X = (float)((double)value.X * (1.0 - num10 - num12) + (double)value.Y * (num8 - num6) + (double)value.Z * (num9 + num5));
		result.Y = (float)((double)value.X * (num8 + num6) + (double)value.Y * (1.0 - num7 - num12) + (double)value.Z * (num11 - num4));
		result.Z = (float)((double)value.X * (num9 - num5) + (double)value.Y * (num11 + num4) + (double)value.Z * (1.0 - num7 - num10));
		result.W = value.W;
	}

	public static void Transform(Vector4[] sourceArray, ref Quaternion rotation, Vector4[] destinationArray)
	{
		if (sourceArray == null)
		{
			throw new ArgumentException("sourceArray");
		}
		if (destinationArray == null)
		{
			throw new ArgumentException("destinationArray");
		}
		if (destinationArray.Length < sourceArray.Length)
		{
			throw new ArgumentException("destinationArray is too small to contain the result.");
		}
		for (int i = 0; i < sourceArray.Length; i++)
		{
			Transform(ref sourceArray[i], ref rotation, out destinationArray[i]);
		}
	}

	public static void Transform(Vector4[] sourceArray, int sourceIndex, ref Quaternion rotation, Vector4[] destinationArray, int destinationIndex, int length)
	{
		if (sourceArray == null)
		{
			throw new ArgumentException("sourceArray");
		}
		if (destinationArray == null)
		{
			throw new ArgumentException("destinationArray");
		}
		if (destinationIndex + length > destinationArray.Length)
		{
			throw new ArgumentException("destinationArray is too small to contain the result.");
		}
		if (sourceIndex + length > sourceArray.Length)
		{
			throw new ArgumentException("The combination of sourceIndex and length was greater than sourceArray.Length.");
		}
		for (int i = 0; i < length; i++)
		{
			Transform(ref sourceArray[i + sourceIndex], ref rotation, out destinationArray[i + destinationIndex]);
		}
	}

	public static Vector4 operator -(Vector4 value)
	{
		return new Vector4(0f - value.X, 0f - value.Y, 0f - value.Z, 0f - value.W);
	}

	public static bool operator ==(Vector4 value1, Vector4 value2)
	{
		return value1.X == value2.X && value1.Y == value2.Y && value1.Z == value2.Z && value1.W == value2.W;
	}

	public static bool operator !=(Vector4 value1, Vector4 value2)
	{
		return !(value1 == value2);
	}

	public static Vector4 operator +(Vector4 value1, Vector4 value2)
	{
		value1.W += value2.W;
		value1.X += value2.X;
		value1.Y += value2.Y;
		value1.Z += value2.Z;
		return value1;
	}

	public static Vector4 operator -(Vector4 value1, Vector4 value2)
	{
		value1.W -= value2.W;
		value1.X -= value2.X;
		value1.Y -= value2.Y;
		value1.Z -= value2.Z;
		return value1;
	}

	public static Vector4 operator *(Vector4 value1, Vector4 value2)
	{
		value1.W *= value2.W;
		value1.X *= value2.X;
		value1.Y *= value2.Y;
		value1.Z *= value2.Z;
		return value1;
	}

	public static Vector4 operator *(Vector4 value1, float scaleFactor)
	{
		value1.W *= scaleFactor;
		value1.X *= scaleFactor;
		value1.Y *= scaleFactor;
		value1.Z *= scaleFactor;
		return value1;
	}

	public static Vector4 operator *(float scaleFactor, Vector4 value1)
	{
		value1.W *= scaleFactor;
		value1.X *= scaleFactor;
		value1.Y *= scaleFactor;
		value1.Z *= scaleFactor;
		return value1;
	}

	public static Vector4 operator /(Vector4 value1, Vector4 value2)
	{
		value1.W /= value2.W;
		value1.X /= value2.X;
		value1.Y /= value2.Y;
		value1.Z /= value2.Z;
		return value1;
	}

	public static Vector4 operator /(Vector4 value1, float divider)
	{
		float num = 1f / divider;
		value1.W *= num;
		value1.X *= num;
		value1.Y *= num;
		value1.Z *= num;
		return value1;
	}

	public Vector4(Vector3 vector)
	{
		X = vector.X;
		Y = vector.Y;
		Z = vector.Z;
		W = 0f;
	}

	public Vector4(Vector2 vectorA, Vector2 vectorB)
	{
		X = vectorA.X;
		Y = vectorA.Y;
		Z = vectorB.X;
		W = vectorB.Y;
	}

	public bool IsInsideFrustum()
	{
		return System.Math.Abs(X) <= System.Math.Abs(W) && System.Math.Abs(Y) <= System.Math.Abs(W) && System.Math.Abs(Z) <= System.Math.Abs(W);
	}

	public Vector4 PerspectiveTransform()
	{
		float num = 1f / W;
		X *= num;
		Y *= num;
		Z *= num;
		W = 1f;
		return this;
	}

	public float Get(int index)
	{
		return index switch
		{
			0 => X, 
			1 => Y, 
			2 => Z, 
			3 => W, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}
}
