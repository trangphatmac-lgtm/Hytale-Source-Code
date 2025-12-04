using System;
using System.Collections.Generic;
using HytaleClient.Audio;
using HytaleClient.Data.ClientInteraction.Client;
using HytaleClient.Data.ClientInteraction.None;
using HytaleClient.Data.ClientInteraction.Server;
using HytaleClient.Data.Items;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;
using Wwise;

namespace HytaleClient.Data.ClientInteraction;

internal abstract class ClientInteraction
{
	public class ClientInteractionRules
	{
		private const int TAG_NOT_FOUND = int.MinValue;

		private static readonly HashSet<InteractionType> StandardInput = new HashSet<InteractionType>
		{
			(InteractionType)0,
			(InteractionType)1,
			(InteractionType)2,
			(InteractionType)6,
			(InteractionType)3,
			(InteractionType)4,
			(InteractionType)5,
			(InteractionType)7,
			(InteractionType)8,
			(InteractionType)15,
			(InteractionType)14
		};

		private static readonly Dictionary<InteractionType, HashSet<InteractionType>> DefaultInteractionBlockedBy = new Dictionary<InteractionType, HashSet<InteractionType>>
		{
			{
				(InteractionType)0,
				StandardInput
			},
			{
				(InteractionType)1,
				StandardInput
			},
			{
				(InteractionType)2,
				StandardInput
			},
			{
				(InteractionType)3,
				StandardInput
			},
			{
				(InteractionType)4,
				StandardInput
			},
			{
				(InteractionType)5,
				StandardInput
			},
			{
				(InteractionType)7,
				StandardInput
			},
			{
				(InteractionType)8,
				StandardInput
			},
			{
				(InteractionType)6,
				StandardInput
			},
			{
				(InteractionType)9,
				new HashSet<InteractionType>()
			},
			{
				(InteractionType)10,
				new HashSet<InteractionType>()
			},
			{
				(InteractionType)11,
				new HashSet<InteractionType>()
			},
			{
				(InteractionType)12,
				new HashSet<InteractionType>()
			},
			{
				(InteractionType)13,
				new HashSet<InteractionType>()
			},
			{
				(InteractionType)16,
				new HashSet<InteractionType>()
			},
			{
				(InteractionType)17,
				new HashSet<InteractionType>()
			},
			{
				(InteractionType)14,
				new HashSet<InteractionType>
				{
					(InteractionType)14,
					(InteractionType)15
				}
			},
			{
				(InteractionType)15,
				new HashSet<InteractionType>
				{
					(InteractionType)14,
					(InteractionType)15
				}
			},
			{
				(InteractionType)18,
				new HashSet<InteractionType>()
			},
			{
				(InteractionType)21,
				new HashSet<InteractionType>()
			},
			{
				(InteractionType)20,
				new HashSet<InteractionType>()
			},
			{
				(InteractionType)19,
				new HashSet<InteractionType>()
			},
			{
				(InteractionType)22,
				new HashSet<InteractionType> { (InteractionType)22 }
			},
			{
				(InteractionType)23,
				new HashSet<InteractionType> { (InteractionType)23 }
			},
			{
				(InteractionType)24,
				new HashSet<InteractionType> { (InteractionType)24 }
			},
			{
				(InteractionType)25,
				new HashSet<InteractionType> { (InteractionType)25 }
			}
		};

		public readonly HashSet<InteractionType> BlockedBy;

		public readonly int BlockedByBypassIndex;

		public readonly HashSet<InteractionType> Blocking;

		public readonly int BlockingBypassIndex;

		public readonly HashSet<InteractionType> InterruptedBy;

		public readonly int InterruptedByBypassIndex;

		public readonly HashSet<InteractionType> Interrupting;

		public readonly int InterruptingBypassIndex;

		public ClientInteractionRules(InteractionRules rules)
		{
			if (rules.BlockedBy != null)
			{
				BlockedBy = new HashSet<InteractionType>(rules.BlockedBy);
			}
			BlockedByBypassIndex = rules.BlockedByBypassIndex;
			if (rules.Blocking != null)
			{
				Blocking = new HashSet<InteractionType>(rules.Blocking);
			}
			BlockingBypassIndex = rules.BlockingBypassIndex;
			if (rules.InterruptedBy != null)
			{
				InterruptedBy = new HashSet<InteractionType>(rules.InterruptedBy);
			}
			InterruptedByBypassIndex = rules.InterruptedByBypassIndex;
			if (rules.Interrupting != null)
			{
				Interrupting = new HashSet<InteractionType>(rules.Interrupting);
			}
			InterruptingBypassIndex = rules.InterruptingBypassIndex;
		}

		public bool ValidateInterrupts(InteractionType type, HashSet<int> selfTags, InteractionType otherType, HashSet<int> otherTags, ClientInteractionRules otherRules)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			if (otherRules.InterruptedBy != null && otherRules.InterruptedBy.Contains(type) && (otherRules.InterruptedByBypassIndex == int.MinValue || !selfTags.Contains(otherRules.InterruptedByBypassIndex)))
			{
				return true;
			}
			if (Interrupting != null && Interrupting.Contains(otherType) && (InterruptingBypassIndex == int.MinValue || !otherTags.Contains(InterruptingBypassIndex)))
			{
				return true;
			}
			return false;
		}

		public bool ValidateBlocked(InteractionType type, HashSet<int> selfTags, InteractionType otherType, HashSet<int> otherTags, ClientInteractionRules otherRules)
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			HashSet<InteractionType> hashSet = BlockedBy ?? DefaultInteractionBlockedBy[type];
			if (hashSet.Contains(otherType) && (BlockedByBypassIndex == int.MinValue || !otherTags.Contains(BlockedByBypassIndex)))
			{
				return true;
			}
			if (otherRules.Blocking != null && otherRules.Blocking.Contains(type) && (otherRules.BlockingBypassIndex == int.MinValue || !selfTags.Contains(otherRules.BlockingBypassIndex)))
			{
				return true;
			}
			return false;
		}
	}

	public const float DefaultCooldownTimeSeconds = 0.35f;

	public const int UndefinedAsset = int.MinValue;

	public readonly int Id;

	public readonly Interaction Interaction;

	public readonly HashSet<int> Tags;

	public readonly ClientInteractionRules Rules;

	public ClientInteraction(int id, Interaction interaction)
	{
		Id = id;
		Interaction = interaction;
		Tags = ((interaction.Tags != null) ? new HashSet<int>(interaction.Tags) : new HashSet<int>());
		Rules = new ClientInteractionRules(interaction.Rules);
	}

	public void Tick(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Invalid comparison between Unknown and I4
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Invalid comparison between Unknown and I4
		int operationCounter = context.OperationCounter;
		int callDepth = context.Chain.GetCallDepth();
		if (!TickInternal(gameInstance, clickType, hasAnyButtonClick, firstRun, time, type, context))
		{
			Tick0(gameInstance, clickType, hasAnyButtonClick, firstRun, time, type, context);
		}
		InteractionState state = context.State.State;
		InteractionState val = state;
		if ((int)val <= 1 || (int)val == 3)
		{
			if (context.InstanceStore.TimeShift.HasValue)
			{
				context.SetTimeShift(context.InstanceStore.TimeShift.Value);
			}
			if (context.OperationCounter == operationCounter && callDepth == context.Chain.GetCallDepth())
			{
				context.OperationCounter++;
			}
		}
	}

	private bool TickInternal(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		Interaction.Settings.TryGetValue(gameInstance.GameMode, out var value);
		if (!firstRun && (context.AllowSkipChainOnClick || (value?.AllowSkipOnClick ?? false)) && hasAnyButtonClick)
		{
			context.State.State = (InteractionState)1;
			context.State.Progress = time;
			return true;
		}
		if (!Failed(context.State.State))
		{
			float val = 0f;
			if (Interaction.Effects != null && Interaction.Effects.WaitForAnimationToFinish)
			{
				ClientItemBase item = gameInstance.ItemLibraryModule.GetItem(context.HeldItem?.Id);
				EntityAnimation entityAnimation = ((Interaction.Effects?.ItemAnimationId == null) ? null : item?.GetAnimation(Interaction.Effects.ItemAnimationId));
				val = ((entityAnimation != null) ? ((float)entityAnimation.FirstPersonData.Duration * entityAnimation.Speed / 60f) : 0f);
			}
			float num = System.Math.Max(Interaction.RunTime, val);
			if (time < num)
			{
				context.State.State = (InteractionState)4;
			}
			else
			{
				if (num > 0f)
				{
					context.InstanceStore.TimeShift = time - num;
				}
				context.State.State = (InteractionState)0;
			}
			context.State.Progress = time;
		}
		return false;
	}

	protected abstract void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context);

	public virtual void Handle(GameInstance gameInstance, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Invalid comparison between Unknown and I4
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Invalid comparison between Unknown and I4
		//IL_0365: Unknown result type (might be due to invalid IL or missing references)
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Invalid comparison between Unknown and I4
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Invalid comparison between Unknown and I4
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		InteractionSyncData state = context.State;
		if (Interaction.Camera != null)
		{
			if (Interaction.Camera.FirstPerson != null)
			{
				if ((int)state.State == 4)
				{
					if (time >= context.InstanceStore.LastFirstPersonCameraTime)
					{
						InteractionCamera[] firstPerson = Interaction.Camera.FirstPerson;
						foreach (InteractionCamera val in firstPerson)
						{
							if (!(val.Time <= context.InstanceStore.LastFirstPersonCameraTime))
							{
								if (!(time > val.Time))
								{
									context.InstanceStore.LastFirstPersonCameraTime = val.Time;
									gameInstance.CharacterControllerModule.MovementController.SetFirstPersonCameraOffset(val.Time - time, new Vector3(val.Position.X, val.Position.Y, val.Position.Z), new Vector3(val.Rotation.Pitch, val.Rotation.Yaw, val.Rotation.Roll));
								}
								break;
							}
						}
					}
				}
				else
				{
					gameInstance.CharacterControllerModule.MovementController.SetFirstPersonCameraOffset(0f, Vector3.Zero, Vector3.Zero);
				}
			}
			if (Interaction.Camera.ThirdPerson != null)
			{
				if ((int)state.State == 4)
				{
					if (time >= context.InstanceStore.LastThirdPersonCameraTime)
					{
						InteractionCamera[] thirdPerson = Interaction.Camera.ThirdPerson;
						foreach (InteractionCamera val2 in thirdPerson)
						{
							if (!(val2.Time <= context.InstanceStore.LastThirdPersonCameraTime))
							{
								if (!(time > val2.Time))
								{
									context.InstanceStore.LastThirdPersonCameraTime = val2.Time;
									gameInstance.CharacterControllerModule.MovementController.SetThirdPersonCameraOffset(val2.Time - time, new Vector3(val2.Position.X, val2.Position.Y, val2.Position.Z), new Vector3(val2.Rotation.Pitch, val2.Rotation.Yaw, val2.Rotation.Roll));
								}
								break;
							}
						}
					}
				}
				else
				{
					gameInstance.CharacterControllerModule.MovementController.SetThirdPersonCameraOffset(0f, Vector3.Zero, Vector3.Zero);
				}
			}
		}
		bool isFirstInteraction = context.Chain.OperationIndex == 0;
		if ((int)state.State != 4)
		{
			if (firstRun && (int)state.State == 0)
			{
				HandlePlayFor(gameInstance, context.Entity, type, context, context.InstanceStore, cancel: false, isFirstInteraction);
			}
			HandlePlayFor(gameInstance, context.Entity, type, context, context.InstanceStore, cancel: true, isFirstInteraction);
		}
		else if (firstRun)
		{
			HandlePlayFor(gameInstance, context.Entity, type, context, context.InstanceStore, cancel: false, isFirstInteraction);
		}
	}

	public void Revert(GameInstance gameInstance, InteractionType type, InteractionContext context)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		bool isFirstInteraction = context.Chain.OperationIndex == 0;
		HandlePlayFor(gameInstance, context.Entity, type, context, context.InstanceStore, cancel: true, isFirstInteraction, force: true);
		Revert0(gameInstance, type, context);
	}

	protected abstract void Revert0(GameInstance gameInstance, InteractionType type, InteractionContext context);

	public void MatchServer(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Invalid comparison between Unknown and I4
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Invalid comparison between Unknown and I4
		int operationCounter = context.OperationCounter;
		int callDepth = context.Chain.GetCallDepth();
		MatchServer0(gameInstance, clickType, hasAnyButtonClick, type, context);
		InteractionState state = context.State.State;
		InteractionState val = state;
		if (((int)val <= 1 || (int)val == 3) && context.OperationCounter == operationCounter && callDepth == context.Chain.GetCallDepth())
		{
			context.OperationCounter++;
		}
	}

	protected abstract void MatchServer0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context);

	public virtual void Compile(InteractionModule module, ClientRootInteraction.OperationsBuilder builder)
	{
		builder.AddOperation(Id);
	}

	public virtual InteractionChain MapForkChain(InteractionContext context, InteractionChainData data)
	{
		return null;
	}

	public void HandlePlayFor(GameInstance gameInstance, Entity entity, InteractionType type, InteractionContext context, InteractionMetaStore metaStore, bool cancel, bool isFirstInteraction, bool force = false)
	{
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Invalid comparison between Unknown and I4
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Invalid comparison between Unknown and I4
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Invalid comparison between Unknown and I4
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		if (!cancel)
		{
			entity.RunningInteractions.Add(Id);
		}
		else
		{
			entity.RunningInteractions.Remove(Id);
		}
		InteractionEffects effects = Interaction.Effects;
		string text = effects?.ItemAnimationId;
		ClientItemBase item = gameInstance.ItemLibraryModule.GetItem(context.HeldItem?.Id);
		bool flag = entity.NetworkId == gameInstance.LocalPlayer.NetworkId;
		EntityAnimation value = null;
		if (text != null)
		{
			if (effects != null && effects.ItemPlayerAnimationsId != null && gameInstance.ItemLibraryModule.GetItemPlayerAnimation(effects.ItemPlayerAnimationsId, out var ret))
			{
				ret.Animations.TryGetValue(text, out value);
			}
			else if (item != null)
			{
				value = item.GetAnimation(text);
			}
			else
			{
				gameInstance.ItemLibraryModule.DefaultItemPlayerAnimations.Animations.TryGetValue(text, out value);
			}
		}
		if (isFirstInteraction && value == null)
		{
			text = (((int)type == 1) ? "SecondaryAction" : "Attack");
			value = item?.GetAnimation(text);
		}
		if (flag && effects?.MovementEffects_ != null)
		{
			gameInstance.LocalPlayer.UpdateActiveInteraction(Id, cancel);
		}
		if (cancel)
		{
			if ((int)type == 6 && !flag)
			{
				entity.RestoreCharacterItem();
			}
			if (value != null && effects != null && (effects.ClearAnimationOnFinish || force))
			{
				entity.SetActionAnimation(EntityAnimation.Empty);
			}
			if (effects != null && effects.ClearSoundEventOnFinish && metaStore.SoundEventReference.PlaybackId != -1)
			{
				gameInstance.AudioModule.ActionOnEvent(ref metaStore.SoundEventReference, (AkActionOnEventType)0);
			}
			return;
		}
		if ((int)type == 6 && !flag)
		{
			entity.SetCharacterItemConsumable();
		}
		if (value != null)
		{
			if (entity is PlayerEntity playerEntity)
			{
				playerEntity.CurrentFirstPersonAnimationId = text;
			}
			entity.SetActionAnimation(value, 0f, Interaction.AllowIndefiniteHold, force: true);
		}
		if (effects == null)
		{
			return;
		}
		ModelParticle[] array = ((gameInstance.CameraModule.Controller.IsFirstPerson && flag && effects.FirstPersonParticles != null) ? effects.FirstPersonParticles : effects.Particles);
		if (array != null || effects.Trails != null)
		{
			entity.ClearCombatSequenceEffects();
			entity.AddCombatSequenceEffects(array, effects.Trails);
		}
		uint networkWwiseId = ResourceManager.GetNetworkWwiseId(effects.LocalSoundEventIndex);
		if (flag)
		{
			if (effects.IgnoreSoundObject)
			{
				gameInstance.AudioModule.PlaySoundEvent(networkWwiseId, entity.Position, entity.BodyOrientation, ref metaStore.SoundEventReference);
			}
			else
			{
				gameInstance.AudioModule.PlaySoundEvent(networkWwiseId, entity.SoundObjectReference, ref metaStore.SoundEventReference);
			}
			if (effects.CameraShake != null)
			{
				gameInstance.CameraModule.CameraShakeController.PlayCameraShake(effects.CameraShake.CameraShakeId, effects.CameraShake.Intensity, effects.CameraShake.Mode);
			}
		}
		uint networkWwiseId2 = ResourceManager.GetNetworkWwiseId(effects.WorldSoundEventIndex);
		if (networkWwiseId2 != 0 && !flag && (networkWwiseId == 0 || !flag))
		{
			if (effects.IgnoreSoundObject)
			{
				gameInstance.AudioModule.PlaySoundEvent(networkWwiseId2, entity.Position, entity.BodyOrientation, ref metaStore.SoundEventReference);
			}
			else
			{
				gameInstance.AudioModule.PlaySoundEvent(networkWwiseId2, entity.SoundObjectReference, ref metaStore.SoundEventReference);
			}
		}
	}

	public override string ToString()
	{
		return string.Format("{0}: {1}, {2}: {3}", "Id", Id, "Interaction", Interaction);
	}

	public static ClientInteraction Parse(int id, Interaction interaction)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Expected I4, but got Unknown
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		Type type_ = interaction.Type_;
		Type val = type_;
		return (int)val switch
		{
			0 => new SimpleInteraction(id, interaction), 
			1 => new SimpleBlockInteraction(id, interaction), 
			2 => new PlaceBlockInteraction(id, interaction), 
			3 => new BreakBlockInteraction(id, interaction), 
			4 => new PickBlockInteraction(id, interaction), 
			5 => new UseBlockInteraction(id, interaction), 
			6 => new UseEntityInteraction(id, interaction), 
			7 => new BuilderToolInteraction(id, interaction), 
			8 => new ModifyInventoryInteraction(id, interaction), 
			9 => new ChargingInteraction(id, interaction), 
			10 => new ChainingInteraction(id, interaction), 
			11 => new ConditionInteraction(id, interaction), 
			17 => new ConditionalPlaceCropInteraction(id, interaction), 
			14 => new ReplaceInteraction(id, interaction), 
			12 => new StatsConditionInteraction(id, interaction), 
			18 => new FirstClickInteraction(id, interaction), 
			19 => new RefillContainerInteraction(id, interaction), 
			13 => new BlockConditionInteraction(id, interaction), 
			16 => new ChangeStateInteraction(id, interaction), 
			15 => new ChangeBlockInteraction(id, interaction), 
			20 => new SelectInteraction(id, interaction), 
			21 => new DamageEntityInteraction(id, interaction), 
			22 => new RepeatInteraction(id, interaction), 
			23 => new ParallelInteraction(id, interaction), 
			24 => new ChangeActiveSlotInteraction(id, interaction), 
			25 => new WieldingInteraction(id, interaction), 
			26 => new EffectConditionInteraction(id, interaction), 
			27 => new ApplyForceInteraction(id, interaction), 
			28 => new ApplyEffectInteraction(id, interaction), 
			29 => new ClearEntityEffectInteraction(id, interaction), 
			30 => new SerialInteraction(id, interaction), 
			31 => new ChangeStatInteraction(id, interaction), 
			32 => new MovementConditionInteraction(id, interaction), 
			33 => new ProjectileInteraction(id, interaction), 
			34 => new RemoveEntityInteraction(id, interaction), 
			35 => new ResetCooldownInteraction(id, interaction), 
			36 => new TriggerCooldownInteraction(id, interaction), 
			37 => new CooldownConditionInteraction(id, interaction), 
			38 => new ChainFlagInteraction(id, interaction), 
			39 => new IncrementCooldown(id, interaction), 
			40 => new CancelChainInteraction(id, interaction), 
			41 => new SetChainVariableInteraction(id, interaction), 
			43 => new ClearChainVariableInteraction(id, interaction), 
			42 => new EvaluateChainVariableInteraction(id, interaction), 
			44 => new RunRootInteraction(id, interaction), 
			_ => throw new Exception($"Unknown Interaction type: {interaction.Type_}"), 
		};
	}

	public static bool Failed(InteractionState state)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected I4, but got Unknown
		InteractionState val = state;
		InteractionState val2 = val;
		switch ((int)val2)
		{
		case 1:
		case 2:
		case 3:
			return true;
		case 0:
		case 4:
			return false;
		default:
			throw new Exception("Unknown state: " + ((object)(InteractionState)(ref state)).ToString());
		}
	}

	public static Entity GetEntity(GameInstance gameInstance, InteractionContext context, InteractionTarget target)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected I4, but got Unknown
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		return (int)target switch
		{
			0 => context.Entity, 
			1 => gameInstance.LocalPlayer, 
			2 => context.MetaStore.TargetEntity, 
			_ => throw new ArgumentOutOfRangeException("target", target, null), 
		};
	}

	public static bool TryGetEntity(GameInstance gameInstance, InteractionContext context, InteractionTarget target, out Entity entity)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		entity = GetEntity(gameInstance, context, target);
		return entity != null;
	}
}
