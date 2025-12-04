using System;
using HytaleClient.InGame.Modules.Machinima.Actors;
using HytaleClient.InGame.Modules.Machinima.Events;
using HytaleClient.InGame.Modules.Machinima.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HytaleClient.InGame.Modules.Machinima;

internal class MachinimaSceneJsonConverter : JsonConverter
{
	private GameInstance _gameInstance;

	private MachinimaModule _machinimaModule;

	public MachinimaSceneJsonConverter(GameInstance gameInstance, MachinimaModule machinimaModule)
	{
		_gameInstance = gameInstance;
		_machinimaModule = machinimaModule;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value is IKeyframeSetting)
		{
			((JToken)((IKeyframeSetting)value).ToJsonObject(serializer)).WriteTo(writer, Array.Empty<JsonConverter>());
		}
		if (value is KeyframeEvent)
		{
			((JToken)((KeyframeEvent)value).ToJsonObject()).WriteTo(writer, Array.Empty<JsonConverter>());
		}
		if (value is SceneActor)
		{
			((SceneActor)value).WriteToJsonObject(serializer, writer);
		}
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected O, but got Unknown
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Expected O, but got Unknown
		if (typeof(SceneActor).IsAssignableFrom(objectType))
		{
			SerializedSceneObject actorData = (SerializedSceneObject)serializer.Deserialize(reader, typeof(SerializedSceneObject));
			return SceneActor.ConvertJsonObject(_gameInstance, actorData);
		}
		if (objectType == typeof(IKeyframeSetting))
		{
			JObject jsonData = (JObject)serializer.Deserialize(reader);
			string keyName = reader.Path.Substring(reader.Path.LastIndexOf(".") + 1);
			return KeyframeSetting<object>.ConvertJsonObject(keyName, jsonData);
		}
		if (objectType == typeof(KeyframeEvent))
		{
			JObject jsonData2 = (JObject)serializer.Deserialize(reader);
			return KeyframeEvent.ConvertJsonObject(jsonData2);
		}
		return null;
	}

	public override bool CanConvert(Type objectType)
	{
		return typeof(SceneActor).IsAssignableFrom(objectType) || typeof(KeyframeEvent).IsAssignableFrom(objectType) || typeof(IKeyframeSetting).IsAssignableFrom(objectType);
	}
}
