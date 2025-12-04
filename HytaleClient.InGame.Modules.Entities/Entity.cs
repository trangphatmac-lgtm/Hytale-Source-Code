using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HytaleClient.Audio;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Data.Characters;
using HytaleClient.Data.Entities;
using HytaleClient.Data.EntityStats;
using HytaleClient.Data.EntityUI;
using HytaleClient.Data.FX;
using HytaleClient.Data.Items;
using HytaleClient.Data.Map;
using HytaleClient.Graphics;
using HytaleClient.Graphics.BlockyModels;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Graphics.Particles;
using HytaleClient.Graphics.Trails;
using HytaleClient.InGame.Modules.Camera.Controllers;
using HytaleClient.InGame.Modules.Entities.Projectile;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Wwise;

namespace HytaleClient.InGame.Modules.Entities;

internal class Entity : Disposable
{
	public struct CombatText
	{
		public double HitAngleDeg;

		public string Text;
	}

	public enum EntityType
	{
		None,
		Character,
		Item,
		Block
	}

	private struct ServerEffectEntry
	{
		public long CreationTime;

		public EntityEffectUpdate Update;
	}

	public struct UniqueEntityEffect
	{
		public struct StatModifier
		{
			public int StatIndex;

			public float Amount;
		}

		public readonly int NetworkEffectIndex;

		public Vector3 BottomTint;

		public Vector3 TopTint;

		public bool IsExpiring;

		public string ScreenEffect;

		public List<EntityParticle> ParticleSystems;

		public ModelRenderer ModelRenderer;

		public EntityAnimation SpawnAnimation;

		public EntityAnimation IdleAnimation;

		public EntityAnimation DespawnAnimation;

		public StatModifier[] StatModifiers;

		public ValueType ValueType;

		public AudioDevice.SoundEventReference SoundEventReference;

		public bool IsInfinite;

		public bool IsDebuff;

		public string StatusEffectIcon;

		public float RemainingDuration;

		private float _timeToNextTick;

		public UniqueEntityEffect(EntityEffect entityEffect, int networkEffectIndex)
		{
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			NetworkEffectIndex = networkEffectIndex;
			ScreenEffect = entityEffect.ApplicationEffects_?.ScreenEffect;
			IsExpiring = false;
			IsInfinite = entityEffect.Infinite;
			IsDebuff = entityEffect.Debuff;
			StatusEffectIcon = entityEffect.StatusEffectIcon;
			RemainingDuration = entityEffect.Duration;
			ValueType = entityEffect.ValueType_;
			if (entityEffect.StatModifiers != null && entityEffect.DamageCalculatorCooldown == 0.0)
			{
				StatModifiers = new StatModifier[entityEffect.StatModifiers.Count];
				int num = 0;
				foreach (KeyValuePair<int, float> statModifier in entityEffect.StatModifiers)
				{
					StatModifiers[num++] = new StatModifier
					{
						StatIndex = statModifier.Key,
						Amount = statModifier.Value
					};
				}
			}
			else
			{
				StatModifiers = null;
			}
			if (entityEffect.ApplicationEffects_?.EntityBottomTint != null)
			{
				BottomTint = new Vector3((float)(int)(byte)entityEffect.ApplicationEffects_.EntityBottomTint.Red / 255f, (float)(int)(byte)entityEffect.ApplicationEffects_.EntityBottomTint.Green / 255f, (float)(int)(byte)entityEffect.ApplicationEffects_.EntityBottomTint.Blue / 255f);
			}
			else
			{
				BottomTint = Vector3.Zero;
			}
			if (entityEffect.ApplicationEffects_?.EntityTopTint != null)
			{
				TopTint = new Vector3((float)(int)(byte)entityEffect.ApplicationEffects_.EntityTopTint.Red / 255f, (float)(int)(byte)entityEffect.ApplicationEffects_.EntityTopTint.Green / 255f, (float)(int)(byte)entityEffect.ApplicationEffects_.EntityTopTint.Blue / 255f);
			}
			else
			{
				TopTint = Vector3.Zero;
			}
			ParticleSystems = null;
			ModelRenderer = null;
			SpawnAnimation = null;
			IdleAnimation = null;
			DespawnAnimation = null;
			SoundEventReference = AudioDevice.SoundEventReference.None;
			_timeToNextTick = 0f;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool NeedTick()
		{
			return StatModifiers != null;
		}

		public void Tick(float invServerUpdatesPerSecond, Entity entity, float deltaTime)
		{
			_timeToNextTick -= deltaTime;
			if (!(_timeToNextTick > 0f))
			{
				_timeToNextTick = invServerUpdatesPerSecond;
				if (StatModifiers != null)
				{
					AttemptEntityStats(entity);
				}
			}
		}

		private void AttemptEntityStats(Entity entity)
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Invalid comparison between Unknown and I4
			for (int i = 0; i < StatModifiers.Length; i++)
			{
				StatModifier statModifier = StatModifiers[i];
				float num = statModifier.Amount;
				if ((int)ValueType == 0)
				{
					ClientEntityStatValue entityStat = entity.GetEntityStat(statModifier.StatIndex);
					if (entityStat == null)
					{
						continue;
					}
					num = num * (entityStat.Max - entityStat.Min) / 100f;
				}
				if (num != 0f)
				{
					entity.AddStatValue(statModifier.StatIndex, num);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int PackModelVFXData(int direction, int switchTo, int useBloom, int useProgressiveHighlight)
		{
			return direction | (switchTo << 3) | (useBloom << 5) | (useProgressiveHighlight << 6);
		}
	}

	public class EntityItem
	{
		public ClientModelVFX ModelVFX;

		public ModelRenderer ModelRenderer;

		public float Scale;

		public int TargetNodeNameId;

		public int TargetNodeIndex;

		public AudioDevice.SoundEventReference SoundEventReference;

		public readonly List<EntityParticle> ParentParticles = new List<EntityParticle>();

		public readonly List<EntityTrail> ParentTrails = new List<EntityTrail>();

		public readonly List<EntityParticle> Particles = new List<EntityParticle>();

		public readonly List<EntityTrail> Trails = new List<EntityTrail>();

		public ClientItemAppearanceCondition.Data CurrentItemAppearanceCondition;

		private readonly GameInstance _gameInstance;

		private Vector3 _rootPositionOffset;

		private Quaternion _rootOrientationOffset;

		private Matrix _rootOffsetMatrix;

		public Vector3 RootPositionOffset => _rootPositionOffset;

		public Quaternion RootOrientationOffset => _rootOrientationOffset;

		public ref Matrix RootOffsetMatrix => ref _rootOffsetMatrix;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetRootOffsets(Vector3 positionOffset, Quaternion orientationOffset)
		{
			_rootPositionOffset = positionOffset;
			_rootOrientationOffset = orientationOffset;
			Matrix.Compose(orientationOffset, positionOffset, out _rootOffsetMatrix);
		}

		public EntityItem(GameInstance gameInstance)
		{
			_gameInstance = gameInstance;
			_rootOrientationOffset = Quaternion.Identity;
			_rootOffsetMatrix = Matrix.Identity;
			ModelVFX = new ClientModelVFX();
		}

		public bool HasFX()
		{
			return Particles.Count + Trails.Count > 0;
		}

		public void ClearFX()
		{
			ParentParticles.Clear();
			for (int i = 0; i < Particles.Count; i++)
			{
				Particles[i].ParticleSystemProxy?.Expire();
			}
			Particles.Clear();
			ParentTrails.Clear();
			for (int j = 0; j < Trails.Count; j++)
			{
				if (Trails[j].TrailProxy != null)
				{
					Trails[j].TrailProxy.IsExpired = true;
				}
			}
			Trails.Clear();
		}
	}

	public class EntityTrail
	{
		public int TargetFirstPersonNodeIndex = -1;

		public Vector3 PositionOffset = Vector3.Zero;

		public Quaternion RotationOffset = Quaternion.Identity;

		public TrailProxy TrailProxy { get; private set; }

		public EntityPart EntityPart { get; private set; }

		public int TargetNodeNameId { get; private set; }

		public int TargetNodeIndex { get; private set; }

		public bool FixedRotation { get; private set; }

		public EntityTrail(TrailProxy trailProxy, EntityPart entityPart, int targetNodeIndex, int targetNodeNameId, bool fixedRotation)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			TrailProxy = trailProxy;
			EntityPart = entityPart;
			TargetNodeIndex = targetNodeIndex;
			TargetNodeNameId = targetNodeNameId;
			FixedRotation = fixedRotation;
		}
	}

	public class EntityParticle
	{
		public int TargetFirstPersonNodeIndex = -1;

		public Vector3 PositionOffset = Vector3.Zero;

		public Quaternion RotationOffset = Quaternion.Identity;

		public float ItemScale;

		public ParticleSystemProxy ParticleSystemProxy { get; private set; }

		public EntityPart EntityPart { get; private set; }

		public int TargetNodeNameId { get; private set; }

		public int TargetNodeIndex { get; private set; }

		public EntityParticle(ParticleSystemProxy particleSystem, EntityPart entityPart, int targetNodeIndex, int targetNodeNameId, float itemScale)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			ParticleSystemProxy = particleSystem;
			EntityPart = entityPart;
			TargetNodeIndex = targetNodeIndex;
			TargetNodeNameId = targetNodeNameId;
			ItemScale = itemScale;
		}
	}

	public struct DetailBoundingBox
	{
		public Vector3 Offset;

		public BoundingBox Box;
	}

	private class ResumeActionAnimationData
	{
		public EntityAnimation EntityAnimation;

		public float EntityModelRendererResumeTime;
	}

	private struct TimedStatUpdate
	{
		public long CreationTime;

		public int Index;

		public EntityStatUpdate Update;
	}

	private const int CombatTextsDefaultSize = 10;

	private const int CombatTextsGrowth = 10;

	public CombatText[] CombatTexts = new CombatText[10];

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public const byte MapAtlasIndex = 0;

	public const byte EntitiesAtlasIndex = 1;

	public const byte CosmeticsAtlasIndex = 2;

	public static int ServerEntitySeed;

	public const float DrawScaleFactor = 64f;

	public const float DefaultScale = 1f / 64f;

	public const float MapToEntityScaleFactor = 0.5f;

	private const float ParticleRenderingDistance = 20f;

	private const long MaxServerEffectHistoryMs = 1000L;

	private static readonly Cosmetic[] _defaultHeadCosmeticsToHide = (Cosmetic[])(object)new Cosmetic[3]
	{
		default(Cosmetic),
		(Cosmetic)8,
		(Cosmetic)10
	};

	private static readonly Cosmetic[] _defaultChestCosmeticsToHide = (Cosmetic[])(object)new Cosmetic[1] { (Cosmetic)3 };

	private static readonly Cosmetic[] _defaultHandsCosmeticsToHide = (Cosmetic[])(object)new Cosmetic[1] { (Cosmetic)7 };

	private static readonly Cosmetic[] _defaultLegsCosmeticsToHide = (Cosmetic[])(object)new Cosmetic[2]
	{
		(Cosmetic)5,
		(Cosmetic)6
	};

	protected readonly GameInstance _gameInstance;

	public int NetworkId;

	public readonly bool IsLocalEntity;

	public bool IsVisible = true;

	public bool Removed = false;

	private bool _wasShouldRender;

	public Guid? PredictionId;

	public Entity ServerEntity;

	public bool Predictable;

	private bool _beenPredicted = false;

	public bool VisibilityPrediction = true;

	public uint LastAnimationUpdateFrameId;

	public uint LastLogicUpdateFrameId;

	private bool _isTangible = true;

	private bool _isInvulnerable = false;

	public UniqueEntityEffect[] EntityEffects = new UniqueEntityEffect[0];

	private List<EntityEffectUpdate> _predictedEffects = new List<EntityEffectUpdate>();

	private List<ServerEffectEntry> _serverEffects = new List<ServerEffectEntry>();

	protected bool _effectsOnEntityDirty = false;

	protected readonly List<EntityParticle> _entityParticles = new List<EntityParticle>();

	protected readonly List<EntityTrail> _entityTrails = new List<EntityTrail>();

	protected readonly List<EntityParticle> _combatSequenceParticles = new List<EntityParticle>();

	protected readonly List<EntityTrail> _combatSequenceTrails = new List<EntityTrail>();

	private ParticleSystemProxy _walkParticleSystem;

	private ParticleSystemProxy _runParticleSystem;

	private ParticleSystemProxy _sprintParticleSystem;

	private int _previousWalkBlockId = -1;

	private int _previousRunBlockId = -1;

	private int _previousSprintBlockId = -1;

	protected Vector3 _previousPosition;

	protected Vector3 _nextPosition;

	public float PositionProgress;

	private Vector3 _position;

	public Vector3 RenderPosition;

	private Vector3 _previousBodyOrientation;

	protected Vector3 _nextBodyOrientation;

	public float BodyOrientationProgress;

	private Vector3 _bodyOrientation;

	public Quaternion RenderOrientation = Quaternion.Identity;

	public List<EntityItem> EntityItems = new List<EntityItem>();

	protected Vector3 _bottomTint = Vector3.Zero;

	protected Vector3 _topTint = Vector3.Zero;

	public bool UseDithering = false;

	public ClientModelVFX ModelVFX;

	private BlockyModel _characterModel;

	public ClientItemBase ConsumableItem;

	private ClientItemBase _originalPrimaryItem;

	public ColorRgb DynamicLight;

	private ColorRgb _armorLight;

	private ColorRgb _itemLight;

	public Vector3 LookOrientation;

	public float Scale;

	public CameraSettings ActionCameraSettings;

	private CameraSettings _modelCameraSettings;

	private CameraSettings _itemCameraSettings;

	public ClientMovementStates ServerMovementStates = ClientMovementStates.Idle;

	protected float _fallHeight = 0f;

	private int _previousBlockId = 0;

	private int _previousBlockY = 0;

	private bool _wasOnGround = true;

	private bool _wasInFluid = false;

	protected bool _wasFalling = false;

	private bool _wasJumping = false;

	private bool _usable = false;

	public AudioDevice.SoundEventReference SoundEventReference = AudioDevice.SoundEventReference.None;

	public AudioDevice.SoundObjectReference SoundObjectReference = AudioDevice.SoundObjectReference.Empty;

	public List<uint> ActiveSounds = new List<uint>();

	public int PredictedStatusCount;

	private readonly Dictionary<string, ClientAnimationSet> _animationSets = new Dictionary<string, ClientAnimationSet>();

	protected string _currentAnimationId;

	protected float _currentAnimationRunTime;

	private float _nextPassiveAnimationTimer;

	private float _nextPassiveAnimationThreshold;

	private int _countPassiveAnimation;

	private EntityAnimation _passiveAnimation;

	private bool _isCurrentActionAnimationHoldingLastFrame;

	public readonly string[] ServerAnimations = new string[typeof(AnimationSlot).GetEnumValues().Length];

	private ResumeActionAnimationData _previousActionAnimation;

	private readonly AudioDevice.SoundEventReference[] _animationSoundEventReferences = new AudioDevice.SoundEventReference[typeof(AnimationSlot).GetEnumValues().Length];

	public int HitboxCollisionConfigIndex = -1;

	public int RepulsionConfigIndex = -1;

	public Vector2 LastPush = default(Vector2);

	private Item _itemPacket;

	private int _blockId;

	private ParticleSystemProxy _itemParticleSystem;

	private int _currentJumpAnimation = 0;

	private string _lastJumpAnimation;

	private static readonly List<string> JumpAnimations = new List<string>(3) { "JumpWalk", "JumpRun", "JumpSprint" };

	public Dictionary<(int, ForkedChainId), Dictionary<int, InteractionMetaStore>> InteractionMetaStores = new Dictionary<(int, ForkedChainId), Dictionary<int, InteractionMetaStore>>();

	public Dictionary<InteractionType, int> Interactions = new Dictionary<InteractionType, int>();

	public List<int> RunningInteractions = new List<int>();

	private static readonly CameraAxis DefaultCameraAxis = new CameraAxis(new Rangef(-(float)System.Math.PI / 4f, (float)System.Math.PI / 4f), (CameraNode[])(object)new CameraNode[1] { (CameraNode)1 });

	public const int UndefinedAttribute = -1;

	public ClientEntityStatValue[] _entityStats;

	public ClientEntityStatValue[] _serverEntityStats;

	private List<TimedStatUpdate> _predictedStats = new List<TimedStatUpdate>();

	private List<TimedStatUpdate> _serverStats = new List<TimedStatUpdate>();

	public int[] UIComponents;

	public int CombatTextsCount { get; private set; }

	public bool ShouldRender => !Removed && IsVisible && (!BeenPredicted || PredictedProjectile.DebugPrediction);

	public bool BeenPredicted
	{
		get
		{
			return _beenPredicted;
		}
		set
		{
			_beenPredicted = true;
			if (PredictedProjectile.DebugPrediction)
			{
				_topTint = new Vector3(0f, 1f, 0f);
				_bottomTint = new Vector3(0f, 1f, 0f);
			}
		}
	}

	public EntityType Type { get; private set; } = EntityType.None;


	public BoundingBox Hitbox { get; private set; }

	public BoundingBox DefaultHitbox { get; private set; }

	public BoundingBox CrouchHitbox { get; private set; }

	public Dictionary<string, DetailBoundingBox[]> DetailBoundingBoxes { get; private set; } = new Dictionary<string, DetailBoundingBox[]>();


	public Vector3 Position => _position;

	public Vector3 NextPosition => _nextPosition;

	public Vector3 PreviousPosition => _previousPosition;

	public Vector3 BodyOrientation => _bodyOrientation;

	public Vector4 BlockLightColor { get; private set; }

	public ModelRenderer ModelRenderer { get; private set; }

	public Vector3 BottomTint => _bottomTint;

	public Vector3 TopTint => _topTint;

	public Model ModelPacket { get; private set; }

	public string[] ArmorIds { get; private set; } = new string[0];


	public ClientItemBase PrimaryItem { get; private set; }

	public ClientItemBase SecondaryItem { get; private set; }

	public float EyeOffset { get; private set; }

	public float CrouchOffset { get; private set; }

	public CameraSettings CameraSettings => ActionCameraSettings ?? _itemCameraSettings ?? _modelCameraSettings;

	public string Name { get; private set; }

	public EntityAnimation CurrentMovementAnimation { get; private set; }

	public EntityAnimation CurrentActionAnimation { get; private set; } = null;


	public TextRenderer NameplateTextRenderer { get; private set; }

	public ClientItemBase ItemBase { get; private set; }

	public float ItemAnimationTime { get; private set; }

	public PlayerSkin PlayerSkin { get; set; }

	public float SmoothHealth { get; set; } = -1f;


	public void AddCombatText(CombatTextUpdate textUpdate)
	{
		ArrayUtils.GrowArrayIfNecessary(ref CombatTexts, CombatTextsCount, 10);
		int num = CombatTextsCount++;
		CombatTexts[num] = new CombatText
		{
			HitAngleDeg = textUpdate.HitAngleDeg,
			Text = textUpdate.Text
		};
	}

	public void ClearCombatTexts()
	{
		CombatTextsCount = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPosition(Vector3 nextPosition)
	{
		_previousPosition = Position;
		_nextPosition = nextPosition;
		bool flag = NetworkId == _gameInstance.LocalPlayerNetworkId || NetworkId == _gameInstance.EntityStoreModule.MountEntityLocalId;
		PositionProgress = (flag ? 1f : 0f);
		_position = (flag ? _nextPosition : _previousPosition);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPositionTeleport(Vector3 nextPosition)
	{
		_previousPosition = nextPosition;
		_nextPosition = nextPosition;
		PositionProgress = 1f;
		_position = nextPosition;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBodyOrientation(Vector3 nextOrientation)
	{
		_previousBodyOrientation = BodyOrientation;
		_nextBodyOrientation = nextOrientation;
		bool flag = NetworkId == _gameInstance.LocalPlayerNetworkId || NetworkId == _gameInstance.EntityStoreModule.MountEntityLocalId;
		BodyOrientationProgress = (flag ? 1f : 0f);
		_bodyOrientation = (flag ? _nextBodyOrientation : _previousBodyOrientation);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBodyOrientationTeleport(Vector3 nextOrientation)
	{
		_previousBodyOrientation = nextOrientation;
		_nextBodyOrientation = nextOrientation;
		BodyOrientationProgress = 1f;
		_bodyOrientation = nextOrientation;
	}

	public Entity(GameInstance gameInstance, int networkId)
	{
		_gameInstance = gameInstance;
		NetworkId = networkId;
		IsLocalEntity = NetworkId < 0;
		ServerSettings serverSettings = _gameInstance.ServerSettings;
		int num = ((serverSettings != null) ? serverSettings.EntityStatTypes.Length : 0);
		_entityStats = new ClientEntityStatValue[num];
		_serverEntityStats = new ClientEntityStatValue[num];
		for (int i = 0; i < _animationSoundEventReferences.Length; i++)
		{
			_animationSoundEventReferences[i] = AudioDevice.SoundEventReference.None;
		}
		ModelVFX = new ClientModelVFX();
		ModelVFX.NoiseScale = new Vector2(50f);
	}

	protected override void DoDispose()
	{
		ClearCombatSequenceEffects();
		ClearFX();
		for (int i = 0; i < EntityItems.Count; i++)
		{
			EntityItem entityItem = EntityItems[i];
			if (entityItem.SoundEventReference.PlaybackId != -1)
			{
				_gameInstance.AudioModule.ActionOnEvent(ref entityItem.SoundEventReference, (AkActionOnEventType)0);
			}
			entityItem.ClearFX();
			entityItem.ModelRenderer.Dispose();
			entityItem.ModelRenderer = null;
		}
		_runParticleSystem?.Expire();
		_sprintParticleSystem?.Expire();
		EntityItems.Clear();
		ModelRenderer?.Dispose();
		_itemParticleSystem?.Expire(instant: true);
		NameplateTextRenderer?.Dispose();
		for (int j = 0; j < _animationSoundEventReferences.Length; j++)
		{
			ref AudioDevice.SoundEventReference reference = ref _animationSoundEventReferences[j];
			if (reference.PlaybackId != -1)
			{
				_gameInstance.AudioModule.ActionOnEvent(ref reference, (AkActionOnEventType)0);
			}
		}
		for (int k = 0; k < EntityEffects.Length; k++)
		{
			ref UniqueEntityEffect reference2 = ref EntityEffects[k];
			if (reference2.SoundEventReference.PlaybackId != -1)
			{
				_gameInstance.AudioModule.ActionOnEvent(ref reference2.SoundEventReference, (AkActionOnEventType)0);
			}
			if (reference2.ParticleSystems != null)
			{
				for (int l = 0; l < reference2.ParticleSystems.Count; l++)
				{
					reference2.ParticleSystems[l].ParticleSystemProxy.Expire();
				}
			}
		}
		foreach (Dictionary<int, InteractionMetaStore> value in InteractionMetaStores.Values)
		{
			foreach (InteractionMetaStore value2 in value.Values)
			{
				if (value2.SoundEventReference.PlaybackId != -1)
				{
					_gameInstance.AudioModule.ActionOnEvent(ref value2.SoundEventReference, (AkActionOnEventType)0);
				}
			}
		}
		if (SoundObjectReference.SoundObjectId != 1)
		{
			_gameInstance.AudioModule.UnregisterSoundObject(ref SoundObjectReference);
		}
	}

	public virtual void SetTransform(Vector3 position, Vector3 bodyOrientation, Vector3 lookOrientation)
	{
		SetPosition(position);
		SetBodyOrientation(bodyOrientation);
		LookOrientation = lookOrientation;
	}

	public void SkipTransformLerp()
	{
		_previousPosition = _nextPosition;
		_previousBodyOrientation = _nextBodyOrientation;
	}

	public void SetSpawnTransform(Vector3 position, Vector3 bodyOrientation, Vector3 lookOrientation)
	{
		SetTransform(position, bodyOrientation, lookOrientation);
		_position = (_previousPosition = _nextPosition);
		_bodyOrientation = (_previousBodyOrientation = _nextBodyOrientation);
	}

	public void SetName(string name, bool nameTagVisible)
	{
		Name = name;
		if (string.IsNullOrWhiteSpace(name) || !nameTagVisible)
		{
			NameplateTextRenderer?.Dispose();
			NameplateTextRenderer = null;
		}
		else if (NameplateTextRenderer == null)
		{
			NameplateTextRenderer = new TextRenderer(_gameInstance.Engine.Graphics, _gameInstance.App.Fonts.DefaultFontFamily.RegularFont, name);
		}
		else
		{
			NameplateTextRenderer.Text = name;
		}
	}

	private void CalculateEffectTint()
	{
		_bottomTint = Vector3.Zero;
		_topTint = Vector3.Zero;
		for (int i = 0; i < EntityEffects.Length; i++)
		{
			ref UniqueEntityEffect reference = ref EntityEffects[i];
			if (!reference.IsExpiring)
			{
				_bottomTint.X = 1f - (1f - _bottomTint.X) * (1f - reference.BottomTint.X);
				_bottomTint.Y = 1f - (1f - _bottomTint.Y) * (1f - reference.BottomTint.Y);
				_bottomTint.Z = 1f - (1f - _bottomTint.Z) * (1f - reference.BottomTint.Z);
				_topTint.X = 1f - (1f - _topTint.X) * (1f - reference.TopTint.X);
				_topTint.Y = 1f - (1f - _topTint.Y) * (1f - reference.TopTint.Y);
				_topTint.Z = 1f - (1f - _topTint.Z) * (1f - reference.TopTint.Z);
			}
		}
	}

	private void ServerAddEffect(EntityEffectUpdate update)
	{
		if (!FindAndRemoveMatchEffectPrediction(_predictedEffects, update.Id, (EffectOp)0, update.Infinite))
		{
			_serverEffects.Add(new ServerEffectEntry
			{
				CreationTime = DateTime.Now.Ticks / 10000,
				Update = update
			});
			EntityEffect protocolEntityEffect = _gameInstance.EntityStoreModule.EntityEffects[update.Id];
			AddEffect(update.Id, protocolEntityEffect, update.RemainingTime, update.Infinite, update.Debuff);
		}
	}

	private void ServerRemoveEffect(EntityEffectUpdate update)
	{
		if (!FindAndRemoveMatchEffectPrediction(_predictedEffects, update.Id, (EffectOp)1, null))
		{
			_serverEffects.Add(new ServerEffectEntry
			{
				CreationTime = DateTime.Now.Ticks / 10000,
				Update = update
			});
			RemoveEffect(update.Id);
		}
	}

	public EntityEffectUpdate PredictedAddEffect(int networkEffectIndex, bool? infinite = null)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected O, but got Unknown
		if (FindAndRemoveMatchEffectPrediction(_serverEffects, networkEffectIndex, (EffectOp)0, infinite))
		{
			return null;
		}
		EntityEffect val = _gameInstance.EntityStoreModule.EntityEffects[networkEffectIndex];
		EntityEffectUpdate val2 = new EntityEffectUpdate((EffectOp)0, networkEffectIndex, val.Duration, infinite ?? val.Infinite, val.Debuff, val.StatusEffectIcon);
		_predictedEffects.Add(val2);
		bool? infinite2 = infinite;
		AddEffect(networkEffectIndex, val, null, infinite2);
		return val2;
	}

	public EntityEffectUpdate PredictedRemoveEffect(int networkEffectIndex)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		if (FindAndRemoveMatchEffectPrediction(_serverEffects, networkEffectIndex, (EffectOp)1, null))
		{
			return null;
		}
		EntityEffectUpdate val = new EntityEffectUpdate((EffectOp)1, networkEffectIndex, 0f, false, false, "");
		_predictedEffects.Add(val);
		RemoveEffect(networkEffectIndex);
		return val;
	}

	public void CancelPrediction(EntityEffectUpdate prediction)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Invalid comparison between Unknown and I4
		if (prediction != null && _predictedEffects.Remove(prediction))
		{
			if ((int)prediction.Type == 0)
			{
				RemoveEffect(prediction.Id);
			}
			else
			{
				AddEffect(prediction.Id);
			}
		}
	}

	private static bool FindAndRemoveMatchEffectPrediction(IList<EntityEffectUpdate> data, int networkEffectIndex, EffectOp op, bool? infinite)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < data.Count; i++)
		{
			EntityEffectUpdate val = data[i];
			if (val.Type == op && val.Id == networkEffectIndex && (!infinite.HasValue || val.Infinite == infinite.Value))
			{
				data.RemoveAt(i);
				return true;
			}
		}
		return false;
	}

	private static bool FindAndRemoveMatchEffectPrediction(IList<ServerEffectEntry> data, int networkEffectIndex, EffectOp op, bool? infinite)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < data.Count; i++)
		{
			EntityEffectUpdate update = data[i].Update;
			if (update.Type == op && update.Id == networkEffectIndex && (!infinite.HasValue || update.Infinite == infinite.Value))
			{
				data.RemoveAt(i);
				return true;
			}
		}
		return false;
	}

	public void AddEffect(int networkEffectIndex, float? remainingDuration = null, bool? infinite = null, bool? debuff = null)
	{
		if (networkEffectIndex >= _gameInstance.EntityStoreModule.EntityEffects.Length)
		{
			_gameInstance.Chat.Error($"Entity Effect not found for index: {networkEffectIndex}");
			return;
		}
		EntityEffect val = _gameInstance.EntityStoreModule.EntityEffects[networkEffectIndex];
		if (val == null)
		{
			_gameInstance.Chat.Error($"Entity Effect null for index: {networkEffectIndex}");
		}
		else
		{
			AddEffect(networkEffectIndex, val, remainingDuration, infinite, debuff);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int GetEffectIndexFromNetworkEffectIndex(int networkEffectIndex)
	{
		for (int i = 0; i < EntityEffects.Length; i++)
		{
			if (EntityEffects[i].NetworkEffectIndex == networkEffectIndex)
			{
				return i;
			}
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool HasEffect(int networkEffectIndex)
	{
		int effectIndexFromNetworkEffectIndex = GetEffectIndexFromNetworkEffectIndex(networkEffectIndex);
		return effectIndexFromNetworkEffectIndex != -1 && !EntityEffects[effectIndexFromNetworkEffectIndex].IsExpiring;
	}

	[Obsolete("Deprecated method. Use Entity#AddEffect(int entityEffectIndex) instead. This method is a temporary workaround to have BlockPlacementPreview#UpdatePreview() working.")]
	public void AddEffect(int networkEffectIndex, EntityEffect protocolEntityEffect, float? remainingDuration = null, bool? infinite = null, bool? debuff = null)
	{
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Expected I4, but got Unknown
		//IL_04b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c3: Expected I4, but got Unknown
		//IL_04c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e7: Expected I4, but got Unknown
		//IL_0613: Unknown result type (might be due to invalid IL or missing references)
		//IL_061a: Expected I4, but got Unknown
		//IL_061c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0621: Unknown result type (might be due to invalid IL or missing references)
		//IL_0649: Unknown result type (might be due to invalid IL or missing references)
		//IL_0654: Expected I4, but got Unknown
		int num = GetEffectIndexFromNetworkEffectIndex(networkEffectIndex);
		bool flag = num != -1;
		if (num == -1)
		{
			num = EntityEffects.Length;
			ArrayUtils.GrowArrayIfNecessary(ref EntityEffects, EntityEffects.Length, 1);
			EntityEffects[num] = new UniqueEntityEffect(protocolEntityEffect, networkEffectIndex);
			_effectsOnEntityDirty = true;
		}
		if (NetworkId < 0)
		{
		}
		ref UniqueEntityEffect reference = ref EntityEffects[num];
		reference.IsInfinite = infinite ?? protocolEntityEffect.Infinite;
		reference.IsDebuff = debuff ?? protocolEntityEffect.Debuff;
		if (flag)
		{
			if (!reference.IsExpiring)
			{
				OverlapBehavior overlapBehavior_ = protocolEntityEffect.OverlapBehavior_;
				OverlapBehavior val = overlapBehavior_;
				switch ((int)val)
				{
				case 0:
					if (!reference.IsInfinite && !remainingDuration.HasValue)
					{
						remainingDuration = reference.RemainingDuration + protocolEntityEffect.Duration;
					}
					break;
				case 2:
					return;
				default:
					throw new ArgumentOutOfRangeException();
				case 1:
					break;
				}
			}
			else if (NetworkId == _gameInstance.LocalPlayerNetworkId && protocolEntityEffect.ApplicationEffects_?.ScreenEffect != null)
			{
				_gameInstance.ScreenEffectStoreModule.AddEntityScreenEffect(protocolEntityEffect.ApplicationEffects_.ScreenEffect);
			}
			if (reference.ModelRenderer != null && reference.SpawnAnimation != null)
			{
				reference.ModelRenderer.SetSlotAnimation(0, reference.SpawnAnimation.Data, reference.SpawnAnimation.Looping, 1f, 0f, reference.SpawnAnimation.BlendingDuration);
			}
			reference.IsExpiring = false;
		}
		else if (NetworkId == _gameInstance.LocalPlayerNetworkId && protocolEntityEffect.ApplicationEffects_?.ScreenEffect != null)
		{
			_gameInstance.ScreenEffectStoreModule.AddEntityScreenEffect(protocolEntityEffect.ApplicationEffects_.ScreenEffect);
		}
		reference.RemainingDuration = remainingDuration ?? protocolEntityEffect.Duration;
		if (protocolEntityEffect.ApplicationEffects_?.EntityAnimationId != null)
		{
			EntityAnimation animation = GetAnimation(protocolEntityEffect.ApplicationEffects_.EntityAnimationId);
			if (animation != null)
			{
				ModelRenderer.SetSlotAnimation(6, animation.Data, animation.Looping, animation.Speed, 0f, animation.BlendingDuration);
				ServerAnimations[1] = protocolEntityEffect.ApplicationEffects_.EntityAnimationId;
			}
		}
		if (protocolEntityEffect.ApplicationEffects_ != null)
		{
			if (this != _gameInstance.LocalPlayer)
			{
				uint networkWwiseId = ResourceManager.GetNetworkWwiseId(protocolEntityEffect.ApplicationEffects_.SoundEventIndexWorld);
				_gameInstance.AudioModule.PlaySoundEvent(networkWwiseId, SoundObjectReference, ref reference.SoundEventReference);
			}
			else
			{
				uint networkWwiseId2 = ResourceManager.GetNetworkWwiseId(protocolEntityEffect.ApplicationEffects_.SoundEventIndexLocal);
				_gameInstance.AudioModule.PlayLocalSoundEvent(networkWwiseId2);
			}
			string modelVFXId = protocolEntityEffect.ApplicationEffects_.ModelVFXId;
			if (modelVFXId != null)
			{
				if (!_gameInstance.EntityStoreModule.ModelVFXByIds.TryGetValue(modelVFXId, out var value))
				{
					_gameInstance.App.DevTools.Error("Could not find model vfx: " + modelVFXId);
					return;
				}
				ModelVFX val2 = _gameInstance.EntityStoreModule.ModelVFXs[value];
				if (val2 != null)
				{
					ModelVFX val3 = val2;
					bool flag2 = val3.HighlightColor != null;
					bool flag3 = val3.AnimationDuration > 0f;
					if (flag2)
					{
						ModelVFX.HighlightColor = new Vector3((float)(int)(byte)val3.HighlightColor.Red / 255f, (float)(int)(byte)val3.HighlightColor.Green / 255f, (float)(int)(byte)val3.HighlightColor.Blue / 255f);
					}
					if (flag3)
					{
						ModelVFX.AnimationDuration = val3.AnimationDuration;
					}
					if (val3.AnimationRange != null)
					{
						ModelVFX.AnimationRange = new Vector2(val3.AnimationRange.X, val3.AnimationRange.Y);
					}
					ModelVFX.LoopOption = (ClientModelVFX.LoopOptions)val3.LoopOption_;
					CurveType curveType_ = val3.CurveType_;
					CurveType val4 = curveType_;
					switch ((int)val4)
					{
					case 0:
						ModelVFX.CurveType = Easing.EasingType.Linear;
						break;
					case 1:
						ModelVFX.CurveType = Easing.EasingType.QuartIn;
						break;
					case 3:
						ModelVFX.CurveType = Easing.EasingType.QuartInOut;
						break;
					case 2:
						ModelVFX.CurveType = Easing.EasingType.QuartOut;
						break;
					}
					ModelVFX.HighlightThickness = val3.HighlightThickness;
					if (val3.NoiseScale != null)
					{
						ModelVFX.NoiseScale = new Vector2(val3.NoiseScale.X, val3.NoiseScale.Y);
					}
					if (val3.NoiseScrollSpeed != null)
					{
						ModelVFX.NoiseScrollSpeed = new Vector2(val3.NoiseScrollSpeed.X, val3.NoiseScrollSpeed.Y);
					}
					if (val3.PostColor != null)
					{
						ModelVFX.PostColor = new Vector4((float)(int)(byte)val3.PostColor.Red / 255f, (float)(int)(byte)val3.PostColor.Green / 255f, (float)(int)(byte)val3.PostColor.Blue / 255f, val3.PostColorOpacity);
					}
					ClientModelVFX.EffectDirections direction = (ClientModelVFX.EffectDirections)val3.EffectDirection_;
					SwitchTo switchTo_ = val3.SwitchTo_;
					int useBloom = (val3.UseBloomOnHighlight ? 1 : 0);
					int useProgressiveHighlight = (val3.UseProgessiveHighlight ? 1 : 0);
					ModelVFX.PackedModelVFXParams = UniqueEntityEffect.PackModelVFXData((int)direction, (int)switchTo_, useBloom, useProgressiveHighlight);
					if (NetworkId < 0)
					{
					}
					if (flag2 && flag3)
					{
						ModelVFX.TriggerAnimation = true;
					}
				}
			}
		}
		if (protocolEntityEffect.ModelOverride_ != null)
		{
			ModelOverride modelOverride_ = protocolEntityEffect.ModelOverride_;
			if (!_gameInstance.HashesByServerAssetPath.TryGetValue(modelOverride_.Model, out var value2) || !_gameInstance.EntityStoreModule.GetModel(value2, out var model))
			{
				_gameInstance.App.DevTools.Error("Failed to load entity effect model: " + modelOverride_.Model);
				return;
			}
			if (!_gameInstance.HashesByServerAssetPath.TryGetValue(modelOverride_.Texture, out var value3))
			{
				_gameInstance.App.DevTools.Error("Failed to load entity effect texture: " + modelOverride_.Texture);
				return;
			}
			if (!_gameInstance.EntityStoreModule.ImageLocations.TryGetValue(value3, out var value4))
			{
				_gameInstance.App.DevTools.Error("Cannot use " + modelOverride_.Texture + " as an entity effect texture");
				return;
			}
			BlockyModel blockyModel = model.Clone();
			blockyModel.SetAtlasIndex(1);
			blockyModel.OffsetUVs(value4);
			reference.ModelRenderer = new ModelRenderer(blockyModel, _gameInstance.AtlasSizes, _gameInstance.Engine.Graphics, _gameInstance.FrameCounter);
			if (modelOverride_.AnimationSets != null)
			{
				bool keepPreviousFirstPersonAnimation = false;
				if (modelOverride_.AnimationSets.TryGetValue("Spawn", out var value5))
				{
					Animation[] animations = value5.Animations;
					if (animations != null && animations.Length != 0)
					{
						Animation weightedAnimation = GetWeightedAnimation(value5.Animations);
						if (_gameInstance.HashesByServerAssetPath.TryGetValue(weightedAnimation.Animation_, out var value6) && _gameInstance.EntityStoreModule.GetAnimation(value6, out var animation2))
						{
							float speed = ((weightedAnimation.Speed == 0f) ? 1f : weightedAnimation.Speed);
							reference.SpawnAnimation = new EntityAnimation(animation2, speed, weightedAnimation.BlendingDuration * 60f, weightedAnimation.Looping, keepPreviousFirstPersonAnimation, ResourceManager.GetNetworkWwiseId(weightedAnimation.SoundEventIndex), weightedAnimation.Weight, weightedAnimation.FootstepIntervals, weightedAnimation.PassiveLoopCount);
							reference.ModelRenderer.SetSlotAnimationNoBlending(0, reference.SpawnAnimation.Data, reference.SpawnAnimation.Looping);
						}
					}
				}
				if (modelOverride_.AnimationSets.TryGetValue("Despawn", out value5))
				{
					Animation[] animations2 = value5.Animations;
					if (animations2 != null && animations2.Length != 0)
					{
						Animation weightedAnimation2 = GetWeightedAnimation(value5.Animations);
						if (_gameInstance.HashesByServerAssetPath.TryGetValue(weightedAnimation2.Animation_, out var value7) && _gameInstance.EntityStoreModule.GetAnimation(value7, out var animation3))
						{
							float speed2 = ((weightedAnimation2.Speed == 0f) ? 1f : weightedAnimation2.Speed);
							reference.DespawnAnimation = new EntityAnimation(animation3, speed2, weightedAnimation2.BlendingDuration * 60f, weightedAnimation2.Looping, keepPreviousFirstPersonAnimation, ResourceManager.GetNetworkWwiseId(weightedAnimation2.SoundEventIndex), weightedAnimation2.Weight, weightedAnimation2.FootstepIntervals, weightedAnimation2.PassiveLoopCount);
						}
					}
				}
				if (modelOverride_.AnimationSets.TryGetValue("Idle", out value5))
				{
					Animation[] animations3 = value5.Animations;
					if (animations3 != null && animations3.Length != 0)
					{
						Animation weightedAnimation3 = GetWeightedAnimation(value5.Animations);
						if (_gameInstance.HashesByServerAssetPath.TryGetValue(weightedAnimation3.Animation_, out var value8) && _gameInstance.EntityStoreModule.GetAnimation(value8, out var animation4))
						{
							float speed3 = ((weightedAnimation3.Speed == 0f) ? 1f : weightedAnimation3.Speed);
							reference.IdleAnimation = new EntityAnimation(animation4, speed3, weightedAnimation3.BlendingDuration * 60f, weightedAnimation3.Looping, keepPreviousFirstPersonAnimation, ResourceManager.GetNetworkWwiseId(weightedAnimation3.SoundEventIndex), weightedAnimation3.Weight, weightedAnimation3.FootstepIntervals, weightedAnimation3.PassiveLoopCount);
						}
					}
				}
			}
		}
		bool flag4 = DoFirstPersonParticles() && protocolEntityEffect.ApplicationEffects_?.FirstPersonParticles != null;
		ModelParticle[] array = ((!flag4) ? protocolEntityEffect.ApplicationEffects_?.Particles : protocolEntityEffect.ApplicationEffects_?.FirstPersonParticles);
		if (array != null)
		{
			reference.ParticleSystems = new List<EntityParticle>();
			for (int i = 0; i < array.Length; i++)
			{
				ModelParticleSettings clientModelParticle = new ModelParticleSettings();
				ParticleProtocolInitializer.Initialize(array[i], ref clientModelParticle, _gameInstance.EntityStoreModule.NodeNameManager);
				EntityParticle entityParticle = AttachParticles(_characterModel, _entityParticles, clientModelParticle, Scale);
				if (entityParticle != null)
				{
					entityParticle.ParticleSystemProxy.SetFirstPerson(flag4);
					reference.ParticleSystems.Add(entityParticle);
				}
			}
		}
		CalculateEffectTint();
		if (this == _gameInstance.LocalPlayer)
		{
			_gameInstance.App.Interface.InGameView.OnEffectAdded(networkEffectIndex);
		}
	}

	public bool RemoveEffect(int networkEffectIndex)
	{
		int effectIndexFromNetworkEffectIndex = GetEffectIndexFromNetworkEffectIndex(networkEffectIndex);
		if (effectIndexFromNetworkEffectIndex == -1 || EntityEffects[effectIndexFromNetworkEffectIndex].IsExpiring)
		{
			return false;
		}
		if (_gameInstance.EntityStoreModule.CurrentSetup.DisplayDebugCommandsOnEntityEffect)
		{
			_gameInstance.Chat.Log("Removed " + _gameInstance.EntityStoreModule.EntityEffects[networkEffectIndex].Id);
		}
		_effectsOnEntityDirty = true;
		InternalRemoveEffect(effectIndexFromNetworkEffectIndex);
		return true;
	}

	private void InternalRemoveEffect(int localEntityEffectIndex)
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Invalid comparison between Unknown and I4
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Invalid comparison between Unknown and I4
		ref UniqueEntityEffect reference = ref EntityEffects[localEntityEffectIndex];
		if (NetworkId == _gameInstance.LocalPlayerNetworkId && reference.ScreenEffect != null)
		{
			_gameInstance.ScreenEffectStoreModule.RemoveEntityScreenEffect(reference.ScreenEffect);
		}
		if (reference.ParticleSystems != null)
		{
			for (int i = 0; i < reference.ParticleSystems.Count; i++)
			{
				EntityParticle entityParticle = reference.ParticleSystems[i];
				entityParticle.ParticleSystemProxy.Expire();
				if ((int)entityParticle.EntityPart == 2)
				{
					if (PrimaryItem != null)
					{
						EntityItems[0].Particles.Remove(entityParticle);
					}
				}
				else if ((int)entityParticle.EntityPart == 3)
				{
					if (SecondaryItem != null)
					{
						EntityItems[(EntityItems.Count > 1) ? 1 : 0].Particles.Remove(entityParticle);
					}
				}
				else
				{
					_entityParticles.Remove(entityParticle);
				}
			}
		}
		ModelVFX.AnimationProgress = 0f;
		ModelVFX.AnimationDuration = 0f;
		if (reference.ModelRenderer != null)
		{
			if (reference.DespawnAnimation != null)
			{
				reference.ModelRenderer.SetSlotAnimation(0, reference.DespawnAnimation.Data, reference.DespawnAnimation.Looping, 1f, 0f, reference.DespawnAnimation.BlendingDuration);
			}
			else
			{
				reference.ModelRenderer.SetSlotAnimation(0, null);
			}
		}
		if (reference.SoundEventReference.PlaybackId != -1)
		{
			_gameInstance.AudioModule.ActionOnEvent(ref reference.SoundEventReference, (AkActionOnEventType)0);
		}
		reference.IsExpiring = true;
		CalculateEffectTint();
		if (this == _gameInstance.LocalPlayer)
		{
			_gameInstance.App.Interface.InGameView.OnEffectRemoved(reference.NetworkEffectIndex);
		}
	}

	public void ClearEffects()
	{
		for (int i = 0; i < EntityEffects.Length; i++)
		{
			RemoveEffect(EntityEffects[i].NetworkEffectIndex);
		}
		EntityEffects = new UniqueEntityEffect[0];
	}

	public void SetCharacterModel(Model newModelPacket, string[] newArmorIds)
	{
		//IL_0409: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0413: Expected O, but got Unknown
		//IL_043c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0446: Expected O, but got Unknown
		//IL_0465: Unknown result type (might be due to invalid IL or missing references)
		//IL_046f: Expected O, but got Unknown
		//IL_04a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ac: Expected O, but got Unknown
		//IL_0537: Unknown result type (might be due to invalid IL or missing references)
		//IL_0541: Expected O, but got Unknown
		//IL_0574: Unknown result type (might be due to invalid IL or missing references)
		//IL_057e: Expected O, but got Unknown
		if (newModelPacket != null)
		{
			ModelPacket = newModelPacket;
			LoadCharacterAnimations();
		}
		if (newArmorIds != null)
		{
			if (newModelPacket == null)
			{
				bool flag = false;
				if (ArmorIds.Length == newArmorIds.Length)
				{
					for (int i = 0; i < newArmorIds.Length; i++)
					{
						if (newArmorIds[i] != ArmorIds[i])
						{
							flag = true;
							break;
						}
					}
				}
				else
				{
					flag = true;
				}
				if (!flag)
				{
					return;
				}
			}
			ArmorIds = newArmorIds;
		}
		_armorLight.R = 0;
		_armorLight.G = 0;
		_armorLight.B = 0;
		_itemLight.R = 0;
		_itemLight.G = 0;
		_itemLight.B = 0;
		_itemCameraSettings = null;
		Type = EntityType.Character;
		if (ModelPacket == null)
		{
			throw new Exception("Attempted to set character model without sending a model first: NetworkId: " + NetworkId + ", Name: " + Name + ", PlayerSkin: " + (object)PlayerSkin);
		}
		Scale = ModelPacket.Scale;
		EyeOffset = ModelPacket.EyeHeight;
		CrouchOffset = ModelPacket.CrouchOffset;
		DefaultHitbox = new BoundingBox(new Vector3(ModelPacket.Hitbox_.MinX, ModelPacket.Hitbox_.MinY, ModelPacket.Hitbox_.MinZ), new Vector3(ModelPacket.Hitbox_.MaxX, ModelPacket.Hitbox_.MaxY, ModelPacket.Hitbox_.MaxZ));
		CrouchHitbox = new BoundingBox(new Vector3(ModelPacket.Hitbox_.MinX, ModelPacket.Hitbox_.MinY, ModelPacket.Hitbox_.MinZ), new Vector3(ModelPacket.Hitbox_.MaxX, ModelPacket.Hitbox_.MaxY + CrouchOffset, ModelPacket.Hitbox_.MaxZ));
		Hitbox = DefaultHitbox;
		DetailBoundingBoxes.Clear();
		if (ModelPacket.DetailBoxes != null)
		{
			foreach (KeyValuePair<string, DetailBox[]> detailBox in ModelPacket.DetailBoxes)
			{
				DetailBoundingBox[] array = new DetailBoundingBox[detailBox.Value.Length];
				for (int j = 0; j < array.Length; j++)
				{
					DetailBox val = detailBox.Value[j];
					array[j] = new DetailBoundingBox
					{
						Offset = new Vector3(val.Offset.X, val.Offset.Y, val.Offset.Z),
						Box = new BoundingBox(new Vector3(val.Box.MinX, val.Box.MinY, val.Box.MinZ), new Vector3(val.Box.MaxX, val.Box.MaxY, val.Box.MaxZ))
					};
				}
				DetailBoundingBoxes.Add(detailBox.Key, array);
			}
		}
		_modelCameraSettings = ((ModelPacket.Camera != null) ? new CameraSettings(ModelPacket.Camera) : new CameraSettings());
		if (_modelCameraSettings.PositionOffset == null)
		{
			_modelCameraSettings.PositionOffset = new Vector3f(0.55f, 0.2f, 2.5f);
		}
		if (_modelCameraSettings.Yaw == null)
		{
			_modelCameraSettings.Yaw = new CameraAxis(DefaultCameraAxis);
		}
		else if (_modelCameraSettings.Yaw.AngleRange == null)
		{
			_modelCameraSettings.Yaw.AngleRange = new Rangef(DefaultCameraAxis.AngleRange);
		}
		else
		{
			_modelCameraSettings.Yaw.AngleRange.Min = MathHelper.ToRadians(ModelPacket.Camera.Yaw.AngleRange.Min);
			_modelCameraSettings.Yaw.AngleRange.Max = MathHelper.ToRadians(ModelPacket.Camera.Yaw.AngleRange.Max);
		}
		if (_modelCameraSettings.Pitch == null)
		{
			_modelCameraSettings.Pitch = new CameraAxis(DefaultCameraAxis);
		}
		else if (_modelCameraSettings.Pitch.AngleRange == null)
		{
			_modelCameraSettings.Pitch.AngleRange = new Rangef(DefaultCameraAxis.AngleRange);
		}
		else
		{
			_modelCameraSettings.Pitch.AngleRange.Min = MathHelper.ToRadians(ModelPacket.Camera.Pitch.AngleRange.Min);
			_modelCameraSettings.Pitch.AngleRange.Max = MathHelper.ToRadians(ModelPacket.Camera.Pitch.AngleRange.Max);
		}
		ModelRenderer modelRenderer = ModelRenderer;
		float animationTime = 0f;
		if (ModelRenderer != null)
		{
			animationTime = ModelRenderer.GetSlotAnimationTime(0);
			ModelRenderer.Dispose();
			ClearCombatSequenceEffects();
			ClearFX();
			for (int k = 0; k < EntityItems.Count; k++)
			{
				EntityItem entityItem = EntityItems[k];
				if (entityItem.SoundEventReference.PlaybackId != -1)
				{
					_gameInstance.AudioModule.ActionOnEvent(ref entityItem.SoundEventReference, (AkActionOnEventType)0);
				}
				entityItem.ClearFX();
				entityItem.ModelRenderer.Dispose();
				entityItem.ModelRenderer = null;
			}
			EntityItems.Clear();
			ModelRenderer = null;
			if (NetworkId == _gameInstance.LocalPlayerNetworkId)
			{
				_gameInstance.LocalPlayer.ClearFirstPersonView();
			}
		}
		for (int l = 0; l < _animationSoundEventReferences.Length; l++)
		{
			ref AudioDevice.SoundEventReference reference = ref _animationSoundEventReferences[l];
			if (reference.PlaybackId != -1)
			{
				_gameInstance.AudioModule.ActionOnEvent(ref reference, (AkActionOnEventType)0);
				reference = AudioDevice.SoundEventReference.None;
			}
		}
		if (ModelPacket.Light != null)
		{
			ClientItemBaseProtocolInitializer.ParseLightColor(ModelPacket.Light, ref _armorLight);
		}
		if (PlayerSkin != null)
		{
			LoadPlayerModel();
		}
		else
		{
			LoadCharacterModel();
		}
		if (_characterModel == null)
		{
			return;
		}
		if (ModelPacket.Particles != null)
		{
			for (int m = 0; m < ModelPacket.Particles.Length; m++)
			{
				ModelParticleSettings clientModelParticle = new ModelParticleSettings();
				ParticleProtocolInitializer.Initialize(ModelPacket.Particles[m], ref clientModelParticle, _gameInstance.EntityStoreModule.NodeNameManager);
				AttachParticles(_characterModel, _entityParticles, clientModelParticle, Scale);
			}
		}
		if (ModelPacket.Trails != null)
		{
			for (int n = 0; n < ModelPacket.Trails.Length; n++)
			{
				AttachTrails(_characterModel, _entityTrails, ModelPacket.Trails[n], Scale);
			}
		}
		if (PrimaryItem != null)
		{
			AttachItem(_characterModel, PrimaryItem, CharacterPartStore.RightAttachmentNodeNameId);
			HandleItemConditionAppearanceForAllEntityStats(PrimaryItem, 0);
		}
		if (SecondaryItem != null)
		{
			AttachItem(_characterModel, SecondaryItem, CharacterPartStore.LeftAttachmentNodeNameId);
			HandleItemConditionAppearanceForAllEntityStats(SecondaryItem, (PrimaryItem != null) ? 1 : 0);
		}
		SetupItemCamera();
		ModelRenderer = new ModelRenderer(_characterModel, _gameInstance.AtlasSizes, _gameInstance.Engine.Graphics, _gameInstance.FrameCounter);
		if (modelRenderer != null)
		{
			ModelRenderer.CopyAllSlotAnimations(modelRenderer);
		}
		if (ServerAnimations[0] != null)
		{
			SetServerAnimation(ServerAnimations[0], (AnimationSlot)0);
		}
		else
		{
			SetMovementAnimation(_currentAnimationId ?? "Idle", animationTime, force: true);
		}
		if (NetworkId == _gameInstance.LocalPlayerNetworkId)
		{
			_gameInstance.LocalPlayer.SetFirstPersonView(_characterModel);
		}
		ModelRenderer.SetCameraNodes(CameraSettings);
		ModelRenderer.UpdatePose();
		ICameraController controller = _gameInstance.CameraModule.Controller;
		if (controller.AttachedTo == this)
		{
			controller.Reset(_gameInstance, controller);
		}
	}

	private void LoadCharacterModel()
	{
		//IL_03b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b6: Invalid comparison between Unknown and I4
		//IL_03ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d0: Invalid comparison between Unknown and I4
		_characterModel = null;
		if (ModelPacket.Model_ == null)
		{
			_gameInstance.App.DevTools.Error("Failed to load entity model, model isn't defined");
			return;
		}
		if (!_gameInstance.HashesByServerAssetPath.TryGetValue(ModelPacket.Model_, out var value) || !_gameInstance.EntityStoreModule.GetModel(value, out var model))
		{
			_gameInstance.App.DevTools.Error("Failed to load entity model: " + ModelPacket.Model_);
			return;
		}
		if (ModelPacket.Texture == null)
		{
			_gameInstance.App.DevTools.Error("Failed to load entity model, texture isn't defined");
			return;
		}
		if (!_gameInstance.HashesByServerAssetPath.TryGetValue(ModelPacket.Texture, out var value2))
		{
			_gameInstance.App.DevTools.Error("Failed to load entity texture: " + ModelPacket.Texture);
			return;
		}
		CharacterPartStore characterPartStore = _gameInstance.App.CharacterPartStore;
		byte atlasIndex;
		if (_gameInstance.App.CharacterPartStore.ImageLocations.TryGetValue(ModelPacket.Texture, out var value3))
		{
			atlasIndex = 2;
		}
		else
		{
			if (!_gameInstance.EntityStoreModule.ImageLocations.TryGetValue(value2, out value3))
			{
				_gameInstance.App.DevTools.Error("Cannot use " + ModelPacket.Texture + " as an entity texture");
				return;
			}
			atlasIndex = 1;
		}
		_characterModel = model.Clone();
		_characterModel.SetAtlasIndex(atlasIndex);
		_characterModel.OffsetUVs(value3);
		if (ModelPacket.GradientSet != null && ModelPacket.GradientId != null && characterPartStore.GradientSets.TryGetValue(ModelPacket.GradientSet, out var value4) && value4.Gradients.TryGetValue(ModelPacket.GradientId, out var value5))
		{
			_characterModel.SetGradientId(value5.GradientId);
		}
		bool flag = false;
		bool flag2 = false;
		string[] armorIds = ArmorIds;
		foreach (string text in armorIds)
		{
			if (text == null)
			{
				continue;
			}
			ClientItemBase item = _gameInstance.ItemLibraryModule.GetItem(text);
			if (item == null || item.Armor == null)
			{
				_gameInstance.App.DevTools.Error("Failed to load entity armor part, " + text + " isn't a valid armor item id.");
				continue;
			}
			_armorLight.R = (byte)MathHelper.Max((int)_armorLight.R, (int)item.LightEmitted.R);
			_armorLight.G = (byte)MathHelper.Max((int)_armorLight.G, (int)item.LightEmitted.G);
			_armorLight.B = (byte)MathHelper.Max((int)_armorLight.B, (int)item.LightEmitted.B);
			_characterModel.Attach(item.Model, _gameInstance.EntityStoreModule.NodeNameManager);
			if ((int)item.Armor.ArmorSlot == 0)
			{
				flag = true;
			}
			else if ((int)item.Armor.ArmorSlot == 3)
			{
				flag2 = true;
			}
		}
		if (ModelPacket.Attachments == null)
		{
			return;
		}
		ModelAttachment[] attachments = ModelPacket.Attachments;
		foreach (ModelAttachment val in attachments)
		{
			if (!LoadAttachmentModel(val.Model, val.Texture, out var model2, out var atlasIndex2, out var uvOffset))
			{
				continue;
			}
			if (val.GradientSet != null && val.GradientId != null && characterPartStore.GradientSets.TryGetValue(val.GradientSet, out value4) && value4.Gradients.TryGetValue(val.GradientId, out value5))
			{
				model2.GradientId = value5.GradientId;
			}
			if ((!flag2 || (!val.Model.Contains("Cosmetics/Legs") && !val.Model.Contains("Cosmetics/Feet"))) && (!flag || !val.Model.Contains("Characters/Body_Attachments/Ears")))
			{
				if (flag && val.Model.Contains("Characters/Haircuts"))
				{
					BlockyModelNode node = model2.AllNodes[0].Clone();
					BlockyModelNode node2 = model2.AllNodes[1].Clone();
					model2 = new BlockyModel(2);
					model2.AddNode(ref node);
					model2.AddNode(ref node2, 0);
				}
				_characterModel.Attach(model2, _gameInstance.EntityStoreModule.NodeNameManager, atlasIndex2, uvOffset);
			}
		}
	}

	private void LoadPlayerModel()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		ClientPlayerSkin skin = new ClientPlayerSkin
		{
			BodyType = (((int)PlayerSkin.BodyType_ == 0) ? CharacterBodyType.Masculine : CharacterBodyType.Feminine),
			SkinTone = PlayerSkin.SkinTone,
			Face = PlayerSkin.Face,
			Eyes = CharacterPartId.FromString(PlayerSkin.Eyes),
			Eyebrows = CharacterPartId.FromString(PlayerSkin.Eyebrows),
			SkinFeature = CharacterPartId.FromString(PlayerSkin.SkinFeature),
			Haircut = CharacterPartId.FromString(PlayerSkin.Haircut)
		};
		CharacterPartStore characterPartStore = _gameInstance.App.CharacterPartStore;
		SetModel(characterPartStore, skin);
		HashSet<Cosmetic> cosmeticsToHide = LoadArmorParts();
		SetCosmeticsToDisplay(cosmeticsToHide, skin);
		LoadAttachments(characterPartStore, skin, cosmeticsToHide);
	}

	private HashSet<Cosmetic> LoadArmorParts()
	{
		HashSet<Cosmetic> hashSet = new HashSet<Cosmetic>();
		string[] armorIds = ArmorIds;
		foreach (string text in armorIds)
		{
			if (text == null)
			{
				continue;
			}
			ClientItemBase item = _gameInstance.ItemLibraryModule.GetItem(text);
			if (item == null || item.Armor == null)
			{
				_gameInstance.App.DevTools.Error("Failed to load entity armor part, " + text + " isn't a valid armor item id.");
				continue;
			}
			_armorLight.R = (byte)MathHelper.Max((int)_armorLight.R, (int)item.LightEmitted.R);
			_armorLight.G = (byte)MathHelper.Max((int)_armorLight.G, (int)item.LightEmitted.G);
			_armorLight.B = (byte)MathHelper.Max((int)_armorLight.B, (int)item.LightEmitted.B);
			_characterModel.Attach(item.Model, _gameInstance.EntityStoreModule.NodeNameManager);
			Cosmetic[] cosmeticsToHide = GetCosmeticsToHide(item.Armor);
			if (cosmeticsToHide != null)
			{
				hashSet.UnionWith(cosmeticsToHide);
			}
		}
		return hashSet;
	}

	private void SetModel(CharacterPartStore characterPartStore, ClientPlayerSkin skin)
	{
		characterPartStore.GradientSets["Skin"].Gradients.TryGetValue(skin.SkinTone, out var value);
		_characterModel = characterPartStore.GetAndCloneModel(characterPartStore.GetBodyModelPath(skin.BodyType));
		_characterModel.SetAtlasIndex(2);
		_characterModel.OffsetUVs(characterPartStore.ImageLocations[(skin.BodyType == CharacterBodyType.Masculine) ? "Characters/Player_Textures/Masculine_Greyscale.png" : "Characters/Player_Textures/Feminine_Greyscale.png"]);
		_characterModel.SetGradientId(value?.GradientId ?? 0);
	}

	private void SetCosmeticsToDisplay(HashSet<Cosmetic> cosmeticsToHide, ClientPlayerSkin skin)
	{
		if (!cosmeticsToHide.Contains((Cosmetic)1))
		{
			skin.FacialHair = CharacterPartId.FromString(PlayerSkin.FacialHair);
		}
		if (!cosmeticsToHide.Contains((Cosmetic)4))
		{
			skin.Pants = CharacterPartId.FromString(PlayerSkin.Pants);
		}
		if (!cosmeticsToHide.Contains((Cosmetic)5))
		{
			skin.Overpants = CharacterPartId.FromString(PlayerSkin.Overpants);
		}
		if (!cosmeticsToHide.Contains((Cosmetic)2))
		{
			skin.Undertop = CharacterPartId.FromString(PlayerSkin.Undertop);
		}
		if (!cosmeticsToHide.Contains((Cosmetic)3))
		{
			skin.Overtop = CharacterPartId.FromString(PlayerSkin.Overtop);
		}
		if (!cosmeticsToHide.Contains((Cosmetic)6))
		{
			skin.Shoes = CharacterPartId.FromString(PlayerSkin.Shoes);
		}
		if (!cosmeticsToHide.Contains((Cosmetic)8))
		{
			skin.HeadAccessory = CharacterPartId.FromString(PlayerSkin.HeadAccessory);
		}
		if (!cosmeticsToHide.Contains((Cosmetic)9))
		{
			skin.FaceAccessory = CharacterPartId.FromString(PlayerSkin.FaceAccessory);
		}
		if (!cosmeticsToHide.Contains((Cosmetic)10))
		{
			skin.EarAccessory = CharacterPartId.FromString(PlayerSkin.EarAccessory);
		}
		if (!cosmeticsToHide.Contains((Cosmetic)7))
		{
			skin.Gloves = CharacterPartId.FromString(PlayerSkin.Gloves);
		}
	}

	private void LoadAttachments(CharacterPartStore characterPartStore, ClientPlayerSkin skin, HashSet<Cosmetic> cosmeticsToHide)
	{
		foreach (CharacterAttachment characterAttachment in characterPartStore.GetCharacterAttachments(skin))
		{
			BlockyModel blockyModel = characterPartStore.GetAndCloneModel(characterAttachment.Model);
			if (blockyModel == null)
			{
				Logger.Warn("Tried to load model which is not loaded or does not exist: {0}", characterAttachment.Texture);
				continue;
			}
			if (!characterPartStore.ImageLocations.TryGetValue(characterAttachment.Texture, out var value))
			{
				Logger.Warn("Tried to load model texture which is not loaded or does not exist: {0}", characterAttachment.Texture);
				continue;
			}
			if (characterAttachment.IsUsingBaseNodeOnly || (characterAttachment.Model.StartsWith("Characters/Haircuts") && cosmeticsToHide.Contains((Cosmetic)0)))
			{
				BlockyModelNode node = blockyModel.AllNodes[0].Clone();
				BlockyModelNode node2 = blockyModel.AllNodes[1].Clone();
				blockyModel = new BlockyModel(2);
				blockyModel.AddNode(ref node);
				blockyModel.AddNode(ref node2, 0);
			}
			blockyModel.GradientId = characterAttachment.GradientId;
			_characterModel.Attach(blockyModel, _gameInstance.EntityStoreModule.NodeNameManager, 2, value);
		}
	}

	private Cosmetic[] GetCosmeticsToHide(ClientItemArmor armor)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected I4, but got Unknown
		if (armor.CosmeticsToHide != null)
		{
			return armor.CosmeticsToHide;
		}
		ItemArmorSlot armorSlot = armor.ArmorSlot;
		ItemArmorSlot val = armorSlot;
		return (int)val switch
		{
			0 => _defaultHeadCosmeticsToHide, 
			1 => _defaultChestCosmeticsToHide, 
			2 => _defaultHandsCosmeticsToHide, 
			3 => _defaultLegsCosmeticsToHide, 
			_ => null, 
		};
	}

	private void LoadCharacterAnimations()
	{
		_animationSets.Clear();
		if (ModelPacket.AnimationSets != null)
		{
			foreach (KeyValuePair<string, AnimationSet> animationSet in ModelPacket.AnimationSets)
			{
				ClientAnimationSet clientAnimationSet = new ClientAnimationSet(animationSet.Key);
				clientAnimationSet.PassiveNextDelay = animationSet.Value.NextAnimationDelay;
				if (animationSet.Value.Animations == null)
				{
					continue;
				}
				for (int i = 0; i < animationSet.Value.Animations.Length; i++)
				{
					Animation val = animationSet.Value.Animations[i];
					if (val != null)
					{
						if (val.Animation_ != null && _gameInstance.HashesByServerAssetPath.TryGetValue(val.Animation_, out var value) && _gameInstance.EntityStoreModule.GetAnimation(value, out var animation))
						{
							float speed = ((val.Speed == 0f) ? 1f : val.Speed);
							bool keepPreviousFirstPersonAnimation = false;
							EntityAnimation entityAnimation = new EntityAnimation(animation, speed, val.BlendingDuration * 60f, val.Looping, keepPreviousFirstPersonAnimation, ResourceManager.GetNetworkWwiseId(val.SoundEventIndex), val.Weight, val.FootstepIntervals, val.PassiveLoopCount);
							clientAnimationSet.Animations.Add(entityAnimation);
							clientAnimationSet.WeightSum += entityAnimation.Weight;
						}
						else
						{
							_gameInstance.App.DevTools.Error("Failed to load entity animation in animationSet " + animationSet.Key + ": " + val.Animation_);
						}
					}
				}
				if (clientAnimationSet.Animations.Count == 0)
				{
					_gameInstance.App.DevTools.Error("Failed to load animationSet " + animationSet.Key);
				}
				else
				{
					_animationSets.Add(animationSet.Key, clientAnimationSet);
				}
			}
		}
		SetAnimationSetFallback(_animationSets, "Run", "Idle");
		SetAnimationSetFallback(_animationSets, "RunBackward", "Run");
		SetAnimationSetFallback(_animationSets, "Sprint", "Run");
		SetAnimationSetFallback(_animationSets, "Jump", "Idle");
		SetAnimationSetFallback(_animationSets, "JumpWalk", "Jump");
		SetAnimationSetFallback(_animationSets, "JumpRun", "Jump");
		SetAnimationSetFallback(_animationSets, "JumpSprint", "JumpRun");
		SetAnimationSetFallback(_animationSets, "Fall", "Idle");
		SetAnimationSetFallback(_animationSets, "Crouch", "Idle");
		SetAnimationSetFallback(_animationSets, "CrouchWalk", "Run");
		SetAnimationSetFallback(_animationSets, "CrouchWalkBackward", "CrouchWalk");
		SetAnimationSetFallback(_animationSets, "CrouchSlide", "CrouchWalk");
		SetAnimationSetFallback(_animationSets, "SafetyRoll", "CrouchWalk");
		SetAnimationSetFallback(_animationSets, "FlyIdle", "Idle");
		SetAnimationSetFallback(_animationSets, "Fly", "Run");
		SetAnimationSetFallback(_animationSets, "FlyBackward", "Fly");
		SetAnimationSetFallback(_animationSets, "FlyFast", "Fly");
		SetAnimationSetFallback(_animationSets, "SwimBackward", "Swim");
		SetAnimationSetFallback(_animationSets, "SwimFast", "Swim");
		SetAnimationSetFallback(_animationSets, "SwimSink", "SwimDive");
		SetAnimationSetFallback(_animationSets, "SwimFloat", "SwimSink");
		SetAnimationSetFallback(_animationSets, "SwimIdle", "SwimSink");
		SetAnimationSetFallback(_animationSets, "SwimDive", "Swim");
		SetAnimationSetFallback(_animationSets, "SwimDiveFast", "SwimDive");
		SetAnimationSetFallback(_animationSets, "SwimDiveBackward", "SwimDive");
		SetAnimationSetFallback(_animationSets, "SwimJump", "JumpWalk");
		SetAnimationSetFallback(_animationSets, "FluidIdle", "Idle");
		SetAnimationSetFallback(_animationSets, "FluidWalk", "Run");
		SetAnimationSetFallback(_animationSets, "FluidWalkBackward", "RunBackward");
		SetAnimationSetFallback(_animationSets, "FluidRun", "Sprint");
	}

	public void SetCharacterItemConsumable()
	{
		if (ConsumableItem != null && !(PrimaryItem?.Id == ConsumableItem?.Id))
		{
			_originalPrimaryItem = PrimaryItem;
			SetCharacterItem(ConsumableItem?.Id, SecondaryItem?.Id);
		}
	}

	public void RestoreCharacterItem()
	{
		if (ConsumableItem != null)
		{
			SetCharacterItem(_originalPrimaryItem?.Id, SecondaryItem?.Id);
			ConsumableItem = null;
			_originalPrimaryItem = null;
		}
	}

	public void ChangeCharacterItem(string newItemId, string newSecondaryItemId = null)
	{
		if (ConsumableItem != null)
		{
			GetEquipedItems(newItemId, newSecondaryItemId, out _originalPrimaryItem, out var newSecondaryItem);
			SecondaryItem = newSecondaryItem;
			SetCharacterItem(ConsumableItem?.Id, SecondaryItem?.Id);
		}
		else
		{
			SetCharacterItem(newItemId, newSecondaryItemId);
		}
	}

	private void GetEquipedItems(string newItemId, string newSecondaryItemId, out ClientItemBase newItem, out ClientItemBase newSecondaryItem)
	{
		newItem = _gameInstance.ItemLibraryModule.GetItem(newItemId);
		if (newSecondaryItemId != null && (newItem == null || (newItem?.Utility?.Compatible).GetValueOrDefault()))
		{
			newSecondaryItem = _gameInstance.ItemLibraryModule.GetItem(newSecondaryItemId);
		}
		else
		{
			newSecondaryItem = null;
		}
	}

	public void SetCharacterItem(string newItemId, string newSecondaryItemId = null)
	{
		GetEquipedItems(newItemId, newSecondaryItemId, out var newItem, out var newSecondaryItem);
		if (NetworkId == _gameInstance.LocalPlayerNetworkId && this is PlayerEntity playerEntity)
		{
			playerEntity.UpdateItemStatModifiers(newItem, newSecondaryItem);
		}
		if (newItem == PrimaryItem && newSecondaryItem == SecondaryItem)
		{
			return;
		}
		_itemCameraSettings = null;
		_itemLight.R = 0;
		_itemLight.G = 0;
		_itemLight.B = 0;
		PrimaryItem = newItem;
		SecondaryItem = newSecondaryItem;
		ClearCombatSequenceEffects();
		for (int i = 0; i < EntityItems.Count; i++)
		{
			EntityItem entityItem = EntityItems[i];
			if (entityItem.SoundEventReference.PlaybackId != -1)
			{
				_gameInstance.AudioModule.ActionOnEvent(ref entityItem.SoundEventReference, (AkActionOnEventType)0);
			}
			for (int j = 0; j < EntityItems[i].ParentParticles.Count; j++)
			{
				EntityParticle entityParticle = entityItem.ParentParticles[j];
				entityParticle.ParticleSystemProxy?.Expire();
				_entityParticles.Remove(entityParticle);
			}
			for (int k = 0; k < EntityItems[i].ParentTrails.Count; k++)
			{
				EntityTrail entityTrail = entityItem.ParentTrails[k];
				if (entityTrail.TrailProxy != null)
				{
					entityTrail.TrailProxy.IsExpired = true;
				}
				_entityTrails.Remove(entityTrail);
			}
			entityItem.ClearFX();
			entityItem.ModelRenderer.Dispose();
			entityItem.ModelRenderer = null;
		}
		EntityItems.Clear();
		if (NetworkId == _gameInstance.LocalPlayerNetworkId)
		{
			_gameInstance.LocalPlayer.ClearFirstPersonItems();
		}
		if (_characterModel != null)
		{
			if (PrimaryItem != null)
			{
				AttachItem(_characterModel, PrimaryItem, CharacterPartStore.RightAttachmentNodeNameId);
				HandleItemConditionAppearanceForAllEntityStats(PrimaryItem, 0);
			}
			if (SecondaryItem != null)
			{
				AttachItem(_characterModel, SecondaryItem, CharacterPartStore.LeftAttachmentNodeNameId);
				HandleItemConditionAppearanceForAllEntityStats(SecondaryItem, (PrimaryItem != null) ? 1 : 0);
			}
			SetupItemCamera();
			float slotAnimationTime = ModelRenderer.GetSlotAnimationTime(0);
			if (ServerAnimations[0] != null)
			{
				SetServerAnimation(ServerAnimations[0], (AnimationSlot)0);
			}
			else
			{
				SetMovementAnimation(_currentAnimationId ?? "Idle", slotAnimationTime, force: true);
			}
			if (NetworkId == _gameInstance.LocalPlayerNetworkId)
			{
				_gameInstance.LocalPlayer.SetFirstPersonItems();
			}
			ICameraController controller = _gameInstance.CameraModule.Controller;
			if (controller.AttachedTo == this)
			{
				controller.Reset(_gameInstance, controller);
			}
			ModelRenderer.SetCameraNodes(CameraSettings);
		}
	}

	protected void RefreshCharacterItemParticles()
	{
		for (int i = 0; i < EntityItems.Count; i++)
		{
			EntityItem entityItem = EntityItems[i];
			for (int j = 0; j < EntityItems[i].Particles.Count; j++)
			{
				EntityParticle entityParticle = entityItem.Particles[j];
				entityParticle.ParticleSystemProxy?.Expire();
				_entityParticles.Remove(entityParticle);
			}
			for (int k = 0; k < EntityItems[i].ParentParticles.Count; k++)
			{
				EntityParticle entityParticle2 = entityItem.ParentParticles[k];
				entityParticle2.ParticleSystemProxy?.Expire();
				_entityParticles.Remove(entityParticle2);
			}
			entityItem.ParentParticles.Clear();
			entityItem.Particles.Clear();
		}
		if (PrimaryItem != null)
		{
			RefreshItemParticles(PrimaryItem, EntityItems[0]);
			EntityItems[0].CurrentItemAppearanceCondition = null;
			HandleItemConditionAppearanceForAllEntityStats(PrimaryItem, 0);
		}
		if (SecondaryItem != null)
		{
			int num = ((PrimaryItem != null) ? 1 : 0);
			RefreshItemParticles(SecondaryItem, EntityItems[num]);
			EntityItems[num].CurrentItemAppearanceCondition = null;
			HandleItemConditionAppearanceForAllEntityStats(SecondaryItem, num);
		}
	}

	private void RefreshItemParticles(ClientItemBase item, EntityItem entityItem)
	{
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Invalid comparison between Unknown and I4
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Invalid comparison between Unknown and I4
		if (item == null)
		{
			return;
		}
		BlockyModel model = item.Model;
		if (item.BlockId != 0)
		{
			ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[item.BlockId];
			model = clientBlockType.FinalBlockyModel;
			if (clientBlockType.Particles == null)
			{
				return;
			}
			for (int i = 0; i < clientBlockType.Particles.Length; i++)
			{
				EntityParticle entityParticle = AttachParticles(model, entityItem.Particles, clientBlockType.Particles[i], Scale * entityItem.Scale * 0.5f);
				if (entityParticle != null && (int)entityParticle.EntityPart == 1)
				{
					entityItem.ParentParticles.Add(entityParticle);
				}
			}
			return;
		}
		ModelParticleSettings[] array = ((DoFirstPersonParticles() && item.FirstPersonParticles != null) ? item.FirstPersonParticles : item.Particles);
		if (array == null)
		{
			return;
		}
		for (int j = 0; j < array.Length; j++)
		{
			EntityParticle entityParticle2 = AttachParticles(model, entityItem.Particles, array[j], Scale * entityItem.Scale);
			if (entityParticle2 != null && (int)entityParticle2.EntityPart == 1)
			{
				entityItem.ParentParticles.Add(entityParticle2);
			}
		}
	}

	protected void SetAnimationSetFallback(Dictionary<string, ClientAnimationSet> animations, string source, string fallback)
	{
		if (!animations.ContainsKey(source) && animations.TryGetValue(fallback, out var value))
		{
			animations.Add(source, new ClientAnimationSet(source, value));
		}
	}

	public virtual bool AddCombatSequenceEffects(ModelParticle[] particles, ModelTrail[] trails)
	{
		List<EntityParticle> particleList = null;
		List<EntityTrail> trailList = null;
		BlockyModel model = null;
		float num = 1f;
		if (PrimaryItem != null)
		{
			particleList = EntityItems[0].Particles;
			trailList = EntityItems[0].Trails;
			model = PrimaryItem.Model;
			num = PrimaryItem.Scale;
		}
		if (particles != null)
		{
			for (int i = 0; i < particles.Length; i++)
			{
				ModelParticleSettings clientModelParticle = new ModelParticleSettings();
				ParticleProtocolInitializer.Initialize(particles[i], ref clientModelParticle, _gameInstance.EntityStoreModule.NodeNameManager);
				EntityParticle entityParticle = AttachParticles(model, particleList, clientModelParticle, Scale * num);
				if (entityParticle != null)
				{
					_combatSequenceParticles.Add(entityParticle);
				}
			}
		}
		if (trails != null)
		{
			for (int j = 0; j < trails.Length; j++)
			{
				EntityTrail entityTrail = AttachTrails(model, trailList, trails[j], Scale * num);
				if (entityTrail != null)
				{
					_combatSequenceTrails.Add(entityTrail);
				}
			}
		}
		return true;
	}

	public void ClearCombatSequenceEffects()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Invalid comparison between Unknown and I4
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Invalid comparison between Unknown and I4
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Invalid comparison between Unknown and I4
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Invalid comparison between Unknown and I4
		for (int i = 0; i < _combatSequenceParticles.Count; i++)
		{
			EntityParticle entityParticle = _combatSequenceParticles[i];
			entityParticle.ParticleSystemProxy?.Expire();
			if ((int)entityParticle.EntityPart == 1)
			{
				_entityParticles.Remove(entityParticle);
			}
			else if ((int)entityParticle.EntityPart == 3)
			{
				EntityItems[(EntityItems.Count > 1) ? 1 : 0].Particles.Remove(entityParticle);
			}
			else
			{
				EntityItems[0].Particles.Remove(entityParticle);
			}
		}
		_combatSequenceParticles.Clear();
		for (int j = 0; j < _combatSequenceTrails.Count; j++)
		{
			EntityTrail entityTrail = _combatSequenceTrails[j];
			if (entityTrail.TrailProxy != null)
			{
				entityTrail.TrailProxy.IsExpired = true;
			}
			if ((int)entityTrail.EntityPart == 1)
			{
				_entityTrails.Remove(entityTrail);
			}
			else if ((int)entityTrail.EntityPart == 3)
			{
				EntityItems[(EntityItems.Count > 1) ? 1 : 0].Trails.Remove(entityTrail);
			}
			else
			{
				EntityItems[0].Trails.Remove(entityTrail);
			}
		}
		_combatSequenceTrails.Clear();
	}

	private void SetupItemCamera()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Expected O, but got Unknown
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Expected O, but got Unknown
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Expected O, but got Unknown
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Expected O, but got Unknown
		CameraSettings val = (PrimaryItem ?? SecondaryItem)?.PlayerAnimations?.Camera;
		if (val != null)
		{
			_itemCameraSettings = new CameraSettings(val);
			if (_itemCameraSettings.PositionOffset == null)
			{
				_itemCameraSettings.PositionOffset = _modelCameraSettings.PositionOffset;
			}
			if (_itemCameraSettings.Yaw == null)
			{
				_itemCameraSettings.Yaw = new CameraAxis(_modelCameraSettings.Yaw);
			}
			else if (_itemCameraSettings.Yaw.AngleRange == null)
			{
				_itemCameraSettings.Yaw.AngleRange = new Rangef(_modelCameraSettings.Yaw.AngleRange);
			}
			if (_itemCameraSettings.Pitch == null)
			{
				_itemCameraSettings.Pitch = new CameraAxis(_modelCameraSettings.Pitch);
			}
			else if (_itemCameraSettings.Pitch.AngleRange == null)
			{
				_itemCameraSettings.Pitch.AngleRange = new Rangef(_modelCameraSettings.Pitch.AngleRange);
			}
		}
	}

	private void AttachItem(BlockyModel model, ClientItemBase item, int defaultTargetAttachmentNameId)
	{
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Invalid comparison between Unknown and I4
		//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Invalid comparison between Unknown and I4
		//IL_042e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0434: Invalid comparison between Unknown and I4
		EntityItem entityItem = new EntityItem(_gameInstance);
		BlockyModel blockyModel = item.Model;
		entityItem.Scale = item.Scale;
		if (item.BlockId != 0)
		{
			ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[item.BlockId];
			blockyModel = clientBlockType.FinalBlockyModel;
			entityItem.Scale *= clientBlockType.BlockyModelScale;
		}
		if (item.Armor == null && blockyModel.RootNodes.Count == 1 && blockyModel.AllNodes[blockyModel.RootNodes[0]].IsPiece)
		{
			entityItem.TargetNodeNameId = blockyModel.AllNodes[blockyModel.RootNodes[0]].NameId;
			entityItem.SetRootOffsets(Vector3.Negate(blockyModel.AllNodes[blockyModel.RootNodes[0]].Position), Quaternion.Inverse(blockyModel.AllNodes[blockyModel.RootNodes[0]].Orientation));
		}
		else
		{
			entityItem.TargetNodeNameId = defaultTargetAttachmentNameId;
		}
		if (!model.NodeIndicesByNameId.TryGetValue(entityItem.TargetNodeNameId, out entityItem.TargetNodeIndex))
		{
			entityItem.TargetNodeIndex = 0;
		}
		entityItem.ModelRenderer = new ModelRenderer(blockyModel, _gameInstance.AtlasSizes, _gameInstance.Engine.Graphics, _gameInstance.FrameCounter);
		BlockyAnimation animation = ((item.BlockId != 0) ? _gameInstance.MapModule.ClientBlockTypes[item.BlockId].BlockyAnimation : item?.Animation);
		entityItem.ModelRenderer.SetSlotAnimation(0, animation);
		EntityItems.Add(entityItem);
		if (item.BlockId != 0)
		{
			ClientBlockType clientBlockType2 = _gameInstance.MapModule.ClientBlockTypes[item.BlockId];
			if (clientBlockType2.Particles != null)
			{
				for (int i = 0; i < clientBlockType2.Particles.Length; i++)
				{
					EntityParticle entityParticle = AttachParticles(blockyModel, entityItem.Particles, clientBlockType2.Particles[i], Scale * entityItem.Scale * 0.5f);
					if (entityParticle != null && (int)entityParticle.EntityPart == 1)
					{
						entityItem.ParentParticles.Add(entityParticle);
					}
				}
			}
			_gameInstance.AudioModule.PlaySoundEvent(clientBlockType2.SoundEventIndex, SoundObjectReference, ref entityItem.SoundEventReference);
			_itemLight.R = (byte)MathHelper.Max((int)_itemLight.R, (int)clientBlockType2.LightEmitted.R);
			_itemLight.G = (byte)MathHelper.Max((int)_itemLight.G, (int)clientBlockType2.LightEmitted.G);
			_itemLight.B = (byte)MathHelper.Max((int)_itemLight.B, (int)clientBlockType2.LightEmitted.B);
			return;
		}
		_gameInstance.AudioModule.PlaySoundEvent(item.SoundEventIndex, SoundObjectReference, ref entityItem.SoundEventReference);
		ModelParticleSettings[] array = ((DoFirstPersonParticles() && item.FirstPersonParticles != null) ? item.FirstPersonParticles : item.Particles);
		if (array != null)
		{
			for (int j = 0; j < array.Length; j++)
			{
				EntityParticle entityParticle2 = AttachParticles(blockyModel, entityItem.Particles, array[j], Scale * entityItem.Scale);
				if (entityParticle2 != null && (int)entityParticle2.EntityPart == 1)
				{
					entityItem.ParentParticles.Add(entityParticle2);
				}
			}
		}
		if (item.Trails != null)
		{
			for (int k = 0; k < item.Trails.Length; k++)
			{
				EntityTrail entityTrail = AttachTrails(blockyModel, entityItem.Trails, item.Trails[k], Scale * entityItem.Scale);
				if (entityTrail != null && (int)entityTrail.EntityPart == 1)
				{
					entityItem.ParentTrails.Add(entityTrail);
				}
			}
		}
		_itemLight.R = (byte)MathHelper.Max((int)_itemLight.R, (int)item.LightEmitted.R);
		_itemLight.G = (byte)MathHelper.Max((int)_itemLight.G, (int)item.LightEmitted.G);
		_itemLight.B = (byte)MathHelper.Max((int)_itemLight.B, (int)item.LightEmitted.B);
	}

	public void AddModelParticles(ModelParticleSettings[] modelParticles)
	{
		for (int i = 0; i < modelParticles.Length; i++)
		{
			AttachParticles(_characterModel, _entityParticles, modelParticles[i], Scale);
		}
	}

	private EntityParticle AttachParticles(BlockyModel model, List<EntityParticle> particleList, ModelParticleSettings particle, float itemScale)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected I4, but got Unknown
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		EntityParticle entityParticle = null;
		EntityPart targetEntityPart = particle.TargetEntityPart;
		EntityPart val = targetEntityPart;
		switch (val - 1)
		{
		case 1:
			if (PrimaryItem == null)
			{
				return null;
			}
			particleList = EntityItems[0].Particles;
			itemScale = Scale * PrimaryItem.Scale;
			if (PrimaryItem.BlockId != 0)
			{
				model = _gameInstance.MapModule.ClientBlockTypes[PrimaryItem.BlockId].FinalBlockyModel;
				itemScale *= 0.5f;
			}
			else
			{
				model = PrimaryItem.Model;
			}
			break;
		case 2:
			if (SecondaryItem == null)
			{
				return null;
			}
			particleList = EntityItems[(EntityItems.Count > 1) ? 1 : 0].Particles;
			itemScale = Scale * SecondaryItem.Scale;
			if (SecondaryItem.BlockId != 0)
			{
				model = _gameInstance.MapModule.ClientBlockTypes[SecondaryItem.BlockId].FinalBlockyModel;
				itemScale *= 0.5f;
			}
			else
			{
				model = SecondaryItem.Model;
			}
			break;
		case 0:
			model = _characterModel;
			particleList = _entityParticles;
			itemScale = Scale;
			break;
		default:
			if (model == null || particleList == null)
			{
				return null;
			}
			break;
		}
		bool isTracked = !particle.DetachedFromModel;
		if (model == null || model.NodeCount == 0 || particle.SystemId == null || !_gameInstance.ParticleSystemStoreModule.TrySpawnSystem(particle.SystemId, out var particleSystemProxy, NetworkId == _gameInstance.LocalPlayerNetworkId, isTracked))
		{
			return null;
		}
		int value = 0;
		particleSystemProxy.Scale = itemScale * particle.Scale;
		if (!particle.Color.IsTransparent)
		{
			particleSystemProxy.DefaultColor = particle.Color;
		}
		if (particle.TargetNodeNameId != -1)
		{
			model.NodeIndicesByNameId.TryGetValue(particle.TargetNodeNameId, out value);
		}
		else
		{
			particle.TargetNodeNameId = model.AllNodes[0].NameId;
		}
		if (value >= BlockyModel.MaxNodeCount)
		{
			return null;
		}
		entityParticle = new EntityParticle(particleSystemProxy, particle.TargetEntityPart, value, particle.TargetNodeNameId, itemScale);
		entityParticle.PositionOffset = particle.PositionOffset * itemScale;
		entityParticle.RotationOffset = particle.RotationOffset;
		if (!particle.DetachedFromModel)
		{
			particleList.Add(entityParticle);
			return entityParticle;
		}
		AnimatedRenderer.NodeTransform nodeTransform = ModelRenderer.NodeTransforms[entityParticle.TargetNodeIndex];
		Vector3 position = RenderPosition + Vector3.Transform(nodeTransform.Position, RenderOrientation) * (1f / 64f) * Scale + Vector3.Transform(entityParticle.PositionOffset, RenderOrientation * nodeTransform.Orientation);
		entityParticle.ParticleSystemProxy.Position = position;
		entityParticle.ParticleSystemProxy.Rotation = RenderOrientation * nodeTransform.Orientation * entityParticle.RotationOffset;
		return entityParticle;
	}

	private EntityTrail AttachTrails(BlockyModel model, List<EntityTrail> trailList, ModelTrail modelTrail, float scale)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected I4, but got Unknown
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		if (modelTrail.TrailId == null)
		{
			return null;
		}
		int value = 0;
		int num = -1;
		EntityPart targetEntityPart = modelTrail.TargetEntityPart;
		EntityPart val = targetEntityPart;
		switch (val - 1)
		{
		case 1:
			if (PrimaryItem == null)
			{
				return null;
			}
			model = ((PrimaryItem.BlockId != 0) ? _gameInstance.MapModule.ClientBlockTypes[PrimaryItem.BlockId].FinalBlockyModel : PrimaryItem.Model);
			trailList = EntityItems[0].Trails;
			scale = Scale * PrimaryItem.Scale;
			break;
		case 2:
			if (SecondaryItem == null)
			{
				return null;
			}
			model = ((SecondaryItem.BlockId != 0) ? _gameInstance.MapModule.ClientBlockTypes[SecondaryItem.BlockId].FinalBlockyModel : SecondaryItem.Model);
			trailList = EntityItems[(EntityItems.Count > 1) ? 1 : 0].Trails;
			scale = Scale * SecondaryItem.Scale;
			break;
		case 0:
			model = _characterModel;
			trailList = _entityTrails;
			scale = Scale;
			break;
		default:
			if (model == null || trailList == null)
			{
				return null;
			}
			break;
		}
		if (model.NodeCount == 0 || !_gameInstance.TrailStoreModule.TrySpawnTrailProxy(modelTrail.TrailId, out var trailProxy, NetworkId == _gameInstance.LocalPlayerNetworkId))
		{
			return null;
		}
		trailProxy.Scale = scale;
		if (modelTrail.TargetNodeName != null)
		{
			num = _gameInstance.EntityStoreModule.NodeNameManager.GetOrAddNameId(modelTrail.TargetNodeName);
			model.NodeIndicesByNameId.TryGetValue(num, out value);
		}
		else
		{
			num = model.AllNodes[0].NameId;
		}
		if (value >= BlockyModel.MaxNodeCount)
		{
			return null;
		}
		EntityTrail entityTrail = new EntityTrail(trailProxy, modelTrail.TargetEntityPart, value, num, modelTrail.FixedRotation);
		if (modelTrail.PositionOffset != null)
		{
			entityTrail.PositionOffset.X = modelTrail.PositionOffset.X;
			entityTrail.PositionOffset.Y = modelTrail.PositionOffset.Y;
			entityTrail.PositionOffset.Z = modelTrail.PositionOffset.Z;
		}
		entityTrail.PositionOffset *= scale;
		if (modelTrail.RotationOffset != null)
		{
			entityTrail.RotationOffset = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(modelTrail.RotationOffset.Yaw), MathHelper.ToRadians(modelTrail.RotationOffset.Pitch), MathHelper.ToRadians(modelTrail.RotationOffset.Roll));
		}
		trailList.Add(entityTrail);
		return entityTrail;
	}

	private bool LoadAttachmentModel(string modelPath, string texturePath, out BlockyModel model, out byte atlasIndex, out Point uvOffset)
	{
		model = null;
		atlasIndex = 0;
		uvOffset = Point.Zero;
		if (modelPath == null)
		{
			_gameInstance.App.DevTools.Error("Failed to load entity attachment, model isn't defined");
			return false;
		}
		if (!_gameInstance.App.CharacterPartStore.Models.TryGetValue("Common/" + modelPath, out model) && (!_gameInstance.HashesByServerAssetPath.TryGetValue(modelPath, out var value) || !_gameInstance.EntityStoreModule.GetModel(value, out model)))
		{
			_gameInstance.App.DevTools.Error("Failed to load entity attachment model: " + modelPath + " with texture path: " + texturePath);
			return false;
		}
		if (texturePath == null)
		{
			_gameInstance.App.DevTools.Error("Failed to load entity attachment, texture isn't defined");
			return false;
		}
		if (_gameInstance.App.CharacterPartStore.ImageLocations.TryGetValue(texturePath, out uvOffset))
		{
			atlasIndex = 2;
		}
		else
		{
			if (!_gameInstance.HashesByServerAssetPath.TryGetValue(texturePath, out var value2))
			{
				_gameInstance.App.DevTools.Error("Failed to load entity attachment texture: " + texturePath + " with model path: " + modelPath);
				return false;
			}
			if (!_gameInstance.EntityStoreModule.ImageLocations.TryGetValue(value2, out uvOffset))
			{
				_gameInstance.App.DevTools.Error("Cannot use " + texturePath + " as an entity attachment texture");
				return false;
			}
			atlasIndex = 1;
		}
		return true;
	}

	public void SetItem(Item itemPacket)
	{
		_itemPacket = itemPacket;
		if (_itemParticleSystem != null)
		{
			_itemParticleSystem.Expire(instant: true);
			_itemParticleSystem = null;
		}
		if (itemPacket == null)
		{
			return;
		}
		Type = EntityType.Item;
		DefaultHitbox = new BoundingBox(new Vector3(-0.25f, 0f, -0.25f), new Vector3(0.25f, 0.5f, 0.25f));
		Hitbox = DefaultHitbox;
		if (ModelRenderer != null)
		{
			ModelRenderer.Dispose();
			ModelRenderer = null;
		}
		ClientItemBase item = _gameInstance.ItemLibraryModule.GetItem(itemPacket.ItemId);
		if (item == null)
		{
			item = _gameInstance.ItemLibraryModule.GetItem("Unknown");
			_gameInstance.App.DevTools.Error("Failed to load dropped item: " + itemPacket.ItemId);
		}
		ItemBase = item;
		BlockyModel model = item.Model;
		Scale = item.Scale;
		if (item.BlockId != 0)
		{
			ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[item.BlockId];
			model = clientBlockType.FinalBlockyModel;
			Scale *= clientBlockType.BlockyModelScale;
		}
		ModelRenderer = new ModelRenderer(model, _gameInstance.AtlasSizes, _gameInstance.Engine.Graphics, _gameInstance.FrameCounter);
		ModelRenderer.UpdatePose();
		if (item.DroppedItemAnimation != null && !itemPacket.OverrideDroppedItemAnimation)
		{
			ModelRenderer.SetSlotAnimation(0, item.DroppedItemAnimation);
		}
		if (item.ItemEntity.ParticleSystemId != null && !IsUsable() && !IsLocalEntity)
		{
			Color particleColor = item.ItemEntity.ParticleColor;
			UInt32Color defaultColor = ((particleColor != null) ? UInt32Color.FromRGBA((byte)particleColor.Red, (byte)particleColor.Green, (byte)particleColor.Blue, byte.MaxValue) : UInt32Color.White);
			if (_gameInstance.ParticleSystemStoreModule.TrySpawnSystem(item.ItemEntity.ParticleSystemId, out _itemParticleSystem, isLocalPlayer: false, isTracked: true))
			{
				_itemParticleSystem.DefaultColor = defaultColor;
			}
		}
		if (!item.ItemEntity.ShowItemParticles)
		{
			return;
		}
		ModelParticleSettings[] array = ((DoFirstPersonParticles() && item.FirstPersonParticles != null) ? item.FirstPersonParticles : item.Particles);
		if (array != null)
		{
			for (int i = 0; i < array.Length; i++)
			{
				AttachParticles(model, _entityParticles, array[i], Scale);
			}
		}
		if (item.BlockId == 0)
		{
			return;
		}
		ClientBlockType clientBlockType2 = _gameInstance.MapModule.ClientBlockTypes[item.BlockId];
		if (clientBlockType2.Particles != null)
		{
			for (int j = 0; j < clientBlockType2.Particles.Length; j++)
			{
				AttachParticles(model, _entityParticles, clientBlockType2.Particles[j], Scale * 0.5f);
			}
		}
	}

	public void SetBlock(int blockId, float scale)
	{
		SetBlock(blockId);
		if (scale > 1E-05f)
		{
			Scale = scale;
		}
	}

	public void SetBlock(int blockId)
	{
		_blockId = blockId;
		if (blockId != 0)
		{
			Type = EntityType.Block;
			Scale = 2f;
			if (ModelRenderer != null)
			{
				ModelRenderer.Dispose();
				ModelRenderer = null;
			}
			if (blockId >= _gameInstance.MapModule.ClientBlockTypes.Length)
			{
				_gameInstance.App.DevTools.Error("Failed to find block for entity: " + blockId);
				blockId = 1;
			}
			ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[blockId];
			BoundingBox boundingBox = _gameInstance.ServerSettings.BlockHitboxes[clientBlockType.HitboxType].BoundingBox;
			boundingBox.Min.X -= 0.5f;
			boundingBox.Min.Z -= 0.5f;
			boundingBox.Max.X -= 0.5f;
			boundingBox.Max.Z -= 0.5f;
			DefaultHitbox = boundingBox;
			Hitbox = boundingBox;
			ModelRenderer = new ModelRenderer(_characterModel = clientBlockType.FinalBlockyModel, _gameInstance.AtlasSizes, _gameInstance.Engine.Graphics, _gameInstance.FrameCounter);
			ModelRenderer.UpdatePose();
		}
	}

	public void SetDynamicLight(ColorLight dynamicLight)
	{
		if (dynamicLight != null)
		{
			ClientItemBaseProtocolInitializer.ParseLightColor(dynamicLight, ref DynamicLight);
		}
		else
		{
			DynamicLight = ColorRgb.Zero;
		}
		UpdateLight();
	}

	public void UpdateLight()
	{
		if (!ShouldRender)
		{
			_gameInstance.EntityStoreModule.SetEntityLight(NetworkId, null);
		}
		byte r = DynamicLight.R;
		byte g = DynamicLight.G;
		byte b = DynamicLight.B;
		r = (byte)MathHelper.Max((int)r, (int)_armorLight.R);
		g = (byte)MathHelper.Max((int)g, (int)_armorLight.G);
		b = (byte)MathHelper.Max((int)b, (int)_armorLight.B);
		r = (byte)MathHelper.Max((int)r, (int)_itemLight.R);
		g = (byte)MathHelper.Max((int)g, (int)_itemLight.G);
		b = (byte)MathHelper.Max((int)b, (int)_itemLight.B);
		if (r != 0 || g != 0 || b != 0)
		{
			_gameInstance.EntityStoreModule.SetEntityLight(NetworkId, new Vector3((float)(int)r / 15f, (float)(int)g / 15f, (float)(int)b / 15f));
		}
		else
		{
			_gameInstance.EntityStoreModule.SetEntityLight(NetworkId, null);
		}
	}

	public void RebuildRenderers(bool itemOnly)
	{
		if (_itemPacket != null)
		{
			SetItem(_itemPacket);
		}
		else if (_blockId != 0)
		{
			SetBlock(_blockId);
		}
		else if (ModelPacket != null)
		{
			if (!itemOnly)
			{
				LoadCharacterAnimations();
				SetCharacterModel(null, null);
			}
			string newItemId = PrimaryItem?.Id;
			PrimaryItem = null;
			string newSecondaryItemId = SecondaryItem?.Id;
			SecondaryItem = null;
			SetCharacterItem(newItemId, newSecondaryItemId);
			UpdateLight();
		}
	}

	protected EntityAnimation GetItemAnimation(ClientItemBase item, string animationId, bool useDefaultAnimations = true)
	{
		EntityAnimation value = item?.GetAnimation(animationId);
		if (value != null)
		{
			return value;
		}
		if (useDefaultAnimations && _gameInstance.ItemLibraryModule.DefaultItemPlayerAnimations.Animations.TryGetValue(animationId, out value))
		{
			return value;
		}
		return EntityAnimation.Empty;
	}

	private void SetMovementAnimation(string animationId, float animationTime = -1f, bool force = false, bool noBlending = false)
	{
		if (!CanPlayAnimations() || (_currentAnimationId == animationId && !force))
		{
			return;
		}
		CycleJumpAnimations(animationId);
		if (_currentAnimationId != animationId)
		{
			ClearPassiveAnimationData();
		}
		_currentAnimationId = animationId;
		_currentAnimationRunTime = 0f;
		EntityAnimation entityAnimation = GetAnimation(animationId) ?? EntityAnimation.Empty;
		EntityAnimation itemAnimation = GetItemAnimation(PrimaryItem, _currentAnimationId);
		EntityAnimation itemAnimation2 = GetItemAnimation(SecondaryItem, _currentAnimationId, useDefaultAnimations: false);
		bool looping = entityAnimation.Looping;
		if (!looping)
		{
			animationTime = 0f;
		}
		else if (animationTime == -1f)
		{
			animationTime = ModelRenderer.GetSlotAnimationTime(0);
		}
		float num = entityAnimation.Speed;
		float num2 = itemAnimation.Speed;
		float num3 = itemAnimation2.Speed;
		if (this == _gameInstance.LocalPlayer)
		{
			float currentSpeedMultiplierDiff = _gameInstance.CharacterControllerModule.MovementController.CurrentSpeedMultiplierDiff;
			num += num * currentSpeedMultiplierDiff;
			num2 += num2 * currentSpeedMultiplierDiff;
			num3 += num3 * currentSpeedMultiplierDiff;
		}
		if (noBlending)
		{
			ModelRenderer.SetSlotAnimationNoBlending(0, entityAnimation.Data, looping, num, animationTime);
			ModelRenderer.SetSlotAnimationNoBlending(1, itemAnimation.Data, looping, num2, animationTime);
			ModelRenderer.SetSlotAnimationNoBlending(2, itemAnimation2.Data, looping, num3, animationTime);
			if (PrimaryItem != null && PrimaryItem.UsePlayerAnimations)
			{
				EntityItems[0].ModelRenderer.SetSlotAnimationNoBlending(1, itemAnimation.Data, looping, num2, animationTime);
			}
			if (SecondaryItem != null && SecondaryItem.UsePlayerAnimations)
			{
				ModelRenderer modelRenderer = EntityItems[(PrimaryItem != null) ? 1 : 0].ModelRenderer;
				modelRenderer.SetSlotAnimationNoBlending(2, itemAnimation2.Data, looping, num3, animationTime);
			}
		}
		else
		{
			ModelRenderer.SetSlotAnimation(0, entityAnimation.Data, looping, num, animationTime, entityAnimation.BlendingDuration);
			ModelRenderer.SetSlotAnimation(1, itemAnimation.Data, looping, num2, animationTime, itemAnimation.BlendingDuration);
			ModelRenderer.SetSlotAnimation(2, itemAnimation2.Data, looping, num3, animationTime, itemAnimation2.BlendingDuration);
			if (PrimaryItem != null && PrimaryItem.UsePlayerAnimations)
			{
				EntityItems[0].ModelRenderer.SetSlotAnimation(1, itemAnimation.Data, looping, num2, animationTime, itemAnimation.BlendingDuration);
			}
			if (SecondaryItem != null && SecondaryItem.UsePlayerAnimations)
			{
				ModelRenderer modelRenderer2 = EntityItems[(PrimaryItem != null) ? 1 : 0].ModelRenderer;
				modelRenderer2.SetSlotAnimation(2, itemAnimation2.Data, looping, num3, animationTime, itemAnimation2.BlendingDuration);
			}
		}
		CurrentMovementAnimation = entityAnimation;
	}

	private void CycleJumpAnimations(string animationId)
	{
		if (!JumpAnimations.Contains(animationId))
		{
			return;
		}
		if (_lastJumpAnimation == null)
		{
			_lastJumpAnimation = animationId;
			return;
		}
		if (_animationSets.TryGetValue(animationId, out var value))
		{
			if (_lastJumpAnimation == animationId || (_animationSets.TryGetValue(_lastJumpAnimation, out var value2) && value2.Animations.Count == value.Animations.Count))
			{
				_currentJumpAnimation++;
				_currentJumpAnimation %= value.Animations.Count;
			}
			else
			{
				_currentJumpAnimation = 0;
			}
		}
		_lastJumpAnimation = animationId;
	}

	public void PredictStatusAnimation(string animationId)
	{
		SetServerAnimation(animationId, (AnimationSlot)1, 0f);
		PredictedStatusCount++;
	}

	public virtual void SetServerAnimation(string animationId, AnimationSlot slot, float animationTime = -1f, bool storeCurrentAnimationId = false)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Invalid comparison between Unknown and I4
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Invalid comparison between Unknown and I4
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		if (!CanPlayAnimations())
		{
			return;
		}
		if (!EntityAnimation.AnimationSlot.GetSlot(slot, out var slotId))
		{
			_gameInstance.App.DevTools.Error($"Unknown animation slot {slot} on entity with model {ModelPacket.Model_}");
		}
		else if (animationId != null)
		{
			EntityAnimation entityAnimation;
			if ((int)slot == 4)
			{
				Emote emote = _gameInstance.App.CharacterPartStore.GetEmote(animationId);
				if (emote == null)
				{
					_gameInstance.App.DevTools.Error("No emote with id " + animationId + " on entity with model " + ModelPacket.Model_);
					return;
				}
				BlockyAnimation animation = _gameInstance.App.CharacterPartStore.GetAnimation(emote.Animation);
				entityAnimation = new EntityAnimation(animation, 1f, 12f, looping: false, keepPreviousFirstPersonAnimation: false, 0u, 0f, Array.Empty<int>(), 1);
			}
			else if ((int)slot == 2)
			{
				entityAnimation = GetItemAnimation(PrimaryItem, animationId);
				if (entityAnimation == null || entityAnimation == EntityAnimation.Empty)
				{
					entityAnimation = GetAnimation(animationId);
				}
				if (entityAnimation == null)
				{
					_gameInstance.App.DevTools.Error("No animation with id " + animationId + " on entity with model " + ModelPacket.Model_);
					return;
				}
				CurrentActionAnimation = entityAnimation;
				_currentAnimationId = animationId;
				slotId = 5;
			}
			else
			{
				entityAnimation = GetAnimation(animationId);
				if (entityAnimation == null)
				{
					_gameInstance.App.DevTools.Error("No animation with id " + animationId + " on entity with model " + ModelPacket.Model_);
					return;
				}
			}
			if (animationTime == -1f)
			{
				animationTime = ModelRenderer.GetSlotAnimationTime(slotId);
			}
			ModelRenderer.SetSlotAnimation(slotId, entityAnimation.Data, entityAnimation.Looping, entityAnimation.Speed, animationTime, entityAnimation.BlendingDuration);
			ServerAnimations[slot] = animationId;
			if (storeCurrentAnimationId)
			{
				_currentAnimationId = animationId;
			}
			SetAnimationSound(entityAnimation, slot);
		}
		else
		{
			ModelRenderer.SetSlotAnimation(slotId, null, isLooping: true, 1f, 0f, 12f);
			ServerAnimations[slot] = null;
			SetAnimationSound(null, slot);
		}
	}

	public virtual EntityAnimation GetTargetActionAnimation(InteractionType interactionType)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Invalid comparison between Unknown and I4
		if ((int)interactionType == 1 && SecondaryItem != null)
		{
			EntityAnimation animation = SecondaryItem.GetAnimation("Attack");
			if (animation != null)
			{
				return animation;
			}
			return EntityAnimation.Empty;
		}
		string id = (((int)interactionType == 1) ? "SecondaryAction" : "Attack");
		EntityAnimation entityAnimation = PrimaryItem?.GetAnimation(id);
		if (entityAnimation != null)
		{
			return entityAnimation;
		}
		return EntityAnimation.Empty;
	}

	public virtual void SetActionAnimation(EntityAnimation targetAnimation, float animationTime = 0f, bool holdLastFrame = false, bool force = false)
	{
		if (!CanPlayAnimations() && targetAnimation != EntityAnimation.Empty)
		{
			return;
		}
		if (CurrentActionAnimation != null && _isCurrentActionAnimationHoldingLastFrame && (force || CurrentActionAnimation != targetAnimation) && targetAnimation != EntityAnimation.Empty && !holdLastFrame)
		{
			float slotAnimationTime = ModelRenderer.GetSlotAnimationTime(5);
			_previousActionAnimation = new ResumeActionAnimationData
			{
				EntityAnimation = CurrentActionAnimation,
				EntityModelRendererResumeTime = slotAnimationTime
			};
		}
		if (targetAnimation == EntityAnimation.Empty)
		{
			bool flag = _previousActionAnimation != null;
			_previousActionAnimation = null;
			if (CurrentActionAnimation != null && !_isCurrentActionAnimationHoldingLastFrame && !IsDead() && flag)
			{
				return;
			}
		}
		CurrentActionAnimation = targetAnimation;
		_isCurrentActionAnimationHoldingLastFrame = holdLastFrame;
		BlockyAnimation blockyAnimation = ((_currentAnimationId == "Idle") ? targetAnimation.Data : targetAnimation.MovingData);
		BlockyAnimation slotAnimation = ModelRenderer.GetSlotAnimation(5);
		if (blockyAnimation != slotAnimation || force)
		{
			ModelRenderer.SetSlotAnimation(5, blockyAnimation, targetAnimation.Looping, targetAnimation.Speed, animationTime, targetAnimation.BlendingDuration, null, force);
			if (PrimaryItem != null)
			{
				EntityItems[0].ModelRenderer.SetSlotAnimation(5, blockyAnimation, targetAnimation.Looping, targetAnimation.Speed, animationTime, targetAnimation.BlendingDuration, null, force);
			}
		}
		BlockyAnimation faceData = targetAnimation.FaceData;
		BlockyAnimation slotAnimation2 = ModelRenderer.GetSlotAnimation(7);
		if (faceData != slotAnimation2 || force)
		{
			ModelRenderer.SetSlotAnimation(7, faceData, isLooping: true, 1f, 0f, 0f, null, force);
		}
		SetAnimationSound(targetAnimation, (AnimationSlot)2);
	}

	public virtual void FinishAction()
	{
		if (CanPlayAnimations())
		{
			bool flag = _previousActionAnimation != null;
			CurrentActionAnimation = null;
			ModelRenderer.SetSlotAnimation(5, null, isLooping: true, 1f, 0f, 12f);
			if (PrimaryItem != null)
			{
				EntityItems[0].ModelRenderer.SetSlotAnimation(5, null, isLooping: true, 1f, 0f, 12f);
			}
			ModelRenderer.SetSlotAnimation(7, null);
			ClearCombatSequenceEffects();
			if (flag)
			{
				SetActionAnimation(_previousActionAnimation.EntityAnimation, _previousActionAnimation.EntityModelRendererResumeTime, holdLastFrame: true);
				_previousActionAnimation = null;
			}
		}
	}

	public void SetEmotionAnimation(string animationId)
	{
		if (CanPlayAnimations())
		{
			Emote emote = null;
			emote = _gameInstance.App.CharacterPartStore.Emotes.Find((Emote x) => x.Id == animationId);
			BlockyAnimation blockyAnimation = null;
			if (emote != null)
			{
				blockyAnimation = new BlockyAnimation();
				BlockyAnimationInitializer.Parse(AssetManager.GetBuiltInAsset("Common/" + emote.Animation), _gameInstance.EntityStoreModule.NodeNameManager, ref blockyAnimation);
			}
			ModelRenderer.SetSlotAnimation(7, blockyAnimation, emote != null, 1f, 0f, 12f);
		}
	}

	public void SetDebugAnimation(string animationId, string particleSystemId, string nodeName)
	{
		if (!CanPlayAnimations())
		{
			return;
		}
		EntityAnimation animation = GetAnimation(animationId);
		bool looping = animation.Looping;
		ModelRenderer.SetSlotAnimation(3, animation.Data, looping, 1f, 0f, 12f);
		if (particleSystemId != null)
		{
			ModelParticleSettings modelParticleSettings = new ModelParticleSettings();
			modelParticleSettings.SystemId = particleSystemId;
			if (nodeName != null)
			{
				modelParticleSettings.TargetNodeNameId = _gameInstance.EntityStoreModule.NodeNameManager.GetOrAddNameId(nodeName);
			}
			AttachParticles(_characterModel, _entityParticles, modelParticleSettings, Scale);
		}
	}

	private void SetAnimationSound(EntityAnimation animation, AnimationSlot slot, bool stopPreviousSoundEvent = false)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		ref AudioDevice.SoundEventReference reference = ref _animationSoundEventReferences[slot];
		if (reference.PlaybackId != -1)
		{
			_gameInstance.AudioModule.ActionOnEvent(ref reference, (AkActionOnEventType)((!stopPreviousSoundEvent) ? 3 : 0));
		}
		if (animation != null)
		{
			_gameInstance.AudioModule.PlaySoundEvent(animation.SoundEventIndex, SoundObjectReference, ref reference);
		}
	}

	public void SetIsTangible(bool isTangible)
	{
		_isTangible = isTangible;
	}

	public bool IsTangible()
	{
		return _isTangible;
	}

	public void SetInvulnerable(bool isInvulnerable)
	{
		_isInvulnerable = isInvulnerable;
	}

	public bool IsInvulnerable()
	{
		return _isInvulnerable;
	}

	public void SetUsable(bool usable)
	{
		_usable = usable;
	}

	public bool IsUsable()
	{
		return _usable;
	}

	public bool IsDead(bool nullHealthIsDead = true)
	{
		if (ModelRenderer == null)
		{
			return true;
		}
		if (DefaultEntityStats.Health == -1 || NetworkId < 0)
		{
			return false;
		}
		ClientEntityStatValue entityStat = GetEntityStat(DefaultEntityStats.Health);
		if (entityStat == null)
		{
			return nullHealthIsDead;
		}
		return entityStat.Value <= entityStat.Min;
	}

	private bool CanPlayAnimations()
	{
		return !IsDead(nullHealthIsDead: false);
	}

	private int GetEntitySeed()
	{
		return System.Math.Abs(MathHelper.Hash(NetworkId) ^ MathHelper.Hash(ServerEntitySeed));
	}

	public bool HasFX()
	{
		return _entityParticles.Count + _entityTrails.Count > 0;
	}

	public void UpdateFX()
	{
		for (int i = 0; i < _entityParticles.Count; i++)
		{
			EntityParticle entityParticle = _entityParticles[i];
			entityParticle.ParticleSystemProxy.Visible = ShouldRender;
			entityParticle.ParticleSystemProxy.Position = RenderPosition + Vector3.Transform(ModelRenderer.NodeTransforms[entityParticle.TargetNodeIndex].Position, RenderOrientation) * (1f / 64f) * Scale + Vector3.Transform(entityParticle.PositionOffset, RenderOrientation * ModelRenderer.NodeTransforms[entityParticle.TargetNodeIndex].Orientation);
			entityParticle.ParticleSystemProxy.Rotation = RenderOrientation * ModelRenderer.NodeTransforms[entityParticle.TargetNodeIndex].Orientation * entityParticle.RotationOffset;
		}
		for (int j = 0; j < _entityTrails.Count; j++)
		{
			EntityTrail entityTrail = _entityTrails[j];
			entityTrail.TrailProxy.Visible = ShouldRender;
			entityTrail.TrailProxy.Position = RenderPosition + Vector3.Transform(ModelRenderer.NodeTransforms[entityTrail.TargetNodeIndex].Position, RenderOrientation) * (1f / 64f) * Scale + Vector3.Transform(entityTrail.PositionOffset, RenderOrientation * ModelRenderer.NodeTransforms[entityTrail.TargetNodeIndex].Orientation);
			entityTrail.TrailProxy.Rotation = (entityTrail.FixedRotation ? entityTrail.RotationOffset : (RenderOrientation * ModelRenderer.NodeTransforms[entityTrail.TargetNodeIndex].Orientation * entityTrail.RotationOffset));
		}
		for (int k = 0; k < EntityItems.Count; k++)
		{
			EntityItem entityItem = EntityItems[k];
			if (entityItem.Particles.Count != 0 || entityItem.Trails.Count != 0)
			{
				ref AnimatedRenderer.NodeTransform reference = ref ModelRenderer.NodeTransforms[entityItem.TargetNodeIndex];
				Quaternion quaternion = reference.Orientation * entityItem.RootOrientationOffset;
				for (int l = 0; l < entityItem.Particles.Count; l++)
				{
					EntityParticle entityParticle2 = entityItem.Particles[l];
					ref AnimatedRenderer.NodeTransform reference2 = ref entityItem.ModelRenderer.NodeTransforms[entityParticle2.TargetNodeIndex];
					entityParticle2.ParticleSystemProxy.Position = RenderPosition + Vector3.Transform(reference.Position + Vector3.Transform(reference2.Position * entityItem.Scale + entityItem.RootPositionOffset, quaternion), RenderOrientation) * (1f / 64f) * Scale + Vector3.Transform(entityParticle2.PositionOffset, RenderOrientation * quaternion * reference2.Orientation);
					entityParticle2.ParticleSystemProxy.Rotation = RenderOrientation * quaternion * reference2.Orientation * entityParticle2.RotationOffset;
				}
				for (int m = 0; m < entityItem.Trails.Count; m++)
				{
					EntityTrail entityTrail2 = entityItem.Trails[m];
					ref AnimatedRenderer.NodeTransform reference3 = ref entityItem.ModelRenderer.NodeTransforms[entityTrail2.TargetNodeIndex];
					entityTrail2.TrailProxy.Position = RenderPosition + Vector3.Transform(reference.Position + Vector3.Transform(reference3.Position * entityItem.Scale + entityItem.RootPositionOffset, quaternion), RenderOrientation) * (1f / 64f) * Scale + Vector3.Transform(entityTrail2.PositionOffset, RenderOrientation * quaternion * reference3.Orientation);
					entityTrail2.TrailProxy.Rotation = (entityTrail2.FixedRotation ? entityTrail2.RotationOffset : (RenderOrientation * quaternion * reference3.Orientation * entityTrail2.RotationOffset));
				}
				entityItem.ModelVFX.UpdateAnimation(_gameInstance.FrameTime);
			}
		}
	}

	public void ClearFX()
	{
		for (int i = 0; i < _entityParticles.Count; i++)
		{
			_entityParticles[i].ParticleSystemProxy?.Expire();
		}
		_entityParticles.Clear();
		for (int j = 0; j < _entityTrails.Count; j++)
		{
			if (_entityTrails[j].TrailProxy != null)
			{
				_entityTrails[j].TrailProxy.IsExpired = true;
			}
		}
		_entityTrails.Clear();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void UpdatePosition(float deltaTime, int serverUpdatesPerSecond)
	{
		PositionProgress = System.Math.Min(1f, PositionProgress + deltaTime * (float)serverUpdatesPerSecond);
		_position = Vector3.Lerp(_previousPosition, _nextPosition, PositionProgress);
		RenderPosition = Position;
	}

	public void UpdateEffectsFromServerPacket(EntityEffectUpdate[] changes)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		foreach (EntityEffectUpdate val in changes)
		{
			EffectOp type = val.Type;
			EffectOp val2 = type;
			if ((int)val2 != 0)
			{
				if ((int)val2 != 1)
				{
					throw new ArgumentOutOfRangeException();
				}
				ServerRemoveEffect(val);
			}
			else
			{
				ServerAddEffect(val);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void UpdateBodyOrientation(float deltaTime, int serverUpdatesPerSecond)
	{
		BodyOrientationProgress = System.Math.Min(1f, BodyOrientationProgress + deltaTime * (float)serverUpdatesPerSecond);
		_bodyOrientation = Vector3.LerpAngle(_previousBodyOrientation, _nextBodyOrientation, BodyOrientationProgress);
	}

	public void Update(float deltaTime, float distanceToCamera, bool skipUpdateLogic = false)
	{
		int serverUpdatesPerSecond = _gameInstance.ServerUpdatesPerSecond;
		UpdatePosition(deltaTime, serverUpdatesPerSecond);
		UpdateWithoutPosition(deltaTime, distanceToCamera, skipUpdateLogic);
	}

	public virtual void UpdateWithoutPosition(float deltaTime, float distanceToCamera, bool skipUpdateLogic = false)
	{
		if (ShouldRender && !_wasShouldRender)
		{
			UpdateLight();
		}
		_wasShouldRender = ShouldRender;
		int serverUpdatesPerSecond = _gameInstance.ServerUpdatesPerSecond;
		UpdateBodyOrientation(deltaTime, serverUpdatesPerSecond);
		if (!skipUpdateLogic)
		{
			Vector4 lightColorAtBlockPosition = _gameInstance.MapModule.GetLightColorAtBlockPosition((int)System.Math.Floor(RenderPosition.X), (int)System.Math.Floor(RenderPosition.Y + EyeOffset), (int)System.Math.Floor(RenderPosition.Z));
			BlockLightColor = lightColorAtBlockPosition;
		}
		UpdateEntityEffects(deltaTime);
		UpdateStats();
		if (Type == EntityType.Character)
		{
			UpdateCharacter(deltaTime, distanceToCamera, skipUpdateLogic);
		}
		else if (Type == EntityType.Item)
		{
			if (_itemParticleSystem != null)
			{
				_itemParticleSystem.Position = RenderPosition;
			}
			if (!IsUsable() && !IsLocalEntity)
			{
				ItemAnimationTime += deltaTime;
			}
			if (ModelRenderer != null && ItemBase.DroppedItemAnimation != null)
			{
				UpdateModelRenderer(deltaTime, skipUpdateLogic);
			}
		}
		else if (Type == EntityType.Block)
		{
			UpdateCharacter(deltaTime, distanceToCamera, skipUpdateLogic: true);
		}
	}

	private void UpdateEntityEffects(float deltaTime)
	{
		int num = _serverEffects.FindIndex((ServerEffectEntry v) => DateTime.Now.Ticks / 10000 > v.CreationTime + 1000);
		if (num != -1)
		{
			_serverEffects.RemoveRange(0, num + 1);
		}
		int num2 = 0;
		float elapsedTime = deltaTime * 60f;
		float invServerUpdatesPerSecond = 1f / (float)_gameInstance.ServerUpdatesPerSecond;
		for (int i = 0; i < EntityEffects.Length; i++)
		{
			ref UniqueEntityEffect reference = ref EntityEffects[i];
			if (reference.NeedTick())
			{
				reference.Tick(invServerUpdatesPerSecond, this, deltaTime);
			}
			if (!reference.IsInfinite)
			{
				reference.RemainingDuration -= deltaTime;
				if (reference.RemainingDuration <= 0f)
				{
					RemoveEffect(reference.NetworkEffectIndex);
				}
			}
			if (reference.ModelRenderer == null)
			{
				if (reference.IsExpiring)
				{
					num2++;
				}
				continue;
			}
			reference.ModelRenderer.AdvancePlayback(elapsedTime);
			if (!reference.ModelRenderer.IsSlotPlayingAnimation(0))
			{
				if (reference.IsExpiring)
				{
					reference.ModelRenderer.Dispose();
					reference.ModelRenderer = null;
					num2++;
				}
				else if (reference.IdleAnimation != null)
				{
					reference.ModelRenderer.SetSlotAnimation(0, reference.IdleAnimation.Data, reference.IdleAnimation.Looping, reference.IdleAnimation.Speed, 0f, reference.IdleAnimation.BlendingDuration);
				}
			}
		}
		if (num2 > 0)
		{
			int newSize = EntityEffects.Length - num2;
			ArrayUtils.CompactArray(ref EntityEffects, 0, EntityEffects.Length, delegate(ref UniqueEntityEffect e)
			{
				return e.IsExpiring;
			});
			_effectsOnEntityDirty = true;
			Array.Resize(ref EntityEffects, newSize);
		}
	}

	private void UpdateCharacter(float deltaTime, float distanceToCamera, bool skipUpdateLogic = false)
	{
		if (ServerAnimations[0] == null)
		{
			UpdateMovementAnimation(deltaTime);
		}
		ModelVFX.UpdateAnimation(_gameInstance.FrameTime);
		for (int i = 0; i < EntityItems.Count; i++)
		{
			EntityItems[i].ModelVFX.UpdateAnimation(_gameInstance.FrameTime);
		}
		if (ModelRenderer != null)
		{
			UpdateModelRenderer(deltaTime, skipUpdateLogic);
		}
		UpdateMovementFX(distanceToCamera, skipUpdateLogic);
		if (skipUpdateLogic)
		{
			return;
		}
		ClientEntityStatValue entityStat = GetEntityStat(DefaultEntityStats.Health);
		if (entityStat != null)
		{
			if (entityStat.Value == entityStat.Max && SmoothHealth >= 0.99f)
			{
				SmoothHealth = 1f;
			}
			else
			{
				SmoothHealth = MathHelper.Lerp(SmoothHealth, entityStat.Value / entityStat.Max, 0.35f);
			}
		}
		Hitbox = (GetRelativeMovementStates().IsCrouching ? CrouchHitbox : DefaultHitbox);
	}

	private void UpdateModelRenderer(float deltaTime, bool skipUpdateLogic)
	{
		float elapsedTime = deltaTime * 60f;
		ModelRenderer.AdvancePlayback(elapsedTime);
		for (int i = 0; i < EntityItems.Count; i++)
		{
			EntityItems[i].ModelRenderer.AdvancePlayback(elapsedTime);
		}
		if (!CanPlayAnimations())
		{
			return;
		}
		if (CurrentActionAnimation != null)
		{
			if (!ModelRenderer.IsSlotPlayingAnimation(5) && !_isCurrentActionAnimationHoldingLastFrame)
			{
				FinishAction();
			}
			else
			{
				float slotAnimationTime = ModelRenderer.GetSlotAnimationTime(5);
				SetActionAnimation(CurrentActionAnimation, slotAnimationTime, _isCurrentActionAnimationHoldingLastFrame);
			}
		}
		float blendingDuration = (skipUpdateLogic ? 0f : 12f);
		if (ModelRenderer.GetSlotAnimation(3) != null && !ModelRenderer.IsSlotPlayingAnimation(3))
		{
			ModelRenderer.SetSlotAnimation(3, null, isLooping: true, 1f, 0f, blendingDuration);
		}
		if (ModelRenderer.GetSlotAnimation(4) != null && !ModelRenderer.IsSlotPlayingAnimation(4))
		{
			ModelRenderer.SetSlotAnimation(4, null, isLooping: true, 1f, 0f, blendingDuration);
			ServerAnimations[1] = null;
		}
		if (ModelRenderer.GetSlotAnimation(6) != null && !ModelRenderer.IsSlotPlayingAnimation(6))
		{
			ModelRenderer.SetSlotAnimation(6, null, isLooping: true, 1f, 0f, blendingDuration);
			ServerAnimations[2] = null;
		}
		if (ModelRenderer.GetSlotAnimation(7) != null && !ModelRenderer.IsSlotPlayingAnimation(7))
		{
			ModelRenderer.SetSlotAnimation(7, null, isLooping: true, 1f, 0f, blendingDuration);
			ServerAnimations[3] = null;
		}
		if (ModelRenderer.GetSlotAnimation(8) != null && !ModelRenderer.IsSlotPlayingAnimation(8))
		{
			ModelRenderer.SetSlotAnimation(8, null);
			ServerAnimations[4] = null;
		}
		if (ServerAnimations[0] != null && !ModelRenderer.IsSlotPlayingAnimation(0))
		{
			ServerAnimations[0] = null;
		}
	}

	private void UpdateMovementAnimation(float deltaTime)
	{
		ref ClientMovementStates relativeMovementStates = ref GetRelativeMovementStates();
		if (!relativeMovementStates.IsIdle)
		{
			Vector3 vector = Vector3.Transform(_nextPosition - _previousPosition, Quaternion.Inverse(Quaternion.CreateFromYawPitchRoll(0f, BodyOrientation.Pitch, BodyOrientation.Roll)));
			float num = (float)System.Math.Atan2(0f - vector.X, 0f - vector.Z);
			float num2 = MathHelper.WrapAngle(num - LookOrientation.Yaw);
			bool flag = num2 > (float)System.Math.PI * 2f / 3f || num2 < (float)System.Math.PI * -2f / 3f;
			if (relativeMovementStates.IsFlying)
			{
				string animationId = ((!flag) ? (relativeMovementStates.IsForcedCrouching ? "FlyCrouch" : (relativeMovementStates.IsSprinting ? "FlyFast" : "Fly")) : (relativeMovementStates.IsForcedCrouching ? "FlyCrouchBackward" : "FlyBackward"));
				SetMovementAnimation(animationId);
			}
			else if (relativeMovementStates.IsClimbing)
			{
				float y = vector.Y;
				float num3 = MathHelper.WrapAngle(BodyOrientation.Yaw);
				if (System.Math.Abs(vector.Z) > 0.01f)
				{
					if (num3 > 0f)
					{
						if (vector.Z < 0f)
						{
							SetMovementAnimation("ClimbRight");
						}
						else
						{
							SetMovementAnimation("ClimbLeft");
						}
					}
					else if (vector.Z < 0f)
					{
						SetMovementAnimation("ClimbLeft");
					}
					else
					{
						SetMovementAnimation("ClimbRight");
					}
				}
				else if (System.Math.Abs(vector.X) > 0.01f)
				{
					if (num3 < (float)System.Math.PI / 2f && num3 > -(float)System.Math.PI / 2f)
					{
						if (vector.X < 0f)
						{
							SetMovementAnimation("ClimbLeft");
						}
						else
						{
							SetMovementAnimation("ClimbRight");
						}
					}
					else if (vector.X < 0f)
					{
						SetMovementAnimation("ClimbRight");
					}
					else
					{
						SetMovementAnimation("ClimbLeft");
					}
				}
				else if (y < 0f)
				{
					SetMovementAnimation("ClimbDown");
				}
				else if (y > 0f)
				{
					SetMovementAnimation("ClimbUp");
				}
			}
			else if (relativeMovementStates.IsSwimJumping)
			{
				if (_currentAnimationId != "SwimJump")
				{
					SetMovementAnimation("SwimJump");
				}
			}
			else if (relativeMovementStates.IsSwimming)
			{
				float y2 = vector.Y;
				if (!relativeMovementStates.IsHorizontalIdle)
				{
					if (y2 >= 0f)
					{
						if (relativeMovementStates.IsOnGround)
						{
							if (relativeMovementStates.IsSprinting)
							{
								SetMovementAnimation("FluidRun");
							}
							else if (relativeMovementStates.IsCrouching)
							{
								if (flag)
								{
									SetMovementAnimation("CrouchWalkBackward");
								}
								else
								{
									SetMovementAnimation("CrouchWalk");
								}
							}
							else if (flag)
							{
								SetMovementAnimation("FluidWalkBackward");
							}
							else
							{
								SetMovementAnimation("FluidWalk");
							}
						}
						else if (relativeMovementStates.IsSprinting)
						{
							SetMovementAnimation("SwimFast");
						}
						else if (flag)
						{
							SetMovementAnimation("SwimBackward");
						}
						else
						{
							SetMovementAnimation("Swim");
						}
					}
					else if (relativeMovementStates.IsSprinting)
					{
						SetMovementAnimation("SwimDiveFast");
					}
					else if (flag)
					{
						SetMovementAnimation("SwimDiveBackward");
					}
					else
					{
						SetMovementAnimation("SwimDive");
					}
				}
				else if (y2 < 0f)
				{
					SetMovementAnimation("SwimSink");
				}
				else if (y2 > 0f)
				{
					SetMovementAnimation("SwimFloat");
				}
			}
			else if (relativeMovementStates.IsRolling)
			{
				SetMovementAnimation("SafetyRoll");
			}
			else if (relativeMovementStates.IsSliding)
			{
				SetMovementAnimation("CrouchSlide");
				(this as PlayerEntity)?.SetFpAnimation("CrouchSlide", GetItemAnimation(null, "CrouchSlide"));
			}
			else if (relativeMovementStates.IsCrouching || relativeMovementStates.IsForcedCrouching)
			{
				if (relativeMovementStates.IsJumping || relativeMovementStates.IsFalling)
				{
					SetMovementAnimation("JumpCrouch");
				}
				else
				{
					SetMovementAnimation(flag ? "CrouchWalkBackward" : "CrouchWalk");
				}
			}
			else if (relativeMovementStates.IsJumping)
			{
				if (_currentAnimationId != "Jump" && _currentAnimationId != "JumpWalk" && _currentAnimationId != "JumpRun" && _currentAnimationId != "JumpSprint" && _currentAnimationId != "SwimJump")
				{
					if (relativeMovementStates.IsHorizontalIdle)
					{
						SetMovementAnimation("Jump");
					}
					else if (relativeMovementStates.IsSprinting)
					{
						SetMovementAnimation("JumpSprint");
					}
					else if (relativeMovementStates.IsWalking)
					{
						SetMovementAnimation("JumpWalk");
					}
					else
					{
						SetMovementAnimation("JumpRun");
					}
				}
			}
			else if (relativeMovementStates.IsFalling)
			{
				SetMovementAnimation("Fall");
			}
			else if (relativeMovementStates.IsHorizontalIdle)
			{
				SetMovementAnimation("Idle");
			}
			else if (relativeMovementStates.IsMantling)
			{
				SetMovementAnimation("MantleUp");
				(this as PlayerEntity)?.SetFpAnimation("MantleUp", GetItemAnimation(null, "MantleUp"));
			}
			else
			{
				string animationId2 = ((relativeMovementStates.IsSprinting || (!relativeMovementStates.IsWalking && _gameInstance.CharacterControllerModule.MovementController.SprintForceDurationLeft > 0f)) ? "Sprint" : ((!relativeMovementStates.IsWalking) ? (flag ? "RunBackward" : "Run") : (flag ? "WalkBackward" : "Walk")));
				SetMovementAnimation(animationId2);
			}
		}
		else if (relativeMovementStates.IsFlying)
		{
			SetMovementAnimation(relativeMovementStates.IsForcedCrouching ? "FlyCrouchIdle" : "FlyIdle");
		}
		else if (relativeMovementStates.IsCrouching)
		{
			SetMovementAnimation("Crouch");
		}
		else if (relativeMovementStates.IsClimbing)
		{
			SetMovementAnimation("ClimbIdle");
		}
		else if (relativeMovementStates.IsSwimming)
		{
			SetMovementAnimation(relativeMovementStates.IsOnGround ? "FluidIdle" : "SwimIdle");
		}
		else if (relativeMovementStates.IsMounting)
		{
			SetMovementAnimation("Mount");
		}
		else
		{
			SetMovementAnimation("Idle");
		}
		_currentAnimationRunTime += deltaTime;
		if (_currentAnimationRunTime >= StaticRandom.NextFloat(GetEntitySeed(), 2f, 10f))
		{
			SetMovementAnimation(_currentAnimationId, -1f, force: true);
		}
		if (ModelRenderer != null)
		{
			UpdatePassiveAnimation(deltaTime);
		}
	}

	private void UpdatePassiveAnimation(float deltaTime)
	{
		if (ModelRenderer.IsSlotPlayingAnimation(3))
		{
			return;
		}
		if (_passiveAnimation != null)
		{
			if (_countPassiveAnimation > 0)
			{
				PlayPassiveAnimation();
				return;
			}
			_passiveAnimation = null;
		}
		_nextPassiveAnimationTimer += deltaTime;
		if (_nextPassiveAnimationTimer < _nextPassiveAnimationThreshold)
		{
			return;
		}
		if (!TryGetTargetPassiveAnimation(out var targetAnimationSet))
		{
			ClearPassiveAnimationData();
			return;
		}
		if (_nextPassiveAnimationThreshold > 0f)
		{
			SetPassiveAnimation(targetAnimationSet.Id);
			PlayPassiveAnimation();
		}
		_nextPassiveAnimationThreshold = StaticRandom.NextFloat(GetEntitySeed(), targetAnimationSet.PassiveNextDelay.Min, targetAnimationSet.PassiveNextDelay.Max);
		_nextPassiveAnimationTimer = 0f;
	}

	public void ClearPassiveAnimationData()
	{
		_nextPassiveAnimationTimer = 0f;
		_nextPassiveAnimationThreshold = 0f;
		_passiveAnimation = null;
		if (ModelRenderer.GetSlotAnimation(3) != null)
		{
			ModelRenderer.SetSlotAnimation(3, null, isLooping: true, 1f, 0f, 12f);
		}
	}

	private void SetPassiveAnimation(string targetAnimationSetId)
	{
		EntityAnimation animation = GetAnimation(targetAnimationSetId);
		_countPassiveAnimation = animation.PassiveLoopCount;
		_passiveAnimation = animation;
	}

	private void PlayPassiveAnimation()
	{
		ModelRenderer.SetSlotAnimation(3, _passiveAnimation.Data, isLooping: false, _passiveAnimation.Speed, 0f, _passiveAnimation.BlendingDuration);
		_countPassiveAnimation--;
	}

	private bool TryGetTargetPassiveAnimation(out ClientAnimationSet targetAnimationSet)
	{
		if (!_animationSets.TryGetValue(_currentAnimationId + "Passive", out var value))
		{
			targetAnimationSet = null;
			return false;
		}
		if (value.PassiveNextDelay == null)
		{
			targetAnimationSet = null;
			return false;
		}
		for (int i = 4; i < 9; i++)
		{
			if (ModelRenderer.GetSlotAnimation(i) != null)
			{
				targetAnimationSet = null;
				return false;
			}
		}
		targetAnimationSet = value;
		return true;
	}

	private void UpdateMovementFX(float distanceToCamera, bool skipUpdateLogic)
	{
		ref ClientMovementStates relativeMovementStates = ref GetRelativeMovementStates();
		bool flag = relativeMovementStates.IsInFluid;
		if (!skipUpdateLogic && distanceToCamera < 20f)
		{
			if (!_wasInFluid && flag)
			{
				Vector3 nextPosition = _nextPosition;
				int num = _gameInstance.MapModule.GetBlock(nextPosition, 0);
				ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[num];
				if (num == 0)
				{
					flag = false;
				}
				else if (clientBlockType.FluidBlockId != num)
				{
					num = clientBlockType.FluidBlockId;
					clientBlockType = _gameInstance.MapModule.ClientBlockTypes[num];
				}
				if ((!relativeMovementStates.IsOnGround || clientBlockType.VerticalFill > 3) && clientBlockType.BlockParticleSetId != null)
				{
					int num2 = 0;
					ClientBlockParticleEvent particleEvent = ((_fallHeight - nextPosition.Y > 3f) ? ClientBlockParticleEvent.HardLand : ClientBlockParticleEvent.SoftLand);
					num2 = 1;
					nextPosition.Y = (float)System.Math.Floor(nextPosition.Y);
					for (; (float)num2 < Hitbox.Max.Y + 1f; num2++)
					{
						int block = _gameInstance.MapModule.GetBlock((int)System.Math.Floor(nextPosition.X), (int)nextPosition.Y + num2, (int)System.Math.Floor(nextPosition.Z), 0);
						if (block == 0)
						{
							break;
						}
						ClientBlockType clientBlockType2 = _gameInstance.MapModule.ClientBlockTypes[block];
						if (num != block)
						{
							if (clientBlockType2.FluidBlockId == 0)
							{
								break;
							}
							if (clientBlockType2.FluidBlockId != num)
							{
								num = clientBlockType2.FluidBlockId;
								clientBlockType = _gameInstance.MapModule.ClientBlockTypes[num];
							}
						}
					}
					if ((float)(num2 - 1) <= Hitbox.Max.Y && _gameInstance.ParticleSystemStoreModule.TrySpawnBlockSystem(nextPosition, clientBlockType, particleEvent, out var particleSystemProxy))
					{
						byte b = (byte)((clientBlockType.MaxFillLevel == 0) ? 8 : clientBlockType.MaxFillLevel);
						nextPosition.Y += num2 - (1 - clientBlockType.VerticalFill / b);
						particleSystemProxy.Position = nextPosition;
						particleSystemProxy.Rotation = RenderOrientation;
					}
				}
			}
			else if (!_wasOnGround && relativeMovementStates.IsOnGround && !flag)
			{
				Vector3 nextPosition2 = _nextPosition;
				nextPosition2.Y -= 0.01f;
				float num3 = _fallHeight - nextPosition2.Y;
				if (num3 > 0.5f)
				{
					ClientBlockParticleEvent particleEvent2 = ((num3 > 3f) ? ClientBlockParticleEvent.HardLand : ClientBlockParticleEvent.SoftLand);
					int block2 = _gameInstance.MapModule.GetBlock(nextPosition2, 0);
					ClientBlockType clientBlockType3 = _gameInstance.MapModule.ClientBlockTypes[block2];
					if (clientBlockType3.BlockParticleSetId != null && clientBlockType3.FluidBlockId == 0 && _gameInstance.ParticleSystemStoreModule.TrySpawnBlockSystem(nextPosition2, clientBlockType3, particleEvent2, out var particleSystemProxy2))
					{
						particleSystemProxy2.Position = nextPosition2;
						particleSystemProxy2.Rotation = RenderOrientation;
					}
				}
			}
			Vector3 renderPosition = RenderPosition;
			int num4 = _gameInstance.MapModule.GetBlock(renderPosition, 1);
			ClientBlockType clientBlockType4 = _gameInstance.MapModule.ClientBlockTypes[num4];
			if (clientBlockType4.FluidBlockId != 0)
			{
				num4 = clientBlockType4.FluidBlockId;
				clientBlockType4 = _gameInstance.MapModule.ClientBlockTypes[num4];
			}
			renderPosition.Y = (float)System.Math.Floor(renderPosition.Y);
			if (num4 != _previousBlockId)
			{
				if ((float)_previousBlockY == renderPosition.Y)
				{
					renderPosition.Y += (float)(int)clientBlockType4.VerticalFill * (1f / (float)(int)clientBlockType4.MaxFillLevel);
				}
				else
				{
					renderPosition.Y -= 1f - (float)(int)clientBlockType4.VerticalFill * (1f / (float)(int)clientBlockType4.MaxFillLevel);
				}
				ClientBlockType clientBlockType5 = _gameInstance.MapModule.ClientBlockTypes[_previousBlockId];
				if (clientBlockType5.BlockParticleSetId != null && _gameInstance.ParticleSystemStoreModule.TrySpawnBlockSystem(renderPosition, clientBlockType5, ClientBlockParticleEvent.MoveOut, out var particleSystemProxy3))
				{
					particleSystemProxy3.Position = renderPosition;
					particleSystemProxy3.Rotation = RenderOrientation;
				}
				_previousBlockId = num4;
			}
			_previousBlockY = (int)renderPosition.Y;
			if (relativeMovementStates.IsSprinting)
			{
				Vector3 renderPosition2 = RenderPosition;
				if (relativeMovementStates.IsOnGround && !flag)
				{
					renderPosition2.Y -= 0.01f;
				}
				int num5 = _gameInstance.MapModule.GetBlock(renderPosition2, 0);
				ClientBlockType clientBlockType6 = _gameInstance.MapModule.ClientBlockTypes[num5];
				int i = 0;
				if (flag)
				{
					if (clientBlockType6.FluidBlockId != num5)
					{
						num5 = clientBlockType6.FluidBlockId;
						clientBlockType6 = _gameInstance.MapModule.ClientBlockTypes[num5];
					}
					i = 1;
					renderPosition2.Y = (float)System.Math.Floor(renderPosition2.Y);
					for (; (float)i < Hitbox.Max.Y + 1f; i++)
					{
						int block3 = _gameInstance.MapModule.GetBlock((int)System.Math.Floor(renderPosition2.X), (int)renderPosition2.Y + i, (int)System.Math.Floor(renderPosition2.Z), 0);
						if (block3 == 0)
						{
							break;
						}
						ClientBlockType clientBlockType7 = _gameInstance.MapModule.ClientBlockTypes[block3];
						if (num5 != block3)
						{
							if (clientBlockType7.FluidBlockId == 0)
							{
								break;
							}
							if (clientBlockType7.FluidBlockId != num5)
							{
								num5 = clientBlockType7.FluidBlockId;
								clientBlockType6 = _gameInstance.MapModule.ClientBlockTypes[num5];
							}
						}
					}
					renderPosition2.Y += (float)i - (1f - (float)(int)clientBlockType6.VerticalFill * (1f / (float)(int)clientBlockType6.MaxFillLevel));
				}
				bool flag2 = (float)(i - 1) > Hitbox.Max.Y;
				if ((_previousSprintBlockId != num5 || _sprintParticleSystem == null) && !flag2)
				{
					if (_sprintParticleSystem != null)
					{
						_sprintParticleSystem.Expire();
						_sprintParticleSystem = null;
					}
					if (clientBlockType6.BlockParticleSetId != null)
					{
						_gameInstance.ParticleSystemStoreModule.TrySpawnBlockSystem(renderPosition2, clientBlockType6, ClientBlockParticleEvent.Sprint, out _sprintParticleSystem, faceCameraYaw: false, isTracked: true);
					}
					_previousSprintBlockId = num5;
				}
				if (_sprintParticleSystem != null)
				{
					if (flag2)
					{
						_sprintParticleSystem.Expire();
						_sprintParticleSystem = null;
					}
					else
					{
						_sprintParticleSystem.Position = renderPosition2;
						_sprintParticleSystem.Rotation = RenderOrientation;
					}
				}
			}
			else if (_sprintParticleSystem != null)
			{
				_sprintParticleSystem.Expire();
				_sprintParticleSystem = null;
			}
			if (relativeMovementStates.IsWalking)
			{
				Vector3 renderPosition3 = RenderPosition;
				if (relativeMovementStates.IsOnGround && !flag)
				{
					renderPosition3.Y -= 0.01f;
				}
				int num6 = _gameInstance.MapModule.GetBlock(renderPosition3, 0);
				ClientBlockType clientBlockType8 = _gameInstance.MapModule.ClientBlockTypes[num6];
				int j = 0;
				if (flag)
				{
					if (clientBlockType8.FluidBlockId != num6)
					{
						num6 = clientBlockType8.FluidBlockId;
						clientBlockType8 = _gameInstance.MapModule.ClientBlockTypes[num6];
					}
					j = 1;
					renderPosition3.Y = (float)System.Math.Floor(renderPosition3.Y);
					for (; (float)j < Hitbox.Max.Y + 1f; j++)
					{
						int block4 = _gameInstance.MapModule.GetBlock((int)System.Math.Floor(renderPosition3.X), (int)renderPosition3.Y + j, (int)System.Math.Floor(renderPosition3.Z), 0);
						if (block4 == 0)
						{
							break;
						}
						ClientBlockType clientBlockType9 = _gameInstance.MapModule.ClientBlockTypes[block4];
						if (num6 != block4)
						{
							if (clientBlockType9.FluidBlockId == 0)
							{
								break;
							}
							if (clientBlockType9.FluidBlockId != num6)
							{
								num6 = clientBlockType9.FluidBlockId;
								clientBlockType8 = _gameInstance.MapModule.ClientBlockTypes[num6];
							}
						}
					}
					renderPosition3.Y += (float)j - (1f - (float)(int)clientBlockType8.VerticalFill * (1f / (float)(int)clientBlockType8.MaxFillLevel));
				}
				bool flag3 = (float)(j - 1) > Hitbox.Max.Y;
				if ((_previousWalkBlockId != num6 || _walkParticleSystem == null) && !flag3)
				{
					if (_walkParticleSystem != null)
					{
						_walkParticleSystem.Expire();
						_walkParticleSystem = null;
					}
					if (clientBlockType8.BlockParticleSetId != null)
					{
						_gameInstance.ParticleSystemStoreModule.TrySpawnBlockSystem(renderPosition3, clientBlockType8, ClientBlockParticleEvent.Walk, out _walkParticleSystem, faceCameraYaw: false, isTracked: true);
					}
					_previousWalkBlockId = num6;
				}
				if (_walkParticleSystem != null)
				{
					if (flag3)
					{
						_walkParticleSystem.Expire();
						_walkParticleSystem = null;
					}
					else
					{
						_walkParticleSystem.Position = renderPosition3;
						_walkParticleSystem.Rotation = RenderOrientation;
					}
				}
			}
			else if (_walkParticleSystem != null)
			{
				_walkParticleSystem.Expire();
				_walkParticleSystem = null;
			}
			if (!relativeMovementStates.IsSprinting && !relativeMovementStates.IsWalking && !relativeMovementStates.IsCrouching && !relativeMovementStates.IsIdle && !relativeMovementStates.IsFalling)
			{
				Vector3 renderPosition4 = RenderPosition;
				if (relativeMovementStates.IsOnGround && !flag)
				{
					renderPosition4.Y -= 0.01f;
				}
				int num7 = _gameInstance.MapModule.GetBlock(renderPosition4, 1);
				ClientBlockType clientBlockType10 = _gameInstance.MapModule.ClientBlockTypes[num7];
				int k = 0;
				if (flag)
				{
					if (clientBlockType10.FluidBlockId != 0)
					{
						num7 = clientBlockType10.FluidBlockId;
						clientBlockType10 = _gameInstance.MapModule.ClientBlockTypes[num7];
					}
					k = 1;
					renderPosition4.Y = (float)System.Math.Floor(renderPosition4.Y);
					for (; (float)k < Hitbox.Max.Y + 1f; k++)
					{
						int block5 = _gameInstance.MapModule.GetBlock((int)System.Math.Floor(renderPosition4.X), (int)renderPosition4.Y + k, (int)System.Math.Floor(renderPosition4.Z), 0);
						if (block5 == 0)
						{
							break;
						}
						if (num7 != block5)
						{
							ClientBlockType clientBlockType11 = _gameInstance.MapModule.ClientBlockTypes[block5];
							if (clientBlockType11.FluidBlockId == 0)
							{
								break;
							}
							if (clientBlockType11.FluidBlockId != num7)
							{
								num7 = clientBlockType11.FluidBlockId;
								clientBlockType10 = _gameInstance.MapModule.ClientBlockTypes[num7];
							}
						}
					}
					renderPosition4.Y += (float)k - (1f - (float)(int)clientBlockType10.VerticalFill * (1f / (float)(int)clientBlockType10.MaxFillLevel));
				}
				bool flag4 = (float)(k - 1) > Hitbox.Max.Y;
				if ((_previousRunBlockId != num7 || _runParticleSystem == null) && !flag4)
				{
					if (_runParticleSystem != null)
					{
						_runParticleSystem.Expire();
						_runParticleSystem = null;
					}
					if (clientBlockType10.BlockParticleSetId != null)
					{
						_gameInstance.ParticleSystemStoreModule.TrySpawnBlockSystem(renderPosition4, clientBlockType10, ClientBlockParticleEvent.Run, out _runParticleSystem, faceCameraYaw: false, isTracked: true);
					}
					_previousRunBlockId = num7;
				}
				if (_runParticleSystem != null)
				{
					if (flag4)
					{
						_runParticleSystem.Expire();
						_runParticleSystem = null;
					}
					else
					{
						_runParticleSystem.Position = renderPosition4;
						_runParticleSystem.Rotation = RenderOrientation;
					}
				}
			}
			else if (_runParticleSystem != null)
			{
				_runParticleSystem.Expire();
				_runParticleSystem = null;
			}
			if (relativeMovementStates.IsFalling && !_wasFalling)
			{
				_fallHeight = _nextPosition.Y;
			}
			else if (relativeMovementStates.IsJumping)
			{
				if (!_wasJumping)
				{
					_fallHeight = 0f;
				}
				_fallHeight = System.Math.Max(_fallHeight, _nextPosition.Y);
			}
		}
		else
		{
			ResetMovementParticleSystems();
		}
		_wasInFluid = flag;
		_wasOnGround = relativeMovementStates.IsOnGround;
		_wasFalling = relativeMovementStates.IsFalling;
		_wasJumping = relativeMovementStates.IsJumping;
	}

	public void ResetMovementParticleSystems()
	{
		if (_walkParticleSystem != null)
		{
			_walkParticleSystem.Expire(instant: true);
			_walkParticleSystem = null;
			_previousWalkBlockId = -1;
		}
		if (_runParticleSystem != null)
		{
			_runParticleSystem.Expire(instant: true);
			_runParticleSystem = null;
			_previousRunBlockId = -1;
		}
		if (_sprintParticleSystem != null)
		{
			_sprintParticleSystem.Expire(instant: true);
			_sprintParticleSystem = null;
			_previousSprintBlockId = -1;
		}
	}

	public virtual ref ClientMovementStates GetRelativeMovementStates()
	{
		return ref ServerMovementStates;
	}

	public EntityAnimation GetAnimation(string id)
	{
		if (_animationSets.TryGetValue(id, out var value))
		{
			if (JumpAnimations.Contains(id))
			{
				return value.Animations[_currentJumpAnimation];
			}
			return value.GetWeightedAnimation(GetEntitySeed());
		}
		return null;
	}

	public List<string> GetAnimationList(AnimationSlot slot)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Invalid comparison between Unknown and I4
		List<string> animations = new List<string>();
		if ((int)slot > 2)
		{
			if (slot - 3 <= 1)
			{
				_gameInstance.App.CharacterPartStore.Emotes.ForEach(delegate(Emote k)
				{
					animations.Add(k.Id);
				});
			}
		}
		else
		{
			animations.AddRange(_animationSets.Keys);
		}
		return animations;
	}

	private Animation GetWeightedAnimation(Animation[] animations)
	{
		if (animations.Length == 1)
		{
			return animations[0];
		}
		float num = 0f;
		foreach (Animation val in animations)
		{
			num += val.Weight;
		}
		if (num == 0f)
		{
			return animations[GetEntitySeed() % animations.Length];
		}
		float num2 = StaticRandom.NextFloat(GetEntitySeed(), 0f, num);
		Animation result = null;
		foreach (Animation val2 in animations)
		{
			result = val2;
			num2 -= val2.Weight;
			if (num2 <= 0f)
			{
				break;
			}
		}
		return result;
	}

	private bool DoFirstPersonParticles()
	{
		return NetworkId == _gameInstance.LocalPlayerNetworkId && _gameInstance.CameraModule.Controller.IsFirstPerson;
	}

	public void UpdateInterpolation(float timeFraction)
	{
		RenderPosition = Vector3.Lerp(_previousPosition, _nextPosition, timeFraction) + new Vector3(0f, _gameInstance.CharacterControllerModule.MovementController.AutoJumpHeightShift, 0f);
	}

	public void UpdateEntityStats(Dictionary<int, EntityStatUpdate[]> updates)
	{
		foreach (KeyValuePair<int, EntityStatUpdate[]> update2 in updates)
		{
			int key = update2.Key;
			float? previousValue = _entityStats[key]?.Value;
			for (int i = 0; i < update2.Value.Length; i++)
			{
				EntityStatUpdate update = update2.Value[i];
				TryAddServerStat(key, update);
			}
			ClientEntityStatValue clientEntityStatValue = _entityStats[key];
			if (clientEntityStatValue != null)
			{
				EntityStatUpdated(key, previousValue, clientEntityStatValue);
			}
		}
	}

	private void UpdateStats()
	{
		int num = _serverStats.FindIndex((TimedStatUpdate v) => DateTime.Now.Ticks / 10000 > v.CreationTime + _gameInstance.TimeModule.StatTimeoutThreshold);
		if (num != -1)
		{
			_serverStats.RemoveRange(0, num + 1);
		}
		num = _predictedStats.FindIndex((TimedStatUpdate v) => DateTime.Now.Ticks / 10000 > v.CreationTime + _gameInstance.TimeModule.StatTimeoutThreshold);
		if (num == -1)
		{
			return;
		}
		HashSet<int> hashSet = new HashSet<int>();
		for (int i = 0; i <= num; i++)
		{
			Logger.Warn($"Removing mis-prediction: {_predictedStats[i].Update}");
			hashSet.Add(_predictedStats[i].Index);
		}
		_predictedStats.RemoveRange(0, num + 1);
		foreach (int item in hashSet)
		{
			float value = _entityStats[item].Value;
			ReapplyPredictions(item);
			EntityStatUpdated(item, value, _entityStats[item]);
		}
	}

	public bool GetStatModifier(int index, string key, out Modifier modifier)
	{
		modifier = null;
		ClientEntityStatValue entityStat = GetEntityStat(index);
		if (entityStat == null)
		{
			Logger.Warn($"No EntityStatValue found for index: {index}");
			return false;
		}
		return entityStat.Modifiers?.TryGetValue(key, out modifier) ?? false;
	}

	public EntityStatUpdate PutStatModifier(int index, string key, Modifier modifier)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		ClientEntityStatValue entityStat = GetEntityStat(index);
		if (entityStat == null)
		{
			Logger.Warn($"No EntityStatValue found for index: {index}");
			return null;
		}
		EntityStatUpdate val = new EntityStatUpdate((EntityStatOp)2, true, 0f, (Dictionary<string, Modifier>)null, key, modifier);
		TryAddPredictedStat(index, val);
		return val;
	}

	public bool RemoveModifier(int index, string key, out EntityStatUpdate update)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		ClientEntityStatValue entityStat = GetEntityStat(index);
		if (entityStat == null)
		{
			Logger.Warn($"No EntityStatValue found for index: {index}");
			update = null;
			return false;
		}
		Dictionary<string, Modifier> modifiers = entityStat.Modifiers;
		if (modifiers == null || !modifiers.ContainsKey(key))
		{
			update = null;
			return false;
		}
		update = new EntityStatUpdate((EntityStatOp)3, true, 0f, (Dictionary<string, Modifier>)null, key, (Modifier)null);
		TryAddPredictedStat(index, update);
		return true;
	}

	public EntityStatUpdate SetStatValue(int index, float newValue)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		ClientEntityStatValue entityStat = GetEntityStat(index);
		if (entityStat == null)
		{
			Logger.Warn($"No EntityStatValue found for index: {index}");
			return null;
		}
		EntityStatUpdate val = new EntityStatUpdate((EntityStatOp)5, true, newValue, (Dictionary<string, Modifier>)null, (string)null, (Modifier)null);
		TryAddPredictedStat(index, val);
		return val;
	}

	public EntityStatUpdate AddStatValue(int index, float amount)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		ClientEntityStatValue entityStat = GetEntityStat(index);
		if (entityStat == null)
		{
			Logger.Warn($"No EntityStatValue found for index: {index}");
			return null;
		}
		EntityStatUpdate val = new EntityStatUpdate((EntityStatOp)4, true, amount, (Dictionary<string, Modifier>)null, (string)null, (Modifier)null);
		TryAddPredictedStat(index, val);
		return val;
	}

	public EntityStatUpdate SubtractStatValue(int index, float amount)
	{
		return AddStatValue(index, 0f - amount);
	}

	public EntityStatUpdate MinimizeStatValue(int index)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		ClientEntityStatValue entityStat = GetEntityStat(index);
		if (entityStat == null)
		{
			Logger.Warn($"No EntityStatValue found for index: {index}");
			return null;
		}
		EntityStatUpdate val = new EntityStatUpdate((EntityStatOp)6, true, 0f, (Dictionary<string, Modifier>)null, (string)null, (Modifier)null);
		TryAddPredictedStat(index, val);
		return val;
	}

	public EntityStatUpdate MaximizeStatValue(int index)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		ClientEntityStatValue entityStat = GetEntityStat(index);
		if (entityStat == null)
		{
			Logger.Warn($"No EntityStatValue found for index: {index}");
			return null;
		}
		EntityStatUpdate val = new EntityStatUpdate((EntityStatOp)7, true, 0f, (Dictionary<string, Modifier>)null, (string)null, (Modifier)null);
		TryAddPredictedStat(index, val);
		return val;
	}

	public EntityStatUpdate ResetStatValue(int index)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		ClientEntityStatValue entityStat = GetEntityStat(index);
		if (entityStat == null)
		{
			Logger.Warn($"No EntityStatValue found for index: {index}");
			return null;
		}
		EntityStatUpdate val = new EntityStatUpdate((EntityStatOp)8, true, 0f, (Dictionary<string, Modifier>)null, (string)null, (Modifier)null);
		TryAddPredictedStat(index, val);
		return val;
	}

	public void CancelStatPrediction(int index, EntityStatUpdate update)
	{
		float? previousValue = _entityStats[index]?.Value;
		for (int i = 0; i < _predictedStats.Count; i++)
		{
			TimedStatUpdate timedStatUpdate = _predictedStats[i];
			if (timedStatUpdate.Index == index && ((object)timedStatUpdate.Update).Equals((object?)update))
			{
				_predictedStats.RemoveAt(i);
				ReapplyPredictions(index);
				break;
			}
		}
		ClientEntityStatValue clientEntityStatValue = _entityStats[index];
		if (clientEntityStatValue != null)
		{
			EntityStatUpdated(index, previousValue, clientEntityStatValue);
		}
	}

	private void ApplyStatUpdate(ClientEntityStatValue[] stats, int index, EntityStatUpdate update)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I4
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected I4, but got Unknown
		ClientEntityStatValue clientEntityStatValue = stats[index];
		ClientEntityStatType statType = _gameInstance.ServerSettings.EntityStatTypes[index];
		if (clientEntityStatValue == null && (int)update.Op > 0)
		{
			Logger.Error($"Attempted to access null entity stat {index}");
			return;
		}
		EntityStatOp op = update.Op;
		EntityStatOp val = op;
		switch ((int)val)
		{
		case 0:
		{
			ClientEntityStatValue obj = new ClientEntityStatValue
			{
				Min = statType.Min,
				Max = statType.Max,
				Value = update.Value,
				Modifiers = update.Modifiers
			};
			ClientEntityStatValue clientEntityStatValue2 = obj;
			stats[index] = obj;
			clientEntityStatValue = clientEntityStatValue2;
			clientEntityStatValue.CalculateModifiers(statType);
			clientEntityStatValue.Value = update.Value;
			break;
		}
		case 1:
			stats[index] = null;
			break;
		case 5:
			clientEntityStatValue.Value = update.Value;
			break;
		case 4:
			clientEntityStatValue.Value += update.Value;
			break;
		case 6:
			clientEntityStatValue.Value = clientEntityStatValue.Min;
			break;
		case 7:
			clientEntityStatValue.Value = clientEntityStatValue.Max;
			break;
		case 8:
			clientEntityStatValue.Value = statType.Value;
			break;
		case 2:
			clientEntityStatValue.Modifiers = clientEntityStatValue.Modifiers ?? new Dictionary<string, Modifier>();
			clientEntityStatValue.Modifiers[update.ModifierKey] = update.Modifier_;
			clientEntityStatValue.CalculateModifiers(statType);
			break;
		case 3:
			clientEntityStatValue.Modifiers?.Remove(update.ModifierKey);
			clientEntityStatValue.CalculateModifiers(statType);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private void TryAddPredictedStat(int index, EntityStatUpdate update)
	{
		for (int i = 0; i < _serverStats.Count; i++)
		{
			TimedStatUpdate timedStatUpdate = _serverStats[i];
			if (timedStatUpdate.Index == index && ((object)timedStatUpdate.Update).Equals((object?)update))
			{
				_serverStats.RemoveAt(i);
				return;
			}
		}
		float? previousValue = _entityStats[index]?.Value;
		ApplyStatUpdate(_entityStats, index, update);
		_predictedStats.Add(new TimedStatUpdate
		{
			CreationTime = DateTime.Now.Ticks / 10000,
			Index = index,
			Update = update
		});
		ClientEntityStatValue clientEntityStatValue = _entityStats[index];
		if (clientEntityStatValue != null)
		{
			EntityStatUpdated(index, previousValue, clientEntityStatValue);
		}
	}

	private void TryAddServerStat(int index, EntityStatUpdate update)
	{
		ApplyStatUpdate(_serverEntityStats, index, update);
		if (update.Predictable)
		{
			for (int i = 0; i < _predictedStats.Count; i++)
			{
				TimedStatUpdate timedStatUpdate = _predictedStats[i];
				if (timedStatUpdate.Index == index && ((object)timedStatUpdate.Update).Equals((object?)update))
				{
					_predictedStats.RemoveAt(i);
					ReapplyPredictions(index);
					return;
				}
			}
		}
		ReapplyPredictions(index);
		if (update.Predictable)
		{
			_serverStats.Add(new TimedStatUpdate
			{
				CreationTime = DateTime.Now.Ticks / 10000,
				Index = index,
				Update = update
			});
		}
	}

	private void ReapplyPredictions(int index)
	{
		ClientEntityStatValue clientEntityStatValue = _entityStats[index];
		ClientEntityStatValue clientEntityStatValue2 = _serverEntityStats[index];
		if (clientEntityStatValue == null && clientEntityStatValue2 != null)
		{
			clientEntityStatValue = (_entityStats[index] = new ClientEntityStatValue());
		}
		else if (clientEntityStatValue != null && clientEntityStatValue2 == null)
		{
			_entityStats[index] = null;
			return;
		}
		clientEntityStatValue.Modifiers?.Clear();
		if (clientEntityStatValue.Modifiers == null && clientEntityStatValue2.Modifiers != null)
		{
			clientEntityStatValue.Modifiers = new Dictionary<string, Modifier>();
		}
		if (clientEntityStatValue2.Modifiers != null)
		{
			foreach (KeyValuePair<string, Modifier> modifier in clientEntityStatValue2.Modifiers)
			{
				clientEntityStatValue.Modifiers.Add(modifier.Key, modifier.Value);
			}
		}
		clientEntityStatValue.Max = clientEntityStatValue2.Max;
		clientEntityStatValue.Min = clientEntityStatValue2.Min;
		clientEntityStatValue.Value = clientEntityStatValue2.Value;
		for (int i = 0; i < _predictedStats.Count; i++)
		{
			TimedStatUpdate timedStatUpdate = _predictedStats[i];
			if (timedStatUpdate.Index == index)
			{
				ApplyStatUpdate(_entityStats, index, timedStatUpdate.Update);
			}
		}
	}

	private void EntityStatUpdated(int entityStatIndex, float? previousValue, ClientEntityStatValue value)
	{
		if (entityStatIndex == DefaultEntityStats.Health)
		{
			float smoothHealth = value.Value / value.Max;
			if (value.Value <= value.Min || SmoothHealth == -1f)
			{
				SmoothHealth = smoothHealth;
			}
		}
		HandleItemConditionAppearanceForEntityStat(PrimaryItem, 0, entityStatIndex);
		HandleItemConditionAppearanceForEntityStat(SecondaryItem, (PrimaryItem != null) ? 1 : 0, entityStatIndex);
		if (NetworkId != _gameInstance.LocalPlayerNetworkId)
		{
			return;
		}
		ClientEntityStatType clientEntityStatType = _gameInstance.ServerSettings.EntityStatTypes[entityStatIndex];
		if (TestMaxValueEffects(value, previousValue, clientEntityStatType.MaxValueEffects))
		{
			RunEntityStatEffects(clientEntityStatType.MaxValueEffects);
		}
		if (TestMinValueEffects(value, previousValue, clientEntityStatType.MinValueEffects))
		{
			RunEntityStatEffects(clientEntityStatType.MinValueEffects);
		}
		if (entityStatIndex == DefaultEntityStats.Health && previousValue.HasValue)
		{
			if (previousValue <= value.Min)
			{
				for (int i = 0; i < 9; i++)
				{
					ModelRenderer.SetSlotAnimationNoBlending(i, null);
				}
				SetMovementAnimation("Idle", 0f, force: true, noBlending: true);
			}
			else if (value.Value < previousValue && value.Max > value.Value)
			{
				float alpha = MathHelper.Min(0.1f + (previousValue - value.Value).Value / value.Max * 2f, 1f);
				_gameInstance.DamageEffectModule.IncreaseDamageEffect(alpha);
			}
			if (IsDead())
			{
				_gameInstance.App.Interface.InGameView.Wielding = false;
			}
		}
		_gameInstance.App.Interface.InGameView.OnStatChanged(entityStatIndex, value, previousValue);
	}

	private void RunEntityStatEffects(EntityStatEffects effects)
	{
		uint networkWwiseId = ResourceManager.GetNetworkWwiseId(effects.SoundEventIndex);
		_gameInstance.AudioModule.PlayLocalSoundEvent(networkWwiseId);
		if (effects.Particles != null && effects.Particles.Length != 0)
		{
			ModelParticle[] particles = effects.Particles;
			foreach (ModelParticle networkParticle in particles)
			{
				ModelParticleSettings clientModelParticle = new ModelParticleSettings();
				ParticleProtocolInitializer.Initialize(networkParticle, ref clientModelParticle, _gameInstance.EntityStoreModule.NodeNameManager);
				AttachParticles(_characterModel, _entityParticles, clientModelParticle, Scale);
			}
		}
	}

	private void HandleItemConditionAppearanceForAllEntityStats(ClientItemBase item, int itemEntityIndex)
	{
		for (int i = 0; i < _entityStats.Length && !HandleItemConditionAppearanceForEntityStat(item, itemEntityIndex, i); i++)
		{
		}
	}

	private bool HandleItemConditionAppearanceForEntityStat(ClientItemBase item, int itemEntityIndex, int entityStatIndex)
	{
		Dictionary<int, ClientItemAppearanceCondition[]> dictionary = item?.ItemAppearanceConditions;
		if (dictionary == null || !dictionary.TryGetValue(entityStatIndex, out var value))
		{
			return false;
		}
		ClientEntityStatValue clientEntityStatValue = _entityStats[entityStatIndex];
		if (clientEntityStatValue == null)
		{
			return false;
		}
		EntityItem entityItem = EntityItems[itemEntityIndex];
		if (entityItem.CurrentItemAppearanceCondition != null && entityItem.CurrentItemAppearanceCondition.EntityStatIndex == entityStatIndex)
		{
			CheckItemAppearanceConditionsToRemove(item, itemEntityIndex, clientEntityStatValue, value);
		}
		if (entityItem.CurrentItemAppearanceCondition != null)
		{
			return false;
		}
		return CheckItemAppearanceConditionsToApply(item, itemEntityIndex, entityStatIndex, clientEntityStatValue, value);
	}

	private void CheckItemAppearanceConditionsToRemove(ClientItemBase item, int itemEntityIndex, ClientEntityStatValue entityStatValue, ClientItemAppearanceCondition[] itemAppearanceConditions)
	{
		ClientItemAppearanceCondition.Data currentItemAppearanceCondition = EntityItems[itemEntityIndex].CurrentItemAppearanceCondition;
		ClientItemAppearanceCondition clientItemAppearanceCondition = itemAppearanceConditions[currentItemAppearanceCondition.ConditionIndex];
		if (clientItemAppearanceCondition.CanApplyCondition(entityStatValue))
		{
			return;
		}
		if (item.Model != null)
		{
			EntityItems[itemEntityIndex].ModelRenderer.Dispose();
			EntityItems[itemEntityIndex].ModelRenderer = new ModelRenderer(item.Model, _gameInstance.AtlasSizes, _gameInstance.Engine.Graphics, _gameInstance.FrameCounter);
			EntityItems[itemEntityIndex].ModelRenderer.SetSlotAnimation(0, item.Animation);
		}
		if (currentItemAppearanceCondition.EntityParticles != null)
		{
			for (int i = 0; i < currentItemAppearanceCondition.EntityParticles.Length; i++)
			{
				EntityParticle entityParticle = currentItemAppearanceCondition.EntityParticles[i];
				entityParticle?.ParticleSystemProxy?.Expire();
				EntityItems[itemEntityIndex].Particles.Remove(entityParticle);
			}
		}
		EntityItems[itemEntityIndex].ModelVFX.Id = null;
		EntityItems[itemEntityIndex].CurrentItemAppearanceCondition = null;
	}

	private bool CheckItemAppearanceConditionsToApply(ClientItemBase item, int itemEntityIndex, int entityStatIndex, ClientEntityStatValue entityStatValue, ClientItemAppearanceCondition[] itemAppearanceConditionsForStat)
	{
		for (int i = 0; i < itemAppearanceConditionsForStat.Length; i++)
		{
			ClientItemAppearanceCondition clientItemAppearanceCondition = itemAppearanceConditionsForStat[i];
			if (!clientItemAppearanceCondition.CanApplyCondition(entityStatValue))
			{
				continue;
			}
			ClientItemAppearanceCondition.Data data = new ClientItemAppearanceCondition.Data(entityStatIndex, i);
			if (clientItemAppearanceCondition.ModelId != null || clientItemAppearanceCondition.Texture != null)
			{
				ApplyItemAppearanceConditionModel(item, itemEntityIndex, clientItemAppearanceCondition);
			}
			bool flag = NetworkId == _gameInstance.LocalPlayerNetworkId && _gameInstance.CameraModule.Controller.IsFirstPerson && clientItemAppearanceCondition.FirstPersonParticles != null;
			ModelParticleSettings[] array = (flag ? clientItemAppearanceCondition.FirstPersonParticles : clientItemAppearanceCondition.Particles);
			if (array != null)
			{
				ApplyItemAppearanceConditionParticles(item, itemEntityIndex, clientItemAppearanceCondition, data, flag);
			}
			if (clientItemAppearanceCondition.ModelVFXId != null)
			{
				if (!_gameInstance.EntityStoreModule.ModelVFXByIds.TryGetValue(clientItemAppearanceCondition.ModelVFXId, out var value))
				{
					_gameInstance.App.DevTools.Error("Could not find model vfx: " + clientItemAppearanceCondition.ModelVFXId);
				}
				else
				{
					ModelVFX protocolModelVFX = _gameInstance.EntityStoreModule.ModelVFXs[value];
					ApplyItemAppearanceConditionModelVFX(itemEntityIndex, clientItemAppearanceCondition, protocolModelVFX);
				}
			}
			EntityItems[itemEntityIndex].CurrentItemAppearanceCondition = data;
			return true;
		}
		return false;
	}

	private void ApplyItemAppearanceConditionModel(ClientItemBase item, int itemEntityIndex, ClientItemAppearanceCondition condition)
	{
		EntityItems[itemEntityIndex].ModelRenderer.Dispose();
		EntityItems[itemEntityIndex].ModelRenderer = new ModelRenderer(condition.Model, _gameInstance.AtlasSizes, _gameInstance.Engine.Graphics, _gameInstance.FrameCounter);
		EntityItems[itemEntityIndex].ModelRenderer.SetSlotAnimation(0, item.Animation);
	}

	protected virtual void ApplyItemAppearanceConditionParticles(ClientItemBase item, int itemEntityIndex, ClientItemAppearanceCondition condition, ClientItemAppearanceCondition.Data data, bool firstPerson)
	{
		ModelParticleSettings[] array = (firstPerson ? condition.FirstPersonParticles : condition.Particles);
		data.EntityParticles = new EntityParticle[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			EntityParticle entityParticle = AttachParticles(item.Model, EntityItems[itemEntityIndex].Particles, array[i], Scale * EntityItems[itemEntityIndex].Scale);
			data.EntityParticles[i] = entityParticle;
		}
	}

	private void ApplyItemAppearanceConditionModelVFX(int itemEntityIndex, ClientItemAppearanceCondition condition, ModelVFX protocolModelVFX)
	{
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Expected I4, but got Unknown
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Expected I4, but got Unknown
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Expected I4, but got Unknown
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Expected I4, but got Unknown
		EntityItems[itemEntityIndex].ModelVFX.Id = condition.ModelVFXId;
		bool flag = protocolModelVFX.HighlightColor != null;
		bool flag2 = protocolModelVFX.AnimationDuration > 0f;
		if (flag)
		{
			EntityItems[itemEntityIndex].ModelVFX.HighlightColor = new Vector3((float)(int)(byte)protocolModelVFX.HighlightColor.Red / 255f, (float)(int)(byte)protocolModelVFX.HighlightColor.Green / 255f, (float)(int)(byte)protocolModelVFX.HighlightColor.Blue / 255f);
		}
		if (flag2)
		{
			EntityItems[itemEntityIndex].ModelVFX.AnimationDuration = protocolModelVFX.AnimationDuration;
		}
		if (protocolModelVFX.AnimationRange != null)
		{
			EntityItems[itemEntityIndex].ModelVFX.AnimationRange = new Vector2(protocolModelVFX.AnimationRange.X, protocolModelVFX.AnimationRange.Y);
		}
		EntityItems[itemEntityIndex].ModelVFX.LoopOption = (ClientModelVFX.LoopOptions)protocolModelVFX.LoopOption_;
		CurveType curveType_ = protocolModelVFX.CurveType_;
		CurveType val = curveType_;
		switch ((int)val)
		{
		case 0:
			EntityItems[itemEntityIndex].ModelVFX.CurveType = Easing.EasingType.Linear;
			break;
		case 1:
			EntityItems[itemEntityIndex].ModelVFX.CurveType = Easing.EasingType.QuartIn;
			break;
		case 3:
			EntityItems[itemEntityIndex].ModelVFX.CurveType = Easing.EasingType.QuartInOut;
			break;
		case 2:
			EntityItems[itemEntityIndex].ModelVFX.CurveType = Easing.EasingType.QuartOut;
			break;
		}
		EntityItems[itemEntityIndex].ModelVFX.HighlightThickness = protocolModelVFX.HighlightThickness;
		if (protocolModelVFX.NoiseScale != null)
		{
			EntityItems[itemEntityIndex].ModelVFX.NoiseScale = new Vector2(protocolModelVFX.NoiseScale.X, protocolModelVFX.NoiseScale.Y);
		}
		if (protocolModelVFX.NoiseScrollSpeed != null)
		{
			EntityItems[itemEntityIndex].ModelVFX.NoiseScrollSpeed = new Vector2(protocolModelVFX.NoiseScrollSpeed.X, protocolModelVFX.NoiseScrollSpeed.Y);
		}
		if (protocolModelVFX.PostColor != null)
		{
			EntityItems[itemEntityIndex].ModelVFX.PostColor = new Vector4((float)(int)(byte)protocolModelVFX.PostColor.Red / 255f, (float)(int)(byte)protocolModelVFX.PostColor.Green / 255f, (float)(int)(byte)protocolModelVFX.PostColor.Blue / 255f, protocolModelVFX.PostColorOpacity);
		}
		ClientModelVFX.EffectDirections direction = (ClientModelVFX.EffectDirections)protocolModelVFX.EffectDirection_;
		SwitchTo switchTo_ = protocolModelVFX.SwitchTo_;
		int useBloom = (protocolModelVFX.UseBloomOnHighlight ? 1 : 0);
		int useProgressiveHighlight = (protocolModelVFX.UseProgessiveHighlight ? 1 : 0);
		EntityItems[itemEntityIndex].ModelVFX.PackedModelVFXParams = UniqueEntityEffect.PackModelVFXData((int)direction, (int)switchTo_, useBloom, useProgressiveHighlight);
		if (flag && flag2)
		{
			EntityItems[itemEntityIndex].ModelVFX.TriggerAnimation = true;
		}
	}

	public ClientEntityStatValue GetEntityStat(int entityStatIndex)
	{
		if (entityStatIndex < 0 || entityStatIndex >= _entityStats.Length)
		{
			return null;
		}
		return _entityStats[entityStatIndex];
	}

	private static bool TestMaxValueEffects(ClientEntityStatValue value, float? previousValue, EntityStatEffects effects)
	{
		if (effects == null)
		{
			return false;
		}
		if (effects.TriggerAtZero && value.Min > 0f)
		{
			return previousValue < 0f && value.Value >= 0f;
		}
		return previousValue != value.Max && value.Value == value.Max;
	}

	private static bool TestMinValueEffects(ClientEntityStatValue value, float? previousValue, EntityStatEffects effects)
	{
		if (effects == null)
		{
			return false;
		}
		if (effects.TriggerAtZero && value.Min < 0f)
		{
			return previousValue > 0f && value.Value <= 0f;
		}
		return previousValue != value.Min && value.Value == value.Min;
	}

	public bool TryGetUIComponent(int id, out ClientEntityUIComponent component)
	{
		component = null;
		ClientEntityUIComponent[] entityUIComponents = _gameInstance.ServerSettings.EntityUIComponents;
		if (entityUIComponents == null)
		{
			return false;
		}
		if (id > entityUIComponents.Length - 1)
		{
			throw new ArgumentOutOfRangeException();
		}
		component = entityUIComponents[id];
		return true;
	}

	public void SetUIComponents(int[] components)
	{
		UIComponents = components;
	}

	public void ClearUIComponents()
	{
		UIComponents = Array.Empty<int>();
	}
}
