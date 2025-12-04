using System.Linq;
using HytaleClient.Data.ClientInteraction;
using HytaleClient.Data.EntityStats;
using HytaleClient.Data.Items;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.InGame.Hud.Abilities;

internal class AbilitiesHudComponent : InterfaceComponent
{
	public readonly InGameView InGameView;

	private Group _hudContainer;

	private MainAbility _mainAbility;

	private SecondaryAbility _secondaryAbility;

	private SignatureAbility _signatureAbility;

	public AbilitiesHudComponent(InGameView inGameView)
		: base(inGameView.Interface, inGameView.HudContainer)
	{
		InGameView = inGameView;
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Hud/Abilities/AbilitiesHud.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_hudContainer = uIFragment.Get<Group>("AbilitiesHudContainer");
		_mainAbility = new MainAbility(InGameView, Desktop, uIFragment.Get<Group>("MainAbility"));
		_mainAbility.Build();
		_secondaryAbility = new SecondaryAbility(InGameView, Desktop, uIFragment.Get<Group>("SecondaryAbility"));
		_secondaryAbility.Build();
		_signatureAbility = new SignatureAbility(InGameView, Desktop, uIFragment.Get<Group>("SignatureAbility"));
		_signatureAbility.Build();
	}

	public void ShowOrHideHud()
	{
		ClientItemStack activeHotbarItem = InGameView.InGame.Instance.InventoryModule.GetActiveHotbarItem();
		bool valueOrDefault = (InGameView.InGame.Instance.ItemLibraryModule.GetItem(activeHotbarItem?.Id)?.Categories?.Contains("Items.Weapons")).GetValueOrDefault();
		if (!valueOrDefault)
		{
			ClientItemStack utilityItem = InGameView.InGame.Instance.InventoryModule.GetUtilityItem(InGameView.InGame.Instance.InventoryModule.UtilityActiveSlot);
			valueOrDefault = (InGameView.InGame.Instance.ItemLibraryModule.GetItem(utilityItem?.Id)?.Categories?.Contains("Items.Weapons")).GetValueOrDefault();
		}
		_hudContainer.Visible = valueOrDefault;
		_mainAbility.SetCharges();
		_secondaryAbility.SetCharges();
		_hudContainer.Layout(base.RectangleAfterPadding);
	}

	public void CooldownError(ClientRootInteraction rootInteraction)
	{
		_mainAbility.CooldownError(rootInteraction.Id);
		_secondaryAbility.CooldownError(rootInteraction.Id);
	}

	public void OnSignatureEnergyStatChanged(ClientEntityStatValue entityStatValue)
	{
		if (_hudContainer.Visible)
		{
			_signatureAbility.OnSignatureEnergyStatChanged(entityStatValue);
		}
	}

	protected override void OnMounted()
	{
		Interface.InGameView.UpdateAbilitiesHudVisibility(doLayout: true);
		ShowOrHideHud();
	}

	public void OnEffectRemoved(int effectIndex)
	{
		_mainAbility.OnEffectRemoved(effectIndex);
		_secondaryAbility.OnEffectRemoved(effectIndex);
	}

	public void OnStartChain(string rootInteractionId)
	{
		_mainAbility.OnStartChain(rootInteractionId);
		_secondaryAbility.OnStartChain(rootInteractionId);
	}

	public void OnTertiaryAction()
	{
		_signatureAbility.OnSignatureAction();
	}

	public void OnUpdateInputBindings()
	{
		_mainAbility.UpdateInputBiding();
		_secondaryAbility.UpdateInputBiding();
		_signatureAbility.UpdateInputBiding();
	}
}
