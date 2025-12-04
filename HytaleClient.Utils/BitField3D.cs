using System;
using System.Runtime.CompilerServices;

namespace HytaleClient.Utils;

public struct BitField3D
{
	private uint[] _bits;

	public int MinX { get; private set; }

	public int MinY { get; private set; }

	public int MinZ { get; private set; }

	public int MaxX { get; private set; }

	public int MaxY { get; private set; }

	public int MaxZ { get; private set; }

	public int SizeX { get; private set; }

	public int SizeY { get; private set; }

	public int SizeZ { get; private set; }

	public void Initialize(int bufferSize)
	{
		_bits = new uint[bufferSize];
	}

	public void Setup(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
	{
		if (minX > maxX || minY > maxY || minZ > maxZ)
		{
			throw new Exception("Min Max values are incorrect.");
		}
		int num = maxX - minX + 1;
		int num2 = maxY - minY + 1;
		int num3 = maxZ - minZ + 1;
		int num4 = num * num2 * num3 / 32 + 1;
		if (_bits == null || _bits.Length < num4)
		{
			Initialize(num4);
		}
		else
		{
			Array.Clear(_bits, 0, num4);
		}
		MinX = minX;
		MinY = minY;
		MinZ = minZ;
		MaxX = maxX;
		MaxY = maxY;
		MaxZ = maxZ;
		SizeX = num;
		SizeY = num2;
		SizeZ = num3;
	}

	public bool IsBitOn(int x, int y, int z)
	{
		if (MinX > x || MaxX < x || MinY > y || MaxY < y || MinZ > z || MaxZ < z)
		{
			return false;
		}
		GetAccess(x, y, z, out var slotID, out var mask);
		return (_bits[slotID] & mask) != 0;
	}

	public void SwitchBitOn(int x, int y, int z)
	{
		if (MinX > x || MaxX < x || MinY > y || MaxY < y || MinZ > z || MaxZ < z)
		{
			throw new Exception("3D position out of bounds.");
		}
		GetAccess(x, y, z, out var slotID, out var mask);
		uint num = _bits[slotID] | mask;
		_bits[slotID] = num;
	}

	public void SwitchBitOff(int x, int y, int z)
	{
		if (MinX > x || MaxX < x || MinY > y || MaxY < y || MinZ > z || MaxZ < z)
		{
			throw new Exception("3D position out of bounds.");
		}
		GetAccess(x, y, z, out var slotID, out var mask);
		uint num = _bits[slotID] & ~mask;
		_bits[slotID] = num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void GetAccess(int x, int y, int z, out uint slotID, out uint mask)
	{
		int num = x - MinX;
		int num2 = y - MinY;
		int num3 = z - MinZ;
		int num4 = num + num2 * SizeX + num3 * (SizeY * SizeX);
		slotID = (uint)num4 / 32u;
		mask = (uint)(1 << num4 % 32);
	}
}
