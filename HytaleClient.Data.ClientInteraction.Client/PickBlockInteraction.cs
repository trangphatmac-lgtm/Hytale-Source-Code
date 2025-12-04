using HytaleClient.Audio;
using HytaleClient.Data.Map;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class PickBlockInteraction : SimpleBlockInteraction
{
	public PickBlockInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void InteractWithBlock(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context, BlockPosition targetBlockHit, int blockId)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		if ((int)gameInstance.GameMode != 1)
		{
			return;
		}
		if (gameInstance.BuilderToolsModule.HasActiveTool)
		{
			gameInstance.BuilderToolsModule.OnPickBlockInteraction();
			return;
		}
		ClientBlockType blockType = gameInstance.MapModule.ClientBlockTypes[blockId];
		gameInstance.Engine.RunOnMainThread(gameInstance, delegate
		{
			gameInstance.InventoryModule.AddAndSelectHotbarItem(blockType.Item);
		}, allowCallFromMainThread: true);
		if (gameInstance.ServerSettings.BlockSoundSets[blockType.BlockSoundSetIndex].SoundEventIndices.TryGetValue((BlockSoundEvent)6, out var value))
		{
			uint networkWwiseId = ResourceManager.GetNetworkWwiseId(value);
			gameInstance.AudioModule.PlaySoundEvent(networkWwiseId, new Vector3((float)targetBlockHit.X + 0.5f, (float)targetBlockHit.Y + 0.5f, (float)targetBlockHit.Z + 0.5f), Vector3.Zero);
		}
	}
}
