using HytaleClient.Math;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HytaleClient.InGame.Modules.Machinima.Settings;

internal class CurveSetting : KeyframeSetting<Vector3[]>
{
	public const string KEY_NAME = "Curve";

	public static KeyframeSettingType KeyframeType = KeyframeSettingType.Curve;

	public CurveSetting(Vector3[] positions)
		: base("Curve", positions)
	{
	}

	public override JObject ToJsonObject(JsonSerializer serializer)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		JObject val = new JObject();
		val.Add("Value", (JToken)(object)JArray.FromObject((object)base.Value, serializer));
		return val;
	}

	public override IKeyframeSetting Clone()
	{
		return new CurveSetting(base.Value);
	}
}
