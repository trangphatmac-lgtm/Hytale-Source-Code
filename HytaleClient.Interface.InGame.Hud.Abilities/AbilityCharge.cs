using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.InGame.Hud.Abilities;

internal class AbilityCharge : Element
{
	private Group _chargeEmtpy;

	private Group _chargeFull;

	private Group _abilityChargeContainer;

	public AbilityCharge(InGameView inGameView, Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	public void Build()
	{
		Desktop.Provider.TryGetDocument("InGame/Hud/Abilities/AbilityCharge.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_chargeEmtpy = uIFragment.Get<Group>("ChargeEmpty");
		_chargeFull = uIFragment.Get<Group>("ChargeFull");
		_abilityChargeContainer = uIFragment.Get<Group>("AbilityChargeContainer");
	}

	public void SetEmpty()
	{
		_chargeEmtpy.Visible = true;
		_chargeFull.Visible = false;
		_abilityChargeContainer.Layout();
	}

	public void SetFull()
	{
		_chargeEmtpy.Visible = false;
		_chargeFull.Visible = true;
		_abilityChargeContainer.Layout();
	}
}
