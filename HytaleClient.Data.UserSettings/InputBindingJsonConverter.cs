using System;
using HytaleClient.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SDL2;

namespace HytaleClient.Data.UserSettings;

internal class InputBindingJsonConverter : JsonConverter
{
	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Expected O, but got Unknown
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		InputBinding inputBinding = (InputBinding)value;
		JObject val = new JObject();
		if (inputBinding.Type == InputBinding.BindingType.Keycode)
		{
			val.Add("Keycode", JToken.FromObject((object)inputBinding.Keycode.Value));
		}
		else
		{
			val.Add("MouseButton", JToken.FromObject((object)inputBinding.MouseButton));
		}
		((JToken)val).WriteTo(writer, Array.Empty<JsonConverter>());
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		object obj = serializer.Deserialize(reader);
		JObject val = (JObject)((obj is JObject) ? obj : null);
		if (val == null)
		{
			return null;
		}
		return (val["Keycode"] != null) ? new InputBinding
		{
			Keycode = (SDL_Keycode)(int)val["Keycode"]
		} : ((val["keycode"] != null) ? new InputBinding
		{
			Keycode = (SDL_Keycode)(int)val["keycode"]
		} : ((val["MouseButton"] == null) ? new InputBinding
		{
			MouseButton = (Input.MouseButton)(int)val["mouseButton"]
		} : new InputBinding
		{
			MouseButton = (Input.MouseButton)(int)val["MouseButton"]
		}));
	}

	public override bool CanConvert(Type objectType)
	{
		return typeof(InputBinding).IsAssignableFrom(objectType);
	}
}
