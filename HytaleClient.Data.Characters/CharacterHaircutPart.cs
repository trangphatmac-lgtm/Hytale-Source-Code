namespace HytaleClient.Data.Characters;

public class CharacterHaircutPart : CharacterPart
{
	public enum CharacterHairType
	{
		Short,
		Medium,
		Long
	}

	public CharacterHairType HairType;

	public bool RequiresGenericHaircut;
}
