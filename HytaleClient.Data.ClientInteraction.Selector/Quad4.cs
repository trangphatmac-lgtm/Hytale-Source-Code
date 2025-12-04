using System;
using HytaleClient.Math;

namespace HytaleClient.Data.ClientInteraction.Selector;

public struct Quad4
{
	public Vector4 A;

	public Vector4 B;

	public Vector4 C;

	public Vector4 D;

	public Quad4(Vector4[] points, int a, int b, int c, int d)
	{
		A = points[a];
		B = points[b];
		C = points[c];
		D = points[d];
	}

	public Quad4 Multiply(Matrix matrix)
	{
		A = Vector4.Transform(A, matrix);
		B = Vector4.Transform(B, matrix);
		C = Vector4.Transform(C, matrix);
		D = Vector4.Transform(D, matrix);
		return this;
	}

	public bool IsFullyInsideFrustum()
	{
		return A.IsInsideFrustum() && B.IsInsideFrustum() && C.IsInsideFrustum() && D.IsInsideFrustum();
	}

	public Vector4 GetRandom(Random random)
	{
		float num = random.NextFloat(0f, 1f);
		float num2 = random.NextFloat(0f, 1f) * (1f - num);
		float num3 = 1f - num - num2;
		if (random.NextDouble() < 0.5)
		{
			return new Vector4(A.X * num3 + B.X * num + C.X * num2, A.Y * num3 + B.Y * num + C.Y * num2, A.Z * num3 + B.Z * num + C.Z * num2, A.W * num3 + B.W * num + C.W * num2);
		}
		return new Vector4(A.X * num3 + C.X * num + D.X * num2, A.Y * num3 + C.Y * num + D.Y * num2, A.Z * num3 + C.Z * num + D.Z * num2, A.W * num3 + C.W * num + D.W * num2);
	}
}
