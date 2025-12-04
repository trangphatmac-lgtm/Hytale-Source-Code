using System;
using System.IO;
using System.Text;
using HytaleClient.Data;
using HytaleClient.Interface.Messages;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Utils;

public static class DataConversionUtils
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private static readonly string TempPngFilePath = Path.Combine(Paths.UserData, "AssetEditorPNGConversion.tmp");

	private static readonly object ImageSaveLock = new object();

	private static byte[] GetPNGBytes(Image image)
	{
		byte[] result;
		lock (ImageSaveLock)
		{
			image.SavePNG(TempPngFilePath);
			result = File.ReadAllBytes(TempPngFilePath);
			File.Delete(TempPngFilePath);
		}
		return result;
	}

	public static sbyte[] EncodeObject(object data)
	{
		JObject val = (JObject)((data is JObject) ? data : null);
		if (val == null)
		{
			if (!(data is string s))
			{
				if (data is Image image)
				{
					return (sbyte[])(object)GetPNGBytes(image);
				}
				throw new Exception("Invalid object of type " + data.GetType()?.ToString() + " passed");
			}
			return (sbyte[])(object)Encoding.UTF8.GetBytes(s);
		}
		string s2 = ((object)val).ToString();
		return (sbyte[])(object)Encoding.UTF8.GetBytes(s2);
	}

	public static bool TryDecodeBytes(sbyte[] data, AssetEditorEditorType editorType, out object result, out FormattedMessage error)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected I4, but got Unknown
		result = null;
		error = null;
		switch (editorType - 1)
		{
		case 4:
		{
			Image image;
			try
			{
				image = new Image((byte[])(object)data);
			}
			catch (Exception ex2)
			{
				Logger.Error<Exception>(ex2);
				error = new FormattedMessage
				{
					MessageId = "ui.assetEditor.errors.errorOccurredFetching"
				};
				return false;
			}
			result = image;
			return true;
		}
		case 1:
		case 2:
		{
			JObject val;
			try
			{
				string string2 = Encoding.UTF8.GetString((byte[])(object)data);
				val = JObject.Parse(string2);
			}
			catch (Exception ex3)
			{
				Logger.Error<Exception>(ex3);
				error = new FormattedMessage
				{
					MessageId = "ui.assetEditor.errors.invalidJson"
				};
				return false;
			}
			result = val;
			return true;
		}
		case 0:
		{
			string @string;
			try
			{
				@string = Encoding.UTF8.GetString((byte[])(object)data);
			}
			catch (Exception ex)
			{
				Logger.Error<Exception>(ex);
				error = new FormattedMessage
				{
					MessageId = "ui.assetEditor.errors.errorOccurredFetching"
				};
				return false;
			}
			result = @string;
			return true;
		}
		default:
			error = new FormattedMessage
			{
				RawText = "Invalid format"
			};
			return false;
		}
	}
}
