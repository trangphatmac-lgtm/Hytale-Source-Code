using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HytaleClient.Math;

[Serializable]
[DebuggerDisplay("{DebugDisplayString,nq}")]
public struct BoundingBox : IEquatable<BoundingBox>
{
	public Vector3 Min;

	public Vector3 Max;

	public const int CornerCount = 8;

	private static readonly Vector3 MaxVector3 = new Vector3(float.MaxValue);

	private static readonly Vector3 MinVector3 = new Vector3(float.MinValue);

	internal string DebugDisplayString => "Min( " + Min.DebugDisplayString + " ) \r\n" + "Max( " + Max.DebugDisplayString + " )";

	public BoundingBox(Vector3 min, Vector3 max)
	{
		Min = min;
		Max = max;
	}

	public void Contains(ref BoundingBox box, out ContainmentType result)
	{
		result = Contains(box);
	}

	public void Contains(ref BoundingSphere sphere, out ContainmentType result)
	{
		result = Contains(sphere);
	}

	public ContainmentType Contains(Vector3 point)
	{
		Contains(ref point, out var result);
		return result;
	}

	public ContainmentType Contains(BoundingBox box)
	{
		if (box.Max.X < Min.X || box.Min.X > Max.X || box.Max.Y < Min.Y || box.Min.Y > Max.Y || box.Max.Z < Min.Z || box.Min.Z > Max.Z)
		{
			return ContainmentType.Disjoint;
		}
		if (box.Min.X >= Min.X && box.Max.X <= Max.X && box.Min.Y >= Min.Y && box.Max.Y <= Max.Y && box.Min.Z >= Min.Z && box.Max.Z <= Max.Z)
		{
			return ContainmentType.Contains;
		}
		return ContainmentType.Intersects;
	}

	public ContainmentType Contains(BoundingFrustum frustum)
	{
		Vector3[] corners = frustum.GetCorners();
		ContainmentType result;
		int i;
		for (i = 0; i < corners.Length; i++)
		{
			Contains(ref corners[i], out result);
			if (result == ContainmentType.Disjoint)
			{
				break;
			}
		}
		if (i == corners.Length)
		{
			return ContainmentType.Contains;
		}
		if (i != 0)
		{
			return ContainmentType.Intersects;
		}
		for (i++; i < corners.Length; i++)
		{
			Contains(ref corners[i], out result);
			if (result != ContainmentType.Contains)
			{
				return ContainmentType.Intersects;
			}
		}
		return ContainmentType.Contains;
	}

	public ContainmentType Contains(BoundingSphere sphere)
	{
		if (sphere.Center.X - Min.X >= sphere.Radius && sphere.Center.Y - Min.Y >= sphere.Radius && sphere.Center.Z - Min.Z >= sphere.Radius && Max.X - sphere.Center.X >= sphere.Radius && Max.Y - sphere.Center.Y >= sphere.Radius && Max.Z - sphere.Center.Z >= sphere.Radius)
		{
			return ContainmentType.Contains;
		}
		double num = 0.0;
		double num2 = sphere.Center.X - Min.X;
		if (num2 < 0.0)
		{
			if (num2 < (double)(0f - sphere.Radius))
			{
				return ContainmentType.Disjoint;
			}
			num += num2 * num2;
		}
		else
		{
			num2 = sphere.Center.X - Max.X;
			if (num2 > 0.0)
			{
				if (num2 > (double)sphere.Radius)
				{
					return ContainmentType.Disjoint;
				}
				num += num2 * num2;
			}
		}
		num2 = sphere.Center.Y - Min.Y;
		if (num2 < 0.0)
		{
			if (num2 < (double)(0f - sphere.Radius))
			{
				return ContainmentType.Disjoint;
			}
			num += num2 * num2;
		}
		else
		{
			num2 = sphere.Center.Y - Max.Y;
			if (num2 > 0.0)
			{
				if (num2 > (double)sphere.Radius)
				{
					return ContainmentType.Disjoint;
				}
				num += num2 * num2;
			}
		}
		num2 = sphere.Center.Z - Min.Z;
		if (num2 < 0.0)
		{
			if (num2 < (double)(0f - sphere.Radius))
			{
				return ContainmentType.Disjoint;
			}
			num += num2 * num2;
		}
		else
		{
			num2 = sphere.Center.Z - Max.Z;
			if (num2 > 0.0)
			{
				if (num2 > (double)sphere.Radius)
				{
					return ContainmentType.Disjoint;
				}
				num += num2 * num2;
			}
		}
		if (num <= (double)(sphere.Radius * sphere.Radius))
		{
			return ContainmentType.Intersects;
		}
		return ContainmentType.Disjoint;
	}

	public void Contains(ref Vector3 point, out ContainmentType result)
	{
		if (point.X < Min.X || point.X > Max.X || point.Y < Min.Y || point.Y > Max.Y || point.Z < Min.Z || point.Z > Max.Z)
		{
			result = ContainmentType.Disjoint;
		}
		else
		{
			result = ContainmentType.Contains;
		}
	}

	public Vector3[] GetCorners()
	{
		return new Vector3[8]
		{
			new Vector3(Min.X, Max.Y, Max.Z),
			new Vector3(Max.X, Max.Y, Max.Z),
			new Vector3(Max.X, Min.Y, Max.Z),
			new Vector3(Min.X, Min.Y, Max.Z),
			new Vector3(Min.X, Max.Y, Min.Z),
			new Vector3(Max.X, Max.Y, Min.Z),
			new Vector3(Max.X, Min.Y, Min.Z),
			new Vector3(Min.X, Min.Y, Min.Z)
		};
	}

	public void GetCorners(Vector3[] corners)
	{
		if (corners == null)
		{
			throw new ArgumentNullException("corners");
		}
		if (corners.Length < 8)
		{
			throw new ArgumentOutOfRangeException("corners", "Not Enought Corners");
		}
		corners[0].X = Min.X;
		corners[0].Y = Max.Y;
		corners[0].Z = Max.Z;
		corners[1].X = Max.X;
		corners[1].Y = Max.Y;
		corners[1].Z = Max.Z;
		corners[2].X = Max.X;
		corners[2].Y = Min.Y;
		corners[2].Z = Max.Z;
		corners[3].X = Min.X;
		corners[3].Y = Min.Y;
		corners[3].Z = Max.Z;
		corners[4].X = Min.X;
		corners[4].Y = Max.Y;
		corners[4].Z = Min.Z;
		corners[5].X = Max.X;
		corners[5].Y = Max.Y;
		corners[5].Z = Min.Z;
		corners[6].X = Max.X;
		corners[6].Y = Min.Y;
		corners[6].Z = Min.Z;
		corners[7].X = Min.X;
		corners[7].Y = Min.Y;
		corners[7].Z = Min.Z;
	}

	public float? Intersects(Ray ray)
	{
		return ray.Intersects(this);
	}

	public void Intersects(ref Ray ray, out float? result)
	{
		result = Intersects(ray);
	}

	public bool Intersects(BoundingFrustum frustum)
	{
		return frustum.Intersects(this);
	}

	public void Intersects(ref BoundingSphere sphere, out bool result)
	{
		result = Intersects(sphere);
	}

	public bool Intersects(BoundingBox box)
	{
		Intersects(ref box, out var result);
		return result;
	}

	public PlaneIntersectionType Intersects(Plane plane)
	{
		Intersects(ref plane, out var result);
		return result;
	}

	public void Intersects(ref BoundingBox box, out bool result)
	{
		if (Max.X >= box.Min.X && Min.X <= box.Max.X)
		{
			if (Max.Y < box.Min.Y || Min.Y > box.Max.Y)
			{
				result = false;
			}
			else
			{
				result = Max.Z >= box.Min.Z && Min.Z <= box.Max.Z;
			}
		}
		else
		{
			result = false;
		}
	}

	public bool Intersects(BoundingSphere sphere)
	{
		if (sphere.Center.X - Min.X > sphere.Radius && sphere.Center.Y - Min.Y > sphere.Radius && sphere.Center.Z - Min.Z > sphere.Radius && Max.X - sphere.Center.X > sphere.Radius && Max.Y - sphere.Center.Y > sphere.Radius && Max.Z - sphere.Center.Z > sphere.Radius)
		{
			return true;
		}
		double num = 0.0;
		if (sphere.Center.X - Min.X <= sphere.Radius)
		{
			num += (double)((sphere.Center.X - Min.X) * (sphere.Center.X - Min.X));
		}
		else if (Max.X - sphere.Center.X <= sphere.Radius)
		{
			num += (double)((sphere.Center.X - Max.X) * (sphere.Center.X - Max.X));
		}
		if (sphere.Center.Y - Min.Y <= sphere.Radius)
		{
			num += (double)((sphere.Center.Y - Min.Y) * (sphere.Center.Y - Min.Y));
		}
		else if (Max.Y - sphere.Center.Y <= sphere.Radius)
		{
			num += (double)((sphere.Center.Y - Max.Y) * (sphere.Center.Y - Max.Y));
		}
		if (sphere.Center.Z - Min.Z <= sphere.Radius)
		{
			num += (double)((sphere.Center.Z - Min.Z) * (sphere.Center.Z - Min.Z));
		}
		else if (Max.Z - sphere.Center.Z <= sphere.Radius)
		{
			num += (double)((sphere.Center.Z - Max.Z) * (sphere.Center.Z - Max.Z));
		}
		if (num <= (double)(sphere.Radius * sphere.Radius))
		{
			return true;
		}
		return false;
	}

	public void Intersects(ref Plane plane, out PlaneIntersectionType result)
	{
		Vector3 vector = default(Vector3);
		Vector3 vector2 = default(Vector3);
		if (plane.Normal.X >= 0f)
		{
			vector.X = Max.X;
			vector2.X = Min.X;
		}
		else
		{
			vector.X = Min.X;
			vector2.X = Max.X;
		}
		if (plane.Normal.Y >= 0f)
		{
			vector.Y = Max.Y;
			vector2.Y = Min.Y;
		}
		else
		{
			vector.Y = Min.Y;
			vector2.Y = Max.Y;
		}
		if (plane.Normal.Z >= 0f)
		{
			vector.Z = Max.Z;
			vector2.Z = Min.Z;
		}
		else
		{
			vector.Z = Min.Z;
			vector2.Z = Max.Z;
		}
		float num = plane.Normal.X * vector2.X + plane.Normal.Y * vector2.Y + plane.Normal.Z * vector2.Z + plane.D;
		if (num > 0f)
		{
			result = PlaneIntersectionType.Front;
			return;
		}
		num = plane.Normal.X * vector.X + plane.Normal.Y * vector.Y + plane.Normal.Z * vector.Z + plane.D;
		if (num < 0f)
		{
			result = PlaneIntersectionType.Back;
		}
		else
		{
			result = PlaneIntersectionType.Intersecting;
		}
	}

	public bool Equals(BoundingBox other)
	{
		return Min == other.Min && Max == other.Max;
	}

	public static BoundingBox CreateFromPoints(IEnumerable<Vector3> points)
	{
		if (points == null)
		{
			throw new ArgumentNullException("points");
		}
		bool flag = true;
		Vector3 maxVector = MaxVector3;
		Vector3 minVector = MinVector3;
		foreach (Vector3 point in points)
		{
			maxVector.X = ((maxVector.X < point.X) ? maxVector.X : point.X);
			maxVector.Y = ((maxVector.Y < point.Y) ? maxVector.Y : point.Y);
			maxVector.Z = ((maxVector.Z < point.Z) ? maxVector.Z : point.Z);
			minVector.X = ((minVector.X > point.X) ? minVector.X : point.X);
			minVector.Y = ((minVector.Y > point.Y) ? minVector.Y : point.Y);
			minVector.Z = ((minVector.Z > point.Z) ? minVector.Z : point.Z);
			flag = false;
		}
		if (flag)
		{
			throw new ArgumentException("Collection is empty", "points");
		}
		return new BoundingBox(maxVector, minVector);
	}

	public static BoundingBox CreateFromSphere(BoundingSphere sphere)
	{
		CreateFromSphere(ref sphere, out var result);
		return result;
	}

	public static void CreateFromSphere(ref BoundingSphere sphere, out BoundingBox result)
	{
		Vector3 vector = new Vector3(sphere.Radius);
		result.Min = sphere.Center - vector;
		result.Max = sphere.Center + vector;
	}

	public static BoundingBox CreateMerged(BoundingBox original, BoundingBox additional)
	{
		CreateMerged(ref original, ref additional, out var result);
		return result;
	}

	public static void CreateMerged(ref BoundingBox original, ref BoundingBox additional, out BoundingBox result)
	{
		result.Min.X = System.Math.Min(original.Min.X, additional.Min.X);
		result.Min.Y = System.Math.Min(original.Min.Y, additional.Min.Y);
		result.Min.Z = System.Math.Min(original.Min.Z, additional.Min.Z);
		result.Max.X = System.Math.Max(original.Max.X, additional.Max.X);
		result.Max.Y = System.Math.Max(original.Max.Y, additional.Max.Y);
		result.Max.Z = System.Math.Max(original.Max.Z, additional.Max.Z);
	}

	public override bool Equals(object obj)
	{
		return obj is BoundingBox && Equals((BoundingBox)obj);
	}

	public override int GetHashCode()
	{
		return Min.GetHashCode() + Max.GetHashCode();
	}

	public static bool operator ==(BoundingBox a, BoundingBox b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(BoundingBox a, BoundingBox b)
	{
		return !a.Equals(b);
	}

	public override string ToString()
	{
		return "{{Min:" + Min.ToString() + " Max:" + Max.ToString() + "}}";
	}

	public void Grow(Vector3 amount)
	{
		Min -= amount;
		Max += amount;
	}

	public void Translate(Vector3 offset)
	{
		Min.X += offset.X;
		Min.Y += offset.Y;
		Min.Z += offset.Z;
		Max.X += offset.X;
		Max.Y += offset.Y;
		Max.Z += offset.Z;
	}

	public BoundingBox MinkowskiSum(BoundingBox bb)
	{
		return new BoundingBox(Min - bb.Max, Max - bb.Min);
	}

	public Vector3 GetSize()
	{
		return new Vector3(Max.X - Min.X, Max.Y - Min.Y, Max.Z - Min.Z);
	}

	public Vector3 GetCenter()
	{
		return new Vector3(Max.X + Min.X, Max.Y + Min.Y, Max.Z + Min.Z) * 0.5f;
	}

	public bool IntersectsExclusive(BoundingBox box)
	{
		if (Max.X > box.Min.X && Min.X < box.Max.X)
		{
			if (Max.Y <= box.Min.Y || Min.Y >= box.Max.Y)
			{
				return false;
			}
			return Max.Z > box.Min.Z && Min.Z < box.Max.Z;
		}
		return false;
	}

	public bool IntersectsExclusive(BoundingBox box, float offsetX, float offsetY, float offsetZ)
	{
		float num = box.Min.X + offsetX;
		float num2 = box.Max.X + offsetX;
		if (Max.X > num && Min.X < num2)
		{
			float num3 = box.Min.Y + offsetY;
			float num4 = box.Max.Y + offsetY;
			if (Max.Y <= num3 || Min.Y >= num4)
			{
				return false;
			}
			float num5 = box.Min.Z + offsetZ;
			float num6 = box.Max.Z + offsetZ;
			return Max.Z > num5 && Min.Z < num6;
		}
		return false;
	}

	public float GetVolume()
	{
		Vector3 size = GetSize();
		return size.X * size.Y * size.Z;
	}

	public bool ForEachBlock<T>(Vector3 v, float epsilon, T t, Func<int, int, int, T, bool> consumer)
	{
		return ForEachBlock(v.X, v.Y, v.Z, epsilon, t, consumer);
	}

	public bool ForEachBlock<T>(float x, float y, float z, float epsilon, T t, Func<int, int, int, T, bool> consumer)
	{
		int num = (int)System.Math.Floor(x + Min.X - epsilon);
		int num2 = (int)System.Math.Floor(y + Min.Y - epsilon);
		int num3 = (int)System.Math.Floor(z + Min.Z - epsilon);
		int num4 = (int)System.Math.Floor(x + Max.X + epsilon);
		int num5 = (int)System.Math.Floor(y + Max.Y + epsilon);
		int num6 = (int)System.Math.Floor(z + Max.Z + epsilon);
		for (int i = num; i <= num4; i++)
		{
			for (int j = num2; j <= num5; j++)
			{
				for (int k = num3; k <= num6; k++)
				{
					if (!consumer(i, j, k, t))
					{
						return false;
					}
				}
			}
		}
		return true;
	}
}
