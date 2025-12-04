using Hypixel.ProtoPlus;
using HytaleClient.Data.Map;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class BreakBlockInteraction : SimpleBlockInteraction
{
	public BreakBlockInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void InteractWithBlock(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context, BlockPosition targetBlockHit, int blockId)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Invalid comparison between Unknown and I4
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Invalid comparison between Unknown and I4
		ClientBlockType clientBlockType = gameInstance.MapModule.ClientBlockTypes[blockId];
		bool flag = clientBlockType.Gathering?.Soft != null;
		bool flag2 = clientBlockType.Gathering?.Harvest != null;
		int x = targetBlockHit.X;
		int y = targetBlockHit.Y;
		int z = targetBlockHit.Z;
		if ((int)gameInstance.GameMode == 1)
		{
			if (Interaction.Harvest)
			{
				if (flag2)
				{
					SimulateBreakBlock(context, gameInstance, x, y, z, blockId, clientBlockType);
				}
			}
			else
			{
				SimulateBreakBlock(context, gameInstance, x, y, z, blockId, clientBlockType);
			}
			return;
		}
		if (!Interaction.Harvest && !flag)
		{
			if ((int)clientBlockType.DrawType > 0)
			{
			}
			if (clientBlockType.BlockParticleSetId != null)
			{
				Vector3 blockPosition = gameInstance.InteractionModule.TargetBlockHit.BlockPosition;
				if (gameInstance.ParticleSystemStoreModule.TrySpawnBlockSystem(blockPosition, clientBlockType, ClientBlockParticleEvent.Hit, out var particleSystemProxy, faceCameraYaw: true))
				{
					particleSystemProxy.Position = blockPosition + new Vector3(0.5f) + particleSystemProxy.Position;
				}
			}
		}
		if (Interaction.Harvest)
		{
			if (flag2)
			{
				SimulateBreakBlock(context, gameInstance, x, y, z, blockId, clientBlockType);
			}
		}
		else if (flag)
		{
			SimulateBreakBlock(context, gameInstance, x, y, z, blockId, clientBlockType);
		}
	}

	private static void SimulateBreakBlock(InteractionContext context, GameInstance gameInstance, int targetBlockX, int targetBlockY, int targetBlockZ, int blockId, ClientBlockType blockType)
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected O, but got Unknown
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		context.InstanceStore.OldBlockId = blockId;
		if (blockType.VariantOriginalId != 0 && blockType.FluidBlockId != 0 && blockType.FluidBlockId != blockId)
		{
			context.InstanceStore.ExpectedBlockId = blockType.FluidBlockId;
			gameInstance.InjectPacket((ProtoPacket)new ServerSetBlock(targetBlockX, targetBlockY, targetBlockZ, blockType.FluidBlockId, false));
		}
		else
		{
			context.InstanceStore.ExpectedBlockId = 0;
			gameInstance.InjectPacket((ProtoPacket)new ServerSetBlock(targetBlockX, targetBlockY, targetBlockZ, 0, false));
		}
	}

	protected override void Revert0(GameInstance gameInstance, InteractionType type, InteractionContext context)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		base.Revert0(gameInstance, type, context);
		InteractionSyncData state = context.State;
		int oldBlockId = context.InstanceStore.OldBlockId;
		int expectedBlockId = context.InstanceStore.ExpectedBlockId;
		if (state.BlockPosition_ != null && oldBlockId != int.MaxValue && gameInstance.MapModule.GetBlock(state.BlockPosition_.X, state.BlockPosition_.Y, state.BlockPosition_.Z, int.MaxValue) == expectedBlockId)
		{
			gameInstance.MapModule.SetClientBlock(state.BlockPosition_.X, state.BlockPosition_.Y, state.BlockPosition_.Z, oldBlockId);
		}
	}
}
