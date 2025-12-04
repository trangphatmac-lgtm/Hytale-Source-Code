using System;
using System.Linq;
using HytaleClient.Audio;
using HytaleClient.Data.FX;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Particles;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;
using Wwise;

namespace HytaleClient.Data.ClientInteraction.None;

internal class DamageEntityInteraction : ClientInteraction
{
	private const int FailedLabelIndex = 0;

	private const int SuccessLabelIndex = 1;

	private const int BlockedLabelIndex = 2;

	private const int AngledLabelOffset = 3;

	private string[] _sortedTargetDamageKeys;

	public DamageEntityInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
		_sortedTargetDamageKeys = interaction.TargetedDamage_.Keys.ToArray();
		Array.Sort(_sortedTargetDamageKeys);
	}

	protected override void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Invalid comparison between Unknown and I4
		//IL_04d5: Unknown result type (might be due to invalid IL or missing references)
		Entity targetEntity = context.MetaStore.TargetEntity;
		if (targetEntity == null || targetEntity.IsInvulnerable())
		{
			context.Jump(context.Labels[0]);
			context.State.State = (InteractionState)3;
			return;
		}
		bool flag = false;
		int num = 1;
		Vector4? hitLocation = context.MetaStore.HitLocation;
		Vector3 position = targetEntity.Position;
		Vector3 position2 = context.Entity.Position;
		float num2 = (float)System.Math.Atan2(position2.X - position.X, position2.Z - position.Z);
		DamageEffects val = null;
		for (int i = 0; i < targetEntity.RunningInteractions.Count; i++)
		{
			ClientInteraction clientInteraction = gameInstance.InteractionModule.Interactions[targetEntity.RunningInteractions[i]];
			if ((int)clientInteraction.Interaction.Type_ != 25)
			{
				continue;
			}
			if (clientInteraction.Interaction.HasModifiers)
			{
				val = clientInteraction.Interaction.BlockedEffects;
				flag = true;
				break;
			}
			if (clientInteraction.Interaction.AngledWielding_ != null)
			{
				float num3 = MathHelper.WrapAngle(num2 + (float)System.Math.PI - targetEntity.BodyOrientation.Yaw);
				if (System.Math.Abs(MathHelper.CompareAngle(num3, clientInteraction.Interaction.AngledWielding_.AngleRad)) < (double)clientInteraction.Interaction.AngledWielding_.AngleDistanceRad)
				{
					val = clientInteraction.Interaction.BlockedEffects;
					flag = true;
					break;
				}
			}
		}
		bool flag2 = false;
		if (Interaction.AngledDamage_ != null)
		{
			float num4 = MathHelper.WrapAngle(num2 + (float)System.Math.PI - targetEntity.BodyOrientation.Yaw);
			for (int j = 0; j < Interaction.AngledDamage_.Length; j++)
			{
				AngledDamage val2 = Interaction.AngledDamage_[j];
				if (System.Math.Abs(MathHelper.CompareAngle(num4, val2.Angle)) < val2.AngleDistance)
				{
					num = 3 + j;
					ApplyDamageEffects(context, val ?? val2.DamageEffects_, gameInstance, targetEntity, hitLocation, num2);
					flag2 = true;
					break;
				}
			}
		}
		string hitDetail = context.MetaStore.HitDetail;
		if (hitDetail != null && Interaction.TargetedDamage_.TryGetValue(hitDetail, out var value))
		{
			num = value.Index;
			ApplyDamageEffects(context, val ?? value.DamageEffects_, gameInstance, targetEntity, hitLocation, num2);
			flag2 = true;
		}
		if (!flag2)
		{
			ApplyDamageEffects(context, val ?? Interaction.DamageEffects_, gameInstance, targetEntity, hitLocation, num2);
		}
		if (!flag && context.MetaStore.SelectMetaStore != null && Interaction.EntityStatsOnHit != null)
		{
			InteractionMetaStore selectMetaStore = context.MetaStore.SelectMetaStore;
			selectMetaStore.Sequence++;
			context.InstanceStore.PredictedStats = (EntityStatUpdate[])(object)new EntityStatUpdate[Interaction.EntityStatsOnHit.Length];
			for (int k = 0; k < Interaction.EntityStatsOnHit.Length; k++)
			{
				EntityStatOnHit val3 = Interaction.EntityStatsOnHit[k];
				float num5 = ((selectMetaStore.Sequence > val3.MultipliersPerEntitiesHit.Length) ? val3.MultiplierPerExtraEntityHit : val3.MultipliersPerEntitiesHit[selectMetaStore.Sequence - 1]);
				context.InstanceStore.PredictedStats[k] = gameInstance.LocalPlayer.AddStatValue(val3.EntityStatIndex, val3.Amount * num5);
			}
		}
		if (gameInstance.InteractionModule.ShowSelectorDebug && hitLocation.HasValue)
		{
			Vector4 valueOrDefault = hitLocation.GetValueOrDefault();
			if (true)
			{
				Mesh result = default(Mesh);
				MeshProcessor.CreateSphere(ref result, 5, 8, 0.2f, 0);
				Matrix matrix = Matrix.CreateTranslation(valueOrDefault.X, valueOrDefault.Y, valueOrDefault.Z);
				gameInstance.InteractionModule.SelectorDebugMeshes.Add(new InteractionModule.DebugSelectorMesh(matrix, result, 5f, new Vector3(1f, 1f, 0f)));
			}
		}
		gameInstance.App.Interface.InGameView.ReticleComponent.OnClientEvent((ItemReticleClientEvent)0);
		if (flag)
		{
			context.Jump(context.Labels[2]);
		}
		else
		{
			context.Jump(context.Labels[num]);
		}
		context.State.State = (InteractionState)0;
	}

	private static void ApplyDamageEffects(InteractionContext context, DamageEffects damageEffects, GameInstance gameInstance, Entity target, Vector4? hitPos, float yawAngle)
	{
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		if (damageEffects == null)
		{
			return;
		}
		WorldParticle[] worldParticles = damageEffects.WorldParticles;
		if (worldParticles != null && hitPos.HasValue)
		{
			Vector4 valueOrDefault = hitPos.GetValueOrDefault();
			if (true)
			{
				Quaternion.CreateFromYaw(yawAngle, out var result);
				context.InstanceStore.DamageParticles = new ParticleSystemProxy[worldParticles.Length];
				for (int i = 0; i < worldParticles.Length; i++)
				{
					WorldParticle val = worldParticles[i];
					if (gameInstance.ParticleSystemStoreModule.TrySpawnSystem(val.SystemId, out var particleSystemProxy))
					{
						context.InstanceStore.DamageParticles[i] = particleSystemProxy;
						Vector3 value = ((val.PositionOffset != null) ? new Vector3(val.PositionOffset.X, val.PositionOffset.Y, val.PositionOffset.Z) : Vector3.Zero);
						value = Vector3.Transform(value, result);
						particleSystemProxy.Position = new Vector3(valueOrDefault.X + value.X, valueOrDefault.Y + value.Y, valueOrDefault.Z + value.Z);
						Direction val2 = (Direction)((val.RotationOffset != null) ? ((object)val.RotationOffset) : ((object)new Direction(0f, 0f, 0f)));
						Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(val2.Yaw), MathHelper.ToRadians(val2.Pitch), MathHelper.ToRadians(val2.Roll), out var result2);
						particleSystemProxy.Rotation = result * result2;
						if (val.Color_ != null)
						{
							particleSystemProxy.DefaultColor = UInt32Color.FromRGBA((byte)val.Color_.Red, (byte)val.Color_.Green, (byte)val.Color_.Blue, byte.MaxValue);
						}
						particleSystemProxy.Scale = val.Scale;
					}
				}
			}
		}
		if (target != null)
		{
			ModelParticle[] modelParticles = damageEffects.ModelParticles;
			if (modelParticles != null)
			{
				ParticleProtocolInitializer.Initialize(modelParticles, out var clientModelParticles, gameInstance.EntityStoreModule.NodeNameManager);
				target.AddModelParticles(clientModelParticles);
			}
			target.PredictStatusAnimation("Hurt");
		}
		uint networkWwiseId = ResourceManager.GetNetworkWwiseId(damageEffects.SoundEventIndex);
		if (hitPos.HasValue)
		{
			Vector4 valueOrDefault2 = hitPos.GetValueOrDefault();
			if (true)
			{
				gameInstance.AudioModule.PlaySoundEvent(networkWwiseId, new Vector3(valueOrDefault2.X, valueOrDefault2.Y, valueOrDefault2.Z), Vector3.Zero, ref context.InstanceStore.DamageSoundEventReference);
			}
		}
	}

	public override void Compile(InteractionModule module, ClientRootInteraction.OperationsBuilder builder)
	{
		AngledDamage[] angledDamage_ = Interaction.AngledDamage_;
		ClientRootInteraction.Label[] array = new ClientRootInteraction.Label[3 + ((angledDamage_ != null) ? angledDamage_.Length : 0) + Interaction.TargetedDamage_.Count];
		builder.AddOperation(Id, array);
		ClientRootInteraction.Label label = builder.CreateUnresolvedLabel();
		array[0] = builder.CreateLabel();
		if (Interaction.Failed != int.MinValue)
		{
			ClientInteraction clientInteraction = module.Interactions[Interaction.Failed];
			clientInteraction.Compile(module, builder);
		}
		builder.Jump(label);
		array[1] = builder.CreateLabel();
		if (Interaction.Next != int.MinValue)
		{
			ClientInteraction clientInteraction2 = module.Interactions[Interaction.Next];
			clientInteraction2.Compile(module, builder);
		}
		builder.Jump(label);
		array[2] = builder.CreateLabel();
		if (Interaction.Blocked != int.MinValue)
		{
			ClientInteraction clientInteraction3 = module.Interactions[Interaction.Blocked];
			clientInteraction3.Compile(module, builder);
		}
		builder.Jump(label);
		int num = 3;
		if (angledDamage_ != null)
		{
			foreach (AngledDamage val in angledDamage_)
			{
				array[num++] = builder.CreateLabel();
				if (val.Next != int.MinValue)
				{
					ClientInteraction clientInteraction4 = module.Interactions[val.Next];
					clientInteraction4.Compile(module, builder);
				}
				builder.Jump(label);
			}
		}
		string[] sortedTargetDamageKeys = _sortedTargetDamageKeys;
		foreach (string key in sortedTargetDamageKeys)
		{
			TargetedDamage val2 = Interaction.TargetedDamage_[key];
			array[num++] = builder.CreateLabel();
			if (val2.Next != int.MinValue)
			{
				ClientInteraction clientInteraction5 = module.Interactions[val2.Next];
				clientInteraction5.Compile(module, builder);
			}
			builder.Jump(label);
		}
		builder.ResolveLabel(label);
	}

	protected override void Revert0(GameInstance gameInstance, InteractionType type, InteractionContext context)
	{
		Entity targetEntity = context.MetaStore.TargetEntity;
		if (targetEntity != null)
		{
			targetEntity.SetServerAnimation(null, (AnimationSlot)1, 0f);
			targetEntity.PredictedStatusCount--;
		}
		ParticleSystemProxy[] damageParticles = context.InstanceStore.DamageParticles;
		if (damageParticles != null)
		{
			ParticleSystemProxy[] array = damageParticles;
			foreach (ParticleSystemProxy particleSystemProxy in array)
			{
				particleSystemProxy.Expire(instant: true);
			}
		}
		if (context.InstanceStore.DamageSoundEventReference.PlaybackId != -1)
		{
			gameInstance.AudioModule.ActionOnEvent(ref context.InstanceStore.DamageSoundEventReference, (AkActionOnEventType)0);
		}
		if (context.InstanceStore.PredictedStats != null)
		{
			for (int j = 0; j < context.InstanceStore.PredictedStats.Length; j++)
			{
				EntityStatUpdate update = context.InstanceStore.PredictedStats[j];
				gameInstance.LocalPlayer.CancelStatPrediction(Interaction.EntityStatsOnHit[j].EntityStatIndex, update);
			}
		}
	}

	protected override void MatchServer0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		context.State.State = context.ServerData.State;
		context.Jump(context.Labels[context.ServerData.NextLabel]);
	}
}
