using System;
using Hypixel.ProtoPlus;
using HytaleClient.Data.Map;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.Interaction;

internal class BlockPlacementPreview
{
	public enum DisplayMode
	{
		None,
		All,
		Multipart
	}

	private readonly GameInstance _gameInstance;

	private static readonly Vector3 _baseOffset = new Vector3(0.5f, 0f, 0.5f);

	private readonly Entity _entity;

	private Vector3 _offset;

	private IntVector3 _previousBlockPosition = IntVector3.Zero;

	private int _currentEntityEffect = -1;

	private bool _hasSupport = true;

	public int BlockId;

	public IntVector3 BlockPosition = IntVector3.Zero;

	public bool HasSupport => _hasSupport;

	public bool HasValidPosition { get; private set; }

	public bool UseDithering => _entity.UseDithering;

	public DisplayMode _displayMode
	{
		get
		{
			if (_gameInstance.InteractionModule.CurrentRotationMode != InteractionModule.RotationMode.None || (_gameInstance.Input.IsAltHeld() && !_gameInstance.InteractionModule.FluidityActive))
			{
				return DisplayMode.All;
			}
			return _gameInstance.App.Settings.PlacementPreviewMode;
		}
	}

	public bool IsEnabled => _displayMode != DisplayMode.None;

	public bool IsVisible
	{
		get
		{
			return _entity?.IsVisible ?? false;
		}
		set
		{
			if (_entity != null)
			{
				_entity.IsVisible = value;
			}
		}
	}

	public BlockPlacementPreview(GameInstance gameInstance)
	{
		_gameInstance = gameInstance;
		_gameInstance.EntityStoreModule.Spawn(-1, out _entity);
		_entity.SetIsTangible(isTangible: false);
		_entity.UseDithering = true;
	}

	public void CheckSupportValidation()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		_gameInstance.Connection.SendPacket((ProtoPacket)new SupportValidationCheck(new BlockPosition(BlockPosition.X, BlockPosition.Y, BlockPosition.Z), BlockId));
	}

	public void HandleSupportValidationResponse(SupportValidationResponse response)
	{
		IntVector3 intVector = new IntVector3(response.BlockPosition_.X, response.BlockPosition_.Y, response.BlockPosition_.Z);
		if (BlockId == response.BlockId && BlockPosition == intVector)
		{
			_hasSupport = response.Valid;
			UpdateEffect();
		}
	}

	public void UpdateEffect()
	{
		int value = -1;
		if (!_gameInstance.InteractionModule.HeldBlockCanBePlaced)
		{
			_gameInstance.EntityStoreModule.EntityEffectIndicesByIds.TryGetValue("PrototypeBlockPlaceFail", out value);
			HasValidPosition = false;
		}
		else if (_gameInstance.InteractionModule.CurrentRotationMode != InteractionModule.RotationMode.None)
		{
			_gameInstance.EntityStoreModule.EntityEffectIndicesByIds.TryGetValue("PrototypeBlockPlaceRotated", out value);
			HasValidPosition = true;
		}
		else
		{
			_gameInstance.EntityStoreModule.EntityEffectIndicesByIds.TryGetValue("PrototypeBlockPlaceSuccess", out value);
			HasValidPosition = true;
		}
		if (value != _currentEntityEffect || !_entity.HasEffect(value))
		{
			_entity.ClearEffects();
			if (value != -1)
			{
				_entity.AddEffect(value);
			}
			_currentEntityEffect = value;
		}
	}

	public void EnableDithering(bool enable)
	{
		_entity.UseDithering = enable;
	}

	public void UpdatePreview(int blockId, int worldX, int worldY, int worldZ)
	{
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Invalid comparison between Unknown and I4
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Invalid comparison between Unknown and I4
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Invalid comparison between Unknown and I4
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Invalid comparison between Unknown and I4
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Invalid comparison between Unknown and I4
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Invalid comparison between Unknown and I4
		bool flag = false;
		if (_gameInstance.App.Settings.BlockPlacementSupportValidation)
		{
			if (_previousBlockPosition != BlockPosition || blockId != BlockId)
			{
				flag = true;
				BlockId = blockId;
				CheckSupportValidation();
			}
		}
		else
		{
			_hasSupport = true;
		}
		if (!IsEnabled || blockId == -1)
		{
			IsVisible = false;
			_currentEntityEffect = -1;
			return;
		}
		ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[blockId];
		BlockHitbox blockHitbox = _gameInstance.ServerSettings.BlockHitboxes[clientBlockType.HitboxType];
		BlockPosition = new IntVector3(worldX, worldY, worldZ);
		if (_displayMode == DisplayMode.Multipart && !blockHitbox.IsOversized())
		{
			IsVisible = false;
			_currentEntityEffect = -1;
			return;
		}
		if (blockId != BlockId || flag)
		{
			BlockId = blockId;
			int num = (((int)clientBlockType.RotationYaw == 1) ? 90 : (((int)clientBlockType.RotationYaw == 2) ? 180 : (((int)clientBlockType.RotationYaw == 3) ? 270 : 0)));
			int num2 = (((int)clientBlockType.RotationPitch == 1) ? 90 : (((int)clientBlockType.RotationPitch == 2) ? 180 : (((int)clientBlockType.RotationPitch == 3) ? 270 : 0)));
			int num3 = 0;
			_offset = _baseOffset;
			switch (num2)
			{
			case 90:
				switch (num)
				{
				case 0:
					_offset.Z -= 0.5f;
					break;
				case 90:
					_offset.X -= 0.5f;
					break;
				case 180:
					_offset.Z += 0.5f;
					break;
				case 270:
					_offset.X += 0.5f;
					break;
				}
				_offset.Y += 0.5f;
				num += 90;
				num3 = -90;
				break;
			case 180:
				_offset += Vector3.Up;
				break;
			default:
				num = (num + 180) % 360;
				break;
			}
			Console.WriteLine($"X: {num3}  Y: {num}  Z: {num2}");
			_entity.SetBlock(clientBlockType.Id);
			_entity.LookOrientation = new Vector3(MathHelper.ToRadians(num3), MathHelper.ToRadians(num), MathHelper.ToRadians(num2));
		}
		_entity.SetPosition(new Vector3((float)worldX + _offset.X, (float)worldY + _offset.Y, (float)worldZ + _offset.Z));
		_entity.PositionProgress = 1f;
		if (_previousBlockPosition != BlockPosition || flag)
		{
			UpdateEffect();
		}
		_previousBlockPosition = BlockPosition;
		IsVisible = true;
	}

	public void RegisterSoundObjectReference()
	{
		_gameInstance.AudioModule.TryRegisterSoundObject(Vector3.Zero, Vector3.Zero, ref _entity.SoundObjectReference);
	}
}
