using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Machinima.Settings;

internal class PositionSetting : KeyframeSetting<Vector3>
{
	public const string KEY_NAME = "Position";

	public static KeyframeSettingType KeyframeType;

	public PositionSetting(Vector3 position)
		: base("Position", position)
	{
	}

	public override IKeyframeSetting Clone()
	{
		return new PositionSetting(base.Value);
	}
}
