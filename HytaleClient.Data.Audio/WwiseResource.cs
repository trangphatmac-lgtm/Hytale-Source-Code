namespace HytaleClient.Data.Audio;

internal struct WwiseResource
{
	public enum WwiseResourceType : byte
	{
		Event,
		GameParameter
	}

	public WwiseResourceType Type;

	public uint Id;

	public WwiseResource(WwiseResourceType type, uint id)
	{
		Type = type;
		Id = id;
	}
}
