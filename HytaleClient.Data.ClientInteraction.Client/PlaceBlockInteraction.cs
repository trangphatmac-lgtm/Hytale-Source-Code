using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Hypixel.ProtoPlus;
using HytaleClient.Data.Items;
using HytaleClient.Data.Map;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.Data.ClientInteraction.Client;

internal class PlaceBlockInteraction : SimpleInteraction
{
	private static readonly Rotation[] _connectionTestRotations;

	private const float ForwardLookAllowance = 1f;

	private static readonly IntVector3[] CandidateVectors;

	private IntVector3? _prevPlacePosition;

	private IntVector3? _dragVector;

	private Vector3? _initialVector;

	private int? _lastVariantId;

	private readonly int _interactionBlockIdOverride;

	private float _lastPlacementTime;

	public bool IsFluidityEnabled => !Interaction.RemoveItemInHand;

	private void SetDragVector(IntVector3? direction, GameInstance gameInstance)
	{
		if (!direction.HasValue)
		{
			gameInstance.InteractionModule.FluidityActive = false;
		}
		else
		{
			gameInstance.InteractionModule.FluidityActive = true;
		}
		_dragVector = direction;
	}

	public bool IsFluidityActive()
	{
		return _dragVector.HasValue;
	}

	public PlaceBlockInteraction(int id, Interaction interaction)
		: base(id, interaction)
	{
		_interactionBlockIdOverride = interaction.BlockId;
	}

	protected override void Tick0(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
	{
		if (clickType == InteractionModule.ClickType.None)
		{
			ResetAndLeave(context, (InteractionState)0);
			return;
		}
		int? num = gameInstance.ItemLibraryModule.GetItem(context.HeldItem?.Id)?.BlockId;
		ClientBlockType clientBlockType = (num.HasValue ? gameInstance.MapModule.ClientBlockTypes[num.Value] : null);
		if (clientBlockType == null)
		{
			ResetAndLeave(context, (InteractionState)0);
		}
		else
		{
			Tick1(gameInstance, clickType, firstRun, time, context, clientBlockType);
		}
	}

	private void ResetAndLeave(InteractionContext context, InteractionState state)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		_prevPlacePosition = null;
		SetDragVector(null, context.GameInstance);
		_lastVariantId = null;
		_initialVector = null;
		_lastPlacementTime = 0f;
		context.State.State = state;
	}

	private void Tick1(GameInstance gameInstance, InteractionModule.ClickType clickType, bool firstRun, float time, InteractionContext context, ClientBlockType heldBlockType)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Invalid comparison between Unknown and I4
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if (clickType == InteractionModule.ClickType.Single && !firstRun)
		{
			context.State.State = (InteractionState)3;
			return;
		}
		if (clickType == InteractionModule.ClickType.Held && !_dragVector.HasValue)
		{
			TryLatch(gameInstance, clickType, time);
			if (!_dragVector.HasValue)
			{
				context.State.State = (InteractionState)4;
				return;
			}
		}
		if (!UpdateCooldown(gameInstance, clickType, time))
		{
			context.State.State = (InteractionState)4;
			return;
		}
		PlaceNextBlock(gameInstance, clickType, context, heldBlockType);
		if ((int)context.State.State == 4)
		{
			_lastPlacementTime = time;
		}
	}

	private void TryLatch(GameInstance gameInstance, InteractionModule.ClickType clickType, float time)
	{
		IntVector3 playerMovementDirection = GetPlayerMovementDirection(gameInstance);
		if (playerMovementDirection != IntVector3.Zero)
		{
			SetDragVector(playerMovementDirection, gameInstance);
			return;
		}
		if (!_prevPlacePosition.HasValue)
		{
			return;
		}
		IntVector3 value = _prevPlacePosition.Value;
		bool flag = time - _lastPlacementTime > 0.4f;
		Vector3 blockPosition;
		Vector3 blockNormal;
		if (gameInstance.InteractionModule.InteractionTarget == InteractionModule.InteractionTargetType.Block)
		{
			blockPosition = gameInstance.InteractionModule.TargetBlockHit.BlockPosition;
			blockNormal = gameInstance.InteractionModule.TargetBlockHit.BlockNormal;
			if (blockPosition == value)
			{
				if (_initialVector.HasValue)
				{
					Vector3 value2 = blockNormal;
					Vector3? initialVector = _initialVector;
					if (!(value2 == initialVector))
					{
						goto IL_00fe;
					}
				}
				if (!flag)
				{
					return;
				}
			}
			goto IL_00fe;
		}
		goto IL_01ac;
		IL_00fe:
		IntVector3 next = new IntVector3((int)System.Math.Floor(blockPosition.X + blockNormal.X), (int)System.Math.Floor(blockPosition.Y + blockNormal.Y), (int)System.Math.Floor(blockPosition.Z + blockNormal.Z));
		IntVector3? snappedPlacementPosition = GetSnappedPlacementPosition(value, next);
		if (snappedPlacementPosition.HasValue && IsValidPlacementPosition(snappedPlacementPosition.Value, clickType))
		{
			SetDragVector(snappedPlacementPosition - value, gameInstance);
		}
		goto IL_01ac;
		IL_01ac:
		IntVector3? direction = null;
		float num = float.PositiveInfinity;
		IntVector3[] candidateVectors = CandidateVectors;
		foreach (IntVector3 intVector in candidateVectors)
		{
			Vector3? initialVector = _initialVector;
			Vector3 value2 = intVector;
			if (initialVector.HasValue && initialVector.GetValueOrDefault() == value2 && !flag)
			{
				continue;
			}
			IntVector3 intVector2 = value + intVector;
			BoundingBox forwardBox = new BoundingBox(intVector2, intVector2 + Vector3.One);
			HitDetection.RayBoxCollision? rayBoxCollision = CheckForwardLookVector(gameInstance, forwardBox);
			if (rayBoxCollision.HasValue)
			{
				float num2 = Vector3.DistanceSquared(rayBoxCollision.Value.Position, gameInstance.CameraModule.Controller.Position);
				if (!(num2 > num))
				{
					num = num2;
					direction = intVector;
				}
			}
		}
		if (direction.HasValue)
		{
			SetDragVector(direction, gameInstance);
		}
	}

	private IntVector3 GetPlayerMovementDirection(GameInstance gameInstance)
	{
		Vector3 vector = gameInstance.LocalPlayer.Position - gameInstance.LocalPlayer.PreviousPosition;
		Vector3 vector2 = Vector3.Zero;
		float num = 0f;
		IntVector3[] candidateVectors = CandidateVectors;
		foreach (IntVector3 intVector in candidateVectors)
		{
			float num2 = Vector3.Dot(vector, intVector);
			if (!(num2 <= num))
			{
				num = num2;
				vector2 = intVector;
			}
		}
		return new IntVector3((int)vector2.X, (int)vector2.Y, (int)vector2.Z);
	}

	private void PlaceNextBlock(GameInstance gameInstance, InteractionModule.ClickType clickType, InteractionContext context, ClientBlockType heldBlockType)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		bool flag = TryPlaceBlock(gameInstance, clickType, context, heldBlockType);
		switch (clickType)
		{
		case InteractionModule.ClickType.Held:
			context.State.State = (InteractionState)4;
			break;
		case InteractionModule.ClickType.Single:
			context.State.State = (InteractionState)(flag ? 4 : 3);
			break;
		}
	}

	private bool UpdateCooldown(GameInstance gameInstance, InteractionModule.ClickType clickType, float time)
	{
		if (clickType == InteractionModule.ClickType.Held)
		{
			float cooldownRate = GetCooldownRate(gameInstance);
			if (time - _lastPlacementTime < cooldownRate)
			{
				return false;
			}
		}
		return true;
	}

	private float GetCooldownRate(GameInstance gameInstance)
	{
		float playerSpeed = GetPlayerSpeed(gameInstance);
		float num = 1f - System.Math.Min(playerSpeed * 3f, 1f);
		return 0.12f * num;
	}

	private static float GetPlayerSpeed(GameInstance gameInstance)
	{
		return (gameInstance.LocalPlayer.Position - gameInstance.LocalPlayer.PreviousPosition).Length();
	}

	private bool TryPlaceBlock(GameInstance gameInstance, InteractionModule.ClickType clickType, InteractionContext context, ClientBlockType heldBlockType)
	{
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Expected O, but got Unknown
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Expected O, but got Unknown
		//IL_02fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0307: Expected O, but got Unknown
		bool flag = false;
		IntVector3 intVector;
		if (clickType != InteractionModule.ClickType.Held)
		{
			if (gameInstance.InteractionModule.BlockPreview.IsVisible)
			{
				intVector = gameInstance.InteractionModule.BlockPreview.BlockPosition;
				flag = (gameInstance.InteractionModule.BlockPreview.IsVisible && gameInstance.InteractionModule.BlockPreview.HasValidPosition) || (!gameInstance.InteractionModule.BlockPreview.IsVisible && gameInstance.InteractionModule.HeldBlockCanBePlaced);
			}
			else
			{
				if (!TryGetPlacementPosition(gameInstance, heldBlockType, out var blockX, out var blockY, out var blockZ).Valid)
				{
					return false;
				}
				intVector = new IntVector3(blockX, blockY, blockZ);
				flag = true;
			}
		}
		else
		{
			IntVector3? nextPlacementPosition = GetNextPlacementPosition(gameInstance, heldBlockType, clickType);
			if (!nextPlacementPosition.HasValue)
			{
				return false;
			}
			intVector = nextPlacementPosition.Value;
			flag = gameInstance.InteractionModule.HeldBlockCanBePlaced;
		}
		int x = intVector.X;
		int y = intVector.Y;
		int z = intVector.Z;
		int block = gameInstance.MapModule.GetBlock(x, y, z, 1);
		if (block == 1)
		{
			return false;
		}
		ClientBlockType targetBlockType = gameInstance.MapModule.ClientBlockTypes[block];
		int num = _interactionBlockIdOverride;
		if (num == -1)
		{
			num = _lastVariantId ?? _GetPlacedBlockVariant(gameInstance, heldBlockType, targetBlockType, x, y, z);
		}
		ClientBlockType clientBlockType = gameInstance.MapModule.ClientBlockTypes[num];
		if (!flag)
		{
			context.State.State = (InteractionState)3;
			Vector3 worldPosition = new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f);
			gameInstance.AudioModule.TryPlayBlockSoundEvent(heldBlockType.BlockSoundSetIndex, (BlockSoundEvent)4, worldPosition, Vector3.Zero);
			return false;
		}
		context.InstanceStore.OldBlockId = block;
		Vector3 worldPosition2 = new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f);
		gameInstance.AudioModule.TryPlayBlockSoundEvent(heldBlockType.BlockSoundSetIndex, (BlockSoundEvent)5, worldPosition2, Vector3.Zero);
		gameInstance.MapModule.SetClientBlock(x, y, z, num);
		ClientItemStack heldItem = context.HeldItem;
		if (Interaction.RemoveItemInHand && (int)gameInstance.GameMode == 0 && heldItem != null && heldItem.Quantity == 1)
		{
			context.HeldItem = null;
		}
		context.State.BlockPosition_ = new BlockPosition(x, y, z);
		context.State.BlockRotation_ = new BlockRotation(clientBlockType.RotationYaw, clientBlockType.RotationPitch, clientBlockType.RotationRoll);
		context.State.PlacedBlockId = num;
		if (_dragVector.HasValue)
		{
			gameInstance.Connection.SendPacket((ProtoPacket)new PrototypeClientPlaceBlock(context.State.BlockPosition_, context.State.BlockRotation_));
		}
		_lastVariantId = num;
		_prevPlacePosition = intVector;
		Ray lookRay = gameInstance.CameraModule.GetLookRay();
		if (HitDetection.CheckRayBoxCollision(new BoundingBox(intVector, intVector + Vector3.One), lookRay.Position, lookRay.Direction, out var collision))
		{
			_initialVector = collision.Normal;
		}
		else
		{
			_initialVector = null;
		}
		return true;
	}

	private bool IsValidPlacementPosition(IntVector3 placementPosition, InteractionModule.ClickType clickType)
	{
		IntVector3? prevPlacePosition = _prevPlacePosition;
		IntVector3? dragVector = _dragVector;
		if (clickType != InteractionModule.ClickType.Held)
		{
			return true;
		}
		if (!prevPlacePosition.HasValue)
		{
			return false;
		}
		if (!dragVector.HasValue)
		{
			return CandidateVectors.Contains(placementPosition - prevPlacePosition.Value);
		}
		IntVector3 intVector = prevPlacePosition.Value + dragVector.Value;
		return intVector == placementPosition;
	}

	private IntVector3? GetNextPlacementPosition(GameInstance gameInstance, ClientBlockType heldBlockType, InteractionModule.ClickType clickType)
	{
		if (TryGetPlacementPosition(gameInstance, heldBlockType, out var blockX, out var blockY, out var blockZ).Valid)
		{
			IntVector3 intVector = new IntVector3(blockX, blockY, blockZ);
			if (clickType == InteractionModule.ClickType.Held && _prevPlacePosition.HasValue)
			{
				IntVector3? snappedPlacementPosition = GetSnappedPlacementPosition(_prevPlacePosition.Value, intVector);
				if (snappedPlacementPosition.HasValue)
				{
					intVector = snappedPlacementPosition.Value;
				}
			}
			if (IsValidPlacementPosition(intVector, clickType))
			{
				return intVector;
			}
		}
		if (_prevPlacePosition.HasValue)
		{
			IntVector3 value = _prevPlacePosition.Value;
			IntVector3 intVector2 = value;
			Vector3 end = value + Vector3.One;
			IntVector3? result = null;
			float num = float.PositiveInfinity;
			IntVector3[] candidateVectors = CandidateVectors;
			for (int i = 0; i < candidateVectors.Length; i++)
			{
				IntVector3 intVector3 = candidateVectors[i];
				IntVector3 intVector4 = value + intVector3;
				if (!IsValidPlacementPosition(intVector4, clickType))
				{
					continue;
				}
				HitDetection.RayBoxCollision? rayBoxCollision = CheckForwardLookVector(gameInstance, CreateForwardLookBox(gameInstance, intVector2, end, intVector3));
				if (!rayBoxCollision.HasValue)
				{
					continue;
				}
				Vector3 position = rayBoxCollision.Value.Position;
				if (((float)intVector3.X == 0f || ((!((float)intVector3.X > 0f) || !(position.X < (float)intVector4.X - 1f)) && (!((float)intVector3.X < 0f) || !(position.X > (float)intVector4.X + 1f)))) && ((float)intVector3.Y == 0f || ((!((float)intVector3.Y > 0f) || !(position.Y < (float)intVector4.Y - 1f)) && (!((float)intVector3.Y < 0f) || !(position.Y > (float)intVector4.Y + 1f)))) && ((float)intVector3.Z == 0f || ((!((float)intVector3.Z > 0f) || !(position.Z < (float)intVector4.Z - 1f)) && (!((float)intVector3.Z < 0f) || !(position.Z > (float)intVector4.Z + 1f)))))
				{
					float num2 = Vector3.DistanceSquared(intVector4 + new Vector3(0.5f, 0.5f, 0.5f), gameInstance.LocalPlayer.Position);
					if (num2 < num)
					{
						result = intVector4;
						num = num2;
					}
				}
			}
			return result;
		}
		return null;
	}

	private IntVector3? GetSnappedPlacementPosition(IntVector3 prev, IntVector3 next)
	{
		if (prev.X == next.X && prev.Y == next.Y)
		{
			return new IntVector3(next.X, next.Y, prev.Z + System.Math.Sign(next.Z - prev.Z));
		}
		if (prev.Y == next.Y && prev.Z == next.Z)
		{
			return new IntVector3(prev.X + System.Math.Sign(next.X - prev.X), next.Y, next.Z);
		}
		if (prev.Z == next.Z && prev.X == next.X)
		{
			return new IntVector3(next.X, prev.Y + System.Math.Sign(next.Y - prev.Y), next.Z);
		}
		return null;
	}

	private static BoundingBox CreateForwardLookBox(GameInstance gameInstance, Vector3 start, Vector3 end, IntVector3 direction)
	{
		float num = System.Math.Min(3f, GetPlayerSpeed(gameInstance) * 3f);
		float num2 = 24f;
		if ((float)direction.X != 0f)
		{
			if ((float)direction.X < 0f)
			{
				start.X -= num2;
			}
			else if ((float)direction.X > 0f)
			{
				end.X += num2;
			}
			start.Y -= num;
			end.Y += num;
			start.Z -= num;
			end.Z += num;
		}
		if ((float)direction.Y != 0f)
		{
			if ((float)direction.Y < 0f)
			{
				start.Y -= num2;
			}
			else if ((float)direction.Y > 0f)
			{
				end.Y += num2;
			}
			start.X -= num;
			end.X += num;
			start.Z -= num;
			end.Z += num;
		}
		if ((float)direction.Z != 0f)
		{
			if ((float)direction.Z < 0f)
			{
				start.Z -= num2;
			}
			else if ((float)direction.Z > 0f)
			{
				end.Z += num2;
			}
			start.X -= num;
			end.Y += num;
			start.Z -= num;
			end.Y += num;
		}
		return new BoundingBox(start, end);
	}

	private static HitDetection.RayBoxCollision? CheckForwardLookVector(GameInstance gameInstance, BoundingBox forwardBox)
	{
		Ray lookRay = gameInstance.CameraModule.GetLookRay();
		if (!HitDetection.CheckRayBoxCollision(forwardBox, lookRay.Position, lookRay.Direction, out var collision))
		{
			return null;
		}
		float num = 8f;
		if (Vector3.Distance(lookRay.Position, collision.Position) >= num)
		{
			return null;
		}
		HitDetection.RaycastOptions options = new HitDetection.RaycastOptions
		{
			Distance = num
		};
		gameInstance.HitDetection.Raycast(lookRay.Position, lookRay.Direction, options, out var hasFoundTargetBlock, out var _, out var _, out var _);
		if (hasFoundTargetBlock)
		{
			return null;
		}
		return collision;
	}

	public static (bool Valid, IntVector3? ConflictingBlock) TryGetPlacementPosition(GameInstance gameInstance, ClientBlockType heldBlockType, out int blockX, out int blockY, out int blockZ)
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Invalid comparison between Unknown and I4
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Invalid comparison between Unknown and I4
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Invalid comparison between Unknown and I4
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Invalid comparison between Unknown and I4
		HitDetection.RaycastHit targetBlockHit = gameInstance.InteractionModule.TargetBlockHit;
		ClientBlockType clientBlockType = gameInstance.MapModule.ClientBlockTypes[targetBlockHit.BlockId];
		blockX = (int)System.Math.Floor(targetBlockHit.BlockPositionNoFiller.X);
		blockY = (int)System.Math.Floor(targetBlockHit.BlockPositionNoFiller.Y);
		blockZ = (int)System.Math.Floor(targetBlockHit.BlockPositionNoFiller.Z);
		if (clientBlockType.FillerX == 0 && clientBlockType.FillerY == 0 && clientBlockType.FillerZ == 0 && (int)clientBlockType.CollisionMaterial == 0 && (int)heldBlockType.CollisionMaterial == 1)
		{
			return (Valid: true, ConflictingBlock: null);
		}
		blockX += (int)System.Math.Floor(targetBlockHit.BlockNormal.X);
		blockY += (int)System.Math.Floor(targetBlockHit.BlockNormal.Y);
		blockZ += (int)System.Math.Floor(targetBlockHit.BlockNormal.Z);
		int block = gameInstance.MapModule.GetBlock(blockX, blockY, blockZ, int.MaxValue);
		if (block == int.MaxValue)
		{
			return (Valid: false, ConflictingBlock: null);
		}
		clientBlockType = gameInstance.MapModule.ClientBlockTypes[block];
		int num = blockX;
		int num2 = blockY;
		int num3 = blockZ;
		if (clientBlockType.FillerX != 0 || clientBlockType.FillerY != 0 || clientBlockType.FillerZ != 0)
		{
			num -= clientBlockType.FillerX;
			num2 -= clientBlockType.FillerY;
			num3 -= clientBlockType.FillerZ;
			block = gameInstance.MapModule.GetBlock(num, num2, num3, 1);
			clientBlockType = gameInstance.MapModule.ClientBlockTypes[block];
		}
		if (block == 0)
		{
			return (Valid: true, ConflictingBlock: null);
		}
		if ((int)clientBlockType.CollisionMaterial == 1)
		{
			return (Valid: false, ConflictingBlock: new IntVector3(num, num2, num3));
		}
		if ((int)clientBlockType.CollisionMaterial == 2)
		{
			return (Valid: true, ConflictingBlock: null);
		}
		if ((int)heldBlockType.CollisionMaterial == 1)
		{
			return (Valid: true, ConflictingBlock: null);
		}
		return (Valid: false, ConflictingBlock: new IntVector3(num, num2, num3));
	}

	protected virtual int _GetPlacedBlockVariant(GameInstance gameInstance, ClientBlockType heldBlockType, ClientBlockType targetBlockType, int targetBlockX, int targetBlockY, int targetBlockZ)
	{
		if (gameInstance.InteractionModule.RotatedBlockIdOverride != -1)
		{
			return gameInstance.InteractionModule.RotatedBlockIdOverride;
		}
		return GetPlacedBlockVariant(gameInstance, heldBlockType, targetBlockType, targetBlockX, targetBlockY, targetBlockZ);
	}

	private static bool FindBestConnectionType(GameInstance gameInstance, int blockX, int blockY, int blockZ, BlockConnections connections, Vector3 horizontalNormal, out BlockConnectionType? connectionType, out Rotation connectionRotation)
	{
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Expected I4, but got Unknown
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Expected I4, but got Unknown
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Expected I4, but got Unknown
		if (connections.Outputs.TryGetValue((BlockConnectionType)5, out var value) && IsMatchingBlockType(gameInstance, blockX, blockY, blockZ, (Rotation)0, -1, 0, 0, connections.ConnectableBlocks) && IsMatchingBlockType(gameInstance, blockX, blockY, blockZ, (Rotation)0, 1, 0, 0, connections.ConnectableBlocks) && IsMatchingBlockType(gameInstance, blockX, blockY, blockZ, (Rotation)0, 0, 0, -1, connections.ConnectableBlocks) && IsMatchingBlockType(gameInstance, blockX, blockY, blockZ, (Rotation)0, 0, 0, 1, connections.ConnectableBlocks))
		{
			connectionType = (BlockConnectionType)5;
			connectionRotation = (Rotation)0;
			return true;
		}
		if (connections.Outputs.TryGetValue((BlockConnectionType)3, out value))
		{
			Rotation[] connectionTestRotations = _connectionTestRotations;
			foreach (Rotation val in connectionTestRotations)
			{
				if (IsMatchingBlockType(gameInstance, blockX, blockY, blockZ, val, 1, 0, 0, connections.ConnectableBlocks) && IsMatchingBlockType(gameInstance, blockX, blockY, blockZ, val, -1, 0, 0, connections.ConnectableBlocks) && IsMatchingBlockType(gameInstance, blockX, blockY, blockZ, val, 0, 0, 1, connections.ConnectableBlocks))
				{
					connectionType = (BlockConnectionType)3;
					connectionRotation = (Rotation)(int)val;
					return true;
				}
			}
		}
		if (connections.Outputs.TryGetValue((BlockConnectionType)0, out var value2))
		{
			int block = gameInstance.MapModule.GetBlock(blockX, blockY, blockZ, int.MaxValue);
			if (block != int.MaxValue)
			{
				ClientBlockType clientBlockType = gameInstance.MapModule.ClientBlockTypes[block];
				if (clientBlockType.TryGetRotatedVariant((Rotation)0, clientBlockType.RotationPitch, (Rotation)0) == value2)
				{
					Rotation yawRotation = RotationHelper.Subtract(clientBlockType.RotationYaw, (Rotation)1);
					if (IsMatchingBlockType(gameInstance, blockX, blockY, blockZ, yawRotation, 1, 0, 0, connections.ConnectableBlocks) && IsMatchingBlockType(gameInstance, blockX, blockY, blockZ, yawRotation, 0, 0, 1, connections.ConnectableBlocks))
					{
						connectionType = (BlockConnectionType)0;
						connectionRotation = (Rotation)(int)clientBlockType.RotationYaw;
						return true;
					}
				}
			}
			Rotation[] connectionTestRotations2 = _connectionTestRotations;
			foreach (Rotation val2 in connectionTestRotations2)
			{
				if (IsMatchingBlockType(gameInstance, blockX, blockY, blockZ, val2, 1, 0, 0, connections.ConnectableBlocks) && IsMatchingBlockType(gameInstance, blockX, blockY, blockZ, val2, 0, 0, 1, connections.ConnectableBlocks))
				{
					connectionType = (BlockConnectionType)0;
					connectionRotation = (Rotation)(int)RotationHelper.Add(val2, (Rotation)1);
					return true;
				}
			}
		}
		connectionType = null;
		connectionRotation = (Rotation)0;
		return false;
	}

	private static bool IsMatchingBlockType(GameInstance gameInstance, int blockX, int blockY, int blockZ, Rotation yawRotation, int relX, int relY, int relZ, int[] allowedBlockIds)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		RotationHelper.Rotate(relX, relZ, yawRotation, out relX, out relZ);
		int block = gameInstance.MapModule.GetBlock(blockX + relX, blockY + relY, blockZ + relZ, int.MaxValue);
		if (block == int.MaxValue)
		{
			return false;
		}
		bool flag = Array.IndexOf(allowedBlockIds, block) >= 0;
		if (!flag)
		{
			ClientBlockType clientBlockType = gameInstance.MapModule.ClientBlockTypes[block];
			if (clientBlockType.VariantOriginalId != block)
			{
				flag = Array.IndexOf(allowedBlockIds, clientBlockType.VariantOriginalId) >= 0;
			}
		}
		return flag;
	}

	public static int GetPlacedBlockVariant(GameInstance gameInstance, ClientBlockType heldBlockType, ClientBlockType targetBlockType, int targetBlockX, int targetBlockY, int targetBlockZ)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Invalid comparison between Unknown and I4
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		HitDetection.RaycastHit targetBlockHit = gameInstance.InteractionModule.TargetBlockHit;
		Vector3 blockNormal = targetBlockHit.BlockNormal;
		RotationHelper.GetVariantRotationOptions(blockNormal, heldBlockType.VariantRotation, out var rotateX, out var rotateY, out var defaultPitch, out var yawValues);
		int id = heldBlockType.Id;
		if (blockNormal.Y != 0f)
		{
			RotationHelper.GetHorizontalNormal(gameInstance.CameraModule.Controller.Rotation, out blockNormal.X, out blockNormal.Z);
		}
		if (heldBlockType.IsConnectable() && FindBestConnectionType(gameInstance, targetBlockX, targetBlockY, targetBlockZ, heldBlockType.Connections, blockNormal, out var connectionType, out var connectionRotation) && connectionType.HasValue)
		{
			int num = heldBlockType.Connections.Outputs[connectionType.Value];
			connectionRotation = yawValues[connectionRotation];
			return gameInstance.MapModule.ClientBlockTypes[num].TryGetRotatedVariant(connectionRotation, (Rotation)0, (Rotation)0);
		}
		GetVariantByNormal(blockNormal, rotateX, rotateY, defaultPitch, out var yaw, out var pitch);
		if (gameInstance.App.Settings.UseBlockSubfaces)
		{
			SubfacePlacement(targetBlockHit, ref yaw, ref pitch, heldBlockType, gameInstance);
		}
		_ = heldBlockType.RotationYawPlacementOffset;
		if (true)
		{
			yaw = RotationHelper.Add(yaw, heldBlockType.RotationYawPlacementOffset);
		}
		yaw = yawValues[yaw];
		id = heldBlockType.TryGetRotatedVariant(yaw, pitch, (Rotation)0);
		if ((int)targetBlockType.CollisionMaterial == 2)
		{
			ClientBlockType clientBlockType = gameInstance.MapModule.ClientBlockTypes[id];
			if (clientBlockType.Variants.TryGetValue("Fluid=" + targetBlockType.Name, out var value))
			{
				id = value;
			}
		}
		return id;
	}

	private static void GetVariantByNormal(Vector3 normal, bool rotateX, bool rotateY, Rotation defaultPitch, out Rotation yaw, out Rotation pitch)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Expected I4, but got Unknown
		yaw = (Rotation)0;
		pitch = (Rotation)(int)defaultPitch;
		if (rotateY && normal.Y == -1f)
		{
			pitch = (Rotation)2;
			normal.X *= -1f;
			normal.Z *= -1f;
		}
		if (rotateX)
		{
			if (normal.X == -1f)
			{
				yaw = (Rotation)3;
			}
			else if (normal.X == 1f)
			{
				yaw = (Rotation)1;
			}
			else if (normal.Z == -1f)
			{
				yaw = (Rotation)2;
			}
			else if (normal.Z == 1f)
			{
				yaw = (Rotation)0;
			}
		}
	}

	private static void SubfacePlacement(HitDetection.RaycastHit targetBlockHit, ref Rotation yaw, ref Rotation pitch, ClientBlockType blockType, GameInstance gameInstance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Invalid comparison between Unknown and I4
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Invalid comparison between Unknown and I4
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Invalid comparison between Unknown and I4
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Invalid comparison between Unknown and I4
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Invalid comparison between Unknown and I4
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Invalid comparison between Unknown and I4
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Expected I4, but got Unknown
		VariantRotation variantRotation = blockType.VariantRotation;
		BlockHitbox blockHitbox = gameInstance.ServerSettings.BlockHitboxes[blockType.HitboxType].Clone();
		if (blockHitbox.BoundingBox.Max.X - blockHitbox.BoundingBox.Min.X > 1f || blockHitbox.BoundingBox.Max.Y - blockHitbox.BoundingBox.Min.Y > 1f || blockHitbox.BoundingBox.Max.Z - blockHitbox.BoundingBox.Min.Z > 1f || ((int)variantRotation != 3 && (int)variantRotation != 4 && (int)variantRotation != 6))
		{
			return;
		}
		Vector3 vector = targetBlockHit.HitPosition - targetBlockHit.BlockOrigin;
		float num = System.Math.Abs(vector.X - 0.5f);
		float num2 = System.Math.Abs(vector.Y - 0.5f);
		float num3 = System.Math.Abs(vector.Z - 0.5f);
		if (targetBlockHit.BlockNormal.X != 0f)
		{
			num = 0f;
		}
		if (targetBlockHit.BlockNormal.Y != 0f)
		{
			num2 = 0f;
		}
		if (targetBlockHit.BlockNormal.Z != 0f)
		{
			num3 = 0f;
		}
		float num4 = 0.35f;
		float num5 = 1f - num4;
		if ((int)variantRotation == 6)
		{
			if (targetBlockHit.BlockNormal.Y == 0f && vector.Y > num5)
			{
				yaw = (Rotation)(int)RotationHelper.Subtract(yaw, (Rotation)2);
				pitch = (Rotation)2;
			}
		}
		else
		{
			if ((int)variantRotation != 3 && (int)variantRotation != 4)
			{
				return;
			}
			if (num2 > num3 && num2 > num)
			{
				if (vector.Y > num5)
				{
					yaw = (Rotation)0;
					pitch = (Rotation)2;
				}
				else if (vector.Y < num4)
				{
					yaw = (Rotation)0;
					pitch = (Rotation)0;
				}
			}
			if (num3 > num2 && num3 > num)
			{
				if (vector.Z > num5)
				{
					pitch = (Rotation)1;
					yaw = (Rotation)2;
				}
				else if (vector.Z < num4)
				{
					pitch = (Rotation)1;
					yaw = (Rotation)0;
				}
			}
			if (num > num2 && num > num3)
			{
				if (vector.X > num5)
				{
					pitch = (Rotation)1;
					yaw = (Rotation)3;
				}
				else if (vector.X < num4)
				{
					pitch = (Rotation)1;
					yaw = (Rotation)1;
				}
			}
		}
	}

	public static bool CanPlaceBlock(GameInstance gameInstance, ClientBlockType heldBlockType, int worldX, int worldY, int worldZ, out BoundingBox? collisionArea)
	{
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Invalid comparison between Unknown and I4
		collisionArea = null;
		ClientBlockType clientBlockType = gameInstance.MapModule.ClientBlockTypes[heldBlockType.Id];
		BlockHitbox blockHitbox = gameInstance.ServerSettings.BlockHitboxes[clientBlockType.HitboxType].Clone();
		BoundingBox voxelBounds = blockHitbox.GetVoxelBounds();
		for (int i = (int)voxelBounds.Min.X; (float)i < voxelBounds.Max.X; i++)
		{
			for (int j = (int)voxelBounds.Min.Y; (float)j < voxelBounds.Max.Y; j++)
			{
				for (int k = (int)voxelBounds.Min.Z; (float)k < voxelBounds.Max.Z; k++)
				{
					int num = worldX + i;
					int num2 = worldY + j;
					int num3 = worldZ + k;
					int block = gameInstance.MapModule.GetBlock(num, num2, num3, int.MaxValue);
					if (block != int.MaxValue)
					{
						ClientBlockType clientBlockType2 = gameInstance.MapModule.ClientBlockTypes[block];
						if (clientBlockType2.FillerX != 0 || clientBlockType2.FillerY != 0 || clientBlockType2.FillerZ != 0)
						{
							int worldX2 = num - clientBlockType2.FillerX;
							int worldY2 = num2 - clientBlockType2.FillerY;
							int worldZ2 = num3 - clientBlockType2.FillerZ;
							block = gameInstance.MapModule.GetBlock(worldX2, worldY2, worldZ2, 1);
							clientBlockType2 = gameInstance.MapModule.ClientBlockTypes[block];
						}
						if ((int)clientBlockType2.CollisionMaterial == 1)
						{
							Vector3 vector = new Vector3(num, num2, num3);
							collisionArea = new BoundingBox(vector, vector + Vector3.One);
							return false;
						}
					}
				}
			}
		}
		return true;
	}

	public static bool IsEntityBlockingPlacement(GameInstance gameInstance, ClientBlockType heldBlockType, int targetBlockX, int targetBlockY, int targetBlockZ, out BoundingBox? collisionArea)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		collisionArea = null;
		if ((int)heldBlockType.CollisionMaterial != 1)
		{
			return false;
		}
		BlockHitbox blockHitbox = gameInstance.ServerSettings.BlockHitboxes[heldBlockType.HitboxType];
		Entity[] allEntities = gameInstance.EntityStoreModule.GetAllEntities();
		int entitiesCount = gameInstance.EntityStoreModule.GetEntitiesCount();
		float offsetX = targetBlockX - heldBlockType.FillerX;
		float offsetY = targetBlockY - heldBlockType.FillerY;
		float offsetZ = targetBlockZ - heldBlockType.FillerZ;
		for (int i = 0; i < entitiesCount; i++)
		{
			Entity entity = allEntities[i];
			if (!entity.IsTangible())
			{
				continue;
			}
			BoundingBox hitbox = entity.Hitbox;
			hitbox.Translate(entity.Position);
			BoundingBox[] boxes = blockHitbox.Boxes;
			foreach (BoundingBox box in boxes)
			{
				if (hitbox.IntersectsExclusive(box, offsetX, offsetY, offsetZ) || hitbox.IntersectsExclusive(box, targetBlockX, targetBlockY, targetBlockZ))
				{
					collisionArea = hitbox;
					return true;
				}
			}
		}
		return false;
	}

	protected override void Revert0(GameInstance gameInstance, InteractionType type, InteractionContext context)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		base.Revert0(gameInstance, type, context);
		InteractionSyncData state = context.State;
		int oldBlockId = context.InstanceStore.OldBlockId;
		if (state?.BlockPosition_ != null && oldBlockId != int.MaxValue && !_dragVector.HasValue)
		{
			gameInstance.MapModule.SetClientBlock(state.BlockPosition_.X, state.BlockPosition_.Y, state.BlockPosition_.Z, oldBlockId);
		}
	}

	static PlaceBlockInteraction()
	{
		Rotation[] array = new Rotation[4];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		_connectionTestRotations = (Rotation[])(object)array;
		CandidateVectors = new IntVector3[6]
		{
			IntVector3.Up,
			IntVector3.Down,
			IntVector3.Left,
			IntVector3.Right,
			IntVector3.Forward,
			IntVector3.Backward
		};
	}
}
