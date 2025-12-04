using System.Runtime.CompilerServices;

namespace HytaleClient.Math;

public struct KDop
{
	public int PlaneCount;

	public Plane[] Planes;

	public KDop(int planesCount = 13)
	{
		PlaneCount = planesCount;
		Planes = new Plane[PlaneCount];
	}

	public void BuildFrom(BoundingFrustum frustum, Vector3 direction)
	{
		int num = 0;
		Vector3[] array = new Vector3[8];
		frustum.GetCorners(array);
		for (int i = 0; i < 6; i++)
		{
			ref Plane plane = ref frustum.GetPlane((BoundingFrustum.PlaneId)i);
			if (Vector3.Dot(plane.Normal, direction) > 0f)
			{
				Planes[num] = plane;
				num++;
			}
		}
		for (int j = 0; j < 6; j++)
		{
			if (Vector3.Dot(frustum.GetPlane((BoundingFrustum.PlaneId)j).Normal, direction) < 0f)
			{
				continue;
			}
			for (uint num2 = 0u; num2 < 4; num2++)
			{
				BoundingFrustum.PlaneId neighbourId = BoundingFrustum.GetNeighbourId((BoundingFrustum.PlaneId)j, num2);
				if (Vector3.Dot(frustum.GetPlane(neighbourId).Normal, direction) < 0f)
				{
					BoundingFrustum.GetSharedCorners((BoundingFrustum.PlaneId)j, neighbourId, out var cornerA, out var cornerB);
					Plane plane2 = new Plane(array[(int)cornerA], array[(int)cornerB], array[(int)cornerA] + direction);
					Planes[num] = plane2;
					num++;
				}
			}
		}
		PlaneCount = num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Intersects(BoundingSphere volume)
	{
		Contains(volume, out var result);
		return result != ContainmentType.Disjoint;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Intersects(BoundingBox volume)
	{
		Contains(volume, out var result);
		return result != ContainmentType.Disjoint;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Contains(BoundingSphere volume, out ContainmentType result)
	{
		bool flag = false;
		for (int i = 0; i < PlaneCount; i++)
		{
			PlaneIntersectionType result2 = PlaneIntersectionType.Front;
			volume.Intersects(ref Planes[i], out result2);
			switch (result2)
			{
			case PlaneIntersectionType.Front:
				result = ContainmentType.Disjoint;
				return;
			case PlaneIntersectionType.Intersecting:
				flag = true;
				break;
			}
		}
		result = ((!flag) ? ContainmentType.Contains : ContainmentType.Intersects);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Contains(BoundingBox volume, out ContainmentType result)
	{
		bool flag = false;
		for (int i = 0; i < PlaneCount; i++)
		{
			PlaneIntersectionType result2 = PlaneIntersectionType.Front;
			volume.Intersects(ref Planes[i], out result2);
			switch (result2)
			{
			case PlaneIntersectionType.Front:
				result = ContainmentType.Disjoint;
				return;
			case PlaneIntersectionType.Intersecting:
				flag = true;
				break;
			}
		}
		result = ((!flag) ? ContainmentType.Contains : ContainmentType.Intersects);
	}
}
