using HytaleClient.Math;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HytaleClient.InGame.Modules.Machinima.Settings;

internal class FieldOfViewSetting : KeyframeSetting<float>
{
	public const string KEY_NAME = "FieldOfView";

	public static KeyframeSettingType KeyframeType = KeyframeSettingType.FieldOfView;

	public FieldOfViewSetting(float fov)
		: base("FieldOfView", fov)
	{
		base.Value = MathHelper.Clamp(base.Value, 1f, 179f);
	}

	public override JObject ToJsonObject(JsonSerializer serializer)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		JObject val = new JObject();
		val.Add("Value", JToken.op_Implicit(base.Value));
		return val;
	}

	public override IKeyframeSetting Clone()
	{
		return new FieldOfViewSetting(base.Value);
	}
}
