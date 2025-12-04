using System;
using System.Globalization;
using System.IO;
using HytaleClient.AssetEditor.Interface.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Utils;

public class JsonUtils
{
	public static void ValidateJson(string json)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		JsonTextReader val = new JsonTextReader((TextReader)new StringReader(json));
		try
		{
			while (((JsonReader)val).Read())
			{
				if ((int)((JsonReader)val).TokenType != 5)
				{
					continue;
				}
				throw new JsonReaderException("Comments are not allowed in JSON!");
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public static bool IsNull(JToken token)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		return token == null || (int)token.Type == 10;
	}

	public static void RemoveProperty(JObject obj, PropertyPath path)
	{
		JToken val = (JToken)(object)obj;
		string[] elements = path.Elements;
		foreach (string text in elements)
		{
			JToken val2 = val[(object)text];
			if (val2 == null)
			{
				return;
			}
			val = val2;
		}
		if (val.Parent is JProperty)
		{
			((JToken)val.Parent).Remove();
		}
		else
		{
			val.Remove();
		}
	}

	public static JToken ParseLenient(string value)
	{
		if (bool.TryParse(value, out var result))
		{
			return JToken.op_Implicit(result);
		}
		if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result2))
		{
			return JToken.op_Implicit(result2);
		}
		if (value.StartsWith("{") || value.StartsWith("["))
		{
			try
			{
				return JToken.Parse(value);
			}
			catch (Exception)
			{
			}
		}
		return JToken.op_Implicit(value);
	}

	public static string GetTitleFromKey(string text)
	{
		string text2 = "";
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (char.IsUpper(c) && text2.Length > 0 && (i >= text.Length - 1 || !char.IsUpper(text[i + 1])))
			{
				text2 += " ";
			}
			text2 += c;
		}
		return text2;
	}

	public static decimal ConvertToDecimal(double value)
	{
		if (value > 7.922816251426434E+28)
		{
			return decimal.MaxValue;
		}
		if (value < -7.922816251426434E+28)
		{
			return decimal.MinValue;
		}
		return Convert.ToDecimal(value);
	}

	public static decimal ConvertToDecimal(JToken token)
	{
		return ConvertToDecimal((double)token);
	}
}
