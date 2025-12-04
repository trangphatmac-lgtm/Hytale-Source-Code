using System;
using System.Runtime.CompilerServices;
using HytaleClient.Protocol;

namespace HytaleClient.Math;

public static class MathHelper
{
	public const float E = (float)System.Math.E;

	public const float Log10E = 0.4342945f;

	public const float Log2E = 1.442695f;

	public const float Pi = (float)System.Math.PI;

	public const float PiOver2 = (float)System.Math.PI / 2f;

	public const float PiOver4 = (float)System.Math.PI / 4f;

	public const float TwoPi = (float)System.Math.PI * 2f;

	internal static readonly float MachineEpsilonFloat = GetMachineEpsilonFloat();

	public const float TwoPiOver3 = (float)System.Math.PI * 2f / 3f;

	public static readonly int HashOne = Hash(1);

	public static readonly int HashTwo = Hash(2);

	public static readonly int HashThree = Hash(3);

	public static readonly int HashFour = Hash(4);

	public static readonly int HashFive = Hash(5);

	public static float Barycentric(float value1, float value2, float value3, float amount1, float amount2)
	{
		return value1 + (value2 - value1) * amount1 + (value3 - value1) * amount2;
	}

	public static float CatmullRom(float value1, float value2, float value3, float value4, float amount)
	{
		double num = amount * amount;
		double num2 = num * (double)amount;
		return (float)(0.5 * (2.0 * (double)value2 + (double)((value3 - value1) * amount) + (2.0 * (double)value1 - 5.0 * (double)value2 + 4.0 * (double)value3 - (double)value4) * num + (3.0 * (double)value2 - (double)value1 - 3.0 * (double)value3 + (double)value4) * num2));
	}

	public static float Clamp(float value, float min, float max)
	{
		value = ((value > max) ? max : value);
		value = ((value < min) ? min : value);
		return value;
	}

	public static float Distance(float value1, float value2)
	{
		return System.Math.Abs(value1 - value2);
	}

	public static float Hermite(float value1, float tangent1, float value2, float tangent2, float amount)
	{
		double num = value1;
		double num2 = value2;
		double num3 = tangent1;
		double num4 = tangent2;
		double num5 = amount;
		double num6 = num5 * num5 * num5;
		double num7 = num5 * num5;
		double num8 = (WithinEpsilon(amount, 0f) ? ((double)value1) : ((!WithinEpsilon(amount, 1f)) ? ((2.0 * num - 2.0 * num2 + num4 + num3) * num6 + (3.0 * num2 - 3.0 * num - 2.0 * num3 - num4) * num7 + num3 * num5 + num) : ((double)value2)));
		return (float)num8;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Lerp(float value1, float value2, float amount)
	{
		return value1 + (value2 - value1) * amount;
	}

	public static float Max(float value1, float value2)
	{
		return (value1 > value2) ? value1 : value2;
	}

	public static float Min(float value1, float value2)
	{
		return (value1 < value2) ? value1 : value2;
	}

	public static float Min(float v, float a, float c)
	{
		if (a < v)
		{
			v = a;
		}
		if (c < v)
		{
			v = c;
		}
		return v;
	}

	public static float Max(float v, float a, float b)
	{
		if (a > v)
		{
			v = a;
		}
		if (b > v)
		{
			v = b;
		}
		return v;
	}

	public static float SmoothStep(float value1, float value2, float amount)
	{
		float amount2 = Clamp(amount, 0f, 1f);
		return Hermite(value1, 0f, value2, 0f, amount2);
	}

	public static float Sin(float a)
	{
		return (float)System.Math.Sin(a);
	}

	public static float Cos(float a)
	{
		return (float)System.Math.Cos(a);
	}

	public static float ToDegrees(float radians)
	{
		return (float)((double)radians * (180.0 / System.Math.PI));
	}

	public static float ToRadians(float degrees)
	{
		return (float)((double)degrees * (System.Math.PI / 180.0));
	}

	public static float WrapAngle(float angle)
	{
		angle = (float)System.Math.IEEERemainder(angle, 6.2831854820251465);
		if (angle <= -(float)System.Math.PI)
		{
			angle += (float)System.Math.PI * 2f;
		}
		else if (angle > (float)System.Math.PI)
		{
			angle -= (float)System.Math.PI * 2f;
		}
		return angle;
	}

	internal static int Clamp(int value, int min, int max)
	{
		value = ((value > max) ? max : value);
		value = ((value < min) ? min : value);
		return value;
	}

	internal static bool WithinEpsilon(float floatA, float floatB)
	{
		return System.Math.Abs(floatA - floatB) < MachineEpsilonFloat;
	}

	internal static int ClosestMSAAPower(int value)
	{
		if (value == 1)
		{
			return 0;
		}
		int num = value - 1;
		num |= num >> 1;
		num |= num >> 2;
		num |= num >> 4;
		num |= num >> 8;
		num |= num >> 16;
		num++;
		if (num == value)
		{
			return num;
		}
		return num >> 1;
	}

	private static float GetMachineEpsilonFloat()
	{
		float num = 1f;
		float num2;
		do
		{
			num *= 0.5f;
			num2 = 1f + num;
		}
		while (num2 > 1f);
		return num;
	}

	public static int Round(float value)
	{
		return (int)System.Math.Round(value, MidpointRounding.AwayFromZero);
	}

	public static double FloorMod(double x, double y)
	{
		return x - System.Math.Floor(x / y) * y;
	}

	public static float Step(float value, float goal, float step)
	{
		if (value < goal)
		{
			return System.Math.Min(value + step, goal);
		}
		if (value > goal)
		{
			return System.Math.Max(value - step, goal);
		}
		return value;
	}

	public static float ShortAngleDistance(float value1, float value2)
	{
		float num = (value2 - value1) % ((float)System.Math.PI * 2f);
		return 2f * num % ((float)System.Math.PI * 2f) - num;
	}

	public static float LerpAngle(float value1, float value2, float amount)
	{
		return value1 + ShortAngleDistance(value1, value2) * amount;
	}

	public static float SnapRadianTo90Degrees(float radian)
	{
		return (float)(1.5707963705062866 * System.Math.Round(radian / ((float)System.Math.PI / 2f)));
	}

	public static int WrapAngleDegrees(int angle)
	{
		angle = ((angle + 180) % 360 + 360) % 360;
		return angle - 180;
	}

	public static double CompareAngle(double a, double b)
	{
		double num = b - a;
		return FloorMod(num + 3.1415927410125732, 6.2831854820251465) - 3.1415927410125732;
	}

	public static uint HashUnsigned(uint value)
	{
		value = ((value >> 16) ^ value) * 73244475;
		value = ((value >> 16) ^ value) * 73244475;
		value = (value >> 16) ^ value;
		return value;
	}

	public static int Hash(int value)
	{
		return (int)HashUnsigned((uint)value);
	}

	public static uint HashUnsigned(uint x, uint y, uint z)
	{
		uint num = x;
		num = ((HashUnsigned(x) >> 16) ^ num) * 73244475;
		num = ((HashUnsigned(y) >> 16) ^ num) * 73244475;
		num = (HashUnsigned(z) >> 16) ^ num;
		return HashUnsigned(num);
	}

	public static int Hash(int x, int y, int z)
	{
		return (int)HashUnsigned((uint)x, (uint)y, (uint)z);
	}

	public static uint HashUnsigned(uint x, uint z)
	{
		uint num = x;
		num = ((HashUnsigned(x) >> 16) ^ num) * 73244475;
		num = (HashUnsigned(z) >> 16) ^ num;
		return HashUnsigned(num);
	}

	public static int Hash(int x, int z)
	{
		return (int)HashUnsigned((uint)x, (uint)z);
	}

	public static float Spline(float t, float before, float start, float end, float after)
	{
		return 0.5f * (2f * start + (0f - before + end) * t + (2f * before - 5f * start + 4f * end - after) * (t * t) + (0f - before + 3f * start - 3f * end + after) * (t * t * t));
	}

	public static float CubicBezierCurve(float t, float start, float control0, float control1, float end)
	{
		return start + ((0f - start) * 3f + t * (3f * start - start * t)) * t + (3f * control0 + t * (-6f * control0 + control0 * 3f * t)) * t + (control1 * 3f - control1 * 3f * t) * t * t + end * t * t * t;
	}

	public static float CubicBezierCurveTangent(float t, float start, float control0, float control1, float end)
	{
		return 3f * (end - 3f * control1 + 3f * control0 - start) * t * t + (2f * (3f * control1) - 6f * control0 + 3f * start * t) + 3f * control0 - 3f * start;
	}

	public static void RotateAroundPoint(ref int x, ref int y, float radians, int originX, int originY)
	{
		double num = System.Math.Cos(radians);
		double num2 = System.Math.Sin(radians);
		int num3 = x - originX;
		int num4 = y - originY;
		x = (int)(num * (double)num3 - num2 * (double)num4 + (double)originX);
		y = (int)(num2 * (double)num3 + num * (double)num4 + (double)originY);
	}

	public static float SplineAngle(float t, float p0, float p1, float p2, float p3)
	{
		if (p1 - p0 > (float)System.Math.PI)
		{
			p1 -= (float)System.Math.PI * 2f;
		}
		else if (p1 - p0 < -(float)System.Math.PI)
		{
			p1 += (float)System.Math.PI * 2f;
		}
		if (p2 - p1 > (float)System.Math.PI)
		{
			p2 -= (float)System.Math.PI * 2f;
			p3 -= (float)System.Math.PI * 2f;
		}
		else if (p2 - p1 < -(float)System.Math.PI)
		{
			p2 += (float)System.Math.PI * 2f;
			p3 += (float)System.Math.PI * 2f;
		}
		if (p3 - p2 > (float)System.Math.PI)
		{
			p3 -= (float)System.Math.PI * 2f;
		}
		else if (p3 - p2 < -(float)System.Math.PI)
		{
			p3 += (float)System.Math.PI * 2f;
		}
		return Spline(t, p0, p1, p2, p3);
	}

	public static float SnapValue(float value, float interval)
	{
		float num = value % interval;
		if (num == 0f)
		{
			return value;
		}
		value -= num;
		if (num * 2f >= interval)
		{
			value += interval;
		}
		else if (num * 2f < 0f - interval)
		{
			value -= interval;
		}
		return value;
	}

	public static float Slerp(float a, float b, float t)
	{
		return Lerp(a, b, GetSlerpRatio(t));
	}

	public static float GetSlerpRatio(float t)
	{
		return (float)((System.Math.Sin(t * (float)System.Math.PI - (float)System.Math.PI / 2f) + 1.0) / 2.0);
	}

	public static bool IsPowerOfTwo(int x)
	{
		return (x & (x - 1)) == 0;
	}

	public static float RotationToRadians(Rotation rotation)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return (float)rotation * ((float)System.Math.PI / 2f);
	}

	public static int RotationToDegrees(Rotation rotation)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected I4, but got Unknown
		return rotation * 90;
	}

	public static uint NumberOfLeadingZeros(uint i)
	{
		if (i == 0)
		{
			return 32u;
		}
		uint num = 1u;
		if (i >> 16 == 0)
		{
			num += 16;
			i <<= 16;
		}
		if (i >> 24 == 0)
		{
			num += 8;
			i <<= 8;
		}
		if (i >> 28 == 0)
		{
			num += 4;
			i <<= 4;
		}
		if (i >> 30 == 0)
		{
			num += 2;
			i <<= 2;
		}
		return num - (i >> 31);
	}

	public static float Square(float value)
	{
		return value * value;
	}

	public static void ComputeScreenArea(Vector3 position, Vector3 extent, ref Matrix viewProjectionMatrix, out Vector4 screenArea)
	{
		Vector4 vector = new Vector4(position + new Vector3(extent.X, 0f, 0f), 1f);
		Vector4 vector2 = new Vector4(position + new Vector3(extent.X, extent.Y, 0f), 1f);
		Vector4 vector3 = new Vector4(position + new Vector3(extent.X, extent.Y, extent.Z), 1f);
		Vector4 vector4 = new Vector4(position + new Vector3(extent.X, 0f, extent.Z), 1f);
		Vector4 vector5 = new Vector4(position + new Vector3(0f, 0f, 0f), 1f);
		Vector4 vector6 = new Vector4(position + new Vector3(0f, extent.Y, 0f), 1f);
		Vector4 vector7 = new Vector4(position + new Vector3(0f, extent.Y, extent.Z), 1f);
		Vector4 vector8 = new Vector4(position + new Vector3(0f, 0f, extent.Z), 1f);
		Vector4.Transform(ref vector, ref viewProjectionMatrix, out var result);
		Vector4.Transform(ref vector2, ref viewProjectionMatrix, out var result2);
		Vector4.Transform(ref vector3, ref viewProjectionMatrix, out var result3);
		Vector4.Transform(ref vector4, ref viewProjectionMatrix, out var result4);
		Vector4.Transform(ref vector5, ref viewProjectionMatrix, out var result5);
		Vector4.Transform(ref vector6, ref viewProjectionMatrix, out var result6);
		Vector4.Transform(ref vector7, ref viewProjectionMatrix, out var result7);
		Vector4.Transform(ref vector8, ref viewProjectionMatrix, out var result8);
		result.X /= result.W;
		result.Y /= result.W;
		result2.X /= result2.W;
		result2.Y /= result2.W;
		result3.X /= result3.W;
		result3.Y /= result3.W;
		result4.X /= result4.W;
		result4.Y /= result4.W;
		result5.X /= result5.W;
		result5.Y /= result5.W;
		result6.X /= result6.W;
		result6.Y /= result6.W;
		result7.X /= result7.W;
		result7.Y /= result7.W;
		result8.X /= result8.W;
		result8.Y /= result8.W;
		screenArea.X = System.Math.Min(result.X, System.Math.Min(result2.X, System.Math.Min(result3.X, System.Math.Min(result4.X, System.Math.Min(result5.X, System.Math.Min(result6.X, System.Math.Min(result7.X, result8.X)))))));
		screenArea.Y = System.Math.Min(result.Y, System.Math.Min(result2.Y, System.Math.Min(result3.Y, System.Math.Min(result4.Y, System.Math.Min(result5.Y, System.Math.Min(result6.Y, System.Math.Min(result7.Y, result8.Y)))))));
		screenArea.Z = System.Math.Max(result.X, System.Math.Max(result2.X, System.Math.Max(result3.X, System.Math.Max(result4.X, System.Math.Max(result5.X, System.Math.Max(result6.X, System.Math.Max(result7.X, result8.X)))))));
		screenArea.W = System.Math.Max(result.Y, System.Math.Max(result2.Y, System.Math.Max(result3.Y, System.Math.Max(result4.Y, System.Math.Max(result5.Y, System.Math.Max(result6.Y, System.Math.Max(result7.Y, result8.Y)))))));
		Vector4 vector9 = new Vector4(0.5f);
		screenArea = screenArea * vector9 + vector9;
	}

	public static float ConvertToNewRange(float value, float oldMinRange, float oldMaxRange, float newMinRange, float newMaxRange)
	{
		if (newMinRange == newMaxRange || oldMinRange == oldMaxRange)
		{
			return newMinRange;
		}
		float value2 = (value - oldMinRange) * (newMaxRange - newMinRange) / (oldMaxRange - oldMinRange) + newMinRange;
		return Clamp(value2, Min(newMinRange, newMaxRange), Max(newMinRange, newMaxRange));
	}

	public static float ClipToZero(float v, float epsilon)
	{
		return (v >= 0f - epsilon && v <= epsilon) ? 0f : v;
	}
}
