using System;
using System.Diagnostics;
using HytaleClient.Protocol;

namespace HytaleClient.Utils;

public static class TimeHelper
{
	public const long NanosPerTick = 100L;

	public const long NanosPerMillisecond = 1000000L;

	public const long NanosPerSecond = 1000000000L;

	public const long MicrosPerMillisecond = 1000L;

	public const long MicrosPerSecond = 1000000L;

	public const long MillisPerSecond = 1000L;

	public const int SecondsPerDay = 86400;

	public const float HoursPerDay = 24f;

	public static readonly DateTime EpochDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public static readonly DateTime ZeroYear = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public static readonly DateTime MaxTime = DateTime.MaxValue;

	public static long GetEpochMilliseconds(DateTime? time = null)
	{
		return (long)((time ?? DateTime.UtcNow) - EpochDateTime).TotalMilliseconds;
	}

	public static long GetEpochSeconds(DateTime? time = null)
	{
		return (long)((time ?? DateTime.UtcNow) - EpochDateTime).TotalSeconds;
	}

	public static DateTime InstantDataToDateTime(InstantData instantData)
	{
		TimeSpan timeSpan = TimeSpan.FromTicks(instantData.Seconds * 10000000 + (long)instantData.Nanos / 100L);
		return EpochDateTime + timeSpan;
	}

	public static InstantData DateTimeToInstantData(DateTime dateTime)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		TimeSpan timeSpan = dateTime - EpochDateTime;
		long num = (long)timeSpan.TotalSeconds;
		long num2 = timeSpan.Ticks - num * 10000000;
		return new InstantData(num, (int)(num2 * 100));
	}

	public static string FormatMillis(long millis)
	{
		long num = millis / 1000;
		millis %= 1000;
		if (num > 0)
		{
			return num + "s " + millis + "ms";
		}
		return millis + "ms";
	}

	public static string FormatNanos(long nanos)
	{
		long num = nanos / 1000000000;
		nanos %= 1000000000;
		long num2 = nanos / 1000000;
		nanos %= 1000000;
		if (num > 0)
		{
			return num + "s " + num2 + "ms " + nanos + "ns";
		}
		if (num2 > 0)
		{
			return num2 + "ms " + nanos + "ns";
		}
		return nanos + "ns";
	}

	public static string FormatMicros(long micros)
	{
		long num = micros / 1000000;
		micros %= 1000000;
		long num2 = micros / 1000;
		micros %= 1000;
		if (num > 0)
		{
			return num + "s " + num2 + "ms " + micros + "µs";
		}
		if (num2 > 0)
		{
			return num2 + "ms " + micros + "µs";
		}
		return micros + "µs";
	}

	public static string FormatTicks(long ticks)
	{
		return FormatMicros(ticks * (Stopwatch.Frequency / 1000000));
	}

	public static float GetDayProgressInHours(DateTime dateTime)
	{
		return (float)dateTime.Hour + (float)dateTime.Minute / 60f + (float)dateTime.Second / 3600f + (float)dateTime.Millisecond / 3600000f;
	}

	public static DateTime IncrementDateTimeBySeconds(DateTime dateTime, float seconds, int secondsPerDay)
	{
		long num = (long)((double)seconds * (86400.0 / (double)secondsPerDay) * 10000000.0);
		long num2 = dateTime.Ticks + num;
		DateTime zeroYear = ZeroYear;
		if (num2 < zeroYear.Ticks)
		{
			DateTime maxTime = MaxTime;
			zeroYear = ZeroYear;
			return maxTime - TimeSpan.FromTicks(zeroYear.Ticks - num2);
		}
		zeroYear = MaxTime;
		if (num2 > zeroYear.Ticks)
		{
			DateTime zeroYear2 = ZeroYear;
			zeroYear = MaxTime;
			return zeroYear2 + TimeSpan.FromTicks(zeroYear.Ticks - num2);
		}
		return dateTime + TimeSpan.FromTicks(num);
	}
}
