using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HytaleClient.Math;

[Serializable]
[DebuggerDisplay("{DebugDisplayString,nq}")]
public struct BoundingSphere : IEquatable<BoundingSphere>
{
	public Vector3 Center;

	public float Radius;

	internal string DebugDisplayString => "Center( " + Center.DebugDisplayString + " ) \r\n" + "Radius( " + Radius + " ) ";

	public BoundingSphere(Vector3 center, float radius)
	{
		Center = center;
		Radius = radius;
	}

	public BoundingSphere Transform(Matrix matrix)
	{
		BoundingSphere result = default(BoundingSphere);
		result.Center = Vector3.Transform(Center, matrix);
		result.Radius = Radius * (float)System.Math.Sqrt(System.Math.Max(matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12 + matrix.M13 * matrix.M13, System.Math.Max(matrix.M21 * matrix.M21 + matrix.M22 * matrix.M22 + matrix.M23 * matrix.M23, matrix.M31 * matrix.M31 + matrix.M32 * matrix.M32 + matrix.M33 * matrix.M33)));
		return result;
	}

	public void Transform(ref Matrix matrix, out BoundingSphere result)
	{
		result.Center = Vector3.Transform(Center, matrix);
		result.Radius = Radius * (float)System.Math.Sqrt(System.Math.Max(matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12 + matrix.M13 * matrix.M13, System.Math.Max(matrix.M21 * matrix.M21 + matrix.M22 * matrix.M22 + matrix.M23 * matrix.M23, matrix.M31 * matrix.M31 + matrix.M32 * matrix.M32 + matrix.M33 * matrix.M33)));
	}

	public void Contains(ref BoundingBox box, out ContainmentType result)
	{
		result = Contains(box);
	}

	public void Contains(ref BoundingSphere sphere, out ContainmentType result)
	{
		result = Contains(sphere);
	}

	public void Contains(ref Vector3 point, out ContainmentType result)
	{
		result = Contains(point);
	}

	public ContainmentType Contains(BoundingBox box)
	{
		bool flag = true;
		Vector3[] corners = box.GetCorners();
		foreach (Vector3 point in corners)
		{
			if (Contains(point) == ContainmentType.Disjoint)
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			return ContainmentType.Contains;
		}
		double num = 0.0;
		if (Center.X < box.Min.X)
		{
			num += (double)((Center.X - box.Min.X) * (Center.X - box.Min.X));
		}
		else if (Center.X > box.Max.X)
		{
			num += (double)((Center.X - box.Max.X) * (Center.X - box.Max.X));
		}
		if (Center.Y < box.Min.Y)
		{
			num += (double)((Center.Y - box.Min.Y) * (Center.Y - box.Min.Y));
		}
		else if (Center.Y > box.Max.Y)
		{
			num += (double)((Center.Y - box.Max.Y) * (Center.Y - box.Max.Y));
		}
		if (Center.Z < box.Min.Z)
		{
			num += (double)((Center.Z - box.Min.Z) * (Center.Z - box.Min.Z));
		}
		else if (Center.Z > box.Max.Z)
		{
			num += (double)((Center.Z - box.Max.Z) * (Center.Z - box.Max.Z));
		}
		if (num <= (double)(Radius * Radius))
		{
			return ContainmentType.Intersects;
		}
		return ContainmentType.Disjoint;
	}

	public ContainmentType Contains(BoundingFrustum frustum)
	{
		bool flag = true;
		Vector3[] corners = frustum.GetCorners();
		Vector3[] array = corners;
		foreach (Vector3 point in array)
		{
			if (Contains(point) == ContainmentType.Disjoint)
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			return ContainmentType.Contains;
		}
		double num = 0.0;
		if (num <= (double)(Radius * Radius))
		{
			return ContainmentType.Intersects;
		}
		return ContainmentType.Disjoint;
	}

	public ContainmentType Contains(BoundingSphere sphere)
	{
		Vector3.DistanceSquared(ref sphere.Center, ref Center, out var result);
		if (result > (sphere.Radius + Radius) * (sphere.Radius + Radius))
		{
			return ContainmentType.Disjoint;
		}
		if (result <= Radius * sphere.Radius * (Radius - sphere.Radius))
		{
			return ContainmentType.Contains;
		}
		return ContainmentType.Intersects;
	}

	public ContainmentType Contains(Vector3 point)
	{
		float num = Radius * Radius;
		Vector3.DistanceSquared(ref point, ref Center, out var result);
		if (result > num)
		{
			return ContainmentType.Disjoint;
		}
		if (result < num)
		{
			return ContainmentType.Contains;
		}
		return ContainmentType.Intersects;
	}

	public bool Equals(BoundingSphere other)
	{
		return Center == other.Center && Radius == other.Radius;
	}

	public static BoundingSphere CreateFromBoundingBox(BoundingBox box)
	{
		CreateFromBoundingBox(ref box, out var result);
		return result;
	}

	public static void CreateFromBoundingBox(ref BoundingBox box, out BoundingSphere result)
	{
		Vector3 vector = new Vector3((box.Min.X + box.Max.X) * 0.5f, (box.Min.Y + box.Max.Y) * 0.5f, (box.Min.Z + box.Max.Z) * 0.5f);
		float radius = Vector3.Distance(vector, box.Max);
		result = new BoundingSphere(vector, radius);
	}

	public static BoundingSphere CreateFromFrustum(BoundingFrustum frustum)
	{
		return CreateFromPoints(frustum.GetCorners());
	}

	public static BoundingSphere CreateFromPoints(IEnumerable<Vector3> points)
	{
		if (points == null)
		{
			throw new ArgumentNullException("points");
		}
		Vector3 vector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 vector2 = -vector;
		Vector3 vector3 = vector;
		Vector3 vector4 = -vector;
		Vector3 vector5 = vector;
		Vector3 vector6 = -vector;
		int num = 0;
		foreach (Vector3 point in points)
		{
			num++;
			if (point.X < vector.X)
			{
				vector = point;
			}
			if (point.X > vector2.X)
			{
				vector2 = point;
			}
			if (point.Y < vector3.Y)
			{
				vector3 = point;
			}
			if (point.Y > vector4.Y)
			{
				vector4 = point;
			}
			if (point.Z < vector5.Z)
			{
				vector5 = point;
			}
			if (point.Z > vector6.Z)
			{
				vector6 = point;
			}
		}
		if (num == 0)
		{
			throw new ArgumentException("You should have at least one point in points.");
		}
		float num2 = Vector3.DistanceSquared(vector2, vector);
		float num3 = Vector3.DistanceSquared(vector4, vector3);
		float num4 = Vector3.DistanceSquared(vector6, vector5);
		Vector3 vector7 = vector;
		Vector3 vector8 = vector2;
		if (num3 > num2 && num3 > num4)
		{
			vector8 = vector4;
			vector7 = vector3;
		}
		if (num4 > num2 && num4 > num3)
		{
			vector8 = vector6;
			vector7 = vector5;
		}
		Vector3 vector9 = (vector7 + vector8) * 0.5f;
		float num5 = Vector3.Distance(vector8, vector9);
		float num6 = num5 * num5;
		foreach (Vector3 point2 in points)
		{
			Vector3 vector10 = point2 - vector9;
			float num7 = vector10.LengthSquared();
			if (num7 > num6)
			{
				float num8 = (float)System.Math.Sqrt(num7);
				Vector3 vector11 = vector10 / num8;
				Vector3 vector12 = vector9 - num5 * vector11;
				vector9 = (vector12 + point2) * 0.5f;
				num5 = Vector3.Distance(point2, vector9);
				num6 = num5 * num5;
			}
		}
		return new BoundingSphere(vector9, num5);
	}

	public static BoundingSphere CreateMerged(BoundingSphere original, BoundingSphere additional)
	{
		CreateMerged(ref original, ref additional, out var result);
		return result;
	}

	public static void CreateMerged(ref BoundingSphere original, ref BoundingSphere additional, out BoundingSphere result)
	{
		Vector3 vector = Vector3.Subtract(additional.Center, original.Center);
		float num = vector.Length();
		if (num <= original.Radius + additional.Radius)
		{
			if (num <= original.Radius - additional.Radius)
			{
				result = original;
				return;
			}
			if (num <= additional.Radius - original.Radius)
			{
				result = additional;
				return;
			}
		}
		float num2 = System.Math.Max(original.Radius - num, additional.Radius);
		float num3 = System.Math.Max(original.Radius + num, additional.Radius);
		vector += (num2 - num3) / (2f * vector.Length()) * vector;
		result = default(BoundingSphere);
		result.Center = original.Center + vector;
		result.Radius = (num2 + num3) * 0.5f;
	}

	public bool Intersects(BoundingBox box)
	{
		return box.Intersects(this);
	}

	public void Intersects(ref BoundingBox box, out bool result)
	{
		box.Intersects(ref this, out result);
	}

	public bool Intersects(BoundingFrustum frustum)
	{
		return frustum.Intersects(this);
	}

	public bool Intersects(BoundingSphere sphere)
	{
		Intersects(ref sphere, out var result);
		return result;
	}

	public void Intersects(ref BoundingSphere sphere, out bool result)
	{
		Vector3.DistanceSquared(ref sphere.Center, ref Center, out var result2);
		result = !(result2 > (sphere.Radius + Radius) * (sphere.Radius + Radius));
	}

	public float? Intersects(Ray ray)
	{
		return ray.Intersects(this);
	}

	public void Intersects(ref Ray ray, out float? result)
	{
		ray.Intersects(ref this, out result);
	}

	public PlaneIntersectionType Intersects(Plane plane)
	{
		PlaneIntersectionType result = PlaneIntersectionType.Front;
		Intersects(ref plane, out result);
		return result;
	}

	public void Intersects(ref Plane plane, out PlaneIntersectionType result)
	{
		float result2 = 0f;
		Vector3.Dot(ref plane.Normal, ref Center, out result2);
		result2 += plane.D;
		if (result2 > Radius)
		{
			result = PlaneIntersectionType.Front;
		}
		else if (result2 < 0f - Radius)
		{
			result = PlaneIntersectionType.Back;
		}
		else
		{
			result = PlaneIntersectionType.Intersecting;
		}
	}

	public override bool Equals(object obj)
	{
		return obj is BoundingSphere && Equals((BoundingSphere)obj);
	}

	public override int GetHashCode()
	{
		return Center.GetHashCode() + Radius.GetHashCode();
	}

	public override string ToString()
	{
		return "{Center:" + Center.ToString() + " Radius:" + Radius + "}";
	}

	public static bool operator ==(BoundingSphere a, BoundingSphere b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(BoundingSphere a, BoundingSphere b)
	{
		return !a.Equals(b);
	}
}
