using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HytaleClient.Data.Map;
using HytaleClient.Math;
using NLog;

namespace HytaleClient.Data.BlockyModels;

internal class BlockyModel
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public const int EmptyNodeNameId = -1;

	public const int NodeGrowthAmount = 5;

	public static int MaxNodeCount = 256;

	public readonly List<int> RootNodes = new List<int>();

	public BlockyModelNode[] AllNodes = new BlockyModelNode[0];

	public readonly Dictionary<int, int> NodeIndicesByNameId = new Dictionary<int, int>();

	public int[] ParentNodes = new int[0];

	public LodMode Lod = LodMode.Auto;

	public byte GradientId;

	public int NodeCount { get; private set; }

	public BlockyModel(int preAllocatedNodeCount)
	{
		AllNodes = new BlockyModelNode[preAllocatedNodeCount];
		ParentNodes = new int[preAllocatedNodeCount];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AddNode(ref BlockyModelNode node, int parentNodeIndex = -1)
	{
		if (NodeCount >= MaxNodeCount)
		{
			Logger.Warn("Trying to setup a model with more than {0} nodes", MaxNodeCount);
			return;
		}
		EnsureNodeCountAllocated(1, 5);
		ParentNodes[NodeCount] = parentNodeIndex;
		if (parentNodeIndex != -1)
		{
			AllNodes[parentNodeIndex].Children.Add(NodeCount);
		}
		else
		{
			RootNodes.Add(NodeCount);
		}
		AllNodes[NodeCount] = node;
		if (!NodeIndicesByNameId.ContainsKey(node.NameId))
		{
			NodeIndicesByNameId[node.NameId] = NodeCount;
		}
		NodeCount++;
	}

	public void AddMapBlockNode(ClientBlockType clientBlockType, int blockNameId, int sideMaskNameId, int mapAtlasWidth)
	{
		EnsureNodeCountAllocated(2);
		float num = (float)(int)clientBlockType.VerticalFill / (float)((clientBlockType.MaxFillLevel == 0) ? 8 : clientBlockType.MaxFillLevel);
		BlockyModelNode node = BlockyModelNode.CreateMapBlockNode(blockNameId, 16f * num, num);
		node.ShadingMode = clientBlockType.CubeShadingMode;
		for (int i = 0; i < 6; i++)
		{
			int num2 = i switch
			{
				4 => 0, 
				5 => 1, 
				3 => 2, 
				1 => 3, 
				2 => 4, 
				0 => 5, 
				_ => throw new Exception("Can't be reached"), 
			};
			int num3 = clientBlockType.CubeTextures[i].TileLinearPositionsInAtlas[0] * 32;
			node.TextureLayout[num2].Offset.X = num3 % mapAtlasWidth;
			node.TextureLayout[num2].Offset.Y = num3 / mapAtlasWidth * 32;
			if (i >= 2)
			{
				node.TextureLayout[num2].Offset.Y += (int)((1f - num) * 32f);
			}
		}
		AddNode(ref node);
		int cubeSideMaskTextureAtlasIndex = clientBlockType.CubeSideMaskTextureAtlasIndex;
		if (cubeSideMaskTextureAtlasIndex == -1)
		{
			return;
		}
		BlockyModelNode node2 = BlockyModelNode.CreateMapBlockNode(sideMaskNameId, 0f, num);
		cubeSideMaskTextureAtlasIndex *= 32;
		for (int j = 0; j < 6; j++)
		{
			int num4 = j switch
			{
				4 => 0, 
				5 => 1, 
				3 => 2, 
				1 => 3, 
				2 => 4, 
				0 => 5, 
				_ => throw new Exception("Can't be reached"), 
			};
			if (j == 0 || j == 1)
			{
				node2.TextureLayout[num4].Hidden = true;
				continue;
			}
			node2.TextureLayout[num4].Offset.X = cubeSideMaskTextureAtlasIndex % mapAtlasWidth;
			node2.TextureLayout[num4].Offset.Y = (cubeSideMaskTextureAtlasIndex / mapAtlasWidth + (int)(1f - num)) * 32;
		}
		AddNode(ref node2, NodeCount - 1);
	}

	public BlockyModel Clone()
	{
		BlockyModel blockyModel = new BlockyModel(NodeCount);
		for (int i = 0; i < NodeCount; i++)
		{
			BlockyModelNode node = AllNodes[i].Clone();
			blockyModel.AddNode(ref node, ParentNodes[i]);
		}
		return blockyModel;
	}

	public BlockyModel CloneArmsAndLegs(int rightArmNameId, int rightForeamrNameId, int leftArmNameId, int leftForearmNameId, int rightThighNameId, int leftThighNameId)
	{
		BlockyModel blockyModel = new BlockyModel(MaxNodeCount);
		if (NodeIndicesByNameId.TryGetValue(rightArmNameId, out var value))
		{
			RecurseCloneNode(this, blockyModel, value, -1);
			int num = blockyModel.NodeIndicesByNameId[rightArmNameId];
			ref BlockyModelNode reference = ref blockyModel.AllNodes[num];
			reference.Position.X = 0f;
			reference.Position.Y = 0f;
			reference.Position.Z = -32f;
			reference.Orientation = Quaternion.Identity;
			if (blockyModel.NodeIndicesByNameId.TryGetValue(rightForeamrNameId, out var value2))
			{
				blockyModel.AllNodes[value2].Orientation = Quaternion.Identity;
			}
		}
		if (NodeIndicesByNameId.TryGetValue(leftArmNameId, out var value3))
		{
			RecurseCloneNode(this, blockyModel, value3, -1);
			int num2 = blockyModel.NodeIndicesByNameId[leftArmNameId];
			ref BlockyModelNode reference2 = ref blockyModel.AllNodes[num2];
			reference2.Position.X = 0f;
			reference2.Position.Y = 0f;
			reference2.Position.Z = -32f;
			reference2.Orientation = Quaternion.Identity;
			if (blockyModel.NodeIndicesByNameId.TryGetValue(leftForearmNameId, out var value4))
			{
				blockyModel.AllNodes[value4].Orientation = Quaternion.Identity;
			}
		}
		if (NodeIndicesByNameId.TryGetValue(rightThighNameId, out var value5))
		{
			RecurseCloneNode(this, blockyModel, value5, -1);
			int num3 = blockyModel.NodeIndicesByNameId[rightThighNameId];
			ref BlockyModelNode reference3 = ref blockyModel.AllNodes[num3];
			reference3.Position.X = 0f;
			reference3.Position.Y = 0f;
			reference3.Position.Z = -32f;
			reference3.Orientation = Quaternion.Identity;
		}
		if (NodeIndicesByNameId.TryGetValue(leftThighNameId, out var value6))
		{
			RecurseCloneNode(this, blockyModel, value6, -1);
			int num4 = blockyModel.NodeIndicesByNameId[leftThighNameId];
			ref BlockyModelNode reference4 = ref blockyModel.AllNodes[num4];
			reference4.Position.X = 0f;
			reference4.Position.Y = 0f;
			reference4.Position.Z = -32f;
			reference4.Orientation = Quaternion.Identity;
		}
		Array.Resize(ref blockyModel.AllNodes, blockyModel.NodeCount);
		Array.Resize(ref blockyModel.ParentNodes, blockyModel.NodeCount);
		return blockyModel;
	}

	private static void RecurseCloneNode(BlockyModel original, BlockyModel clone, int originalNodeIndex, int parentNodeIndex)
	{
		int nodeCount = clone.NodeCount;
		BlockyModelNode node = original.AllNodes[originalNodeIndex].Clone();
		clone.AddNode(ref node, parentNodeIndex);
		List<int> children = original.AllNodes[originalNodeIndex].Children;
		clone.EnsureNodeCountAllocated(children.Count);
		foreach (int item in children)
		{
			RecurseCloneNode(original, clone, item, nodeCount);
		}
	}

	public void Attach(BlockyModel attachment, NodeNameManager nodeNameManager, byte? atlasIndex = null, Point? uvOffset = null, int forcedTargetNodeNameId = -1)
	{
		EnsureNodeCountAllocated(attachment.NodeCount);
		if (!NodeIndicesByNameId.TryGetValue(forcedTargetNodeNameId, out var value))
		{
			value = -1;
		}
		for (int i = 0; i < attachment.RootNodes.Count; i++)
		{
			int num = attachment.RootNodes[i];
			RecurseAttach(attachment, ref attachment.AllNodes[num], value, nodeNameManager, atlasIndex, uvOffset, value != -1);
		}
	}

	private void RecurseAttach(BlockyModel attachment, ref BlockyModelNode attachmentNode, int parentNodeIndex, NodeNameManager nodeNameManager, byte? atlasIndex, Point? uvOffset, bool forcedAttachment)
	{
		if (!forcedAttachment && attachmentNode.IsPiece)
		{
			if (NodeIndicesByNameId.TryGetValue(attachmentNode.NameId, out var value))
			{
				for (int i = 0; i < attachmentNode.Children.Count; i++)
				{
					int num = attachmentNode.Children[i];
					RecurseAttach(attachment, ref attachment.AllNodes[num], value, nodeNameManager, atlasIndex, uvOffset, forcedAttachment);
				}
				return;
			}
			if (!nodeNameManager.TryGetNameFromId(attachmentNode.NameId, out var nodeName))
			{
				throw new Exception("Node name not found in manager");
			}
			Logger.Warn("Couldn't find attachment target: {0}", nodeName);
		}
		int nodeCount = NodeCount;
		BlockyModelNode node = attachmentNode.Clone();
		if (atlasIndex.HasValue)
		{
			node.AtlasIndex = atlasIndex.Value;
		}
		node.GradientId = attachment.GradientId;
		if (uvOffset.HasValue && node.TextureLayout != null)
		{
			for (int j = 0; j < node.TextureLayout.Length; j++)
			{
				node.TextureLayout[j].Offset.X += uvOffset.Value.X;
				node.TextureLayout[j].Offset.Y += uvOffset.Value.Y;
			}
		}
		AddNode(ref node, parentNodeIndex);
		for (int k = 0; k < attachmentNode.Children.Count; k++)
		{
			int num2 = attachmentNode.Children[k];
			RecurseAttach(attachment, ref attachment.AllNodes[num2], nodeCount, nodeNameManager, atlasIndex, uvOffset, forcedAttachment);
		}
	}

	public void SetAtlasIndex(byte atlasIndex)
	{
		for (int i = 0; i < NodeCount; i++)
		{
			AllNodes[i].AtlasIndex = atlasIndex;
		}
	}

	public void SetGradientId(byte gradientId)
	{
		for (int i = 0; i < NodeCount; i++)
		{
			ref BlockyModelNode reference = ref AllNodes[i];
			if (reference.TextureLayout != null)
			{
				reference.GradientId = gradientId;
			}
		}
	}

	public void OffsetUVs(Point offset)
	{
		for (int i = 0; i < NodeCount; i++)
		{
			ref BlockyModelNode reference = ref AllNodes[i];
			if (reference.TextureLayout != null)
			{
				for (int j = 0; j < reference.TextureLayout.Length; j++)
				{
					reference.TextureLayout[j].Offset.X += offset.X;
					reference.TextureLayout[j].Offset.Y += offset.Y;
				}
			}
		}
	}

	private void EnsureNodeCountAllocated(int required, int growth = 0)
	{
		if (AllNodes.Length < NodeCount + required)
		{
			Array.Resize(ref AllNodes, NodeCount + required + growth);
			Array.Resize(ref ParentNodes, NodeCount + required + growth);
		}
	}
}
