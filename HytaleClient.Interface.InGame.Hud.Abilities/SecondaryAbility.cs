using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Protocol;

namespace HytaleClient.Interface.InGame.Hud.Abilities;

internal class SecondaryAbility : BaseAbility
{
	public SecondaryAbility(InGameView inGameView, Desktop desktop, Element parent)
		: base(inGameView, desktop, parent)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		_iterationType = (InteractionType)1;
		_inputBidingKey = InGameView.Interface.App.Settings.InputBindings.SecondaryItemAction;
	}

	public override void Build()
	{
		base.Build();
		SetIcon(IconName.ShieldAbility);
	}

	public override void UpdateInputBiding()
	{
		if (InGameView != null)
		{
			_inputBidingKey = InGameView.Interface.App.Settings.InputBindings.SecondaryItemAction;
			base.UpdateInputBiding();
		}
	}
}
