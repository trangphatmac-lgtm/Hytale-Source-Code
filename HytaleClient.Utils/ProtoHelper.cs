using System;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Utils;

public static class ProtoHelper
{
	private const float RbRatio = 127f / (float)System.Math.PI;

	private const float RbInverse = (float)System.Math.PI * 2f / 255f;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte RadianToByte(float rad)
	{
		return (byte)(rad * (127f / (float)System.Math.PI));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float ByteToRadian(byte b)
	{
		return (float)(int)b * ((float)System.Math.PI * 2f / 255f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int SerializeFloat(float p)
	{
		return (int)(p * 32f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int SerializeFloat(double p)
	{
		return (int)(p * 32.0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DeserializeFloat(int i)
	{
		return (float)i / 32f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int SerializeFloatPrecise(float p)
	{
		return (int)(p * 1000f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DeserializeFloatPrecise(int i)
	{
		return (float)i / 1000f;
	}

	public static JObject DeserializeBson(byte[] data)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		if (data.Length == 0)
		{
			return null;
		}
		using MemoryStream memoryStream = new MemoryStream(data);
		BsonDataReader val = new BsonDataReader((Stream)memoryStream);
		try
		{
			JsonSerializer val2 = new JsonSerializer();
			return val2.Deserialize<JObject>((JsonReader)(object)val);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public static byte[] SerializeBson(JObject val)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		if (val == null)
		{
			return new byte[0];
		}
		using MemoryStream memoryStream = new MemoryStream();
		BsonDataWriter val2 = new BsonDataWriter((Stream)memoryStream);
		try
		{
			JsonSerializer val3 = new JsonSerializer();
			val3.Serialize((JsonWriter)(object)val2, (object)val);
			return memoryStream.ToArray();
		}
		finally
		{
			((IDisposable)val2)?.Dispose();
		}
	}
}
