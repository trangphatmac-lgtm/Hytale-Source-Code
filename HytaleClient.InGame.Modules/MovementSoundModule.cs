using System;
using HytaleClient.Data.Entities;
using HytaleClient.Data.Map;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using HytaleClient.Protocol;
using Wwise;

namespace HytaleClient.InGame.Modules;

internal class MovementSoundModule : Module
{
	private int _previousBlockId;

	private bool _wasFalling = false;

	private bool _wasFlying = false;

	private bool _wasJumping = false;

	private bool _wasOnGround = true;

	private int _moveInPlaybackId = -1;

	private int _moveOutPlaybackId = -1;

	private int _walkPlaybackId = -1;

	private int _nextFootstepIntervalIndex;

	private const string GroundLandWwiseId = "SFX_PL_FS_LAND";

	public MovementSoundModule(GameInstance gameInstance)
		: base(gameInstance)
	{
	}

	public void Update(float deltaTime)
	{
		Vector3 position = _gameInstance.LocalPlayer.Position;
		int num = _gameInstance.MapModule.GetBlock(position, 1);
		ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[num];
		if (clientBlockType.FluidBlockId != 0)
		{
			num = clientBlockType.FluidBlockId;
			clientBlockType = _gameInstance.MapModule.ClientBlockTypes[num];
		}
		if (num != _previousBlockId)
		{
			_gameInstance.AudioModule.TryPlayLocalBlockSoundEvent(clientBlockType.BlockSoundSetIndex, (BlockSoundEvent)1, ref _moveInPlaybackId);
			ClientBlockType clientBlockType2 = _gameInstance.MapModule.ClientBlockTypes[_previousBlockId];
			_gameInstance.AudioModule.TryPlayLocalBlockSoundEvent(clientBlockType2.BlockSoundSetIndex, (BlockSoundEvent)2, ref _moveOutPlaybackId);
			_previousBlockId = num;
		}
		ClientMovementStates relativeMovementStates = _gameInstance.LocalPlayer.GetRelativeMovementStates();
		EntityAnimation currentMovementAnimation = _gameInstance.LocalPlayer.CurrentMovementAnimation;
		if (currentMovementAnimation != null)
		{
			bool flag = !_wasFalling && relativeMovementStates.IsFalling && !relativeMovementStates.IsInFluid;
			bool flag2 = !_wasFlying && relativeMovementStates.IsFlying;
			bool flag3 = _wasFalling && !relativeMovementStates.IsFalling;
			bool flag4 = !_wasJumping && !relativeMovementStates.IsOnGround && relativeMovementStates.IsJumping;
			bool flag5 = relativeMovementStates.IsInFluid || relativeMovementStates.IsSwimming;
			bool flag6 = !_wasOnGround && relativeMovementStates.IsOnGround;
			bool flag7 = _wasFalling && flag5;
			bool flag8 = currentMovementAnimation.FootstepIntervals.Length != 0 && relativeMovementStates.IsOnGround && !flag5 && !relativeMovementStates.IsIdle;
			if (flag2 || flag7 || flag3)
			{
				ClearPlayback();
			}
			else if (flag6)
			{
				ClearPlayback();
				if (!flag5 && _gameInstance.Engine.Audio.ResourceManager.WwiseEventIds.TryGetValue("SFX_PL_FS_LAND", out var value))
				{
					PlayLocalSoundEventWithBlockType(position, value);
				}
			}
			else if (flag || flag4 || relativeMovementStates.IsSwimJumping || (relativeMovementStates.IsSwimming && !relativeMovementStates.IsOnGround))
			{
				PlayLocalSoundEvent(currentMovementAnimation.SoundEventIndex);
			}
			else if (flag8)
			{
				TickFootsteps(position);
			}
		}
		else
		{
			ClearPlayback();
		}
		_wasFalling = relativeMovementStates.IsFalling;
		_wasFlying = relativeMovementStates.IsFlying;
		_wasJumping = relativeMovementStates.IsJumping;
		_wasOnGround = relativeMovementStates.IsOnGround;
	}

	private void ClearPlayback()
	{
		if (_walkPlaybackId != -1)
		{
			_gameInstance.AudioModule.ActionOnEvent(_walkPlaybackId, (AkActionOnEventType)0);
			_walkPlaybackId = -1;
		}
	}

	private void PlayLocalSoundEventWithBlockType(Vector3 blockPosition, uint soundEventIndex)
	{
		blockPosition.Y -= 0.01f;
		int block = _gameInstance.MapModule.GetBlock(blockPosition, 1);
		ClientBlockType clientBlockType = _gameInstance.MapModule.ClientBlockTypes[block];
		if (_gameInstance.AudioModule.TryPlayLocalBlockSoundEvent(clientBlockType.BlockSoundSetIndex, (BlockSoundEvent)0, ref _walkPlaybackId))
		{
			PlayLocalSoundEvent(soundEventIndex);
		}
	}

	private void PlayLocalSoundEvent(uint soundEventIndex)
	{
		if (soundEventIndex != 0)
		{
			if (_walkPlaybackId != -1)
			{
				_gameInstance.AudioModule.ActionOnEvent(_walkPlaybackId, (AkActionOnEventType)3);
			}
			_walkPlaybackId = _gameInstance.AudioModule.PlayLocalSoundEvent(soundEventIndex);
		}
	}

	private void TickFootsteps(Vector3 blockPosition)
	{
		EntityAnimation currentMovementAnimation = _gameInstance.LocalPlayer.CurrentMovementAnimation;
		int[] footstepIntervals = currentMovementAnimation.FootstepIntervals;
		float slotAnimationTime = _gameInstance.LocalPlayer.ModelRenderer.GetSlotAnimationTime(0);
		int num = (int)System.Math.Floor(slotAnimationTime / (float)currentMovementAnimation.Data.Duration * 100f);
		int nextFootstepIntervalIndex = _nextFootstepIntervalIndex;
		int num2 = footstepIntervals[nextFootstepIntervalIndex];
		int num3 = ((nextFootstepIntervalIndex < footstepIntervals.Length - 1) ? (nextFootstepIntervalIndex + 1) : 0);
		int num4 = footstepIntervals[num3];
		if (num > num2 && (num < num4 || num4 < num2))
		{
			PlayLocalSoundEventWithBlockType(blockPosition, currentMovementAnimation.SoundEventIndex);
			_nextFootstepIntervalIndex = num3;
		}
	}
}
