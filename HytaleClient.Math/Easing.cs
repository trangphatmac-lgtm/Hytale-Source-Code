using System;

namespace HytaleClient.Math;

public static class Easing
{
	public enum EasingType
	{
		Linear,
		QuadIn,
		QuadOut,
		QuadInOut,
		CubicIn,
		CubicOut,
		CubicInOut,
		QuartIn,
		QuartOut,
		QuartInOut,
		QuintIn,
		QuintOut,
		QuintInOut,
		SineIn,
		SineOut,
		SineInOut,
		ExpoIn,
		ExpoOut,
		ExpoInOut,
		CircIn,
		CircOut,
		CircInOut,
		ElasticIn,
		ElasticOut,
		ElasticInOut,
		BackIn,
		BackOut,
		BackInOut,
		BounceIn,
		BounceOut,
		BounceInOut
	}

	public static float Ease(EasingType easingType, float t, float b = 0f, float c = 1f, float d = 1f)
	{
		return easingType switch
		{
			EasingType.Linear => Linear(t, b, c, d), 
			EasingType.QuadIn => QuadIn(t, b, c, d), 
			EasingType.QuadOut => QuadOut(t, b, c, d), 
			EasingType.QuadInOut => QuadInOut(t, b, c, d), 
			EasingType.CubicIn => CubicIn(t, b, c, d), 
			EasingType.CubicOut => CubicOut(t, b, c, d), 
			EasingType.CubicInOut => CubicInOut(t, b, c, d), 
			EasingType.QuartIn => QuartIn(t, b, c, d), 
			EasingType.QuartOut => QuartOut(t, b, c, d), 
			EasingType.QuartInOut => QuartInOut(t, b, c, d), 
			EasingType.QuintIn => QuintIn(t, b, c, d), 
			EasingType.QuintOut => QuintOut(t, b, c, d), 
			EasingType.QuintInOut => QuintInOut(t, b, c, d), 
			EasingType.SineIn => SineIn(t, b, c, d), 
			EasingType.SineOut => SineOut(t, b, c, d), 
			EasingType.SineInOut => SineInOut(t, b, c, d), 
			EasingType.ExpoIn => ExpoIn(t, b, c, d), 
			EasingType.ExpoOut => ExpoOut(t, b, c, d), 
			EasingType.ExpoInOut => ExpoInOut(t, b, c, d), 
			EasingType.CircIn => CircIn(t, b, c, d), 
			EasingType.CircOut => CircOut(t, b, c, d), 
			EasingType.CircInOut => CircInOut(t, b, c, d), 
			EasingType.ElasticIn => ElasticIn(t, b, c, d), 
			EasingType.ElasticOut => ElasticOut(t, b, c, d), 
			EasingType.ElasticInOut => ElasticInOut(t, b, c, d), 
			EasingType.BackIn => BackIn(t, b, c, d), 
			EasingType.BackOut => BackOut(t, b, c, d), 
			EasingType.BackInOut => BackInOut(t, b, c, d), 
			EasingType.BounceIn => BounceIn(t, b, c, d), 
			EasingType.BounceOut => BounceOut(t, b, c, d), 
			EasingType.BounceInOut => BounceInOut(t, b, c, d), 
			_ => throw new Exception($"Invalid easing type - {easingType}"), 
		};
	}

	public static float Linear(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		return b + (c - b) * t / d;
	}

	public static float QuadIn(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		return c * (t /= d) * t + b;
	}

	public static float QuadOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		return (0f - c) * (t /= d) * (t - 2f) + b;
	}

	public static float QuadInOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		if ((t /= d / 2f) < 1f)
		{
			return c / 2f * t * t + b;
		}
		return (0f - c) / 2f * ((t -= 1f) * (t - 2f) - 1f) + b;
	}

	public static float CubicIn(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		return c * (t /= d) * t * t + b;
	}

	public static float CubicOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		return c * ((t = t / d - 1f) * t * t + 1f) + b;
	}

	public static float CubicInOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		if ((t /= d / 2f) < 1f)
		{
			return c / 2f * t * t * t + b;
		}
		return c / 2f * ((t -= 2f) * t * t + 2f) + b;
	}

	public static float QuartIn(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		return c * (t /= d) * t * t * t + b;
	}

	public static float QuartOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		return (0f - c) * ((t = t / d - 1f) * t * t * t - 1f) + b;
	}

	public static float QuartInOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		if ((t /= d / 2f) < 1f)
		{
			return c / 2f * t * t * t * t + b;
		}
		return (0f - c) / 2f * ((t -= 2f) * t * t * t - 2f) + b;
	}

	public static float QuintIn(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		return c * (t /= d) * t * t * t * t + b;
	}

	public static float QuintOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		return c * ((t = t / d - 1f) * t * t * t * t + 1f) + b;
	}

	public static float QuintInOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		if ((t /= d / 2f) < 1f)
		{
			return c / 2f * t * t * t * t * t + b;
		}
		return c / 2f * ((t -= 2f) * t * t * t * t + 2f) + b;
	}

	public static float SineIn(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		return (0f - c) * (float)System.Math.Cos(t / d * ((float)System.Math.PI / 2f)) + c + b;
	}

	public static float SineOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		return c * (float)System.Math.Sin(t / d * ((float)System.Math.PI / 2f)) + b;
	}

	public static float SineInOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		return (0f - c) / 2f * (float)(System.Math.Cos((float)System.Math.PI * t / d) - 1.0) + b;
	}

	public static float ExpoIn(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		return (t == 0f) ? b : (c * (float)System.Math.Pow(2.0, 10f * (t / d - 1f)) + b);
	}

	public static float ExpoOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		return (t == d) ? (b + c) : (c * (0f - (float)System.Math.Pow(2.0, -10f * t / d) + 1f) + b);
	}

	public static float ExpoInOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		if (t == 0f)
		{
			return b;
		}
		if (t == d)
		{
			return b + c;
		}
		if ((t /= d / 2f) < 1f)
		{
			return c / 2f * (float)System.Math.Pow(2.0, 10f * (t - 1f)) + b;
		}
		return c / 2f * (0f - (float)System.Math.Pow(2.0, -10f * (t -= 1f)) + 2f) + b;
	}

	public static float CircIn(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		return (0f - c) * ((float)System.Math.Sqrt(1f - (t /= d) * t) - 1f) + b;
	}

	public static float CircOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		return c * (float)System.Math.Sqrt(1f - (t = t / d - 1f) * t) + b;
	}

	public static float CircInOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		if ((t /= d / 2f) < 1f)
		{
			return (0f - c) / 2f * ((float)System.Math.Sqrt(1f - t * t) - 1f) + b;
		}
		return c / 2f * ((float)System.Math.Sqrt(1f - (t -= 2f) * t) + 1f) + b;
	}

	public static float ElasticIn(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		double num = 1.70158;
		float num2 = c;
		if (t == 0f)
		{
			return b;
		}
		if ((t /= d) == 1f)
		{
			return b + c;
		}
		double num3 = (double)d * 0.3;
		if (num2 < System.Math.Abs(c))
		{
			num2 = c;
			num = num3 / 4.0;
		}
		else
		{
			num = num3 / 6.2831854820251465 * System.Math.Asin(c / num2);
		}
		return 0f - (float)((double)num2 * System.Math.Pow(2.0, 10f * (t -= 1f)) * System.Math.Sin(((double)(t * d) - num) * 6.2831854820251465 / num3)) + b;
	}

	public static float ElasticOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		double num = 1.70158;
		float num2 = c;
		if (t == 0f)
		{
			return b;
		}
		if ((t /= d) == 1f)
		{
			return b + c;
		}
		double num3 = (double)d * 0.3;
		if (num2 < System.Math.Abs(c))
		{
			num2 = c;
			num = num3 / 4.0;
		}
		else
		{
			num = num3 / 6.2831854820251465 * System.Math.Asin(c / num2);
		}
		return num2 * (float)(System.Math.Pow(2.0, -10f * t) * System.Math.Sin(((double)(t * d) - num) * 6.2831854820251465 / num3)) + c + b;
	}

	public static float ElasticInOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		double num = 1.70158;
		float num2 = c;
		if (t == 0f)
		{
			return b;
		}
		if ((t /= d / 2f) == 2f)
		{
			return b + c;
		}
		double num3 = (double)d * 0.44999999999999996;
		if (num2 < System.Math.Abs(c))
		{
			num2 = c;
			num = num3 / 4.0;
		}
		else
		{
			num = num3 / 6.2831854820251465 * System.Math.Asin(c / num2);
		}
		if (t < 1f)
		{
			return -0.5f * (float)((double)num2 * System.Math.Pow(2.0, 10f * (t -= 1f)) * System.Math.Sin(((double)(t * d) - num) * 6.2831854820251465 / num3)) + b;
		}
		return (float)((double)num2 * System.Math.Pow(2.0, -10f * (t -= 1f)) * System.Math.Sin(((double)(t * d) - num) * 6.2831854820251465 / num3) * 0.5 + (double)c + (double)b);
	}

	public static float BackIn(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		float num = 1.70158f;
		return c * (t /= d) * t * ((num + 1f) * t - num) + b;
	}

	public static float BackOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		float num = 1.70158f;
		return c * ((t = t / d - 1f) * t * ((num + 1f) * t + num) + 1f) + b;
	}

	public static float BackInOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		float num = 1.70158f;
		if ((t /= d / 2f) < 1f)
		{
			return c / 2f * (t * t * (((num *= 1.525f) + 1f) * t - num)) + b;
		}
		return c / 2f * ((t -= 2f) * t * (((num *= 1.525f) + 1f) * t + num) + 2f) + b;
	}

	public static float BounceIn(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		return c - BounceOut(d - t, 0f, c, d) + b;
	}

	public static float BounceOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		if ((double)(t /= d) < 0.36363636363636365)
		{
			return c * (7.5625f * t * t) + b;
		}
		if ((double)t < 0.7272727272727273)
		{
			return c * (7.5625f * (t -= 0.54545456f) * t + 0.75f) + b;
		}
		if ((double)t < 0.9090909090909091)
		{
			return c * (7.5625f * (t -= 0.8181818f) * t + 0.9375f) + b;
		}
		return c * (7.5625f * (t -= 21f / 22f) * t + 63f / 64f) + b;
	}

	public static float BounceInOut(float t, float b = 0f, float c = 1f, float d = 1f)
	{
		if (t < d / 2f)
		{
			return BounceIn(t * 2f, 0f, c, d) * 0.5f + b;
		}
		return BounceOut(t * 2f - d, 0f, c, d) * 0.5f + c * 0.5f + b;
	}

	public static double QuadEaseOut(double currentTime, double startValue, double endValue, double duration)
	{
		double num = currentTime / duration;
		return (0.0 - endValue) * num * (num - 2.0) + startValue;
	}

	public static double QuadEaseIn(double currentTime, double startValue, double endValue, double duration)
	{
		double num = currentTime / duration;
		return endValue * num * num + startValue;
	}

	public static double BackEaseOutExtended(double currentTime, double startValue, double endValue, double duration)
	{
		double num = currentTime / duration - 1.0;
		return endValue * (num * num * (5.70158 * num + 4.70158) + 1.0) + startValue;
	}

	public static float CubicEaseInAndOut(float t)
	{
		float num = (float)System.Math.Pow(1f - t, 3.0) * 0f * 2f + 3f * (float)System.Math.Pow(1f - t, 2.0) * t * 0.05f * 1f + 3f * (1f - t) * t * t * 0.95f * 2f + t * t * t * 1f * 1f;
		float num2 = (float)System.Math.Pow(1f - t, 3.0) * 2f + 3f * (float)System.Math.Pow(1f - t, 2.0) * t * 1f + 3f * (1f - t) * t * t * 2f + t * t * t * 1f;
		return num / num2;
	}
}
