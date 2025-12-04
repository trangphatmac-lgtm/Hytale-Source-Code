using System;
using System.Collections.Generic;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Machinima.TrackPath;

internal class BezierPath : LinePath
{
	private readonly int _segmentCount = 25;

	private readonly int _curveOrder = 3;

	public BezierPath()
	{
	}

	public BezierPath(Vector3[] points, int curveOrder = 3, int segmentCount = 25)
	{
		_segmentCount = segmentCount;
		_curveOrder = curveOrder;
		UpdatePoints(points);
	}

	public override void UpdatePoints(Vector3[] points)
	{
		base.ControlPoints = points;
		UpdateSegmentPoints();
	}

	public override Vector3 GetPathPosition(int index, float progress, bool lengthCorrected = false, Easing.EasingType easingType = Easing.EasingType.Linear)
	{
		if (base.ControlPoints.Length < 1)
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
		int num = index * _curveOrder;
		Vector3[] array = new Vector3[_curveOrder + 1];
		for (int i = 0; i <= _curveOrder; i++)
		{
			array[i] = base.ControlPoints[num + i];
		}
		if (easingType != 0)
		{
			progress = Easing.Ease(easingType, progress);
		}
		if (lengthCorrected)
		{
			progress = GetAdjustedProgress(index, progress);
		}
		Vector3.CubicBezierCurve(ref progress, ref array[0], ref array[1], ref array[2], ref array[3], out var result);
		return result;
	}

	public Vector3 GetPathTangent(int index, float progress, bool lengthCorrected = false)
	{
		if (base.ControlPoints.Length < 1)
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
		int num = index * _curveOrder;
		Vector3[] array = new Vector3[_curveOrder + 1];
		for (int i = 0; i <= _curveOrder; i++)
		{
			array[i] = base.ControlPoints[num + i];
		}
		if (lengthCorrected)
		{
			progress = GetAdjustedProgress(index, progress);
		}
		Vector3 result = new Vector3(MathHelper.CubicBezierCurveTangent(progress, array[0].X, array[1].X, array[2].X, array[3].X), MathHelper.CubicBezierCurveTangent(progress, array[0].Y, array[1].Y, array[2].Y, array[3].Y), MathHelper.CubicBezierCurveTangent(progress, array[0].Z, array[1].Z, array[2].Z, array[3].Z));
		result.Normalize();
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
		Vector3[] array = new Vector3[_curveOrder + 1];
		int num = 0;
		for (int i = 0; i < base.ControlPoints.Length - 1; i += _curveOrder)
		{
			for (int j = 0; j <= _curveOrder; j++)
			{
				array[j] = base.ControlPoints[i + j];
			}
			float num2 = 0f;
			for (int k = 0; k <= _segmentCount; k++)
			{
				float t = (float)k / (float)_segmentCount;
				Vector3.CubicBezierCurve(ref t, ref array[0], ref array[1], ref array[2], ref array[3], out var result);
				float num3 = ((k > 0) ? Vector3.Distance(list3[list3.Count - 1], result) : 0f);
				num2 += num3;
				list3.Add(result);
				list4.Add(new Vector3((float)num + t, 0f, num2));
			}
			for (int l = 0; l < list4.Count; l++)
			{
				list4[l] = new Vector3(list4[l].X, list4[l].Z / num2, list4[l].Z);
			}
			list.Add(list3.ToArray());
			list2.Add(list4.ToArray());
			list3.Clear();
			list4.Clear();
			num++;
		}
		base.SegmentPoints = list.ToArray();
		base.SegmentInfo = list2.ToArray();
	}

	public override float GetAdjustedProgress(int index, float progress)
	{
		float num = (float)index + progress;
		for (int i = 0; i < base.SegmentInfo[index].Length; i++)
		{
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

	public static void GetCurveControlPoints(Vector3[] knots, out Vector3[] firstControlPoints, out Vector3[] secondControlPoints)
	{
		if (knots == null)
		{
			throw new ArgumentNullException("knots");
		}
		int num = knots.Length - 1;
		if (num < 1)
		{
			throw new ArgumentException("At least two knot points required", "knots");
		}
		if (num == 1)
		{
			firstControlPoints = new Vector3[1];
			firstControlPoints[0].X = (2f * knots[0].X + knots[1].X) / 3f;
			firstControlPoints[0].Y = (2f * knots[0].Y + knots[1].Y) / 3f;
			secondControlPoints = new Vector3[1];
			secondControlPoints[0].X = 2f * firstControlPoints[0].X - knots[0].X;
			secondControlPoints[0].Y = 2f * firstControlPoints[0].Y - knots[0].Y;
			return;
		}
		float[] array = new float[num];
		for (int i = 1; i < num - 1; i++)
		{
			array[i] = 4f * knots[i].X + 2f * knots[i + 1].X;
		}
		array[0] = knots[0].X + 2f * knots[1].X;
		array[num - 1] = (8f * knots[num - 1].X + knots[num].X) / 2f;
		float[] firstControlPoints2 = GetFirstControlPoints(array);
		for (int j = 1; j < num - 1; j++)
		{
			array[j] = 4f * knots[j].Y + 2f * knots[j + 1].Y;
		}
		array[0] = knots[0].Y + 2f * knots[1].Y;
		array[num - 1] = (8f * knots[num - 1].Y + knots[num].Y) / 2f;
		float[] firstControlPoints3 = GetFirstControlPoints(array);
		for (int k = 1; k < num - 1; k++)
		{
			array[k] = 4f * knots[k].Z + 2f * knots[k + 1].Z;
		}
		array[0] = knots[0].Z + 2f * knots[1].Z;
		array[num - 1] = (8f * knots[num - 1].Z + knots[num].Z) / 2f;
		float[] firstControlPoints4 = GetFirstControlPoints(array);
		firstControlPoints = new Vector3[num];
		secondControlPoints = new Vector3[num];
		for (int l = 0; l < num; l++)
		{
			firstControlPoints[l] = new Vector3(firstControlPoints2[l], firstControlPoints3[l], firstControlPoints4[l]);
			if (l < num - 1)
			{
				secondControlPoints[l] = new Vector3(2f * knots[l + 1].X - firstControlPoints2[l + 1], 2f * knots[l + 1].Y - firstControlPoints3[l + 1], 2f * knots[l + 1].Z - firstControlPoints4[l + 1]);
			}
			else
			{
				secondControlPoints[l] = new Vector3((knots[num].X + firstControlPoints2[num - 1]) / 2f, (knots[num].Y + firstControlPoints3[num - 1]) / 2f, (knots[num].Z + firstControlPoints4[num - 1]) / 2f);
			}
		}
	}

	private static float[] GetFirstControlPoints(float[] rhs)
	{
		int num = rhs.Length;
		float[] array = new float[num];
		float[] array2 = new float[num];
		float num2 = 2f;
		array[0] = rhs[0] / num2;
		for (int i = 1; i < num; i++)
		{
			array2[i] = 1f / num2;
			num2 = (((float)i < (float)num - 1f) ? 4f : 3.5f) - array2[i];
			array[i] = (rhs[i] - array[i - 1]) / num2;
		}
		for (int j = 1; j < num; j++)
		{
			array[num - j - 1] -= array2[num - j] * array[num - j];
		}
		return array;
	}
}
