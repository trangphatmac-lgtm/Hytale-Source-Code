using System;
using System.Collections.Generic;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Map;
using HytaleClient.Graphics.Particles;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.Data.Map;

internal class ClientBlockType
{
	public enum ClientShaderEffect
	{
		None = 31,
		ParamOn = 32,
		WindAttached = 0,
		WindAttachedMax = 14,
		Wind = 15,
		Ice = 16,
		Water = 17,
		WaterEnvironmentColor = 18,
		WaterEnvironmentTransition = 19,
		Lava = 20,
		Slime = 21,
		Ripple = 22
	}

	public class CubeTexture
	{
		public string[] Names;

		public int[] TileLinearPositionsInAtlas;

		public int Rotation;
	}

	public class BlockyTexture
	{
		public string Name;

		public string Hash;
	}

	public const int EmptyBlockId = 0;

	public const int UnknownBlockId = 1;

	public const int UndefinedBlockId = int.MaxValue;

	public const string EmptyBlockName = "Empty";

	public const string UnknownBlockName = "Unknown";

	public const char VariantTypeSeparator = '|';

	public const char VariantValueSeparator = '=';

	public const string NullStateId = "default";

	public const float MaxBlockHealth = 1f;

	public const byte MaxVerticalFill = 8;

	public int Id;

	public string Name;

	public string Item;

	public bool Unknown;

	public DrawType DrawType;

	public int FillerX;

	public int FillerY;

	public int FillerZ;

	public Rotation RotationYaw;

	public Rotation RotationPitch;

	public Rotation RotationRoll;

	public RandomRotation RandomRotation;

	public Rotation RotationYawPlacementOffset;

	public bool ShouldRenderCube;

	public bool RequiresAlphaBlending;

	public bool IsOccluder;

	public bool HasModel;

	public byte VerticalFill;

	public byte MaxFillLevel;

	public Opacity Opacity;

	public CubeTexture[] CubeTextures;

	public float[] CubeTextureWeights;

	public string CubeSideMaskTexture;

	public int CubeSideMaskTextureAtlasIndex = -1;

	public ShadingMode CubeShadingMode;

	public string TransitionTexture;

	public int TransitionTextureAtlasIndex = -1;

	public int TransitionGroupId = -1;

	public int[] TransitionToGroupIds;

	public BlockyTexture[] BlockyTextures;

	public float[] BlockyTextureWeights;

	public string BlockyModelHash;

	public float BlockyModelScale = 1f;

	public BlockyModel OriginalBlockyModel;

	public BlockyModel FinalBlockyModel;

	public BlockyAnimation BlockyAnimation;

	public ModelParticleSettings[] Particles;

	public RenderedStaticBlockyModel RenderedBlockyModel;

	public Vector2[] RenderedBlockyModelTextureOrigins;

	public ChunkGeometryData VertexData;

	public Matrix WorldMatrix;

	public Matrix RotationMatrix;

	public Matrix BlockyModelTranslatedScaleMatrix;

	public Matrix CubeBlockInvertMatrix;

	public int[] SelfTintColorsBySide;

	public float[] BiomeTintMultipliersBySide;

	public ColorRgb LightEmitted;

	public Material CollisionMaterial;

	public int HitboxType;

	public BlockMovementSettings MovementSettings;

	public bool IsUsable;

	public string InteractionHint;

	public BlockGathering Gathering;

	public ClientShaderEffect CubeShaderEffect;

	public ClientShaderEffect BlockyModelShaderEffect;

	public int FluidBlockId;

	public int FluidFXIndex;

	public readonly Dictionary<string, int> Variants = new Dictionary<string, int>();

	public string BlockParticleSetId;

	public UInt32Color ParticleColor = UInt32Color.Transparent;

	public int BlockSoundSetIndex;

	public Dictionary<InteractionType, int> Interactions;

	public VariantRotation VariantRotation;

	public int VariantOriginalId;

	public BlockConnections Connections;

	public uint SoundEventIndex;

	public bool Looping;

	public Dictionary<string, int> States;

	public Dictionary<int, string> StatesReverse;

	public Dictionary<int, int[]> TagIndexes;

	public static string GetOriginalBlockName(string name)
	{
		int num = name.IndexOf('|');
		return (num == -1) ? name : name.Substring(0, num);
	}

	public static Dictionary<string, string> GetBlockVariantData(string name)
	{
		string[] array = name.Split(new char[1] { '|' });
		if (array.Length < 2)
		{
			return null;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		for (int i = 1; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(new char[1] { '=' });
			if (array2.Length < 2)
			{
				throw new Exception($"Invalid variant data for block '{name}' - Missing value separator character '{'='}'");
			}
			dictionary.Add(array2[0], array2[1]);
		}
		return dictionary;
	}

	public bool IsAnimated()
	{
		return BlockyAnimation != null;
	}

	public bool IsConnectable()
	{
		return Connections?.ConnectableBlocks != null && Connections.ConnectableBlocks.Length != 0 && Connections.Outputs != null && Connections.Outputs.Count > 0;
	}

	public int TryGetRotatedVariant(Rotation yaw = 0, Rotation pitch = 0, Rotation roll = 0)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Invalid comparison between Unknown and I4
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected I4, but got Unknown
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Invalid comparison between Unknown and I4
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected I4, but got Unknown
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected I4, but got Unknown
		string text = "";
		if ((int)yaw > 0)
		{
			text += $"Yaw={yaw * 90}";
		}
		if ((int)pitch > 0)
		{
			if (text != "")
			{
				text += "|";
			}
			text += $"Pitch={pitch * 90}";
		}
		if ((int)roll > 0)
		{
			if (text != "")
			{
				text += "|";
			}
			text += $"Roll={roll * 90}";
		}
		if (text == "")
		{
			return Id;
		}
		int value;
		return Variants.TryGetValue(text, out value) ? value : Id;
	}
}
