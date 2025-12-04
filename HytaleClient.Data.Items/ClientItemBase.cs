using System;
using System.Collections.Generic;
using Coherent.UI.Binding;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Graphics.Particles;
using HytaleClient.InGame.Modules.BuilderTools.Tools;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.Data.Items;

[CoherentType]
internal class ClientItemBase
{
	public const string EmptyItemName = "Empty";

	public const string UnknownItemName = "Unknown";

	[CoherentProperty("id")]
	public string Id;

	[CoherentProperty("categories")]
	public string[] Categories;

	[CoherentProperty("set")]
	public string Set;

	[CoherentProperty("qualityIndex")]
	public int QualityIndex;

	[CoherentProperty("itemLevel")]
	public int ItemLevel;

	[CoherentProperty("recipe")]
	public ClientItemCraftingRecipe Recipe;

	[CoherentProperty("resourceTypes")]
	public ClientItemResourceType[] ResourceTypes;

	public uint SoundEventIndex;

	public ModelParticleSettings[] Particles;

	public ModelParticleSettings[] FirstPersonParticles;

	public ModelTrail[] Trails;

	public ColorRgb LightEmitted;

	public BlockyModel Model;

	public float Scale;

	public string Texture;

	public BlockyAnimation Animation;

	public string PlayerAnimationsId;

	public ClientItemPlayerAnimations PlayerAnimations;

	public bool UsePlayerAnimations;

	public bool Consumable;

	public int BlockId;

	public ItemUtility Utility;

	[CoherentProperty("tool")]
	public ItemTool Tool;

	[CoherentProperty("weapon")]
	public ItemWeapon Weapon;

	public BuilderTool BuilderTool;

	[CoherentProperty("armor")]
	public ClientItemArmor Armor;

	[CoherentProperty("iconProperties")]
	public ClientItemIconProperties IconProperties;

	[CoherentProperty("maxStack")]
	public int MaxStack;

	[CoherentProperty("icon")]
	public string Icon;

	public int ReticleIndex;

	[CoherentProperty("durability")]
	public double Durability;

	public ItemEntityConfig ItemEntity;

	public Dictionary<InteractionType, int> Interactions;

	public Dictionary<string, int> InteractionVars;

	public InteractionConfiguration InteractionConfiguration;

	public Dictionary<int, ClientItemAppearanceCondition[]> ItemAppearanceConditions;

	public BlockyAnimation DroppedItemAnimation;

	public int[] TagIndexes;

	public int[] DisplayEntityStatsHUD;

	public ClientItemPullbackConfig PullbackConfig;

	public bool ClipsGeometry;

	public EntityAnimation GetAnimation(string id)
	{
		if (!PlayerAnimations.Animations.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public bool ShouldDisplayHudForEntityStat(int entityStatIndex)
	{
		if (DisplayEntityStatsHUD == null)
		{
			return false;
		}
		return Array.IndexOf(DisplayEntityStatsHUD, entityStatIndex) >= 0;
	}
}
