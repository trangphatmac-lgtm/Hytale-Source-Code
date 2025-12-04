using System;
using System.Diagnostics;

namespace HytaleClient.Math;

[Serializable]
[DebuggerDisplay("{DebugDisplayString,nq}")]
public struct Quaternion : IEquatable<Quaternion>
{
	public float X;

	public float Y;

	public float Z;

	public float W;

	private static readonly Quaternion identity = new Quaternion(0f, 0f, 0f, 1f);

	public static Quaternion Identity => identity;

	internal string DebugDisplayString
	{
		get
		{
			if (this == Identity)
			{
				return "Identity";
			}
			return X + " " + Y + " " + Z + " " + W;
		}
	}

	public Quaternion(float x, float y, float z, float w)
	{
		X = x;
		Y = y;
		Z = z;
		W = w;
	}

	public Quaternion(Vector3 vectorPart, float scalarPart)
	{
		X = vectorPart.X;
		Y = vectorPart.Y;
		Z = vectorPart.Z;
		W = scalarPart;
	}

	public void Conjugate()
	{
		X = 0f - X;
		Y = 0f - Y;
		Z = 0f - Z;
	}

	public override bool Equals(object obj)
	{
		return obj is Quaternion && Equals((Quaternion)obj);
	}

	public bool Equals(Quaternion other)
	{
		return X == other.X && Y == other.Y && Z == other.Z && W == other.W;
	}

	public override int GetHashCode()
	{
		return X.GetHashCode() + Y.GetHashCode() + Z.GetHashCode() + W.GetHashCode();
	}

	public float Length()
	{
		float num = X * X + Y * Y + Z * Z + W * W;
		return (float)System.Math.Sqrt(num);
	}

	public float LengthSquared()
	{
		return X * X + Y * Y + Z * Z + W * W;
	}

	public void Normalize()
	{
		float num = 1f / (float)System.Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
		X *= num;
		Y *= num;
		Z *= num;
		W *= num;
	}

	public override string ToString()
	{
		return "{X:" + X + " Y:" + Y + " Z:" + Z + " W:" + W + "}";
	}

	public static Quaternion Add(Quaternion quaternion1, Quaternion quaternion2)
	{
		Add(ref quaternion1, ref quaternion2, out var result);
		return result;
	}

	public static void Add(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
	{
		result.X = quaternion1.X + quaternion2.X;
		result.Y = quaternion1.Y + quaternion2.Y;
		result.Z = quaternion1.Z + quaternion2.Z;
		result.W = quaternion1.W + quaternion2.W;
	}

	public static Quaternion Concatenate(Quaternion value1, Quaternion value2)
	{
		Concatenate(ref value1, ref value2, out var result);
		return result;
	}

	public static void Concatenate(ref Quaternion value1, ref Quaternion value2, out Quaternion result)
	{
		float x = value1.X;
		float y = value1.Y;
		float z = value1.Z;
		float w = value1.W;
		float x2 = value2.X;
		float y2 = value2.Y;
		float z2 = value2.Z;
		float w2 = value2.W;
		result.X = x2 * w + x * w2 + (y2 * z - z2 * y);
		result.Y = y2 * w + y * w2 + (z2 * x - x2 * z);
		result.Z = z2 * w + z * w2 + (x2 * y - y2 * x);
		result.W = w2 * w - (x2 * x + y2 * y + z2 * z);
	}

	public static Quaternion Conjugate(Quaternion value)
	{
		return new Quaternion(0f - value.X, 0f - value.Y, 0f - value.Z, value.W);
	}

	public static void Conjugate(ref Quaternion value, out Quaternion result)
	{
		result.X = 0f - value.X;
		result.Y = 0f - value.Y;
		result.Z = 0f - value.Z;
		result.W = value.W;
	}

	public static Quaternion CreateFromAxisAngle(Vector3 axis, float angle)
	{
		CreateFromAxisAngle(ref axis, angle, out var result);
		return result;
	}

	public static void CreateFromAxisAngle(ref Vector3 axis, float angle, out Quaternion result)
	{
		float num = angle * 0.5f;
		float num2 = (float)System.Math.Sin(num);
		float w = (float)System.Math.Cos(num);
		result.X = axis.X * num2;
		result.Y = axis.Y * num2;
		result.Z = axis.Z * num2;
		result.W = w;
	}

	public static Quaternion CreateFromRotationMatrix(Matrix matrix)
	{
		CreateFromRotationMatrix(ref matrix, out var result);
		return result;
	}

	public static void CreateFromRotationMatrix(ref Matrix matrix, out Quaternion result)
	{
		float num = matrix.M11 + matrix.M22 + matrix.M33;
		if (num > 0f)
		{
			float num2 = (float)System.Math.Sqrt(num + 1f);
			result.W = num2 * 0.5f;
			num2 = 0.5f / num2;
			result.X = (matrix.M23 - matrix.M32) * num2;
			result.Y = (matrix.M31 - matrix.M13) * num2;
			result.Z = (matrix.M12 - matrix.M21) * num2;
		}
		else if (matrix.M11 >= matrix.M22 && matrix.M11 >= matrix.M33)
		{
			float num2 = (float)System.Math.Sqrt(1f + matrix.M11 - matrix.M22 - matrix.M33);
			float num3 = 0.5f / num2;
			result.X = 0.5f * num2;
			result.Y = (matrix.M12 + matrix.M21) * num3;
			result.Z = (matrix.M13 + matrix.M31) * num3;
			result.W = (matrix.M23 - matrix.M32) * num3;
		}
		else if (matrix.M22 > matrix.M33)
		{
			float num2 = (float)System.Math.Sqrt(1f + matrix.M22 - matrix.M11 - matrix.M33);
			float num3 = 0.5f / num2;
			result.X = (matrix.M21 + matrix.M12) * num3;
			result.Y = 0.5f * num2;
			result.Z = (matrix.M32 + matrix.M23) * num3;
			result.W = (matrix.M31 - matrix.M13) * num3;
		}
		else
		{
			float num2 = (float)System.Math.Sqrt(1f + matrix.M33 - matrix.M11 - matrix.M22);
			float num3 = 0.5f / num2;
			result.X = (matrix.M31 + matrix.M13) * num3;
			result.Y = (matrix.M32 + matrix.M23) * num3;
			result.Z = 0.5f * num2;
			result.W = (matrix.M12 - matrix.M21) * num3;
		}
	}

	public static Quaternion CreateFromYawPitchRoll(float yaw, float pitch, float roll)
	{
		CreateFromYawPitchRoll(yaw, pitch, roll, out var result);
		return result;
	}

	public static void CreateFromYawPitchRoll(float yaw, float pitch, float roll, out Quaternion result)
	{
		float num = roll * 0.5f;
		float num2 = (float)System.Math.Sin(num);
		float num3 = (float)System.Math.Cos(num);
		float num4 = pitch * 0.5f;
		float num5 = (float)System.Math.Sin(num4);
		float num6 = (float)System.Math.Cos(num4);
		float num7 = yaw * 0.5f;
		float num8 = (float)System.Math.Sin(num7);
		float num9 = (float)System.Math.Cos(num7);
		result.X = num9 * num5 * num3 + num8 * num6 * num2;
		result.Y = num8 * num6 * num3 - num9 * num5 * num2;
		result.Z = num9 * num6 * num2 - num8 * num5 * num3;
		result.W = num9 * num6 * num3 + num8 * num5 * num2;
	}

	public static Quaternion Divide(Quaternion quaternion1, Quaternion quaternion2)
	{
		Divide(ref quaternion1, ref quaternion2, out var result);
		return result;
	}

	public static void Divide(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
	{
		float x = quaternion1.X;
		float y = quaternion1.Y;
		float z = quaternion1.Z;
		float w = quaternion1.W;
		float num = quaternion2.X * quaternion2.X + quaternion2.Y * quaternion2.Y + quaternion2.Z * quaternion2.Z + quaternion2.W * quaternion2.W;
		float num2 = 1f / num;
		float num3 = (0f - quaternion2.X) * num2;
		float num4 = (0f - quaternion2.Y) * num2;
		float num5 = (0f - quaternion2.Z) * num2;
		float num6 = quaternion2.W * num2;
		float num7 = y * num5 - z * num4;
		float num8 = z * num3 - x * num5;
		float num9 = x * num4 - y * num3;
		float num10 = x * num3 + y * num4 + z * num5;
		result.X = x * num6 + num3 * w + num7;
		result.Y = y * num6 + num4 * w + num8;
		result.Z = z * num6 + num5 * w + num9;
		result.W = w * num6 - num10;
	}

	public static float Dot(Quaternion quaternion1, Quaternion quaternion2)
	{
		return quaternion1.X * quaternion2.X + quaternion1.Y * quaternion2.Y + quaternion1.Z * quaternion2.Z + quaternion1.W * quaternion2.W;
	}

	public static void Dot(ref Quaternion quaternion1, ref Quaternion quaternion2, out float result)
	{
		result = quaternion1.X * quaternion2.X + quaternion1.Y * quaternion2.Y + quaternion1.Z * quaternion2.Z + quaternion1.W * quaternion2.W;
	}

	public static Quaternion Inverse(Quaternion quaternion)
	{
		Inverse(ref quaternion, out var result);
		return result;
	}

	public static void Inverse(ref Quaternion quaternion, out Quaternion result)
	{
		float num = quaternion.X * quaternion.X + quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z + quaternion.W * quaternion.W;
		float num2 = 1f / num;
		result.X = (0f - quaternion.X) * num2;
		result.Y = (0f - quaternion.Y) * num2;
		result.Z = (0f - quaternion.Z) * num2;
		result.W = quaternion.W * num2;
	}

	public static Quaternion Lerp(Quaternion quaternion1, Quaternion quaternion2, float amount)
	{
		Lerp(ref quaternion1, ref quaternion2, amount, out var result);
		return result;
	}

	public static void Lerp(ref Quaternion quaternion1, ref Quaternion quaternion2, float amount, out Quaternion result)
	{
		float num = 1f - amount;
		float num2 = quaternion1.X * quaternion2.X + quaternion1.Y * quaternion2.Y + quaternion1.Z * quaternion2.Z + quaternion1.W * quaternion2.W;
		if (num2 >= 0f)
		{
			result.X = num * quaternion1.X + amount * quaternion2.X;
			result.Y = num * quaternion1.Y + amount * quaternion2.Y;
			result.Z = num * quaternion1.Z + amount * quaternion2.Z;
			result.W = num * quaternion1.W + amount * quaternion2.W;
		}
		else
		{
			result.X = num * quaternion1.X - amount * quaternion2.X;
			result.Y = num * quaternion1.Y - amount * quaternion2.Y;
			result.Z = num * quaternion1.Z - amount * quaternion2.Z;
			result.W = num * quaternion1.W - amount * quaternion2.W;
		}
		float num3 = result.X * result.X + result.Y * result.Y + result.Z * result.Z + result.W * result.W;
		float num4 = 1f / (float)System.Math.Sqrt(num3);
		result.X *= num4;
		result.Y *= num4;
		result.Z *= num4;
		result.W *= num4;
	}

	public static Quaternion Slerp(Quaternion quaternion1, Quaternion quaternion2, float amount)
	{
		Slerp(ref quaternion1, ref quaternion2, amount, out var result);
		return result;
	}

	public static void Slerp(ref Quaternion quaternion1, ref Quaternion quaternion2, float amount, out Quaternion result)
	{
		float num = quaternion1.X * quaternion2.X + quaternion1.Y * quaternion2.Y + quaternion1.Z * quaternion2.Z + quaternion1.W * quaternion2.W;
		float num2 = 1f;
		if (num < 0f)
		{
			num2 = -1f;
			num = 0f - num;
		}
		float num3;
		float num4;
		if (num > 0.999999f)
		{
			num3 = 1f - amount;
			num4 = amount * num2;
		}
		else
		{
			float num5 = (float)System.Math.Acos(num);
			float num6 = (float)(1.0 / System.Math.Sin(num5));
			num3 = (float)System.Math.Sin((1f - amount) * num5) * num6;
			num4 = num2 * ((float)System.Math.Sin(amount * num5) * num6);
		}
		result.X = num3 * quaternion1.X + num4 * quaternion2.X;
		result.Y = num3 * quaternion1.Y + num4 * quaternion2.Y;
		result.Z = num3 * quaternion1.Z + num4 * quaternion2.Z;
		result.W = num3 * quaternion1.W + num4 * quaternion2.W;
	}

	public static Quaternion Subtract(Quaternion quaternion1, Quaternion quaternion2)
	{
		Subtract(ref quaternion1, ref quaternion2, out var result);
		return result;
	}

	public static void Subtract(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
	{
		result.X = quaternion1.X - quaternion2.X;
		result.Y = quaternion1.Y - quaternion2.Y;
		result.Z = quaternion1.Z - quaternion2.Z;
		result.W = quaternion1.W - quaternion2.W;
	}

	public static Quaternion Multiply(Quaternion quaternion1, Quaternion quaternion2)
	{
		Multiply(ref quaternion1, ref quaternion2, out var result);
		return result;
	}

	public static Quaternion Multiply(Quaternion quaternion1, float scaleFactor)
	{
		Multiply(ref quaternion1, scaleFactor, out var result);
		return result;
	}

	public static void Multiply(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
	{
		float x = quaternion1.X;
		float y = quaternion1.Y;
		float z = quaternion1.Z;
		float w = quaternion1.W;
		float x2 = quaternion2.X;
		float y2 = quaternion2.Y;
		float z2 = quaternion2.Z;
		float w2 = quaternion2.W;
		float num = y * z2 - z * y2;
		float num2 = z * x2 - x * z2;
		float num3 = x * y2 - y * x2;
		float num4 = x * x2 + y * y2 + z * z2;
		result.X = x * w2 + x2 * w + num;
		result.Y = y * w2 + y2 * w + num2;
		result.Z = z * w2 + z2 * w + num3;
		result.W = w * w2 - num4;
	}

	public static void Multiply(ref Quaternion quaternion1, float scaleFactor, out Quaternion result)
	{
		result.X = quaternion1.X * scaleFactor;
		result.Y = quaternion1.Y * scaleFactor;
		result.Z = quaternion1.Z * scaleFactor;
		result.W = quaternion1.W * scaleFactor;
	}

	public static Quaternion Negate(Quaternion quaternion)
	{
		return new Quaternion(0f - quaternion.X, 0f - quaternion.Y, 0f - quaternion.Z, 0f - quaternion.W);
	}

	public static void Negate(ref Quaternion quaternion, out Quaternion result)
	{
		result.X = 0f - quaternion.X;
		result.Y = 0f - quaternion.Y;
		result.Z = 0f - quaternion.Z;
		result.W = 0f - quaternion.W;
	}

	public static Quaternion Normalize(Quaternion quaternion)
	{
		Normalize(ref quaternion, out var result);
		return result;
	}

	public static void Normalize(ref Quaternion quaternion, out Quaternion result)
	{
		float num = 1f / (float)System.Math.Sqrt(quaternion.X * quaternion.X + quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z + quaternion.W * quaternion.W);
		result.X = quaternion.X * num;
		result.Y = quaternion.Y * num;
		result.Z = quaternion.Z * num;
		result.W = quaternion.W * num;
	}

	public static Quaternion operator +(Quaternion quaternion1, Quaternion quaternion2)
	{
		Add(ref quaternion1, ref quaternion2, out var result);
		return result;
	}

	public static Quaternion operator /(Quaternion quaternion1, Quaternion quaternion2)
	{
		Divide(ref quaternion1, ref quaternion2, out var result);
		return result;
	}

	public static bool operator ==(Quaternion quaternion1, Quaternion quaternion2)
	{
		return quaternion1.Equals(quaternion2);
	}

	public static bool operator !=(Quaternion quaternion1, Quaternion quaternion2)
	{
		return !quaternion1.Equals(quaternion2);
	}

	public static Quaternion operator *(Quaternion quaternion1, Quaternion quaternion2)
	{
		Multiply(ref quaternion1, ref quaternion2, out var result);
		return result;
	}

	public static Quaternion operator *(Quaternion quaternion1, float scaleFactor)
	{
		Multiply(ref quaternion1, scaleFactor, out var result);
		return result;
	}

	public static Quaternion operator -(Quaternion quaternion1, Quaternion quaternion2)
	{
		Subtract(ref quaternion1, ref quaternion2, out var result);
		return result;
	}

	public static Quaternion operator -(Quaternion quaternion)
	{
		Negate(ref quaternion, out var result);
		return result;
	}

	public static Quaternion CreateFromVectors(Vector3 source, Vector3 destination)
	{
		CreateFromVectors(ref source, ref destination, out var result);
		return result;
	}

	public static void CreateFromVectors(ref Vector3 source, ref Vector3 destination, out Quaternion result)
	{
		source.Normalize();
		destination.Normalize();
		float num = Vector3.Dot(source, destination);
		if (num >= 1f)
		{
			result.X = 0f;
			result.Y = 0f;
			result.Z = 0f;
			result.W = 1f;
		}
		else if (num <= -1f)
		{
			Vector3 axis = Vector3.Cross(Vector3.UnitX, source);
			if (axis.LengthSquared() == 0f)
			{
				axis = Vector3.Cross(Vector3.UnitY, source);
			}
			axis.Normalize();
			CreateFromAxisAngle(ref axis, (float)System.Math.PI, out result);
		}
		else
		{
			float num2 = (float)System.Math.Sqrt((1f + num) * 2f);
			Vector3 vector = Vector3.Cross(source, destination) / num2;
			result.X = vector.X;
			result.Y = vector.Y;
			result.Z = vector.Z;
			result.W = 0.5f * num2;
			result.Normalize();
		}
	}

	public static Quaternion CreateFromNormalizedVectors(Vector3 source, Vector3 destination)
	{
		CreateFromNormalizedVectors(ref source, ref destination, out var result);
		return result;
	}

	public static void CreateFromNormalizedVectors(ref Vector3 source, ref Vector3 destination, out Quaternion result)
	{
		float num = Vector3.Dot(source, destination);
		if (num >= 1f)
		{
			result.X = 0f;
			result.Y = 0f;
			result.Z = 0f;
			result.W = 1f;
		}
		else if (num <= -1f)
		{
			Vector3 axis = Vector3.Cross(Vector3.UnitX, source);
			if (axis.LengthSquared() == 0f)
			{
				axis = Vector3.Cross(Vector3.UnitY, source);
			}
			axis.Normalize();
			CreateFromAxisAngle(ref axis, (float)System.Math.PI, out result);
		}
		else
		{
			float num2 = (float)System.Math.Sqrt((1f + num) * 2f);
			Vector3 vector = Vector3.Cross(source, destination) / num2;
			result.X = vector.X;
			result.Y = vector.Y;
			result.Z = vector.Z;
			result.W = 0.5f * num2;
			result.Normalize();
		}
	}

	public static void CreateFromYaw(float yaw, out Quaternion result)
	{
		float num = yaw * 0.5f;
		float y = (float)System.Math.Sin(num);
		float w = (float)System.Math.Cos(num);
		result.X = 0f;
		result.Y = y;
		result.Z = 0f;
		result.W = w;
	}

	public static void CreateFromPitch(float pitch, out Quaternion result)
	{
		float num = pitch * 0.5f;
		float x = (float)System.Math.Sin(num);
		float w = (float)System.Math.Cos(num);
		result.X = x;
		result.Y = 0f;
		result.Z = 0f;
		result.W = w;
	}
}
