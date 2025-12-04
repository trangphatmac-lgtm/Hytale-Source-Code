using System;
using System.Collections.Generic;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Machinima.TrackPath;

internal class SplinePath : LinePath
{
	private readonly int _segmentCount = 25;

	public SplinePath()
	{
	}

	public SplinePath(Vector3[] points, int segmentCount = 25)
	{
		_segmentCount = segmentCount;
		UpdatePoints(points);
	}

	public override void UpdatePoints(Vector3[] points)
	{
		base.ControlPoints = points;
		UpdateSegmentPoints();
	}

	public override Vector3 GetPathPosition(int index, float progress, bool lengthCorrected = false, Easing.EasingType easingType = Easing.EasingType.Linear)
	{
		if (base.ControlPoints == null || base.ControlPoints.Length < 1)
		{
			return Vector3.NaN;
		}
		if (index < 0)
		{
			return base.ControlPoints[0];
		}
		if (index >= base.ControlPoints.Length)
		{
			return base.ControlPoints[base.ControlPoints.Length - 1];
		}
		int num = index + 1;
		int num2 = ((index - 1 >= 0) ? (index - 1) : index);
		int num3 = ((index + 2 < base.ControlPoints.Length) ? (index + 2) : num);
		Vector3 p = base.ControlPoints[num2];
		Vector3 p2 = base.ControlPoints[index];
		Vector3 p3 = base.ControlPoints[num];
		Vector3 p4 = base.ControlPoints[num3];
		if (easingType != 0)
		{
			progress = Easing.Ease(easingType, progress);
		}
		if (lengthCorrected)
		{
			progress = GetAdjustedProgress(index, progress);
		}
		Vector3.Spline(ref progress, ref p, ref p2, ref p3, ref p4, out var result);
		return result;
	}

	private void UpdateSegmentPoints()
	{
		if (base.ControlPoints.Length < 2)
		{
			Array.Clear(base.SegmentInfo, 0, base.SegmentInfo.Length);
			Array.Clear(base.SegmentPoints, 0, base.SegmentPoints.Length);
			return;
		}
		List<Vector3[]> list = new List<Vector3[]>();
		List<Vector3[]> list2 = new List<Vector3[]>();
		List<Vector3> list3 = new List<Vector3>();
		List<Vector3> list4 = new List<Vector3>();
		for (int i = 0; i < base.ControlPoints.Length - 1; i++)
		{
			int num = i;
			int num2 = i + 1;
			int num3 = ((i - 1 >= 0) ? (i - 1) : num);
			int num4 = ((i + 2 < base.ControlPoints.Length) ? (i + 2) : num2);
			Vector3 p = base.ControlPoints[num3];
			Vector3 p2 = base.ControlPoints[num];
			Vector3 p3 = base.ControlPoints[num2];
			Vector3 p4 = base.ControlPoints[num4];
			float num5 = 0f;
			for (int j = 0; j <= _segmentCount; j++)
			{
				float t = (float)j / (float)_segmentCount;
				Vector3.Spline(ref t, ref p, ref p2, ref p3, ref p4, out var result);
				float num6 = ((j > 0) ? Vector3.Distance(list3[list3.Count - 1], result) : 0f);
				num5 += num6;
				list3.Add(result);
				list4.Add(new Vector3((float)i + t, 0f, num5));
			}
			if (num5 > 0f)
			{
				for (int k = 0; k < list4.Count; k++)
				{
					list4[k] = new Vector3(list4[k].X, list4[k].Z / num5, list4[k].Z);
				}
			}
			list.Add(list3.ToArray());
			list2.Add(list4.ToArray());
			list3.Clear();
			list4.Clear();
		}
		base.SegmentPoints = list.ToArray();
		base.SegmentInfo = list2.ToArray();
	}

	public override float GetAdjustedProgress(int index, float progress)
	{
		float num = (float)index + progress;
		for (int i = 0; i < base.SegmentInfo[index].Length; i++)
		{
			if (i + 1 >= base.SegmentInfo[index].Length)
			{
				return 1f;
			}
			float y = base.SegmentInfo[index][i].Y;
			float num2 = ((y >= 1f) ? 1f : base.SegmentInfo[index][i + 1].Y);
			if (progress >= y && progress < num2)
			{
				float num3 = num2 - y;
				float amount = (progress - y) / num3;
				float num4 = MathHelper.Lerp(base.SegmentInfo[index][i].X, base.SegmentInfo[index][i + 1].X, amount);
				return num4 - (float)index;
			}
		}
		return progress;
	}
}
