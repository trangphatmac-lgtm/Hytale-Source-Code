using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Machinima.Settings;

internal class RotationSetting : KeyframeSetting<Vector3>
{
	public const string KEY_NAME = "Rotation";

	public static KeyframeSettingType KeyframeType = KeyframeSettingType.Rotation;

	public RotationSetting(Vector3 position)
		: base("Rotation", position)
	{
	}

	public override IKeyframeSetting Clone()
	{
		return new RotationSetting(base.Value);
	}
}
