using System.Collections.Generic;
using HytaleClient.Math;

namespace HytaleClient.Data.BlockyModels;

internal class BlockyAnimation
{
	public enum BlockyAnimNodeInterpolationType
	{
		None,
		Linear,
		Smooth
	}

	public class BlockyAnimKeyframe<T>
	{
		public int Time;

		public T Delta;

		public BlockyAnimNodeInterpolationType InterpolationType = BlockyAnimNodeInterpolationType.None;
	}

	public class BlockyAnimNodeAnim
	{
		public BlockyAnimNodeFrame[] Frames;

		public bool HasPosition;

		public bool HasOrientation;

		public bool HasShapeStretch;

		public bool HasShapeVisible;

		public bool HasShapeUvOffset;
	}

	public struct BlockyAnimNodeFrame
	{
		public Vector3 Position;

		public Quaternion Orientation;

		public Vector3 ShapeStretch;

		public bool ShapeVisible;

		public Point ShapeUvOffset;
	}

	public const int FramesPerSecond = 60;

	public int Duration;

	public bool HoldLastKeyframe;

	public Dictionary<int, BlockyAnimNodeAnim> NodeAnimationsByNameId = new Dictionary<int, BlockyAnimNodeAnim>();

	public static void GetInterpolationData<T>(List<BlockyAnimKeyframe<T>> keyframes, float time, bool holdLastKeyframe, int duration, out T prevDelta, out T nextDelta, out float t)
	{
		BlockyAnimKeyframe<T> blockyAnimKeyframe = keyframes[keyframes.Count - 1];
		BlockyAnimKeyframe<T> blockyAnimKeyframe2 = null;
		for (int i = 0; i < keyframes.Count; i++)
		{
			blockyAnimKeyframe2 = keyframes[i];
			if ((float)blockyAnimKeyframe2.Time > time)
			{
				break;
			}
			blockyAnimKeyframe = keyframes[i];
		}
		if (blockyAnimKeyframe == blockyAnimKeyframe2)
		{
			blockyAnimKeyframe2 = keyframes[0];
		}
		int num = blockyAnimKeyframe2.Time - blockyAnimKeyframe.Time;
		if (num < 0 && !holdLastKeyframe)
		{
			num += duration;
		}
		float num2 = time - (float)blockyAnimKeyframe.Time;
		if (num2 < 0f)
		{
			num2 += (float)duration;
		}
		t = ((num > 0) ? (num2 / (float)num) : 0f);
		if (blockyAnimKeyframe.InterpolationType == BlockyAnimNodeInterpolationType.Smooth)
		{
			t = Easing.CubicEaseInAndOut(t);
		}
		prevDelta = blockyAnimKeyframe.Delta;
		nextDelta = blockyAnimKeyframe2.Delta;
	}
}
