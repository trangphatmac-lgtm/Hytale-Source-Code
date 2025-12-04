using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.InGame.Hud;

internal class UtilitySlotSelector : ItemSlotSelector
{
	public const string Id = "Hytale:UtilitySlotSelector";

	public UtilitySlotSelector(InGameView inGameView)
		: base(inGameView)
	{
	}

	public override void Build()
	{
		base.Build();
		Find<Element>("Icon").Background = new PatchStyle("InGame/Hud/UtilitySlotSelectorIcon.png");
	}

	protected override void OnSlotSelected(int slot, bool clicked)
	{
		if (slot != SelectedSlot - 1)
		{
			Interface.TriggerEventFromInterface("game.selectActiveUtilitySlot", slot);
		}
	}
}
