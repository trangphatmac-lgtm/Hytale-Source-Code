using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Machinima.Settings;

internal class LookSetting : KeyframeSetting<Vector3>
{
	public const string KEY_NAME = "Look";

	public static KeyframeSettingType KeyframeType = KeyframeSettingType.Look;

	public LookSetting(Vector3 position)
		: base("Look", position)
	{
	}

	public override IKeyframeSetting Clone()
	{
		return new LookSetting(base.Value);
	}
}
