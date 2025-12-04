using System;
using System.Runtime.CompilerServices;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;

namespace HytaleClient.Graphics.BlockyModels;

internal class ModelRenderer : AnimatedRenderer
{
	public static class BoxCorner
	{
		public const int BackTopLeft = 0;

		public const int BackTopRight = 1;

		public const int BackBottomRight = 2;

		public const int BackBottomLeft = 3;

		public const int FrontTopRight = 4;

		public const int FrontTopLeft = 5;

		public const int FrontBottomLeft = 6;

		public const int FrontBottomRight = 7;
	}

	public static class QuadCorner
	{
		public const int TopLeft = 0;

		public const int TopRight = 1;

		public const int BottomRight = 2;

		public const int BottomLeft = 3;
	}

	public readonly int IndicesCount;

	private ModelVertex[] _vertices;

	private ushort[] _indices;

	private GLBuffer _verticesBuffer;

	private GLBuffer _indicesBuffer;

	private uint _timestamp;

	private static readonly Vector3[] TempCorners = new Vector3[8];

	public static readonly Vector3[] BoxCorners = new Vector3[8]
	{
		new Vector3(-0.5f, 0.5f, -0.5f),
		new Vector3(0.5f, 0.5f, -0.5f),
		new Vector3(0.5f, -0.5f, -0.5f),
		new Vector3(-0.5f, -0.5f, -0.5f),
		new Vector3(0.5f, 0.5f, 0.5f),
		new Vector3(-0.5f, 0.5f, 0.5f),
		new Vector3(-0.5f, -0.5f, 0.5f),
		new Vector3(0.5f, -0.5f, 0.5f)
	};

	public uint Timestamp => _timestamp;

	public GLVertexArray VertexArray { get; private set; }

	public ModelRenderer(BlockyModel model, Point[] atlasSizes, GraphicsDevice graphics, uint timestamp, bool selfManageNodeBuffer = false)
		: base(model, atlasSizes, selfManageNodeBuffer)
	{
		_timestamp = timestamp;
		MakeGeometry(model, atlasSizes, out _vertices, out _indices);
		IndicesCount = _indices.Length;
		if (graphics != null)
		{
			CreateGPUData(graphics);
		}
	}

	public unsafe override void CreateGPUData(GraphicsDevice graphics)
	{
		base.CreateGPUData(graphics);
		GLFunctions gL = graphics.GL;
		VertexArray = gL.GenVertexArray();
		gL.BindVertexArray(VertexArray);
		_verticesBuffer = gL.GenBuffer();
		gL.BindBuffer(VertexArray, GL.ARRAY_BUFFER, _verticesBuffer);
		fixed (ModelVertex* ptr = _vertices)
		{
			gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(_vertices.Length * ModelVertex.Size), (IntPtr)ptr, GL.STATIC_DRAW);
		}
		_vertices = null;
		_indicesBuffer = gL.GenBuffer();
		gL.BindBuffer(VertexArray, GL.ELEMENT_ARRAY_BUFFER, _indicesBuffer);
		fixed (ushort* ptr2 = _indices)
		{
			gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(IndicesCount * 2), (IntPtr)ptr2, GL.STATIC_DRAW);
		}
		_indices = null;
		BlockyModelProgram blockyModelProgram = graphics.GPUProgramStore.BlockyModelProgram;
		IntPtr zero = IntPtr.Zero;
		gL.EnableVertexAttribArray(blockyModelProgram.AttribNodeIndex.Index);
		gL.VertexAttribIPointer(blockyModelProgram.AttribNodeIndex.Index, 1, GL.UNSIGNED_INT, ModelVertex.Size, zero);
		zero += 4;
		gL.EnableVertexAttribArray(blockyModelProgram.AttribAtlasIndexAndShadingModeAndGradientId.Index);
		gL.VertexAttribIPointer(blockyModelProgram.AttribAtlasIndexAndShadingModeAndGradientId.Index, 1, GL.UNSIGNED_INT, ModelVertex.Size, zero);
		zero += 4;
		gL.EnableVertexAttribArray(blockyModelProgram.AttribPosition.Index);
		gL.VertexAttribPointer(blockyModelProgram.AttribPosition.Index, 3, GL.FLOAT, normalized: false, ModelVertex.Size, zero);
		zero += 12;
		gL.EnableVertexAttribArray(blockyModelProgram.AttribTexCoords.Index);
		gL.VertexAttribPointer(blockyModelProgram.AttribTexCoords.Index, 2, GL.FLOAT, normalized: false, ModelVertex.Size, zero);
		zero += 8;
	}

	protected override void DoDispose()
	{
		base.DoDispose();
		if (_graphics != null)
		{
			GLFunctions gL = _graphics.GL;
			gL.DeleteBuffer(_verticesBuffer);
			gL.DeleteBuffer(_indicesBuffer);
			gL.DeleteVertexArray(VertexArray);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Draw()
	{
		if (base.Disposed)
		{
			throw new ObjectDisposedException(typeof(ModelRenderer).FullName);
		}
		GLFunctions gL = _graphics.GL;
		gL.BindVertexArray(VertexArray);
		gL.DrawElements(GL.TRIANGLES, IndicesCount, GL.UNSIGNED_SHORT, (IntPtr)0);
	}

	private static void MakeGeometry(BlockyModel model, Point[] atlasSizes, out ModelVertex[] vertices, out ushort[] indices)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < model.NodeCount; i++)
		{
			if (model.AllNodes[i].Type == BlockyModelNode.ShapeType.None)
			{
				continue;
			}
			BlockyModelFaceTextureLayout[] textureLayout = model.AllNodes[i].TextureLayout;
			for (int j = 0; j < textureLayout.Length; j++)
			{
				BlockyModelFaceTextureLayout blockyModelFaceTextureLayout = textureLayout[j];
				if (!blockyModelFaceTextureLayout.Hidden)
				{
					num += 4;
					num2 += (model.AllNodes[i].DoubleSided ? 4 : 2);
				}
			}
		}
		vertices = new ModelVertex[num];
		indices = new ushort[num2 * 3];
		int verticesOffset = 0;
		int indicesOffset = 0;
		for (uint num3 = 0u; num3 < model.NodeCount; num3++)
		{
			SetupVertices(ref model.AllNodes[num3], num3, ref verticesOffset, vertices, ref indicesOffset, indices, atlasSizes);
		}
	}

	private static void SetupVertices(ref BlockyModelNode node, uint nodeIndex, ref int verticesOffset, ModelVertex[] vertices, ref int indicesOffset, ushort[] indices, Point[] atlasSizes)
	{
		bool flag = (node.Stretch.X > 0f) ^ (node.Stretch.Y > 0f) ^ (node.Stretch.Z > 0f);
		byte gradientId = node.GradientId;
		switch (node.Type)
		{
		case BlockyModelNode.ShapeType.Box:
		{
			Matrix matrix = Matrix.CreateScale(node.Size * node.Stretch);
			Vector3.Transform(BoxCorners, ref matrix, TempCorners);
			int num = 0;
			if (!node.TextureLayout[0].Hidden)
			{
				SetupQuad(nodeIndex, node.AtlasIndex, (byte)node.ShadingMode, gradientId, vertices, verticesOffset + num * 4, 4, 5, 6, 7);
				SetupQuadUV(vertices, verticesOffset + num * 4, ref node.TextureLayout[0], GetShapeTextureFaceSize(ref node, "front"), atlasSizes[node.AtlasIndex]);
				SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, flag);
				indicesOffset += 6;
				if (node.DoubleSided)
				{
					SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, !flag);
					indicesOffset += 6;
				}
				num++;
			}
			if (!node.TextureLayout[1].Hidden)
			{
				SetupQuad(nodeIndex, node.AtlasIndex, (byte)node.ShadingMode, gradientId, vertices, verticesOffset + num * 4, 0, 1, 2, 3);
				SetupQuadUV(vertices, verticesOffset + num * 4, ref node.TextureLayout[1], GetShapeTextureFaceSize(ref node, "back"), atlasSizes[node.AtlasIndex]);
				SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, flag);
				indicesOffset += 6;
				if (node.DoubleSided)
				{
					SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, !flag);
					indicesOffset += 6;
				}
				num++;
			}
			if (!node.TextureLayout[2].Hidden)
			{
				SetupQuad(nodeIndex, node.AtlasIndex, (byte)node.ShadingMode, gradientId, vertices, verticesOffset + num * 4, 1, 4, 7, 2);
				SetupQuadUV(vertices, verticesOffset + num * 4, ref node.TextureLayout[2], GetShapeTextureFaceSize(ref node, "right"), atlasSizes[node.AtlasIndex]);
				SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, flag);
				indicesOffset += 6;
				if (node.DoubleSided)
				{
					SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, !flag);
					indicesOffset += 6;
				}
				num++;
			}
			if (!node.TextureLayout[3].Hidden)
			{
				SetupQuad(nodeIndex, node.AtlasIndex, (byte)node.ShadingMode, gradientId, vertices, verticesOffset + num * 4, 7, 6, 3, 2);
				SetupQuadUV(vertices, verticesOffset + num * 4, ref node.TextureLayout[3], GetShapeTextureFaceSize(ref node, "bottom"), atlasSizes[node.AtlasIndex]);
				SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, flag);
				indicesOffset += 6;
				if (node.DoubleSided)
				{
					SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, !flag);
					indicesOffset += 6;
				}
				num++;
			}
			if (!node.TextureLayout[4].Hidden)
			{
				SetupQuad(nodeIndex, node.AtlasIndex, (byte)node.ShadingMode, gradientId, vertices, verticesOffset + num * 4, 5, 0, 3, 6);
				SetupQuadUV(vertices, verticesOffset + num * 4, ref node.TextureLayout[4], GetShapeTextureFaceSize(ref node, "left"), atlasSizes[node.AtlasIndex]);
				SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, flag);
				indicesOffset += 6;
				if (node.DoubleSided)
				{
					SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, !flag);
					indicesOffset += 6;
				}
				num++;
			}
			if (!node.TextureLayout[5].Hidden)
			{
				SetupQuad(nodeIndex, node.AtlasIndex, (byte)node.ShadingMode, gradientId, vertices, verticesOffset + num * 4, 1, 0, 5, 4);
				SetupQuadUV(vertices, verticesOffset + num * 4, ref node.TextureLayout[5], GetShapeTextureFaceSize(ref node, "top"), atlasSizes[node.AtlasIndex]);
				SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, flag);
				indicesOffset += 6;
				if (node.DoubleSided)
				{
					SetupQuadIndices(indices, indicesOffset, verticesOffset + num * 4, !flag);
					indicesOffset += 6;
				}
				num++;
			}
			verticesOffset += num * 4;
			break;
		}
		case BlockyModelNode.ShapeType.Quad:
		{
			Vector3 vector = node.Size / 2f;
			if (!node.TextureLayout[0].Hidden)
			{
				if (node.QuadNormalDirection == BlockyModelNode.QuadNormal.PlusZ)
				{
					vector.X *= node.Stretch.X;
					vector.Y *= node.Stretch.Y;
					TempCorners[0] = new Vector3(0f - vector.X, vector.Y, 0f);
					TempCorners[1] = new Vector3(vector.X, vector.Y, 0f);
					TempCorners[2] = new Vector3(vector.X, 0f - vector.Y, 0f);
					TempCorners[3] = new Vector3(0f - vector.X, 0f - vector.Y, 0f);
				}
				else if (node.QuadNormalDirection == BlockyModelNode.QuadNormal.MinusZ)
				{
					vector.X *= node.Stretch.X;
					vector.Y *= node.Stretch.Y;
					TempCorners[0] = new Vector3(vector.X, vector.Y, 0f);
					TempCorners[1] = new Vector3(0f - vector.X, vector.Y, 0f);
					TempCorners[2] = new Vector3(0f - vector.X, 0f - vector.Y, 0f);
					TempCorners[3] = new Vector3(vector.X, 0f - vector.Y, 0f);
				}
				else if (node.QuadNormalDirection == BlockyModelNode.QuadNormal.PlusX)
				{
					vector.X *= node.Stretch.Z;
					vector.Y *= node.Stretch.Y;
					TempCorners[0] = new Vector3(0f, vector.Y, vector.X);
					TempCorners[1] = new Vector3(0f, vector.Y, 0f - vector.X);
					TempCorners[2] = new Vector3(0f, 0f - vector.Y, 0f - vector.X);
					TempCorners[3] = new Vector3(0f, 0f - vector.Y, vector.X);
				}
				else if (node.QuadNormalDirection == BlockyModelNode.QuadNormal.MinusX)
				{
					vector.X *= node.Stretch.Z;
					vector.Y *= node.Stretch.Y;
					TempCorners[0] = new Vector3(0f, vector.Y, 0f - vector.X);
					TempCorners[1] = new Vector3(0f, vector.Y, vector.X);
					TempCorners[2] = new Vector3(0f, 0f - vector.Y, vector.X);
					TempCorners[3] = new Vector3(0f, 0f - vector.Y, 0f - vector.X);
				}
				else if (node.QuadNormalDirection == BlockyModelNode.QuadNormal.PlusY)
				{
					vector.X *= node.Stretch.X;
					vector.Y *= node.Stretch.Z;
					TempCorners[0] = new Vector3(0f - vector.X, 0f, 0f - vector.Y);
					TempCorners[1] = new Vector3(vector.X, 0f, 0f - vector.Y);
					TempCorners[2] = new Vector3(vector.X, 0f, vector.Y);
					TempCorners[3] = new Vector3(0f - vector.X, 0f, vector.Y);
				}
				else if (node.QuadNormalDirection == BlockyModelNode.QuadNormal.MinusY)
				{
					vector.X *= node.Stretch.X;
					vector.Y *= node.Stretch.Z;
					TempCorners[0] = new Vector3(0f - vector.X, 0f, vector.Y);
					TempCorners[1] = new Vector3(vector.X, 0f, vector.Y);
					TempCorners[2] = new Vector3(vector.X, 0f, 0f - vector.Y);
					TempCorners[3] = new Vector3(0f - vector.X, 0f, 0f - vector.Y);
				}
				SetupQuad(nodeIndex, node.AtlasIndex, (byte)node.ShadingMode, gradientId, vertices, verticesOffset, 1, 0, 3, 2);
				SetupQuadUV(vertices, verticesOffset, ref node.TextureLayout[0], GetShapeTextureFaceSize(ref node, "front"), atlasSizes[node.AtlasIndex]);
				SetupQuadIndices(indices, indicesOffset, verticesOffset, flag);
				indicesOffset += 6;
				if (node.DoubleSided)
				{
					SetupQuadIndices(indices, indicesOffset, verticesOffset, !flag);
					indicesOffset += 6;
				}
				verticesOffset += 4;
			}
			break;
		}
		}
	}

	private static void SetupQuad(uint nodeIndex, byte atlasIndex, byte shadingMode, byte gradientId, ModelVertex[] vertices, int offset, int a, int b, int c, int d)
	{
		vertices[offset].NodeIndex = (vertices[offset + 1].NodeIndex = (vertices[offset + 2].NodeIndex = (vertices[offset + 3].NodeIndex = nodeIndex)));
		uint atlasIndexAndShadingModeAndGradienId = (uint)(atlasIndex | (shadingMode << 8) | (gradientId << 10));
		vertices[offset].AtlasIndexAndShadingModeAndGradienId = (vertices[offset + 1].AtlasIndexAndShadingModeAndGradienId = (vertices[offset + 2].AtlasIndexAndShadingModeAndGradienId = (vertices[offset + 3].AtlasIndexAndShadingModeAndGradienId = atlasIndexAndShadingModeAndGradienId)));
		vertices[offset].Position = TempCorners[a];
		vertices[offset + 1].Position = TempCorners[b];
		vertices[offset + 2].Position = TempCorners[c];
		vertices[offset + 3].Position = TempCorners[d];
	}

	private static void SetupQuadUV(ModelVertex[] vertices, int offset, ref BlockyModelFaceTextureLayout faceData, Vector2 size, Point textureSize)
	{
		int x = faceData.Offset.X;
		float x2 = (float)faceData.Offset.X + (float)((!faceData.MirrorX) ? 1 : (-1)) * size.X;
		int num = textureSize.Y - faceData.Offset.Y;
		float y = (float)textureSize.Y - ((float)faceData.Offset.Y + (float)((!faceData.MirrorY) ? 1 : (-1)) * size.Y);
		vertices[offset].TextureCoordinates = new Vector2(x2, num);
		vertices[offset + 1].TextureCoordinates = new Vector2(x, num);
		vertices[offset + 2].TextureCoordinates = new Vector2(x, y);
		vertices[offset + 3].TextureCoordinates = new Vector2(x2, y);
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
		for (int i = 0; i < 4; i++)
		{
			Vector2 textureCoordinates = vertices[offset + i].TextureCoordinates;
			float num4 = (float)x + (textureCoordinates.X - (float)x) * (float)num2 - (textureCoordinates.Y - (float)num) * (float)num3;
			float num5 = (float)num + (textureCoordinates.X - (float)x) * (float)num3 + (textureCoordinates.Y - (float)num) * (float)num2;
			vertices[offset + i].TextureCoordinates.X = num4 / (float)textureSize.X;
			vertices[offset + i].TextureCoordinates.Y = 1f - num5 / (float)textureSize.Y;
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

	private static Vector2 GetShapeTextureFaceSize(ref BlockyModelNode node, string faceName)
	{
		switch (node.Type)
		{
		case BlockyModelNode.ShapeType.Box:
			switch (faceName)
			{
			case "front":
			case "back":
				return new Vector2(node.Size.X, node.Size.Y);
			case "left":
			case "right":
				return new Vector2(node.Size.Z, node.Size.Y);
			case "top":
			case "bottom":
				return new Vector2(node.Size.X, node.Size.Z);
			}
			break;
		case BlockyModelNode.ShapeType.Quad:
			if (!(faceName == "front"))
			{
				break;
			}
			return new Vector2(node.Size.X, node.Size.Y);
		}
		throw new Exception("Unreachable");
	}
}
