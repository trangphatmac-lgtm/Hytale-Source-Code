using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;

namespace HytaleClient.Interface.InGame.Hud.StatusEffects;

internal class TrinketPermanentBuff : TrinketBuffStatusEffect
{
	public TrinketPermanentBuff(InGameView InGameView, Desktop desktop, Element parent, string id = "")
		: base(InGameView, desktop, parent, id)
	{
	}
}
