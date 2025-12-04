using System.Collections.Generic;

namespace HytaleClient.Data.Characters;

public class CharacterPart
{
	public string Id;

	public string Name;

	public string Model;

	public string GradientSet;

	public string GreyscaleTexture;

	public Dictionary<string, CharacterPartTexture> Textures;

	public Dictionary<string, CharacterPartVariant> Variants;

	public CharacterBodyType DefaultFor = CharacterBodyType.None;

	public string[] Tags;
}
