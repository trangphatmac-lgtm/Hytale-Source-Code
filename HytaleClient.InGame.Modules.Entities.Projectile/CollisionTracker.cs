using System;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class CollisionTracker : BlockTracker
{
	protected BlockData[] _blockData = new BlockData[4];

	protected BlockContactData[] _contactData = new BlockContactData[4];

	public CollisionTracker()
	{
		for (int i = 0; i < 4; i++)
		{
			_blockData[i] = new BlockData();
			_contactData[i] = new BlockContactData();
		}
	}

	public BlockData GetBlockData(int index)
	{
		return _blockData[index];
	}

	public BlockContactData GetContactData(int index)
	{
		return _contactData[index];
	}

	public override void Reset()
	{
		base.Reset();
		for (int i = 0; i < Count; i++)
		{
			_blockData[i].Clear();
			_contactData[i].Clear();
		}
	}

	public bool Track(int x, int y, int z, BlockContactData contactData, BlockData blockData)
	{
		if (IsTracked(x, y, z))
		{
			return true;
		}
		TrackNew(x, y, z, contactData, blockData);
		return false;
	}

	public BlockContactData TrackNew(int x, int y, int z, BlockContactData contactData, BlockData blockData)
	{
		TrackNew(x, y, z);
		_blockData[Count - 1].Assign(blockData);
		BlockContactData blockContactData = _contactData[Count - 1];
		blockContactData.Assign(contactData);
		return blockContactData;
	}

	public override void Untrack(int index)
	{
		base.Untrack(index);
		if (Count == 0)
		{
			_blockData[0].Clear();
			_contactData[0].Clear();
			return;
		}
		int length = Count - index;
		BlockData blockData = _blockData[index];
		blockData.Clear();
		Array.Copy(_blockData, index + 1, _blockData, index, length);
		_blockData[Count] = null;
		BlockContactData blockContactData = _contactData[index];
		blockContactData.Clear();
		Array.Copy(_contactData, index + 1, _contactData, index, length);
		_contactData[Count] = null;
	}

	public BlockContactData GetContactData(int x, int y, int z)
	{
		int index = GetIndex(x, y, z);
		if (index == -1)
		{
			return null;
		}
		return _contactData[index];
	}

	protected override void Alloc()
	{
		base.Alloc();
		int num = _blockData.Length + 4;
		Array.Resize(ref _blockData, num);
		Array.Resize(ref _contactData, num);
		for (int i = Count; i < num; i++)
		{
			_blockData[i] = new BlockData();
			_contactData[i] = new BlockContactData();
		}
	}
}
