using System;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Data.Characters;
using HytaleClient.Data.Items;
using HytaleClient.Data.Map;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Map;
using HytaleClient.Interface.InGame;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Utils;

internal static class ItemPreviewUtils
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public static void CreateBlockGeometry(ClientBlockType blockType, Texture texture)
	{
		float[] array = new float[16];
		float num = 0.05f;
		for (int i = 0; i < 16; i++)
		{
			array[i] = num + (float)System.Math.Pow((float)i / 15f, 1.5) * (1f - num);
		}
		int[] cornerOcclusions = new int[4];
		UShortVector2[] texCoordsByCorner = new UShortVector2[4];
		UShortVector2[] sideMaskTexCoordsByCorner = new UShortVector2[4];
		ClientBlockType.ClientShaderEffect[] cornerShaderEffects = new ClientBlockType.ClientShaderEffect[4];
		uint[] array2 = new uint[1156];
		uint[] array3 = new uint[8];
		for (int j = 0; j < array3.Length; j++)
		{
			array3[j] = 301989887u;
		}
		int alphaTestedLowLODIndicesOffset = 0;
		int? seed = 0;
		int num2 = 32;
		int num3 = ChunkHelper.IndexOfBlockInBorderedChunk(1, 1, 1);
		int[] array4 = new int[39304];
		ushort[] array5 = new ushort[39304];
		for (int k = 0; k < 39304; k++)
		{
			array5[k] = 61440;
		}
		uint biomeTintColor = (array2[35] = (array2[34] = (array2[1] = (array2[0] = (uint)blockType.SelfTintColorsBySide[0]))));
		array4[num3] = blockType.Id;
		ChunkGeometryBuilder.CreateBlockGeometry(new ClientBlockType[1] { blockType }, array, blockType, num3, num2, Vector3.Zero, 0, 0, 0, ref seed, byte.MaxValue, Matrix.Identity, blockType.RotationMatrix, blockType.CubeBlockInvertMatrix, texCoordsByCorner, sideMaskTexCoordsByCorner, cornerOcclusions, cornerShaderEffects, biomeTintColor, array4, array5, array2, array3, texture.Width, texture.Height, blockType.VertexData, blockType.VertexData, alphaTestedLowLODIndicesOffset, ref alphaTestedLowLODIndicesOffset, isAnimated: true);
	}

	public static ClientBlockType ToClientBlockType(BlockType networkBlockType, JObject modelJson, NodeNameManager nodeNameManager)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Invalid comparison between Unknown and I4
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Invalid comparison between Unknown and I4
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Invalid comparison between Unknown and I4
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Invalid comparison between Unknown and I4
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Invalid comparison between Unknown and I4
		//IL_0554: Unknown result type (might be due to invalid IL or missing references)
		//IL_055f: Unknown result type (might be due to invalid IL or missing references)
		ClientBlockType clientBlockType = new ClientBlockType();
		clientBlockType.DrawType = networkBlockType.DrawType_;
		clientBlockType.CubeTextures = new ClientBlockType.CubeTexture[6];
		clientBlockType.RequiresAlphaBlending = networkBlockType.RequiresAlphaBlending;
		clientBlockType.CubeSideMaskTexture = networkBlockType.CubeSideMaskTexture;
		clientBlockType.VerticalFill = (byte)networkBlockType.VerticalFill;
		clientBlockType.IsOccluder = false;
		clientBlockType.CubeTextureWeights = new float[1] { 1f };
		clientBlockType.BlockyModelScale = networkBlockType.ModelScale;
		clientBlockType.SelfTintColorsBySide = new int[6] { -1, -1, -1, -1, -1, -1 };
		clientBlockType.BiomeTintMultipliersBySide = new float[6];
		clientBlockType.CubeShaderEffect = ClientBlockType.ClientShaderEffect.None;
		clientBlockType.VertexData = new ChunkGeometryData();
		ClientBlockType clientBlockType2 = clientBlockType;
		clientBlockType2.HasModel = (int)clientBlockType2.DrawType == 3 || (int)clientBlockType2.DrawType == 4;
		clientBlockType2.ShouldRenderCube = (int)clientBlockType2.DrawType == 2 || (int)clientBlockType2.DrawType == 4 || (int)clientBlockType2.DrawType == 1;
		BlockTypeProtocolInitializer.ConvertShadingMode(networkBlockType.CubeShadingMode, out clientBlockType2.CubeShadingMode);
		Tint tint_ = networkBlockType.Tint_;
		if (tint_ != null)
		{
			clientBlockType2.SelfTintColorsBySide[0] = tint_.Top;
			clientBlockType2.SelfTintColorsBySide[1] = tint_.Bottom;
			clientBlockType2.SelfTintColorsBySide[2] = tint_.Left;
			clientBlockType2.SelfTintColorsBySide[3] = tint_.Right;
			clientBlockType2.SelfTintColorsBySide[4] = tint_.Front;
			clientBlockType2.SelfTintColorsBySide[5] = tint_.Back;
		}
		if (networkBlockType.CubeTextures != null)
		{
			int num = networkBlockType.CubeTextures.Length;
			for (int i = 0; i < 6; i++)
			{
				clientBlockType2.CubeTextures[i] = new ClientBlockType.CubeTexture
				{
					Names = new string[num],
					TileLinearPositionsInAtlas = new int[System.Math.Max(1, num)]
				};
			}
			for (int j = 0; j < networkBlockType.CubeTextures.Length; j++)
			{
				BlockTextures val = networkBlockType.CubeTextures[j];
				clientBlockType2.CubeTextures[0].Names[j] = val.Top;
				clientBlockType2.CubeTextures[1].Names[j] = val.Bottom;
				clientBlockType2.CubeTextures[2].Names[j] = val.Left;
				clientBlockType2.CubeTextures[3].Names[j] = val.Right;
				clientBlockType2.CubeTextures[4].Names[j] = val.Front;
				clientBlockType2.CubeTextures[5].Names[j] = val.Back;
			}
		}
		else
		{
			for (int k = 0; k < 6; k++)
			{
				clientBlockType2.CubeTextures[k] = new ClientBlockType.CubeTexture
				{
					Names = new string[0],
					TileLinearPositionsInAtlas = new int[1]
				};
			}
		}
		if (clientBlockType2.HasModel)
		{
			clientBlockType2.BlockyTextures = new ClientBlockType.BlockyTexture[System.Math.Max(1, networkBlockType.ModelTexture_.Length)];
			clientBlockType2.BlockyTextureWeights = new float[System.Math.Max(1, networkBlockType.ModelTexture_.Length)];
			if (networkBlockType.ModelTexture_ != null)
			{
				for (int l = 0; l < networkBlockType.ModelTexture_.Length; l++)
				{
					ModelTexture val2 = networkBlockType.ModelTexture_[l];
					if (val2 != null)
					{
						clientBlockType2.BlockyTextures[l] = new ClientBlockType.BlockyTexture
						{
							Name = val2.Texture
						};
						clientBlockType2.BlockyTextureWeights[l] = val2.Weight;
					}
				}
			}
			if (networkBlockType.Model != null)
			{
				BlockyModel blockyModel = new BlockyModel(BlockyModel.MaxNodeCount);
				BlockyModelInitializer.Parse(modelJson, nodeNameManager, ref blockyModel);
				clientBlockType2.OriginalBlockyModel = blockyModel;
			}
			else
			{
				clientBlockType2.OriginalBlockyModel = new BlockyModel(1);
				BlockyModelNode node = BlockyModelNode.CreateMapBlockNode(CharacterPartStore.BlockNameId, 16f, 1f);
				clientBlockType2.OriginalBlockyModel.AddNode(ref node);
			}
			clientBlockType2.RenderedBlockyModel = new RenderedStaticBlockyModel(clientBlockType2.OriginalBlockyModel);
			clientBlockType2.VertexData.VerticesCount += clientBlockType2.RenderedBlockyModel.AnimatedVertices.Length;
			clientBlockType2.VertexData.IndicesCount += clientBlockType2.RenderedBlockyModel.AnimatedIndices.Length;
		}
		clientBlockType2.VertexData.VerticesCount += 24;
		clientBlockType2.VertexData.IndicesCount += 36;
		clientBlockType2.VertexData.Vertices = new ChunkVertex[clientBlockType2.VertexData.VerticesCount];
		clientBlockType2.VertexData.Indices = new uint[clientBlockType2.VertexData.IndicesCount];
		if (clientBlockType2.RenderedBlockyModel != null)
		{
			for (int m = 0; m < clientBlockType2.RenderedBlockyModel.AnimatedIndices.Length; m++)
			{
				clientBlockType2.VertexData.Indices[clientBlockType2.VertexData.IndicesOffset + m] = clientBlockType2.VertexData.VerticesOffset + clientBlockType2.RenderedBlockyModel.AnimatedIndices[m];
			}
			clientBlockType2.VertexData.IndicesOffset += clientBlockType2.RenderedBlockyModel.AnimatedIndices.Length;
		}
		clientBlockType2.VertexData.VerticesOffset = 0u;
		clientBlockType2.VertexData.IndicesOffset = 0;
		if (clientBlockType2.OriginalBlockyModel == null)
		{
			clientBlockType2.OriginalBlockyModel = new BlockyModel(0);
		}
		clientBlockType2.FinalBlockyModel = clientBlockType2.OriginalBlockyModel;
		Matrix.CreateFromYawPitchRoll(MathHelper.RotationToRadians(clientBlockType2.RotationYaw), MathHelper.RotationToRadians(clientBlockType2.RotationPitch), 0f, out clientBlockType2.RotationMatrix);
		Matrix.CreateScale(clientBlockType2.BlockyModelScale * (1f / 32f), out clientBlockType2.BlockyModelTranslatedScaleMatrix);
		Matrix.AddTranslation(ref clientBlockType2.BlockyModelTranslatedScaleMatrix, 0.5f, 0f, 0.5f);
		Matrix.Multiply(ref clientBlockType2.BlockyModelTranslatedScaleMatrix, ref ChunkGeometryBuilder.NegativeHalfBlockOffsetMatrix, out clientBlockType2.WorldMatrix);
		Matrix.Multiply(ref clientBlockType2.WorldMatrix, ref clientBlockType2.RotationMatrix, out clientBlockType2.WorldMatrix);
		Matrix.Multiply(ref clientBlockType2.WorldMatrix, ref ChunkGeometryBuilder.PositiveHalfBlockOffsetMatrix, out clientBlockType2.WorldMatrix);
		Matrix.Invert(ref clientBlockType2.WorldMatrix, out clientBlockType2.CubeBlockInvertMatrix);
		Matrix.AddTranslation(ref clientBlockType2.CubeBlockInvertMatrix, 0f, -16f, 0f);
		return clientBlockType2;
	}

	public static bool TryGetDefaultIconProperties(JObject json, out ClientItemIconProperties iconProperties)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Invalid comparison between Unknown and I4
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Invalid comparison between Unknown and I4
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Invalid comparison between Unknown and I4
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Invalid comparison between Unknown and I4
		try
		{
			ItemArmorSlot? armorSlot = null;
			JToken obj = json["Armor"];
			if (obj != null && (int)obj.Type == 1)
			{
				JToken obj2 = json["Armor"][(object)"ArmorSlot"];
				if (obj2 != null && (int)obj2.Type == 8 && Enum.TryParse<ItemArmorSlot>((string)json["Armor"][(object)"ArmorSlot"], out ItemArmorSlot result))
				{
					armorSlot = result;
				}
			}
			JToken obj3 = json["Weapon"];
			bool isWeapon = obj3 != null && (int)obj3.Type == 1;
			JToken obj4 = json["Tool"];
			iconProperties = IconHelper.GetDefaultIconProperties(isWeapon, obj4 != null && (int)obj4.Type == 1, armorSlot.HasValue, armorSlot);
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to get icon properties");
			iconProperties = null;
			return false;
		}
		return true;
	}

	public static bool TryGetIconProperties(JObject json, out ClientItemIconProperties iconProperties)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Invalid comparison between Unknown and I4
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		if (!TryGetDefaultIconProperties(json, out iconProperties))
		{
			return false;
		}
		try
		{
			JToken obj = json["IconProperties"];
			if (obj != null && (int)obj.Type == 1)
			{
				JObject val = (JObject)json["IconProperties"];
				iconProperties.Scale = (float)val["Scale"];
				if (val.ContainsKey("Rotation"))
				{
					iconProperties.Rotation = new Vector3((float)val["Rotation"][(object)0], (float)val["Rotation"][(object)1], (float)val["Rotation"][(object)2]);
				}
				if (val.ContainsKey("Translation"))
				{
					iconProperties.Translation = new Vector2((float)val["Translation"][(object)0], (float)val["Translation"][(object)1]);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to get icon properties");
			iconProperties = null;
			return false;
		}
		return true;
	}
}
