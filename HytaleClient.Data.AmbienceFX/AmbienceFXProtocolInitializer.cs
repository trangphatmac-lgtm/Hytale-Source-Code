using System.Collections.Generic;
using HytaleClient.Audio;
using HytaleClient.InGame.Modules.AmbienceFX;
using HytaleClient.Protocol;

namespace HytaleClient.Data.AmbienceFX;

internal class AmbienceFXProtocolInitializer
{
	public static void Initialize(AmbienceFX networkAmbienceFX, ref AmbienceFXSettings clientAmbienceFX, List<AmbienceFXSoundSettings> validSoundSettings)
	{
		clientAmbienceFX.Id = networkAmbienceFX.Id;
		if (networkAmbienceFX.Conditions != null)
		{
			clientAmbienceFX.Conditions = new AmbienceFXConditionSettings();
			Initialize(networkAmbienceFX.Conditions, ref clientAmbienceFX.Conditions);
		}
		if (networkAmbienceFX.Sounds != null)
		{
			for (int i = 0; i < networkAmbienceFX.Sounds.Length; i++)
			{
				validSoundSettings.Clear();
				AmbienceFXSoundSettings clientAmbienceFXSound = new AmbienceFXSoundSettings();
				Initialize(networkAmbienceFX.Sounds[i], ref clientAmbienceFXSound);
				if (clientAmbienceFXSound.SoundEventIndex != 0)
				{
					validSoundSettings.Add(clientAmbienceFXSound);
				}
			}
			if (validSoundSettings.Count != 0)
			{
				clientAmbienceFX.Sounds = validSoundSettings.ToArray();
			}
		}
		clientAmbienceFX.MusicSoundEventIndex = ResourceManager.GetNetworkWwiseId(networkAmbienceFX.MusicSoundEventIndex);
		clientAmbienceFX.AmbientBedSoundEventIndex = ResourceManager.GetNetworkWwiseId(networkAmbienceFX.AmbientBedSoundEventIndex);
		clientAmbienceFX.EffectSoundEventIndex = ResourceManager.GetNetworkWwiseId(networkAmbienceFX.EffectSoundEventIndex);
	}

	public static void Initialize(AmbienceFXConditions networkAmbienceFXConditions, ref AmbienceFXConditionSettings clientAmbienceFXConditions)
	{
		clientAmbienceFXConditions.EnvironmentIndices = networkAmbienceFXConditions.EnvironmentIndices;
		clientAmbienceFXConditions.WeatherIndices = networkAmbienceFXConditions.WeatherIndices;
		clientAmbienceFXConditions.FluidFXIndices = networkAmbienceFXConditions.FluidFXIndices;
		if (networkAmbienceFXConditions.SurroundingBlockSoundSets != null)
		{
			clientAmbienceFXConditions.SurroundingBlockSoundSets = new AmbienceFXConditionSettings.AmbienceFXBlockSoundSet[networkAmbienceFXConditions.SurroundingBlockSoundSets.Length];
			for (int i = 0; i < networkAmbienceFXConditions.SurroundingBlockSoundSets.Length; i++)
			{
				clientAmbienceFXConditions.SurroundingBlockSoundSets[i] = default(AmbienceFXConditionSettings.AmbienceFXBlockSoundSet);
				Initialize(networkAmbienceFXConditions.SurroundingBlockSoundSets[i], ref clientAmbienceFXConditions.SurroundingBlockSoundSets[i]);
			}
		}
		clientAmbienceFXConditions.Altitude = new Range(networkAmbienceFXConditions.Altitude.Min, networkAmbienceFXConditions.Altitude.Max);
		clientAmbienceFXConditions.Walls = new Range(networkAmbienceFXConditions.Walls.Min, networkAmbienceFXConditions.Walls.Max);
		clientAmbienceFXConditions.Roof = networkAmbienceFXConditions.Roof;
		clientAmbienceFXConditions.Floor = networkAmbienceFXConditions.Floor;
		clientAmbienceFXConditions.SunLightLevel = new Range(networkAmbienceFXConditions.SunLightLevel.Min, networkAmbienceFXConditions.SunLightLevel.Max);
		clientAmbienceFXConditions.TorchLightLevel = new Range(networkAmbienceFXConditions.TorchLightLevel.Min, networkAmbienceFXConditions.TorchLightLevel.Max);
		clientAmbienceFXConditions.GlobalLightLevel = new Range(networkAmbienceFXConditions.GlobalLightLevel.Min, networkAmbienceFXConditions.GlobalLightLevel.Max);
		clientAmbienceFXConditions.DayTime = new Rangef(networkAmbienceFXConditions.DayTime.Min, networkAmbienceFXConditions.DayTime.Max);
	}

	public static void Initialize(AmbienceFXBlockSoundSet networkAmbienceFXBlockSoundSet, ref AmbienceFXConditionSettings.AmbienceFXBlockSoundSet clientAmbienceFXBlockSoundSet)
	{
		clientAmbienceFXBlockSoundSet.BlockSoundSetIndex = networkAmbienceFXBlockSoundSet.BlockSoundSetIndex;
		clientAmbienceFXBlockSoundSet.Percent = new Rangef(networkAmbienceFXBlockSoundSet.Percent.Min, networkAmbienceFXBlockSoundSet.Percent.Max);
	}

	public static void Initialize(AmbienceFXSound networkAmbienceFXSound, ref AmbienceFXSoundSettings clientAmbienceFXSound)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected I4, but got Unknown
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected I4, but got Unknown
		clientAmbienceFXSound.SoundEventIndex = ResourceManager.GetNetworkWwiseId(networkAmbienceFXSound.SoundEventIndex);
		clientAmbienceFXSound.Play3D = (AmbienceFXSoundSettings.AmbienceFXSoundPlay3D)networkAmbienceFXSound.Play3D;
		clientAmbienceFXSound.BlockSoundSetIndex = networkAmbienceFXSound.BlockSoundSetIndex;
		clientAmbienceFXSound.Altitude = (AmbienceFXSoundSettings.AmbienceFXAltitude)networkAmbienceFXSound.Altitude;
		clientAmbienceFXSound.Frequency = new Rangef(networkAmbienceFXSound.Frequency.Min, networkAmbienceFXSound.Frequency.Max);
		clientAmbienceFXSound.Radius = new Range(networkAmbienceFXSound.Radius.Min, networkAmbienceFXSound.Radius.Max);
	}
}
