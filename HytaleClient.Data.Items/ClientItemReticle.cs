using HytaleClient.Protocol;

namespace HytaleClient.Data.Items;

public class ClientItemReticle
{
	public readonly bool HideBase;

	public readonly string[] Parts;

	public readonly float Duration;

	public ClientItemReticle(ItemReticle packet)
	{
		HideBase = packet.HideBase;
		Parts = packet.Parts;
		Duration = packet.Duration;
	}
}
