using HytaleClient.Data.Items;
using HytaleClient.Data.Map;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class BlockConditionInteraction : SimpleBlockInteraction
{
	public BlockConditionInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void InteractWithBlock(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context, BlockPosition targetBlockHit, int blockId)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Invalid comparison between Unknown and I4
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		Vector3 normal = gameInstance.InteractionModule.TargetBlockHit.Normal;
		BlockFace val = RotationHelper.FromNormal(normal);
		ClientBlockType clientBlockType = gameInstance.MapModule.ClientBlockTypes[blockId];
		ClientItemBase item = gameInstance.ItemLibraryModule.GetItem(clientBlockType.Item);
		if (item == null)
		{
			context.State.State = (InteractionState)3;
			return;
		}
		bool flag = false;
		context.State.BlockFace_ = val;
		BlockMatcher[] matchers = Interaction.Matchers;
		foreach (BlockMatcher val2 in matchers)
		{
			if ((int)val2.Face > 0)
			{
				BlockFace val3 = val2.Face;
				if (!val2.StaticFace)
				{
					val3 = RotationHelper.RotateBlockFace(val3, clientBlockType);
				}
				if (val3 != val)
				{
					continue;
				}
			}
			if (val2.Block != null)
			{
				if (val2.Block.Id != null && val2.Block.Id != item.Id)
				{
					continue;
				}
				if (val2.Block.State != null)
				{
					string value = null;
					clientBlockType.StatesReverse?.TryGetValue(blockId, out value);
					value = value ?? "default";
					if (val2.Block.State != value)
					{
						continue;
					}
				}
				if (val2.Block.TagIndex != int.MinValue && (clientBlockType.TagIndexes == null || !clientBlockType.TagIndexes.ContainsKey(val2.Block.TagIndex)))
				{
					continue;
				}
			}
			flag = true;
			break;
		}
		if (flag)
		{
			context.State.State = (InteractionState)0;
		}
		else
		{
			context.State.State = (InteractionState)3;
		}
	}
}
