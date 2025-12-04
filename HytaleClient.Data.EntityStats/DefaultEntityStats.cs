namespace HytaleClient.Data.EntityStats;

public static class DefaultEntityStats
{
	public static int Health = -1;

	public static int Oxygen = -1;

	public static int Stamina = -1;

	public static int Mana = -1;

	public static int SignatureEnergy = -1;

	public static int Ammo = -1;

	public static void Update(ClientEntityStatType[] types)
	{
		for (int i = 0; i < types.Length; i++)
		{
			switch (types[i].Id)
			{
			case "Health":
				Health = i;
				break;
			case "Oxygen":
				Oxygen = i;
				break;
			case "Stamina":
				Stamina = i;
				break;
			case "Mana":
				Mana = i;
				break;
			case "SignatureEnergy":
				SignatureEnergy = i;
				break;
			case "Ammo":
				Ammo = i;
				break;
			}
		}
	}

	public static bool IsDefault(int type)
	{
		return type == Health || type == Oxygen || type == Stamina || type == Mana || type == SignatureEnergy || type == Ammo;
	}
}
