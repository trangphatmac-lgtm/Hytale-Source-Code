using HytaleClient.Data.Items;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.InGame.Hud;

internal class BuilderToolsMaterialSlotSelector : ItemSlotSelector
{
	public BuilderToolsMaterialSlotSelector(InGameView inGameView)
		: base(inGameView, enableEmptySlot: false)
	{
	}

	public override void Build()
	{
		base.Build();
		Find<Element>("Icon").Background = new PatchStyle("InGame/Hud/UtilitySlotSelectorIcon.png");
	}

	protected override void OnSlotSelected(int slot, bool clicked)
	{
		ClientItemStack itemStack = GetItemStack(slot);
		if (itemStack != null)
		{
			SelectedSlot = slot;
			Interface.TriggerEventFromInterface("builderTools.selectActiveToolMaterial", itemStack);
		}
	}
}
