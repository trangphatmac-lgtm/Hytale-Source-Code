using System;
using System.Collections.Generic;
using HytaleClient.Audio;
using HytaleClient.Data.Items;
using HytaleClient.Data.Map;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class ConditionalPlaceCropInteraction : SimpleInstantInteraction
{
	private readonly Dictionary<string, int> seedToCrop;

	private readonly int[] tilledSoil;

	public ConditionalPlaceCropInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
		seedToCrop = interaction.SeedToCrop;
		tilledSoil = interaction.TilledSoilBlocks;
	}

	protected override void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Invalid comparison between Unknown and I4
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Invalid comparison between Unknown and I4
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Invalid comparison between Unknown and I4
		//IL_0616: Unknown result type (might be due to invalid IL or missing references)
		//IL_061c: Invalid comparison between Unknown and I4
		//IL_0814: Unknown result type (might be due to invalid IL or missing references)
		//IL_072b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0847: Unknown result type (might be due to invalid IL or missing references)
		//IL_0851: Expected O, but got Unknown
		//IL_085a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0861: Unknown result type (might be due to invalid IL or missing references)
		//IL_0868: Unknown result type (might be due to invalid IL or missing references)
		//IL_086d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0877: Expected O, but got Unknown
		if (seedToCrop == null || context.MetaStore.TargetBlockRaw == null)
		{
			return;
		}
		ClientItemBase primaryItem = gameInstance.LocalPlayer.PrimaryItem;
		HitDetection.RaycastHit targetBlockHit = gameInstance.InteractionModule.TargetBlockHit;
		if (!seedToCrop.TryGetValue(primaryItem.Id, out var value))
		{
			context.State.State = (InteractionState)3;
			return;
		}
		ClientBlockType clientBlockType = gameInstance.MapModule.ClientBlockTypes[value];
		int num = (int)System.Math.Floor(targetBlockHit.BlockPosition.X);
		int num2 = (int)System.Math.Floor(targetBlockHit.BlockPosition.Y);
		int num3 = (int)System.Math.Floor(targetBlockHit.BlockPosition.Z);
		int num4 = targetBlockHit.BlockId;
		ClientBlockType clientBlockType2 = gameInstance.MapModule.ClientBlockTypes[num4];
		if ((int)clientBlockType2.CollisionMaterial != 0 || (int)clientBlockType.CollisionMaterial != 1)
		{
			num = (int)System.Math.Floor(targetBlockHit.HitPosition.X + targetBlockHit.Normal.X * 0.5f);
			num2 = (int)System.Math.Floor(targetBlockHit.HitPosition.Y + targetBlockHit.Normal.Y * 0.5f);
			num3 = (int)System.Math.Floor(targetBlockHit.HitPosition.Z + targetBlockHit.Normal.Z * 0.5f);
			num4 = gameInstance.MapModule.GetBlock(num, num2, num3, int.MaxValue);
			if (num4 == int.MaxValue)
			{
				context.State.State = (InteractionState)3;
				return;
			}
			clientBlockType2 = gameInstance.MapModule.ClientBlockTypes[num4];
			if (num4 != 0 && ((int)clientBlockType2.CollisionMaterial == 1 || ((int)clientBlockType2.CollisionMaterial == 0 && (int)clientBlockType.CollisionMaterial != 1)))
			{
				context.State.State = (InteractionState)3;
				return;
			}
		}
		Vector3 rotation = gameInstance.CameraModule.Controller.Rotation;
		int value2;
		if (clientBlockType.Variants.ContainsKey("Pitch=90"))
		{
			if ((float)num2 == targetBlockHit.BlockPosition.Y)
			{
				if ((float)num3 == targetBlockHit.BlockPosition.Z)
				{
					if ((float)num > targetBlockHit.BlockPosition.X || !clientBlockType.Variants.TryGetValue("Yaw=270|Pitch=90", out value2))
					{
						value2 = clientBlockType.Variants["Yaw=90|Pitch=90"];
					}
				}
				else if ((float)num3 > targetBlockHit.BlockPosition.Z || !clientBlockType.Variants.TryGetValue("Yaw=180|Pitch=90", out value2))
				{
					value2 = clientBlockType.Variants["Pitch=90"];
				}
			}
			else if ((float)num2 > targetBlockHit.BlockPosition.Y || !clientBlockType.Variants.TryGetValue("Pitch=180", out value2))
			{
				value2 = (ushort)primaryItem.BlockId;
			}
		}
		else if (clientBlockType.Variants.ContainsKey("Yaw=90") && !clientBlockType.Variants.ContainsKey("Yaw=180"))
		{
			value2 = ((rotation.Y >= -(float)System.Math.PI / 4f && rotation.Y <= (float)System.Math.PI / 4f) ? ((ushort)primaryItem.BlockId) : ((!(rotation.Y >= (float)System.Math.PI * 3f / 4f) && !(rotation.Y <= (float)System.Math.PI * -3f / 4f)) ? clientBlockType.Variants["Yaw=90"] : ((ushort)primaryItem.BlockId)));
		}
		else
		{
			string text = ((rotation.Y >= -(float)System.Math.PI / 4f && rotation.Y <= (float)System.Math.PI / 4f) ? "" : ((rotation.Y >= (float)System.Math.PI / 4f && rotation.Y <= (float)System.Math.PI * 3f / 4f) ? "Yaw=90" : ((rotation.Y >= (float)System.Math.PI * 3f / 4f || rotation.Y <= (float)System.Math.PI * -3f / 4f) ? "Yaw=180" : "Yaw=270")));
			string text2 = (((float)num2 < targetBlockHit.BlockPosition.Y) ? "Pitch=180" : "");
			string text3 = "";
			if (text2 == "Pitch=180")
			{
				string text4 = text;
				string text5 = text4;
				if (text5 == null || text5.Length != 0)
				{
					switch (text5)
					{
					case "Yaw=180":
						text = "";
						break;
					case "Yaw=90":
						text = "Yaw=270";
						break;
					case "Yaw=270":
						text = "Yaw=90";
						break;
					}
				}
				else
				{
					text = "Yaw=180";
				}
			}
			if ((!(text != "") || !(text2 != "") || !(text3 != "") || !clientBlockType.Variants.TryGetValue(text + "|" + text2 + "|" + text3, out value2)) && (!(text3 != "") || !clientBlockType.Variants.TryGetValue(text3 ?? "", out value2)) && (!(text2 != "") || !clientBlockType.Variants.TryGetValue(text2 ?? "", out value2)) && (!(text != "") || !clientBlockType.Variants.TryGetValue(text ?? "", out value2)))
			{
				value2 = (ushort)primaryItem.BlockId;
			}
		}
		ClientBlockType clientBlockType3 = gameInstance.MapModule.ClientBlockTypes[value2];
		if ((int)clientBlockType2.CollisionMaterial == 2 && clientBlockType3.Variants.TryGetValue("Fluid=" + clientBlockType2.Name, out var value3))
		{
			value2 = value3;
			clientBlockType3 = gameInstance.MapModule.ClientBlockTypes[value2];
		}
		BlockHitbox blockHitbox = gameInstance.ServerSettings.BlockHitboxes[clientBlockType3.HitboxType];
		Entity[] allEntities = gameInstance.EntityStoreModule.GetAllEntities();
		int entitiesCount = gameInstance.EntityStoreModule.GetEntitiesCount();
		for (int i = 0; i < entitiesCount; i++)
		{
			Entity entity = allEntities[i];
			if (entity.Type != Entity.EntityType.Character)
			{
				continue;
			}
			BoundingBox hitbox = entity.Hitbox;
			hitbox.Translate(entity.Position);
			int num5 = num - clientBlockType3.FillerX;
			int num6 = num2 - clientBlockType3.FillerY;
			int num7 = num3 - clientBlockType3.FillerZ;
			for (int j = 0; j < blockHitbox.Boxes.Length; j++)
			{
				BoundingBox box = blockHitbox.Boxes[j];
				if (hitbox.IntersectsExclusive(box, num5, num6, num7))
				{
					context.State.State = (InteractionState)3;
					return;
				}
			}
		}
		context.InstanceStore.OldBlockId = num4;
		if (gameInstance.ServerSettings.BlockSoundSets[clientBlockType.BlockSoundSetIndex].SoundEventIndices.TryGetValue((BlockSoundEvent)5, out var value4))
		{
			uint networkWwiseId = ResourceManager.GetNetworkWwiseId(value4);
			if (networkWwiseId != 0)
			{
				Vector3 position = new Vector3((float)num + 0.5f, (float)num2 + 0.5f, (float)num3 + 0.5f);
				gameInstance.AudioModule.PlaySoundEvent(networkWwiseId, position, Vector3.Zero);
			}
		}
		gameInstance.MapModule.SetClientBlock(num, num2, num3, value2);
		ClientItemStack hotbarItem = gameInstance.InventoryModule.GetHotbarItem(gameInstance.InventoryModule.HotbarActiveSlot);
		if ((int)gameInstance.GameMode == 0 && hotbarItem != null && hotbarItem.Quantity == 1)
		{
			context.HeldItem = null;
		}
		context.State.BlockPosition_ = new BlockPosition(num, num2, num3);
		context.State.BlockRotation_ = new BlockRotation(clientBlockType3.RotationYaw, clientBlockType3.RotationPitch, clientBlockType3.RotationRoll);
	}

	public override void Handle(GameInstance gameInstance, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Invalid comparison between Unknown and I4
		base.Handle(gameInstance, firstRun, time, type, context);
		InteractionSyncData state = context.State;
		if ((int)state.State == 3)
		{
			int oldBlockId = context.InstanceStore.OldBlockId;
			if (state?.BlockPosition_ != null && oldBlockId != int.MaxValue)
			{
				gameInstance.MapModule.SetClientBlock(state.BlockPosition_.X, state.BlockPosition_.Y, state.BlockPosition_.Z, oldBlockId);
			}
		}
	}
}
