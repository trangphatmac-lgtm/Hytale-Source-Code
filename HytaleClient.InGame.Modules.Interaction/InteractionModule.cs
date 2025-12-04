#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Hypixel.ProtoPlus;
using HytaleClient.Data.ClientInteraction;
using HytaleClient.Data.ClientInteraction.Client;
using HytaleClient.Data.Items;
using HytaleClient.Data.Map;
using HytaleClient.Data.UserSettings;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using SDL2;

namespace HytaleClient.InGame.Modules.Interaction;

internal class InteractionModule : Module
{
	public enum InteractionTargetType
	{
		None,
		Entity,
		Block
	}

	internal readonly struct BlockTargetInfo
	{
		internal readonly IntVector3 Position;

		internal readonly BoundingBox[] CollidingBoxes;

		internal readonly bool Valid;

		public BlockTargetInfo(IntVector3 position, BoundingBox[] collidingBoxes, bool valid)
		{
			Position = position;
			CollidingBoxes = collidingBoxes;
			Valid = valid;
		}

		public BlockTargetInfo(IntVector3 position, BoundingBox? box, bool valid)
		{
			Position = position;
			CollidingBoxes = ((!box.HasValue) ? Array.Empty<BoundingBox>() : new BoundingBox[1] { box.Value });
			Valid = valid;
		}

		public BlockTargetInfo(IntVector3 position, bool valid)
		{
			Position = position;
			CollidingBoxes = Array.Empty<BoundingBox>();
			Valid = valid;
		}

		public static BlockTargetInfo FromFailedBlocks(GameInstance gameInstance, IntVector3 position, IntVector3? conflict)
		{
			if (!conflict.HasValue)
			{
				return new BlockTargetInfo(position, valid: false);
			}
			int block = gameInstance.MapModule.GetBlock(conflict.Value.X, conflict.Value.Y, conflict.Value.Z, 1);
			if (block == 1)
			{
				return new BlockTargetInfo(position, valid: false);
			}
			ClientBlockType clientBlockType = gameInstance.MapModule.ClientBlockTypes[block];
			BlockHitbox blockHitbox = gameInstance.ServerSettings.BlockHitboxes[clientBlockType.HitboxType];
			BoundingBox boundingBox = blockHitbox.BoundingBox;
			boundingBox.Translate(conflict.Value);
			return new BlockTargetInfo(position, boundingBox, valid: false);
		}
	}

	public enum ClickType
	{
		Single,
		Held,
		None
	}

	public struct InteractionHintData
	{
		public static readonly InteractionHintData None = new InteractionHintData(InteractionTargetType.None, null, null);

		public InteractionTargetType Target;

		public string Name;

		public string Hint;

		public InteractionHintData(InteractionTargetType target, string name, string hint)
		{
			Target = target;
			Name = name;
			Hint = hint;
		}

		public bool Equals(InteractionHintData other)
		{
			return other.Target == Target && other.Name == Name && other.Hint == Hint;
		}
	}

	internal enum RotationMode
	{
		PrePlacement,
		PostPlacement,
		None
	}

	private struct ClickQueueData
	{
		public Stopwatch Timer;

		public float Timeout;

		public int? TargetSlot;
	}

	public interface ChainSyncStorage
	{
		InteractionState ServerState { get; set; }

		void PutInteractionSyncData(int index, InteractionSyncData data);

		void SyncFork(GameInstance gameInstance, SyncInteractionChain packet);
	}

	private class RollbackException : Exception
	{
	}

	public class DebugSelectorMesh
	{
		public readonly Matrix Matrix;

		public Mesh Mesh;

		public float Time;

		public readonly float InitialTime;

		public readonly Vector3 DebugColor;

		public DebugSelectorMesh(Matrix matrix, Mesh mesh, float time, Vector3 debugColor)
		{
			Matrix = matrix;
			Mesh = mesh;
			InitialTime = (Time = time);
			DebugColor = debugColor;
		}
	}

	public const float BlockSubfaceWidth = 0.35f;

	private const string DefaultInteractionHint = "interactionHints.generic";

	private InteractionHintData _interactionHint;

	public InteractionTargetType InteractionTarget = InteractionTargetType.None;

	private const float BlockOutlineIntersectOffset = 0.005f;

	public readonly BlockPlacementPreview BlockPreview;

	public readonly BlockBreakHealth BlockBreakHealth;

	private readonly BlockOutlineRenderer _blockOutlineRenderer;

	private readonly HitDetection.RaycastOptions _targetBlockRaycastOptions = new HitDetection.RaycastOptions
	{
		IgnoreFluids = true,
		CheckOversizedBoxes = true
	};

	private bool _placingAtRange = false;

	private bool _fluidityActive = false;

	private BlockTargetInfo? _targetBlockInfo;

	private static readonly Axis[] AxisValues = Enum.GetValues(typeof(Axis)) as Axis[];

	private static readonly Rotation[] RotationValues = Enum.GetValues(typeof(Rotation)) as Rotation[];

	private Axis _currentBlockRotationAxis = (Axis)0;

	private IntVector3? _currentLockedBlockPosition = null;

	private int _currentBlockId = -1;

	private int _currentRotatedBlockId = -1;

	private readonly Dictionary<Axis, Rotation> _rotationMatrix = AxisValues.ToDictionary((Axis axis) => axis, (Axis axis) => (Rotation)0);

	public RotationMode CurrentRotationMode = RotationMode.None;

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private static readonly int InteractionTypeLength = typeof(InteractionType).GetEnumNames().Length;

	public const float DefaultCooldown = 0.35f;

	public static readonly float[] DefaultChargeTimes = new float[1];

	public bool ShowSelectorDebug = false;

	public ClientRootInteraction[] RootInteractions;

	public ClientInteraction[] Interactions;

	private readonly Dictionary<string, Cooldown> _cooldowns = new Dictionary<string, Cooldown>();

	public readonly Dictionary<int, InteractionChain> Chains = new Dictionary<int, InteractionChain>();

	public readonly Random Random = new Random();

	public List<DamageInfo> DamageInfos = new List<DamageInfo>();

	private readonly List<InteractionSyncData> _tempSyncDataList = new List<InteractionSyncData>();

	private int _lastServerChainId;

	private int _lastClientChainId;

	private readonly float[] _globalTimeShift = new float[InteractionTypeLength];

	private RotationAxis _currentRotationAxis = (RotationAxis)0;

	private int _queuedClickTypes;

	private readonly ClickQueueData[] _clickQueueData = new ClickQueueData[InteractionTypeLength];

	private readonly bool[] _disabledInputs = new bool[InteractionTypeLength];

	private readonly bool[] _requireNewClick = new bool[InteractionTypeLength];

	private readonly int[] _activeSlot = new int[InteractionTypeLength];

	private readonly string[] _itemOnClick = new string[InteractionTypeLength];

	private readonly InventorySectionType[] _activeInventory = new InventorySectionType[InteractionTypeLength];

	public readonly List<DebugSelectorMesh> SelectorDebugMeshes = new List<DebugSelectorMesh>();

	public bool HasFoundTargetBlock { get; private set; }

	public HitDetection.RaycastHit TargetBlockHit { get; private set; }

	public Entity TargetEntityHit { get; private set; }

	public bool PlacingAtRange => _placingAtRange;

	public bool FluidityActive
	{
		get
		{
			return _fluidityActive;
		}
		set
		{
			_fluidityActive = value;
		}
	}

	private bool IsTargetPositionValid => _targetBlockInfo?.Valid ?? false;

	public bool HeldBlockCanBePlaced => IsTargetPositionValid && (!BlockPreview.IsEnabled || !_gameInstance.App.Settings.BlockPlacementSupportValidation || BlockPreview.HasSupport);

	public int RotatedBlockIdOverride => (CurrentRotationMode == RotationMode.PrePlacement) ? _currentRotatedBlockId : (-1);

	public bool ShouldForwardMouseWheelEvents => CurrentRotationMode != RotationMode.None || _placingAtRange || _gameInstance.BuilderToolsModule.ShouldSendMouseWheelEventToPlaySelectionTool();

	private void UpdateInteractionTarget()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		_placingAtRange = _gameInstance.Input.IsAltHeld();
		InteractionConfiguration val = _gameInstance.LocalPlayer.PrimaryItem?.InteractionConfiguration;
		GameMode gameMode = _gameInstance.GameMode;
		float value = (((object)(GameMode)(ref gameMode)).Equals((object)(GameMode)0) ? ((!_placingAtRange) ? ((float)_gameInstance.App.Settings.adventureInteractionDistance) : ((float)_gameInstance.App.Settings.CurrentAdventureInteractionDistance)) : ((!_placingAtRange) ? ((float)_gameInstance.App.Settings.creativeInteractionDistance) : ((float)_gameInstance.App.Settings.CurrentCreativeInteractionDistance)));
		if (!_placingAtRange)
		{
			val?.UseDistance?.TryGetValue(_gameInstance.GameMode, out value);
		}
		bool interactFromEntity = _gameInstance.CameraModule.Controller.InteractFromEntity;
		_targetBlockRaycastOptions.Distance = (interactFromEntity ? (value + _gameInstance.CameraModule.Controller.PositionOffset.Length()) : value);
		_targetBlockRaycastOptions.CheckOnlyTangibleEntities = !(val?.AllEntities ?? false);
		_targetBlockRaycastOptions.ReturnEndpointBlock = _placingAtRange;
		Ray lookRay = _gameInstance.CameraModule.GetLookRay();
		_gameInstance.HitDetection.Raycast(lookRay.Position, lookRay.Direction, _targetBlockRaycastOptions, out var hasFoundTargetBlock, out var blockHitData, out var _, out var entityHitData);
		if (_placingAtRange && hasFoundTargetBlock && _gameInstance.App.Settings.InteractionDistanceIsMinimum(_gameInstance.GameMode))
		{
			Vector3 vector = Vector3.Floor(_gameInstance.LocalPlayer.Position);
			Vector3 vector2 = new Vector3(blockHitData.BlockPosition.X, vector.Y - 1f, blockHitData.BlockPosition.Z);
			int block = _gameInstance.MapModule.GetBlock((int)vector2.X, (int)vector2.Y, (int)vector2.Z, 1);
			ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[block];
			int boxId = 0;
			blockHitData = new HitDetection.RaycastHit(vector2, vector2, vector2, vector2, blockHitData.HitPosition, Vector3.UnitY, Vector3.UnitY, blockHitData.TextureCoord, blockHitData.Distance, block, clientBlockType.HitboxType, boxId);
		}
		HasFoundTargetBlock = hasFoundTargetBlock;
		HitDetection.RaycastHit targetBlockHit = TargetBlockHit;
		TargetBlockHit = blockHitData;
		TargetEntityHit = entityHitData.Entity;
		InteractionTarget = InteractionTargetType.None;
		InteractionHintData interactionHintData = InteractionHintData.None;
		if (InteractionTarget == InteractionTargetType.None && TargetEntityHit != null)
		{
			bool flag = true;
			if (interactFromEntity)
			{
				float num = (_gameInstance.CameraModule.Controller.AttachmentPosition - entityHitData.RayBoxCollision.Position).Length();
				flag = num < value;
			}
			if (flag)
			{
				InteractionTarget = InteractionTargetType.Entity;
				if (TargetEntityHit.IsUsable())
				{
					interactionHintData = new InteractionHintData(InteractionTargetType.Entity, TargetEntityHit.Name, "interactionHints.generic");
				}
			}
		}
		else if (InteractionTarget == InteractionTargetType.None && HasFoundTargetBlock)
		{
			bool flag2 = true;
			if (interactFromEntity)
			{
				float num2 = (_gameInstance.CameraModule.Controller.AttachmentPosition - TargetBlockHit.HitPosition).Length();
				flag2 = _placingAtRange || num2 < value;
			}
			if (flag2)
			{
				InteractionTarget = InteractionTargetType.Block;
				if (_gameInstance.MapModule.ClientBlockTypes[TargetBlockHit.BlockId].IsUsable)
				{
					interactionHintData = new InteractionHintData(InteractionTargetType.Block, _gameInstance.MapModule.ClientBlockTypes[TargetBlockHit.BlockId].Item, _gameInstance.MapModule.ClientBlockTypes[TargetBlockHit.BlockId].InteractionHint ?? "interactionHints.generic");
				}
			}
		}
		if (!interactionHintData.Equals(_interactionHint))
		{
			_gameInstance.App.Interface.TriggerEvent("crosshair.setInteractionHint", interactionHintData);
			_interactionHint = interactionHintData;
		}
	}

	private BlockTargetInfo GetHeldBlockTargetInfo(out int heldBlockId, out int targetX, out int targetY, out int targetZ)
	{
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		heldBlockId = _gameInstance.LocalPlayer.PrimaryItem?.BlockId ?? 0;
		if (CurrentRotationMode != RotationMode.None && _currentRotatedBlockId != -1)
		{
			heldBlockId = _currentRotatedBlockId;
		}
		int num = _gameInstance.LocalPlayer.PrimaryItem?.BlockId ?? 0;
		if (_currentBlockId != num)
		{
			_currentBlockId = num;
			if (CurrentRotationMode != RotationMode.None)
			{
				ClientBlockType blockType = _gameInstance.MapModule.ClientBlockTypes[_currentBlockId];
				ClientBlockType clientBlockType = TryGetRotatedVariant(blockType, _currentBlockRotationAxis, _rotationMatrix[_currentBlockRotationAxis]);
				heldBlockId = clientBlockType.Id;
				_currentRotatedBlockId = heldBlockId;
			}
		}
		int num2 = heldBlockId;
		ClientBlockType clientBlockType2 = _gameInstance.MapModule.ClientBlockTypes[heldBlockId];
		(bool, IntVector3?) tuple = PlaceBlockInteraction.TryGetPlacementPosition(_gameInstance, clientBlockType2, out targetX, out targetY, out targetZ);
		if (!tuple.Item1)
		{
			return BlockTargetInfo.FromFailedBlocks(_gameInstance, new IntVector3(targetX, targetY, targetZ), tuple.Item2);
		}
		int block = _gameInstance.MapModule.GetBlock(targetX, targetY, targetZ, 1);
		ClientBlockType targetBlockType = _gameInstance.MapModule.ClientBlockTypes[block];
		if (CurrentRotationMode == RotationMode.None)
		{
			heldBlockId = PlaceBlockInteraction.GetPlacedBlockVariant(_gameInstance, clientBlockType2, targetBlockType, targetX, targetY, targetZ);
			clientBlockType2 = _gameInstance.MapModule.ClientBlockTypes[heldBlockId];
			_rotationMatrix[(Axis)0] = clientBlockType2.RotationYaw;
			_rotationMatrix[(Axis)1] = clientBlockType2.RotationPitch;
			_rotationMatrix[(Axis)2] = clientBlockType2.RotationRoll;
		}
		tuple = PlaceBlockInteraction.TryGetPlacementPosition(_gameInstance, clientBlockType2, out targetX, out targetY, out targetZ);
		if (!tuple.Item1)
		{
			heldBlockId = num2;
			return BlockTargetInfo.FromFailedBlocks(_gameInstance, new IntVector3(targetX, targetY, targetZ), tuple.Item2);
		}
		if (PlaceBlockInteraction.IsEntityBlockingPlacement(_gameInstance, clientBlockType2, targetX, targetY, targetZ, out var collisionArea))
		{
			return new BlockTargetInfo(new IntVector3(targetX, targetY, targetZ), collisionArea, valid: false);
		}
		if (!PlaceBlockInteraction.CanPlaceBlock(_gameInstance, clientBlockType2, targetX, targetY, targetZ, out var collisionArea2))
		{
			return new BlockTargetInfo(new IntVector3(targetX, targetY, targetZ), collisionArea2, valid: false);
		}
		return new BlockTargetInfo(new IntVector3(targetX, targetY, targetZ), valid: true);
	}

	private void UpdateBlockPreview()
	{
		_targetBlockInfo = GetHeldBlockTargetInfo(out var heldBlockId, out var targetX, out var targetY, out var targetZ);
		if (!BlockPreview.IsEnabled)
		{
			BlockPreview.IsVisible = false;
			return;
		}
		if (!IsTargetPositionValid)
		{
			if (BlockPreview.BlockId != heldBlockId)
			{
				BlockPreview.IsVisible = false;
			}
			return;
		}
		if (InteractionTarget != InteractionTargetType.Block || (_gameInstance.LocalPlayer.PrimaryItem?.BlockId ?? 0) == 0)
		{
			BlockPreview.IsVisible = false;
			return;
		}
		if (_currentLockedBlockPosition.HasValue)
		{
			IntVector3 intVector = _currentLockedBlockPosition ?? throw new InvalidOperationException();
			targetX = intVector.X;
			targetY = intVector.Y;
			targetZ = intVector.Z;
		}
		BlockPreview.UpdatePreview(heldBlockId, targetX, targetY, targetZ);
	}

	public bool TargetBlockOutineNeedsDrawing()
	{
		if (!_gameInstance.App.InGame.IsHudVisible || InteractionTarget != InteractionTargetType.Block || !(_gameInstance.LocalPlayer.PrimaryItem?.InteractionConfiguration?.DisplayOutlines).GetValueOrDefault(true))
		{
			return false;
		}
		int num = (int)System.Math.Floor(TargetBlockHit.BlockPosition.X);
		int num2 = (int)System.Math.Floor(TargetBlockHit.BlockPosition.Y);
		int num3 = (int)System.Math.Floor(TargetBlockHit.BlockPosition.Z);
		int worldChunkX = num >> 5;
		int worldChunkY = num2 >> 5;
		int worldChunkZ = num3 >> 5;
		Chunk chunk = _gameInstance.MapModule.GetChunk(worldChunkX, worldChunkY, worldChunkZ);
		if (chunk == null)
		{
			return false;
		}
		int blockIndex = ChunkHelper.IndexOfWorldBlockInChunk(num, num2, num3);
		int slotIndex;
		float hitTimer;
		bool flag = chunk.Data.TryGetBlockHitTimer(blockIndex, out slotIndex, out hitTimer);
		return !flag;
	}

	public void DrawTargetBlockOutline(ref Vector3 cameraPosition, ref Matrix viewRotationProjectionMatrix)
	{
		if (!TargetBlockOutineNeedsDrawing())
		{
			throw new Exception("DrawTargetBlockOutline called when it was not required. Please check with RequestsDrawing() first before calling this.");
		}
		ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[TargetBlockHit.BlockId];
		BlockHitbox hitbox = _gameInstance.ServerSettings.BlockHitboxes[clientBlockType.HitboxType];
		bool valueOrDefault = (_gameInstance.LocalPlayer.PrimaryItem?.InteractionConfiguration?.DebugOutlines).GetValueOrDefault();
		Vector3 vector = TargetBlockHit.BlockOrigin - cameraPosition;
		Vector3 position = vector;
		position.X += (float)((!(vector.X > 0f)) ? 1 : (-1)) * 0.005f;
		position.Y += (float)((!(vector.Y > 0f)) ? 1 : (-1)) * 0.005f;
		position.Z += (float)((!(vector.Z > 0f)) ? 1 : (-1)) * 0.005f;
		_blockOutlineRenderer.Draw(position, hitbox, viewRotationProjectionMatrix, valueOrDefault);
		if (!_gameInstance.App.Settings.DisplayBlockBoundaries || !_targetBlockInfo.HasValue)
		{
			return;
		}
		BlockTargetInfo value = _targetBlockInfo.Value;
		List<(BoundingBox, float)> list = new List<(BoundingBox, float)>();
		BoundingBox[] collidingBoxes = value.CollidingBoxes;
		foreach (BoundingBox boundingBox in collidingBoxes)
		{
			BoundingBox item = boundingBox;
			item.Grow(new Vector3(0.005f));
			list.Add((item, 1f));
		}
		if (!value.Valid)
		{
			BoundingBox item2 = new BoundingBox(value.Position, value.Position + Vector3.One);
			item2.Grow(new Vector3(-0.005f));
			list.Add((item2, 0.5f));
		}
		if (list.Count <= 0)
		{
			return;
		}
		GraphicsDevice graphics = _gameInstance.Engine.Graphics;
		BoxRenderer boxRenderer = new BoxRenderer(graphics, graphics.GPUProgramStore.BasicProgram);
		BoundingBox hitbox2 = _gameInstance.LocalPlayer.Hitbox;
		hitbox2.Translate(_gameInstance.LocalPlayer.Position);
		foreach (var (box, num) in list)
		{
			if (box.Contains(hitbox2) == ContainmentType.Disjoint)
			{
				boxRenderer.Draw(-cameraPosition, box, viewRotationProjectionMatrix, graphics.RedColor, 0.4f * num, graphics.RedColor, 0.15f * num);
			}
		}
		boxRenderer.Dispose();
	}

	public void DrawTargetBlockSubface(ref Vector3 cameraPosition, ref Matrix viewRotationProjectionMatrix, ClientBlockType blockType)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Invalid comparison between Unknown and I4
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0330: Invalid comparison between Unknown and I4
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Invalid comparison between Unknown and I4
		VariantRotation variantRotation = blockType.VariantRotation;
		BlockHitbox blockHitbox = _gameInstance.ServerSettings.BlockHitboxes[blockType.HitboxType].Clone();
		bool flag = blockHitbox.BoundingBox.Max.X - blockHitbox.BoundingBox.Min.X > 1f || blockHitbox.BoundingBox.Max.Y - blockHitbox.BoundingBox.Min.Y > 1f || blockHitbox.BoundingBox.Max.Z - blockHitbox.BoundingBox.Min.Z > 1f;
		Vector3 position = TargetBlockHit.BlockPosition - cameraPosition;
		if (TargetBlockHit.BlockNormal.X > 0f || TargetBlockHit.BlockNormal.Y > 0f || TargetBlockHit.BlockNormal.Z > 0f)
		{
			position += TargetBlockHit.BlockNormal;
		}
		float x = (float)System.Math.Sign(TargetBlockHit.BlockNormal.X) * 0.001f;
		float y = (float)System.Math.Sign(TargetBlockHit.BlockNormal.Y) * 0.001f;
		float z = (float)System.Math.Sign(TargetBlockHit.BlockNormal.Z) * 0.001f;
		BoundingBox box = new BoundingBox(Vector3.Zero, Vector3.One);
		Vector3 vector = TargetBlockHit.HitPosition - TargetBlockHit.BlockOrigin;
		float num = System.Math.Abs(vector.X - 0.5f);
		float num2 = System.Math.Abs(vector.Y - 0.5f);
		float num3 = System.Math.Abs(vector.Z - 0.5f);
		float num4 = 0.65f;
		if (!flag && (int)variantRotation == 6)
		{
			if (TargetBlockHit.BlockNormal.X != 0f)
			{
				box.Max.X = x;
				box.Min.X = x;
				if (vector.Y > num4)
				{
					box.Min.Y = num4;
				}
			}
			if (TargetBlockHit.BlockNormal.Y != 0f)
			{
				box.Max.Y = y;
				box.Min.Y = y;
			}
			if (TargetBlockHit.BlockNormal.Z != 0f)
			{
				box.Max.Z = z;
				box.Min.Z = z;
				if (vector.Y > num4)
				{
					box.Min.Y = num4;
				}
			}
		}
		else if (!flag && ((int)variantRotation == 3 || (int)variantRotation == 4))
		{
			if (TargetBlockHit.BlockNormal.X != 0f)
			{
				box.Max.X = x;
				box.Min.X = x;
				if (num3 > num2)
				{
					if (vector.Z > num4)
					{
						box.Min.Z = num4;
					}
					else if (vector.Z < 0.35f)
					{
						box.Max.Z = 0.35f;
					}
				}
				else if (vector.Y > num4)
				{
					box.Min.Y = num4;
				}
				else if (vector.Y < 0.35f)
				{
					box.Max.Y = 0.35f;
				}
			}
			if (TargetBlockHit.BlockNormal.Y != 0f)
			{
				box.Max.Y = y;
				box.Min.Y = y;
				if (num > num3)
				{
					if (vector.X > num4)
					{
						box.Min.X = num4;
					}
					else if (vector.X < 0.35f)
					{
						box.Max.X = 0.35f;
					}
				}
				else if (vector.Z > num4)
				{
					box.Min.Z = num4;
				}
				else if (vector.Z < 0.35f)
				{
					box.Max.Z = 0.35f;
				}
			}
			if (TargetBlockHit.BlockNormal.Z != 0f)
			{
				box.Max.Z = z;
				box.Min.Z = z;
				if (num > num2)
				{
					if (vector.X > num4)
					{
						box.Min.X = num4;
					}
					else if (vector.X < 0.35f)
					{
						box.Max.X = 0.35f;
					}
				}
				else if (vector.Y > num4)
				{
					box.Min.Y = num4;
				}
				else if (vector.Y < 0.35f)
				{
					box.Max.Y = 0.35f;
				}
			}
		}
		else
		{
			if (TargetBlockHit.BlockNormal.X != 0f)
			{
				box.Max.X = x;
				box.Min.X = x;
			}
			if (TargetBlockHit.BlockNormal.Y != 0f)
			{
				box.Max.Y = y;
				box.Min.Y = y;
			}
			if (TargetBlockHit.BlockNormal.Z != 0f)
			{
				box.Max.Z = z;
				box.Min.Z = z;
			}
		}
		GraphicsDevice graphics = _gameInstance.Engine.Graphics;
		BoxRenderer boxRenderer = new BoxRenderer(graphics, graphics.GPUProgramStore.BasicProgram);
		boxRenderer.Draw(position, box, viewRotationProjectionMatrix, graphics.WhiteColor, 0.25f, new Vector3(0.4f, 0.694f, 1f), 0.1f);
		boxRenderer.Dispose();
	}

	private void HandleBlockRotationInteractions()
	{
		InputBindings inputBindings = _gameInstance.App.Settings.InputBindings;
		ValidateBlockRotationInteractions();
		if (!_gameInstance.Input.IsShiftHeld())
		{
			ConsumeRotationInteraction(inputBindings.TogglePostRotationMode, OnTogglePostRotationMode);
			ConsumeRotationInteraction(inputBindings.TogglePreRotationMode, OnTogglePreRotationMode);
			if (CurrentRotationMode != RotationMode.None)
			{
				ConsumeRotationInteraction(inputBindings.NextRotationAxis, OnNextRotationAxis);
				ConsumeRotationInteraction(inputBindings.PreviousRotationAxis, OnPrevRotationAxis);
			}
		}
	}

	private void ValidateBlockRotationInteractions()
	{
		if (CurrentRotationMode != RotationMode.None && _currentBlockId != -1 && _gameInstance.MapModule.ClientBlockTypes[_currentBlockId]?.Item != _gameInstance.InventoryModule.GetActiveHotbarItem()?.Id)
		{
			ToggleOrSwitchRotationMode(RotationMode.None);
		}
	}

	private void OnTogglePostRotationMode()
	{
		ToggleOrSwitchRotationMode(RotationMode.PostPlacement);
	}

	private void OnTogglePreRotationMode()
	{
		ToggleOrSwitchRotationMode(RotationMode.PrePlacement);
	}

	private void OnNextRotationAxis()
	{
		SwitchRotationAxis(1);
	}

	private void OnPrevRotationAxis()
	{
		SwitchRotationAxis(-1);
	}

	private void SwitchRotationAxis(int dir)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[_currentBlockId];
		Axis val = _currentBlockRotationAxis;
		do
		{
			val = NextEnumerationValue(AxisValues, val, dir);
			if (HasRotation(clientBlockType.VariantRotation, val))
			{
				_gameInstance.Chat.Log($"Rotating {GetAxisName(_currentBlockRotationAxis)} in {CurrentRotationMode}");
				_currentBlockRotationAxis = val;
				return;
			}
		}
		while (val != _currentBlockRotationAxis);
		PlayErrorSound();
	}

	private static string GetAxisName(Axis axis)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected I4, but got Unknown
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		return (int)axis switch
		{
			0 => "Yaw", 
			1 => "Pitch", 
			2 => "Roll", 
			_ => Enum.GetName(typeof(Axis), axis), 
		};
	}

	private void ToggleOrSwitchRotationMode(RotationMode mode)
	{
		if (CurrentRotationMode == mode)
		{
			mode = RotationMode.None;
		}
		if (mode != RotationMode.None)
		{
			if (InteractionTarget != InteractionTargetType.Block)
			{
				return;
			}
			bool flag = false;
			if (mode == RotationMode.PostPlacement && !TryEnterPostPlacementMode())
			{
				flag = true;
			}
			if (mode == RotationMode.PrePlacement && !TryEnterPrePlacementMode())
			{
				flag = true;
			}
			if (flag)
			{
				PlayErrorSound();
				return;
			}
		}
		else
		{
			if (CurrentRotationMode == RotationMode.PostPlacement)
			{
				LeavePostPlacementMode();
			}
			if (CurrentRotationMode == RotationMode.PrePlacement)
			{
				LeavePrePlacementMode();
			}
			_rotationMatrix[(Axis)0] = (Rotation)0;
			_rotationMatrix[(Axis)1] = (Rotation)0;
			_rotationMatrix[(Axis)2] = (Rotation)0;
		}
		CurrentRotationMode = mode;
		BlockPreview.UpdateEffect();
		_gameInstance.Chat.Log($"Switched to rotation mode: {CurrentRotationMode}");
	}

	private void LeavePrePlacementMode()
	{
		_currentBlockId = -1;
		_currentRotatedBlockId = -1;
		BlockPreview.UpdateEffect();
	}

	private void LeavePostPlacementMode()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		IntVector3 intVector = _currentLockedBlockPosition ?? throw new InvalidOperationException();
		_gameInstance.InjectPacket((ProtoPacket)new ServerSetBlock(intVector.X, intVector.Y, intVector.Z, _currentRotatedBlockId, false));
		_currentBlockId = -1;
		_currentRotatedBlockId = -1;
		_currentLockedBlockPosition = null;
		BlockPreview.UpdateEffect();
	}

	private bool TryEnterPostPlacementMode()
	{
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Expected O, but got Unknown
		HitDetection.RaycastHit targetBlockHit = _gameInstance.InteractionModule.TargetBlockHit;
		IntVector3 value = new IntVector3((int)System.Math.Floor(targetBlockHit.BlockPositionNoFiller.X), (int)System.Math.Floor(targetBlockHit.BlockPositionNoFiller.Y), (int)System.Math.Floor(targetBlockHit.BlockPositionNoFiller.Z));
		int block = _gameInstance.MapModule.GetBlock(value.X, value.Y, value.Z, -1);
		if (block <= 0)
		{
			return false;
		}
		ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[block];
		if (!DoesBlockSupportRotation(clientBlockType))
		{
			return false;
		}
		BlockHitbox blockHitbox = _gameInstance.ServerSettings.BlockHitboxes[clientBlockType.HitboxType];
		if (blockHitbox.IsOversized())
		{
			return false;
		}
		_currentRotatedBlockId = block;
		_currentBlockId = ((clientBlockType.VariantOriginalId == -1) ? clientBlockType.Id : clientBlockType.VariantOriginalId);
		_currentLockedBlockPosition = value;
		_currentBlockRotationAxis = GetSupportedBlockRotations(clientBlockType).First();
		_gameInstance.InjectPacket((ProtoPacket)new ServerSetBlock(value.X, value.Y, value.Z, 0, false));
		return true;
	}

	private bool TryEnterPrePlacementMode()
	{
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		if (_gameInstance.LocalPlayer.PrimaryItem == null)
		{
			return false;
		}
		int blockId = _gameInstance.LocalPlayer.PrimaryItem.BlockId;
		ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[blockId];
		PlaceBlockInteraction.TryGetPlacementPosition(_gameInstance, clientBlockType, out var blockX, out var blockY, out var blockZ);
		int block = _gameInstance.MapModule.GetBlock(blockX, blockY, blockZ, 1);
		ClientBlockType targetBlockType = _gameInstance.MapModule.ClientBlockTypes[block];
		if (!DoesBlockSupportRotation(_gameInstance.MapModule.ClientBlockTypes[blockId]))
		{
			return false;
		}
		_currentBlockId = blockId;
		_currentRotatedBlockId = PlaceBlockInteraction.GetPlacedBlockVariant(_gameInstance, clientBlockType, targetBlockType, blockX, blockY, blockZ);
		_currentBlockRotationAxis = GetSupportedBlockRotations(clientBlockType).First();
		return true;
	}

	private void ConsumeRotationInteraction(InputBinding binding, Action action)
	{
		if (_gameInstance.Input.CanConsumeBinding(binding))
		{
			_gameInstance.Input.ConsumeBinding(binding);
			action();
		}
	}

	public void OnMouseWheelEvent(SDL_Event evt)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		if (evt.wheel.y == 0)
		{
			return;
		}
		if (_gameInstance.BuilderToolsModule.ShouldSendMouseWheelEventToPlaySelectionTool())
		{
			_gameInstance.BuilderToolsModule.SendMouseWheelEventToPlaySelectionTool(System.Math.Sign(evt.wheel.y));
		}
		else if (_placingAtRange)
		{
			GameMode gameMode = _gameInstance.GameMode;
			if (((object)(GameMode)(ref gameMode)).Equals((object)(GameMode)0))
			{
				_gameInstance.App.Settings.CurrentAdventureInteractionDistance += evt.wheel.y;
			}
			else
			{
				_gameInstance.App.Settings.CurrentCreativeInteractionDistance += evt.wheel.y;
			}
		}
		else
		{
			if (_currentBlockId == -1)
			{
				return;
			}
			int direction = System.Math.Sign(evt.wheel.y);
			ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[_currentBlockId];
			if (!HasRotation(clientBlockType.VariantRotation, _currentBlockRotationAxis))
			{
				PlayErrorSound();
				return;
			}
			Rotation val = _rotationMatrix[_currentBlockRotationAxis];
			Rotation val2 = val;
			do
			{
				ClientBlockType clientBlockType2 = TryGetRotatedVariant(clientBlockType, _currentBlockRotationAxis, val2 = NextEnumerationValue(RotationValues, val2, direction));
				if (clientBlockType2.Id != _currentRotatedBlockId)
				{
					_currentRotatedBlockId = clientBlockType2.Id;
					_rotationMatrix[(Axis)0] = clientBlockType2.RotationYaw;
					_rotationMatrix[(Axis)1] = clientBlockType2.RotationPitch;
					_rotationMatrix[(Axis)2] = clientBlockType2.RotationRoll;
					string audioEventForAxisRotation = GetAudioEventForAxisRotation(_currentBlockRotationAxis);
					if (audioEventForAxisRotation != null)
					{
						_gameInstance.AudioModule.PlayLocalSoundEvent(audioEventForAxisRotation);
					}
					break;
				}
			}
			while (val2 != val);
		}
	}

	private void PlayErrorSound()
	{
		_gameInstance.AudioModule.PlayLocalSoundEvent("CREATE_ERROR");
	}

	private ClientBlockType TryGetRotatedVariant(ClientBlockType blockType, Axis axis, Rotation rotation)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Invalid comparison between Unknown and I4
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		Rotation val = (((int)axis == 0) ? rotation : _rotationMatrix[(Axis)0]);
		Rotation val2 = (((int)axis == 1) ? rotation : _rotationMatrix[(Axis)1]);
		Rotation val3 = (((int)axis == 2) ? rotation : _rotationMatrix[(Axis)2]);
		if ((int)val == 0 && (int)val2 == 0 && (int)val3 == 0)
		{
			return blockType;
		}
		int num = blockType.TryGetRotatedVariant(val, val2, val3);
		if (num != blockType.Id)
		{
			return _gameInstance.MapModule.ClientBlockTypes[num];
		}
		return blockType;
	}

	private static T NextEnumerationValue<T>(T[] array, T value, int direction)
	{
		int num = Array.IndexOf(array, value);
		if (num == -1)
		{
			throw new InvalidOperationException($"Value {value} is not valid for enumeration");
		}
		int num2 = num + direction;
		if (num2 < 0)
		{
			return array[^1];
		}
		if (num2 >= array.Length)
		{
			return array[0];
		}
		return array[num2];
	}

	private static bool DoesBlockSupportRotation(ClientBlockType blockType)
	{
		return GetSupportedBlockRotations(blockType).Any();
	}

	private static IEnumerable<Axis> GetSupportedBlockRotations(ClientBlockType blockType)
	{
		return AxisValues.Where((Axis axis) => HasRotation(blockType.VariantRotation, axis));
	}

	private static bool HasRotation(VariantRotation rotation, Axis axis)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected I4, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Invalid comparison between Unknown and I4
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Invalid comparison between Unknown and I4
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Invalid comparison between Unknown and I4
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Invalid comparison between Unknown and I4
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Invalid comparison between Unknown and I4
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Invalid comparison between Unknown and I4
		return (int)rotation switch
		{
			0 => false, 
			1 => (int)axis == 0, 
			2 => (int)axis == 1, 
			3 => (int)axis == 0 || (int)axis == 1, 
			4 => (int)axis == 0 || (int)axis == 1, 
			5 => (int)axis == 0, 
			6 => (int)axis == 0 || (int)axis == 1, 
			7 => true, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	private static string GetAudioEventForAxisRotation(Axis axis)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected I4, but got Unknown
		return (int)axis switch
		{
			0 => "CREATE_ROTATE_YAW", 
			1 => "CREATE_ROTATE_PITCH", 
			2 => "CREATE_ROTATE_ROLL", 
			_ => null, 
		};
	}

	public InteractionModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		BlockPreview = new BlockPlacementPreview(_gameInstance);
		BlockBreakHealth = new BlockBreakHealth(_gameInstance);
		_blockOutlineRenderer = new BlockOutlineRenderer(_gameInstance.Engine.Graphics);
	}

	protected override void DoDispose()
	{
		_blockOutlineRenderer.Dispose();
		BlockBreakHealth.Dispose();
	}

	public void PrepareInteractions(Interaction[] networkInteractions, out ClientInteraction[] upcomingInteractions)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		upcomingInteractions = new ClientInteraction[networkInteractions.Length];
		for (int i = 0; i < networkInteractions.Length; i++)
		{
			Interaction val = networkInteractions[i];
			if (val != null)
			{
				upcomingInteractions[i] = ClientInteraction.Parse(i, val);
			}
		}
	}

	public void PrepareRootInteractions(RootInteraction[] networkRootInteractions, out ClientRootInteraction[] upcomingRootInteractions)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		upcomingRootInteractions = new ClientRootInteraction[networkRootInteractions.Length];
		for (int i = 0; i < networkRootInteractions.Length; i++)
		{
			RootInteraction root = networkRootInteractions[i];
			upcomingRootInteractions[i] = new ClientRootInteraction(i, root);
		}
	}

	public void SetupInteractions(ClientInteraction[] interactions)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		Interactions = interactions;
		if (RootInteractions != null)
		{
			ClientRootInteraction[] rootInteractions = RootInteractions;
			foreach (ClientRootInteraction clientRootInteraction in rootInteractions)
			{
				clientRootInteraction.Build(this);
			}
		}
	}

	public void SetupRootInteractions(ClientRootInteraction[] rootInteractions)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		RootInteractions = rootInteractions;
		ClientRootInteraction[] rootInteractions2 = RootInteractions;
		foreach (ClientRootInteraction clientRootInteraction in rootInteractions2)
		{
			clientRootInteraction.Build(this);
		}
	}

	public void Update(float dt)
	{
		bool flag = true;
		if (_gameInstance.CameraModule.Controller.AttachedTo != _gameInstance.LocalPlayer)
		{
			if (!_interactionHint.Equals(InteractionHintData.None))
			{
				_interactionHint = InteractionHintData.None;
				_gameInstance.App.Interface.TriggerEvent("crosshair.setInteractionHint", InteractionHintData.None);
				InteractionTarget = InteractionTargetType.None;
			}
			flag = false;
		}
		if (flag)
		{
			UpdateInteractionTarget();
			UpdateBlockPreview();
		}
		HandleInteractions(dt, flag);
		DamageInfos.Clear();
	}

	private void RequireNewClick(InteractionType type)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		_requireNewClick[type] = true;
	}

	public void DisableInput(InteractionType type)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		_disabledInputs[type] = true;
	}

	private void QueueClick(InteractionContext context, InteractionType type, float clickQueueTimeout)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		ref ClickQueueData reference = ref _clickQueueData[type];
		if (reference.Timer == null)
		{
			reference.Timer = Stopwatch.StartNew();
			_queuedClickTypes++;
		}
		else
		{
			float num = (float)reference.Timer.ElapsedMilliseconds / 1000f;
			num *= _gameInstance.TimeDilationModifier;
			if (num + clickQueueTimeout < reference.Timeout)
			{
				return;
			}
		}
		reference.Timer.Restart();
		reference.Timeout = clickQueueTimeout;
		reference.TargetSlot = context.MetaStore.TargetSlot;
	}

	public void UpdateCooldown(ClientRootInteraction root, bool click, out bool isOnCooldown)
	{
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		InteractionCooldown cooldown = root.RootInteraction.Cooldown;
		string cooldownId = root.Id;
		float maxTime = 0.35f;
		float[] chargeTimes = DefaultChargeTimes;
		bool interruptRecharge = false;
		if (cooldown != null)
		{
			maxTime = cooldown.Cooldown;
			if (cooldown.ChargeTimes != null && cooldown.ChargeTimes.Length != 0)
			{
				chargeTimes = cooldown.ChargeTimes;
			}
			if (cooldown.CooldownId != null)
			{
				cooldownId = cooldown.CooldownId;
			}
			if (cooldown.InterruptRecharge)
			{
				interruptRecharge = true;
			}
			if (cooldown.ClickBypass && click)
			{
				ResetCooldown(cooldownId, maxTime, chargeTimes, interruptRecharge);
				isOnCooldown = false;
				return;
			}
		}
		if (root.RootInteraction.Settings.TryGetValue(_gameInstance.GameMode, out var value) && value.AllowSkipChainOnClick && click)
		{
			ResetCooldown(cooldownId, maxTime, chargeTimes, interruptRecharge);
			isOnCooldown = false;
		}
		else
		{
			isOnCooldown = IsOnCooldown(root, cooldownId, maxTime, chargeTimes, interruptRecharge);
		}
	}

	public bool IsOnCooldown(ClientRootInteraction root, string cooldownId, float maxTime, float[] chargeTimes, bool interruptRecharge)
	{
		if (maxTime <= 0f)
		{
			return false;
		}
		return GetCooldown(cooldownId, maxTime, chargeTimes, root.RootInteraction.Cooldown == null || !root.RootInteraction.Cooldown.SkipCooldownReset, interruptRecharge)?.HasCooldown(deduct: true) ?? false;
	}

	public void ResetCooldown(string cooldownId, float maxTime, float[] chargeTimes, bool interruptRecharge)
	{
		Cooldown cooldown = GetCooldown(cooldownId, maxTime, chargeTimes, force: true, interruptRecharge);
		cooldown.ResetCooldown();
		cooldown.ResetCharges();
	}

	public Cooldown GetCooldown(string cooldownId, float maxTime, float[] chargeTimes, bool force, bool interruptRecharge)
	{
		if (force && !_cooldowns.ContainsKey(cooldownId))
		{
			_cooldowns.Add(cooldownId, new Cooldown(maxTime, chargeTimes, interruptRecharge));
		}
		return _cooldowns.ContainsKey(cooldownId) ? _cooldowns[cooldownId] : null;
	}

	public Cooldown GetCooldown(string cooldownId)
	{
		if (_cooldowns.TryGetValue(cooldownId, out var value))
		{
			return value;
		}
		return null;
	}

	private void HandleInteractions(float dt, bool allInteractions)
	{
		for (int i = 0; i < _disabledInputs.Length; i++)
		{
			_disabledInputs[i] = false;
		}
		Tick(dt);
		InputBindings inputBindings = _gameInstance.App.Settings.InputBindings;
		if (allInteractions)
		{
			if (!_gameInstance.LocalPlayer.IsMounting && _gameInstance.InteractionModule.CurrentRotationMode != RotationMode.PostPlacement)
			{
				ConsumeInteractionType(inputBindings.PrimaryItemAction, (InteractionType)0);
				ConsumeInteractionType(inputBindings.SecondaryItemAction, (InteractionType)1);
				ConsumeInteractionType(inputBindings.TertiaryItemAction, (InteractionType)2);
				ConsumeInteractionType(inputBindings.Ability1ItemAction, (InteractionType)3);
				ConsumeInteractionType(inputBindings.Ability2ItemAction, (InteractionType)4);
				ConsumeInteractionType(inputBindings.Ability3ItemAction, (InteractionType)5);
				ConsumeInteractionType(inputBindings.BlockInteractAction, (InteractionType)7);
				ConsumeInteractionType(inputBindings.PickBlock, (InteractionType)8);
				ConsumeInteractionType(inputBindings.Sprint, (InteractionType)25);
				TryRunHeldInteraction((InteractionType)22);
				TryRunHeldInteraction((InteractionType)23);
				for (int j = 0; j < _gameInstance.InventoryModule._armorInventory.Length; j++)
				{
					TryRunHeldInteraction((InteractionType)24, j);
				}
			}
			else if (_gameInstance.Input.ConsumeBinding(inputBindings.DismountAction))
			{
				_gameInstance.CharacterControllerModule.DismountNpc(isLocalInteraction: true);
			}
		}
		bool flag = false;
		for (int k = 0; k < 9; k++)
		{
			InputBinding hotbarSlot = inputBindings.GetHotbarSlot(k);
			if (_gameInstance.Input.CanConsumeBinding(hotbarSlot))
			{
				if (!flag)
				{
					ConsumeInteractionType(inputBindings.GetHotbarSlot(k), (InteractionType)15, k);
					flag = true;
				}
				_gameInstance.Input.ConsumeBinding(hotbarSlot);
			}
		}
		HandleBlockRotationInteractions();
		ConsumeClickQueue();
	}

	private bool CanPlayerUseInteraction(InteractionType type)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		return (int)_gameInstance.GameMode == 1 || !_gameInstance.LocalPlayer.IsInteractionDisabled(type);
	}

	private void TryRunHeldInteraction(InteractionType type, int? equipSlot = null)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected I4, but got Unknown
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		if (!CanPlayerUseInteraction(type))
		{
			return;
		}
		InventoryModule inventoryModule = _gameInstance.InventoryModule;
		ClientItemStack clientItemStack;
		switch (type - 22)
		{
		case 0:
			clientItemStack = inventoryModule.GetHotbarItem(inventoryModule.HotbarActiveSlot);
			break;
		case 1:
			clientItemStack = inventoryModule.GetUtilityItem(inventoryModule.UtilityActiveSlot);
			break;
		case 2:
			if (!equipSlot.HasValue)
			{
				throw new ArgumentException();
			}
			clientItemStack = inventoryModule.GetArmorItem(equipSlot.Value);
			break;
		default:
			throw new ArgumentException();
		}
		ClientItemBase item = _gameInstance.ItemLibraryModule.GetItem(clientItemStack?.Id);
		int value = 0;
		if ((item?.Interactions?.TryGetValue(type, out value)).GetValueOrDefault())
		{
			ClientRootInteraction rootInteraction = RootInteractions[value];
			if (CanRun(type, equipSlot.GetValueOrDefault(-1), rootInteraction))
			{
				InventoryModule inventoryModule2 = _gameInstance.InventoryModule;
				InteractionContext context = InteractionContext.ForInteraction(_gameInstance, inventoryModule2, type, equipSlot);
				StartChain(context, type, ClickType.None, null);
			}
		}
	}

	public void ConsumeInteractionType(InputBinding binding, InteractionType type, int? targetSlot = null)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		if ((binding != null && _gameInstance.LocalPlayer.GetRelativeMovementStates().IsMantling) || (binding != null && !_gameInstance.Input.IsBindingHeld(binding)) || _disabledInputs[type] || !CanPlayerUseInteraction(type))
		{
			return;
		}
		InventoryModule inventoryModule = _gameInstance.InventoryModule;
		InteractionContext interactionContext = InteractionContext.ForInteraction(_gameInstance, inventoryModule, type);
		if (targetSlot.HasValue)
		{
			if (targetSlot == inventoryModule.HotbarActiveSlot && !inventoryModule.UsingToolsItem())
			{
				return;
			}
			interactionContext.MetaStore.TargetSlot = targetSlot;
		}
		ClickType clickType = ClickType.Single;
		if (binding == null || _gameInstance.Input.CanConsumeBinding(binding))
		{
			_requireNewClick[type] = false;
		}
		else
		{
			if (_requireNewClick[type])
			{
				return;
			}
			if (_itemOnClick[type] != null && interactionContext.HeldItem?.Id != _itemOnClick[type] && _activeSlot[type] == interactionContext.HeldItemSlot && _activeInventory[type] == interactionContext.HeldItemSectionId)
			{
				_requireNewClick[type] = true;
				return;
			}
			clickType = ClickType.Held;
		}
		if (StartChain(interactionContext, type, clickType, null))
		{
			if (binding != null)
			{
				_gameInstance.Input.ConsumeBinding(binding);
			}
			if (clickType == ClickType.Single || _itemOnClick[type] == null || _activeSlot[type] != interactionContext.HeldItemSlot || _activeInventory[type] != interactionContext.HeldItemSectionId)
			{
				_itemOnClick[type] = interactionContext.HeldItem?.Id;
				_activeSlot[type] = interactionContext.HeldItemSlot;
				_activeInventory[type] = interactionContext.HeldItemSectionId;
			}
		}
	}

	public InputBinding GetInputBindingForType(InteractionType type)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected I4, but got Unknown
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		InputBindings inputBindings = _gameInstance.App.Settings.InputBindings;
		switch ((int)type)
		{
		case 0:
			return inputBindings.PrimaryItemAction;
		case 1:
			return inputBindings.SecondaryItemAction;
		case 2:
			return inputBindings.TertiaryItemAction;
		case 3:
			return inputBindings.Ability1ItemAction;
		case 4:
			return inputBindings.Ability2ItemAction;
		case 5:
			return inputBindings.Ability3ItemAction;
		case 7:
			return inputBindings.BlockInteractAction;
		case 8:
			return inputBindings.PickBlock;
		case 14:
		case 15:
			return inputBindings.HotbarSlot1;
		default:
			throw new ArgumentOutOfRangeException("type", type, null);
		}
	}

	private void ConsumeClickQueue()
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		if (Chains.Count > 0 || _queuedClickTypes == 0)
		{
			return;
		}
		for (int i = 0; i < _clickQueueData.Length; i++)
		{
			ref ClickQueueData reference = ref _clickQueueData[i];
			if (reference.Timer == null)
			{
				continue;
			}
			float num = (float)reference.Timer.ElapsedMilliseconds / 1000f;
			num *= _gameInstance.TimeDilationModifier;
			if (!(num > reference.Timeout))
			{
				InteractionType type = (InteractionType)i;
				InputBinding inputBindingForType = GetInputBindingForType(type);
				InventoryModule inventoryModule = _gameInstance.InventoryModule;
				InteractionContext interactionContext = InteractionContext.ForInteraction(_gameInstance, inventoryModule, type);
				if (reference.TargetSlot.HasValue)
				{
					interactionContext.MetaStore.TargetSlot = reference.TargetSlot;
				}
				if (StartChain(interactionContext, type, (!_gameInstance.Input.CanConsumeBinding(inputBindingForType)) ? ClickType.Held : ClickType.Single, null))
				{
					reference.Timer = null;
					_queuedClickTypes--;
					break;
				}
			}
		}
	}

	private void Tick(float dt)
	{
		for (int i = 0; i < _globalTimeShift.Length; i++)
		{
			_globalTimeShift[i] = 0f;
		}
		if (_cooldowns.Count > 0)
		{
			KeyValuePair<string, Cooldown>[] array = _cooldowns.ToArray();
			for (int j = 0; j < array.Length; j++)
			{
				KeyValuePair<string, Cooldown> keyValuePair = array[j];
				Cooldown value = keyValuePair.Value;
				if (value.Tick(dt))
				{
					_cooldowns.Remove(keyValuePair.Key);
				}
			}
		}
		if (Chains.Count == 0)
		{
			return;
		}
		KeyValuePair<int, InteractionChain>[] array2 = Chains.ToArray();
		for (int k = 0; k < array2.Length; k++)
		{
			KeyValuePair<int, InteractionChain> keyValuePair2 = array2[k];
			InteractionChain value2 = keyValuePair2.Value;
			if (TickChain(value2))
			{
				Chains.Remove(keyValuePair2.Key);
			}
		}
	}

	private bool TickChain(InteractionChain chain)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Invalid comparison between Unknown and I4
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Invalid comparison between Unknown and I4
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Invalid comparison between Unknown and I4
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Invalid comparison between Unknown and I4
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Invalid comparison between Unknown and I4
		KeyValuePair<ulong, InteractionChain>[] array = chain.ForkedChains.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<ulong, InteractionChain> keyValuePair = array[i];
			InteractionChain value = keyValuePair.Value;
			if (TickChain(value))
			{
				chain.ForkedChains.Remove(keyValuePair.Key);
			}
		}
		if ((int)chain.ClientState != 4)
		{
			TryCancelAndRevert(chain);
			if ((int)chain.ServerState != 4)
			{
				if (Logger.IsEnabled(LogLevel.Trace))
				{
					Logger.Trace($"Remove Chain: {chain.ChainId}, {chain}");
				}
				chain.OnCompletion?.Invoke();
				chain.OnCompletion = null;
				if (chain.ForkedChains.Count == 0)
				{
					return true;
				}
			}
			else
			{
				long elapsedMilliseconds = chain.WaitingForServerFinished.ElapsedMilliseconds;
				if (Logger.IsEnabled(LogLevel.Trace))
				{
					Logger.Trace($"Client finished chain but server hasn't! {chain.ChainId}, {chain}, {TimeHelper.FormatMillis(elapsedMilliseconds)}");
				}
				chain.WaitingForServerFinished.Start();
				if ((float)elapsedMilliseconds > _gameInstance.TimeModule.OperationTimeoutThreshold && Logger.IsEnabled(LogLevel.Trace))
				{
					Logger.Trace("Error: Server took too long to finish chain!");
				}
			}
			return false;
		}
		DoTickChain(chain, (InteractionModule module, InteractionChain c, ClickType clickType, bool hasAnyButtonClick, object p) => module.ClientTick(c, clickType, hasAnyButtonClick), null);
		TryCancelAndRevert(chain);
		if ((int)chain.ClientState != 4)
		{
			if (chain.HasTempSyncData)
			{
				throw new Exception("Finished yet server took a different route?");
			}
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.Trace($"Client finished chain: {chain.ChainId}, {chain} in {chain.Time.Elapsed.TotalSeconds:f}s");
			}
			if ((int)chain.ServerState != 4)
			{
				if (Logger.IsEnabled(LogLevel.Trace))
				{
					Logger.Trace($"Remove Chain: {chain.ChainId}, {chain}");
				}
				chain.OnCompletion?.Invoke();
				chain.OnCompletion = null;
				if (chain.ForkedChains.Count == 0)
				{
					return true;
				}
			}
		}
		else if ((int)chain.ServerState != 4)
		{
			long elapsedMilliseconds2 = chain.WaitingForClientFinished.ElapsedMilliseconds;
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.Trace($"Server finished chain but client hasn't! {chain.ChainId}, {chain}, {TimeHelper.FormatMillis(elapsedMilliseconds2)}");
			}
			chain.WaitingForClientFinished.Start();
			if ((float)elapsedMilliseconds2 > _gameInstance.TimeModule.OperationTimeoutThreshold && Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.Trace("Error: Server finished chain earlier than client!");
			}
		}
		return false;
	}

	private void TryCancelAndRevert(InteractionChain chain)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Invalid comparison between Unknown and I4
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Invalid comparison between Unknown and I4
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		int num = chain.PreviousInteractionEntry?.Index ?? 0;
		if (!chain.ServerCancelled || chain.OperationIndex < num || ((int)chain.ClientState == 3 && (int)chain.ServerState == 3))
		{
			return;
		}
		chain.ClearIncompleteSyncData();
		DoRollback(chain, num);
		chain.ClientState = (InteractionState)3;
		chain.ServerState = (InteractionState)3;
		if (chain.GetInteraction(chain.OperationIndex, out var ret))
		{
			try
			{
				ClientRootInteraction rootInteraction = chain.RootInteraction;
				ClientRootInteraction.Operation operation = rootInteraction.Operations[chain.OperationCounter];
				ret.State.State = (InteractionState)3;
				chain.Context.InitEntry(chain, ret, _gameInstance);
				operation.Handle(_gameInstance, firstRun: false, ret.TimeOffset, chain.Type, chain.Context);
			}
			finally
			{
				chain.Context.DeinitEntry(chain, ret, _gameInstance);
			}
		}
		if (chain.ServerCancelled)
		{
			chain.ClearSyncData();
		}
		SendSyncPacket(chain, chain.OperationIndex, null, force: true);
	}

	private void DoTickChain<T>(InteractionChain chain, Func<InteractionModule, InteractionChain, ClickType, bool, T, InteractionSyncData> tickFunc, T param, bool force = false)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		List<InteractionSyncData> tempSyncDataList = _tempSyncDataList;
		tempSyncDataList.Clear();
		ClientRootInteraction rootInteraction = chain.RootInteraction;
		int num = rootInteraction.Operations.Length;
		int operationCounter = chain.OperationCounter;
		int operationIndex = chain.OperationIndex;
		int callDepth = chain.GetCallDepth();
		ClickType clickType;
		bool arg = HasAnyButtonClick(chain.Type, out clickType, chain.Context);
		if (chain.ConsumeFirstRun())
		{
			if (chain.ForkedChainId == null)
			{
				chain.TimeShift = GetGlobalTimeShift(chain.Type);
			}
		}
		else
		{
			chain.TimeShift = 0f;
		}
		while (true)
		{
			int serverCompleteIndex = chain.ServerCompleteIndex;
			if (chain.ServerCancelled && chain.OperationIndex >= serverCompleteIndex)
			{
				break;
			}
			try
			{
				tempSyncDataList.Add(tickFunc(this, chain, clickType, arg, param));
			}
			catch (RollbackException)
			{
				SendSyncPacket(chain, operationIndex, tempSyncDataList);
				DoRollback(chain, chain.OperationIndex - 1);
				return;
			}
			if (callDepth != chain.GetCallDepth())
			{
				callDepth = chain.GetCallDepth();
				rootInteraction = chain.RootInteraction;
				num = rootInteraction.Operations.Length;
			}
			else if (operationCounter == chain.OperationCounter)
			{
				break;
			}
			chain.NextOperationIndex();
			operationCounter = chain.OperationCounter;
			if (operationCounter < num)
			{
				continue;
			}
			while (callDepth > 0)
			{
				chain.PopRoot();
				callDepth = chain.GetCallDepth();
				operationCounter = chain.OperationCounter;
				rootInteraction = chain.RootInteraction;
				num = rootInteraction.Operations.Length;
				if (operationCounter < num || callDepth == 0)
				{
					break;
				}
			}
			if (callDepth == 0 && operationCounter >= num)
			{
				break;
			}
		}
		chain.UpdateClientState(_gameInstance, clickType);
		SendSyncPacket(chain, operationIndex, tempSyncDataList, force);
	}

	private InteractionSyncData ClientTick(InteractionChain chain, ClickType clickType, bool hasAnyButtonClick)
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Invalid comparison between Unknown and I4
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		ClientRootInteraction rootInteraction = chain.RootInteraction;
		ClientRootInteraction.Operation operation = rootInteraction.Operations[chain.OperationCounter];
		InteractionEntry orCreateInteractionEntry = chain.GetOrCreateInteractionEntry(chain.OperationIndex);
		if (orCreateInteractionEntry.ServerState == null)
		{
			InteractionSyncData interactionSyncData = chain.GetInteractionSyncData(chain.OperationIndex);
			if (interactionSyncData != null && (orCreateInteractionEntry.State.OperationCounter != interactionSyncData.OperationCounter || orCreateInteractionEntry.State.RootInteraction != interactionSyncData.RootInteraction))
			{
				throw new RollbackException();
			}
			chain.RemoveInteractionSyncData(chain.OperationIndex);
			orCreateInteractionEntry.ServerState = interactionSyncData;
		}
		if ((int)operation.GetWaitForDataFrom(_gameInstance) == 1 && orCreateInteractionEntry.ServerState == null)
		{
			long elapsedMilliseconds = orCreateInteractionEntry.WaitingForSyncData.ElapsedMilliseconds;
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.Trace($"Wait for interaction serverData: {chain.OperationIndex}, {orCreateInteractionEntry}, {TimeHelper.FormatMillis(elapsedMilliseconds)}");
			}
			orCreateInteractionEntry.WaitingForSyncData.Start();
			if ((float)elapsedMilliseconds > _gameInstance.TimeModule.OperationTimeoutThreshold && Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.Trace("Error: Server took too long to send serverData!");
			}
			return null;
		}
		int clientDataHashCode = orCreateInteractionEntry.GetClientDataHashCode();
		InteractionContext context = chain.Context;
		float num = orCreateInteractionEntry.TimeOffset + (float)orCreateInteractionEntry.Time.Elapsed.TotalSeconds;
		bool flag = !orCreateInteractionEntry.Time.IsRunning;
		if (flag)
		{
			num = chain.TimeShift;
			orCreateInteractionEntry.SetTimestamp(num);
		}
		num *= _gameInstance.TimeDilationModifier;
		try
		{
			context.InitEntry(chain, orCreateInteractionEntry, _gameInstance);
			operation.Tick(_gameInstance, clickType, hasAnyButtonClick, flag, num, chain.Type, context);
		}
		finally
		{
			context.DeinitEntry(chain, orCreateInteractionEntry, _gameInstance);
		}
		InteractionSyncData val = null;
		InteractionSyncData state = orCreateInteractionEntry.State;
		if (flag || clientDataHashCode != orCreateInteractionEntry.GetClientDataHashCode())
		{
			val = state;
		}
		try
		{
			context.InitEntry(chain, orCreateInteractionEntry, _gameInstance);
			operation.Handle(_gameInstance, flag, num, chain.Type, context);
		}
		finally
		{
			context.DeinitEntry(chain, orCreateInteractionEntry, _gameInstance);
		}
		RemoveInteractionIfFinished(chain, orCreateInteractionEntry);
		return (val == null) ? ((InteractionSyncData)null) : new InteractionSyncData(val);
	}

	private void RemoveInteractionIfFinished(InteractionChain chain, InteractionEntry entry)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Invalid comparison between Unknown and I4
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Invalid comparison between Unknown and I4
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Invalid comparison between Unknown and I4
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Invalid comparison between Unknown and I4
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		if (chain.OperationIndex == entry.Index && (int)entry.State.State != 4)
		{
			chain.FinalState = entry.State.State;
		}
		if (entry.State != null && (int)entry.State.State != 4)
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.Trace($"Client finished interaction: {entry.Index}, {entry}");
			}
			if (entry.ServerState != null && (int)entry.ServerState.State != 4)
			{
				if (Logger.IsEnabled(LogLevel.Trace))
				{
					Logger.Trace($"Remove Interaction: {entry.Index}, {entry}");
				}
				chain.RemoveInteractionEntry(entry.Index);
			}
		}
		else if (entry.ServerState != null && (int)entry.ServerState.State != 4)
		{
			long elapsedMilliseconds = entry.WaitingForClientFinished.ElapsedMilliseconds;
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.Trace($"Server finished interaction but client hasn't! {entry.ServerState.State}, {entry.Index}, {entry}, {TimeHelper.FormatMillis(elapsedMilliseconds)}");
			}
			entry.WaitingForClientFinished.Start();
			if ((float)elapsedMilliseconds > _gameInstance.TimeModule.OperationTimeoutThreshold && Logger.IsEnabled(LogLevel.Warn))
			{
				Logger.Warn("Error: Server finished interaction earlier than client!");
			}
		}
	}

	public void RevertChain(InteractionChain chain, int index)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		chain.ClientState = (InteractionState)4;
		if (chain.OperationCounter >= chain.RootInteraction.Operations.Length)
		{
			chain.OperationIndex--;
		}
		while (chain.OperationIndex >= index)
		{
			if (!chain.GetInteraction(chain.OperationIndex, out var ret) && chain.PreviousInteractionEntry != null && chain.OperationIndex == chain.PreviousInteractionEntry.Index)
			{
				ret = chain.PreviousInteractionEntry;
			}
			if (ret == null)
			{
				if (chain.OperationIndex == 0)
				{
					Logger.Warn($"Tried to revert chain {chain.ChainId}-{chain.ForkedChainId} that never started?");
					return;
				}
				Logger.Error($"Tried to revert chain {chain.ChainId}-{chain.ForkedChainId} to {index} but couldn't due to a missing entry. Index={chain.OperationIndex}, Prev={chain.PreviousInteractionEntry?.Index}");
				chain.OperationIndex = index;
				return;
			}
			chain.OperationCounter = ret.State.OperationCounter;
			chain.RootInteraction = RootInteractions[ret.State.RootInteraction];
			ClientRootInteraction.Operation operation = chain.RootInteraction.Operations[chain.OperationCounter];
			try
			{
				chain.Context.InitEntry(chain, ret, _gameInstance);
				operation.Revert(_gameInstance, chain.Type, chain.Context);
			}
			finally
			{
				chain.Context.DeinitEntry(chain, ret, _gameInstance);
			}
			chain.RemoveForksForEntry(this, ret.Index);
			chain.OperationIndex--;
		}
		chain.OperationIndex++;
	}

	private void DoRollback(InteractionChain chain, int index, int? rewriteRoot = null)
	{
		if (chain.OperationIndex == index)
		{
			return;
		}
		chain.Desync = true;
		bool flag = true;
		if (index < 0)
		{
			index = 0;
			flag = false;
		}
		RevertChain(chain, index);
		if (rewriteRoot.HasValue)
		{
			int value = rewriteRoot.Value;
			if (chain.InitialRootInteraction.Index != value)
			{
				Logger.Warn($"Incorrect root, swapping: Client: {chain.InitialRootInteraction.Index}, Server: {value}");
				chain.RootInteraction = (chain.InitialRootInteraction = RootInteractions[value]);
				chain.ClearInteractions();
			}
		}
		if (flag)
		{
			chain.ShiftInteractionEntryOffset(1);
		}
		if (chain.GetInteractionSyncData(index) == null && chain.PreviousInteractionEntry != null)
		{
			InteractionEntry orCreateInteractionEntry = chain.GetOrCreateInteractionEntry(index);
			orCreateInteractionEntry.ServerState = chain.PreviousInteractionEntry.ServerState;
		}
		DoTickChain(chain, delegate(InteractionModule module, InteractionChain c, ClickType clickType, bool hasAnyButtonClick, int rootIndex)
		{
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
			ClientRootInteraction rootInteraction = c.RootInteraction;
			ClientRootInteraction.Operation operation = rootInteraction.Operations[c.OperationCounter];
			InteractionEntry orCreateInteractionEntry2 = c.GetOrCreateInteractionEntry(c.OperationIndex);
			InteractionSyncData val = c.RemoveInteractionSyncData(c.OperationIndex);
			if (val != null)
			{
				orCreateInteractionEntry2.ServerState = val;
			}
			if (orCreateInteractionEntry2.ServerState == null)
			{
				return (InteractionSyncData)null;
			}
			orCreateInteractionEntry2.Time.Start();
			orCreateInteractionEntry2.TimeOffset = orCreateInteractionEntry2.ServerState.Progress;
			try
			{
				c.Context.InitEntry(c, orCreateInteractionEntry2, module._gameInstance);
				operation.MatchServer(module._gameInstance, clickType, hasAnyButtonClick, c.Type, c.Context);
				operation.Handle(module._gameInstance, firstRun: true, orCreateInteractionEntry2.TimeOffset, c.Type, c.Context);
			}
			finally
			{
				c.Context.DeinitEntry(c, orCreateInteractionEntry2, module._gameInstance);
			}
			module.RemoveInteractionIfFinished(c, orCreateInteractionEntry2);
			return (orCreateInteractionEntry2.Index == rootIndex) ? null : orCreateInteractionEntry2.State;
		}, index, force: true);
	}

	private bool HasAnyButtonClick(InteractionType type, out ClickType clickType, InteractionContext context)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected I4, but got Unknown
		//IL_031b: Unknown result type (might be due to invalid IL or missing references)
		InputBindings inputBindings = _gameInstance.App.Settings.InputBindings;
		InputBinding binding;
		bool flag;
		switch ((int)type)
		{
		case 0:
			binding = inputBindings.PrimaryItemAction;
			flag = _gameInstance.Input.CanConsumeBinding(inputBindings.SecondaryItemAction) || _gameInstance.Input.CanConsumeBinding(inputBindings.BlockInteractAction) || _gameInstance.Input.CanConsumeBinding(inputBindings.PickBlock);
			break;
		case 1:
			binding = inputBindings.SecondaryItemAction;
			flag = _gameInstance.Input.CanConsumeBinding(inputBindings.PrimaryItemAction) || _gameInstance.Input.CanConsumeBinding(inputBindings.BlockInteractAction) || _gameInstance.Input.CanConsumeBinding(inputBindings.PickBlock);
			break;
		case 2:
			binding = inputBindings.TertiaryItemAction;
			flag = _gameInstance.Input.CanConsumeBinding(inputBindings.TertiaryItemAction);
			break;
		case 3:
			binding = inputBindings.Ability1ItemAction;
			flag = _gameInstance.Input.CanConsumeBinding(inputBindings.Ability1ItemAction);
			break;
		case 4:
			binding = inputBindings.Ability2ItemAction;
			flag = _gameInstance.Input.CanConsumeBinding(inputBindings.Ability2ItemAction);
			break;
		case 5:
			binding = inputBindings.Ability3ItemAction;
			flag = _gameInstance.Input.CanConsumeBinding(inputBindings.Ability3ItemAction);
			break;
		case 6:
		case 10:
		case 11:
		case 12:
		case 13:
		case 16:
		case 17:
		case 18:
		case 19:
		case 20:
		case 21:
		case 22:
		case 23:
		case 24:
		case 25:
			clickType = ClickType.None;
			return false;
		case 7:
			binding = inputBindings.BlockInteractAction;
			flag = _gameInstance.Input.CanConsumeBinding(inputBindings.PrimaryItemAction) || _gameInstance.Input.CanConsumeBinding(inputBindings.SecondaryItemAction) || _gameInstance.Input.CanConsumeBinding(inputBindings.PickBlock);
			break;
		case 8:
			binding = inputBindings.PickBlock;
			flag = _gameInstance.Input.CanConsumeBinding(inputBindings.PrimaryItemAction) || _gameInstance.Input.CanConsumeBinding(inputBindings.SecondaryItemAction) || _gameInstance.Input.CanConsumeBinding(inputBindings.BlockInteractAction);
			break;
		case 14:
		case 15:
			binding = inputBindings.GetHotbarSlot(context.MetaStore.TargetSlot.Value);
			flag = false;
			break;
		case 9:
			clickType = ClickType.None;
			return _gameInstance.Input.CanConsumeBinding(inputBindings.PrimaryItemAction) || _gameInstance.Input.CanConsumeBinding(inputBindings.SecondaryItemAction) || _gameInstance.Input.CanConsumeBinding(inputBindings.BlockInteractAction) || _gameInstance.Input.CanConsumeBinding(inputBindings.PickBlock);
		default:
			throw new ArgumentOutOfRangeException("type", type, null);
		}
		if (_gameInstance.Input.IsBindingHeld(binding))
		{
			bool flag2 = _gameInstance.Input.CanConsumeBinding(binding);
			clickType = ((!flag2) ? ClickType.Held : ClickType.Single);
			flag = flag || flag2;
		}
		else
		{
			clickType = ClickType.None;
		}
		return flag;
	}

	private void PutChain(int chainId, ForkedChainId forkedChainId, InteractionChain val)
	{
		InteractionChain value;
		if (forkedChainId == null)
		{
			Chains.Add(chainId, val);
		}
		else if (Chains.TryGetValue(chainId, out value))
		{
			while (forkedChainId.ForkedId != null)
			{
				if (!value.GetForkedChain(forkedChainId, out value))
				{
					Logger.Warn("Missing middle chain for fork");
					return;
				}
				forkedChainId = forkedChainId.ForkedId;
			}
			value.PutForkedChain(forkedChainId, val);
		}
		else
		{
			Logger.Warn("Missing primary chain for fork");
		}
	}

	private void CancelChains(int chainId, ForkedChainId forkedChainId)
	{
		InteractionChain value2;
		if (forkedChainId == null)
		{
			if (Chains.TryGetValue(chainId, out var value))
			{
				CancelChains(value);
			}
		}
		else if (Chains.TryGetValue(chainId, out value2))
		{
			while (forkedChainId.ForkedId != null)
			{
				if (!value2.GetForkedChain(forkedChainId, out value2))
				{
					Logger.Warn("Missing middle chain for fork");
					return;
				}
				forkedChainId = forkedChainId.ForkedId;
			}
			if (value2.GetForkedChain(forkedChainId, out var ret))
			{
				CancelChains(ret);
			}
		}
		else
		{
			Logger.Warn("Missing primary chain for fork");
		}
	}

	private void CancelChains(InteractionChain chain)
	{
		CancelChain(chain.ChainId, chain.ForkedChainId);
		foreach (InteractionChain value in chain.ForkedChains.Values)
		{
			CancelChains(value);
		}
	}

	private void CancelChain(int chainId, ForkedChainId forkedChainId)
	{
		InteractionChain value2;
		if (forkedChainId == null)
		{
			if (Chains.TryGetValue(chainId, out var value))
			{
				value.ServerCancelled = true;
			}
		}
		else if (Chains.TryGetValue(chainId, out value2))
		{
			while (forkedChainId.ForkedId != null)
			{
				if (!value2.GetForkedChain(forkedChainId, out value2))
				{
					Logger.Warn("Missing middle chain for fork");
					return;
				}
				forkedChainId = forkedChainId.ForkedId;
			}
			if (value2.GetForkedChain(forkedChainId, out var ret))
			{
				ret.ServerCancelled = true;
			}
		}
		else
		{
			Logger.Warn("Missing primary chain for fork");
		}
	}

	private void InterruptChains(int chainId, ForkedChainId forkedChainId)
	{
		InteractionChain value2;
		if (forkedChainId == null)
		{
			if (Chains.TryGetValue(chainId, out var value))
			{
				InterruptChains(value);
			}
		}
		else if (Chains.TryGetValue(chainId, out value2))
		{
			while (forkedChainId.ForkedId != null)
			{
				if (!value2.GetForkedChain(forkedChainId, out value2))
				{
					Logger.Warn("Missing middle chain for fork");
					return;
				}
				forkedChainId = forkedChainId.ForkedId;
			}
			if (value2.GetForkedChain(forkedChainId, out var ret))
			{
				InterruptChains(ret);
			}
		}
		else
		{
			Logger.Warn("Missing primary chain for fork");
		}
	}

	private void InterruptChains(InteractionChain chain)
	{
		InterruptChain(chain.ChainId, chain.ForkedChainId);
		foreach (InteractionChain value in chain.ForkedChains.Values)
		{
			InterruptChains(value);
		}
	}

	private void InterruptChain(int chainId, ForkedChainId forkedChainId)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Invalid comparison between Unknown and I4
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Invalid comparison between Unknown and I4
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Invalid comparison between Unknown and I4
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Invalid comparison between Unknown and I4
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		if (forkedChainId == null)
		{
			if (Chains.TryGetValue(chainId, out var value))
			{
				if ((int)value.ClientState == 4)
				{
					value.ClientState = (InteractionState)1;
				}
				if (value.GetInteraction(value.OperationIndex, out var ret) && (int)ret.State.State == 4)
				{
					ret.State.State = (InteractionState)1;
				}
			}
		}
		else
		{
			if (!Chains.TryGetValue(chainId, out var value2))
			{
				return;
			}
			while (forkedChainId.ForkedId != null)
			{
				if (!value2.GetForkedChain(forkedChainId, out value2))
				{
					Logger.Warn("Interrupt: Missing middle chain for fork");
					return;
				}
				forkedChainId = forkedChainId.ForkedId;
			}
			if (value2.GetForkedChain(forkedChainId, out var ret2))
			{
				if ((int)ret2.ClientState == 4)
				{
					ret2.ClientState = (InteractionState)1;
				}
				if (ret2.GetInteraction(ret2.OperationIndex, out var ret3) && (int)ret3.State.State == 4)
				{
					ret3.State.State = (InteractionState)1;
				}
			}
		}
	}

	public void Handle(SyncInteractionChain packet)
	{
		if (Logger.IsEnabled(LogLevel.Trace))
		{
			Logger.Trace($"Receive Sync Packet: {packet}");
		}
		bool flag = packet.OverrideRootInteraction != int.MinValue && packet.ForkedId != null;
		InteractionChain value = null;
		if (Chains.TryGetValue(packet.ChainId, out value))
		{
			for (ForkedChainId forkedId = packet.ForkedId; forkedId != null; forkedId = forkedId.ForkedId)
			{
				InteractionChain interactionChain = value;
				if (!value.GetForkedChain(forkedId, out value))
				{
					if (flag)
					{
						value = null;
						break;
					}
					InteractionChain.TempChain tempForkedChain = interactionChain.GetTempForkedChain(forkedId);
					for (forkedId = forkedId.ForkedId; forkedId != null; forkedId = forkedId.ForkedId)
					{
						tempForkedChain = tempForkedChain.GetTempForkedChain(forkedId);
					}
					Sync(tempForkedChain, packet);
					return;
				}
			}
		}
		if (value == null && packet.ForkedId != null && !flag)
		{
			Logger.Info("Ignoring incorrect fork. Assuming it was cancelled.");
		}
		else if (value == null)
		{
			SyncStart(packet);
		}
		else
		{
			Sync(value, packet);
		}
	}

	public void Handle(CancelInteractionChain packet)
	{
		if (Logger.IsEnabled(LogLevel.Trace))
		{
			Logger.Trace($"Receive Cancel Packet: {packet}");
		}
		CancelChain(packet.ChainId, packet.ForkedId);
	}

	private void SyncStart(SyncInteractionChain packet)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		int chainId = packet.ChainId;
		InteractionType interactionType_ = packet.InteractionType_;
		if (!packet.Initial)
		{
			Logger.Warn($"Got SyncStart for {chainId} but packet wasn't the first.");
			return;
		}
		if (packet.ForkedId == null)
		{
			if (chainId >= 0)
			{
				if (Logger.IsEnabled(LogLevel.Trace))
				{
					Logger.Trace($"Invalid server chainId! Got {chainId} but server id's should be < 0");
				}
				return;
			}
			if (chainId >= _lastServerChainId)
			{
				if (Logger.IsEnabled(LogLevel.Trace))
				{
					Logger.Trace($"Invalid server chainId! The last serverChainId was {_lastServerChainId} but just got {chainId}");
				}
				return;
			}
			_lastServerChainId = chainId;
		}
		InventoryModule inventoryModule = _gameInstance.InventoryModule;
		InteractionType type = interactionType_;
		if (packet.ForkedId != null && Chains.TryGetValue(packet.ChainId, out var value))
		{
			type = value.Type;
		}
		InteractionContext interactionContext = InteractionContext.ForInteraction(_gameInstance, inventoryModule, type);
		int id = packet.OverrideRootInteraction;
		if (packet.OverrideRootInteraction == int.MinValue && !interactionContext.GetRootInteractionId(_gameInstance, interactionType_, out id))
		{
			throw new Exception("Missing root interaction");
		}
		ClientRootInteraction rootInteraction = RootInteractions[id];
		BlockPosition targetBlock = null;
		if (packet.Data.BlockPosition_ != null)
		{
			targetBlock = _gameInstance.MapModule.GetBaseBlock(packet.Data.BlockPosition_);
		}
		if (packet.Data.BlockPosition_ != null)
		{
			interactionContext.MetaStore.TargetBlockRaw = packet.Data.BlockPosition_;
			interactionContext.MetaStore.TargetBlock = targetBlock;
		}
		if (packet.Data.EntityId != -1)
		{
			interactionContext.MetaStore.TargetEntity = _gameInstance.EntityStoreModule.GetEntity(packet.Data.EntityId);
		}
		if (packet.Data.HitLocation != null)
		{
			interactionContext.MetaStore.HitLocation = new Vector4(packet.Data.HitLocation.X, packet.Data.HitLocation.Y, packet.Data.HitLocation.Z, 0f);
		}
		if (packet.Data.TargetSlot != int.MinValue)
		{
			if (packet.Data.TargetSlot < 0)
			{
				interactionContext.MetaStore.TargetSlot = -(packet.Data.TargetSlot + 1);
				interactionContext.MetaStore.DisableSlotFork = true;
			}
			else
			{
				interactionContext.MetaStore.TargetSlot = packet.Data.TargetSlot;
			}
		}
		ForkedChainId forkedId = packet.ForkedId;
		while (forkedId?.ForkedId != null)
		{
			forkedId = forkedId.ForkedId;
		}
		InteractionChain interactionChain = new InteractionChain(packet.ForkedId, forkedId, interactionType_, interactionContext, packet.Data, rootInteraction, 0, null, null);
		interactionChain.ChainId = chainId;
		interactionChain.Time.Start();
		Sync(interactionChain, packet);
		if (!TickChain(interactionChain))
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.Trace($"Add Chain: {chainId}, {interactionChain}");
			}
			PutChain(chainId, packet.ForkedId, interactionChain);
		}
	}

	public void Sync(ChainSyncStorage chain, SyncInteractionChain packet)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Invalid comparison between Unknown and I4
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Invalid comparison between Unknown and I4
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Invalid comparison between Unknown and I4
		//IL_0377: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Invalid comparison between Unknown and I4
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Invalid comparison between Unknown and I4
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Invalid comparison between Unknown and I4
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Invalid comparison between Unknown and I4
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Invalid comparison between Unknown and I4
		if ((int)chain.ServerState != 4 && (int)packet.State == 4)
		{
			throw new Exception($"Tried to change serverState on chain that is already Finished/Failed to NotFinished! {chain}, {packet}");
		}
		if (packet.NewForks != null)
		{
			SyncInteractionChain[] newForks = packet.NewForks;
			foreach (SyncInteractionChain packet2 in newForks)
			{
				chain.SyncFork(_gameInstance, packet2);
			}
		}
		chain.ServerState = packet.State;
		if (packet.InteractionData == null)
		{
			return;
		}
		if (chain is InteractionChain interactionChain)
		{
			interactionChain.ServerAck = true;
		}
		for (int j = 0; j < packet.InteractionData.Length; j++)
		{
			InteractionSyncData val = packet.InteractionData[j];
			if (val == null)
			{
				continue;
			}
			int num = packet.OperationBaseIndex + j;
			if ((int)val.State != 4 && chain is InteractionChain interactionChain2 && num > interactionChain2.ServerCompleteIndex)
			{
				interactionChain2.ServerCompleteIndex = num;
			}
			if (chain is InteractionChain interactionChain3)
			{
				if (!interactionChain3.GetInteraction(num, out var ret))
				{
					if ((int)interactionChain3.ClientState != 4)
					{
						Logger.Warn($"Client finished while the server continued. Rolling back {interactionChain3.ChainId} - {interactionChain3.ForkedChainId}");
						OnSyncFailed(interactionChain3, packet, j, num);
						return;
					}
					chain.PutInteractionSyncData(num, val);
					continue;
				}
				if (ret.ServerState != null && (int)ret.ServerState.State != 4 && (int)val.State == 4)
				{
					throw new Exception($"Tried to change syncData on interaction that is already Finished/Failed to NotFinished! {ret}, {val}");
				}
				if (ret.State.OperationCounter != val.OperationCounter || ret.State.RootInteraction != val.RootInteraction)
				{
					OnSyncFailed(interactionChain3, packet, j, num, val.RootInteraction);
					return;
				}
				interactionChain3.UpdateSyncPosition(num);
				ret.ServerState = val;
				if (Logger.IsEnabled(LogLevel.Trace))
				{
					Logger.Trace($"{num}: Time (Sync) - Client: {ret.Time.Elapsed.TotalSeconds} vs Server: {ret.ServerState?.Progress.ToString(CultureInfo.InvariantCulture)}");
				}
				RemoveInteractionIfFinished(interactionChain3, ret);
			}
			else
			{
				chain.PutInteractionSyncData(num, val);
			}
		}
		if (chain is InteractionChain { ServerCompleteIndex: var serverCompleteIndex } interactionChain4)
		{
			if ((int)interactionChain4.ClientState != 4 && (int)interactionChain4.ServerState != 4 && interactionChain4.OperationIndex != serverCompleteIndex + 1)
			{
				Logger.Warn($"Client finished incorrectly. Rolling back to: {serverCompleteIndex} for {interactionChain4.ChainId} - {interactionChain4.ForkedChainId}");
				DoRollback(interactionChain4, serverCompleteIndex);
			}
			if ((int)interactionChain4.ClientState == 4 && (int)interactionChain4.ServerState != 4 && interactionChain4.OperationIndex >= serverCompleteIndex + 1)
			{
				Logger.Warn($"Client went down a different path. Rolling back to: {serverCompleteIndex} for {interactionChain4.ChainId} - {interactionChain4.ForkedChainId}");
				DoRollback(interactionChain4, serverCompleteIndex);
			}
		}
	}

	private void OnSyncFailed(InteractionChain interactionChain, SyncInteractionChain packet, int offset, int index, int? realRoot = null)
	{
		for (int i = offset; i < packet.InteractionData.Length; i++)
		{
			interactionChain.PutInteractionSyncData(packet.OperationBaseIndex + i, packet.InteractionData[i]);
		}
		int? rewriteRoot = null;
		if (index == 0)
		{
			if (!realRoot.HasValue)
			{
				throw new Exception("Failed to start chain correctly");
			}
			rewriteRoot = realRoot.Value;
		}
		DoRollback(interactionChain, index - 1, rewriteRoot);
	}

	public bool CanRun(InteractionType type, int equipSlot, ClientRootInteraction rootInteraction)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		List<InteractionChain> chainsToCancel = null;
		return ApplyRules(null, type, equipSlot, rootInteraction, Chains, in chainsToCancel);
	}

	public bool ApplyRules(InteractionContext context, InteractionChainData data, InteractionType type, ClientRootInteraction rootInteraction)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		List<InteractionChain> chainsToCancel = new List<InteractionChain>();
		if (!ApplyRules(data, type, context?.HeldItemSlot ?? 0, rootInteraction, Chains, in chainsToCancel))
		{
			return false;
		}
		chainsToCancel.ForEach(delegate(InteractionChain chain)
		{
			InterruptChains(chain.ChainId, chain.ForkedChainId);
		});
		return true;
	}

	private bool ApplyRules<T>(InteractionChainData data, InteractionType type, int heldItemSlot, ClientRootInteraction rootInteraction, Dictionary<T, InteractionChain> chains, in List<InteractionChain> chainsToCancel)
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Invalid comparison between Unknown and I4
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Invalid comparison between Unknown and I4
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Invalid comparison between Unknown and I4
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		if (chains.Count == 0 || rootInteraction == null)
		{
			return true;
		}
		foreach (InteractionChain value in chains.Values)
		{
			if ((value.ForkedChainId != null && !value.Predicted) || (data != null && value.ChainData.ProxyId != data.ProxyId) || ((int)type == 24 && (int)value.Type == 24 && value.Context.HeldItemSlot != heldItemSlot))
			{
				continue;
			}
			if ((int)value.ClientState == 4)
			{
				ClientRootInteraction.Operation operation = value.RootInteraction.Operations[value.OperationCounter];
				ClientInteraction.ClientInteractionRules rules;
				HashSet<int> tags;
				bool flag = operation.TryGetRules(_gameInstance, out rules, out tags);
				if (rootInteraction.Rules.ValidateInterrupts(type, rootInteraction.Tags, value.Type, value.RootInteraction.Tags, value.RootInteraction.Rules))
				{
					chainsToCancel?.Add(value);
				}
				else if (flag && rootInteraction.Rules.ValidateInterrupts(type, rootInteraction.Tags, value.Type, tags, rules))
				{
					chainsToCancel?.Add(value);
				}
				else
				{
					if (rootInteraction.Rules.ValidateBlocked(type, rootInteraction.Tags, value.Type, value.RootInteraction.Tags, value.RootInteraction.Rules))
					{
						return false;
					}
					if (flag && rootInteraction.Rules.ValidateBlocked(type, rootInteraction.Tags, value.Type, tags, rules))
					{
						return false;
					}
				}
			}
			if ((chainsToCancel == null || chainsToCancel.Count == 0) && !ApplyRules(data, type, heldItemSlot, rootInteraction, value.ForkedChains, in chainsToCancel))
			{
				return false;
			}
		}
		return true;
	}

	public bool StartChain(InteractionType type, ClickType clickType, Action onCompletion)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		InventoryModule inventoryModule = _gameInstance.InventoryModule;
		InteractionContext context = InteractionContext.ForInteraction(_gameInstance, inventoryModule, type);
		return StartChain(context, type, clickType, onCompletion);
	}

	public bool StartChain(InteractionContext context, InteractionType type, ClickType clickType, Action onCompletion, int? targetEntityId = null, Vector4? hitPosition = null, string hitDetail = null)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Expected O, but got Unknown
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Expected O, but got Unknown
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e4: Unknown result type (might be due to invalid IL or missing references)
		if (!context.GetRootInteractionId(_gameInstance, type, out var id))
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.Trace($"No interactions defined for {type} for item {context.HeldItem?.Id}");
			}
			return false;
		}
		ClientRootInteraction clientRootInteraction = RootInteractions[id];
		BlockPosition val = ((InteractionTarget != InteractionTargetType.Block) ? ((BlockPosition)null) : new BlockPosition((int)System.Math.Floor(TargetBlockHit.BlockPosition.X), (int)System.Math.Floor(TargetBlockHit.BlockPosition.Y), (int)System.Math.Floor(TargetBlockHit.BlockPosition.Z)));
		BlockPosition targetBlock = null;
		if (val != null)
		{
			targetBlock = _gameInstance.MapModule.GetBaseBlock(val);
		}
		int num = targetEntityId ?? ((InteractionTarget == InteractionTargetType.Entity) ? TargetEntityHit.NetworkId : (-1));
		context.MetaStore.TargetBlock = targetBlock;
		context.MetaStore.TargetBlockRaw = val;
		context.MetaStore.TargetEntity = _gameInstance.EntityStoreModule.GetEntity(num);
		context.MetaStore.HitLocation = hitPosition;
		context.MetaStore.HitDetail = hitDetail;
		Guid guid = ((context.Entity == _gameInstance.LocalPlayer) ? GuidSerializer.GetDefault() : context.Entity.PredictionId.Value);
		Vector3f val2 = null;
		if (hitPosition.HasValue)
		{
			Vector4 value = hitPosition.Value;
			val2 = new Vector3f(value.X, value.Y, value.Z);
		}
		InteractionChainData val3 = new InteractionChainData(num, guid, val2, hitDetail, val, context.MetaStore.TargetSlot.GetValueOrDefault(int.MinValue));
		List<InteractionChain> chainsToCancel = new List<InteractionChain>();
		if (!ApplyRules(val3, type, context.HeldItemSlot, clientRootInteraction, Chains, in chainsToCancel))
		{
			if (clientRootInteraction.RootInteraction.ClickQueuingTimeout > 0f && clickType == ClickType.Single)
			{
				QueueClick(context, type, clientRootInteraction.RootInteraction.ClickQueuingTimeout);
			}
			return false;
		}
		UpdateCooldown(clientRootInteraction, clickType == ClickType.Single, out var isOnCooldown);
		if (isOnCooldown)
		{
			if (clickType == ClickType.Single)
			{
				_gameInstance.App.Interface.InGameView.AbilitiesHudComponent.CooldownError(clientRootInteraction);
				if (clientRootInteraction.RootInteraction.ClickQueuingTimeout > 0f)
				{
					QueueClick(context, type, clientRootInteraction.RootInteraction.ClickQueuingTimeout);
				}
			}
			return false;
		}
		int hotbarActiveSlot = _gameInstance.InventoryModule.HotbarActiveSlot;
		ClientItemStack hotbarItem = _gameInstance.InventoryModule.GetHotbarItem(_gameInstance.InventoryModule.HotbarActiveSlot);
		int num2 = ++_lastClientChainId;
		if (num2 < 0)
		{
			num2 = (_lastClientChainId = 1);
		}
		InteractionChain interactionChain = new InteractionChain(type, context, val3, clientRootInteraction, hotbarActiveSlot, hotbarItem, onCompletion);
		interactionChain.Predicted = true;
		interactionChain.Time.Start();
		interactionChain.ChainId = num2;
		if (clientRootInteraction.RootInteraction.RequireNewClick)
		{
			RequireNewClick(type);
		}
		bool flag = TickChain(interactionChain);
		chainsToCancel.ForEach(delegate(InteractionChain c)
		{
			InterruptChains(c.ChainId, c.ForkedChainId);
		});
		if (flag)
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.Trace($"Finished Chain: {num2}, {interactionChain}, {interactionChain.ClientState}");
			}
		}
		else
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.Trace($"Add Chain: {num2}, {interactionChain}");
			}
			Chains.Add(num2, interactionChain);
		}
		_gameInstance.App.Interface.InGameView.AbilitiesHudComponent.OnStartChain(clientRootInteraction.Id);
		return true;
	}

	public T ForEachInteraction<T>(Func<InteractionChain, ClientInteraction, T, T> consumer, T val)
	{
		return ForEachInteraction(Chains, consumer, val);
	}

	private T ForEachInteraction<T, TK>(Dictionary<TK, InteractionChain> chains, Func<InteractionChain, ClientInteraction, T, T> consumer, T val)
	{
		foreach (InteractionChain value in chains.Values)
		{
			if (value.OperationCounter < value.RootInteraction.Operations.Length)
			{
				ClientRootInteraction.Operation operation = value.RootInteraction.Operations[value.OperationCounter];
				if (operation is ClientRootInteraction.InteractionWrapper interactionWrapper)
				{
					val = consumer(value, interactionWrapper.GetInteraction(this), val);
				}
				val = ForEachInteraction(value.ForkedChains, consumer, val);
			}
		}
		return val;
	}

	private void SendSyncPacket(InteractionChain chain, int operationBaseIndex, List<InteractionSyncData> interactionData, bool force = false)
	{
		if (force || !chain.SentInitialState || (interactionData != null && !interactionData.All((InteractionSyncData v) => v == null)) || chain.NewForks.Count != 0)
		{
			SyncInteractionChain packet = MakeSyncPacket(chain, operationBaseIndex, interactionData);
			_gameInstance.Connection.SendPacketImmediate((ProtoPacket)(object)packet);
		}
	}

	private SyncInteractionChain MakeSyncPacket(InteractionChain chain, int operationBaseIndex, List<InteractionSyncData> interactionData)
	{
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Expected O, but got Unknown
		SyncInteractionChain[] array = null;
		if (chain.NewForks.Count > 0)
		{
			array = (SyncInteractionChain[])(object)new SyncInteractionChain[chain.NewForks.Count];
			for (int i = 0; i < chain.NewForks.Count; i++)
			{
				InteractionChain chain2 = chain.NewForks[i];
				array[i] = MakeSyncPacket(chain2, operationBaseIndex, null);
			}
			chain.NewForks.Clear();
		}
		SyncInteractionChain result = new SyncInteractionChain(chain.InitialSlot, _gameInstance.InventoryModule.UtilityActiveSlot, _gameInstance.InventoryModule.ToolsActiveSlot, _gameInstance.InventoryModule.ConsumableActiveSlot, chain.SentInitialState ? null : chain.InitialItem?.ToItemPacket(includeMetadata: true), chain.SentInitialState ? null : _gameInstance.InventoryModule.GetUtilityItem(_gameInstance.InventoryModule.UtilityActiveSlot)?.ToItemPacket(includeMetadata: true), chain.SentInitialState ? null : _gameInstance.InventoryModule.GetToolsItem(_gameInstance.InventoryModule.ToolsActiveSlot)?.ToItemPacket(includeMetadata: true), chain.SentInitialState ? null : _gameInstance.InventoryModule.GetConsumableItem(_gameInstance.InventoryModule.ConsumableActiveSlot)?.ToItemPacket(includeMetadata: true), !chain.SentInitialState, chain.ConsumeDesync(), chain.Type, chain.Context.HeldItemSlot, chain.ChainId, chain.ForkedChainId, chain.ChainData, chain.ClientState, array, operationBaseIndex, interactionData?.ToArray());
		chain.SentInitialState = true;
		return result;
	}

	public void SetGlobalTimeShift(InteractionType type, float shift)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (shift < 0f)
		{
			throw new ArgumentException();
		}
		_globalTimeShift[type] = shift;
	}

	public float GetGlobalTimeShift(InteractionType type)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return _globalTimeShift[type];
	}

	public void DrawDebugSelector(GraphicsDevice graphics, GLFunctions gl, ref Vector3 cameraPosition, ref Matrix viewProjectionMatrix)
	{
		foreach (DebugSelectorMesh selectorDebugMesh in SelectorDebugMeshes)
		{
			Matrix matrix = selectorDebugMesh.Matrix * Matrix.CreateTranslation(-cameraPosition) * viewProjectionMatrix;
			graphics.GPUProgramStore.BasicProgram.MVPMatrix.SetValue(ref matrix);
			graphics.GPUProgramStore.BasicProgram.Opacity.SetValue(0.8f * (selectorDebugMesh.Time / selectorDebugMesh.InitialTime));
			graphics.GPUProgramStore.BasicProgram.Color.SetValue(selectorDebugMesh.DebugColor);
			gl.BindVertexArray(selectorDebugMesh.Mesh.VertexArray);
			gl.DrawElements(GL.TRIANGLES, selectorDebugMesh.Mesh.Count, GL.UNSIGNED_SHORT, IntPtr.Zero);
			gl.PolygonMode(GL.FRONT_AND_BACK, GL.LINE);
			graphics.GPUProgramStore.BasicProgram.Opacity.SetValue(1f);
			graphics.GPUProgramStore.BasicProgram.Color.SetValue(graphics.BlackColor);
			gl.DrawElements(GL.TRIANGLES, selectorDebugMesh.Mesh.Count, GL.UNSIGNED_SHORT, IntPtr.Zero);
			gl.PolygonMode(GL.FRONT_AND_BACK, GL.FILL);
			selectorDebugMesh.Time -= 1f / 60f;
		}
		SelectorDebugMeshes.RemoveAll(delegate(DebugSelectorMesh s)
		{
			if (s.Time <= 0f)
			{
				s.Mesh.Dispose();
				return true;
			}
			return false;
		});
	}
}
