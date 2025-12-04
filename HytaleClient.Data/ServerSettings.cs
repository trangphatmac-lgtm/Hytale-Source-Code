using System.Collections.Generic;
using HytaleClient.Data.Entities;
using HytaleClient.Data.EntityStats;
using HytaleClient.Data.EntityUI;
using HytaleClient.Data.Items;
using HytaleClient.Data.Weather;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data;

internal class ServerSettings
{
	public const int EmptyBlockSoundId = 0;

	public const int AirFluidFXId = 0;

	public const int EmptyFluidFXId = 0;

	public const int UnknownEnvironmentId = 0;

	public const int UnknownWeatherId = 0;

	public const int UnknownTag = int.MinValue;

	public Dictionary<string, int> WeatherIndicesByIds;

	public ClientWeather[] Weathers;

	public ClientWorldEnvironment[] Environments;

	public BlockHitbox[] BlockHitboxes;

	public BlockSoundSet[] BlockSoundSets;

	public Dictionary<string, ClientBlockParticleSet> BlockParticleSets;

	public FluidFX[] FluidFXs;

	public Dictionary<InteractionType, int> UnarmedInteractions;

	public ClientEntityStatType[] EntityStatTypes;

	public ClientItemQuality[] ItemQualities;

	public ClientItemReticleConfig[] ItemReticleConfigs;

	public ClientHitboxCollisionConfig[] HitboxCollisionConfigs;

	public ClientRepulsionConfig[] RepulsionConfigs;

	public ClientEntityUIComponent[] EntityUIComponents;

	public Dictionary<string, BlockGroup> BlockGroups;

	public ServerTags ServerTags;

	public int GetServerTag(string tag)
	{
		Dictionary<string, int> dictionary = ServerTags?.Tags;
		if (dictionary == null)
		{
			return int.MinValue;
		}
		int value;
		return dictionary.TryGetValue(tag, out value) ? value : int.MinValue;
	}

	public ServerSettings Clone()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Expected O, but got Unknown
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Expected O, but got Unknown
		//IL_029f: Unknown result type (might be due to invalid IL or missing references)
		ServerSettings serverSettings = new ServerSettings();
		serverSettings.ServerTags = ((ServerTags == null) ? ((ServerTags)null) : new ServerTags(ServerTags));
		if (Weathers != null)
		{
			serverSettings.Weathers = new ClientWeather[Weathers.Length];
			serverSettings.WeatherIndicesByIds = new Dictionary<string, int>();
			for (int i = 0; i < Weathers.Length; i++)
			{
				ClientWeather clientWeather = Weathers[i].Clone();
				serverSettings.Weathers[i] = clientWeather;
				serverSettings.WeatherIndicesByIds[clientWeather.Id] = i;
			}
		}
		if (Environments != null)
		{
			serverSettings.Environments = new ClientWorldEnvironment[Environments.Length];
			for (int j = 0; j < Environments.Length; j++)
			{
				serverSettings.Environments[j] = Environments[j].Clone();
			}
		}
		if (BlockHitboxes != null)
		{
			serverSettings.BlockHitboxes = new BlockHitbox[BlockHitboxes.Length];
			for (int k = 0; k < BlockHitboxes.Length; k++)
			{
				serverSettings.BlockHitboxes[k] = BlockHitboxes[k].Clone();
			}
		}
		if (BlockSoundSets != null)
		{
			serverSettings.BlockSoundSets = (BlockSoundSet[])(object)new BlockSoundSet[BlockSoundSets.Length];
			for (int l = 0; l < BlockSoundSets.Length; l++)
			{
				serverSettings.BlockSoundSets[l] = new BlockSoundSet(BlockSoundSets[l]);
			}
		}
		if (BlockParticleSets != null)
		{
			serverSettings.BlockParticleSets = new Dictionary<string, ClientBlockParticleSet>();
			foreach (KeyValuePair<string, ClientBlockParticleSet> blockParticleSet in BlockParticleSets)
			{
				serverSettings.BlockParticleSets[blockParticleSet.Key] = blockParticleSet.Value.Clone();
			}
		}
		if (FluidFXs != null)
		{
			serverSettings.FluidFXs = (FluidFX[])(object)new FluidFX[FluidFXs.Length];
			for (int m = 0; m < FluidFXs.Length; m++)
			{
				serverSettings.FluidFXs[m] = new FluidFX(FluidFXs[m]);
			}
		}
		if (UnarmedInteractions != null)
		{
			serverSettings.UnarmedInteractions = new Dictionary<InteractionType, int>();
			foreach (KeyValuePair<InteractionType, int> unarmedInteraction in UnarmedInteractions)
			{
				serverSettings.UnarmedInteractions[unarmedInteraction.Key] = unarmedInteraction.Value;
			}
		}
		if (EntityStatTypes != null)
		{
			serverSettings.EntityStatTypes = new ClientEntityStatType[EntityStatTypes.Length];
			for (int n = 0; n < EntityStatTypes.Length; n++)
			{
				ClientEntityStatType clientEntityStatType = EntityStatTypes[n];
				serverSettings.EntityStatTypes[n] = clientEntityStatType;
			}
		}
		if (ItemQualities != null)
		{
			serverSettings.ItemQualities = new ClientItemQuality[ItemQualities.Length];
			for (int num = 0; num < ItemQualities.Length; num++)
			{
				serverSettings.ItemQualities[num] = ItemQualities[num].Clone();
			}
		}
		if (ItemReticleConfigs != null)
		{
			serverSettings.ItemReticleConfigs = new ClientItemReticleConfig[ItemReticleConfigs.Length];
			for (int num2 = 0; num2 < ItemReticleConfigs.Length; num2++)
			{
				serverSettings.ItemReticleConfigs[num2] = ItemReticleConfigs[num2].Clone();
			}
		}
		if (HitboxCollisionConfigs != null)
		{
			serverSettings.HitboxCollisionConfigs = new ClientHitboxCollisionConfig[HitboxCollisionConfigs.Length];
			for (int num3 = 0; num3 < HitboxCollisionConfigs.Length; num3++)
			{
				serverSettings.HitboxCollisionConfigs[num3] = HitboxCollisionConfigs[num3].Clone();
			}
		}
		if (EntityUIComponents != null)
		{
			serverSettings.EntityUIComponents = new ClientEntityUIComponent[EntityUIComponents.Length];
			for (int num4 = 0; num4 < EntityUIComponents.Length; num4++)
			{
				serverSettings.EntityUIComponents[num4] = EntityUIComponents[num4].Clone();
			}
		}
		if (RepulsionConfigs != null)
		{
			serverSettings.RepulsionConfigs = new ClientRepulsionConfig[RepulsionConfigs.Length];
			for (int num5 = 0; num5 < RepulsionConfigs.Length; num5++)
			{
				serverSettings.RepulsionConfigs[num5] = RepulsionConfigs[num5].Clone();
			}
		}
		if (BlockGroups != null)
		{
			serverSettings.BlockGroups = new Dictionary<string, BlockGroup>(BlockGroups);
		}
		return serverSettings;
	}
}
