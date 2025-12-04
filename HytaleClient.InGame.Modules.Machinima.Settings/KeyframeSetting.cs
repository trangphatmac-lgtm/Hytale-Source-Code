using System;
using System.Collections.ObjectModel;
using Coherent.UI.Binding;
using HytaleClient.Math;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace HytaleClient.InGame.Modules.Machinima.Settings;

[CoherentType]
internal abstract class KeyframeSetting<T> : IKeyframeSetting
{
	[CoherentProperty("name")]
	public string Name { get; }

	public T Value { get; set; }

	[CoherentProperty("value")]
	public string ValueStringified
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Expected O, but got Unknown
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Expected O, but got Unknown
			JsonSerializer val = new JsonSerializer();
			((Collection<JsonConverter>)(object)val.Converters).Add((JsonConverter)new StringEnumConverter());
			return ((object)ToJsonObject(val)).ToString();
		}
	}

	[CoherentProperty("type")]
	public string ValueTypeName => ValueType.ToString();

	public Type ValueType => typeof(T);

	public KeyframeSetting(string name, T value)
	{
		Name = name;
		Value = value;
	}

	public virtual JObject ToJsonObject(JsonSerializer serializer)
	{
		return JObject.FromObject((object)Value, serializer);
	}

	public abstract IKeyframeSetting Clone();

	public static IKeyframeSetting ConvertJsonObject(string keyName, JObject jsonData)
	{
		return keyName switch
		{
			"Position" => new PositionSetting(((JToken)jsonData).ToObject<Vector3>()), 
			"Rotation" => new RotationSetting(((JToken)jsonData).ToObject<Vector3>()), 
			"Look" => new LookSetting(((JToken)jsonData).ToObject<Vector3>()), 
			"FieldOfView" => new FieldOfViewSetting(jsonData["Value"].ToObject<float>()), 
			"Curve" => new CurveSetting(jsonData["Value"].ToObject<Vector3[]>()), 
			"Easing" => new EasingSetting(jsonData["Value"].ToObject<Easing.EasingType>()), 
			_ => null, 
		};
	}
}
