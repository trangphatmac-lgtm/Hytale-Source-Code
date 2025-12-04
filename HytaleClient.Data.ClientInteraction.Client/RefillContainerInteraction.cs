using System.Collections.Generic;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Collision;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class RefillContainerInteraction : SimpleInstantInteraction
{
	private readonly HashSet<int> refillBlocks;

	public RefillContainerInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
		refillBlocks = new HashSet<int>(interaction.RefillBlocks);
	}

	protected override void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		Ray ray = gameInstance.CameraModule.GetLookRay();
		CollisionModule.BlockRaycastOptions options = CollisionModule.BlockRaycastOptions.Default;
		options.Block.BlockWhitelist = refillBlocks;
		CollisionModule.BlockResult result = CollisionModule.BlockResult.Default;
		if (gameInstance.CollisionModule.FindTargetBlock(ref ray, ref options, ref result) && refillBlocks.Contains(result.BlockId))
		{
			context.State.BlockPosition_ = new BlockPosition(result.Block.X, result.Block.Y, result.Block.Z);
		}
	}
}
