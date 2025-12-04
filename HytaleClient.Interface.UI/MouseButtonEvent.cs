namespace HytaleClient.Interface.UI;

public struct MouseButtonEvent
{
	public readonly int Button;

	public readonly int Clicks;

	public MouseButtonEvent(int button, int clicks)
	{
		Button = button;
		Clicks = clicks;
	}
}
