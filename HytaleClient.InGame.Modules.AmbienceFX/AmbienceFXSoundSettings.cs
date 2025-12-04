namespace HytaleClient.InGame.Modules.AmbienceFX;

internal class AmbienceFXSoundSettings
{
	public enum AmbienceFXSoundPlay3D
	{
		Random,
		LocationName,
		No
	}

	public enum AmbienceFXAltitude
	{
		Normal,
		Lowest,
		Highest,
		Random
	}

	public uint SoundEventIndex;

	public AmbienceFXSoundPlay3D Play3D;

	public int BlockSoundSetIndex;

	public AmbienceFXAltitude Altitude;

	public Rangef Frequency;

	public float LastTime;

	public float NextTime;

	public Range Radius;
}
