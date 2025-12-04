using System;
using System.Diagnostics;

namespace HytaleClient.Math;

[Serializable]
[DebuggerDisplay("{DebugDisplayString,nq}")]
public struct Matrix : IEquatable<Matrix>
{
	public float M11;

	public float M12;

	public float M13;

	public float M14;

	public float M21;

	public float M22;

	public float M23;

	public float M24;

	public float M31;

	public float M32;

	public float M33;

	public float M34;

	public float M41;

	public float M42;

	public float M43;

	public float M44;

	private static Matrix identity = new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f);

	public Vector3 Backward
	{
		get
		{
			return new Vector3(M31, M32, M33);
		}
		set
		{
			M31 = value.X;
			M32 = value.Y;
			M33 = value.Z;
		}
	}

	public Vector3 Down
	{
		get
		{
			return new Vector3(0f - M21, 0f - M22, 0f - M23);
		}
		set
		{
			M21 = 0f - value.X;
			M22 = 0f - value.Y;
			M23 = 0f - value.Z;
		}
	}

	public Vector3 Forward
	{
		get
		{
			return new Vector3(0f - M31, 0f - M32, 0f - M33);
		}
		set
		{
			M31 = 0f - value.X;
			M32 = 0f - value.Y;
			M33 = 0f - value.Z;
		}
	}

	public static Matrix Identity => identity;

	public Vector3 Left
	{
		get
		{
			return new Vector3(0f - M11, 0f - M12, 0f - M13);
		}
		set
		{
			M11 = 0f - value.X;
			M12 = 0f - value.Y;
			M13 = 0f - value.Z;
		}
	}

	public Vector3 Right
	{
		get
		{
			return new Vector3(M11, M12, M13);
		}
		set
		{
			M11 = value.X;
			M12 = value.Y;
			M13 = value.Z;
		}
	}

	public Vector3 Translation
	{
		get
		{
			return new Vector3(M41, M42, M43);
		}
		set
		{
			M41 = value.X;
			M42 = value.Y;
			M43 = value.Z;
		}
	}

	public Vector3 Up
	{
		get
		{
			return new Vector3(M21, M22, M23);
		}
		set
		{
			M21 = value.X;
			M22 = value.Y;
			M23 = value.Z;
		}
	}

	internal string DebugDisplayString => "( " + M11 + " " + M12 + " " + M13 + " " + M14 + " ) \r\n" + "( " + M21 + " " + M22 + " " + M23 + " " + M24 + " ) \r\n" + "( " + M31 + " " + M32 + " " + M33 + " " + M34 + " ) \r\n" + "( " + M41 + " " + M42 + " " + M43 + " " + M44 + " )";

	public Vector3 Row0 => new Vector3(M11, M12, M13);

	public Vector3 Row1 => new Vector3(M21, M22, M23);

	public Vector3 Row2 => new Vector3(M31, M32, M33);

	public Vector3 Row3 => new Vector3(M41, M42, M43);

	public Vector3 Column0 => new Vector3(M11, M21, M31);

	public Vector3 Column1 => new Vector3(M12, M22, M32);

	public Vector3 Column2 => new Vector3(M13, M23, M33);

	public Vector3 Column3 => new Vector3(M14, M24, M34);

	public Matrix(float m11, float m12, float m13, float m14, float m21, float m22, float m23, float m24, float m31, float m32, float m33, float m34, float m41, float m42, float m43, float m44)
	{
		M11 = m11;
		M12 = m12;
		M13 = m13;
		M14 = m14;
		M21 = m21;
		M22 = m22;
		M23 = m23;
		M24 = m24;
		M31 = m31;
		M32 = m32;
		M33 = m33;
		M34 = m34;
		M41 = m41;
		M42 = m42;
		M43 = m43;
		M44 = m44;
	}

	public bool Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation)
	{
		translation.X = M41;
		translation.Y = M42;
		translation.Z = M43;
		float num = ((System.Math.Sign(M11 * M12 * M13 * M14) >= 0) ? 1 : (-1));
		float num2 = ((System.Math.Sign(M21 * M22 * M23 * M24) >= 0) ? 1 : (-1));
		float num3 = ((System.Math.Sign(M31 * M32 * M33 * M34) >= 0) ? 1 : (-1));
		scale.X = num * (float)System.Math.Sqrt(M11 * M11 + M12 * M12 + M13 * M13);
		scale.Y = num2 * (float)System.Math.Sqrt(M21 * M21 + M22 * M22 + M23 * M23);
		scale.Z = num3 * (float)System.Math.Sqrt(M31 * M31 + M32 * M32 + M33 * M33);
		if (MathHelper.WithinEpsilon(scale.X, 0f) || MathHelper.WithinEpsilon(scale.Y, 0f) || MathHelper.WithinEpsilon(scale.Z, 0f))
		{
			rotation = Quaternion.Identity;
			return false;
		}
		Matrix matrix = new Matrix(M11 / scale.X, M12 / scale.X, M13 / scale.X, 0f, M21 / scale.Y, M22 / scale.Y, M23 / scale.Y, 0f, M31 / scale.Z, M32 / scale.Z, M33 / scale.Z, 0f, 0f, 0f, 0f, 1f);
		rotation = Quaternion.CreateFromRotationMatrix(matrix);
		return true;
	}

	public float Determinant()
	{
		float num = M33 * M44 - M34 * M43;
		float num2 = M32 * M44 - M34 * M42;
		float num3 = M32 * M43 - M33 * M42;
		float num4 = M31 * M44 - M34 * M41;
		float num5 = M31 * M43 - M33 * M41;
		float num6 = M31 * M42 - M32 * M41;
		return M11 * (M22 * num - M23 * num2 + M24 * num3) - M12 * (M21 * num - M23 * num4 + M24 * num5) + M13 * (M21 * num2 - M22 * num4 + M24 * num6) - M14 * (M21 * num3 - M22 * num5 + M23 * num6);
	}

	public bool Equals(Matrix other)
	{
		return M11 == other.M11 && M12 == other.M12 && M13 == other.M13 && M14 == other.M14 && M21 == other.M21 && M22 == other.M22 && M23 == other.M23 && M24 == other.M24 && M31 == other.M31 && M32 == other.M32 && M33 == other.M33 && M34 == other.M34 && M41 == other.M41 && M42 == other.M42 && M43 == other.M43 && M44 == other.M44;
	}

	public override bool Equals(object obj)
	{
		return obj is Matrix && Equals((Matrix)obj);
	}

	public override int GetHashCode()
	{
		return M11.GetHashCode() + M12.GetHashCode() + M13.GetHashCode() + M14.GetHashCode() + M21.GetHashCode() + M22.GetHashCode() + M23.GetHashCode() + M24.GetHashCode() + M31.GetHashCode() + M32.GetHashCode() + M33.GetHashCode() + M34.GetHashCode() + M41.GetHashCode() + M42.GetHashCode() + M43.GetHashCode() + M44.GetHashCode();
	}

	public override string ToString()
	{
		return "{M11:" + M11 + " M12:" + M12 + " M13:" + M13 + " M14:" + M14 + "} {M21:" + M21 + " M22:" + M22 + " M23:" + M23 + " M24:" + M24 + "} {M31:" + M31 + " M32:" + M32 + " M33:" + M33 + " M34:" + M34 + "} {M41:" + M41 + " M42:" + M42 + " M43:" + M43 + " M44:" + M44 + "}";
	}

	public static Matrix Add(Matrix matrix1, Matrix matrix2)
	{
		matrix1.M11 += matrix2.M11;
		matrix1.M12 += matrix2.M12;
		matrix1.M13 += matrix2.M13;
		matrix1.M14 += matrix2.M14;
		matrix1.M21 += matrix2.M21;
		matrix1.M22 += matrix2.M22;
		matrix1.M23 += matrix2.M23;
		matrix1.M24 += matrix2.M24;
		matrix1.M31 += matrix2.M31;
		matrix1.M32 += matrix2.M32;
		matrix1.M33 += matrix2.M33;
		matrix1.M34 += matrix2.M34;
		matrix1.M41 += matrix2.M41;
		matrix1.M42 += matrix2.M42;
		matrix1.M43 += matrix2.M43;
		matrix1.M44 += matrix2.M44;
		return matrix1;
	}

	public static void Add(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
	{
		result.M11 = matrix1.M11 + matrix2.M11;
		result.M12 = matrix1.M12 + matrix2.M12;
		result.M13 = matrix1.M13 + matrix2.M13;
		result.M14 = matrix1.M14 + matrix2.M14;
		result.M21 = matrix1.M21 + matrix2.M21;
		result.M22 = matrix1.M22 + matrix2.M22;
		result.M23 = matrix1.M23 + matrix2.M23;
		result.M24 = matrix1.M24 + matrix2.M24;
		result.M31 = matrix1.M31 + matrix2.M31;
		result.M32 = matrix1.M32 + matrix2.M32;
		result.M33 = matrix1.M33 + matrix2.M33;
		result.M34 = matrix1.M34 + matrix2.M34;
		result.M41 = matrix1.M41 + matrix2.M41;
		result.M42 = matrix1.M42 + matrix2.M42;
		result.M43 = matrix1.M43 + matrix2.M43;
		result.M44 = matrix1.M44 + matrix2.M44;
	}

	public static Matrix CreateBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 cameraUpVector, Vector3? cameraForwardVector)
	{
		CreateBillboard(ref objectPosition, ref cameraPosition, ref cameraUpVector, cameraForwardVector, out var result);
		return result;
	}

	public static void CreateBillboard(ref Vector3 objectPosition, ref Vector3 cameraPosition, ref Vector3 cameraUpVector, Vector3? cameraForwardVector, out Matrix result)
	{
		Vector3 value = default(Vector3);
		value.X = objectPosition.X - cameraPosition.X;
		value.Y = objectPosition.Y - cameraPosition.Y;
		value.Z = objectPosition.Z - cameraPosition.Z;
		float num = value.LengthSquared();
		if (num < 0.0001f)
		{
			value = (cameraForwardVector.HasValue ? (-cameraForwardVector.Value) : Vector3.Forward);
		}
		else
		{
			Vector3.Multiply(ref value, 1f / (float)System.Math.Sqrt(num), out value);
		}
		Vector3.Cross(ref cameraUpVector, ref value, out var result2);
		result2.Normalize();
		Vector3.Cross(ref value, ref result2, out var result3);
		result.M11 = result2.X;
		result.M12 = result2.Y;
		result.M13 = result2.Z;
		result.M14 = 0f;
		result.M21 = result3.X;
		result.M22 = result3.Y;
		result.M23 = result3.Z;
		result.M24 = 0f;
		result.M31 = value.X;
		result.M32 = value.Y;
		result.M33 = value.Z;
		result.M34 = 0f;
		result.M41 = objectPosition.X;
		result.M42 = objectPosition.Y;
		result.M43 = objectPosition.Z;
		result.M44 = 1f;
	}

	public static Matrix CreateConstrainedBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 rotateAxis, Vector3? cameraForwardVector, Vector3? objectForwardVector)
	{
		CreateConstrainedBillboard(ref objectPosition, ref cameraPosition, ref rotateAxis, cameraForwardVector, objectForwardVector, out var result);
		return result;
	}

	public static void CreateConstrainedBillboard(ref Vector3 objectPosition, ref Vector3 cameraPosition, ref Vector3 rotateAxis, Vector3? cameraForwardVector, Vector3? objectForwardVector, out Matrix result)
	{
		Vector3 value = default(Vector3);
		value.X = objectPosition.X - cameraPosition.X;
		value.Y = objectPosition.Y - cameraPosition.Y;
		value.Z = objectPosition.Z - cameraPosition.Z;
		float num = value.LengthSquared();
		if (num < 0.0001f)
		{
			value = (cameraForwardVector.HasValue ? (-cameraForwardVector.Value) : Vector3.Forward);
		}
		else
		{
			Vector3.Multiply(ref value, 1f / (float)System.Math.Sqrt(num), out value);
		}
		Vector3 vector = rotateAxis;
		Vector3.Dot(ref rotateAxis, ref value, out var result2);
		Vector3 result3;
		Vector3 result4;
		if (System.Math.Abs(result2) > 0.9982547f)
		{
			if (objectForwardVector.HasValue)
			{
				result3 = objectForwardVector.Value;
				Vector3.Dot(ref rotateAxis, ref result3, out result2);
				if (System.Math.Abs(result2) > 0.9982547f)
				{
					result2 = rotateAxis.X * Vector3.Forward.X + rotateAxis.Y * Vector3.Forward.Y + rotateAxis.Z * Vector3.Forward.Z;
					result3 = ((System.Math.Abs(result2) > 0.9982547f) ? Vector3.Right : Vector3.Forward);
				}
			}
			else
			{
				result2 = rotateAxis.X * Vector3.Forward.X + rotateAxis.Y * Vector3.Forward.Y + rotateAxis.Z * Vector3.Forward.Z;
				result3 = ((System.Math.Abs(result2) > 0.9982547f) ? Vector3.Right : Vector3.Forward);
			}
			Vector3.Cross(ref rotateAxis, ref result3, out result4);
			result4.Normalize();
			Vector3.Cross(ref result4, ref rotateAxis, out result3);
			result3.Normalize();
		}
		else
		{
			Vector3.Cross(ref rotateAxis, ref value, out result4);
			result4.Normalize();
			Vector3.Cross(ref result4, ref vector, out result3);
			result3.Normalize();
		}
		result.M11 = result4.X;
		result.M12 = result4.Y;
		result.M13 = result4.Z;
		result.M14 = 0f;
		result.M21 = vector.X;
		result.M22 = vector.Y;
		result.M23 = vector.Z;
		result.M24 = 0f;
		result.M31 = result3.X;
		result.M32 = result3.Y;
		result.M33 = result3.Z;
		result.M34 = 0f;
		result.M41 = objectPosition.X;
		result.M42 = objectPosition.Y;
		result.M43 = objectPosition.Z;
		result.M44 = 1f;
	}

	public static Matrix CreateFromAxisAngle(Vector3 axis, float angle)
	{
		CreateFromAxisAngle(ref axis, angle, out var result);
		return result;
	}

	public static void CreateFromAxisAngle(ref Vector3 axis, float angle, out Matrix result)
	{
		float x = axis.X;
		float y = axis.Y;
		float z = axis.Z;
		float num = (float)System.Math.Sin(angle);
		float num2 = (float)System.Math.Cos(angle);
		float num3 = x * x;
		float num4 = y * y;
		float num5 = z * z;
		float num6 = x * y;
		float num7 = x * z;
		float num8 = y * z;
		result.M11 = num3 + num2 * (1f - num3);
		result.M12 = num6 - num2 * num6 + num * z;
		result.M13 = num7 - num2 * num7 - num * y;
		result.M14 = 0f;
		result.M21 = num6 - num2 * num6 - num * z;
		result.M22 = num4 + num2 * (1f - num4);
		result.M23 = num8 - num2 * num8 + num * x;
		result.M24 = 0f;
		result.M31 = num7 - num2 * num7 + num * y;
		result.M32 = num8 - num2 * num8 - num * x;
		result.M33 = num5 + num2 * (1f - num5);
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
	}

	public static Matrix CreateFromQuaternion(Quaternion quaternion)
	{
		CreateFromQuaternion(ref quaternion, out var result);
		return result;
	}

	public static void CreateFromQuaternion(ref Quaternion quaternion, out Matrix result)
	{
		float num = quaternion.X * quaternion.X;
		float num2 = quaternion.Y * quaternion.Y;
		float num3 = quaternion.Z * quaternion.Z;
		float num4 = quaternion.X * quaternion.Y;
		float num5 = quaternion.Z * quaternion.W;
		float num6 = quaternion.Z * quaternion.X;
		float num7 = quaternion.Y * quaternion.W;
		float num8 = quaternion.Y * quaternion.Z;
		float num9 = quaternion.X * quaternion.W;
		result.M11 = 1f - 2f * (num2 + num3);
		result.M12 = 2f * (num4 + num5);
		result.M13 = 2f * (num6 - num7);
		result.M14 = 0f;
		result.M21 = 2f * (num4 - num5);
		result.M22 = 1f - 2f * (num3 + num);
		result.M23 = 2f * (num8 + num9);
		result.M24 = 0f;
		result.M31 = 2f * (num6 + num7);
		result.M32 = 2f * (num8 - num9);
		result.M33 = 1f - 2f * (num2 + num);
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
	}

	public static Matrix CreateFromYawPitchRoll(float yaw, float pitch, float roll)
	{
		CreateFromYawPitchRoll(yaw, pitch, roll, out var result);
		return result;
	}

	public static void CreateFromYawPitchRoll(float yaw, float pitch, float roll, out Matrix result)
	{
		Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll, out var result2);
		CreateFromQuaternion(ref result2, out result);
	}

	public static Matrix CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
	{
		CreateLookAt(ref cameraPosition, ref cameraTarget, ref cameraUpVector, out var result);
		return result;
	}

	public static void CreateLookAt(ref Vector3 cameraPosition, ref Vector3 cameraTarget, ref Vector3 cameraUpVector, out Matrix result)
	{
		Vector3 vector = Vector3.Normalize(cameraPosition - cameraTarget);
		Vector3 vector2 = Vector3.Normalize(Vector3.Cross(cameraUpVector, vector));
		Vector3 vector3 = Vector3.Cross(vector, vector2);
		result.M11 = vector2.X;
		result.M12 = vector3.X;
		result.M13 = vector.X;
		result.M14 = 0f;
		result.M21 = vector2.Y;
		result.M22 = vector3.Y;
		result.M23 = vector.Y;
		result.M24 = 0f;
		result.M31 = vector2.Z;
		result.M32 = vector3.Z;
		result.M33 = vector.Z;
		result.M34 = 0f;
		result.M41 = 0f - Vector3.Dot(vector2, cameraPosition);
		result.M42 = 0f - Vector3.Dot(vector3, cameraPosition);
		result.M43 = 0f - Vector3.Dot(vector, cameraPosition);
		result.M44 = 1f;
	}

	public static Matrix CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane)
	{
		CreateOrthographic(width, height, zNearPlane, zFarPlane, out var result);
		return result;
	}

	public static void CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane, out Matrix result)
	{
		result.M11 = 2f / width;
		result.M12 = (result.M13 = (result.M14 = 0f));
		result.M22 = 2f / height;
		result.M21 = (result.M23 = (result.M24 = 0f));
		result.M33 = 1f / (zNearPlane - zFarPlane);
		result.M31 = (result.M32 = (result.M34 = 0f));
		result.M41 = (result.M42 = 0f);
		result.M43 = zNearPlane / (zNearPlane - zFarPlane);
		result.M44 = 1f;
	}

	public static Matrix CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
	{
		CreateOrthographicOffCenter(left, right, bottom, top, zNearPlane, zFarPlane, out var result);
		return result;
	}

	public static void CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane, out Matrix result)
	{
		result.M11 = (float)(2.0 / ((double)right - (double)left));
		result.M12 = 0f;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = (float)(2.0 / ((double)top - (double)bottom));
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f;
		result.M33 = (float)(1.0 / ((double)zNearPlane - (double)zFarPlane));
		result.M34 = 0f;
		result.M41 = (float)(((double)left + (double)right) / ((double)left - (double)right));
		result.M42 = (float)(((double)top + (double)bottom) / ((double)bottom - (double)top));
		result.M43 = (float)((double)zNearPlane / ((double)zNearPlane - (double)zFarPlane));
		result.M44 = 1f;
	}

	public static Matrix CreatePerspective(float width, float height, float nearPlaneDistance, float farPlaneDistance)
	{
		CreatePerspective(width, height, nearPlaneDistance, farPlaneDistance, out var result);
		return result;
	}

	public static void CreatePerspective(float width, float height, float nearPlaneDistance, float farPlaneDistance, out Matrix result)
	{
		if (nearPlaneDistance <= 0f)
		{
			throw new ArgumentException("nearPlaneDistance <= 0");
		}
		if (farPlaneDistance <= 0f)
		{
			throw new ArgumentException("farPlaneDistance <= 0");
		}
		if (nearPlaneDistance >= farPlaneDistance)
		{
			throw new ArgumentException("nearPlaneDistance >= farPlaneDistance");
		}
		result.M11 = 2f * nearPlaneDistance / width;
		result.M12 = (result.M13 = (result.M14 = 0f));
		result.M22 = 2f * nearPlaneDistance / height;
		result.M21 = (result.M23 = (result.M24 = 0f));
		result.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
		result.M31 = (result.M32 = 0f);
		result.M34 = -1f;
		result.M41 = (result.M42 = (result.M44 = 0f));
		result.M43 = nearPlaneDistance * farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
	}

	public static Matrix CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
	{
		CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, nearPlaneDistance, farPlaneDistance, out var result);
		return result;
	}

	public static void CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance, out Matrix result)
	{
		if (fieldOfView <= 0f || fieldOfView >= 3.141593f)
		{
			throw new ArgumentException("fieldOfView <= 0 or >= PI");
		}
		if (nearPlaneDistance <= 0f)
		{
			throw new ArgumentException("nearPlaneDistance <= 0");
		}
		if (farPlaneDistance <= 0f)
		{
			throw new ArgumentException("farPlaneDistance <= 0");
		}
		if (nearPlaneDistance >= farPlaneDistance)
		{
			throw new ArgumentException("nearPlaneDistance >= farPlaneDistance");
		}
		float num = 1f / (float)System.Math.Tan(fieldOfView * 0.5f);
		float m = num / aspectRatio;
		result.M11 = m;
		result.M12 = (result.M13 = (result.M14 = 0f));
		result.M22 = num;
		result.M21 = (result.M23 = (result.M24 = 0f));
		result.M31 = (result.M32 = 0f);
		result.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
		result.M34 = -1f;
		result.M41 = (result.M42 = (result.M44 = 0f));
		result.M43 = nearPlaneDistance * farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
	}

	public static Matrix CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float nearPlaneDistance, float farPlaneDistance)
	{
		CreatePerspectiveOffCenter(left, right, bottom, top, nearPlaneDistance, farPlaneDistance, out var result);
		return result;
	}

	public static void CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float nearPlaneDistance, float farPlaneDistance, out Matrix result)
	{
		if (nearPlaneDistance <= 0f)
		{
			throw new ArgumentException("nearPlaneDistance <= 0");
		}
		if (farPlaneDistance <= 0f)
		{
			throw new ArgumentException("farPlaneDistance <= 0");
		}
		if (nearPlaneDistance >= farPlaneDistance)
		{
			throw new ArgumentException("nearPlaneDistance >= farPlaneDistance");
		}
		result.M11 = 2f * nearPlaneDistance / (right - left);
		result.M12 = (result.M13 = (result.M14 = 0f));
		result.M22 = 2f * nearPlaneDistance / (top - bottom);
		result.M21 = (result.M23 = (result.M24 = 0f));
		result.M31 = (left + right) / (right - left);
		result.M32 = (top + bottom) / (top - bottom);
		result.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
		result.M34 = -1f;
		result.M43 = nearPlaneDistance * farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
		result.M41 = (result.M42 = (result.M44 = 0f));
	}

	public static Matrix CreateRotationX(float radians)
	{
		CreateRotationX(radians, out var result);
		return result;
	}

	public static void CreateRotationX(float radians, out Matrix result)
	{
		result = Identity;
		float num = (float)System.Math.Cos(radians);
		float num2 = (float)System.Math.Sin(radians);
		result.M22 = num;
		result.M23 = num2;
		result.M32 = 0f - num2;
		result.M33 = num;
	}

	public static Matrix CreateRotationY(float radians)
	{
		CreateRotationY(radians, out var result);
		return result;
	}

	public static void CreateRotationY(float radians, out Matrix result)
	{
		result = Identity;
		float num = (float)System.Math.Cos(radians);
		float num2 = (float)System.Math.Sin(radians);
		result.M11 = num;
		result.M13 = 0f - num2;
		result.M31 = num2;
		result.M33 = num;
	}

	public static Matrix CreateRotationZ(float radians)
	{
		CreateRotationZ(radians, out var result);
		return result;
	}

	public static void CreateRotationZ(float radians, out Matrix result)
	{
		result = Identity;
		float num = (float)System.Math.Cos(radians);
		float num2 = (float)System.Math.Sin(radians);
		result.M11 = num;
		result.M12 = num2;
		result.M21 = 0f - num2;
		result.M22 = num;
	}

	public static Matrix CreateScale(float scale)
	{
		CreateScale(scale, scale, scale, out var result);
		return result;
	}

	public static void CreateScale(float scale, out Matrix result)
	{
		CreateScale(scale, scale, scale, out result);
	}

	public static Matrix CreateScale(float xScale, float yScale, float zScale)
	{
		CreateScale(xScale, yScale, zScale, out var result);
		return result;
	}

	public static void CreateScale(float xScale, float yScale, float zScale, out Matrix result)
	{
		result.M11 = xScale;
		result.M12 = 0f;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = yScale;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f;
		result.M33 = zScale;
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
	}

	public static Matrix CreateScale(Vector3 scales)
	{
		CreateScale(ref scales, out var result);
		return result;
	}

	public static void CreateScale(ref Vector3 scales, out Matrix result)
	{
		result.M11 = scales.X;
		result.M12 = 0f;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = scales.Y;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f;
		result.M33 = scales.Z;
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
	}

	public static Matrix CreateShadow(Vector3 lightDirection, Plane plane)
	{
		CreateShadow(ref lightDirection, ref plane, out var result);
		return result;
	}

	public static void CreateShadow(ref Vector3 lightDirection, ref Plane plane, out Matrix result)
	{
		float num = plane.Normal.X * lightDirection.X + plane.Normal.Y * lightDirection.Y + plane.Normal.Z * lightDirection.Z;
		float num2 = 0f - plane.Normal.X;
		float num3 = 0f - plane.Normal.Y;
		float num4 = 0f - plane.Normal.Z;
		float num5 = 0f - plane.D;
		result.M11 = num2 * lightDirection.X + num;
		result.M12 = num2 * lightDirection.Y;
		result.M13 = num2 * lightDirection.Z;
		result.M14 = 0f;
		result.M21 = num3 * lightDirection.X;
		result.M22 = num3 * lightDirection.Y + num;
		result.M23 = num3 * lightDirection.Z;
		result.M24 = 0f;
		result.M31 = num4 * lightDirection.X;
		result.M32 = num4 * lightDirection.Y;
		result.M33 = num4 * lightDirection.Z + num;
		result.M34 = 0f;
		result.M41 = num5 * lightDirection.X;
		result.M42 = num5 * lightDirection.Y;
		result.M43 = num5 * lightDirection.Z;
		result.M44 = num;
	}

	public static Matrix CreateTranslation(float xPosition, float yPosition, float zPosition)
	{
		CreateTranslation(xPosition, yPosition, zPosition, out var result);
		return result;
	}

	public static void CreateTranslation(ref Vector3 position, out Matrix result)
	{
		result.M11 = 1f;
		result.M12 = 0f;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = 1f;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f;
		result.M33 = 1f;
		result.M34 = 0f;
		result.M41 = position.X;
		result.M42 = position.Y;
		result.M43 = position.Z;
		result.M44 = 1f;
	}

	public static Matrix CreateTranslation(Vector3 position)
	{
		CreateTranslation(ref position, out var result);
		return result;
	}

	public static void CreateTranslation(float xPosition, float yPosition, float zPosition, out Matrix result)
	{
		result.M11 = 1f;
		result.M12 = 0f;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = 1f;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f;
		result.M33 = 1f;
		result.M34 = 0f;
		result.M41 = xPosition;
		result.M42 = yPosition;
		result.M43 = zPosition;
		result.M44 = 1f;
	}

	public static Matrix CreateReflection(Plane value)
	{
		CreateReflection(ref value, out var result);
		return result;
	}

	public static void CreateReflection(ref Plane value, out Matrix result)
	{
		Plane.Normalize(ref value, out var result2);
		value.Normalize();
		float x = result2.Normal.X;
		float y = result2.Normal.Y;
		float z = result2.Normal.Z;
		float num = -2f * x;
		float num2 = -2f * y;
		float num3 = -2f * z;
		result.M11 = num * x + 1f;
		result.M12 = num2 * x;
		result.M13 = num3 * x;
		result.M14 = 0f;
		result.M21 = num * y;
		result.M22 = num2 * y + 1f;
		result.M23 = num3 * y;
		result.M24 = 0f;
		result.M31 = num * z;
		result.M32 = num2 * z;
		result.M33 = num3 * z + 1f;
		result.M34 = 0f;
		result.M41 = num * result2.D;
		result.M42 = num2 * result2.D;
		result.M43 = num3 * result2.D;
		result.M44 = 1f;
	}

	public static Matrix CreateWorld(Vector3 position, Vector3 forward, Vector3 up)
	{
		CreateWorld(ref position, ref forward, ref up, out var result);
		return result;
	}

	public static void CreateWorld(ref Vector3 position, ref Vector3 forward, ref Vector3 up, out Matrix result)
	{
		Vector3.Normalize(ref forward, out var result2);
		Vector3.Cross(ref forward, ref up, out var result3);
		Vector3.Cross(ref result3, ref forward, out var result4);
		result3.Normalize();
		result4.Normalize();
		result = default(Matrix);
		result.Right = result3;
		result.Up = result4;
		result.Forward = result2;
		result.Translation = position;
		result.M44 = 1f;
	}

	public static Matrix Divide(Matrix matrix1, Matrix matrix2)
	{
		matrix1.M11 /= matrix2.M11;
		matrix1.M12 /= matrix2.M12;
		matrix1.M13 /= matrix2.M13;
		matrix1.M14 /= matrix2.M14;
		matrix1.M21 /= matrix2.M21;
		matrix1.M22 /= matrix2.M22;
		matrix1.M23 /= matrix2.M23;
		matrix1.M24 /= matrix2.M24;
		matrix1.M31 /= matrix2.M31;
		matrix1.M32 /= matrix2.M32;
		matrix1.M33 /= matrix2.M33;
		matrix1.M34 /= matrix2.M34;
		matrix1.M41 /= matrix2.M41;
		matrix1.M42 /= matrix2.M42;
		matrix1.M43 /= matrix2.M43;
		matrix1.M44 /= matrix2.M44;
		return matrix1;
	}

	public static void Divide(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
	{
		result.M11 = matrix1.M11 / matrix2.M11;
		result.M12 = matrix1.M12 / matrix2.M12;
		result.M13 = matrix1.M13 / matrix2.M13;
		result.M14 = matrix1.M14 / matrix2.M14;
		result.M21 = matrix1.M21 / matrix2.M21;
		result.M22 = matrix1.M22 / matrix2.M22;
		result.M23 = matrix1.M23 / matrix2.M23;
		result.M24 = matrix1.M24 / matrix2.M24;
		result.M31 = matrix1.M31 / matrix2.M31;
		result.M32 = matrix1.M32 / matrix2.M32;
		result.M33 = matrix1.M33 / matrix2.M33;
		result.M34 = matrix1.M34 / matrix2.M34;
		result.M41 = matrix1.M41 / matrix2.M41;
		result.M42 = matrix1.M42 / matrix2.M42;
		result.M43 = matrix1.M43 / matrix2.M43;
		result.M44 = matrix1.M44 / matrix2.M44;
	}

	public static Matrix Divide(Matrix matrix1, float divider)
	{
		float num = 1f / divider;
		matrix1.M11 *= num;
		matrix1.M12 *= num;
		matrix1.M13 *= num;
		matrix1.M14 *= num;
		matrix1.M21 *= num;
		matrix1.M22 *= num;
		matrix1.M23 *= num;
		matrix1.M24 *= num;
		matrix1.M31 *= num;
		matrix1.M32 *= num;
		matrix1.M33 *= num;
		matrix1.M34 *= num;
		matrix1.M41 *= num;
		matrix1.M42 *= num;
		matrix1.M43 *= num;
		matrix1.M44 *= num;
		return matrix1;
	}

	public static void Divide(ref Matrix matrix1, float divider, out Matrix result)
	{
		float num = 1f / divider;
		result.M11 = matrix1.M11 * num;
		result.M12 = matrix1.M12 * num;
		result.M13 = matrix1.M13 * num;
		result.M14 = matrix1.M14 * num;
		result.M21 = matrix1.M21 * num;
		result.M22 = matrix1.M22 * num;
		result.M23 = matrix1.M23 * num;
		result.M24 = matrix1.M24 * num;
		result.M31 = matrix1.M31 * num;
		result.M32 = matrix1.M32 * num;
		result.M33 = matrix1.M33 * num;
		result.M34 = matrix1.M34 * num;
		result.M41 = matrix1.M41 * num;
		result.M42 = matrix1.M42 * num;
		result.M43 = matrix1.M43 * num;
		result.M44 = matrix1.M44 * num;
	}

	public static Matrix Invert(Matrix matrix)
	{
		Invert(ref matrix, out matrix);
		return matrix;
	}

	public static void Invert(ref Matrix matrix, out Matrix result)
	{
		float m = matrix.M11;
		float m2 = matrix.M12;
		float m3 = matrix.M13;
		float m4 = matrix.M14;
		float m5 = matrix.M21;
		float m6 = matrix.M22;
		float m7 = matrix.M23;
		float m8 = matrix.M24;
		float m9 = matrix.M31;
		float m10 = matrix.M32;
		float m11 = matrix.M33;
		float m12 = matrix.M34;
		float m13 = matrix.M41;
		float m14 = matrix.M42;
		float m15 = matrix.M43;
		float m16 = matrix.M44;
		float num = (float)((double)m11 * (double)m16 - (double)m12 * (double)m15);
		float num2 = (float)((double)m10 * (double)m16 - (double)m12 * (double)m14);
		float num3 = (float)((double)m10 * (double)m15 - (double)m11 * (double)m14);
		float num4 = (float)((double)m9 * (double)m16 - (double)m12 * (double)m13);
		float num5 = (float)((double)m9 * (double)m15 - (double)m11 * (double)m13);
		float num6 = (float)((double)m9 * (double)m14 - (double)m10 * (double)m13);
		float num7 = (float)((double)m6 * (double)num - (double)m7 * (double)num2 + (double)m8 * (double)num3);
		float num8 = (float)(0.0 - ((double)m5 * (double)num - (double)m7 * (double)num4 + (double)m8 * (double)num5));
		float num9 = (float)((double)m5 * (double)num2 - (double)m6 * (double)num4 + (double)m8 * (double)num6);
		float num10 = (float)(0.0 - ((double)m5 * (double)num3 - (double)m6 * (double)num5 + (double)m7 * (double)num6));
		float num11 = (float)(1.0 / ((double)m * (double)num7 + (double)m2 * (double)num8 + (double)m3 * (double)num9 + (double)m4 * (double)num10));
		result.M11 = num7 * num11;
		result.M21 = num8 * num11;
		result.M31 = num9 * num11;
		result.M41 = num10 * num11;
		result.M12 = (float)((0.0 - ((double)m2 * (double)num - (double)m3 * (double)num2 + (double)m4 * (double)num3)) * (double)num11);
		result.M22 = (float)(((double)m * (double)num - (double)m3 * (double)num4 + (double)m4 * (double)num5) * (double)num11);
		result.M32 = (float)((0.0 - ((double)m * (double)num2 - (double)m2 * (double)num4 + (double)m4 * (double)num6)) * (double)num11);
		result.M42 = (float)(((double)m * (double)num3 - (double)m2 * (double)num5 + (double)m3 * (double)num6) * (double)num11);
		float num12 = (float)((double)m7 * (double)m16 - (double)m8 * (double)m15);
		float num13 = (float)((double)m6 * (double)m16 - (double)m8 * (double)m14);
		float num14 = (float)((double)m6 * (double)m15 - (double)m7 * (double)m14);
		float num15 = (float)((double)m5 * (double)m16 - (double)m8 * (double)m13);
		float num16 = (float)((double)m5 * (double)m15 - (double)m7 * (double)m13);
		float num17 = (float)((double)m5 * (double)m14 - (double)m6 * (double)m13);
		result.M13 = (float)(((double)m2 * (double)num12 - (double)m3 * (double)num13 + (double)m4 * (double)num14) * (double)num11);
		result.M23 = (float)((0.0 - ((double)m * (double)num12 - (double)m3 * (double)num15 + (double)m4 * (double)num16)) * (double)num11);
		result.M33 = (float)(((double)m * (double)num13 - (double)m2 * (double)num15 + (double)m4 * (double)num17) * (double)num11);
		result.M43 = (float)((0.0 - ((double)m * (double)num14 - (double)m2 * (double)num16 + (double)m3 * (double)num17)) * (double)num11);
		float num18 = (float)((double)m7 * (double)m12 - (double)m8 * (double)m11);
		float num19 = (float)((double)m6 * (double)m12 - (double)m8 * (double)m10);
		float num20 = (float)((double)m6 * (double)m11 - (double)m7 * (double)m10);
		float num21 = (float)((double)m5 * (double)m12 - (double)m8 * (double)m9);
		float num22 = (float)((double)m5 * (double)m11 - (double)m7 * (double)m9);
		float num23 = (float)((double)m5 * (double)m10 - (double)m6 * (double)m9);
		result.M14 = (float)((0.0 - ((double)m2 * (double)num18 - (double)m3 * (double)num19 + (double)m4 * (double)num20)) * (double)num11);
		result.M24 = (float)(((double)m * (double)num18 - (double)m3 * (double)num21 + (double)m4 * (double)num22) * (double)num11);
		result.M34 = (float)((0.0 - ((double)m * (double)num19 - (double)m2 * (double)num21 + (double)m4 * (double)num23)) * (double)num11);
		result.M44 = (float)(((double)m * (double)num20 - (double)m2 * (double)num22 + (double)m3 * (double)num23) * (double)num11);
	}

	public static Matrix Lerp(Matrix matrix1, Matrix matrix2, float amount)
	{
		matrix1.M11 += (matrix2.M11 - matrix1.M11) * amount;
		matrix1.M12 += (matrix2.M12 - matrix1.M12) * amount;
		matrix1.M13 += (matrix2.M13 - matrix1.M13) * amount;
		matrix1.M14 += (matrix2.M14 - matrix1.M14) * amount;
		matrix1.M21 += (matrix2.M21 - matrix1.M21) * amount;
		matrix1.M22 += (matrix2.M22 - matrix1.M22) * amount;
		matrix1.M23 += (matrix2.M23 - matrix1.M23) * amount;
		matrix1.M24 += (matrix2.M24 - matrix1.M24) * amount;
		matrix1.M31 += (matrix2.M31 - matrix1.M31) * amount;
		matrix1.M32 += (matrix2.M32 - matrix1.M32) * amount;
		matrix1.M33 += (matrix2.M33 - matrix1.M33) * amount;
		matrix1.M34 += (matrix2.M34 - matrix1.M34) * amount;
		matrix1.M41 += (matrix2.M41 - matrix1.M41) * amount;
		matrix1.M42 += (matrix2.M42 - matrix1.M42) * amount;
		matrix1.M43 += (matrix2.M43 - matrix1.M43) * amount;
		matrix1.M44 += (matrix2.M44 - matrix1.M44) * amount;
		return matrix1;
	}

	public static void Lerp(ref Matrix matrix1, ref Matrix matrix2, float amount, out Matrix result)
	{
		result.M11 = matrix1.M11 + (matrix2.M11 - matrix1.M11) * amount;
		result.M12 = matrix1.M12 + (matrix2.M12 - matrix1.M12) * amount;
		result.M13 = matrix1.M13 + (matrix2.M13 - matrix1.M13) * amount;
		result.M14 = matrix1.M14 + (matrix2.M14 - matrix1.M14) * amount;
		result.M21 = matrix1.M21 + (matrix2.M21 - matrix1.M21) * amount;
		result.M22 = matrix1.M22 + (matrix2.M22 - matrix1.M22) * amount;
		result.M23 = matrix1.M23 + (matrix2.M23 - matrix1.M23) * amount;
		result.M24 = matrix1.M24 + (matrix2.M24 - matrix1.M24) * amount;
		result.M31 = matrix1.M31 + (matrix2.M31 - matrix1.M31) * amount;
		result.M32 = matrix1.M32 + (matrix2.M32 - matrix1.M32) * amount;
		result.M33 = matrix1.M33 + (matrix2.M33 - matrix1.M33) * amount;
		result.M34 = matrix1.M34 + (matrix2.M34 - matrix1.M34) * amount;
		result.M41 = matrix1.M41 + (matrix2.M41 - matrix1.M41) * amount;
		result.M42 = matrix1.M42 + (matrix2.M42 - matrix1.M42) * amount;
		result.M43 = matrix1.M43 + (matrix2.M43 - matrix1.M43) * amount;
		result.M44 = matrix1.M44 + (matrix2.M44 - matrix1.M44) * amount;
	}

	public static Matrix Multiply(Matrix matrix1, Matrix matrix2)
	{
		float m = matrix1.M11 * matrix2.M11 + matrix1.M12 * matrix2.M21 + matrix1.M13 * matrix2.M31 + matrix1.M14 * matrix2.M41;
		float m2 = matrix1.M11 * matrix2.M12 + matrix1.M12 * matrix2.M22 + matrix1.M13 * matrix2.M32 + matrix1.M14 * matrix2.M42;
		float m3 = matrix1.M11 * matrix2.M13 + matrix1.M12 * matrix2.M23 + matrix1.M13 * matrix2.M33 + matrix1.M14 * matrix2.M43;
		float m4 = matrix1.M11 * matrix2.M14 + matrix1.M12 * matrix2.M24 + matrix1.M13 * matrix2.M34 + matrix1.M14 * matrix2.M44;
		float m5 = matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M21 + matrix1.M23 * matrix2.M31 + matrix1.M24 * matrix2.M41;
		float m6 = matrix1.M21 * matrix2.M12 + matrix1.M22 * matrix2.M22 + matrix1.M23 * matrix2.M32 + matrix1.M24 * matrix2.M42;
		float m7 = matrix1.M21 * matrix2.M13 + matrix1.M22 * matrix2.M23 + matrix1.M23 * matrix2.M33 + matrix1.M24 * matrix2.M43;
		float m8 = matrix1.M21 * matrix2.M14 + matrix1.M22 * matrix2.M24 + matrix1.M23 * matrix2.M34 + matrix1.M24 * matrix2.M44;
		float m9 = matrix1.M31 * matrix2.M11 + matrix1.M32 * matrix2.M21 + matrix1.M33 * matrix2.M31 + matrix1.M34 * matrix2.M41;
		float m10 = matrix1.M31 * matrix2.M12 + matrix1.M32 * matrix2.M22 + matrix1.M33 * matrix2.M32 + matrix1.M34 * matrix2.M42;
		float m11 = matrix1.M31 * matrix2.M13 + matrix1.M32 * matrix2.M23 + matrix1.M33 * matrix2.M33 + matrix1.M34 * matrix2.M43;
		float m12 = matrix1.M31 * matrix2.M14 + matrix1.M32 * matrix2.M24 + matrix1.M33 * matrix2.M34 + matrix1.M34 * matrix2.M44;
		float m13 = matrix1.M41 * matrix2.M11 + matrix1.M42 * matrix2.M21 + matrix1.M43 * matrix2.M31 + matrix1.M44 * matrix2.M41;
		float m14 = matrix1.M41 * matrix2.M12 + matrix1.M42 * matrix2.M22 + matrix1.M43 * matrix2.M32 + matrix1.M44 * matrix2.M42;
		float m15 = matrix1.M41 * matrix2.M13 + matrix1.M42 * matrix2.M23 + matrix1.M43 * matrix2.M33 + matrix1.M44 * matrix2.M43;
		float m16 = matrix1.M41 * matrix2.M14 + matrix1.M42 * matrix2.M24 + matrix1.M43 * matrix2.M34 + matrix1.M44 * matrix2.M44;
		matrix1.M11 = m;
		matrix1.M12 = m2;
		matrix1.M13 = m3;
		matrix1.M14 = m4;
		matrix1.M21 = m5;
		matrix1.M22 = m6;
		matrix1.M23 = m7;
		matrix1.M24 = m8;
		matrix1.M31 = m9;
		matrix1.M32 = m10;
		matrix1.M33 = m11;
		matrix1.M34 = m12;
		matrix1.M41 = m13;
		matrix1.M42 = m14;
		matrix1.M43 = m15;
		matrix1.M44 = m16;
		return matrix1;
	}

	public static void Multiply(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
	{
		float m = matrix1.M11 * matrix2.M11 + matrix1.M12 * matrix2.M21 + matrix1.M13 * matrix2.M31 + matrix1.M14 * matrix2.M41;
		float m2 = matrix1.M11 * matrix2.M12 + matrix1.M12 * matrix2.M22 + matrix1.M13 * matrix2.M32 + matrix1.M14 * matrix2.M42;
		float m3 = matrix1.M11 * matrix2.M13 + matrix1.M12 * matrix2.M23 + matrix1.M13 * matrix2.M33 + matrix1.M14 * matrix2.M43;
		float m4 = matrix1.M11 * matrix2.M14 + matrix1.M12 * matrix2.M24 + matrix1.M13 * matrix2.M34 + matrix1.M14 * matrix2.M44;
		float m5 = matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M21 + matrix1.M23 * matrix2.M31 + matrix1.M24 * matrix2.M41;
		float m6 = matrix1.M21 * matrix2.M12 + matrix1.M22 * matrix2.M22 + matrix1.M23 * matrix2.M32 + matrix1.M24 * matrix2.M42;
		float m7 = matrix1.M21 * matrix2.M13 + matrix1.M22 * matrix2.M23 + matrix1.M23 * matrix2.M33 + matrix1.M24 * matrix2.M43;
		float m8 = matrix1.M21 * matrix2.M14 + matrix1.M22 * matrix2.M24 + matrix1.M23 * matrix2.M34 + matrix1.M24 * matrix2.M44;
		float m9 = matrix1.M31 * matrix2.M11 + matrix1.M32 * matrix2.M21 + matrix1.M33 * matrix2.M31 + matrix1.M34 * matrix2.M41;
		float m10 = matrix1.M31 * matrix2.M12 + matrix1.M32 * matrix2.M22 + matrix1.M33 * matrix2.M32 + matrix1.M34 * matrix2.M42;
		float m11 = matrix1.M31 * matrix2.M13 + matrix1.M32 * matrix2.M23 + matrix1.M33 * matrix2.M33 + matrix1.M34 * matrix2.M43;
		float m12 = matrix1.M31 * matrix2.M14 + matrix1.M32 * matrix2.M24 + matrix1.M33 * matrix2.M34 + matrix1.M34 * matrix2.M44;
		float m13 = matrix1.M41 * matrix2.M11 + matrix1.M42 * matrix2.M21 + matrix1.M43 * matrix2.M31 + matrix1.M44 * matrix2.M41;
		float m14 = matrix1.M41 * matrix2.M12 + matrix1.M42 * matrix2.M22 + matrix1.M43 * matrix2.M32 + matrix1.M44 * matrix2.M42;
		float m15 = matrix1.M41 * matrix2.M13 + matrix1.M42 * matrix2.M23 + matrix1.M43 * matrix2.M33 + matrix1.M44 * matrix2.M43;
		float m16 = matrix1.M41 * matrix2.M14 + matrix1.M42 * matrix2.M24 + matrix1.M43 * matrix2.M34 + matrix1.M44 * matrix2.M44;
		result.M11 = m;
		result.M12 = m2;
		result.M13 = m3;
		result.M14 = m4;
		result.M21 = m5;
		result.M22 = m6;
		result.M23 = m7;
		result.M24 = m8;
		result.M31 = m9;
		result.M32 = m10;
		result.M33 = m11;
		result.M34 = m12;
		result.M41 = m13;
		result.M42 = m14;
		result.M43 = m15;
		result.M44 = m16;
	}

	public static Matrix Multiply(Matrix matrix1, float scaleFactor)
	{
		matrix1.M11 *= scaleFactor;
		matrix1.M12 *= scaleFactor;
		matrix1.M13 *= scaleFactor;
		matrix1.M14 *= scaleFactor;
		matrix1.M21 *= scaleFactor;
		matrix1.M22 *= scaleFactor;
		matrix1.M23 *= scaleFactor;
		matrix1.M24 *= scaleFactor;
		matrix1.M31 *= scaleFactor;
		matrix1.M32 *= scaleFactor;
		matrix1.M33 *= scaleFactor;
		matrix1.M34 *= scaleFactor;
		matrix1.M41 *= scaleFactor;
		matrix1.M42 *= scaleFactor;
		matrix1.M43 *= scaleFactor;
		matrix1.M44 *= scaleFactor;
		return matrix1;
	}

	public static void Multiply(ref Matrix matrix1, float scaleFactor, out Matrix result)
	{
		result.M11 = matrix1.M11 * scaleFactor;
		result.M12 = matrix1.M12 * scaleFactor;
		result.M13 = matrix1.M13 * scaleFactor;
		result.M14 = matrix1.M14 * scaleFactor;
		result.M21 = matrix1.M21 * scaleFactor;
		result.M22 = matrix1.M22 * scaleFactor;
		result.M23 = matrix1.M23 * scaleFactor;
		result.M24 = matrix1.M24 * scaleFactor;
		result.M31 = matrix1.M31 * scaleFactor;
		result.M32 = matrix1.M32 * scaleFactor;
		result.M33 = matrix1.M33 * scaleFactor;
		result.M34 = matrix1.M34 * scaleFactor;
		result.M41 = matrix1.M41 * scaleFactor;
		result.M42 = matrix1.M42 * scaleFactor;
		result.M43 = matrix1.M43 * scaleFactor;
		result.M44 = matrix1.M44 * scaleFactor;
	}

	public static Matrix Negate(Matrix matrix)
	{
		matrix.M11 = 0f - matrix.M11;
		matrix.M12 = 0f - matrix.M12;
		matrix.M13 = 0f - matrix.M13;
		matrix.M14 = 0f - matrix.M14;
		matrix.M21 = 0f - matrix.M21;
		matrix.M22 = 0f - matrix.M22;
		matrix.M23 = 0f - matrix.M23;
		matrix.M24 = 0f - matrix.M24;
		matrix.M31 = 0f - matrix.M31;
		matrix.M32 = 0f - matrix.M32;
		matrix.M33 = 0f - matrix.M33;
		matrix.M34 = 0f - matrix.M34;
		matrix.M41 = 0f - matrix.M41;
		matrix.M42 = 0f - matrix.M42;
		matrix.M43 = 0f - matrix.M43;
		matrix.M44 = 0f - matrix.M44;
		return matrix;
	}

	public static void Negate(ref Matrix matrix, out Matrix result)
	{
		result.M11 = 0f - matrix.M11;
		result.M12 = 0f - matrix.M12;
		result.M13 = 0f - matrix.M13;
		result.M14 = 0f - matrix.M14;
		result.M21 = 0f - matrix.M21;
		result.M22 = 0f - matrix.M22;
		result.M23 = 0f - matrix.M23;
		result.M24 = 0f - matrix.M24;
		result.M31 = 0f - matrix.M31;
		result.M32 = 0f - matrix.M32;
		result.M33 = 0f - matrix.M33;
		result.M34 = 0f - matrix.M34;
		result.M41 = 0f - matrix.M41;
		result.M42 = 0f - matrix.M42;
		result.M43 = 0f - matrix.M43;
		result.M44 = 0f - matrix.M44;
	}

	public static Matrix Subtract(Matrix matrix1, Matrix matrix2)
	{
		matrix1.M11 -= matrix2.M11;
		matrix1.M12 -= matrix2.M12;
		matrix1.M13 -= matrix2.M13;
		matrix1.M14 -= matrix2.M14;
		matrix1.M21 -= matrix2.M21;
		matrix1.M22 -= matrix2.M22;
		matrix1.M23 -= matrix2.M23;
		matrix1.M24 -= matrix2.M24;
		matrix1.M31 -= matrix2.M31;
		matrix1.M32 -= matrix2.M32;
		matrix1.M33 -= matrix2.M33;
		matrix1.M34 -= matrix2.M34;
		matrix1.M41 -= matrix2.M41;
		matrix1.M42 -= matrix2.M42;
		matrix1.M43 -= matrix2.M43;
		matrix1.M44 -= matrix2.M44;
		return matrix1;
	}

	public static void Subtract(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
	{
		result.M11 = matrix1.M11 - matrix2.M11;
		result.M12 = matrix1.M12 - matrix2.M12;
		result.M13 = matrix1.M13 - matrix2.M13;
		result.M14 = matrix1.M14 - matrix2.M14;
		result.M21 = matrix1.M21 - matrix2.M21;
		result.M22 = matrix1.M22 - matrix2.M22;
		result.M23 = matrix1.M23 - matrix2.M23;
		result.M24 = matrix1.M24 - matrix2.M24;
		result.M31 = matrix1.M31 - matrix2.M31;
		result.M32 = matrix1.M32 - matrix2.M32;
		result.M33 = matrix1.M33 - matrix2.M33;
		result.M34 = matrix1.M34 - matrix2.M34;
		result.M41 = matrix1.M41 - matrix2.M41;
		result.M42 = matrix1.M42 - matrix2.M42;
		result.M43 = matrix1.M43 - matrix2.M43;
		result.M44 = matrix1.M44 - matrix2.M44;
	}

	public static Matrix Transpose(Matrix matrix)
	{
		Transpose(ref matrix, out var result);
		return result;
	}

	public static void Transpose(ref Matrix matrix, out Matrix result)
	{
		Matrix matrix2 = default(Matrix);
		matrix2.M11 = matrix.M11;
		matrix2.M12 = matrix.M21;
		matrix2.M13 = matrix.M31;
		matrix2.M14 = matrix.M41;
		matrix2.M21 = matrix.M12;
		matrix2.M22 = matrix.M22;
		matrix2.M23 = matrix.M32;
		matrix2.M24 = matrix.M42;
		matrix2.M31 = matrix.M13;
		matrix2.M32 = matrix.M23;
		matrix2.M33 = matrix.M33;
		matrix2.M34 = matrix.M43;
		matrix2.M41 = matrix.M14;
		matrix2.M42 = matrix.M24;
		matrix2.M43 = matrix.M34;
		matrix2.M44 = matrix.M44;
		result = matrix2;
	}

	public static Matrix Transform(Matrix value, Quaternion rotation)
	{
		Transform(ref value, ref rotation, out var result);
		return result;
	}

	public static void Transform(ref Matrix value, ref Quaternion rotation, out Matrix result)
	{
		Matrix matrix = CreateFromQuaternion(rotation);
		Multiply(ref value, ref matrix, out result);
	}

	public static Matrix operator +(Matrix matrix1, Matrix matrix2)
	{
		return Add(matrix1, matrix2);
	}

	public static Matrix operator /(Matrix matrix1, Matrix matrix2)
	{
		return Divide(matrix1, matrix2);
	}

	public static Matrix operator /(Matrix matrix, float divider)
	{
		return Divide(matrix, divider);
	}

	public static bool operator ==(Matrix matrix1, Matrix matrix2)
	{
		return matrix1.Equals(matrix2);
	}

	public static bool operator !=(Matrix matrix1, Matrix matrix2)
	{
		return !matrix1.Equals(matrix2);
	}

	public static Matrix operator *(Matrix matrix1, Matrix matrix2)
	{
		return Multiply(matrix1, matrix2);
	}

	public static Matrix operator *(Matrix matrix, float scaleFactor)
	{
		return Multiply(matrix, scaleFactor);
	}

	public static Matrix operator -(Matrix matrix1, Matrix matrix2)
	{
		return Subtract(matrix1, matrix2);
	}

	public static Matrix operator -(Matrix matrix)
	{
		return Negate(matrix);
	}

	public Matrix(float[] data)
	{
		if (data.Length != 16)
		{
			throw new ArgumentException();
		}
		M11 = data[0];
		M12 = data[1];
		M13 = data[2];
		M14 = data[3];
		M21 = data[4];
		M22 = data[5];
		M23 = data[6];
		M24 = data[7];
		M31 = data[8];
		M32 = data[9];
		M33 = data[10];
		M34 = data[11];
		M41 = data[12];
		M42 = data[13];
		M43 = data[14];
		M44 = data[15];
	}

	public static void Copy(ref Matrix source, out Matrix result)
	{
		result.M11 = source.M11;
		result.M12 = source.M12;
		result.M13 = source.M13;
		result.M14 = source.M14;
		result.M21 = source.M21;
		result.M22 = source.M22;
		result.M23 = source.M23;
		result.M24 = source.M24;
		result.M31 = source.M31;
		result.M32 = source.M32;
		result.M33 = source.M33;
		result.M34 = source.M34;
		result.M41 = source.M41;
		result.M42 = source.M42;
		result.M43 = source.M43;
		result.M44 = source.M44;
	}

	public static void CreatePerspectiveFieldOfViewReverseZ(float fieldOfView, float aspectRatio, float nearPlaneDistance, out Matrix result)
	{
		if (fieldOfView <= 0f || fieldOfView >= 3.141593f)
		{
			throw new ArgumentException("fieldOfView <= 0 or >= PI");
		}
		if (nearPlaneDistance <= 0f)
		{
			throw new ArgumentException("nearPlaneDistance <= 0");
		}
		float num = 1f / (float)System.Math.Tan(fieldOfView * 0.5f);
		float m = num / aspectRatio;
		result.M11 = m;
		result.M12 = (result.M13 = (result.M14 = 0f));
		result.M22 = num;
		result.M21 = (result.M23 = (result.M24 = 0f));
		result.M31 = (result.M32 = (result.M33 = (result.M41 = (result.M42 = (result.M44 = 0f)))));
		result.M34 = -1f;
		result.M43 = 2f * nearPlaneDistance;
	}

	public static Matrix CreateProjectionFrustum(float left, float right, float bottom, float top, float near, float far)
	{
		float num = 1f / (right + left);
		float num2 = 1f / (top + bottom);
		float num3 = 1f / (near - far);
		Matrix result = default(Matrix);
		result.M34 = -1f;
		result.M11 = 2f * (near * num);
		result.M22 = 2f * (near * num2);
		result.M43 = 2f * (far * near * num3);
		result.M31 = 2f * (right - left) * num;
		result.M32 = (top - bottom) * num2;
		result.M33 = (far + near) * num3;
		return result;
	}

	public static Matrix CreateProjectionOrtho(float left, float right, float bottom, float top, float near, float far)
	{
		float num = 1f / (right + left);
		float num2 = 1f / (top + bottom);
		float num3 = -1f / (far - near);
		float m = 2f * num;
		float m2 = 2f * num2;
		float m3 = 2f * num3;
		Matrix result = default(Matrix);
		result.M44 = 1f;
		result.M11 = m;
		result.M22 = m2;
		result.M33 = m3;
		result.M41 = (0f - (right - left)) * num;
		result.M42 = (0f - (top - bottom)) * num2;
		result.M43 = (far + near) * num3;
		return result;
	}

	public static Matrix CreateViewDirection(float eyeX, float eyeY, float eyeZ, float dirX, float dirY, float dirZ, float upX, float upY, float upZ)
	{
		float num = 1f / (float)System.Math.Sqrt(dirX * dirX + dirY * dirY + dirZ * dirZ);
		dirX *= num;
		dirY *= num;
		dirZ *= num;
		float num2 = dirY * upZ - dirZ * upY;
		float num3 = dirZ * upX - dirX * upZ;
		float num4 = dirX * upY - dirY * upX;
		float num5 = 1f / (float)System.Math.Sqrt(num2 * num2 + num3 * num3 + num4 * num4);
		num2 *= num5;
		num3 *= num5;
		num4 *= num5;
		float m = num3 * dirZ - num4 * dirY;
		float m2 = num4 * dirX - num2 * dirZ;
		float m3 = num2 * dirY - num3 * dirX;
		Matrix matrix = default(Matrix);
		matrix.M11 = num2;
		matrix.M12 = m;
		matrix.M13 = 0f - dirX;
		matrix.M14 = 0f;
		matrix.M21 = num3;
		matrix.M22 = m2;
		matrix.M23 = 0f - dirY;
		matrix.M24 = 0f;
		matrix.M31 = num4;
		matrix.M32 = m3;
		matrix.M33 = 0f - dirZ;
		matrix.M34 = 0f;
		matrix.M41 = 0f;
		matrix.M42 = 0f;
		matrix.M43 = 0f;
		matrix.M44 = 1f;
		Matrix matrix2 = matrix;
		return CreateTranslation(0f - eyeX, 0f - eyeY, 0f - eyeZ) * matrix2;
	}

	public static void AddTranslation(ref Matrix matrix, float x, float y, float z)
	{
		matrix.M41 += x;
		matrix.M42 += y;
		matrix.M43 += z;
	}

	public static void ApplyScale(ref Matrix matrix, Vector3 scale)
	{
		matrix.M11 *= scale.X;
		matrix.M12 *= scale.X;
		matrix.M13 *= scale.X;
		matrix.M14 *= scale.X;
		matrix.M21 *= scale.Y;
		matrix.M22 *= scale.Y;
		matrix.M23 *= scale.Y;
		matrix.M24 *= scale.Y;
		matrix.M31 *= scale.Z;
		matrix.M32 *= scale.Z;
		matrix.M33 *= scale.Z;
		matrix.M34 *= scale.Z;
	}

	public static void ApplyScale(ref Matrix matrix, float scale)
	{
		matrix.M11 *= scale;
		matrix.M12 *= scale;
		matrix.M13 *= scale;
		matrix.M14 *= scale;
		matrix.M21 *= scale;
		matrix.M22 *= scale;
		matrix.M23 *= scale;
		matrix.M24 *= scale;
		matrix.M31 *= scale;
		matrix.M32 *= scale;
		matrix.M33 *= scale;
		matrix.M34 *= scale;
	}

	public static Matrix Compose(float scale, Quaternion quaternion, Vector3 translation)
	{
		Compose(scale, quaternion, translation, out var result);
		return result;
	}

	public static void Compose(float scale, Quaternion quaternion, Vector3 translation, out Matrix result)
	{
		float num = quaternion.X * quaternion.X;
		float num2 = quaternion.Y * quaternion.Y;
		float num3 = quaternion.Z * quaternion.Z;
		float num4 = quaternion.X * quaternion.Y;
		float num5 = quaternion.Z * quaternion.W;
		float num6 = quaternion.Z * quaternion.X;
		float num7 = quaternion.Y * quaternion.W;
		float num8 = quaternion.Y * quaternion.Z;
		float num9 = quaternion.X * quaternion.W;
		result.M11 = scale * (1f - 2f * (num2 + num3));
		result.M12 = scale * 2f * (num4 + num5);
		result.M13 = scale * 2f * (num6 - num7);
		result.M14 = 0f;
		result.M21 = scale * 2f * (num4 - num5);
		result.M22 = scale * (1f - 2f * (num3 + num));
		result.M23 = scale * 2f * (num8 + num9);
		result.M24 = 0f;
		result.M31 = scale * 2f * (num6 + num7);
		result.M32 = scale * 2f * (num8 - num9);
		result.M33 = scale * (1f - 2f * (num2 + num));
		result.M34 = 0f;
		result.M41 = translation.X;
		result.M42 = translation.Y;
		result.M43 = translation.Z;
		result.M44 = 1f;
	}

	public static void Compose(Quaternion quaternion, Vector3 translation, out Matrix result)
	{
		float num = quaternion.X * quaternion.X;
		float num2 = quaternion.Y * quaternion.Y;
		float num3 = quaternion.Z * quaternion.Z;
		float num4 = quaternion.X * quaternion.Y;
		float num5 = quaternion.Z * quaternion.W;
		float num6 = quaternion.Z * quaternion.X;
		float num7 = quaternion.Y * quaternion.W;
		float num8 = quaternion.Y * quaternion.Z;
		float num9 = quaternion.X * quaternion.W;
		result.M11 = 1f - 2f * (num2 + num3);
		result.M12 = 2f * (num4 + num5);
		result.M13 = 2f * (num6 - num7);
		result.M14 = 0f;
		result.M21 = 2f * (num4 - num5);
		result.M22 = 1f - 2f * (num3 + num);
		result.M23 = 2f * (num8 + num9);
		result.M24 = 0f;
		result.M31 = 2f * (num6 + num7);
		result.M32 = 2f * (num8 - num9);
		result.M33 = 1f - 2f * (num2 + num);
		result.M34 = 0f;
		result.M41 = translation.X;
		result.M42 = translation.Y;
		result.M43 = translation.Z;
		result.M44 = 1f;
	}

	public static void Compose(float scaleX, float scaleY, float scaleZ, Quaternion quaternion, Vector3 translation, out Matrix result)
	{
		float num = quaternion.X * quaternion.X;
		float num2 = quaternion.Y * quaternion.Y;
		float num3 = quaternion.Z * quaternion.Z;
		float num4 = quaternion.X * quaternion.Y;
		float num5 = quaternion.Z * quaternion.W;
		float num6 = quaternion.Z * quaternion.X;
		float num7 = quaternion.Y * quaternion.W;
		float num8 = quaternion.Y * quaternion.Z;
		float num9 = quaternion.X * quaternion.W;
		result.M11 = scaleX * (1f - 2f * (num2 + num3));
		result.M12 = scaleX * 2f * (num4 + num5);
		result.M13 = scaleX * 2f * (num6 - num7);
		result.M14 = 0f;
		result.M21 = scaleY * 2f * (num4 - num5);
		result.M22 = scaleY * (1f - 2f * (num3 + num));
		result.M23 = scaleY * 2f * (num8 + num9);
		result.M24 = 0f;
		result.M31 = scaleZ * 2f * (num6 + num7);
		result.M32 = scaleZ * 2f * (num8 - num9);
		result.M33 = scaleZ * (1f - 2f * (num2 + num));
		result.M34 = 0f;
		result.M41 = translation.X;
		result.M42 = translation.Y;
		result.M43 = translation.Z;
		result.M44 = 1f;
	}

	public static void Compose(Vector3 scale, Quaternion quaternion, Vector3 translation, out Matrix result)
	{
		float num = quaternion.X * quaternion.X;
		float num2 = quaternion.Y * quaternion.Y;
		float num3 = quaternion.Z * quaternion.Z;
		float num4 = quaternion.X * quaternion.Y;
		float num5 = quaternion.Z * quaternion.W;
		float num6 = quaternion.Z * quaternion.X;
		float num7 = quaternion.Y * quaternion.W;
		float num8 = quaternion.Y * quaternion.Z;
		float num9 = quaternion.X * quaternion.W;
		result.M11 = scale.X * (1f - 2f * (num2 + num3));
		result.M12 = scale.X * 2f * (num4 + num5);
		result.M13 = scale.X * 2f * (num6 - num7);
		result.M14 = 0f;
		result.M21 = scale.Y * 2f * (num4 - num5);
		result.M22 = scale.Y * (1f - 2f * (num3 + num));
		result.M23 = scale.Y * 2f * (num8 + num9);
		result.M24 = 0f;
		result.M31 = scale.Z * 2f * (num6 + num7);
		result.M32 = scale.Z * 2f * (num8 - num9);
		result.M33 = scale.Z * (1f - 2f * (num2 + num));
		result.M34 = 0f;
		result.M41 = translation.X;
		result.M42 = translation.Y;
		result.M43 = translation.Z;
		result.M44 = 1f;
	}

	public static void Compose(float scale, float yaw, Vector3 translation, out Matrix result)
	{
		float num = (float)System.Math.Cos(yaw);
		float num2 = (float)System.Math.Sin(yaw);
		result.M11 = scale * num;
		result.M12 = 0f;
		result.M13 = scale * (0f - num2);
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = scale;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = scale * num2;
		result.M32 = 0f;
		result.M33 = scale * num;
		result.M34 = 0f;
		result.M41 = translation.X;
		result.M42 = translation.Y;
		result.M43 = translation.Z;
		result.M44 = 1f;
	}

	public static void Compose(Vector3 scale, float yaw, Vector3 translation, out Matrix result)
	{
		float num = (float)System.Math.Cos(yaw);
		float num2 = (float)System.Math.Sin(yaw);
		result.M11 = scale.X * num;
		result.M12 = 0f;
		result.M13 = scale.X * (0f - num2);
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = scale.Y;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = scale.Z * num2;
		result.M32 = 0f;
		result.M33 = scale.Z * num;
		result.M34 = 0f;
		result.M41 = translation.X;
		result.M42 = translation.Y;
		result.M43 = translation.Z;
		result.M44 = 1f;
	}

	public static float[] ToFlatFloatArray(Matrix matrix)
	{
		return new float[16]
		{
			matrix.M11, matrix.M12, matrix.M13, matrix.M14, matrix.M21, matrix.M22, matrix.M23, matrix.M24, matrix.M31, matrix.M32,
			matrix.M33, matrix.M34, matrix.M41, matrix.M42, matrix.M43, matrix.M44
		};
	}

	public static void CreateLookDirection(Vector3 direction, out Matrix result)
	{
		Vector3 vector = new Vector3(0f, 1f, 0f);
		Vector3 vector2 = Vector3.Normalize(direction);
		Vector3 vector3 = Vector3.Normalize(Vector3.Cross(vector, vector2));
		Vector3 vector4 = Vector3.Cross(vector2, vector3);
		result.M11 = vector3.X;
		result.M12 = vector4.X;
		result.M13 = vector2.X;
		result.M14 = 0f;
		result.M21 = vector3.Y;
		result.M22 = vector4.Y;
		result.M23 = vector2.Y;
		result.M24 = 0f;
		result.M31 = vector3.Z;
		result.M32 = vector4.Z;
		result.M33 = vector2.Z;
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
	}
}
