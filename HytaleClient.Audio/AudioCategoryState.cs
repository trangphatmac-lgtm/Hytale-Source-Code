namespace HytaleClient.Audio;

internal struct AudioCategoryState
{
	public readonly int Category;

	public readonly string RtpcName;

	public float Volume;

	public AudioCategoryState(int category, string rtpcName, float volume)
	{
		Category = category;
		RtpcName = rtpcName;
		Volume = volume;
	}
}
