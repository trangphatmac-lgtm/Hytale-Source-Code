using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Protocol;

namespace HytaleClient.Interface.InGame.Hud.Abilities;

internal class MainAbility : BaseAbility
{
	public MainAbility(InGameView inGameView, Desktop desktop, Element parent)
		: base(inGameView, desktop, parent)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		_iterationType = (InteractionType)0;
	}

	public override void Build()
	{
		base.Build();
		SetIcon(IconName.SwordChargeUpAttack);
	}

	public override void UpdateInputBiding()
	{
		if (InGameView != null)
		{
			_inputBidingKey = InGameView.Interface.App.Settings.InputBindings.PrimaryItemAction;
			base.UpdateInputBiding();
		}
	}
}
