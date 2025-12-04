using HytaleClient.Data.Map;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal class BlockCollisionData : BoxCollisionData
{
	public int X;

	public int Y;

	public int Z;

	public int BlockId;

	public ClientBlockType BlockType;

	public Material? BlockMaterial;

	public int DetailBoxIndex;

	public bool Touching;

	public bool Overlapping;

	public void SetBlockData(CollisionConfig collisionConfig)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		X = collisionConfig.BlockX;
		Y = collisionConfig.BlockY;
		Z = collisionConfig.BlockZ;
		BlockId = collisionConfig.BlockId;
		BlockType = collisionConfig.BlockType;
		BlockMaterial = collisionConfig.BlockMaterial;
	}

	public void SetDetailBoxIndex(int detailBoxIndex)
	{
		DetailBoxIndex = detailBoxIndex;
	}

	public void SetTouchingOverlapping(bool touching, bool overlapping)
	{
		Touching = touching;
		Overlapping = overlapping;
	}

	public void clear()
	{
		BlockType = null;
		BlockMaterial = null;
	}
}
