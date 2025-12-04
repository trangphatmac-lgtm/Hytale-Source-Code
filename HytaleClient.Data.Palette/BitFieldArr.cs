using System;

namespace HytaleClient.Data.Palette;

internal class BitFieldArr
{
	private readonly int _bits;

	private readonly int _length;

	private readonly byte[] _array;

	public BitFieldArr(int bits, int length)
	{
		_bits = bits;
		_array = new byte[length * bits / 8];
		_length = length;
	}

	public int GetLength()
	{
		return _length;
	}

	public uint Get(int index)
	{
		int num = index * _bits;
		uint num2 = 0u;
		int num3 = 0;
		while (num3 < _bits)
		{
			num2 |= (uint)(GetBit(num) << num3);
			num3++;
			num++;
		}
		return num2;
	}

	private int GetBit(int bitIndex)
	{
		return (_array[bitIndex / 8] >> bitIndex % 8) & 1;
	}

	public void Set(int index, int value)
	{
		int num = index * _bits;
		int num2 = 0;
		while (num2 < _bits)
		{
			SetBit(num, (value >> num2) & 1);
			num2++;
			num++;
		}
	}

	private void SetBit(int bitIndex, int bit)
	{
		if (bit == 0)
		{
			_array[bitIndex / 8] &= (byte)(~(1 << bitIndex % 8));
		}
		else
		{
			_array[bitIndex / 8] |= (byte)(1 << bitIndex % 8);
		}
	}

	public byte[] Get()
	{
		byte[] array = new byte[_array.Length];
		Buffer.BlockCopy(_array, 0, array, 0, _array.Length);
		return array;
	}

	public void Set(byte[] bytes)
	{
		Buffer.BlockCopy(bytes, 0, _array, 0, (bytes.Length < _array.Length) ? bytes.Length : _array.Length);
	}
}
