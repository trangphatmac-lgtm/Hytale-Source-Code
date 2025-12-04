using System;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.Common;

internal class BackButton : Button
{
	private readonly Interface _interface;

	private Group _escContainer;

	private PatchStyle _escContainerBackground;

	private PatchStyle _escContainerBackgroundHovered;

	private PatchStyle _escContainerBackgroundPressed;

	public BackButton(Interface @interface, Action onActivate)
		: base(@interface.Desktop, null)
	{
		_interface = @interface;
		Activating = onActivate;
		Desktop.Provider.TryGetDocument("Common/BackButton.ui", out var document);
		_escContainerBackground = document.ResolveNamedValue<PatchStyle>(_interface, "EscButtonBackground");
		_escContainerBackgroundHovered = document.ResolveNamedValue<PatchStyle>(_interface, "EscButtonBackgroundHovered");
		_escContainerBackgroundPressed = document.ResolveNamedValue<PatchStyle>(_interface, "EscButtonBackgroundPressed");
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_escContainer = uIFragment.Get<Group>("EscContainer");
	}

	protected override void ApplyStyles()
	{
		if (base.CapturedMouseButton == 1u)
		{
			_escContainer.Background = _escContainerBackgroundPressed;
		}
		else if (base.IsHovered)
		{
			_escContainer.Background = _escContainerBackgroundHovered;
		}
		else
		{
			_escContainer.Background = _escContainerBackground;
		}
		base.ApplyStyles();
	}
}
