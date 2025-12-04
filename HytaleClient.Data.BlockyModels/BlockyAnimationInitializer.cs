using System.Collections.Generic;
using HytaleClient.Math;
using Utf8Json;
using Utf8Json.Resolvers;

namespace HytaleClient.Data.BlockyModels;

internal class BlockyAnimationInitializer
{
	public static void Parse(byte[] data, NodeNameManager nodeNameManager, ref BlockyAnimation blockyAnimation)
	{
		BlockyAnimationJson blockyAnimationJson = JsonSerializer.Deserialize<BlockyAnimationJson>(data, StandardResolver.CamelCase);
		blockyAnimation.Duration = blockyAnimationJson.Duration;
		blockyAnimation.HoldLastKeyframe = blockyAnimationJson.HoldLastKeyframe;
		foreach (KeyValuePair<string, AnimationNode> nodeAnimation in blockyAnimationJson.NodeAnimations)
		{
			BlockyAnimation.BlockyAnimNodeAnim blockyAnimNodeAnim = new BlockyAnimation.BlockyAnimNodeAnim();
			blockyAnimNodeAnim.Frames = new BlockyAnimation.BlockyAnimNodeFrame[blockyAnimation.Duration + 1];
			BlockyAnimation.BlockyAnimNodeAnim blockyAnimNodeAnim2 = blockyAnimNodeAnim;
			int orAddNameId = nodeNameManager.GetOrAddNameId(nodeAnimation.Key);
			blockyAnimation.NodeAnimationsByNameId[orAddNameId] = blockyAnimNodeAnim2;
			AnimationNode value = nodeAnimation.Value;
			List<BlockyAnimation.BlockyAnimKeyframe<Vector3>> list = new List<BlockyAnimation.BlockyAnimKeyframe<Vector3>>();
			for (int i = 0; i < value.Position.Length; i++)
			{
				ref PositionFrame reference = ref value.Position[i];
				list.Add(new BlockyAnimation.BlockyAnimKeyframe<Vector3>
				{
					Time = reference.Time,
					Delta = reference.Delta,
					InterpolationType = ((!(reference.InterpolationType == "smooth")) ? BlockyAnimation.BlockyAnimNodeInterpolationType.Linear : BlockyAnimation.BlockyAnimNodeInterpolationType.Smooth)
				});
			}
			List<BlockyAnimation.BlockyAnimKeyframe<Quaternion>> list2 = new List<BlockyAnimation.BlockyAnimKeyframe<Quaternion>>();
			for (int j = 0; j < value.Orientation.Length; j++)
			{
				ref OrientationFrame reference2 = ref value.Orientation[j];
				list2.Add(new BlockyAnimation.BlockyAnimKeyframe<Quaternion>
				{
					Time = reference2.Time,
					Delta = reference2.Delta,
					InterpolationType = ((!(reference2.InterpolationType == "smooth")) ? BlockyAnimation.BlockyAnimNodeInterpolationType.Linear : BlockyAnimation.BlockyAnimNodeInterpolationType.Smooth)
				});
			}
			List<BlockyAnimation.BlockyAnimKeyframe<Vector3>> list3 = new List<BlockyAnimation.BlockyAnimKeyframe<Vector3>>();
			for (int k = 0; k < value.ShapeStretch.Length; k++)
			{
				ref ShapeStretchFrame reference3 = ref value.ShapeStretch[k];
				list3.Add(new BlockyAnimation.BlockyAnimKeyframe<Vector3>
				{
					Time = reference3.Time,
					Delta = reference3.Delta,
					InterpolationType = ((!(reference3.InterpolationType == "smooth")) ? BlockyAnimation.BlockyAnimNodeInterpolationType.Linear : BlockyAnimation.BlockyAnimNodeInterpolationType.Smooth)
				});
			}
			List<BlockyAnimation.BlockyAnimKeyframe<bool>> list4 = new List<BlockyAnimation.BlockyAnimKeyframe<bool>>();
			for (int l = 0; l < value.ShapeVisible.Length; l++)
			{
				ref ShapeVisibleFrame reference4 = ref value.ShapeVisible[l];
				list4.Add(new BlockyAnimation.BlockyAnimKeyframe<bool>
				{
					Time = reference4.Time,
					Delta = reference4.Delta
				});
			}
			List<BlockyAnimation.BlockyAnimKeyframe<Point>> list5 = new List<BlockyAnimation.BlockyAnimKeyframe<Point>>();
			for (int m = 0; m < value.ShapeUvOffset.Length; m++)
			{
				ref ShapeUvOffsetFrame reference5 = ref value.ShapeUvOffset[m];
				list5.Add(new BlockyAnimation.BlockyAnimKeyframe<Point>
				{
					Time = reference5.Time,
					Delta = reference5.Delta
				});
			}
			blockyAnimNodeAnim2.HasPosition = list.Count > 0;
			blockyAnimNodeAnim2.HasOrientation = list2.Count > 0;
			blockyAnimNodeAnim2.HasShapeStretch = list3.Count > 0;
			blockyAnimNodeAnim2.HasShapeVisible = list4.Count > 0;
			blockyAnimNodeAnim2.HasShapeUvOffset = list5.Count > 0;
			for (int n = 0; n <= blockyAnimation.Duration; n++)
			{
				Vector3 prevDelta;
				Vector3 nextDelta;
				float t;
				if (blockyAnimNodeAnim2.HasPosition)
				{
					BlockyAnimation.GetInterpolationData(list, n, blockyAnimation.HoldLastKeyframe, blockyAnimation.Duration, out prevDelta, out nextDelta, out t);
					blockyAnimNodeAnim2.Frames[n].Position = Vector3.Lerp(prevDelta, nextDelta, t);
				}
				if (blockyAnimNodeAnim2.HasOrientation)
				{
					BlockyAnimation.GetInterpolationData(list2, n, blockyAnimation.HoldLastKeyframe, blockyAnimation.Duration, out var prevDelta2, out var nextDelta2, out t);
					blockyAnimNodeAnim2.Frames[n].Orientation = Quaternion.Slerp(prevDelta2, nextDelta2, t);
				}
				if (blockyAnimNodeAnim2.HasShapeStretch)
				{
					BlockyAnimation.GetInterpolationData(list3, n, blockyAnimation.HoldLastKeyframe, blockyAnimation.Duration, out prevDelta, out nextDelta, out t);
					blockyAnimNodeAnim2.Frames[n].ShapeStretch = Vector3.Lerp(prevDelta, nextDelta, t);
				}
				if (blockyAnimNodeAnim2.HasShapeVisible)
				{
					BlockyAnimation.GetInterpolationData(list4, n, blockyAnimation.HoldLastKeyframe, blockyAnimation.Duration, out var prevDelta3, out var nextDelta3, out t);
					blockyAnimNodeAnim2.Frames[n].ShapeVisible = ((t == 1f) ? nextDelta3 : prevDelta3);
				}
				if (blockyAnimNodeAnim2.HasShapeUvOffset)
				{
					BlockyAnimation.GetInterpolationData(list5, n, blockyAnimation.HoldLastKeyframe, blockyAnimation.Duration, out var prevDelta4, out var nextDelta4, out t);
					blockyAnimNodeAnim2.Frames[n].ShapeUvOffset = ((t == 1f) ? nextDelta4 : prevDelta4);
				}
			}
		}
	}
}
