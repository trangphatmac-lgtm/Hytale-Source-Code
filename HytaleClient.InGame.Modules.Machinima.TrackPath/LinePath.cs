using System.Collections.Generic;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Machinima.TrackPath;

internal abstract class LinePath
{
	public Vector3[] ControlPoints { get; protected set; }

	public Vector3[][] SegmentPoints { get; protected set; }

	public Vector3[][] SegmentInfo { get; protected set; }

	public LinePath()
	{
	}

	public LinePath(Vector3[] points)
	{
		UpdatePoints(points);
	}

	public virtual void UpdatePoints(Vector3[] points)
	{
		ControlPoints = points;
	}

	public abstract Vector3 GetPathPosition(int index, float progress, bool lengthCorrected = false, Easing.EasingType easingType = Easing.EasingType.Linear);

	public virtual Vector3[] GetDrawPoints()
	{
		List<Vector3> list = new List<Vector3>();
		for (int i = 0; i < SegmentPoints.Length; i++)
		{
			Vector3[] array = SegmentPoints[i];
			foreach (Vector3 item in array)
			{
				list.Add(item);
			}
		}
		return list.ToArray();
	}

	public virtual float[] GetDrawFrames()
	{
		List<float> list = new List<float>();
		for (int i = 0; i < SegmentInfo.Length; i++)
		{
			Vector3[] array = SegmentInfo[i];
			for (int j = 0; j < array.Length; j++)
			{
				Vector3 vector = array[j];
				list.Add(vector.X);
			}
		}
		return list.ToArray();
	}

	public virtual float[] GetSegmentLengths()
	{
		float[] array = new float[SegmentInfo.Length];
		for (int i = 0; i < SegmentInfo.Length; i++)
		{
			array[i] = SegmentInfo[i][SegmentInfo[i].Length - 1].Z;
		}
		return array;
	}

	public virtual float GetAdjustedProgress(int index, float progress)
	{
		return progress;
	}
}
