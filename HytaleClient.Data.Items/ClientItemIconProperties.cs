using Coherent.UI.Binding;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Items;

[CoherentType]
public class ClientItemIconProperties
{
	[CoherentProperty("scale")]
	public float Scale;

	[CoherentProperty("translation")]
	public Vector2? Translation;

	[CoherentProperty("rotation")]
	public Vector3? Rotation;

	public ClientItemIconProperties(ItemIconProperties properties)
	{
		Scale = properties.Scale;
		if (properties.Translation != null)
		{
			Translation = new Vector2(properties.Translation.X, properties.Translation.Y);
		}
		if (properties.Rotation != null)
		{
			Rotation = new Vector3(properties.Rotation.X, properties.Rotation.Y, properties.Rotation.Z);
		}
	}

	public ClientItemIconProperties()
	{
	}
}
