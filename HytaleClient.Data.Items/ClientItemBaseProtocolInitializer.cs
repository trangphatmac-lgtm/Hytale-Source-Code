using System;
using System.Collections.Generic;
using System.Linq;
using HytaleClient.Audio;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Data.FX;
using HytaleClient.Graphics.Particles;
using HytaleClient.InGame.Modules.BuilderTools.Tools;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.Data.Items;

internal class ClientItemBaseProtocolInitializer
{
	public static void Parse(ItemBase networkItemBase, NodeNameManager nodeNameManager, ref ClientItemBase itemBase)
	{
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		itemBase.Id = networkItemBase.Id;
		itemBase.Categories = networkItemBase.Categories;
		itemBase.Set = networkItemBase.Set;
		itemBase.ItemLevel = networkItemBase.ItemLevel;
		itemBase.QualityIndex = networkItemBase.QualityIndex;
		itemBase.SoundEventIndex = ResourceManager.GetNetworkWwiseId(networkItemBase.SoundEventIndex);
		if (networkItemBase.Particles != null)
		{
			itemBase.Particles = new ModelParticleSettings[networkItemBase.Particles.Length];
			for (int i = 0; i < networkItemBase.Particles.Length; i++)
			{
				itemBase.Particles[i] = new ModelParticleSettings();
				ParticleProtocolInitializer.Initialize(networkItemBase.Particles[i], ref itemBase.Particles[i], nodeNameManager);
			}
		}
		if (networkItemBase.FirstPersonParticles != null)
		{
			itemBase.FirstPersonParticles = new ModelParticleSettings[networkItemBase.FirstPersonParticles.Length];
			for (int j = 0; j < networkItemBase.FirstPersonParticles.Length; j++)
			{
				itemBase.FirstPersonParticles[j] = new ModelParticleSettings();
				ParticleProtocolInitializer.Initialize(networkItemBase.FirstPersonParticles[j], ref itemBase.FirstPersonParticles[j], nodeNameManager);
			}
		}
		itemBase.Trails = networkItemBase.Trails;
		if (networkItemBase.Light != null)
		{
			ParseLightColor(networkItemBase.Light, ref itemBase.LightEmitted);
		}
		itemBase.Scale = networkItemBase.Scale;
		itemBase.Texture = networkItemBase.Texture;
		itemBase.MaxStack = networkItemBase.MaxStack;
		itemBase.Icon = networkItemBase.Icon;
		if (networkItemBase.IconProperties != null)
		{
			itemBase.IconProperties = new ClientItemIconProperties(networkItemBase.IconProperties);
		}
		if (networkItemBase.Recipe != null)
		{
			itemBase.Recipe = new ClientItemCraftingRecipe(networkItemBase.Recipe);
		}
		itemBase.ResourceTypes = networkItemBase.ResourceTypes?.Select((ItemResourceType resource) => new ClientItemResourceType(resource)).ToArray();
		itemBase.Consumable = networkItemBase.Consumable;
		itemBase.PlayerAnimationsId = networkItemBase.PlayerAnimationsId;
		itemBase.UsePlayerAnimations = networkItemBase.UsePlayerAnimations;
		itemBase.ReticleIndex = networkItemBase.ReticleIndex;
		itemBase.BlockId = networkItemBase.BlockId;
		itemBase.Tool = networkItemBase.Tool;
		itemBase.BuilderTool = ((networkItemBase.BuilderToolData != null) ? new BuilderTool(networkItemBase.BuilderToolData) : null);
		itemBase.Armor = ((networkItemBase.Armor != null) ? new ClientItemArmor(networkItemBase.Armor) : null);
		itemBase.Weapon = networkItemBase.Weapon;
		itemBase.Utility = (ItemUtility)(((object)networkItemBase.Utility) ?? ((object)new ItemUtility()));
		itemBase.Durability = networkItemBase.Durability;
		itemBase.ItemEntity = networkItemBase.ItemEntity;
		itemBase.Interactions = networkItemBase.Interactions;
		itemBase.InteractionVars = networkItemBase.InteractionVars;
		itemBase.InteractionConfiguration = networkItemBase.InteractionConfig;
		itemBase.TagIndexes = networkItemBase.TagIndexes;
		if (networkItemBase.ItemAppearanceConditions != null)
		{
			itemBase.ItemAppearanceConditions = new Dictionary<int, ClientItemAppearanceCondition[]>();
			foreach (KeyValuePair<int, ItemAppearanceCondition[]> itemAppearanceCondition in networkItemBase.ItemAppearanceConditions)
			{
				ClientItemAppearanceCondition[] array = new ClientItemAppearanceCondition[itemAppearanceCondition.Value.Length];
				for (int k = 0; k < itemAppearanceCondition.Value.Length; k++)
				{
					array[k] = ParseItemAppearanceCondition(itemAppearanceCondition.Value[k], nodeNameManager);
				}
				itemBase.ItemAppearanceConditions.Add(itemAppearanceCondition.Key, array);
			}
		}
		itemBase.DisplayEntityStatsHUD = networkItemBase.DisplayEntityStatsHUD;
		itemBase.PullbackConfig = ((networkItemBase.PullbackConfig != null) ? new ClientItemPullbackConfig(networkItemBase.PullbackConfig) : null);
		itemBase.ClipsGeometry = networkItemBase.ClipsGeometry;
	}

	public static void ParseLightColor(ColorLight colorLight, ref ColorRgb light)
	{
		light.R = System.Math.Max((byte)colorLight.Red, (byte)colorLight.Radius);
		light.G = System.Math.Max((byte)colorLight.Green, (byte)colorLight.Radius);
		light.B = System.Math.Max((byte)colorLight.Blue, (byte)colorLight.Radius);
	}

	public static ClientItemAppearanceCondition ParseItemAppearanceCondition(ItemAppearanceCondition itemAppearanceCondition, NodeNameManager nodeNameManager)
	{
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Invalid comparison between Unknown and I4
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		ClientItemAppearanceCondition clientItemAppearanceCondition = new ClientItemAppearanceCondition();
		if (itemAppearanceCondition.Particles != null)
		{
			clientItemAppearanceCondition.Particles = new ModelParticleSettings[itemAppearanceCondition.Particles.Length];
			for (int i = 0; i < itemAppearanceCondition.Particles.Length; i++)
			{
				clientItemAppearanceCondition.Particles[i] = new ModelParticleSettings();
				ParticleProtocolInitializer.Initialize(itemAppearanceCondition.Particles[i], ref clientItemAppearanceCondition.Particles[i], nodeNameManager);
			}
		}
		if (itemAppearanceCondition.FirstPersonParticles != null)
		{
			clientItemAppearanceCondition.FirstPersonParticles = new ModelParticleSettings[itemAppearanceCondition.FirstPersonParticles.Length];
			for (int j = 0; j < itemAppearanceCondition.FirstPersonParticles.Length; j++)
			{
				clientItemAppearanceCondition.FirstPersonParticles[j] = new ModelParticleSettings();
				ParticleProtocolInitializer.Initialize(itemAppearanceCondition.FirstPersonParticles[j], ref clientItemAppearanceCondition.FirstPersonParticles[j], nodeNameManager);
			}
		}
		clientItemAppearanceCondition.ModelId = itemAppearanceCondition.Model;
		clientItemAppearanceCondition.Texture = itemAppearanceCondition.Texture;
		clientItemAppearanceCondition.Condition = new FloatRange(itemAppearanceCondition.Condition.InclusiveMin, itemAppearanceCondition.Condition.InclusiveMax);
		if (itemAppearanceCondition.ModelVFXId != null)
		{
			clientItemAppearanceCondition.ModelVFXId = itemAppearanceCondition.ModelVFXId;
		}
		ValueType conditionValueType = itemAppearanceCondition.ConditionValueType;
		ValueType val = conditionValueType;
		if ((int)val == 0 || (int)val != 1)
		{
			clientItemAppearanceCondition.Type = (ValueType)0;
		}
		else
		{
			clientItemAppearanceCondition.Type = (ValueType)1;
		}
		return clientItemAppearanceCondition;
	}
}
