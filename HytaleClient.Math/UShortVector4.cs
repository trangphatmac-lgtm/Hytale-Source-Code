namespace HytaleClient.Math;

public struct UShortVector4
{
	public ushort X;

	public ushort Y;

	public ushort Z;

	public ushort W;

	private static UShortVector4 _zero;

	public static UShortVector4 Zero => _zero;

	public UShortVector4(ushort x, ushort y, ushort z, ushort w)
	{
		X = x;
		Y = y;
		Z = z;
		W = w;
	}
}
