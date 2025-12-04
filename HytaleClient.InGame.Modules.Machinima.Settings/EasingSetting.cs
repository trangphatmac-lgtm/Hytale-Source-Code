using HytaleClient.Math;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HytaleClient.InGame.Modules.Machinima.Settings;

internal class EasingSetting : KeyframeSetting<Easing.EasingType>
{
	public const string KEY_NAME = "Easing";

	public static KeyframeSettingType KeyframeType = KeyframeSettingType.Easing;

	public EasingSetting(Easing.EasingType easing)
		: base("Easing", easing)
	{
		base.Value = easing;
	}

	public override JObject ToJsonObject(JsonSerializer serializer)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		JObject val = new JObject();
		val.Add("Value", JToken.FromObject((object)base.Value, serializer));
		return val;
	}

	public override IKeyframeSetting Clone()
	{
		return new EasingSetting(base.Value);
	}
}
