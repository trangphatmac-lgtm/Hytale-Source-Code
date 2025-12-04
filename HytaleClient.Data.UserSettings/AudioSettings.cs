using System;
using System.Collections.Generic;

namespace HytaleClient.Data.UserSettings;

internal class AudioSettings
{
	public enum SoundCategory
	{
		MusicVolume,
		AmbienceVolume,
		SFXVolume,
		UIVolume
	}

	public const string MasterVolumeRTPCName = "MASTERVOLUME";

	public uint OutputDeviceId;

	public float MasterVolume = 100f;

	public Dictionary<string, float> CategoryVolumes = new Dictionary<string, float>();

	public void Initialize()
	{
		string[] names = Enum.GetNames(typeof(SoundCategory));
		foreach (string text in names)
		{
			if (!CategoryVolumes.ContainsKey(text))
			{
				SoundCategory result;
				float value = ((Enum.TryParse<SoundCategory>(text, out result) && result == SoundCategory.MusicVolume) ? 70f : 100f);
				CategoryVolumes[text] = value;
			}
		}
	}

	public AudioSettings Clone()
	{
		AudioSettings audioSettings = new AudioSettings();
		audioSettings.OutputDeviceId = OutputDeviceId;
		audioSettings.MasterVolume = MasterVolume;
		audioSettings.CategoryVolumes = new Dictionary<string, float>(CategoryVolumes);
		return audioSettings;
	}

	internal string[] GetCategoryRTPCsArray()
	{
		string[] names = Enum.GetNames(typeof(SoundCategory));
		string[] array = new string[names.Length];
		for (int i = 0; i < names.Length; i++)
		{
			array[i] = names[i].ToUpper();
		}
		return array;
	}

	internal float[] GetCategoryVolumesArray()
	{
		string[] names = Enum.GetNames(typeof(SoundCategory));
		float[] array = new float[names.Length];
		for (int i = 0; i < names.Length; i++)
		{
			array[i] = CategoryVolumes[names[i]];
		}
		return array;
	}
}
