using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Styles;

[UIMarkupData]
public class ButtonSounds
{
	public SoundStyle Activate;

	public SoundStyle Context;

	public SoundStyle MouseHover;

	public ButtonSounds Clone()
	{
		return new ButtonSounds
		{
			Activate = Activate,
			Context = Context,
			MouseHover = MouseHover
		};
	}
}
