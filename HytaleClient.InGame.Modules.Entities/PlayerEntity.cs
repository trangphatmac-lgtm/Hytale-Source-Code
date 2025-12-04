#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Data.Characters;
using HytaleClient.Data.Entities;
using HytaleClient.Data.Items;
using HytaleClient.Data.UserSettings;
using HytaleClient.Graphics;
using HytaleClient.Graphics.BlockyModels;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.Entities;

internal class PlayerEntity : Entity
{
	private struct ItemModelDrawTask
	{
		public int NodeIndex;

		public Matrix ModelMatrix;
	}

	private struct MouseWiggle
	{
		public float X;

		public float Y;

		public float Pitch;

		public float Roll;
	}

	private struct MovementWiggle
	{
		public float X;

		public float Y;

		public float Z;

		public float Pitch;

		public float Roll;
	}

	public const float PitchEdgePadding = 0.01f;

	private const float ShakeMaxHeight = 15f;

	private const float MaxShake = 0.75f;

	private const float ShakeDuration = 0.1f;

	private static readonly Vector3 defaultPullbackOffsetRight = new Vector3(-1f, 0.25f, -1.2f);

	private static readonly Vector3 defaultPullbackOffsetLeft = new Vector3(1f, 0.25f, -1.2f);

	private static readonly Vector3 defaultPullbackRotationRight = new Vector3(-2f, -0.5f, 0f);

	private static readonly Vector3 defaultPullbackRotationLeft = new Vector3(-2f, 0.5f, 0f);

	private bool _needsDistortionEffect;

	public const int MaxItems = 2;

	private int _lastHotbarSlot = -1;

	private int _lastUtilitySlot = -1;

	private BlockyModel _baseFirstPersonModel;

	private ItemModelDrawTask[] _itemModelDrawTasks = new ItemModelDrawTask[2];

	private int _itemModelRendererCount = 0;

	private ModelRenderer _firstPersonModelRenderer;

	public string CurrentFirstPersonAnimationId;

	private bool _wasFirstPerson = false;

	private bool _forceFXViewSwitch = false;

	private bool _wasLookYawOffsetClockwise = true;

	private Matrix _tempMatrix;

	private Matrix _modelMatrix;

	private float _moveAngle;

	private MouseWiggle _previousMouseWiggle;

	private MouseWiggle _nextMouseWiggle;

	private MouseWiggle _mouseWiggle;

	private MovementWiggle _previousMovementWiggle;

	private MovementWiggle _nextMovementWiggle;

	private MovementWiggle _movementWiggle;

	private MovementEffects _movementEffects = new MovementEffects();

	private List<int> _activeInteractions = new List<int>();

	private float _horizontalSpeedMultiplier = 1f;

	private bool _hasStaminaDepletedEffect = false;

	private HashSet<InteractionType> _disabledAbilities;

	private bool _wasOnGround = true;

	private readonly Quaternion _renderOrientationOffset = Quaternion.CreateFromYawPitchRoll((float)System.Math.PI, 0f, 0f);

	private Vector3 _renderPosition;

	private Quaternion _renderOrientation;

	private float _firstPersonObstacleDistance = -1f;

	private float _firstPersonWeaponDrawbackStartDistance = 1.5f;

	private int _firstPersonArmRightIdx;

	private int _firstPersonArmLeftIdx;

	private readonly HitDetection.RaycastOptions _firstPersonObstacleRaycastOptions = new HitDetection.RaycastOptions
	{
		Distance = 5f,
		IgnoreFluids = true,
		IgnoreEmptyCollisionMaterial = true
	};

	private ClientItemBase _pullbackPrevPrimaryItem;

	private ClientItemBase _pullbackPrevSecondaryItem;

	private ClientItemPullbackConfig _pullbackPrevPrimaryAnimConfig;

	private ClientItemPullbackConfig _pullbackPrevSecondaryAnimConfig;

	private Vector3 _pullbackLeftOffset;

	private Vector3 _pullbackLeftRotation;

	private Vector3 _pullbackRightOffset;

	private Vector3 _pullbackRightRotation;

	private bool animationClipsGeometry;

	private const string WeaponModifierPrefix = "*Weapon_";

	private const string UtilityModifierPrefix = "*Utility_";

	public bool IsMounting { get; set; }

	public bool NeedsDistortionDraw => _needsDistortionEffect && FirstPersonViewNeedsDrawing();

	public bool HasStaminaDepletedEffect
	{
		get
		{
			UpdateStatsDependentOnChanges();
			return _hasStaminaDepletedEffect;
		}
	}

	public float HorizontalSpeedMultiplier
	{
		get
		{
			UpdateStatsDependentOnChanges();
			return _horizontalSpeedMultiplier;
		}
	}

	public bool DisableForward
	{
		get
		{
			UpdateStatsDependentOnChanges();
			return _movementEffects.DisableForward;
		}
	}

	public bool DisableBackward
	{
		get
		{
			UpdateStatsDependentOnChanges();
			return _movementEffects.DisableBackward;
		}
	}

	public bool DisableLeft
	{
		get
		{
			UpdateStatsDependentOnChanges();
			return _movementEffects.DisableLeft;
		}
	}

	public bool DisableRight
	{
		get
		{
			UpdateStatsDependentOnChanges();
			return _movementEffects.DisableRight;
		}
	}

	public bool DisableSprint
	{
		get
		{
			UpdateStatsDependentOnChanges();
			return _movementEffects.DisableSprint;
		}
	}

	public bool DisableJump
	{
		get
		{
			UpdateStatsDependentOnChanges();
			return _movementEffects.DisableJump;
		}
	}

	public bool DisableCrouch
	{
		get
		{
			UpdateStatsDependentOnChanges();
			return _movementEffects.DisableCrouch;
		}
	}

	public HashSet<InteractionType> DisabledAbilities
	{
		get
		{
			UpdateStatsDependentOnChanges();
			return _disabledAbilities;
		}
	}

	public PlayerEntity(GameInstance gameInstance, int networkId)
		: base(gameInstance, networkId)
	{
	}//IL_0037: Unknown result type (might be due to invalid IL or missing references)
	//IL_0041: Expected O, but got Unknown


	protected override void DoDispose()
	{
		base.DoDispose();
		ClearFirstPersonView();
	}

	public void ClearFirstPersonView()
	{
		if (_firstPersonModelRenderer != null)
		{
			_firstPersonModelRenderer.Dispose();
			_firstPersonModelRenderer = null;
		}
		ClearFirstPersonItems();
	}

	public void ClearFirstPersonItems()
	{
		_itemModelRendererCount = 0;
	}

	public void SetFirstPersonItems()
	{
		if (base.PrimaryItem != null)
		{
			SetFirstPersonItem(_baseFirstPersonModel, base.PrimaryItem);
		}
		if (base.SecondaryItem != null)
		{
			SetFirstPersonItem(_baseFirstPersonModel, base.SecondaryItem);
		}
		CurrentFirstPersonAnimationId = null;
		_forceFXViewSwitch = true;
	}

	public void SetFirstPersonView(BlockyModel thirdPersonModel)
	{
		_baseFirstPersonModel = thirdPersonModel.CloneArmsAndLegs(CharacterPartStore.RightArmNameId, CharacterPartStore.RightForearmNameId, CharacterPartStore.LeftArmNameId, CharacterPartStore.LeftForearmNameId, CharacterPartStore.RightThighNameId, CharacterPartStore.LeftThighNameId);
		if (_baseFirstPersonModel.NodeCount == 0)
		{
			return;
		}
		_firstPersonModelRenderer = new ModelRenderer(_baseFirstPersonModel, _gameInstance.AtlasSizes, _gameInstance.Engine.Graphics, _gameInstance.FrameCounter);
		for (int i = 0; i < _entityParticles.Count; i++)
		{
			if (!_baseFirstPersonModel.NodeIndicesByNameId.TryGetValue(_entityParticles[i].TargetNodeNameId, out _entityParticles[i].TargetFirstPersonNodeIndex))
			{
				_entityParticles[i].TargetFirstPersonNodeIndex = -1;
			}
		}
		for (int j = 0; j < _entityTrails.Count; j++)
		{
			if (!_baseFirstPersonModel.NodeIndicesByNameId.TryGetValue(_entityTrails[j].TargetNodeNameId, out _entityTrails[j].TargetFirstPersonNodeIndex))
			{
				_entityTrails[j].TargetFirstPersonNodeIndex = -1;
			}
		}
		_baseFirstPersonModel.NodeIndicesByNameId.TryGetValue(CharacterPartStore.RightArmNameId, out _firstPersonArmRightIdx);
		_baseFirstPersonModel.NodeIndicesByNameId.TryGetValue(CharacterPartStore.LeftArmNameId, out _firstPersonArmLeftIdx);
		SetFirstPersonItems();
	}

	private void SetFirstPersonItem(BlockyModel model, ClientItemBase item)
	{
		model.NodeIndicesByNameId.TryGetValue(EntityItems[_itemModelRendererCount].TargetNodeNameId, out _itemModelDrawTasks[_itemModelRendererCount].NodeIndex);
		_itemModelRendererCount++;
	}

	public override void SetTransform(Vector3 position, Vector3 bodyOrientation, Vector3 lookOrientation)
	{
		if (lookOrientation != LookOrientation)
		{
			_gameInstance.CameraModule.Controller.SetRotation(lookOrientation);
		}
		base.SetTransform(position, bodyOrientation, lookOrientation);
	}

	public override void SetServerAnimation(string animationId, AnimationSlot slot, float animationTime = -1f, bool storeCurrentAnimationId = false)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		base.SetServerAnimation(animationId, slot, animationTime, storeCurrentAnimationId);
		if ((int)slot != 2 || _firstPersonModelRenderer == null)
		{
			return;
		}
		EntityAnimation entityAnimation = GetItemAnimation(base.PrimaryItem, animationId);
		if (entityAnimation == null)
		{
			entityAnimation = GetAnimation(animationId);
		}
		if (entityAnimation != null)
		{
			animationClipsGeometry = entityAnimation.ClipsGeometry;
			int num = 5;
			BlockyAnimation slotAnimation = _firstPersonModelRenderer.GetSlotAnimation(num);
			if ((entityAnimation.FirstPersonData != slotAnimation || _gameInstance.App.Settings.UseOverrideFirstPersonAnimations) && (entityAnimation.FirstPersonOverrideData == null || entityAnimation.FirstPersonOverrideData != slotAnimation || !_gameInstance.App.Settings.UseOverrideFirstPersonAnimations))
			{
				bool looping = entityAnimation.Looping;
				BlockyAnimation firstPersonAnimation = GetFirstPersonAnimation(entityAnimation);
				_firstPersonModelRenderer.SetSlotAnimation(num, firstPersonAnimation, looping, 1f, 0f, 12f, entityAnimation.PullbackConfig);
				CurrentFirstPersonAnimationId = animationId;
			}
		}
	}

	public override EntityAnimation GetTargetActionAnimation(InteractionType interactionType)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)interactionType == 1 && base.SecondaryItem != null)
		{
			EntityAnimation animation = base.SecondaryItem.GetAnimation("Attack");
			if (animation != null)
			{
				return animation;
			}
			return EntityAnimation.Empty;
		}
		EntityAnimation entityAnimation = (base.PrimaryItem ?? base.SecondaryItem)?.GetAnimation(CurrentFirstPersonAnimationId);
		if (entityAnimation != null)
		{
			return entityAnimation;
		}
		return EntityAnimation.Empty;
	}

	public override void SetActionAnimation(EntityAnimation targetAnimation, float animationTime = 0f, bool holdLastFrame = false, bool force = false)
	{
		base.SetActionAnimation(targetAnimation, animationTime, holdLastFrame, force);
		if (_firstPersonModelRenderer != null)
		{
			BlockyAnimation slotAnimation = _firstPersonModelRenderer.GetSlotAnimation(5);
			if ((force || targetAnimation.FirstPersonData != slotAnimation || _gameInstance.App.Settings.UseOverrideFirstPersonAnimations) && (force || targetAnimation.FirstPersonOverrideData == null || targetAnimation.FirstPersonOverrideData != slotAnimation || !_gameInstance.App.Settings.UseOverrideFirstPersonAnimations))
			{
				BlockyAnimation firstPersonAnimation = GetFirstPersonAnimation(targetAnimation);
				_firstPersonModelRenderer.SetSlotAnimation(5, firstPersonAnimation, targetAnimation.Looping, targetAnimation.Speed, animationTime, targetAnimation.BlendingDuration, targetAnimation.PullbackConfig, force);
				animationClipsGeometry = targetAnimation.ClipsGeometry;
			}
		}
	}

	public bool ShouldDisplayHudForEntityStat(int entityStatIndex)
	{
		return (base.PrimaryItem != null && base.PrimaryItem.ShouldDisplayHudForEntityStat(entityStatIndex)) || (base.SecondaryItem != null && base.SecondaryItem.ShouldDisplayHudForEntityStat(entityStatIndex));
	}

	public void UsePrimaryItem()
	{
		UseItem((InteractionType)0, "Attack");
	}

	public void UseSecondaryItem()
	{
		UseItem((InteractionType)1, "SecondaryAction");
	}

	private void UseItem(InteractionType interactionType, string animationId)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		CurrentFirstPersonAnimationId = animationId;
		EntityAnimation targetActionAnimation = GetTargetActionAnimation(interactionType);
		SetActionAnimation(targetActionAnimation);
	}

	public override void UpdateWithoutPosition(float deltaTime, float distanceToCamera, bool skipUpdateLogic = false)
	{
		base.UpdateWithoutPosition(deltaTime, distanceToCamera, skipUpdateLogic);
		ref ClientMovementStates relativeMovementStates = ref GetRelativeMovementStates();
		float pitch = base.BodyOrientation.Pitch;
		float num = base.BodyOrientation.Yaw;
		float roll = base.BodyOrientation.Roll;
		Vector2 vector = ((_gameInstance.CameraModule.Controller.AttachedTo == this) ? _gameInstance.CharacterControllerModule.MovementController.GetWishDirection() : Vector2.Zero);
		if (vector.Length() > 0.1f)
		{
			float num2 = ((vector.Y == 0f) ? 1f : vector.Y);
			float num3 = (float)System.Math.PI / 4f * (0f - vector.X) * num2;
			_moveAngle = MathHelper.LerpAngle(_moveAngle, MathHelper.WrapAngle(_gameInstance.CameraModule.Controller.MovementForceRotation.Yaw + num3), 0.4f);
			float num4 = MathHelper.WrapAngle(LookOrientation.Yaw - _moveAngle);
			if (num4 < (float)System.Math.PI * -2f / 3f || num4 > (float)System.Math.PI * 2f / 3f)
			{
				num4 = MathHelper.WrapAngle(num4 - (float)System.Math.PI);
			}
			float num5 = LookOrientation.Yaw - MathHelper.Clamp(num4, base.CameraSettings.Yaw.AngleRange.Min, base.CameraSettings.Yaw.AngleRange.Max);
			float value = MathHelper.WrapAngle(num5 - num);
			value = MathHelper.Clamp(value, -6f * deltaTime, 6f * deltaTime);
			num += value;
		}
		else if (base.CameraSettings.Yaw.AngleRange.Max != (float)System.Math.PI && base.CameraSettings.Yaw.AngleRange.Min != -(float)System.Math.PI)
		{
			float num6 = MathHelper.WrapAngle(LookOrientation.Yaw) - MathHelper.WrapAngle(num);
			if (num6 < 0f)
			{
				num6 += (float)System.Math.PI * 2f;
			}
			float num7 = MathHelper.WrapAngle(LookOrientation.Yaw - num);
			bool flag = num6 > (float)System.Math.PI;
			if (flag != _wasLookYawOffsetClockwise && (num7 > (float)System.Math.PI * 2f / 3f || num7 < (float)System.Math.PI * -2f / 3f))
			{
				num7 = ((!_wasLookYawOffsetClockwise) ? ((float)System.Math.PI + ((float)System.Math.PI + num7)) : (-(float)System.Math.PI - ((float)System.Math.PI - num7)));
				flag = !flag;
			}
			if (num7 > base.CameraSettings.Yaw.AngleRange.Max)
			{
				num += num7 - base.CameraSettings.Yaw.AngleRange.Max;
			}
			else if (num7 < base.CameraSettings.Yaw.AngleRange.Min)
			{
				num += num7 - base.CameraSettings.Yaw.AngleRange.Min;
			}
			_wasLookYawOffsetClockwise = flag;
		}
		if (!IsMounting)
		{
			SetBodyOrientation(new Vector3(pitch, num, roll));
		}
		if (_firstPersonModelRenderer == null)
		{
			return;
		}
		if (base.CurrentActionAnimation == null && CurrentFirstPersonAnimationId != _currentAnimationId)
		{
			CurrentFirstPersonAnimationId = _currentAnimationId;
			float currentSpeedMultiplierDiff = _gameInstance.CharacterControllerModule.MovementController.CurrentSpeedMultiplierDiff;
			float slotAnimationTime = _firstPersonModelRenderer.GetSlotAnimationTime(0);
			bool looping = (GetAnimation(_currentAnimationId) ?? EntityAnimation.Empty).Looping;
			EntityAnimation itemAnimation = GetItemAnimation(base.PrimaryItem, _currentAnimationId);
			if (!itemAnimation.KeepPreviousFirstPersonAnimation)
			{
				BlockyAnimation firstPersonAnimation = GetFirstPersonAnimation(itemAnimation);
				_firstPersonModelRenderer.SetSlotAnimation(1, firstPersonAnimation, looping, itemAnimation.Speed + itemAnimation.Speed * currentSpeedMultiplierDiff, slotAnimationTime, itemAnimation.BlendingDuration, itemAnimation.PullbackConfig);
				animationClipsGeometry = itemAnimation.ClipsGeometry;
			}
			EntityAnimation itemAnimation2 = GetItemAnimation(base.SecondaryItem, _currentAnimationId, useDefaultAnimations: false);
			if (!itemAnimation2.KeepPreviousFirstPersonAnimation)
			{
				BlockyAnimation firstPersonAnimation2 = GetFirstPersonAnimation(itemAnimation2);
				int slotIndex = ((base.PrimaryItem == null) ? 1 : 2);
				_firstPersonModelRenderer.SetSlotAnimation(slotIndex, firstPersonAnimation2, looping, itemAnimation2.Speed + itemAnimation2.Speed * currentSpeedMultiplierDiff, slotAnimationTime, itemAnimation2.BlendingDuration, itemAnimation2.PullbackConfig);
				if (!animationClipsGeometry && itemAnimation2.ClipsGeometry)
				{
					animationClipsGeometry = true;
				}
			}
			if (_firstPersonModelRenderer.GetSlotAnimation(5) != null)
			{
				_firstPersonModelRenderer.SetSlotAnimation(5, null, isLooping: true, 1f, 0f, 12f);
			}
		}
		else
		{
			_firstPersonModelRenderer.AdvancePlayback(deltaTime * 60f);
		}
		UpdateFirstPersonObstacleDetection();
		_wasOnGround = relativeMovementStates.IsOnGround;
	}

	private void UpdateFirstPersonObstacleDetection()
	{
		_firstPersonObstacleDistance = -1f;
		Vector3 origin = new Vector3(base.Position.X, base.Position.Y + base.EyeOffset, base.Position.Z);
		Vector3 value = Vector3.Forward;
		Vector3 rotation = _gameInstance.CameraModule.Controller.Rotation;
		Quaternion rotation2 = Quaternion.CreateFromYawPitchRoll(rotation.Yaw, rotation.Pitch, rotation.Roll);
		Vector3.Transform(ref value, ref rotation2, out value);
		if (_gameInstance.HitDetection.Raycast(origin, value, _firstPersonObstacleRaycastOptions, out var hasFoundTargetBlock, out var blockHitData, out var hasFoundTargetEntity, out var entityHitData) && (hasFoundTargetBlock || hasFoundTargetEntity))
		{
			if (hasFoundTargetBlock && !hasFoundTargetEntity)
			{
				_firstPersonObstacleDistance = blockHitData.Distance;
			}
			else if (!hasFoundTargetBlock)
			{
				_firstPersonObstacleDistance = entityHitData.ClosestDistance;
			}
			else
			{
				_firstPersonObstacleDistance = ((blockHitData.Distance < entityHitData.ClosestDistance) ? blockHitData.Distance : entityHitData.ClosestDistance);
			}
		}
	}

	public void UpdateClientInterpolation(float timeFraction)
	{
		RenderPosition = Vector3.Lerp(_previousPosition, _nextPosition, timeFraction) + new Vector3(0f, _gameInstance.CharacterControllerModule.MovementController.AutoJumpHeightShift, 0f);
		_movementWiggle.X = MathHelper.Lerp(_previousMovementWiggle.X, _nextMovementWiggle.X, timeFraction);
		_movementWiggle.Y = MathHelper.Lerp(_previousMovementWiggle.Y, _nextMovementWiggle.Y, timeFraction);
		_movementWiggle.Z = MathHelper.Lerp(_previousMovementWiggle.Z, _nextMovementWiggle.Z, timeFraction);
		_movementWiggle.Roll = MathHelper.Lerp(_previousMovementWiggle.Roll, _nextMovementWiggle.Roll, timeFraction);
		_movementWiggle.Pitch = MathHelper.Lerp(_previousMovementWiggle.Pitch, _nextMovementWiggle.Pitch, timeFraction);
	}

	public void UpdateClientInterpolationMouseWiggle(float timeFraction)
	{
		_mouseWiggle.X = MathHelper.Lerp(_previousMouseWiggle.X, _nextMouseWiggle.X, timeFraction);
		_mouseWiggle.Y = MathHelper.Lerp(_previousMouseWiggle.Y, _nextMouseWiggle.Y, timeFraction);
		_mouseWiggle.Roll = MathHelper.Lerp(_previousMouseWiggle.Roll, _nextMouseWiggle.Roll, timeFraction);
		_mouseWiggle.Pitch = MathHelper.Lerp(_previousMouseWiggle.Pitch, _nextMouseWiggle.Pitch, timeFraction);
	}

	public bool FirstPersonViewNeedsDrawing()
	{
		return _gameInstance.App.InGame.IsFirstPersonViewVisible && _firstPersonModelRenderer != null && _gameInstance.CameraModule.Controller.IsFirstPerson;
	}

	public void ApplyFirstPersonMouseItemWiggle(float moveX, float moveY)
	{
		_previousMouseWiggle = _nextMouseWiggle;
		ClientItemBase clientItemBase = base.PrimaryItem ?? base.SecondaryItem;
		if (clientItemBase != null)
		{
			_nextMouseWiggle.X = CalculateUpdatedItemWiggle(_nextMouseWiggle.X, moveX, clientItemBase.PlayerAnimations.WiggleWeights.XDeceleration);
			_nextMouseWiggle.Y = CalculateUpdatedItemWiggle(_nextMouseWiggle.Y, moveY, clientItemBase.PlayerAnimations.WiggleWeights.YDeceleration);
			_nextMouseWiggle.Roll = CalculateUpdatedItemWiggle(_nextMouseWiggle.Roll, 0f - moveX, clientItemBase.PlayerAnimations.WiggleWeights.RollDeceleration);
			_nextMouseWiggle.Pitch = CalculateUpdatedItemWiggle(_nextMouseWiggle.Pitch, moveY, clientItemBase.PlayerAnimations.WiggleWeights.PitchDeceleration);
		}
	}

	public void ApplyFirstPersonMovementItemWiggle(float moveX, float moveY, float moveZ)
	{
		_previousMovementWiggle = _nextMovementWiggle;
		ClientItemBase clientItemBase = base.PrimaryItem ?? base.SecondaryItem;
		if (clientItemBase != null)
		{
			_nextMovementWiggle.X = CalculateUpdatedItemWiggle(_nextMovementWiggle.X, moveX, clientItemBase.PlayerAnimations.WiggleWeights.XDeceleration);
			_nextMovementWiggle.Y = CalculateUpdatedItemWiggle(_nextMovementWiggle.Y, moveY, clientItemBase.PlayerAnimations.WiggleWeights.YDeceleration);
			_nextMovementWiggle.Z = CalculateUpdatedItemWiggle(_nextMovementWiggle.Z, moveZ, clientItemBase.PlayerAnimations.WiggleWeights.ZDeceleration);
			_nextMovementWiggle.Roll = CalculateUpdatedItemWiggle(_nextMovementWiggle.Roll, 0f - moveX, clientItemBase.PlayerAnimations.WiggleWeights.RollDeceleration);
			_nextMovementWiggle.Pitch = CalculateUpdatedItemWiggle(_nextMovementWiggle.Pitch, moveY, clientItemBase.PlayerAnimations.WiggleWeights.PitchDeceleration);
		}
	}

	private float CalculateUpdatedItemWiggle(float currentValue, float offset, float deceleration)
	{
		float num = (float)System.Math.Sqrt(1f - System.Math.Abs(currentValue)) * 0.25f;
		currentValue = MathHelper.Step(currentValue, (offset > 0f) ? 1f : (-1f), System.Math.Abs(offset) * num);
		currentValue = MathHelper.Step(currentValue, 0f, deceleration * MathHelper.Distance(currentValue, 0f));
		return currentValue;
	}

	public void ClearFirstPersonItemWiggle()
	{
		_nextMouseWiggle.Y = (_nextMouseWiggle.X = (_nextMouseWiggle.Pitch = (_nextMouseWiggle.Roll = 0f)));
		_nextMovementWiggle.Y = (_nextMovementWiggle.X = (_nextMovementWiggle.Z = (_nextMovementWiggle.Pitch = (_nextMovementWiggle.Roll = 0f))));
	}

	public void LookAt(Vector3 relativePosition, float interpolation = 1f)
	{
		relativePosition -= base.Position;
		relativePosition.Y -= base.EyeOffset;
		if (!MathHelper.WithinEpsilon(relativePosition.X, 0f) || !MathHelper.WithinEpsilon(relativePosition.Z, 0f))
		{
			float angle = (float)System.Math.Atan2(0f - relativePosition.X, 0f - relativePosition.Z);
			angle = MathHelper.WrapAngle(angle);
			LookOrientation.Yaw = MathHelper.LerpAngle(LookOrientation.Yaw, angle, interpolation);
		}
		float num = relativePosition.Length();
		if (num > 0f)
		{
			float value = (float)System.Math.PI / 2f - (float)System.Math.Acos(relativePosition.Y / num);
			value = MathHelper.Clamp(value, -1.5607964f, 1.5607964f);
			LookOrientation.Pitch = MathHelper.LerpAngle(LookOrientation.Pitch, value, interpolation);
		}
		UpdateModelLookOrientation();
	}

	public void UpdateModelLookOrientation()
	{
		Quaternion result = Quaternion.Identity;
		Quaternion result2 = Quaternion.Identity;
		if (!_gameInstance.CameraModule.Controller.IsFirstPerson)
		{
			Vector3 bodyOrientation = base.BodyOrientation;
			bodyOrientation.Yaw -= (float)System.Math.PI;
			Quaternion.CreateFromYawPitchRoll(bodyOrientation.Yaw, 0f - bodyOrientation.Pitch, 0f - bodyOrientation.Roll, out result);
			Vector3 lookOrientation = LookOrientation;
			lookOrientation.Yaw -= (float)System.Math.PI;
			float value = MathHelper.WrapAngle(bodyOrientation.Yaw - lookOrientation.Yaw);
			value = MathHelper.Clamp(value, base.CameraSettings.Yaw.AngleRange.Min, base.CameraSettings.Yaw.AngleRange.Max);
			lookOrientation.Yaw = bodyOrientation.Yaw - value;
			float value2 = MathHelper.WrapAngle(bodyOrientation.Pitch - lookOrientation.Pitch);
			value2 = MathHelper.Clamp(value2, base.CameraSettings.Pitch.AngleRange.Min, base.CameraSettings.Pitch.AngleRange.Max);
			lookOrientation.Pitch = bodyOrientation.Pitch - value2;
			Quaternion.CreateFromYawPitchRoll(lookOrientation.Yaw, 0f - lookOrientation.Pitch, 0f - lookOrientation.Roll, out result2);
		}
		base.ModelRenderer.SetCameraOrientation(Quaternion.Inverse(result) * result2);
	}

	public override bool AddCombatSequenceEffects(ModelParticle[] particles, ModelTrail[] trails)
	{
		if (!base.AddCombatSequenceEffects(particles, trails))
		{
			return false;
		}
		bool isFirstPerson = _gameInstance.CameraModule.Controller.IsFirstPerson;
		for (int i = 0; i < _combatSequenceParticles.Count; i++)
		{
			if (!_baseFirstPersonModel.NodeIndicesByNameId.TryGetValue(_combatSequenceParticles[i].TargetNodeNameId, out _combatSequenceParticles[i].TargetFirstPersonNodeIndex))
			{
				_combatSequenceParticles[i].TargetFirstPersonNodeIndex = -1;
			}
			_combatSequenceParticles[i].ParticleSystemProxy.SetFirstPerson(isFirstPerson);
		}
		for (int j = 0; j < _combatSequenceTrails.Count; j++)
		{
			if (!_baseFirstPersonModel.NodeIndicesByNameId.TryGetValue(_combatSequenceTrails[j].TargetNodeNameId, out _combatSequenceTrails[j].TargetFirstPersonNodeIndex))
			{
				_combatSequenceTrails[j].TargetFirstPersonNodeIndex = -1;
			}
			_combatSequenceTrails[j].TrailProxy.SetFirstPerson(isFirstPerson);
		}
		return true;
	}

	public void RegisterAnimationTasks()
	{
		AnimationSystem animationSystem = _gameInstance.Engine.AnimationSystem;
		animationSystem.PrepareForIncomingTasks(2 + _itemModelRendererCount + EntityEffects.Length);
		if (base.ModelRenderer != null)
		{
			animationSystem.RegisterAnimationTask(base.ModelRenderer, skipUpdate: false);
		}
		for (int i = 0; i < _itemModelRendererCount; i++)
		{
			if (EntityItems[i].ModelRenderer != null)
			{
				animationSystem.RegisterAnimationTask(EntityItems[i].ModelRenderer, skipUpdate: false);
			}
		}
		for (int j = 0; j < EntityEffects.Length; j++)
		{
			ref UniqueEntityEffect reference = ref EntityEffects[j];
			if (reference.ModelRenderer != null)
			{
				animationSystem.RegisterAnimationTask(reference.ModelRenderer, skipUpdate: false);
			}
		}
		if (_firstPersonModelRenderer != null)
		{
			animationSystem.RegisterAnimationTask(_firstPersonModelRenderer, skipUpdate: false);
		}
	}

	public void UpdateFirstPersonFX()
	{
		Debug.Assert(FirstPersonViewNeedsDrawing(), "UpdateFirstPersonFX called when it was not required. Please check with FirstPersonViewNeedsDrawing() first before calling this.");
		_renderOrientation *= _renderOrientationOffset;
		for (int i = 0; i < _entityParticles.Count; i++)
		{
			EntityParticle entityParticle = _entityParticles[i];
			int num = entityParticle.TargetFirstPersonNodeIndex;
			ModelRenderer modelRenderer = _firstPersonModelRenderer;
			entityParticle.ParticleSystemProxy.Position = Vector3.Zero;
			if (num == -1)
			{
				num = entityParticle.TargetNodeIndex;
				modelRenderer = base.ModelRenderer;
				entityParticle.ParticleSystemProxy.Position.Y -= base.EyeOffset;
			}
			ref AnimatedRenderer.NodeTransform reference = ref modelRenderer.NodeTransforms[num];
			entityParticle.ParticleSystemProxy.Position = entityParticle.ParticleSystemProxy.Position + _renderPosition + Vector3.Transform(reference.Position, _renderOrientation) * (1f / 64f) * Scale + Vector3.Transform(entityParticle.PositionOffset, _renderOrientation * reference.Orientation);
			entityParticle.ParticleSystemProxy.Rotation = _renderOrientation * reference.Orientation * entityParticle.RotationOffset;
		}
		for (int j = 0; j < _entityTrails.Count; j++)
		{
			EntityTrail entityTrail = _entityTrails[j];
			int num2 = entityTrail.TargetFirstPersonNodeIndex;
			ModelRenderer modelRenderer2 = _firstPersonModelRenderer;
			entityTrail.TrailProxy.Position = Vector3.Zero;
			if (num2 == -1)
			{
				num2 = entityTrail.TargetNodeIndex;
				modelRenderer2 = base.ModelRenderer;
				entityTrail.TrailProxy.Position.Y -= base.EyeOffset;
			}
			ref AnimatedRenderer.NodeTransform reference2 = ref modelRenderer2.NodeTransforms[num2];
			entityTrail.TrailProxy.Position = _renderPosition + Vector3.Transform(reference2.Position, _renderOrientation) * (1f / 64f) * Scale + Vector3.Transform(entityTrail.PositionOffset, _renderOrientation * reference2.Orientation);
			if (entityTrail.FixedRotation)
			{
				Vector3 rotation = _gameInstance.CameraModule.Controller.Rotation;
				entityTrail.TrailProxy.Rotation = Quaternion.CreateFromYawPitchRoll(0f, 0f - rotation.X, 0f) * Quaternion.CreateFromYawPitchRoll(0f - rotation.Y, 0f, 0f) * entityTrail.RotationOffset;
			}
			else
			{
				entityTrail.TrailProxy.Rotation = _renderOrientation * reference2.Orientation * entityTrail.RotationOffset;
			}
		}
		for (int k = 0; k < _itemModelRendererCount; k++)
		{
			EntityItem entityItem = EntityItems[k];
			if (entityItem.Particles.Count == 0 && entityItem.Trails.Count == 0)
			{
				continue;
			}
			ref AnimatedRenderer.NodeTransform reference3 = ref _firstPersonModelRenderer.NodeTransforms[_itemModelDrawTasks[k].NodeIndex];
			Quaternion quaternion = reference3.Orientation * entityItem.RootOrientationOffset;
			for (int l = 0; l < entityItem.Particles.Count; l++)
			{
				EntityParticle entityParticle2 = entityItem.Particles[l];
				ref AnimatedRenderer.NodeTransform reference4 = ref entityItem.ModelRenderer.NodeTransforms[entityParticle2.TargetNodeIndex];
				entityParticle2.ParticleSystemProxy.Position = _renderPosition + Vector3.Transform(reference3.Position + Vector3.Transform(reference4.Position * entityItem.Scale + entityItem.RootPositionOffset, quaternion), _renderOrientation) * (1f / 64f) * Scale + Vector3.Transform(entityParticle2.PositionOffset, _renderOrientation * quaternion * reference4.Orientation);
				entityParticle2.ParticleSystemProxy.Rotation = _renderOrientation * quaternion * reference4.Orientation * entityParticle2.RotationOffset;
			}
			for (int m = 0; m < entityItem.Trails.Count; m++)
			{
				EntityTrail entityTrail2 = entityItem.Trails[m];
				ref AnimatedRenderer.NodeTransform reference5 = ref entityItem.ModelRenderer.NodeTransforms[entityTrail2.TargetNodeIndex];
				entityTrail2.TrailProxy.Position = _renderPosition + Vector3.Transform(reference3.Position + Vector3.Transform(reference5.Position * entityItem.Scale + entityItem.RootPositionOffset, quaternion), _renderOrientation) * (1f / 64f) * Scale + Vector3.Transform(entityTrail2.PositionOffset, _renderOrientation * quaternion * reference5.Orientation);
				if (entityTrail2.FixedRotation)
				{
					Vector3 rotation2 = _gameInstance.CameraModule.Controller.Rotation;
					entityTrail2.TrailProxy.Rotation = Quaternion.CreateFromYawPitchRoll(0f, 0f - rotation2.X, 0f) * Quaternion.CreateFromYawPitchRoll(0f - rotation2.Y, 0f, 0f) * entityTrail2.RotationOffset;
				}
				else
				{
					entityTrail2.TrailProxy.Rotation = _renderOrientation * quaternion * reference5.Orientation * entityTrail2.RotationOffset;
				}
			}
		}
		bool flag = false;
		for (int n = 0; n < _itemModelRendererCount; n++)
		{
			ClientModelVFX modelVFX = EntityItems[n].ModelVFX;
			int num3 = (modelVFX.PackedModelVFXParams >> 3) & 3;
			flag = flag || (modelVFX.AnimationProgress != 0f && num3 == 2);
		}
		int num4 = (ModelVFX.PackedModelVFXParams >> 3) & 3;
		_needsDistortionEffect = flag || (ModelVFX.AnimationProgress != 0f && num4 == 2);
	}

	public void PrepareFXForViewSwitch()
	{
		bool isFirstPerson = _gameInstance.CameraModule.Controller.IsFirstPerson;
		if (isFirstPerson == _wasFirstPerson && !_forceFXViewSwitch)
		{
			return;
		}
		RefreshCharacterItemParticles();
		for (int i = 0; i < _entityParticles.Count; i++)
		{
			_entityParticles[i].ParticleSystemProxy.SetFirstPerson(isFirstPerson);
		}
		for (int j = 0; j < _entityTrails.Count; j++)
		{
			_entityTrails[j].TrailProxy.SetFirstPerson(isFirstPerson);
		}
		for (int k = 0; k < _itemModelRendererCount; k++)
		{
			EntityItem entityItem = EntityItems[k];
			for (int l = 0; l < entityItem.Particles.Count; l++)
			{
				entityItem.Particles[l].ParticleSystemProxy.SetFirstPerson(isFirstPerson);
			}
			for (int m = 0; m < entityItem.Trails.Count; m++)
			{
				entityItem.Trails[m].TrailProxy.SetFirstPerson(isFirstPerson);
			}
		}
		_wasFirstPerson = isFirstPerson;
		_forceFXViewSwitch = false;
	}

	public void PrepareForDrawInFirstPersonView()
	{
		Debug.Assert(FirstPersonViewNeedsDrawing(), "PrepareFirstPersonViewForDraw called when it was not required. Please check with FirstPersonViewNeedsDrawing() first before calling this.");
		Matrix.CreateScale(1f / 64f * Scale, out _modelMatrix);
		Matrix.CreateRotationY((float)System.Math.PI, out _tempMatrix);
		Matrix.Multiply(ref _modelMatrix, ref _tempMatrix, out _modelMatrix);
		_renderPosition = Vector3.Zero;
		_renderOrientation = Quaternion.Identity;
		Quaternion identity = Quaternion.Identity;
		if (IsFirstPersonClipping())
		{
			Quaternion.CreateFromYawPitchRoll(LookOrientation.Yaw, LookOrientation.Pitch, LookOrientation.Roll, out _renderOrientation);
		}
		ClientItemBase clientItemBase = base.PrimaryItem ?? base.SecondaryItem;
		if (clientItemBase != null)
		{
			WiggleWeights wiggleWeights = clientItemBase.PlayerAnimations.WiggleWeights;
			if (wiggleWeights.X != 0f)
			{
				_renderPosition.X += (_movementWiggle.X + _mouseWiggle.X) * wiggleWeights.X * 0.1f;
			}
			if (wiggleWeights.Y != 0f)
			{
				_renderPosition.Y += (_movementWiggle.Y + _mouseWiggle.Y) * wiggleWeights.Y * 0.1f;
			}
			if (wiggleWeights.Z != 0f)
			{
				_renderPosition.Z += _movementWiggle.Z * wiggleWeights.Z * 0.1f;
			}
			if (wiggleWeights.Roll != 0f)
			{
				_renderOrientation *= Quaternion.CreateFromYawPitchRoll(0f, 0f, (_movementWiggle.Roll + _mouseWiggle.Roll) * ((float)System.Math.PI / 32f) * wiggleWeights.Roll);
				identity *= Quaternion.CreateFromYawPitchRoll(0f, 0f, (_movementWiggle.Roll + _mouseWiggle.Roll) * ((float)System.Math.PI / 32f) * wiggleWeights.Roll);
			}
			if (wiggleWeights.Pitch != 0f)
			{
				_renderOrientation *= Quaternion.CreateFromYawPitchRoll(0f, (_movementWiggle.Pitch + _mouseWiggle.Pitch) * ((float)System.Math.PI / 32f) * wiggleWeights.Pitch, 0f);
				identity *= Quaternion.CreateFromYawPitchRoll(0f, (_movementWiggle.Pitch + _mouseWiggle.Pitch) * ((float)System.Math.PI / 32f) * wiggleWeights.Pitch, 0f);
			}
		}
		UpdateFirstPersonWeaponPullback(1f / 32f);
		Vector3 attachmentPosition = _gameInstance.CameraModule.Controller.AttachmentPosition;
		Vector3 position = _gameInstance.CameraModule.Controller.Position;
		Vector3 vector = attachmentPosition - position;
		Matrix.AddTranslation(ref _modelMatrix, _renderPosition.X, _renderPosition.Y, _renderPosition.Z);
		Matrix.CreateFromQuaternion(ref _renderOrientation, out _tempMatrix);
		Matrix.Multiply(ref _modelMatrix, ref _tempMatrix, out _modelMatrix);
		if (IsFirstPersonClipping())
		{
			Matrix.AddTranslation(ref _modelMatrix, vector.X, vector.Y, vector.Z);
		}
		for (int i = 0; i < _itemModelRendererCount; i++)
		{
			EntityItem entityItem = EntityItems[i];
			ref AnimatedRenderer.NodeTransform reference = ref _firstPersonModelRenderer.NodeTransforms[_itemModelDrawTasks[i].NodeIndex];
			Matrix.Compose(reference.Orientation, reference.Position, out var result);
			Matrix.Multiply(ref entityItem.RootOffsetMatrix, ref result, out result);
			Matrix.Multiply(ref result, ref _modelMatrix, out _itemModelDrawTasks[i].ModelMatrix);
			Matrix.ApplyScale(ref _itemModelDrawTasks[i].ModelMatrix, entityItem.Scale);
			_renderOrientation = identity;
		}
	}

	private void UpdateFirstPersonWeaponPullback(float offsetScaleFactor)
	{
		Vector3 value = Vector3.Zero;
		Vector3 value2 = Vector3.Zero;
		Vector3 value3 = Vector3.Zero;
		Vector3 value4 = Vector3.Zero;
		float num = 1f;
		ref ClientMovementStates relativeMovementStates = ref GetRelativeMovementStates();
		bool flag = !relativeMovementStates.IsClimbing && !relativeMovementStates.IsMantling;
		float distance;
		bool flag2 = IsApproachingObstacle(out distance) && distance <= _firstPersonWeaponDrawbackStartDistance;
		if (_gameInstance.App.Settings.WeaponPullback && flag2 && flag)
		{
			CalculateItemPullbackOffsets();
			num = (_firstPersonWeaponDrawbackStartDistance - distance) / _firstPersonWeaponDrawbackStartDistance;
			float num2 = num * 32f;
			value = new Vector3(_pullbackRightOffset.X * num2, _pullbackRightOffset.Y * num2, _pullbackRightOffset.Z * num2);
			value2 = new Vector3(_pullbackLeftOffset.X * num2, _pullbackLeftOffset.Y * num2, _pullbackLeftOffset.Z * num2);
			float num3 = (float)System.Math.PI * offsetScaleFactor;
			value3 = new Vector3(_pullbackRightRotation.X * num3, _pullbackRightRotation.Y * num3, _pullbackRightRotation.Z * num3);
			value4 = new Vector3(_pullbackLeftRotation.X * num3, _pullbackLeftRotation.Y * num3, _pullbackLeftRotation.Z * num3);
		}
		float amount = _gameInstance.DeltaTime * (num * 3.5f);
		if (_firstPersonArmRightIdx != -1)
		{
			ref BlockyModelNode reference = ref _baseFirstPersonModel.AllNodes[_firstPersonArmRightIdx];
			reference.ProceduralOffset = Vector3.Lerp(reference.ProceduralOffset, value, amount);
			reference.ProceduralRotation = Vector3.Lerp(reference.ProceduralRotation, value3, amount);
		}
		if (_firstPersonArmLeftIdx != -1)
		{
			ref BlockyModelNode reference2 = ref _baseFirstPersonModel.AllNodes[_firstPersonArmLeftIdx];
			reference2.ProceduralOffset = Vector3.Lerp(reference2.ProceduralOffset, value2, amount);
			reference2.ProceduralRotation = Vector3.Lerp(reference2.ProceduralRotation, value4, amount);
		}
	}

	private void CalculateItemPullbackOffsets()
	{
		ClientItemPullbackConfig slotPullbackConfig = _firstPersonModelRenderer.GetSlotPullbackConfig(1);
		ClientItemPullbackConfig slotPullbackConfig2 = _firstPersonModelRenderer.GetSlotPullbackConfig(2);
		if (_pullbackPrevPrimaryItem != base.PrimaryItem || _pullbackPrevSecondaryItem != base.SecondaryItem || slotPullbackConfig != _pullbackPrevPrimaryAnimConfig || slotPullbackConfig2 != _pullbackPrevSecondaryAnimConfig)
		{
			_pullbackPrevPrimaryItem = base.PrimaryItem;
			_pullbackPrevSecondaryItem = base.SecondaryItem;
			_pullbackPrevPrimaryAnimConfig = slotPullbackConfig;
			_pullbackPrevSecondaryAnimConfig = slotPullbackConfig2;
			ClientItemPullbackConfig clientItemPullbackConfig = base.PrimaryItem?.PullbackConfig;
			ClientItemPullbackConfig clientItemPullbackConfig2 = base.SecondaryItem?.PullbackConfig;
			if (IsFirstPersonClipping())
			{
				_pullbackRightOffset = Vector3.Zero;
				_pullbackRightRotation = Vector3.Zero;
				_pullbackLeftOffset = Vector3.Zero;
				_pullbackLeftRotation = Vector3.Zero;
			}
			else
			{
				_pullbackRightOffset = clientItemPullbackConfig?.RightOffsetOverride ?? slotPullbackConfig?.RightOffsetOverride ?? slotPullbackConfig2?.RightOffsetOverride ?? defaultPullbackOffsetRight;
				_pullbackRightRotation = clientItemPullbackConfig?.RightRotationOverride ?? clientItemPullbackConfig2?.RightRotationOverride ?? slotPullbackConfig?.RightRotationOverride ?? slotPullbackConfig2?.RightRotationOverride ?? defaultPullbackRotationRight;
				_pullbackLeftOffset = clientItemPullbackConfig?.LeftOffsetOverride ?? clientItemPullbackConfig2?.LeftOffsetOverride ?? slotPullbackConfig?.LeftOffsetOverride ?? slotPullbackConfig2?.LeftOffsetOverride ?? defaultPullbackOffsetLeft;
				_pullbackLeftRotation = clientItemPullbackConfig?.LeftRotationOverride ?? clientItemPullbackConfig2?.LeftRotationOverride ?? slotPullbackConfig?.LeftRotationOverride ?? slotPullbackConfig2?.LeftRotationOverride ?? defaultPullbackRotationLeft;
			}
		}
	}

	public void SendFirstPersonViewUniforms(Vector2 atlasSizeFactor0, Vector2 atlasSizeFactor1, Vector2 atlasSizeFactor2)
	{
		BlockyModelProgram blockyModelProgram = GetBlockyModelProgram();
		Debug.Assert(FirstPersonViewNeedsDrawing(), "SendFirstPersonViewUniforms called when it was not required. Please check with FirstPersonViewNeedsDrawing() first before calling this.");
		blockyModelProgram.AssertInUse();
		blockyModelProgram.StaticLightColor.SetValue(_gameInstance.LocalPlayer.BlockLightColor);
		blockyModelProgram.BottomTint.SetValue(_gameInstance.LocalPlayer.BottomTint);
		blockyModelProgram.TopTint.SetValue(_gameInstance.LocalPlayer.TopTint);
		blockyModelProgram.ModelVFXHighlightColorAndThickness.SetValue(ModelVFX.HighlightColor.X, ModelVFX.HighlightColor.Y, ModelVFX.HighlightColor.Z, ModelVFX.HighlightThickness);
		blockyModelProgram.ModelVFXNoiseParams.SetValue(ModelVFX.NoiseScale.X, ModelVFX.NoiseScale.Y, ModelVFX.NoiseScrollSpeed.X, ModelVFX.NoiseScrollSpeed.Y);
		blockyModelProgram.ModelVFXAnimationProgress.SetValue(ModelVFX.AnimationProgress);
		blockyModelProgram.ModelVFXPackedParams.SetValue(ModelVFX.PackedModelVFXParams);
		blockyModelProgram.ModelVFXPostColor.SetValue(ModelVFX.PostColor);
		blockyModelProgram.AtlasSizeFactor0.SetValue(atlasSizeFactor0);
		blockyModelProgram.AtlasSizeFactor1.SetValue(atlasSizeFactor1);
		blockyModelProgram.AtlasSizeFactor2.SetValue(atlasSizeFactor2);
	}

	public void DrawInFirstPersonView()
	{
		BlockyModelProgram blockyModelProgram = GetBlockyModelProgram();
		Debug.Assert(FirstPersonViewNeedsDrawing(), "DrawFirstPersonView called when it was not required. Please check with FirstPersonViewNeedsDrawing() first before calling this.");
		blockyModelProgram.AssertInUse();
		_gameInstance.Engine.Graphics.GL.AssertTextureBound(GL.TEXTURE0, _gameInstance.MapModule.TextureAtlas.GLTexture);
		_gameInstance.Engine.Graphics.GL.AssertTextureBound(GL.TEXTURE1, _gameInstance.EntityStoreModule.TextureAtlas.GLTexture);
		AnimationSystem animationSystem = _gameInstance.Engine.AnimationSystem;
		blockyModelProgram.ModelMatrix.SetValue(ref _modelMatrix);
		blockyModelProgram.NodeBlock.SetBufferRange(animationSystem.NodeBuffer, _firstPersonModelRenderer.NodeBufferOffset, (uint)(_firstPersonModelRenderer.NodeCount * 64));
		_firstPersonModelRenderer.Draw();
		for (int i = 0; i < _itemModelRendererCount; i++)
		{
			ClientModelVFX clientModelVFX = ((EntityItems[i].ModelVFX.Id != null) ? EntityItems[i].ModelVFX : ModelVFX);
			blockyModelProgram.ModelVFXHighlightColorAndThickness.SetValue(clientModelVFX.HighlightColor.X, clientModelVFX.HighlightColor.Y, clientModelVFX.HighlightColor.Z, clientModelVFX.HighlightThickness);
			blockyModelProgram.ModelVFXNoiseParams.SetValue(clientModelVFX.NoiseScale.X, clientModelVFX.NoiseScale.Y, clientModelVFX.NoiseScrollSpeed.X, clientModelVFX.NoiseScrollSpeed.Y);
			blockyModelProgram.ModelVFXAnimationProgress.SetValue(clientModelVFX.AnimationProgress);
			blockyModelProgram.ModelVFXPackedParams.SetValue(clientModelVFX.PackedModelVFXParams);
			blockyModelProgram.ModelVFXPostColor.SetValue(clientModelVFX.PostColor);
			blockyModelProgram.ModelMatrix.SetValue(ref _itemModelDrawTasks[i].ModelMatrix);
			blockyModelProgram.NodeBlock.SetBufferRange(animationSystem.NodeBuffer, EntityItems[i].ModelRenderer.NodeBufferOffset, (uint)(EntityItems[i].ModelRenderer.NodeCount * 64));
			EntityItems[i].ModelRenderer.Draw();
		}
	}

	public void DrawDistortionInFirstPersonView()
	{
		BlockyModelProgram firstPersonDistortionBlockyModelProgram = _gameInstance.Engine.Graphics.GPUProgramStore.FirstPersonDistortionBlockyModelProgram;
		RenderTargetStore rTStore = _gameInstance.Engine.Graphics.RTStore;
		Debug.Assert(FirstPersonViewNeedsDrawing(), "DrawInFirstPersonViewInDistortionBuffer called when it was not required. Please check with FirstPersonViewNeedsDrawing() first before calling this.");
		firstPersonDistortionBlockyModelProgram.AssertInUse();
		_gameInstance.Engine.Graphics.GL.AssertTextureBound(GL.TEXTURE0, _gameInstance.MapModule.TextureAtlas.GLTexture);
		_gameInstance.Engine.Graphics.GL.AssertTextureBound(GL.TEXTURE1, _gameInstance.EntityStoreModule.TextureAtlas.GLTexture);
		AnimationSystem animationSystem = _gameInstance.Engine.AnimationSystem;
		firstPersonDistortionBlockyModelProgram.ModelMatrix.SetValue(ref _modelMatrix);
		firstPersonDistortionBlockyModelProgram.ModelVFXNoiseParams.SetValue(ModelVFX.NoiseScale.X, ModelVFX.NoiseScale.Y, ModelVFX.NoiseScrollSpeed.X, ModelVFX.NoiseScrollSpeed.Y);
		firstPersonDistortionBlockyModelProgram.ModelVFXAnimationProgress.SetValue(ModelVFX.AnimationProgress);
		firstPersonDistortionBlockyModelProgram.ModelVFXPackedParams.SetValue(ModelVFX.PackedModelVFXParams);
		firstPersonDistortionBlockyModelProgram.CurrentInvViewportSize.SetValue(rTStore.Distortion.InvResolution);
		firstPersonDistortionBlockyModelProgram.NodeBlock.SetBufferRange(animationSystem.NodeBuffer, _firstPersonModelRenderer.NodeBufferOffset, (uint)(_firstPersonModelRenderer.NodeCount * 64));
		_firstPersonModelRenderer.Draw();
		for (int i = 0; i < _itemModelRendererCount; i++)
		{
			ClientModelVFX clientModelVFX = ((EntityItems[i].ModelVFX.Id != null) ? EntityItems[i].ModelVFX : ModelVFX);
			firstPersonDistortionBlockyModelProgram.ModelVFXNoiseParams.SetValue(clientModelVFX.NoiseScale.X, clientModelVFX.NoiseScale.Y, clientModelVFX.NoiseScrollSpeed.X, clientModelVFX.NoiseScrollSpeed.Y);
			firstPersonDistortionBlockyModelProgram.ModelVFXAnimationProgress.SetValue(clientModelVFX.AnimationProgress);
			firstPersonDistortionBlockyModelProgram.ModelVFXPackedParams.SetValue(clientModelVFX.PackedModelVFXParams);
			firstPersonDistortionBlockyModelProgram.ModelMatrix.SetValue(ref _itemModelDrawTasks[i].ModelMatrix);
			firstPersonDistortionBlockyModelProgram.NodeBlock.SetBufferRange(animationSystem.NodeBuffer, EntityItems[i].ModelRenderer.NodeBufferOffset, (uint)(EntityItems[i].ModelRenderer.NodeCount * 64));
			EntityItems[i].ModelRenderer.Draw();
		}
	}

	private void PrepareForDrawInOcclusionMap(SceneView cameraSceneView)
	{
		Debug.Assert(!FirstPersonViewNeedsDrawing(), "PrepareForDrawInOcclusionMap called when it was not required. Please check with FirstPersonViewNeedsDrawing() first before calling this.");
		float scale = 1f / 64f * Scale;
		Vector3 translation = RenderPosition - cameraSceneView.Position;
		Matrix.Compose(scale, RenderOrientation, translation, out _modelMatrix);
		UpdateModelLookOrientation();
		for (int i = 0; i < EntityItems.Count; i++)
		{
			EntityItem entityItem = EntityItems[i];
			ref AnimatedRenderer.NodeTransform reference = ref base.ModelRenderer.NodeTransforms[entityItem.TargetNodeIndex];
			Matrix.Compose(reference.Orientation, reference.Position, out var result);
			Matrix.Multiply(ref entityItem.RootOffsetMatrix, ref result, out result);
			Matrix.Multiply(ref result, ref _modelMatrix, out _itemModelDrawTasks[i].ModelMatrix);
			Matrix.ApplyScale(ref _itemModelDrawTasks[i].ModelMatrix, entityItem.Scale);
		}
	}

	private void DrawInOcclusionMap()
	{
		ZOnlyBlockyModelProgram blockyModelOcclusionMapProgram = _gameInstance.Engine.Graphics.GPUProgramStore.BlockyModelOcclusionMapProgram;
		Debug.Assert(!FirstPersonViewNeedsDrawing(), "DrawOcclusion called when it was not required. Please check with FirstPersonViewNeedsDrawing() first before calling this.");
		blockyModelOcclusionMapProgram.AssertInUse();
		_gameInstance.Engine.Graphics.GL.AssertTextureBound(GL.TEXTURE0, _gameInstance.MapModule.TextureAtlas.GLTexture);
		_gameInstance.Engine.Graphics.GL.AssertTextureBound(GL.TEXTURE1, _gameInstance.EntityStoreModule.TextureAtlas.GLTexture);
		blockyModelOcclusionMapProgram.DrawId.SetValue(-1);
		blockyModelOcclusionMapProgram.ModelMatrix.SetValue(ref _modelMatrix);
		float num = base.Hitbox.Max.Y * 64f;
		blockyModelOcclusionMapProgram.InvModelHeight.SetValue(1f / num);
		blockyModelOcclusionMapProgram.Time.SetValue(_gameInstance.SceneRenderer.Data.Time);
		blockyModelOcclusionMapProgram.ModelVFXAnimationProgress.SetValue(ModelVFX.AnimationProgress);
		blockyModelOcclusionMapProgram.ModelVFXId.SetValue(ModelVFX.IdInTBO);
		AnimationSystem animationSystem = _gameInstance.Engine.AnimationSystem;
		blockyModelOcclusionMapProgram.NodeBlock.SetBufferRange(animationSystem.NodeBuffer, base.ModelRenderer.NodeBufferOffset, (uint)(base.ModelRenderer.NodeCount * 64));
		base.ModelRenderer.Draw();
		for (int i = 0; i < _itemModelRendererCount; i++)
		{
			blockyModelOcclusionMapProgram.ModelMatrix.SetValue(ref _itemModelDrawTasks[i].ModelMatrix);
			blockyModelOcclusionMapProgram.NodeBlock.SetBufferRange(animationSystem.NodeBuffer, EntityItems[i].ModelRenderer.NodeBufferOffset, (uint)(EntityItems[i].ModelRenderer.NodeCount * 64));
			EntityItems[i].ModelRenderer.Draw();
		}
	}

	public void DrawOccluders(SceneView cameraSceneView)
	{
		GraphicsDevice graphics = _gameInstance.Engine.Graphics;
		ref SceneRenderer.SceneData data = ref _gameInstance.SceneRenderer.Data;
		if (_gameInstance.CameraModule.Controller.IsFirstPerson)
		{
			if (FirstPersonViewNeedsDrawing())
			{
				PrepareForDrawInFirstPersonView();
				BlockyModelProgram blockyModelProgram = GetBlockyModelProgram();
				graphics.GL.UseProgram(blockyModelProgram);
				blockyModelProgram.ViewProjectionMatrix.SetValue(ref data.FirstPersonProjectionMatrix);
				DrawInFirstPersonView();
			}
		}
		else
		{
			PrepareForDrawInOcclusionMap(cameraSceneView);
			ZOnlyBlockyModelProgram blockyModelOcclusionMapProgram = graphics.GPUProgramStore.BlockyModelOcclusionMapProgram;
			graphics.GL.UseProgram(blockyModelOcclusionMapProgram);
			blockyModelOcclusionMapProgram.ViewProjectionMatrix.SetValue(ref data.ViewRotationProjectionMatrix);
			DrawInOcclusionMap();
		}
	}

	protected override void ApplyItemAppearanceConditionParticles(ClientItemBase item, int itemEntityIndex, ClientItemAppearanceCondition condition, ClientItemAppearanceCondition.Data data, bool firstPerson)
	{
		base.ApplyItemAppearanceConditionParticles(item, itemEntityIndex, condition, data, firstPerson);
		bool isFirstPerson = _gameInstance.CameraModule.Controller.IsFirstPerson;
		for (int i = 0; i < data.EntityParticles.Length; i++)
		{
			data.EntityParticles[i]?.ParticleSystemProxy.SetFirstPerson(isFirstPerson);
		}
	}

	public override ref ClientMovementStates GetRelativeMovementStates()
	{
		return ref _gameInstance.CharacterControllerModule.MovementController.MovementStates;
	}

	public void UpdateActiveInteraction(int id, bool remove)
	{
		if (remove)
		{
			_activeInteractions.Remove(id);
		}
		else
		{
			_activeInteractions.Add(id);
		}
		_effectsOnEntityDirty = true;
	}

	private void UpdateStatsDependentOnChanges()
	{
		if (_effectsOnEntityDirty)
		{
			_movementEffects.DisableForward = false;
			_movementEffects.DisableBackward = false;
			_movementEffects.DisableLeft = false;
			_movementEffects.DisableRight = false;
			_movementEffects.DisableSprint = false;
			_movementEffects.DisableJump = false;
			_movementEffects.DisableCrouch = false;
			_disabledAbilities = new HashSet<InteractionType>();
			UpdateStatsDependentUponInteractionsIfNecessary();
			UpdateStatsDependentUponEffectsIfNecessary();
			_effectsOnEntityDirty = false;
		}
	}

	private void UpdateStatsDependentUponInteractionsIfNecessary()
	{
		foreach (int activeInteraction in _activeInteractions)
		{
			Interaction val = _gameInstance.InteractionModule.Interactions[activeInteraction]?.Interaction;
			MovementEffects movementEffects_ = val.Effects.MovementEffects_;
			MovementEffects movementEffects = _movementEffects;
			movementEffects.DisableForward |= movementEffects_.DisableForward;
			MovementEffects movementEffects2 = _movementEffects;
			movementEffects2.DisableBackward |= movementEffects_.DisableBackward;
			MovementEffects movementEffects3 = _movementEffects;
			movementEffects3.DisableLeft |= movementEffects_.DisableLeft;
			MovementEffects movementEffects4 = _movementEffects;
			movementEffects4.DisableRight |= movementEffects_.DisableRight;
			MovementEffects movementEffects5 = _movementEffects;
			movementEffects5.DisableSprint |= movementEffects_.DisableSprint;
			MovementEffects movementEffects6 = _movementEffects;
			movementEffects6.DisableJump |= movementEffects_.DisableJump;
			MovementEffects movementEffects7 = _movementEffects;
			movementEffects7.DisableCrouch |= movementEffects_.DisableCrouch;
		}
	}

	private void UpdateStatsDependentUponEffectsIfNecessary()
	{
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		float num = 1f;
		bool hasStaminaDepletedEffect = false;
		int num2 = _gameInstance.EntityStoreModule.EntityEffectIndicesByIds["Stamina_Broken"];
		for (int i = 0; i < EntityEffects.Length; i++)
		{
			ref UniqueEntityEffect reference = ref EntityEffects[i];
			if (reference.IsExpiring)
			{
				continue;
			}
			if (reference.NetworkEffectIndex == num2)
			{
				hasStaminaDepletedEffect = true;
			}
			EntityEffect val = _gameInstance.EntityStoreModule.EntityEffects[reference.NetworkEffectIndex];
			if (val.ApplicationEffects_ == null)
			{
				continue;
			}
			num *= val.ApplicationEffects_.HorizontalSpeedMultiplier;
			MovementEffects movementEffects_ = val.ApplicationEffects_.MovementEffects_;
			if (movementEffects_ != null)
			{
				MovementEffects movementEffects = _movementEffects;
				movementEffects.DisableForward |= movementEffects_.DisableForward;
				MovementEffects movementEffects2 = _movementEffects;
				movementEffects2.DisableBackward |= movementEffects_.DisableBackward;
				MovementEffects movementEffects3 = _movementEffects;
				movementEffects3.DisableLeft |= movementEffects_.DisableLeft;
				MovementEffects movementEffects4 = _movementEffects;
				movementEffects4.DisableRight |= movementEffects_.DisableRight;
				MovementEffects movementEffects5 = _movementEffects;
				movementEffects5.DisableSprint |= movementEffects_.DisableSprint;
				MovementEffects movementEffects6 = _movementEffects;
				movementEffects6.DisableJump |= movementEffects_.DisableJump;
				MovementEffects movementEffects7 = _movementEffects;
				movementEffects7.DisableCrouch |= movementEffects_.DisableCrouch;
			}
			AbilityEffects abilityEffects_ = val.ApplicationEffects_.AbilityEffects_;
			if (abilityEffects_?.Disabled != null)
			{
				InteractionType[] disabled = abilityEffects_.Disabled;
				foreach (InteractionType item in disabled)
				{
					_disabledAbilities.Add(item);
				}
			}
		}
		_horizontalSpeedMultiplier = num;
		_hasStaminaDepletedEffect = hasStaminaDepletedEffect;
	}

	public void OnSpeedMultipliersChanged(float multiplierDiff)
	{
		if (_currentAnimationId != null)
		{
			float speed = (GetAnimation(_currentAnimationId) ?? EntityAnimation.Empty).Speed;
			base.ModelRenderer.SetSlotAnimationSpeedMultiplier(0, speed + speed * multiplierDiff);
			float speed2 = GetItemAnimation(base.PrimaryItem, _currentAnimationId).Speed;
			base.ModelRenderer.SetSlotAnimationSpeedMultiplier(1, speed2 + speed2 * multiplierDiff);
			float speed3 = GetItemAnimation(base.SecondaryItem, _currentAnimationId, useDefaultAnimations: false).Speed;
			base.ModelRenderer.SetSlotAnimationSpeedMultiplier(2, speed3 + speed3 * multiplierDiff);
		}
	}

	public void SetFpAnimation(string animationId, EntityAnimation targetAnimation)
	{
		int slotIndex = 5;
		bool looping = targetAnimation.Looping;
		BlockyAnimation firstPersonAnimation = GetFirstPersonAnimation(targetAnimation);
		_firstPersonModelRenderer.SetSlotAnimation(slotIndex, firstPersonAnimation, looping, 1f, 0f, 12f, targetAnimation.PullbackConfig);
		CurrentFirstPersonAnimationId = animationId;
	}

	public bool IsApproachingObstacle(out float distance)
	{
		distance = _firstPersonObstacleDistance;
		return distance >= 0f;
	}

	private BlockyModelProgram GetBlockyModelProgram()
	{
		GPUProgramStore gPUProgramStore = _gameInstance.Engine.Graphics.GPUProgramStore;
		return IsFirstPersonClipping() ? gPUProgramStore.FirstPersonClippingBlockyModelProgram : gPUProgramStore.FirstPersonBlockyModelProgram;
	}

	public bool IsFirstPersonClipping()
	{
		Settings settings = _gameInstance.App.Settings;
		return (((base.PrimaryItem != null && base.PrimaryItem.ClipsGeometry) || (base.SecondaryItem != null && base.SecondaryItem.ClipsGeometry)) && settings.ItemsClipGeometry) || (animationClipsGeometry && settings.ItemAnimationsClipGeometry);
	}

	private BlockyAnimation GetFirstPersonAnimation(EntityAnimation animation)
	{
		if (_gameInstance.App.Settings.UseOverrideFirstPersonAnimations && animation.FirstPersonOverrideData != null)
		{
			return animation.FirstPersonOverrideData;
		}
		return animation.FirstPersonData;
	}

	public bool IsInteractionDisabled(InteractionType type)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		return DisabledAbilities?.Contains(type) ?? false;
	}

	public void UpdateItemStatModifiers(ClientItemBase newItem, ClientItemBase newSecondaryItem)
	{
		InventoryModule inventoryModule = _gameInstance.InventoryModule;
		bool flag = newItem != base.PrimaryItem || inventoryModule.HotbarActiveSlot != _lastHotbarSlot;
		bool flag2 = newSecondaryItem != base.SecondaryItem || inventoryModule.UtilityActiveSlot != _lastUtilitySlot;
		if (flag)
		{
			AddWeaponStatModifiers(base.PrimaryItem, "*Weapon_");
		}
		if (flag2)
		{
			AddUtilityStatModifiers(base.SecondaryItem, "*Utility_");
		}
		if (flag && base.PrimaryItem?.Weapon?.EntityStatsToClear != null)
		{
			for (int i = 0; i < base.PrimaryItem.Weapon.EntityStatsToClear.Length; i++)
			{
				int index = base.PrimaryItem.Weapon.EntityStatsToClear[i];
				MinimizeStatValue(index);
			}
		}
		if (flag2 && base.SecondaryItem?.Utility?.EntityStatsToClear != null)
		{
			for (int j = 0; j < base.SecondaryItem.Utility.EntityStatsToClear.Length; j++)
			{
				int index2 = base.SecondaryItem.Utility.EntityStatsToClear[j];
				MinimizeStatValue(index2);
			}
		}
		_lastHotbarSlot = inventoryModule.HotbarActiveSlot;
		_lastUtilitySlot = inventoryModule.UtilityActiveSlot;
	}

	public void AddWeaponStatModifiers(ClientItemBase item, string prefix)
	{
		Dictionary<int, Modifier[]> dictionary = item?.Weapon?.StatModifiers;
		if (dictionary == null)
		{
			ClearAllStatModifiers(prefix, null);
		}
		else
		{
			AddItemStatModifiers(dictionary, prefix);
		}
	}

	public void AddUtilityStatModifiers(ClientItemBase item, string prefix)
	{
		Dictionary<int, Modifier[]> dictionary = item?.Utility?.StatModifiers;
		if (dictionary == null)
		{
			ClearAllStatModifiers(prefix, null);
		}
		else
		{
			AddItemStatModifiers(dictionary, prefix);
		}
	}

	private void AddItemStatModifiers(Dictionary<int, Modifier[]> itemStatModifiers, string prefix)
	{
		foreach (KeyValuePair<int, Modifier[]> itemStatModifier in itemStatModifiers)
		{
			int num = 0;
			int key = itemStatModifier.Key;
			for (int i = 0; i < itemStatModifier.Value.Length; i++)
			{
				Modifier val = itemStatModifier.Value[i];
				string key2 = $"{prefix}{num}";
				num++;
				if (!GetStatModifier(key, key2, out var modifier) || !((object)modifier).Equals((object?)val))
				{
					PutStatModifier(key, key2, val);
				}
			}
			ClearStatModifiers(key, prefix, num);
		}
		ClearAllStatModifiers(prefix, itemStatModifiers);
	}

	private void ClearAllStatModifiers(string prefix, Dictionary<int, Modifier[]> excluding)
	{
		for (int i = 0; i < _entityStats.Length; i++)
		{
			if (excluding == null || !excluding.ContainsKey(i))
			{
				ClearStatModifiers(i, prefix, 0);
			}
		}
	}

	private void ClearStatModifiers(int statIndex, string prefix, int offset)
	{
		string key;
		EntityStatUpdate update;
		do
		{
			key = $"{prefix}{offset}";
			offset++;
		}
		while (RemoveModifier(statIndex, key, out update));
	}
}
