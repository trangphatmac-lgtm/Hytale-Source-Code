namespace HytaleClient.Data.UserSettings;

internal class MouseSettings
{
	public bool MouseRawInputMode = false;

	public bool MouseInverted = false;

	public float MouseXSpeed = 3.5f;

	public float MouseYSpeed = 3.5f;

	public MouseSettings Clone()
	{
		MouseSettings mouseSettings = new MouseSettings();
		mouseSettings.MouseRawInputMode = MouseRawInputMode;
		mouseSettings.MouseInverted = MouseInverted;
		mouseSettings.MouseXSpeed = MouseXSpeed;
		mouseSettings.MouseYSpeed = MouseYSpeed;
		return mouseSettings;
	}
}
