using System;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Graphics.BlockyModels;
using HytaleClient.Math;

namespace HytaleClient.Graphics.Map;

internal class RenderedStaticBlockyModel
{
	public readonly StaticBlockyModelVertex[] StaticVertices;

	public readonly ushort[] StaticIndices;

	public readonly StaticBlockyModelVertex[] AnimatedVertices;

	public readonly ushort[] AnimatedIndices;

	public readonly ushort LowLODIndicesCount;

	public readonly bool HasOnlyQuads;

	public readonly bool UsesBillboardLOD;

	public AnimatedRenderer.NodeTransform[] NodeParentTransforms;

	private static readonly Vector3[] QuadCornersPlusZ = new Vector3[4]
	{
		new Vector3(-0.5f, 0.5f, 0f),
		new Vector3(0.5f, 0.5f, 0f),
		new Vector3(0.5f, -0.5f, 0f),
		new Vector3(-0.5f, -0.5f, 0f)
	};

	private static readonly Vector3[] QuadCornersMinusZ = new Vector3[4]
	{
		new Vector3(0.5f, 0.5f, 0f),
		new Vector3(-0.5f, 0.5f, 0f),
		new Vector3(-0.5f, -0.5f, 0f),
		new Vector3(0.5f, -0.5f, 0f)
	};

	private static readonly Vector3[] QuadCornersPlusX = new Vector3[4]
	{
		new Vector3(0f, 0.5f, 0.5f),
		new Vector3(0f, 0.5f, -0.5f),
		new Vector3(0f, -0.5f, -0.5f),
		new Vector3(0f, -0.5f, 0.5f)
	};

	private static readonly Vector3[] QuadCornersMinusX = new Vector3[4]
	{
		new Vector3(0f, 0.5f, -0.5f),
		new Vector3(0f, 0.5f, 0.5f),
		new Vector3(0f, -0.5f, 0.5f),
		new Vector3(0f, -0.5f, -0.5f)
	};

	private static readonly Vector3[] QuadCornersPlusY = new Vector3[4]
	{
		new Vector3(-0.5f, 0f, -0.5f),
		new Vector3(0.5f, 0f, -0.5f),
		new Vector3(0.5f, 0f, 0.5f),
		new Vector3(-0.5f, 0f, 0.5f)
	};

	private static readonly Vector3[] QuadCornersMinusY = new Vector3[4]
	{
		new Vector3(-0.5f, 0f, 0.5f),
		new Vector3(0.5f, 0f, 0.5f),
		new Vector3(0.5f, 0f, -0.5f),
		new Vector3(-0.5f, 0f, -0.5f)
	};

	private static readonly Vector3[] TempCorners = new Vector3[8];

	private static readonly Point[] TempPoints = new Point[4];

	public RenderedStaticBlockyModel(BlockyModel model)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		bool flag = true;
		int num5 = 0;
		for (int i = 0; i < model.NodeCount; i++)
		{
			BlockyModelNode blockyModelNode = model.AllNodes[i];
			if (blockyModelNode.Type == BlockyModelNode.ShapeType.None)
			{
				continue;
			}
			BlockyModelFaceTextureLayout[] textureLayout = blockyModelNode.TextureLayout;
			for (int j = 0; j < textureLayout.Length; j++)
			{
				BlockyModelFaceTextureLayout blockyModelFaceTextureLayout = textureLayout[j];
				if (!blockyModelFaceTextureLayout.Hidden)
				{
					if (blockyModelNode.Visible)
					{
						num += 4;
						num2 += 2;
					}
					num3 += 4;
					num4 += 2;
				}
			}
			if (blockyModelNode.Type == BlockyModelNode.ShapeType.Box)
			{
				flag = false;
				if (num5 == 0 && num2 > 0 && model.Lod == LodMode.Auto)
				{
					num5 = (ushort)(num2 * 3);
				}
			}
		}
		switch (model.Lod)
		{
		case LodMode.Off:
			LowLODIndicesCount = (ushort)(num2 * 3);
			break;
		case LodMode.Disappear:
			LowLODIndicesCount = 0;
			break;
		case LodMode.Billboard:
			LowLODIndicesCount = 6;
			break;
		case LodMode.Auto:
		{
			int num6 = num2 / 2;
			LowLODIndicesCount = (flag ? ((ushort)(num6 / 2 * 6)) : ((ushort)num5));
			break;
		}
		default:
			throw new Exception("Unreachable");
		}
		HasOnlyQuads = flag;
		UsesBillboardLOD = model.Lod == LodMode.Billboard;
		StaticVertices = new StaticBlockyModelVertex[num];
		StaticIndices = new ushort[num2 * 3];
		int verticesOffset = 0;
		int indicesOffset = 0;
		AnimatedVertices = new StaticBlockyModelVertex[num3];
		AnimatedIndices = new ushort[num4 * 3];
		int verticesOffset2 = 0;
		int indicesOffset2 = 0;
		NodeParentTransforms = new AnimatedRenderer.NodeTransform[model.NodeCount];
		for (byte b = 0; b < model.NodeCount; b++)
		{
			ref BlockyModelNode reference = ref model.AllNodes[b];
			float num7;
			float num8;
			float num9;
			if (reference.Type == BlockyModelNode.ShapeType.Quad)
			{
				switch (reference.QuadNormalDirection)
				{
				case BlockyModelNode.QuadNormal.PlusX:
				case BlockyModelNode.QuadNormal.MinusX:
					num7 = 0f;
					num8 = reference.Size.Y * reference.Stretch.Y;
					num9 = reference.Size.X * reference.Stretch.Z;
					break;
				case BlockyModelNode.QuadNormal.PlusY:
				case BlockyModelNode.QuadNormal.MinusY:
					num7 = reference.Size.X * reference.Stretch.X;
					num8 = 0f;
					num9 = reference.Size.Y * reference.Stretch.Z;
					break;
				default:
					num7 = reference.Size.X * reference.Stretch.X;
					num8 = reference.Size.Y * reference.Stretch.Y;
					num9 = 0f;
					break;
				}
			}
			else
			{
				num7 = reference.Size.X * reference.Stretch.X;
				num8 = reference.Size.Y * reference.Stretch.Y;
				num9 = reference.Size.Z * reference.Stretch.Z;
			}
			Matrix.CreateScale(num7, num8, num9, out var result);
			SetupVertices(b, ref reference, ref result, ref verticesOffset2, AnimatedVertices, ref indicesOffset2, AnimatedIndices);
			if (reference.Visible)
			{
				NodeParentTransforms[b].Position = Vector3.Transform(reference.Offset, reference.Orientation) + Vector3.Transform(reference.ProceduralOffset, Quaternion.Identity) + reference.Position;
				Quaternion quaternion = Quaternion.CreateFromYawPitchRoll(reference.ProceduralRotation.Yaw, reference.ProceduralRotation.Pitch, reference.ProceduralRotation.Roll);
				NodeParentTransforms[b].Orientation = quaternion * reference.Orientation;
				int num10 = model.ParentNodes[b];
				if (num10 >= 0)
				{
					NodeParentTransforms[b].Position = Vector3.Transform(NodeParentTransforms[b].Position, NodeParentTransforms[num10].Orientation) + NodeParentTransforms[num10].Position;
					NodeParentTransforms[b].Orientation = NodeParentTransforms[num10].Orientation * NodeParentTransforms[b].Orientation;
				}
				Matrix.Compose(num7, num8, num9, NodeParentTransforms[b].Orientation, NodeParentTransforms[b].Position, out result);
				SetupVertices(b, ref reference, ref result, ref verticesOffset, StaticVertices, ref indicesOffset, StaticIndices);
			}
		}
	}

	public void PrepareUVs(BlockyModel model, Point textureSize, Point atlasSize)
	{
		int verticesOffset = 0;
		int verticesOffset2 = 0;
		for (byte b = 0; b < model.NodeCount; b++)
		{
			ref BlockyModelNode reference = ref model.AllNodes[b];
			SetupUVs(ref reference, ref verticesOffset, AnimatedVertices, textureSize, atlasSize);
			if (reference.Visible)
			{
				SetupUVs(ref reference, ref verticesOffset2, StaticVertices, textureSize, atlasSize);
			}
		}
	}

	private static void SetupVertices(byte nodeIndex, ref BlockyModelNode node, ref Matrix nodeMatrix, ref int verticesOffset, StaticBlockyModelVertex[] vertices, ref int indicesOffset, ushort[] indices)
	{
		bool useDefaultVertexOrder = (node.Stretch.X > 0f) ^ (node.Stretch.Y > 0f) ^ (node.Stretch.Z > 0f);
		int num = 0;
		switch (node.Type)
		{
		case BlockyModelNode.ShapeType.Box:
			Vector3.Transform(ModelRenderer.BoxCorners, ref nodeMatrix, TempCorners);
			if (!node.TextureLayout[0].Hidden)
			{
				SetupQuad(vertices, nodeIndex, node.ShadingMode, node.DoubleSided, verticesOffset + num * 4, 4, 5, 6, 7, useDefaultVertexOrder);
				SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, useDefaultVertexOrder);
				num++;
				indicesOffset += 6;
			}
			if (!node.TextureLayout[1].Hidden)
			{
				SetupQuad(vertices, nodeIndex, node.ShadingMode, node.DoubleSided, verticesOffset + num * 4, 0, 1, 2, 3, useDefaultVertexOrder);
				SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, useDefaultVertexOrder);
				num++;
				indicesOffset += 6;
			}
			if (!node.TextureLayout[2].Hidden)
			{
				SetupQuad(vertices, nodeIndex, node.ShadingMode, node.DoubleSided, verticesOffset + num * 4, 1, 4, 7, 2, useDefaultVertexOrder);
				SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, useDefaultVertexOrder);
				num++;
				indicesOffset += 6;
			}
			if (!node.TextureLayout[3].Hidden)
			{
				SetupQuad(vertices, nodeIndex, node.ShadingMode, node.DoubleSided, verticesOffset + num * 4, 7, 6, 3, 2, useDefaultVertexOrder);
				SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, useDefaultVertexOrder);
				num++;
				indicesOffset += 6;
			}
			if (!node.TextureLayout[4].Hidden)
			{
				SetupQuad(vertices, nodeIndex, node.ShadingMode, node.DoubleSided, verticesOffset + num * 4, 5, 0, 3, 6, useDefaultVertexOrder);
				SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, useDefaultVertexOrder);
				num++;
				indicesOffset += 6;
			}
			if (!node.TextureLayout[5].Hidden)
			{
				SetupQuad(vertices, nodeIndex, node.ShadingMode, node.DoubleSided, verticesOffset + num * 4, 1, 0, 5, 4, useDefaultVertexOrder);
				SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, useDefaultVertexOrder);
				num++;
				indicesOffset += 6;
			}
			verticesOffset += num * 4;
			break;
		case BlockyModelNode.ShapeType.Quad:
			if (!node.TextureLayout[0].Hidden)
			{
				Vector3[] sourceArray = ((node.QuadNormalDirection == BlockyModelNode.QuadNormal.PlusZ) ? QuadCornersPlusZ : ((node.QuadNormalDirection == BlockyModelNode.QuadNormal.MinusZ) ? QuadCornersMinusZ : ((node.QuadNormalDirection == BlockyModelNode.QuadNormal.PlusX) ? QuadCornersPlusX : ((node.QuadNormalDirection == BlockyModelNode.QuadNormal.MinusX) ? QuadCornersMinusX : ((node.QuadNormalDirection == BlockyModelNode.QuadNormal.PlusY) ? QuadCornersPlusY : QuadCornersMinusY)))));
				Vector3.Transform(sourceArray, ref nodeMatrix, TempCorners);
				SetupQuad(vertices, nodeIndex, node.ShadingMode, node.DoubleSided, verticesOffset, 1, 0, 3, 2, useDefaultVertexOrder);
				SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, useDefaultVertexOrder);
				num++;
				indicesOffset += 6;
				verticesOffset += num * 4;
			}
			break;
		}
	}

	private static void SetupUVs(ref BlockyModelNode node, ref int verticesOffset, StaticBlockyModelVertex[] vertices, Point textureSize, Point atlasSize)
	{
		int num = 0;
		switch (node.Type)
		{
		case BlockyModelNode.ShapeType.Box:
			if (!node.TextureLayout[0].Hidden)
			{
				SetupQuadUV(vertices, verticesOffset + num * 4, ref node.TextureLayout[0], GetShapeTextureFaceSize(ref node, "front"), textureSize, atlasSize);
				num++;
			}
			if (!node.TextureLayout[1].Hidden)
			{
				SetupQuadUV(vertices, verticesOffset + num * 4, ref node.TextureLayout[1], GetShapeTextureFaceSize(ref node, "back"), textureSize, atlasSize);
				num++;
			}
			if (!node.TextureLayout[2].Hidden)
			{
				SetupQuadUV(vertices, verticesOffset + num * 4, ref node.TextureLayout[2], GetShapeTextureFaceSize(ref node, "right"), textureSize, atlasSize);
				num++;
			}
			if (!node.TextureLayout[3].Hidden)
			{
				SetupQuadUV(vertices, verticesOffset + num * 4, ref node.TextureLayout[3], GetShapeTextureFaceSize(ref node, "bottom"), textureSize, atlasSize);
				num++;
			}
			if (!node.TextureLayout[4].Hidden)
			{
				SetupQuadUV(vertices, verticesOffset + num * 4, ref node.TextureLayout[4], GetShapeTextureFaceSize(ref node, "left"), textureSize, atlasSize);
				num++;
			}
			if (!node.TextureLayout[5].Hidden)
			{
				SetupQuadUV(vertices, verticesOffset + num * 4, ref node.TextureLayout[5], GetShapeTextureFaceSize(ref node, "top"), textureSize, atlasSize);
				num++;
			}
			verticesOffset += num * 4;
			break;
		case BlockyModelNode.ShapeType.Quad:
			if (!node.TextureLayout[0].Hidden)
			{
				SetupQuadUV(vertices, verticesOffset + num * 4, ref node.TextureLayout[0], GetShapeTextureFaceSize(ref node, "front"), textureSize, atlasSize);
				num++;
				verticesOffset += num * 4;
			}
			break;
		}
	}

	private static void SetupQuad(StaticBlockyModelVertex[] vertices, byte nodeIndex, ShadingMode shadingMode, bool doubleSided, int offset, int a, int b, int c, int d, bool useDefaultVertexOrder)
	{
		vertices[offset].NodeIndex = (vertices[offset + 1].NodeIndex = (vertices[offset + 2].NodeIndex = (vertices[offset + 3].NodeIndex = nodeIndex)));
		vertices[offset].Position = TempCorners[a];
		vertices[offset + 1].Position = TempCorners[b];
		vertices[offset + 2].Position = TempCorners[c];
		vertices[offset + 3].Position = TempCorners[d];
		uint doubleSided2 = (doubleSided ? 1u : 0u);
		vertices[offset].DoubleSided = doubleSided2;
		vertices[offset + 1].DoubleSided = doubleSided2;
		vertices[offset + 2].DoubleSided = doubleSided2;
		vertices[offset + 3].DoubleSided = doubleSided2;
		Vector3 normal = (useDefaultVertexOrder ? Vector3.Cross(vertices[offset].Position - vertices[offset + 1].Position, vertices[offset].Position - vertices[offset + 2].Position) : Vector3.Cross(vertices[offset + 1].Position - vertices[offset].Position, vertices[offset + 1].Position - vertices[offset + 3].Position));
		normal.Normalize();
		vertices[offset].Normal = (vertices[offset + 1].Normal = (vertices[offset + 2].Normal = (vertices[offset + 3].Normal = normal)));
		vertices[offset].ShadingMode = (vertices[offset + 1].ShadingMode = (vertices[offset + 2].ShadingMode = (vertices[offset + 3].ShadingMode = shadingMode)));
	}

	private static void SetupQuadUV(StaticBlockyModelVertex[] vertices, int offset, ref BlockyModelFaceTextureLayout faceData, Point size, Point textureSize, Point atlasSize)
	{
		int x = faceData.Offset.X;
		int x2 = faceData.Offset.X + ((!faceData.MirrorX) ? 1 : (-1)) * size.X;
		int num = textureSize.Y - faceData.Offset.Y;
		int y = textureSize.Y - (faceData.Offset.Y + ((!faceData.MirrorY) ? 1 : (-1)) * size.Y);
		TempPoints[0] = new Point(x2, num);
		TempPoints[1] = new Point(x, num);
		TempPoints[2] = new Point(x, y);
		TempPoints[3] = new Point(x2, y);
		int num2 = 1;
		int num3 = 0;
		switch (faceData.Angle)
		{
		case 90:
			num2 = 0;
			num3 = -1;
			break;
		case 180:
			num2 = -1;
			num3 = 0;
			break;
		case 270:
			num2 = 0;
			num3 = 1;
			break;
		}
		int num4 = int.MaxValue;
		int num5 = int.MaxValue;
		for (int i = 0; i < 4; i++)
		{
			Point point = TempPoints[i];
			int x3 = x + (point.X - x) * num2 - (point.Y - num) * num3;
			int num6 = num + (point.X - x) * num3 + (point.Y - num) * num2;
			TempPoints[i].X = x3;
			TempPoints[i].Y = textureSize.Y - num6;
			num4 = System.Math.Min(num4, TempPoints[i].X);
			num5 = System.Math.Min(num5, TempPoints[i].Y);
		}
		for (int j = 0; j < 4; j++)
		{
			Point point2 = TempPoints[j];
			float num7 = ((point2.X == num4) ? 0.04f : (-0.04f));
			float num8 = ((point2.Y == num5) ? 0.04f : (-0.04f));
			vertices[offset + j].TextureCoordinates.X = ((float)point2.X + num7) / (float)atlasSize.X;
			vertices[offset + j].TextureCoordinates.Y = ((float)point2.Y + num8) / (float)atlasSize.Y;
		}
	}

	private static void SetupQuadIndices(ushort[] indices, int indicesOffset, int targetOffset, bool useDefaultVertexOrder)
	{
		if (useDefaultVertexOrder)
		{
			indices[indicesOffset] = (ushort)targetOffset;
			indices[indicesOffset + 1] = (ushort)(targetOffset + 1);
			indices[indicesOffset + 2] = (ushort)(targetOffset + 2);
			indices[indicesOffset + 3] = (ushort)targetOffset;
			indices[indicesOffset + 4] = (ushort)(targetOffset + 2);
			indices[indicesOffset + 5] = (ushort)(targetOffset + 3);
		}
		else
		{
			indices[indicesOffset] = (ushort)(targetOffset + 1);
			indices[indicesOffset + 1] = (ushort)targetOffset;
			indices[indicesOffset + 2] = (ushort)(targetOffset + 3);
			indices[indicesOffset + 3] = (ushort)(targetOffset + 1);
			indices[indicesOffset + 4] = (ushort)(targetOffset + 3);
			indices[indicesOffset + 5] = (ushort)(targetOffset + 2);
		}
	}

	private static Point GetShapeTextureFaceSize(ref BlockyModelNode node, string faceName)
	{
		switch (node.Type)
		{
		case BlockyModelNode.ShapeType.Box:
			switch (faceName)
			{
			case "front":
			case "back":
				return new Point((int)node.Size.X, (int)node.Size.Y);
			case "left":
			case "right":
				return new Point((int)node.Size.Z, (int)node.Size.Y);
			case "top":
			case "bottom":
				return new Point((int)node.Size.X, (int)node.Size.Z);
			}
			break;
		case BlockyModelNode.ShapeType.Quad:
			if (!(faceName == "front"))
			{
				break;
			}
			return new Point((int)node.Size.X, (int)node.Size.Y);
		}
		throw new Exception("Unreachable");
	}
}
