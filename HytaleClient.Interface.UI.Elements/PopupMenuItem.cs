using System;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.UI.Elements;

internal class PopupMenuItem
{
	public readonly string Label;

	public readonly Action Activating;

	public readonly UIPath IconTexturePath;

	public readonly SoundStyle ActivateSound;

	public PopupMenuItem(string label, Action onActivate, string iconTexturePath = null, SoundStyle activateSound = null)
	{
		Label = label;
		Activating = onActivate;
		IconTexturePath = ((iconTexturePath != null) ? new UIPath(iconTexturePath) : null);
		ActivateSound = activateSound;
	}
}
