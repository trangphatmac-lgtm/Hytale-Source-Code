using System;
using System.Collections.Generic;
using HytaleClient.Graphics;
using HytaleClient.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Utf8Json;
using Utf8Json.Resolvers;

namespace HytaleClient.Data.BlockyModels;

internal class BlockyModelInitializer
{
	private static readonly JsonSerializer JsonSerializerSettings = new JsonSerializer
	{
		ContractResolver = (IContractResolver)new CamelCasePropertyNamesContractResolver()
	};

	public static void Parse(byte[] data, NodeNameManager nodeNameManager, ref BlockyModel blockyModel)
	{
		//IL_001b: Expected O, but got Unknown
		try
		{
			BlockyModelJson json = JsonSerializer.Deserialize<BlockyModelJson>(data, StandardResolver.CamelCase);
			Parse(json, nodeNameManager, ref blockyModel);
		}
		catch (JsonParsingException val)
		{
			JsonParsingException val2 = val;
			throw new Exception(val2.GetUnderlyingStringUnsafe(), (Exception?)(object)val2);
		}
	}

	public static void Parse(JObject jObject, NodeNameManager nodeNameManager, ref BlockyModel blockyModel)
	{
		BlockyModelJson json = ((JToken)jObject).ToObject<BlockyModelJson>(JsonSerializerSettings);
		Parse(json, nodeNameManager, ref blockyModel);
	}

	private static void Parse(BlockyModelJson json, NodeNameManager nodeNameManager, ref BlockyModel blockyModel)
	{
		if (json.Lod != null)
		{
			blockyModel.Lod = ParseLodMode(json.Lod);
		}
		for (int i = 0; i < json.Nodes.Length; i++)
		{
			RecurseParseNode(ref json.Nodes[i], blockyModel, -1, nodeNameManager);
		}
		Array.Resize(ref blockyModel.AllNodes, blockyModel.NodeCount);
		Array.Resize(ref blockyModel.ParentNodes, blockyModel.NodeCount);
	}

	private static void RecurseParseNode(ref BlockyModelNodeJson jsonNode, BlockyModel model, int parentNodeIndex, NodeNameManager nodeNameManager)
	{
		ref NodeShape shape = ref jsonNode.Shape;
		ref NodeShapeSettings settings = ref shape.Settings;
		int orAddNameId = nodeNameManager.GetOrAddNameId(jsonNode.Name);
		BlockyModelNode blockyModelNode = default(BlockyModelNode);
		blockyModelNode.NameId = orAddNameId;
		blockyModelNode.Position = jsonNode.Position;
		blockyModelNode.Orientation = jsonNode.Orientation;
		blockyModelNode.Offset = shape.Offset;
		blockyModelNode.Stretch = shape.Stretch;
		blockyModelNode.Visible = shape.Visible;
		blockyModelNode.DoubleSided = shape.DoubleSided;
		blockyModelNode.ShadingMode = ParseShadingMode(shape.ShadingMode);
		blockyModelNode.Children = new List<int>();
		BlockyModelNode node = blockyModelNode;
		Enum.TryParse<CameraNode>(jsonNode.Name.Replace("-", ""), out node.CameraNode);
		string type = shape.Type;
		IDictionary<string, FaceLayout> textureLayout = shape.TextureLayout;
		switch (type)
		{
		case "none":
			node.Type = BlockyModelNode.ShapeType.None;
			node.IsPiece = settings.IsPiece;
			break;
		case "box":
			node.Type = BlockyModelNode.ShapeType.Box;
			node.Size = settings.Size;
			node.TextureLayout = new BlockyModelFaceTextureLayout[6];
			node.TextureLayout[0] = GetFaceLayout(textureLayout, "front");
			node.TextureLayout[1] = GetFaceLayout(textureLayout, "back");
			node.TextureLayout[2] = GetFaceLayout(textureLayout, "right");
			node.TextureLayout[3] = GetFaceLayout(textureLayout, "bottom");
			node.TextureLayout[4] = GetFaceLayout(textureLayout, "left");
			node.TextureLayout[5] = GetFaceLayout(textureLayout, "top");
			break;
		case "quad":
			node.Type = BlockyModelNode.ShapeType.Quad;
			node.Size = settings.Size;
			node.QuadNormalDirection = ParseQuadNormal(settings.Normal);
			node.TextureLayout = new BlockyModelFaceTextureLayout[1];
			node.TextureLayout[0] = GetFaceLayout(textureLayout, "front");
			break;
		}
		int nodeCount = model.NodeCount;
		model.AddNode(ref node, parentNodeIndex);
		BlockyModelNodeJson[] children = jsonNode.Children;
		if (children != null)
		{
			for (int i = 0; i < children.Length; i++)
			{
				RecurseParseNode(ref children[i], model, nodeCount, nodeNameManager);
			}
		}
	}

	private static LodMode ParseLodMode(string lodMode)
	{
		return lodMode switch
		{
			"auto" => LodMode.Auto, 
			"billboard" => LodMode.Billboard, 
			"disappear" => LodMode.Disappear, 
			_ => LodMode.Off, 
		};
	}

	private static ShadingMode ParseShadingMode(string shadingMode)
	{
		return shadingMode switch
		{
			"flat" => ShadingMode.Flat, 
			"fullbright" => ShadingMode.Fullbright, 
			"reflective" => ShadingMode.Reflective, 
			_ => ShadingMode.Standard, 
		};
	}

	private static BlockyModelNode.QuadNormal ParseQuadNormal(string quadNormal)
	{
		return quadNormal switch
		{
			"-Z" => BlockyModelNode.QuadNormal.MinusZ, 
			"+X" => BlockyModelNode.QuadNormal.PlusX, 
			"-X" => BlockyModelNode.QuadNormal.MinusX, 
			"+Y" => BlockyModelNode.QuadNormal.PlusY, 
			"-Y" => BlockyModelNode.QuadNormal.MinusY, 
			_ => BlockyModelNode.QuadNormal.PlusZ, 
		};
	}

	private static BlockyModelFaceTextureLayout GetFaceLayout(IDictionary<string, FaceLayout> jsonTextureLayout, string faceName)
	{
		BlockyModelFaceTextureLayout result;
		if (!jsonTextureLayout.TryGetValue(faceName, out var value))
		{
			result = default(BlockyModelFaceTextureLayout);
			result.Hidden = true;
			return result;
		}
		result = default(BlockyModelFaceTextureLayout);
		result.Angle = value.Angle;
		result.MirrorX = value.Mirror.X;
		result.MirrorY = value.Mirror.Y;
		result.Offset = value.Offset;
		return result;
	}
}
