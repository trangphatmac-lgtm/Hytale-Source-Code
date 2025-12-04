namespace HytaleClient.Data.Characters;

public class CharacterHeadAccessoryPart : CharacterPart
{
	public enum CharacterHeadAccessoryType
	{
		Simple,
		HalfCovering,
		FullyCovering
	}

	public CharacterHeadAccessoryType HeadAccessoryType;
}
