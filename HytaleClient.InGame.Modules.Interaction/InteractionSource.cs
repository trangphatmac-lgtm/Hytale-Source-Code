using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.Interaction;

public interface InteractionSource
{
	bool TryGetInteractionId(InteractionType type, out int id);
}
