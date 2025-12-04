using System;
using HytaleClient.Math;

namespace HytaleClient.Data.ClientInteraction.Selector;

internal struct Triangle4
{
	public Vector4 A;

	public Vector4 B;

	public Vector4 C;

	public Triangle4(Vector4 a, Vector4 b, Vector4 c)
	{
		A = a;
		B = b;
		C = c;
	}

	public Vector4 GetRandom(Random random)
	{
		float num = random.NextFloat(0f, 1f);
		float num2 = random.NextFloat(0f, 1f);
		float num3 = 1f - num - num2;
		return new Vector4(A.X * num3 + B.X * num + C.X * num2, A.Y * num3 + B.Y * num + C.Y * num2, A.Z * num3 + B.Z * num + C.Z * num2, A.W * num3 + B.W * num + C.W * num2);
	}
}
