using System.Collections.Generic;
using HytaleClient.Graphics;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.BlockyModels;

internal struct BlockyModelNode
{
	public enum ShapeType
	{
		None,
		Box,
		Quad
	}

	public enum QuadNormal
	{
		PlusZ,
		MinusZ,
		PlusX,
		MinusX,
		PlusY,
		MinusY
	}

	public int NameId;

	public CameraNode CameraNode;

	public Vector3 Position;

	public Quaternion Orientation;

	public ShapeType Type;

	public Vector3 Offset;

	public Vector3 ProceduralOffset;

	public Vector3 ProceduralRotation;

	public Vector3 Stretch;

	public bool Visible;

	public bool DoubleSided;

	public ShadingMode ShadingMode;

	public byte GradientId;

	public Vector3 Size;

	public bool IsPiece;

	public QuadNormal QuadNormalDirection;

	public byte AtlasIndex;

	public BlockyModelFaceTextureLayout[] TextureLayout;

	public List<int> Children;

	public static BlockyModelNode CreateMapBlockNode(int nodeNameId, float y, float height)
	{
		BlockyModelNode result = default(BlockyModelNode);
		result.NameId = nodeNameId;
		result.Position = new Vector3(0f, y, 0f);
		result.Orientation = Quaternion.Identity;
		result.Stretch = Vector3.One;
		result.Visible = true;
		result.Children = new List<int>();
		result.Type = ShapeType.Box;
		result.Size = new Vector3(32f, 32f * height, 32f);
		result.TextureLayout = new BlockyModelFaceTextureLayout[6];
		return result;
	}

	public BlockyModelNode Clone()
	{
		BlockyModelNode result = this;
		result.Children = new List<int>();
		if (TextureLayout != null)
		{
			result.TextureLayout = (BlockyModelFaceTextureLayout[])TextureLayout.Clone();
		}
		return result;
	}
}
