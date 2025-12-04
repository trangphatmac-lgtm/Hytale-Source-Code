using System;
using System.Collections.Generic;
using Hypixel.ProtoPlus;
using HytaleClient.InGame.Modules.CharacterController;
using HytaleClient.InGame.Modules.Collision;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using SDL2;

namespace HytaleClient.InGame.Modules.Camera.Controllers;

internal class ServerCameraController : ICameraController
{
	private ServerCameraSettings _cameraSettings;

	private Vector3 _customMovementForceRotation;

	private Entity _cachedEntity;

	private Vector3 _customPositionOffset;

	private Vector3 _positionDistanceOffset;

	private Vector3 _customPosition;

	private Vector3 _customRotation;

	private readonly GameInstance _gameInstance;

	private float _smoothCameraDistance;

	private float _itemWiggleTickAccumulator;

	private Vector2 _itemWiggleAmountAccumulator;

	public ServerCameraSettings CameraSettings
	{
		get
		{
			return _cameraSettings;
		}
		set
		{
			_cameraSettings = value;
			UpdateCameraSettings();
		}
	}

	public float Distance => _cameraSettings.Distance;

	public float SpeedModifier => _cameraSettings.SpeedModifier;

	public bool AllowPitchControls => _cameraSettings.AllowPitchControls;

	public bool DisplayCursor => _cameraSettings.DisplayCursor;

	public bool DisplayReticle => _cameraSettings.DisplayReticle;

	public bool SkipCharacterPhysics => _cameraSettings.SkipCharacterPhysics;

	public bool IsFirstPerson => _cameraSettings.IsFirstPerson;

	public bool InteractFromEntity => false;

	public Vector3 MovementForceRotation
	{
		get
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Expected I4, but got Unknown
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			MovementForceRotationType movementForceRotationType_ = _cameraSettings.MovementForceRotationType_;
			MovementForceRotationType val = movementForceRotationType_;
			return (int)val switch
			{
				0 => AttachedTo.LookOrientation, 
				1 => Rotation, 
				2 => _customMovementForceRotation, 
				_ => throw new ArgumentOutOfRangeException($"Unknown MovementForceRotationType {_cameraSettings.MovementForceRotationType_}"), 
			};
		}
	}

	public Entity AttachedTo
	{
		get
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Expected I4, but got Unknown
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			AttachedToType attachedToType_ = _cameraSettings.AttachedToType_;
			AttachedToType val = attachedToType_;
			switch ((int)val)
			{
			case 0:
				return _gameInstance.LocalPlayer;
			case 1:
				if (_cachedEntity != null && _cachedEntity.NetworkId == _cameraSettings.AttachedToEntityId)
				{
					return _cachedEntity;
				}
				return _cachedEntity = _gameInstance.EntityStoreModule.GetEntity(_cameraSettings.AttachedToEntityId);
			case 2:
				return null;
			default:
				throw new ArgumentOutOfRangeException($"Unknown AttachedToType {_cameraSettings.AttachedToType_}");
			}
		}
	}

	public Vector3 AttachmentPosition
	{
		get
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Invalid comparison between Unknown and I4
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Invalid comparison between Unknown and I4
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			AttachedToType attachedToType_ = _cameraSettings.AttachedToType_;
			AttachedToType val = attachedToType_;
			if ((int)val > 1)
			{
				if ((int)val == 2)
				{
					return Vector3.Zero;
				}
				throw new ArgumentOutOfRangeException($"Unknown AttachedToType {_cameraSettings.AttachedToType_}");
			}
			return AttachedTo.Position + GetEyeOffset();
		}
	}

	public Vector3 PositionOffset => _customPositionOffset + _positionDistanceOffset;

	public Vector3 RotationOffset { get; private set; }

	public Vector3 Position { get; private set; }

	public Vector3 Rotation { get; private set; }

	public Vector3 LookAt { get; private set; }

	public bool CanMove
	{
		get
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Invalid comparison between Unknown and I4
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			CanMoveType canMoveType_ = _cameraSettings.CanMoveType_;
			CanMoveType val = canMoveType_;
			if ((int)val != 0)
			{
				if ((int)val == 1)
				{
					return true;
				}
				throw new ArgumentOutOfRangeException($"Unknown CanMoveType {_cameraSettings.CanMoveType_}");
			}
			return AttachedTo == _gameInstance.LocalPlayer;
		}
	}

	private void UpdateCameraSettings()
	{
		if (_cameraSettings.MovementForceRotation != null)
		{
			_customMovementForceRotation = new Vector3(_cameraSettings.MovementForceRotation.Pitch, _cameraSettings.MovementForceRotation.Yaw, _cameraSettings.MovementForceRotation.Roll);
		}
		else
		{
			_customMovementForceRotation = Vector3.Zero;
		}
		if (_cameraSettings.PositionOffset != null)
		{
			_customPositionOffset = new Vector3((float)_cameraSettings.PositionOffset.X, (float)_cameraSettings.PositionOffset.Y, (float)_cameraSettings.PositionOffset.Z);
		}
		else
		{
			_customPositionOffset = Vector3.Zero;
		}
		if (_cameraSettings.RotationOffset != null)
		{
			RotationOffset = new Vector3(_cameraSettings.RotationOffset.Pitch, _cameraSettings.RotationOffset.Yaw, _cameraSettings.RotationOffset.Roll);
		}
		else
		{
			RotationOffset = Vector3.Zero;
		}
		if (_cameraSettings.Position_ != null)
		{
			_customPosition = new Vector3((float)_cameraSettings.Position_.X, (float)_cameraSettings.Position_.Y, (float)_cameraSettings.Position_.Z);
		}
		else
		{
			_customPosition = Vector3.Zero;
		}
		if (_cameraSettings.Rotation != null)
		{
			_customRotation = new Vector3(_cameraSettings.Rotation.Pitch, _cameraSettings.Rotation.Yaw, _cameraSettings.Rotation.Roll);
		}
		else
		{
			_customRotation = Vector3.Zero;
		}
	}

	public ServerCameraController(GameInstance gameInstance, ServerCameraSettings cameraSettings)
	{
		_gameInstance = gameInstance;
		CameraSettings = cameraSettings;
	}

	public void Reset(GameInstance gameInstance, ICameraController previousCameraController)
	{
		if (_gameInstance.LocalPlayer != null)
		{
			Position = previousCameraController.Position;
			Rotation = previousCameraController.Rotation;
		}
	}

	public void OnWorldJoined()
	{
		Position = GetPosition();
		Rotation = GetRotation();
	}

	public void Update(float deltaTime)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Invalid comparison between Unknown and I4
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Invalid comparison between Unknown and I4
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		PositionDistanceOffsetType positionDistanceOffsetType_ = _cameraSettings.PositionDistanceOffsetType_;
		PositionDistanceOffsetType val = positionDistanceOffsetType_;
		if ((int)val > 1)
		{
			if ((int)val != 2)
			{
				throw new ArgumentOutOfRangeException($"Unknown PositionDistanceOffsetType {_cameraSettings.PositionDistanceOffsetType_}");
			}
			_positionDistanceOffset = Vector3.Zero;
		}
		else
		{
			Vector3 position = Position;
			Vector3 rotation = Rotation;
			Quaternion rotation2 = Quaternion.CreateFromYawPitchRoll(rotation.Y, rotation.X, 0f);
			Vector3 vector = Vector3.Transform(Vector3.Forward, rotation2);
			float amount = 12f * deltaTime;
			Ray ray = new Ray(position, -vector);
			CollisionModule.BlockRaycastOptions options = CollisionModule.BlockRaycastOptions.Default;
			options.Block.IgnoreFluids = true;
			options.Block.IgnoreEmptyCollisionMaterial = true;
			Vector3 vector2 = Vector3.Zero;
			if ((!_gameInstance.CharacterControllerModule.MovementController.SkipHitDetectionWhenFlying || !_gameInstance.CharacterControllerModule.MovementController.MovementStates.IsFlying) && !SkipCharacterPhysics && (int)_cameraSettings.PositionDistanceOffsetType_ == 1 && _gameInstance.CollisionModule.FindTargetBlockOut(ref ray, ref options, out var result))
			{
				float num = MathHelper.Clamp(result.Result.Distance() - 1f, 0f, Distance);
				if (num < _smoothCameraDistance)
				{
					_smoothCameraDistance = num;
				}
				else
				{
					_smoothCameraDistance = MathHelper.Lerp(_smoothCameraDistance, num, amount);
				}
				if (_smoothCameraDistance < Distance)
				{
					vector2 = (Vector3)result.BlockNormal * 0.01f;
				}
			}
			else
			{
				_smoothCameraDistance = MathHelper.Lerp(_smoothCameraDistance, Distance, amount);
			}
			_positionDistanceOffset = -vector * _smoothCameraDistance + vector2;
		}
		Position = Vector3.Lerp(Position, GetPosition(), _cameraSettings.PositionLerpSpeed / (1f / 60f) * deltaTime);
		Rotation = Vector3.LerpAngle(Rotation, GetRotation(), _cameraSettings.RotationLerpSpeed / (1f / 60f) * deltaTime);
	}

	public void ApplyMove(Vector3 movementOffset)
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Invalid comparison between Unknown and I4
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		if (_cameraSettings.MovementMultiplier != null)
		{
			movementOffset.X *= _cameraSettings.MovementMultiplier.X;
			movementOffset.Y *= _cameraSettings.MovementMultiplier.Y;
			movementOffset.Z *= _cameraSettings.MovementMultiplier.Z;
		}
		ApplyMovementType applyMovementType_ = _cameraSettings.ApplyMovementType_;
		ApplyMovementType val = applyMovementType_;
		if ((int)val != 0)
		{
			if ((int)val != 1)
			{
				throw new ArgumentOutOfRangeException($"Unknown ApplyMovementType {_cameraSettings.ApplyMovementType_}");
			}
			_customPosition += movementOffset;
		}
		else
		{
			_gameInstance.CharacterControllerModule.MovementController.ApplyMovementOffset(movementOffset);
		}
	}

	public void ApplyLook(float deltaTime, Vector2 lookOffset)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Invalid comparison between Unknown and I4
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		if (_cameraSettings.LookMultiplier != null)
		{
			lookOffset.X *= _cameraSettings.LookMultiplier.X;
			lookOffset.Y *= _cameraSettings.LookMultiplier.Y;
		}
		ApplyLookType applyLookType_ = _cameraSettings.ApplyLookType_;
		ApplyLookType val = applyLookType_;
		if ((int)val != 0)
		{
			if ((int)val != 1)
			{
				throw new ArgumentOutOfRangeException($"Unknown ApplyLookType {_cameraSettings.ApplyLookType_}");
			}
			Vector3 customRotation = _customRotation;
			ref Vector3 customRotation2 = ref _customRotation;
			customRotation2.Pitch = MathHelper.Clamp(customRotation2.Pitch + lookOffset.X, -1.5607964f, 1.5607964f);
			customRotation2.Yaw = MathHelper.WrapAngle(customRotation2.Yaw + lookOffset.Y);
			if (IsFirstPerson)
			{
				_itemWiggleTickAccumulator += deltaTime;
				_itemWiggleAmountAccumulator.X += (customRotation.Pitch - customRotation2.Pitch) * 4f;
				_itemWiggleAmountAccumulator.Y += lookOffset.Y * 4f;
				if (_itemWiggleTickAccumulator > 1f / 12f)
				{
					_itemWiggleTickAccumulator = 1f / 12f;
				}
				while (_itemWiggleTickAccumulator >= 1f / 60f)
				{
					_gameInstance.LocalPlayer.ApplyFirstPersonMouseItemWiggle(_itemWiggleAmountAccumulator.Y, _itemWiggleAmountAccumulator.X);
					_itemWiggleAmountAccumulator.X = (_itemWiggleAmountAccumulator.Y = 0f);
					_itemWiggleTickAccumulator -= 1f / 60f;
				}
				float timeFraction = System.Math.Min(_itemWiggleTickAccumulator / (1f / 60f), 1f);
				_gameInstance.LocalPlayer.UpdateClientInterpolationMouseWiggle(timeFraction);
			}
		}
		else
		{
			if (AttachedTo != _gameInstance.LocalPlayer)
			{
				return;
			}
			Vector3 lookOrientation = _gameInstance.LocalPlayer.LookOrientation;
			ref Vector3 lookOrientation2 = ref _gameInstance.LocalPlayer.LookOrientation;
			lookOrientation2.Pitch = MathHelper.Clamp(lookOrientation2.Pitch + lookOffset.X, -1.5607964f, 1.5607964f);
			lookOrientation2.Yaw = MathHelper.WrapAngle(lookOrientation2.Yaw + lookOffset.Y);
			if (IsFirstPerson)
			{
				_itemWiggleTickAccumulator += deltaTime;
				_itemWiggleAmountAccumulator.X += (lookOrientation.Pitch - lookOrientation2.Pitch) * 4f;
				_itemWiggleAmountAccumulator.Y += lookOffset.Y * 4f;
				if (_itemWiggleTickAccumulator > 1f / 12f)
				{
					_itemWiggleTickAccumulator = 1f / 12f;
				}
				while (_itemWiggleTickAccumulator >= 1f / 60f)
				{
					_gameInstance.LocalPlayer.ApplyFirstPersonMouseItemWiggle(_itemWiggleAmountAccumulator.Y, _itemWiggleAmountAccumulator.X);
					_itemWiggleAmountAccumulator.X = (_itemWiggleAmountAccumulator.Y = 0f);
					_itemWiggleTickAccumulator -= 1f / 60f;
				}
				float timeFraction2 = System.Math.Min(_itemWiggleTickAccumulator / (1f / 60f), 1f);
				_gameInstance.LocalPlayer.UpdateClientInterpolationMouseWiggle(timeFraction2);
			}
		}
	}

	public void SetRotation(Vector3 rotation)
	{
		Rotation = rotation;
	}

	public void OnMouseInput(SDL_Event evt)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Invalid comparison between Unknown and I4
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Invalid comparison between Unknown and I4
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Expected O, but got Unknown
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03be: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c1: Invalid comparison between Unknown and I4
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_03df: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ed: Invalid comparison between Unknown and I4
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Expected O, but got Unknown
		//IL_0134: Expected O, but got Unknown
		//IL_03c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c8: Invalid comparison between Unknown and I4
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f4: Invalid comparison between Unknown and I4
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_053d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Expected O, but got Unknown
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_032f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0343: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
		//IL_034e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		//IL_0362: Expected O, but got Unknown
		//IL_0367: Expected O, but got Unknown
		//IL_0369: Expected O, but got Unknown
		CollisionModule.BlockResult? blockResult;
		CollisionModule.EntityResult? entityResult;
		if ((int)evt.type == 1025 || (int)evt.type == 1026)
		{
			MouseButtonType mouseButtonType_;
			switch (evt.button.button)
			{
			default:
				return;
			case 1:
				mouseButtonType_ = (MouseButtonType)0;
				break;
			case 2:
				mouseButtonType_ = (MouseButtonType)1;
				break;
			case 3:
				mouseButtonType_ = (MouseButtonType)2;
				break;
			case 4:
				mouseButtonType_ = (MouseButtonType)3;
				break;
			case 5:
				mouseButtonType_ = (MouseButtonType)4;
				break;
			}
			Vector2 screenPoint = _gameInstance.Engine.Window.SDLToNormalizedScreenCenterCoords(evt.button.x, evt.button.y);
			InventoryModule inventoryModule = _gameInstance.InventoryModule;
			MouseInteraction val = new MouseInteraction
			{
				ClientTimestamp = TimeHelper.GetEpochMilliseconds(),
				ActiveSlot = inventoryModule.HotbarActiveSlot,
				ItemInHand = inventoryModule.GetActiveItem().ToItemPacket(includeMetadata: true),
				ScreenPoint = new Vector2f(screenPoint.X, screenPoint.Y),
				MouseButton = new MouseButtonEvent
				{
					Clicks = (sbyte)evt.button.clicks,
					MouseButtonType_ = mouseButtonType_,
					State = (MouseButtonState)(evt.button.state != 1)
				}
			};
			Vector3.ScreenToWorldRay(screenPoint, Position, _gameInstance.SceneRenderer.Data.InvViewProjectionMatrix, out var position, out var direction);
			Ray ray = new Ray(position, direction);
			MouseRaycast(val, ref ray, out blockResult, out entityResult);
			_gameInstance.Connection.SendPacket((ProtoPacket)(object)val);
		}
		else
		{
			if ((int)evt.type != 1024)
			{
				return;
			}
			Vector2 screenPoint2 = _gameInstance.Engine.Window.SDLToNormalizedScreenCenterCoords(evt.motion.x, evt.motion.y);
			Vector3.ScreenToWorldRay(screenPoint2, Position, _gameInstance.SceneRenderer.Data.InvViewProjectionMatrix, out var position2, out var direction2);
			Ray ray2 = new Ray(position2, direction2);
			if (_cameraSettings.SendMouseMotion)
			{
				List<MouseButtonType> list = new List<MouseButtonType>();
				if ((evt.motion.state & SDL.SDL_BUTTON_LMASK) != 0)
				{
					list.Add((MouseButtonType)0);
				}
				if ((evt.motion.state & SDL.SDL_BUTTON_MMASK) != 0)
				{
					list.Add((MouseButtonType)1);
				}
				if ((evt.motion.state & SDL.SDL_BUTTON_RMASK) != 0)
				{
					list.Add((MouseButtonType)2);
				}
				if ((evt.motion.state & SDL.SDL_BUTTON_X1MASK) != 0)
				{
					list.Add((MouseButtonType)3);
				}
				if ((evt.motion.state & SDL.SDL_BUTTON_X2MASK) != 0)
				{
					list.Add((MouseButtonType)4);
				}
				InventoryModule inventoryModule2 = _gameInstance.InventoryModule;
				MouseInteraction val2 = new MouseInteraction
				{
					ClientTimestamp = TimeHelper.GetEpochMilliseconds(),
					ActiveSlot = inventoryModule2.HotbarActiveSlot,
					ItemInHand = inventoryModule2.GetActiveItem().ToItemPacket(includeMetadata: true),
					ScreenPoint = new Vector2f(screenPoint2.X, screenPoint2.Y),
					MouseMotion = new MouseMotionEvent
					{
						MouseButtonType_ = list.ToArray(),
						RelativeMotion = new Vector2i(evt.motion.xrel, evt.motion.yrel)
					}
				};
				MouseRaycast(val2, ref ray2, out blockResult, out entityResult);
				_gameInstance.Connection.SendPacket((ProtoPacket)(object)val2);
			}
			if (AttachedTo != _gameInstance.LocalPlayer)
			{
				return;
			}
			MouseInputType mouseInputType_ = _cameraSettings.MouseInputType_;
			MouseInputType val3 = mouseInputType_;
			if ((int)val3 > 2)
			{
				if ((int)val3 == 3)
				{
					Vector3 attachmentPosition = AttachmentPosition;
					Vector3 normal = ((_cameraSettings.PlaneNormal != null) ? new Vector3(_cameraSettings.PlaneNormal.X, _cameraSettings.PlaneNormal.Y, _cameraSettings.PlaneNormal.Z) : Vector3.Up);
					float num = normal.X * attachmentPosition.X + normal.Y * attachmentPosition.Y + normal.Z * attachmentPosition.Z;
					Plane plane = new Plane(normal, 0f - num);
					float? num2 = ray2.Intersects(plane);
					if (num2.HasValue)
					{
						_gameInstance.LocalPlayer.LookAt(position2 + direction2 * num2.Value);
					}
					return;
				}
				throw new ArgumentOutOfRangeException($"Unknown MouseInputType {_cameraSettings.MouseInputType_}");
			}
			CollisionModule.CombinedOptions options = CollisionModule.CombinedOptions.Default;
			MouseInputType mouseInputType_2 = _cameraSettings.MouseInputType_;
			MouseInputType val4 = mouseInputType_2;
			if ((int)val4 != 1)
			{
				if ((int)val4 == 2)
				{
					options.EnableBlock = false;
				}
			}
			else
			{
				options.Block.IgnoreFluids = true;
				options.EnableEntity = false;
			}
			if (_gameInstance.CollisionModule.FindNearestTarget(ref ray2, ref options, out blockResult, out entityResult, out var result))
			{
				_gameInstance.LocalPlayer.LookAt(result.GetTarget());
			}
		}
	}

	private void MouseRaycast(MouseInteraction mouseInteraction, ref Ray ray, out CollisionModule.BlockResult? blockResult, out CollisionModule.EntityResult? entityResult)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected I4, but got Unknown
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		mouseInteraction.WorldInteraction_ = new WorldInteraction();
		CollisionModule.CombinedOptions options = CollisionModule.CombinedOptions.Default;
		MouseInputTargetType mouseInputTargetType_ = _cameraSettings.MouseInputTargetType_;
		MouseInputTargetType val = mouseInputTargetType_;
		switch ((int)val)
		{
		case 3:
			blockResult = null;
			entityResult = null;
			return;
		case 1:
			options.EnableEntity = false;
			break;
		case 2:
			options.EnableBlock = false;
			break;
		default:
			throw new ArgumentOutOfRangeException($"Unknown MouseClickTargetType_ {_cameraSettings.MouseInputTargetType_}");
		case 0:
			break;
		}
		if (_gameInstance.CollisionModule.FindNearestTarget(ref ray, ref options, out blockResult, out entityResult))
		{
			if (entityResult.HasValue)
			{
				mouseInteraction.WorldInteraction_.EntityId = entityResult.Value.Entity.NetworkId;
			}
			else if (blockResult.HasValue)
			{
				mouseInteraction.WorldInteraction_.BlockPosition_ = blockResult.Value.GetBlockPosition();
			}
		}
	}

	private Vector3 GetEyeOffset()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		if (!_cameraSettings.EyeOffset || AttachedTo == null)
		{
			return Vector3.Zero;
		}
		if ((int)_cameraSettings.AttachedToType_ > 0)
		{
			return new Vector3(0f, AttachedTo.EyeOffset, 0f);
		}
		CharacterControllerModule characterControllerModule = _gameInstance.CharacterControllerModule;
		return new Vector3(0f, AttachedTo.EyeOffset + characterControllerModule.MovementController.CrouchHeightShift, 0f);
	}

	private Vector3 GetPosition()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		PositionType positionType_ = _cameraSettings.PositionType_;
		PositionType val = positionType_;
		if ((int)val != 0)
		{
			if ((int)val == 1)
			{
				return _customPosition;
			}
			throw new ArgumentOutOfRangeException($"Unknown PositionType {_cameraSettings.PositionType_}");
		}
		return AttachmentPosition + PositionOffset;
	}

	private Vector3 GetRotation()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		RotationType rotationType_ = _cameraSettings.RotationType_;
		RotationType val = rotationType_;
		if ((int)val != 0)
		{
			if ((int)val == 1)
			{
				Vector3 result = Vector3.WrapAngle(_customRotation);
				result.Pitch = MathHelper.Clamp(result.Pitch, -1.5607964f, 1.5607964f);
				return result;
			}
			throw new ArgumentOutOfRangeException($"Unknown RotationType {_cameraSettings.RotationType_}");
		}
		Vector3 result2 = Vector3.WrapAngle(AttachedTo.LookOrientation + RotationOffset);
		result2.Pitch = MathHelper.Clamp(result2.Pitch, -1.5607964f, 1.5607964f);
		return result2;
	}
}
