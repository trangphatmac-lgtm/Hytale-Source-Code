using HytaleClient.Math;

namespace HytaleClient.Data.ClientInteraction.Selector;

internal class HitDetectionBuffer
{
	public Vector4 HitPosition;

	public Quad4 TransformedQuad;

	public Vector4 TransformedPoint;

	public Triangle4 VisibleTriangle;

	public bool ContainsFully;
}
