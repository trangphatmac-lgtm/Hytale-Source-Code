using System;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Entities.Projectile;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class ProjectileInteraction : SimpleInstantInteraction
{
	public ProjectileInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
	}

	protected override void FirstRun(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		Entity entity = context.Entity;
		Vector3 position = entity.Position;
		position.Y += entity.EyeOffset;
		context.State.AttackerPos = position.ToPositionPacket();
		context.State.AttackerRot = entity.LookOrientation.ToDirectionPacket();
		Guid guid = Guid.NewGuid();
		context.State.GeneratedUUID = guid;
		Quaternion rotation = Quaternion.CreateFromYawPitchRoll(entity.LookOrientation.Yaw, entity.LookOrientation.Pitch, 0f);
		Vector3 direction = Vector3.Transform(Vector3.Forward, rotation);
		context.InstanceStore.Projectile = PredictedProjectile.Spawn(guid, gameInstance, Interaction.ProjectileConfig_, gameInstance.LocalPlayer, position, direction);
	}

	protected override void Revert0(GameInstance gameInstance, InteractionType type, InteractionContext context)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		base.Revert0(gameInstance, type, context);
		if (context.InstanceStore.Projectile != null)
		{
			int toDespawn = context.InstanceStore.Projectile.NetworkId;
			gameInstance.Engine.RunOnMainThread(context.InstanceStore.Projectile, delegate
			{
				gameInstance.EntityStoreModule.Despawn(toDespawn);
			}, allowCallFromMainThread: true);
		}
	}
}
