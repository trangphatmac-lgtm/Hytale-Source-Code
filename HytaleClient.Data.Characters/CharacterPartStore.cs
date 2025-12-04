#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using HytaleClient.Core;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Graphics;
using HytaleClient.Math;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json;
using Zlib;

namespace HytaleClient.Data.Characters;

internal class CharacterPartStore : Disposable
{
	private class ModelTextureInfo
	{
		public string Path;

		public int Width;

		public int Height;
	}

	public static int RightAttachmentNodeNameId = 0;

	public static int LeftAttachmentNodeNameId = 1;

	public static int RightArmNameId = 2;

	public static int LeftArmNameId = 3;

	public static int RightForearmNameId = 4;

	public static int LeftForearmNameId = 5;

	public static int RightThighNameId = 6;

	public static int LeftThighNameId = 7;

	public static int BlockNameId = 8;

	public static int SideMaskNameId = 9;

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public NodeNameManager CharacterNodeNameManager;

	public Dictionary<string, CharacterPartTintColor> EyeColors;

	public Dictionary<string, CharacterPartGradientSet> GradientSets;

	public List<CharacterPart> FacialHair;

	public List<CharacterPart> Ears;

	public List<CharacterPart> Eyes;

	public List<CharacterPart> Mouths;

	public List<CharacterPart> Eyebrows;

	public List<CharacterPart> Faces;

	public List<CharacterHaircutPart> Haircuts;

	public List<CharacterPart> Pants;

	public List<CharacterPart> Overpants;

	public List<CharacterPart> Undertops;

	public List<CharacterPart> Overtops;

	public List<CharacterPart> Shoes;

	public List<CharacterHeadAccessoryPart> HeadAccessory;

	public List<CharacterPart> FaceAccessory;

	public List<CharacterPart> EarAccessory;

	public List<CharacterPart> SkinFeatures;

	public List<CharacterPart> Gloves;

	public List<Emote> Emotes;

	private CancellationToken _initializationCancellationToken;

	public const string FeminineTexture = "Characters/Player_Textures/Feminine_Greyscale.png";

	public const string MasculineTexture = "Characters/Player_Textures/Masculine_Greyscale.png";

	public const string HairGradientSetId = "Hair";

	public const string SkinGradientSetId = "Skin";

	public BlockyAnimation IdleAnimation;

	public Texture CharacterGradientAtlas;

	public readonly Dictionary<string, Point> ImageLocations = new Dictionary<string, Point>();

	public readonly Dictionary<string, BlockyModel> Models = new Dictionary<string, BlockyModel>();

	public readonly Dictionary<string, BlockyAnimation> Animations = new Dictionary<string, BlockyAnimation>();

	public Texture TextureAtlas { get; private set; }

	public Point[] AtlasSizes { get; private set; }

	public CharacterPartStore(GLFunctions gl)
	{
		CreateGPUData();
	}

	public void CreateGPUData()
	{
		TextureAtlas = new Texture(Texture.TextureTypes.Texture2D);
		TextureAtlas.CreateTexture2D(4096, 32, null, 0);
		CharacterGradientAtlas = new Texture(Texture.TextureTypes.Texture2D);
		CharacterGradientAtlas.CreateTexture2D(256, 256, null, 5, GL.NEAREST, GL.NEAREST, GL.MIRRORED_REPEAT, GL.MIRRORED_REPEAT);
	}

	protected override void DoDispose()
	{
		TextureAtlas?.Dispose();
		CharacterGradientAtlas.Dispose();
	}

	private void InitializeImportantNodeNames()
	{
		CharacterNodeNameManager = new NodeNameManager();
		RightAttachmentNodeNameId = CharacterNodeNameManager.GetOrAddNameId("R-Attachment");
		LeftAttachmentNodeNameId = CharacterNodeNameManager.GetOrAddNameId("L-Attachment");
		RightArmNameId = CharacterNodeNameManager.GetOrAddNameId("R-Arm");
		LeftArmNameId = CharacterNodeNameManager.GetOrAddNameId("L-Arm");
		RightForearmNameId = CharacterNodeNameManager.GetOrAddNameId("R-Forearm");
		LeftForearmNameId = CharacterNodeNameManager.GetOrAddNameId("L-Forearm");
		RightThighNameId = CharacterNodeNameManager.GetOrAddNameId("R-Thigh");
		LeftThighNameId = CharacterNodeNameManager.GetOrAddNameId("L-Thigh");
		BlockNameId = CharacterNodeNameManager.GetOrAddNameId("Block");
		SideMaskNameId = CharacterNodeNameManager.GetOrAddNameId("SideMask");
	}

	public void LoadAssets(HashSet<string> updatedCosmeticsAssets, ref bool textureAtlasNeedsUpdate, CancellationToken cancellationToken)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		InitializeImportantNodeNames();
		_initializationCancellationToken = cancellationToken;
		List<CharacterPartTintColor> list = LoadConfig<CharacterPartTintColor>("EyeColors.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		EyeColors = new Dictionary<string, CharacterPartTintColor>();
		foreach (CharacterPartTintColor item in list)
		{
			EyeColors[item.Id] = item;
		}
		List<CharacterPartGradientSet> list2 = LoadConfig<CharacterPartGradientSet>("GradientSets.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		GradientSets = new Dictionary<string, CharacterPartGradientSet>();
		foreach (CharacterPartGradientSet item2 in list2)
		{
			GradientSets[item2.Id] = item2;
		}
		Eyebrows = LoadConfig<CharacterPart>("Eyebrows.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		Faces = LoadConfig<CharacterPart>("Faces.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		Eyes = LoadConfig<CharacterPart>("Eyes.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		Ears = LoadConfig<CharacterPart>("Ears.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		Mouths = LoadConfig<CharacterPart>("Mouths.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		FacialHair = LoadConfig<CharacterPart>("FacialHair.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		Pants = LoadConfig<CharacterPart>("Pants.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		Overpants = LoadConfig<CharacterPart>("Overpants.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		Undertops = LoadConfig<CharacterPart>("Undertops.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		Overtops = LoadConfig<CharacterPart>("Overtops.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		Haircuts = LoadConfig<CharacterHaircutPart>("Haircuts.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		Shoes = LoadConfig<CharacterPart>("Shoes.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		HeadAccessory = LoadConfig<CharacterHeadAccessoryPart>("HeadAccessory.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		FaceAccessory = LoadConfig<CharacterPart>("FaceAccessory.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		EarAccessory = LoadConfig<CharacterPart>("EarAccessory.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		Gloves = LoadConfig<CharacterPart>("Gloves.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		SkinFeatures = LoadConfig<CharacterPart>("SkinFeatures.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
		foreach (CharacterPart eye in Eyes)
		{
			if (eye.Textures == null)
			{
				continue;
			}
			foreach (KeyValuePair<string, CharacterPartTexture> texture in eye.Textures)
			{
				if (EyeColors.TryGetValue(texture.Key, out var value))
				{
					texture.Value.BaseColor = value.BaseColor;
					continue;
				}
				texture.Value.BaseColor = new string[1] { "#000000" };
				Logger.Warn<string, string>("Eye asset '{0}' has reference to an eye color that does not exist ({1})", eye.Id, texture.Key);
			}
		}
		Emotes = LoadConfig<Emote>("Emotes.json", updatedCosmeticsAssets, ref textureAtlasNeedsUpdate);
	}

	public void PrepareGradientAtlas(out byte[][] upcomingGradientAtlasPixelsPerLevel)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		byte[] array = new byte[CharacterGradientAtlas.Width * CharacterGradientAtlas.Height * 4];
		int num = 0;
		foreach (CharacterPartGradientSet value in GradientSets.Values)
		{
			foreach (KeyValuePair<string, CharacterPartTintColor> gradient in value.Gradients)
			{
				Debug.Assert(num < CharacterGradientAtlas.Height, "Maximum number of gradients reached");
				int num2 = num;
				if (gradient.Value?.Texture == null)
				{
					Logger.Error("Gradient set has invalid color: " + gradient.Key);
					continue;
				}
				try
				{
					Image image = new Image(AssetManager.GetBuiltInAsset(Path.Combine("Common", gradient.Value.Texture)));
					for (int i = 0; i < 1; i++)
					{
						int dstOffset = (num2 + i) * CharacterGradientAtlas.Width * 4;
						Buffer.BlockCopy(image.Pixels, i * image.Width * 4, array, dstOffset, image.Width * 4);
					}
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "Failed to load gradient texture: " + gradient.Value.Texture);
				}
				gradient.Value.GradientId = (byte)(num + 1);
				num++;
			}
		}
		upcomingGradientAtlasPixelsPerLevel = Texture.BuildMipmapPixels(array, CharacterGradientAtlas.Width, 0);
	}

	public void BuildGradientTexture(byte[][] upcomingFXAtlasPixelsPerLevel)
	{
		CharacterGradientAtlas.UpdateTexture2DMipMaps(upcomingFXAtlasPixelsPerLevel);
	}

	public string GetBodyModelPath(CharacterBodyType bodyType)
	{
		return "Characters/Player.blockymodel";
	}

	public Emote GetEmote(string id)
	{
		return Emotes.FirstOrDefault((Emote emote) => emote.Id == id);
	}

	public CharacterPart GetDefaultPartFor(CharacterBodyType bodyType, List<CharacterPart> assets)
	{
		return assets.First((CharacterPart asset) => asset.DefaultFor == bodyType);
	}

	public CharacterPartId GetDefaultPartIdFor(CharacterBodyType bodyType, List<CharacterPart> assets)
	{
		CharacterPart defaultPartFor = GetDefaultPartFor(bodyType, assets);
		if (defaultPartFor.Variants != null)
		{
			if (defaultPartFor.GradientSet != null)
			{
				return new CharacterPartId(defaultPartFor.Id, defaultPartFor.Variants.First().Key, GradientSets[defaultPartFor.GradientSet].Gradients.First().Key);
			}
			return new CharacterPartId(defaultPartFor.Id, defaultPartFor.Variants.First().Key, defaultPartFor.Variants.First().Value.Textures.First().Key);
		}
		if (defaultPartFor.GradientSet != null)
		{
			return new CharacterPartId(defaultPartFor.Id, GradientSets[defaultPartFor.GradientSet].Gradients.First().Key);
		}
		return new CharacterPartId(defaultPartFor.Id, defaultPartFor.Textures.First().Key);
	}

	public CharacterPart GetFacialHair(string id)
	{
		return FacialHair.FirstOrDefault((CharacterPart asset) => asset.Id == id);
	}

	public CharacterPart GetMouth(string id)
	{
		return Mouths.FirstOrDefault((CharacterPart asset) => asset.Id == id);
	}

	public CharacterPart GetFace(string id)
	{
		return Faces.FirstOrDefault((CharacterPart asset) => asset.Id == id);
	}

	public CharacterPart GetEyes(string id)
	{
		return Eyes.FirstOrDefault((CharacterPart asset) => asset.Id == id);
	}

	public CharacterPart GetEars(string id)
	{
		return Ears.FirstOrDefault((CharacterPart asset) => asset.Id == id);
	}

	public CharacterPart GetEyebrows(string id)
	{
		return Eyebrows.FirstOrDefault((CharacterPart asset) => asset.Id == id);
	}

	public CharacterHaircutPart GetHaircut(string id)
	{
		return Haircuts.FirstOrDefault((CharacterHaircutPart asset) => asset.Id == id);
	}

	public CharacterPart GetPants(string id)
	{
		return Pants.FirstOrDefault((CharacterPart asset) => asset.Id == id);
	}

	public CharacterPart GetOverpants(string id)
	{
		return Overpants.FirstOrDefault((CharacterPart asset) => asset.Id == id);
	}

	public CharacterPart GetOvertop(string id)
	{
		return Overtops.FirstOrDefault((CharacterPart asset) => asset.Id == id);
	}

	public CharacterPart GetUndertop(string id)
	{
		return Undertops.FirstOrDefault((CharacterPart asset) => asset.Id == id);
	}

	public CharacterPart GetShoes(string id)
	{
		return Shoes.FirstOrDefault((CharacterPart asset) => asset.Id == id);
	}

	public CharacterHeadAccessoryPart GetHeadAccessory(string id)
	{
		return HeadAccessory.FirstOrDefault((CharacterHeadAccessoryPart asset) => asset.Id == id);
	}

	public CharacterPart GetFaceAccessory(string id)
	{
		return FaceAccessory.FirstOrDefault((CharacterPart asset) => asset.Id == id);
	}

	public CharacterPart GetEarAccessory(string id)
	{
		return EarAccessory.FirstOrDefault((CharacterPart asset) => asset.Id == id);
	}

	public CharacterPart GetSkinFeature(string id)
	{
		return SkinFeatures.FirstOrDefault((CharacterPart asset) => asset.Id == id);
	}

	public CharacterPart GetGloves(string id)
	{
		return Gloves.FirstOrDefault((CharacterPart asset) => asset.Id == id);
	}

	public List<string> GetColorOptions(CharacterPart part, string variantId = null)
	{
		List<string> list = new List<string>();
		if (part.GradientSet != null)
		{
			list.AddRange(GradientSets[part.GradientSet].Gradients.Keys);
		}
		if (variantId != null)
		{
			if (part.Variants[variantId].Textures != null)
			{
				list.AddRange(part.Variants[variantId].Textures.Keys);
			}
		}
		else if (part.Textures != null)
		{
			list.AddRange(part.Textures.Keys);
		}
		return list;
	}

	public bool TryGetGradientByIndex(byte index, out string gradientSetId, out string gradientId)
	{
		foreach (KeyValuePair<string, CharacterPartGradientSet> gradientSet in GradientSets)
		{
			foreach (KeyValuePair<string, CharacterPartTintColor> gradient in gradientSet.Value.Gradients)
			{
				if (gradient.Value.GradientId == index)
				{
					gradientSetId = gradientSet.Key;
					gradientId = gradient.Key;
					return true;
				}
			}
		}
		gradientSetId = null;
		gradientId = null;
		return false;
	}

	public bool TryGetCharacterPart(PlayerSkinProperty property, string partId, out CharacterPart characterPart)
	{
		switch (property)
		{
		case PlayerSkinProperty.Haircut:
			characterPart = Haircuts.FirstOrDefault((CharacterHaircutPart p) => p.Id == partId);
			break;
		case PlayerSkinProperty.Eyebrows:
			characterPart = Eyebrows.FirstOrDefault((CharacterPart p) => p.Id == partId);
			break;
		case PlayerSkinProperty.FacialHair:
			characterPart = FacialHair.FirstOrDefault((CharacterPart p) => p.Id == partId);
			break;
		case PlayerSkinProperty.Eyes:
			characterPart = Eyes.FirstOrDefault((CharacterPart p) => p.Id == partId);
			break;
		case PlayerSkinProperty.Pants:
			characterPart = Pants.FirstOrDefault((CharacterPart p) => p.Id == partId);
			break;
		case PlayerSkinProperty.Overpants:
			characterPart = Overpants.FirstOrDefault((CharacterPart p) => p.Id == partId);
			break;
		case PlayerSkinProperty.Undertop:
			characterPart = Undertops.FirstOrDefault((CharacterPart p) => p.Id == partId);
			break;
		case PlayerSkinProperty.Overtop:
			characterPart = Overtops.FirstOrDefault((CharacterPart p) => p.Id == partId);
			break;
		case PlayerSkinProperty.Shoes:
			characterPart = Shoes.FirstOrDefault((CharacterPart p) => p.Id == partId);
			break;
		case PlayerSkinProperty.HeadAccessory:
			characterPart = HeadAccessory.FirstOrDefault((CharacterHeadAccessoryPart p) => p.Id == partId);
			break;
		case PlayerSkinProperty.FaceAccessory:
			characterPart = FaceAccessory.FirstOrDefault((CharacterPart p) => p.Id == partId);
			break;
		case PlayerSkinProperty.EarAccessory:
			characterPart = EarAccessory.FirstOrDefault((CharacterPart p) => p.Id == partId);
			break;
		case PlayerSkinProperty.SkinFeature:
			characterPart = SkinFeatures.FirstOrDefault((CharacterPart p) => p.Id == partId);
			break;
		case PlayerSkinProperty.Gloves:
			characterPart = Gloves.FirstOrDefault((CharacterPart p) => p.Id == partId);
			break;
		default:
			characterPart = null;
			return false;
		}
		return characterPart != null;
	}

	public List<CharacterAttachment> GetCharacterAttachments(ClientPlayerSkin skin)
	{
		if (skin == null)
		{
			return new List<CharacterAttachment>();
		}
		string text = skin.SkinTone;
		if (text == null || !GradientSets["Skin"].Gradients.ContainsKey(text))
		{
			text = GradientSets["Skin"].Gradients.FirstOrDefault().Key;
		}
		List<CharacterAttachment> list = new List<CharacterAttachment>();
		string partId = ((skin.BodyType == CharacterBodyType.Feminine) ? "NormalFemale" : "NormalMale");
		GetAttachment(list, new CharacterPartId(skin.Face, skin.SkinTone), GetFace);
		GetAttachment(list, new CharacterPartId(partId, text), GetMouth);
		GetAttachment(list, new CharacterPartId("Normal", text), GetEars);
		GetAttachment(list, skin.Eyes, GetEyes);
		GetAttachment(list, skin.SkinFeature, GetSkinFeature);
		GetAttachment(list, skin.Pants, GetPants);
		GetAttachment(list, skin.Overpants, GetOverpants);
		GetAttachment(list, skin.Shoes, GetShoes);
		GetAttachment(list, skin.Undertop, GetUndertop);
		GetAttachment(list, skin.Overtop, GetOvertop);
		GetAttachment(list, skin.Gloves, GetGloves);
		GetAttachment(list, skin.Eyebrows, GetEyebrows);
		GetAttachment(list, skin.HeadAccessory, GetHeadAccessory);
		GetAttachment(list, skin.FaceAccessory, GetFaceAccessory);
		GetAttachment(list, skin.EarAccessory, GetEarAccessory);
		GetHaircutAttachment(list, skin);
		GetAttachment(list, skin.FacialHair, GetFacialHair);
		return list;
	}

	public List<CharacterPart> GetParts(PlayerSkinProperty property)
	{
		return property switch
		{
			PlayerSkinProperty.FacialHair => FacialHair, 
			PlayerSkinProperty.Eyebrows => Eyebrows, 
			PlayerSkinProperty.Eyes => Eyes, 
			PlayerSkinProperty.Face => Faces, 
			PlayerSkinProperty.Haircut => new List<CharacterPart>(Haircuts), 
			PlayerSkinProperty.Pants => Pants, 
			PlayerSkinProperty.Overpants => Overpants, 
			PlayerSkinProperty.Undertop => Undertops, 
			PlayerSkinProperty.Overtop => Overtops, 
			PlayerSkinProperty.Shoes => Shoes, 
			PlayerSkinProperty.Gloves => Gloves, 
			PlayerSkinProperty.HeadAccessory => new List<CharacterPart>(HeadAccessory), 
			PlayerSkinProperty.FaceAccessory => FaceAccessory, 
			PlayerSkinProperty.EarAccessory => EarAccessory, 
			_ => null, 
		};
	}

	public List<string> GetTags(List<CharacterPart> parts)
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (CharacterPart part in parts)
		{
			if (part.Tags != null)
			{
				string[] tags = part.Tags;
				foreach (string item in tags)
				{
					hashSet.Add(item);
				}
			}
		}
		List<string> list = hashSet.ToList();
		list.Sort();
		return list;
	}

	private void GetHaircutAttachment(ICollection<CharacterAttachment> attachments, ClientPlayerSkin skin)
	{
		if (skin.Haircut == null)
		{
			return;
		}
		if (skin.HeadAccessory != null)
		{
			CharacterHeadAccessoryPart headAccessory = GetHeadAccessory(skin.HeadAccessory.PartId);
			if (headAccessory != null)
			{
				if (headAccessory.HeadAccessoryType == CharacterHeadAccessoryPart.CharacterHeadAccessoryType.HalfCovering)
				{
					CharacterHaircutPart haircut = GetHaircut(skin.Haircut.PartId);
					if (haircut.RequiresGenericHaircut)
					{
						CharacterHaircutPart baseHaircut = GetHaircut($"Generic{haircut.HairType}");
						List<string> colorOptions = GetColorOptions(baseHaircut);
						GetAttachment(attachments, new CharacterPartId(baseHaircut.Id, colorOptions.Contains(skin.Haircut.ColorId) ? skin.Haircut.ColorId : colorOptions.First()), (string _) => baseHaircut);
						return;
					}
				}
				else if (headAccessory.HeadAccessoryType == CharacterHeadAccessoryPart.CharacterHeadAccessoryType.FullyCovering)
				{
					GetAttachment(attachments, skin.Haircut, GetHaircut, usesBaseModel: true);
					return;
				}
			}
		}
		GetAttachment(attachments, skin.Haircut, GetHaircut);
	}

	private void GetAttachment(ICollection<CharacterAttachment> attachments, CharacterPartId assetId, Func<string, CharacterPart> getterFunc, bool usesBaseModel = false)
	{
		if (assetId == null)
		{
			return;
		}
		CharacterPart characterPart = getterFunc(assetId.PartId);
		if (characterPart != null)
		{
			Dictionary<string, CharacterPartTexture> textures = characterPart.Textures;
			string model = characterPart.Model;
			string greyscaleTexture = characterPart.GreyscaleTexture;
			if (characterPart.Variants != null)
			{
				if (!characterPart.Variants.TryGetValue((assetId.VariantId != null) ? assetId.VariantId : characterPart.Variants.First().Key, out var value))
				{
					Logger.Warn<string, string>("Invalid variant for character part {0}: {1}", characterPart.Id, assetId.VariantId);
					return;
				}
				textures = value.Textures;
				model = value.Model;
				greyscaleTexture = value.GreyscaleTexture;
			}
			CharacterPartTintColor value3;
			if (textures != null && textures.TryGetValue(assetId.ColorId, out var value2))
			{
				attachments.Add(new CharacterAttachment(model, value2.Texture, usesBaseModel, 0));
			}
			else if (characterPart.GradientSet != null && GradientSets[characterPart.GradientSet].Gradients.TryGetValue(assetId.ColorId, out value3))
			{
				attachments.Add(new CharacterAttachment(model, greyscaleTexture, usesBaseModel, value3.GradientId));
			}
			else
			{
				Logger.Warn<string, string>("Invalid texture for character part {0}: {1}", characterPart.Id, assetId.ColorId);
			}
		}
		else
		{
			Logger.Warn<CharacterPartId>("Invalid texture for character part {0}", assetId);
		}
	}

	private List<T> LoadConfig<T>(string file, HashSet<string> updatedAssets, ref bool textureAtlasNeedsUpdate)
	{
		if (!textureAtlasNeedsUpdate && updatedAssets.Contains(Path.Combine("CharacterCreator", file)))
		{
			textureAtlasNeedsUpdate = true;
		}
		return JsonConvert.DeserializeObject<List<T>>(GetJson(file));
	}

	private string GetJson(string file)
	{
		return Encoding.UTF8.GetString(AssetManager.GetBuiltInAsset("Cosmetics/CharacterCreator/" + file));
	}

	public void LoadModelData(Engine engine, HashSet<string> updatedCommonAssets, bool textureAtlasNeedsUpdate)
	{
		IdleAnimation = new BlockyAnimation();
		BlockyAnimationInitializer.Parse(AssetManager.GetBuiltInAsset("Common/Characters/Animations/Default/Idle.blockyanim"), CharacterNodeNameManager, ref IdleAnimation);
		LoadModel(GetBodyModelPath(CharacterBodyType.Masculine));
		LoadModel(GetBodyModelPath(CharacterBodyType.Feminine));
		IEnumerable<CharacterPart> enumerable = Eyebrows.Concat(Eyes).Concat(Faces).Concat(Ears)
			.Concat(Mouths)
			.Concat(FacialHair)
			.Concat(Pants)
			.Concat(Overpants)
			.Concat(Undertops)
			.Concat(Overtops)
			.Concat(Haircuts)
			.Concat(Shoes)
			.Concat(HeadAccessory)
			.Concat(FaceAccessory)
			.Concat(EarAccessory)
			.Concat(Gloves)
			.Concat(SkinFeatures);
		List<string> list = new List<string> { "Characters/Player_Textures/Masculine_Greyscale.png", "Characters/Player_Textures/Feminine_Greyscale.png" };
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		foreach (CharacterPart item in enumerable)
		{
			if (item.Variants != null)
			{
				foreach (CharacterPartVariant value in item.Variants.Values)
				{
					LoadModel(value.Model);
					if (value.Textures != null)
					{
						foreach (CharacterPartTexture value2 in value.Textures.Values)
						{
							list.Add(value2.Texture);
						}
					}
					if (value.GreyscaleTexture != null)
					{
						list.Add(value.GreyscaleTexture);
					}
				}
				continue;
			}
			LoadModel(item.Model);
			if (item.Textures != null)
			{
				foreach (CharacterPartTexture value3 in item.Textures.Values)
				{
					list.Add(value3.Texture);
				}
			}
			if (item.GreyscaleTexture != null)
			{
				list.Add(item.GreyscaleTexture);
			}
		}
		foreach (Emote emote in Emotes)
		{
			BlockyAnimation blockyAnimation = new BlockyAnimation();
			BlockyAnimationInitializer.Parse(AssetManager.GetBuiltInAsset(Path.Combine("Common", emote.Animation)), CharacterNodeNameManager, ref blockyAnimation);
			Animations[emote.Animation] = blockyAnimation;
		}
		stopwatch.Stop();
		Logger.Info("Took {0}s to load character part models", stopwatch.Elapsed.TotalMilliseconds / 1000.0);
		bool flag = textureAtlasNeedsUpdate;
		if (!flag)
		{
			char directorySeparatorChar = Path.DirectorySeparatorChar;
			string newValue = directorySeparatorChar.ToString();
			foreach (string item2 in list)
			{
				if (updatedCommonAssets.Contains(item2.Replace("/", newValue)))
				{
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			flag = !File.Exists(Path.Combine(Paths.BuiltInAssets, "CharacterTextureAtlasLocations.cache"));
			for (int i = 0; i < TextureAtlas.MipmapLevelCount + 1; i++)
			{
				if (!File.Exists(Path.Combine(Paths.BuiltInAssets, "CharacterTextureAtlas" + i + ".cache")))
				{
					flag = true;
					break;
				}
			}
		}
		byte[][] upcomingAtlasPixelsPerLevel;
		if (flag && !AssetManager.IsAssetsDirectoryImmutable)
		{
			upcomingAtlasPixelsPerLevel = GenerateAtlas(engine, list);
		}
		else
		{
			try
			{
				upcomingAtlasPixelsPerLevel = LoadCachedAtlas(engine);
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to load cached character atlas:");
				upcomingAtlasPixelsPerLevel = GenerateAtlas(engine, list);
			}
		}
		engine.RunOnMainThread(this, delegate
		{
			if (!_initializationCancellationToken.IsCancellationRequested)
			{
				TextureAtlas.UpdateTexture2DMipMaps(upcomingAtlasPixelsPerLevel);
			}
		});
	}

	private byte[][] GenerateAtlas(Engine engine, List<string> textures)
	{
		Logger.Info("Re-generating character part atlas...");
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		PrepareAtlas(textures, out var upcomingAtlasPixelsPerLevel);
		stopwatch.Stop();
		Logger.Info("Took {0} to regenerate character part atlas", stopwatch.Elapsed.TotalMilliseconds / 1000.0);
		return upcomingAtlasPixelsPerLevel;
	}

	private byte[][] LoadCachedAtlas(Engine engine)
	{
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Expected O, but got Unknown
		Logger.Info("Loading cached character part atlas...");
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		string[] array = File.ReadAllLines(Path.Combine(Paths.BuiltInAssets, "CharacterTextureAtlasLocations.cache"));
		int x = int.Parse(array[0].Split(new char[1] { ' ' })[0]);
		int y = int.Parse(array[0].Split(new char[1] { ' ' })[1]);
		AtlasSizes = new Point[1]
		{
			new Point(x, y)
		};
		ImageLocations.Clear();
		for (int i = 1; i < array.Length; i++)
		{
			string text = array[i];
			if (!(text == ""))
			{
				string[] array2 = text.Split(new char[1] { ' ' }, 3);
				string key = array2[2];
				ImageLocations.Add(key, new Point(int.Parse(array2[0]), int.Parse(array2[1])));
			}
		}
		byte[][] array3 = new byte[TextureAtlas.MipmapLevelCount + 1][];
		for (int j = 0; j < TextureAtlas.MipmapLevelCount + 1; j++)
		{
			MemoryStream memoryStream = new MemoryStream();
			byte[] array4;
			using (FileStream fileStream = File.Open(Path.Combine(Paths.BuiltInAssets, "CharacterTextureAtlas" + j + ".cache"), FileMode.Open))
			{
				if (OptionsHelper.DisableCharacterAtlasCompression)
				{
					fileStream.CopyTo(memoryStream);
					fileStream.Close();
					memoryStream.Position = 0L;
					array4 = memoryStream.ToArray();
				}
				else
				{
					ZLibStream val = new ZLibStream((Stream)fileStream, CompressionMode.Decompress);
					try
					{
						((Stream)(object)val).CopyTo((Stream)memoryStream);
						((Stream)(object)val).Close();
						memoryStream.Position = 0L;
						array4 = memoryStream.ToArray();
					}
					finally
					{
						((IDisposable)val)?.Dispose();
					}
				}
			}
			array3[j] = array4;
		}
		stopwatch.Stop();
		Logger.Info("Took {0} to load cached character part atlas", stopwatch.Elapsed.TotalMilliseconds / 1000.0);
		return array3;
	}

	public BlockyAnimation GetAnimation(string path)
	{
		if (!Animations.TryGetValue(path, out var value))
		{
			return null;
		}
		return value;
	}

	public BlockyModel GetAndCloneModel(string path)
	{
		if (!Models.TryGetValue(path, out var value))
		{
			return null;
		}
		return value.Clone();
	}

	private void LoadModel(string path)
	{
		try
		{
			BlockyModel blockyModel = new BlockyModel(BlockyModel.MaxNodeCount);
			BlockyModelInitializer.Parse(AssetManager.GetBuiltInAsset(Path.Combine("Common", path)), CharacterNodeNameManager, ref blockyModel);
			Models[path] = blockyModel;
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Failed to parse model " + path);
		}
	}

	public void PrepareAtlas(List<string> textures, out byte[][] upcomingAtlasPixelsPerLevel)
	{
		//IL_0509: Unknown result type (might be due to invalid IL or missing references)
		//IL_0510: Expected O, but got Unknown
		Debug.Assert(!ThreadHelper.IsMainThread());
		ImageLocations.Clear();
		Dictionary<string, ModelTextureInfo> dictionary = new Dictionary<string, ModelTextureInfo>();
		foreach (string texture in textures)
		{
			if (_initializationCancellationToken.IsCancellationRequested)
			{
				upcomingAtlasPixelsPerLevel = null;
				return;
			}
			if (dictionary.TryGetValue(texture, out var value))
			{
				continue;
			}
			value = new ModelTextureInfo
			{
				Path = texture
			};
			if (Image.TryGetPngDimensions(Path.Combine(Paths.BuiltInAssets, "Common", texture), out value.Width, out value.Height))
			{
				dictionary[texture] = value;
				if (value.Width % 32 != 0 || value.Height % 32 != 0 || value.Width < 32 || value.Height < 32)
				{
					Logger.Info<string, int, int>("Texture width/height must be a multiple of 32 and at least 32x32: {0} ({1}x{2})", texture, value.Width, value.Height);
				}
			}
			else
			{
				Logger.Info("Failed to get PNG dimensions for: {0}", texture);
			}
		}
		List<ModelTextureInfo> list = new List<ModelTextureInfo>(dictionary.Values);
		list.Sort((ModelTextureInfo a, ModelTextureInfo b) => b.Height.CompareTo(a.Height));
		Point zero = Point.Zero;
		int num = 0;
		int num2 = 512;
		foreach (ModelTextureInfo item in list)
		{
			if (_initializationCancellationToken.IsCancellationRequested)
			{
				upcomingAtlasPixelsPerLevel = null;
				return;
			}
			if (zero.X + item.Width > TextureAtlas.Width)
			{
				zero.X = 0;
				zero.Y = num;
			}
			while (zero.Y + item.Height > num2)
			{
				num2 <<= 1;
			}
			ImageLocations.Add(item.Path, zero);
			num = System.Math.Max(num, zero.Y + item.Height);
			zero.X += item.Width;
		}
		AtlasSizes = new Point[1]
		{
			new Point(TextureAtlas.Width, num2)
		};
		byte[] array = new byte[TextureAtlas.Width * num2 * 4];
		zero = Point.Zero;
		foreach (ModelTextureInfo item2 in list)
		{
			if (_initializationCancellationToken.IsCancellationRequested)
			{
				upcomingAtlasPixelsPerLevel = null;
				return;
			}
			try
			{
				Image image = new Image(AssetManager.GetBuiltInAsset(Path.Combine("Common", item2.Path)));
				for (int i = 0; i < image.Height; i++)
				{
					Point point = ImageLocations[item2.Path];
					int dstOffset = ((point.Y + i) * TextureAtlas.Width + point.X) * 4;
					Buffer.BlockCopy(image.Pixels, i * image.Width * 4, array, dstOffset, image.Width * 4);
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to load model texture: " + item2.Path);
			}
		}
		using (StreamWriter streamWriter = new StreamWriter(Path.Combine(Paths.BuiltInAssets, "CharacterTextureAtlasLocations.cache")))
		{
			streamWriter.WriteLine(TextureAtlas.Width + " " + num2);
			foreach (KeyValuePair<string, Point> imageLocation in ImageLocations)
			{
				streamWriter.WriteLine(imageLocation.Value.X + " " + imageLocation.Value.Y + " " + imageLocation.Key);
			}
		}
		upcomingAtlasPixelsPerLevel = Texture.BuildMipmapPixels(array, TextureAtlas.Width, TextureAtlas.MipmapLevelCount);
		for (int j = 0; j < upcomingAtlasPixelsPerLevel.Length; j++)
		{
			using FileStream fileStream = File.Create(Path.Combine(Paths.BuiltInAssets, $"CharacterTextureAtlas{j}.cache"));
			if (OptionsHelper.DisableCharacterAtlasCompression)
			{
				fileStream.Write(upcomingAtlasPixelsPerLevel[j], 0, upcomingAtlasPixelsPerLevel[j].Length);
				continue;
			}
			ZLibStream val = new ZLibStream((Stream)fileStream, CompressionMode.Compress);
			try
			{
				((Stream)(object)val).Write(upcomingAtlasPixelsPerLevel[j], 0, upcomingAtlasPixelsPerLevel[j].Length);
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
	}
}
