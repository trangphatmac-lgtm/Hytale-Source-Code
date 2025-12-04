using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Styles;

[UIMarkupData]
public class SoundStyle
{
	public UIPath SoundPath;

	public float Volume;

	public float MinPitch;

	public float MaxPitch;

	public bool StopExistingPlayback;
}
