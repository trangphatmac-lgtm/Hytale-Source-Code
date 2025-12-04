using System;
using HytaleClient.InGame.Modules.CharacterController;
using HytaleClient.InGame.Modules.Collision;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using HytaleClient.Protocol;
using SDL2;

namespace HytaleClient.InGame.Modules.Camera.Controllers;

internal class ThirdPersonCameraController : ICameraController
{
	private static readonly CollisionModule.BlockOptions ThirdPersonCameraRaycastOptions = new CollisionModule.BlockOptions
	{
		IgnoreEmptyCollisionMaterial = true,
		IgnoreFluids = true
	};

	private const float CollisionPadding = 0.0001f;

	private const float EdgePadding = 0.01f;

	private const float TransitionCancellationMovePadding = 0.01f;

	private const float TransitionCancellationLookPadding = 0.001f;

	private const float FirstPersonDistanceMinDistance = 0.5f;

	private const float HitboxHalfSize = 0.15f;

	private const float MinLookDistance = 5f;

	private Entity _entity;

	protected Vector3 _rotation;

	protected Vector3 _lookAt;

	protected Vector3 _headLookAt;

	protected Vector3 _transitionLookAt;

	private readonly GameInstance _gameInstance;

	internal Vector3 _rotatedPositionOffset;

	internal float _horizontalCollisionDistanceOffset;

	internal float _verticalCollisionDistanceOffset;

	private bool _isFirstPersonOverride = false;

	internal bool _inTransition = false;

	private readonly BoundingBox _hitbox = new BoundingBox(new Vector3(-0.15f), new Vector3(0.15f));

	public float SpeedModifier { get; } = 1f;


	public bool AllowPitchControls => false;

	public bool DisplayCursor => false;

	public virtual bool DisplayReticle => true;

	public bool SkipCharacterPhysics => false;

	public virtual bool IsFirstPerson => _isFirstPersonOverride;

	public virtual bool InteractFromEntity => true;

	public virtual Vector3 MovementForceRotation => _gameInstance.LocalPlayer.GetRelativeMovementStates().IsMounting ? _gameInstance.CharacterControllerModule.MovementController.CameraRotation : Rotation;

	public Entity AttachedTo
	{
		get
		{
			return _entity ?? _gameInstance.LocalPlayer;
		}
		set
		{
			_entity = value;
		}
	}

	public Vector3 AttachmentPosition { get; private set; }

	public Vector3 PositionOffset { get; set; }

	public Vector3 RotationOffset => Vector3.Zero;

	public Vector3 Position => AttachmentPosition + _rotatedPositionOffset + _gameInstance.CameraModule.CameraShakeController.Offset + _gameInstance.CharacterControllerModule.MovementController.MantleCameraOffset;

	public Vector3 Rotation => _rotation + _gameInstance.CameraModule.CameraShakeController.Rotation + _gameInstance.CharacterControllerModule.MovementController.ThirdPersonRotationOffset;

	public Vector3 LookAt => _lookAt;

	public bool CanMove => _entity == null || _entity == _gameInstance.LocalPlayer;

	public virtual bool ApplyHeadRotation => true;

	public ThirdPersonCameraController(GameInstance gameInstance)
	{
		_gameInstance = gameInstance;
	}

	public virtual void Reset(GameInstance gameInstance, ICameraController previousCameraController)
	{
		if (AttachedTo == null)
		{
			PositionOffset = Vector3.Zero;
		}
		else
		{
			PositionOffset = new Vector3(AttachedTo.CameraSettings.PositionOffset.X, AttachedTo.CameraSettings.PositionOffset.Y, AttachedTo.CameraSettings.PositionOffset.Z);
		}
		if (previousCameraController != null && previousCameraController is FirstPersonCameraController)
		{
			_horizontalCollisionDistanceOffset = (_verticalCollisionDistanceOffset = 0f);
			_rotatedPositionOffset = Vector3.Zero;
			_transitionLookAt = previousCameraController.LookAt;
			LookAtPosition(_transitionLookAt);
			Quaternion rotation = Quaternion.CreateFromYawPitchRoll(Rotation.Y, -1.5607964f, 0f);
			Vector3 value = PositionOffset;
			Vector3.Transform(ref value, ref rotation, out var result);
			Vector2 vector = new Vector2(_transitionLookAt.X, _transitionLookAt.Z);
			Vector2 vector2 = new Vector2(AttachedTo.Position.X, AttachedTo.Position.Z);
			Vector2 vector3 = new Vector2(result.X, result.Z);
			Vector2 vector4 = vector - vector2;
			float num = vector3.Length() + 0.2f;
			if (vector4.Length() < num)
			{
				vector4.Normalize();
				Vector2 vector5 = vector4 * num;
				_transitionLookAt.X = vector2.X + vector5.X;
				_transitionLookAt.Z = vector2.Y + vector5.Y;
			}
			_lookAt = _transitionLookAt;
			_inTransition = true;
		}
	}

	public void Update(float deltaTime)
	{
		Vector3 rotation = Rotation;
		rotation.Pitch = MathHelper.Clamp(rotation.Pitch, -1.5607964f, 1.5607964f);
		Quaternion rotation2 = Quaternion.CreateFromYawPitchRoll(rotation.Yaw, rotation.Pitch, rotation.Roll);
		Vector3 direction = Vector3.Transform(Vector3.Forward, rotation2);
		float num = (_inTransition ? 0.2f : 0.1f);
		num = MathHelper.Min(num / (1f / 60f) * deltaTime, 1f);
		Vector3 vector = Vector3.Transform(new Vector3(PositionOffset.X, 0f, 0f), rotation2);
		float x = PositionOffset.X;
		_rotatedPositionOffset = new Vector3(0f, PositionOffset.Y, 0f);
		if (x != 0f)
		{
			CollisionModule.BlockRaycastOptions options = CollisionModule.BlockRaycastOptions.Default;
			options.Block = ThirdPersonCameraRaycastOptions;
			Vector3 vector2 = Vector3.Normalize(vector);
			Ray ray = new Ray(Position, vector2);
			_horizontalCollisionDistanceOffset = MathHelper.Lerp(_horizontalCollisionDistanceOffset, x, num);
			options.RaycastOptions.Distance = _horizontalCollisionDistanceOffset;
			float maxPossibleOffset = GetMaxPossibleOffset(ref ray, ref options);
			_horizontalCollisionDistanceOffset = System.Math.Min(_horizontalCollisionDistanceOffset, maxPossibleOffset);
			vector = vector2 * _horizontalCollisionDistanceOffset;
		}
		_rotatedPositionOffset = new Vector3(0f, PositionOffset.Y, 0f) + vector;
		Vector3 vector3 = Vector3.Transform(new Vector3(0f, 0f, PositionOffset.Z), rotation2);
		float z = PositionOffset.Z;
		if (z != 0f)
		{
			CollisionModule.BlockRaycastOptions options2 = CollisionModule.BlockRaycastOptions.Default;
			options2.Block = ThirdPersonCameraRaycastOptions;
			Vector3 vector4 = Vector3.Normalize(vector3);
			Ray ray2 = new Ray(Position, vector4);
			_verticalCollisionDistanceOffset = MathHelper.Lerp(_verticalCollisionDistanceOffset, z, num);
			options2.RaycastOptions.Distance = _verticalCollisionDistanceOffset;
			float maxPossibleOffset2 = GetMaxPossibleOffset(ref ray2, ref options2);
			_verticalCollisionDistanceOffset = System.Math.Min(_verticalCollisionDistanceOffset, maxPossibleOffset2);
			vector3 = vector4 * _verticalCollisionDistanceOffset;
		}
		_rotatedPositionOffset = new Vector3(0f, PositionOffset.Y, 0f) + vector + vector3;
		if (_inTransition)
		{
			LookAtPosition(_transitionLookAt);
		}
		_isFirstPersonOverride = _rotatedPositionOffset.Length() < 0.5f;
		if (_isFirstPersonOverride)
		{
			_rotatedPositionOffset = Vector3.Zero;
		}
		if (ApplyHeadRotation)
		{
			Ray ray3 = new Ray(Position, direction);
			CollisionModule.CombinedOptions options3 = CollisionModule.CombinedOptions.Default;
			options3.Block = ThirdPersonCameraRaycastOptions;
			if (_gameInstance.CollisionModule.FindNearestTarget(ref ray3, ref options3, out var blockResult, out var entityResult))
			{
				Raycast.Result result;
				if (blockResult.HasValue)
				{
					result = blockResult.Value.Result;
				}
				else
				{
					if (!entityResult.HasValue)
					{
						throw new InvalidOperationException();
					}
					result = entityResult.Value.Result;
				}
				_lookAt = result.GetTarget();
				_headLookAt = ((result.NearT < 5f) ? ray3.GetAt(5f) : _lookAt);
			}
			else
			{
				_headLookAt = (_lookAt = ray3.GetAt(options3.RaycastOptions.Distance));
			}
			_gameInstance.LocalPlayer.LookAt(_headLookAt, _gameInstance.CharacterControllerModule.MovementController.MovementStates.IsIdle ? 0.2f : 0.5f);
		}
		UpdateAttachmentPosition(deltaTime);
	}

	private void UpdateAttachmentPosition(float deltaTime)
	{
		if (_entity != null)
		{
			AttachmentPosition = _entity.RenderPosition + GetEyeOffsetVector();
			return;
		}
		MovementController movementController = _gameInstance.CharacterControllerModule.MovementController;
		Vector3 value = new Vector3(movementController.ThirdPersonPositionOffset.X, movementController.ThirdPersonPositionOffset.Y, movementController.ThirdPersonPositionOffset.Z);
		Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, _rotation.Y);
		Vector3.Transform(ref value, ref rotation, out value);
		AttachmentPosition = AttachedTo.RenderPosition + GetEyeOffsetVector() + value;
		_gameInstance.CameraModule.CameraShakeController.Update(deltaTime, rotation);
	}

	public void ApplyMove(Vector3 movementOffset)
	{
		Vector3 position = _gameInstance.LocalPlayer.Position;
		_gameInstance.CharacterControllerModule.MovementController.ApplyMovementOffset(movementOffset);
		if (_inTransition && Vector3.Distance(position, _gameInstance.LocalPlayer.Position) > 0.01f)
		{
			_inTransition = false;
		}
	}

	public virtual void ApplyLook(float deltaTime, Vector2 lookOffset)
	{
		_rotation.Pitch = MathHelper.Clamp(_rotation.Pitch + lookOffset.X, -1.5607964f, 1.5607964f);
		_rotation.Yaw = MathHelper.WrapAngle(_rotation.Yaw + lookOffset.Y);
		if (_inTransition && (lookOffset.X > 0.001f || lookOffset.X < -0.001f || lookOffset.Y > 0.001f || lookOffset.Y < -0.001f))
		{
			_inTransition = false;
		}
	}

	public void SetRotation(Vector3 rotation)
	{
		_rotation = rotation;
		_inTransition = false;
	}

	public void OnMouseInput(SDL_Event evt)
	{
	}

	protected float GetEyeOffset()
	{
		Entity attachedTo = AttachedTo;
		if (attachedTo == null)
		{
			return 0f;
		}
		if (attachedTo != _gameInstance.LocalPlayer)
		{
			return attachedTo.EyeOffset;
		}
		CharacterControllerModule characterControllerModule = _gameInstance.CharacterControllerModule;
		return attachedTo.EyeOffset + characterControllerModule.MovementController.CrouchHeightShift;
	}

	private Vector3 GetEyeOffsetVector()
	{
		return new Vector3(0f, GetEyeOffset(), 0f);
	}

	public Vector3 GetHitboxSize()
	{
		return AttachedTo?.Hitbox.GetSize() ?? Vector3.Zero;
	}

	private void LookAtPosition(Vector3 relativePosition, float interpolation = 1f)
	{
		relativePosition -= Position;
		if (!MathHelper.WithinEpsilon(relativePosition.X, 0f) || !MathHelper.WithinEpsilon(relativePosition.Z, 0f))
		{
			float angle = (float)System.Math.Atan2(0f - relativePosition.X, 0f - relativePosition.Z);
			angle = MathHelper.WrapAngle(angle);
			_rotation.Yaw = MathHelper.WrapAngle(MathHelper.LerpAngle(_rotation.Yaw, angle, interpolation));
		}
		float num = relativePosition.Length();
		if (num > 0f)
		{
			float value = (float)System.Math.PI / 2f - (float)System.Math.Acos(relativePosition.Y / num);
			value = MathHelper.Clamp(value, -1.5607964f, 1.5607964f);
			_rotation.Pitch = MathHelper.Clamp(MathHelper.LerpAngle(_rotation.Pitch, value, interpolation), -1.5607964f, 1.5607964f);
		}
	}

	private float GetMaxPossibleOffset(ref Ray ray, ref CollisionModule.BlockRaycastOptions options, float horizontalScale = 1f)
	{
		float num = options.RaycastOptions.Distance;
		CollisionModule.BlockResult result = CollisionModule.BlockResult.Default;
		CollisionModule.CollisionHitData hitData;
		if (_gameInstance.CollisionModule.FindTargetBlock(ref ray, ref options, ref result))
		{
			Vector3 vector = result.Result.GetTarget();
			bool flag = CheckCollision(vector, ray.Direction, (Axis)1, horizontalScale, out hitData);
			num = result.Result.NearT;
			if (flag)
			{
				while (flag && num > 0f)
				{
					num -= 0.1f;
					vector = ray.GetAt(num);
					flag = CheckCollision(vector, ray.Direction, (Axis)1, horizontalScale, out hitData);
				}
				if (num <= 0f)
				{
					vector = ray.Position;
					num = 0f;
				}
				if (VolumeCast(vector, ray.Direction, options.RaycastOptions.Distance, horizontalScale, out var outDistance))
				{
					num += outDistance;
				}
			}
		}
		else
		{
			Vector3 vector2 = ray.GetAt(num);
			bool flag2 = CheckCollision(vector2, ray.Direction, (Axis)1, horizontalScale, out hitData);
			if (flag2)
			{
				while (flag2 && num > 0f)
				{
					num -= 0.1f;
					vector2 = ray.GetAt(num);
					flag2 = CheckCollision(vector2, ray.Direction, (Axis)1, horizontalScale, out hitData);
				}
				if (num <= 0f)
				{
					vector2 = ray.Position;
					num = 0f;
				}
				if (VolumeCast(vector2, ray.Direction, options.RaycastOptions.Distance, horizontalScale, out var outDistance2))
				{
					num += outDistance2;
				}
			}
		}
		return num;
	}

	private bool VolumeCast(Vector3 origin, Vector3 direction, float distance, float horizontalScale, out float outDistance)
	{
		Vector3 position = origin;
		float num = 0.15f * horizontalScale;
		int num2 = 0;
		float num3 = 0f;
		outDistance = distance;
		while (num3 < distance && num2 < 5000)
		{
			bool flag = false;
			num2++;
			num3 = MathHelper.Min(num3 + 0.1f, distance);
			Vector3 vector = origin + direction * num3;
			position.Y = vector.Y;
			if (CheckCollision(position, direction, (Axis)1, horizontalScale, out var hitData))
			{
				Vector3 zero = Vector3.Zero;
				if (direction.Y < 0f)
				{
					zero.Y = hitData.Limit.Y + 0.15f + 0.0001f;
				}
				else
				{
					zero.Y = hitData.Limit.Y - 0.15f - 0.0001f;
				}
				if (CollisionModule.CheckRayPlaneDistance(zero, new Vector3(0f, 1f, 0f), origin, direction, out var distance2))
				{
					outDistance = MathHelper.Min(outDistance, distance2);
				}
				position.Y = zero.Y;
				flag = true;
			}
			position.Z = vector.Z;
			if (CheckCollision(position, direction, (Axis)2, horizontalScale, out hitData))
			{
				Vector3 zero2 = Vector3.Zero;
				if (direction.Z < 0f)
				{
					zero2.Z = hitData.Limit.Z + num + 0.0001f;
				}
				else
				{
					zero2.Z = hitData.Limit.Z - num - 0.0001f;
				}
				if (CollisionModule.CheckRayPlaneDistance(zero2, new Vector3(0f, 0f, 1f), origin, direction, out var distance3))
				{
					outDistance = MathHelper.Min(outDistance, distance3);
				}
				position.Z = zero2.Z;
				flag = true;
			}
			position.X = vector.X;
			if (CheckCollision(position, direction, (Axis)0, horizontalScale, out hitData))
			{
				Vector3 zero3 = Vector3.Zero;
				if (direction.X < 0f)
				{
					zero3.X = hitData.Limit.X + num + 0.0001f;
				}
				else
				{
					zero3.X = hitData.Limit.X - num - 0.0001f;
				}
				if (CollisionModule.CheckRayPlaneDistance(zero3, new Vector3(1f, 0f, 0f), origin, direction, out var distance4))
				{
					outDistance = MathHelper.Min(outDistance, distance4);
				}
				position.X = zero3.X;
				flag = true;
			}
			if (flag)
			{
				outDistance = MathHelper.Max(0f, outDistance);
				return true;
			}
		}
		return false;
	}

	private bool CheckCollision(Vector3 position, Vector3 moveOffset, Axis axis, float horizontalScale, out CollisionModule.CollisionHitData hitData)
	{
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Expected I4, but got Unknown
		BoundingBox hitbox = _hitbox;
		if (horizontalScale != 1f)
		{
			hitbox.Min.X *= horizontalScale;
			hitbox.Min.Z *= horizontalScale;
			hitbox.Max.X *= horizontalScale;
			hitbox.Max.Z *= horizontalScale;
		}
		hitbox.Translate(position);
		float num = 0.15f * horizontalScale;
		double num2 = System.Math.Abs((double)position.X - System.Math.Truncate(position.X));
		int num3 = 0;
		int num4 = (int)System.Math.Floor(position.X);
		if (num2 < (double)num || num2 > (double)(1f - num))
		{
			num3 = 1;
			num4 = (int)System.Math.Round(position.X) - 1;
		}
		double num5 = System.Math.Abs((double)position.Y - System.Math.Truncate(position.Y));
		int num6 = 0;
		int num7 = (int)System.Math.Floor(position.Y);
		if (num5 < (double)num || num5 > (double)(1f - num))
		{
			num6 = 1;
			num7 = (int)System.Math.Round(position.Y) - 1;
		}
		double num8 = System.Math.Abs((double)position.Z - System.Math.Truncate(position.Z));
		int num9 = 0;
		int num10 = (int)System.Math.Floor(position.Z);
		if (num8 < (double)num || num8 > (double)(1f - num))
		{
			num9 = 1;
			num10 = (int)System.Math.Round(position.Z) - 1;
		}
		int num11 = 0;
		int num12 = 0;
		int num13 = 0;
		hitData = default(CollisionModule.CollisionHitData);
		float num14 = 0f;
		for (int i = 0; i <= num6; i++)
		{
			num12 = num7 + i;
			for (int j = 0; j <= num9; j++)
			{
				num13 = num10 + j;
				for (int k = 0; k <= num3; k++)
				{
					num11 = num4 + k;
					if (_gameInstance.CollisionModule.CheckBlockCollision(new IntVector3(num11, num12, num13), hitbox, moveOffset, out var hitData2))
					{
						float num15 = 0f;
						switch ((int)axis)
						{
						case 0:
							num15 = hitData2.Overlap.X;
							break;
						case 1:
							num15 = hitData2.Overlap.Y;
							break;
						case 2:
							num15 = hitData2.Overlap.Z;
							break;
						}
						if (num14 == 0f || num15 > num14)
						{
							hitData = hitData2;
							num14 = num15;
						}
					}
				}
			}
		}
		return num14 > 0f;
	}
}
