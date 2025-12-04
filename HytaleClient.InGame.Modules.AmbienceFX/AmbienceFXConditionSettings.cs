namespace HytaleClient.InGame.Modules.AmbienceFX;

internal class AmbienceFXConditionSettings
{
	public struct AmbienceFXBlockSoundSet
	{
		public int BlockSoundSetIndex;

		public Rangef Percent;
	}

	public int[] EnvironmentIndices;

	public int[] WeatherIndices;

	public int[] FluidFXIndices;

	public AmbienceFXBlockSoundSet[] SurroundingBlockSoundSets;

	public Range Altitude;

	public Range Walls;

	public bool Roof;

	public bool Floor;

	public Range SunLightLevel;

	public Range TorchLightLevel;

	public Range GlobalLightLevel;

	public Rangef DayTime;
}
