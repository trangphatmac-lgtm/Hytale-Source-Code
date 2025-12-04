using System;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Data.Items;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.Entities;

internal class EntityAnimation
{
	public static class AnimationSlot
	{
		public const int Movement = 0;

		public const int PrimaryItemMovement = 1;

		public const int SecondaryItemMovement = 2;

		public const int Passive = 3;

		public const int Status = 4;

		public const int Action = 5;

		public const int ServerAction = 6;

		public const int Face = 7;

		public const int Emote = 8;

		public const int SlotCount = 9;

		public static bool GetSlot(AnimationSlot slot, out int slotId)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Expected I4, but got Unknown
			switch ((int)slot)
			{
			case 0:
				slotId = 0;
				return true;
			case 1:
				slotId = 4;
				return true;
			case 2:
				slotId = 6;
				return true;
			case 3:
				slotId = 7;
				return true;
			case 4:
				slotId = 8;
				return true;
			default:
				slotId = -1;
				return false;
			}
		}
	}

	public static readonly EntityAnimation Empty = new EntityAnimation(null, 1f, 12f, looping: false, keepPreviousFirstPersonAnimation: false, 0u, 0f, Array.Empty<int>(), 0);

	public const float DefaultBlendingDuration = 12f;

	public ClientItemPullbackConfig PullbackConfig;

	public BlockyAnimation Data { get; }

	public float Speed { get; }

	public float BlendingDuration { get; }

	public bool Looping { get; }

	public bool KeepPreviousFirstPersonAnimation { get; }

	public uint SoundEventIndex { get; }

	public BlockyAnimation MovingData { get; }

	public BlockyAnimation FaceData { get; }

	public BlockyAnimation FirstPersonData { get; }

	public BlockyAnimation FirstPersonOverrideData { get; }

	public bool ClipsGeometry { get; }

	public float Weight { get; }

	public int[] FootstepIntervals { get; }

	public int PassiveLoopCount { get; }

	public EntityAnimation(BlockyAnimation data, float speed, float blendingDuration, bool looping, bool keepPreviousFirstPersonAnimation, uint soundEventIndex, float weight, int[] footstepIntervals, int passiveLoopCount, BlockyAnimation movingData = null, BlockyAnimation faceData = null, BlockyAnimation firstPersonData = null, BlockyAnimation firstPersonOverrideData = null, ItemPullbackConfiguration pullbackConfig = null, bool clipsGeometry = false)
	{
		Data = data;
		Speed = speed;
		BlendingDuration = blendingDuration;
		Looping = looping;
		KeepPreviousFirstPersonAnimation = keepPreviousFirstPersonAnimation;
		SoundEventIndex = soundEventIndex;
		MovingData = movingData ?? data;
		FaceData = faceData;
		FirstPersonData = firstPersonData;
		FirstPersonOverrideData = firstPersonOverrideData;
		Weight = weight;
		FootstepIntervals = footstepIntervals;
		PassiveLoopCount = passiveLoopCount;
		if (pullbackConfig != null)
		{
			PullbackConfig = new ClientItemPullbackConfig(pullbackConfig);
		}
		ClipsGeometry = clipsGeometry;
	}

	public EntityAnimation(EntityAnimation animation)
	{
		Data = animation.Data;
		Speed = animation.Speed;
		BlendingDuration = animation.BlendingDuration;
		Looping = animation.Looping;
		KeepPreviousFirstPersonAnimation = animation.KeepPreviousFirstPersonAnimation;
		SoundEventIndex = animation.SoundEventIndex;
		MovingData = animation.MovingData;
		FaceData = animation.FaceData;
		FirstPersonData = animation.FirstPersonData;
		FirstPersonOverrideData = animation.FirstPersonOverrideData;
		Weight = animation.Weight;
		FootstepIntervals = animation.FootstepIntervals;
		PassiveLoopCount = animation.PassiveLoopCount;
		PullbackConfig = animation.PullbackConfig;
	}
}
