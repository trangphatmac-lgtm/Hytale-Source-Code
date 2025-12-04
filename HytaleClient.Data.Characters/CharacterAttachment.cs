namespace HytaleClient.Data.Characters;

public class CharacterAttachment
{
	public readonly string Model;

	public readonly string Texture;

	public readonly bool IsUsingBaseNodeOnly;

	public readonly byte GradientId;

	public CharacterAttachment(string model, string texture, bool isUsingBaseNodeOnly, byte gradientId = 0)
	{
		Model = model;
		Texture = texture;
		IsUsingBaseNodeOnly = isUsingBaseNodeOnly;
		GradientId = gradientId;
	}
}
