using System;
using HytaleClient.InGame.Modules.Shortcuts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Data.UserSettings;

internal class ShortcutSettingsJsonConverter : JsonConverter
{
	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value is Shortcut)
		{
			((JToken)((Shortcut)value).ToJsonObject()).WriteTo(writer, Array.Empty<JsonConverter>());
		}
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		SerializedShortcutSetting serializedShortcutSetting = (SerializedShortcutSetting)serializer.Deserialize(reader, typeof(SerializedShortcutSetting));
		if (serializedShortcutSetting.name == null && serializedShortcutSetting.command == null)
		{
			return null;
		}
		if (objectType == typeof(MacroShortcut))
		{
			return new MacroShortcut(serializedShortcutSetting.name, serializedShortcutSetting.command);
		}
		try
		{
			if (objectType == typeof(KeybindShortcut))
			{
				return new KeybindShortcut(serializedShortcutSetting.name, serializedShortcutSetting.command);
			}
		}
		catch (Exception)
		{
		}
		return null;
	}

	public override bool CanConvert(Type objectType)
	{
		return typeof(MacroShortcut).IsAssignableFrom(objectType) || typeof(KeybindShortcut).IsAssignableFrom(objectType);
	}
}
