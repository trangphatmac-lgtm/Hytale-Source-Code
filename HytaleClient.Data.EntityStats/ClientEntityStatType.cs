using HytaleClient.Protocol;

namespace HytaleClient.Data.EntityStats;

public struct ClientEntityStatType
{
	public string Id;

	public float Value;

	public float Min;

	public float Max;

	public EntityStatEffects MinValueEffects;

	public EntityStatEffects MaxValueEffects;

	public ClientEntityStatType(EntityStatType entityStatType)
	{
		Id = entityStatType.Id;
		Value = entityStatType.Value;
		Min = entityStatType.Min;
		Max = entityStatType.Max;
		MinValueEffects = entityStatType.MinValueEffects;
		MaxValueEffects = entityStatType.MaxValueEffects;
	}
}
