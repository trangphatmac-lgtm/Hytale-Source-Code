using System;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class BlockTracker
{
	public const int NotFound = -1;

	protected const int AllocSize = 4;

	protected IntVector3[] _positions = new IntVector3[4];

	public int Count;

	public BlockTracker()
	{
		for (int i = 0; i < _positions.Length; i++)
		{
			_positions[i] = default(IntVector3);
		}
	}

	public IntVector3 GetPosition(int index)
	{
		return _positions[index];
	}

	public virtual void Reset()
	{
		Count = 0;
	}

	public bool Track(int x, int y, int z)
	{
		if (IsTracked(x, y, z))
		{
			return true;
		}
		TrackNew(x, y, z);
		return false;
	}

	public void TrackNew(int x, int y, int z)
	{
		if (Count >= _positions.Length)
		{
			Alloc();
		}
		_positions[Count++] = new IntVector3(x, y, z);
	}

	public bool IsTracked(int x, int y, int z)
	{
		return GetIndex(x, y, z) >= 0;
	}

	public void Untrack(int x, int y, int z)
	{
		int index = GetIndex(x, y, z);
		if (index >= 0)
		{
			Untrack(index);
		}
	}

	public virtual void Untrack(int index)
	{
		if (Count <= 0)
		{
			throw new Exception("Calling untrack on empty tracker");
		}
		Count--;
		if (Count != 0)
		{
			IntVector3 intVector = _positions[index];
			Array.Copy(_positions, index + 1, _positions, index, Count - index);
			_positions[Count] = intVector;
		}
	}

	public int GetIndex(int x, int y, int z)
	{
		for (int num = Count - 1; num >= 0; num--)
		{
			ref IntVector3 reference = ref _positions[num];
			if (reference.X == x && reference.Y == y && reference.Z == z)
			{
				return num;
			}
		}
		return -1;
	}

	protected virtual void Alloc()
	{
		Array.Resize(ref _positions, _positions.Length + 4);
	}
}
