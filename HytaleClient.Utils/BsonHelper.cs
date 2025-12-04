using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Utils;

internal static class BsonHelper
{
	public static sbyte[] ToBson(JToken token)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Expected O, but got Unknown
		using MemoryStream memoryStream = new MemoryStream();
		BsonDataWriter val = new BsonDataWriter((Stream)memoryStream);
		try
		{
			token.WriteTo((JsonWriter)(object)val, Array.Empty<JsonConverter>());
			return (sbyte[])(object)memoryStream.ToArray();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public static JToken FromBson(sbyte[] bson)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		using MemoryStream memoryStream = new MemoryStream((byte[])(object)bson);
		BsonDataReader val = new BsonDataReader((Stream)memoryStream);
		try
		{
			return JToken.ReadFrom((JsonReader)(object)val);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public static T ObjectFromBson<T>(sbyte[] bson)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		using MemoryStream memoryStream = new MemoryStream((byte[])(object)bson);
		BsonDataReader val = new BsonDataReader((Stream)memoryStream);
		try
		{
			JsonSerializer val2 = new JsonSerializer();
			return val2.Deserialize<T>((JsonReader)(object)val);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}
}
