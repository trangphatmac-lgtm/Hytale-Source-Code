using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities;

public struct EntityUIDrawTask
{
	public int ComponentId;

	public float FloatValue;

	public string StringValue;

	public Matrix TransformationMatrix;

	public byte? Opacity;

	public float? Scale;
}
