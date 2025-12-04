using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.InGame.Hud;

internal class ConsumableSlotSelector : ItemSlotSelector
{
	public const string Id = "Hytale:ConsumableSlotSelector";

	public ConsumableSlotSelector(InGameView inGameView)
		: base(inGameView)
	{
	}

	public override void Build()
	{
		base.Build();
		Find<Element>("Icon").Background = new PatchStyle("InGame/Hud/ConsumableSlotSelectorIcon.png");
	}

	protected override void OnSlotSelected(int slot, bool clicked)
	{
		if (clicked)
		{
			Interface.TriggerEventFromInterface("game.useConsumableSlot", slot);
		}
		else
		{
			Interface.TriggerEventFromInterface("game.selectActiveConsumableSlot", slot);
		}
	}
}
