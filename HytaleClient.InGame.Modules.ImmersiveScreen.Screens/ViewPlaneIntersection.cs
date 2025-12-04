using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.ImmersiveScreen.Screens;

public struct ViewPlaneIntersection
{
	public Vector3 WorldPosition;

	public Vector2 PixelPosition;

	public ViewPlaneIntersection(Vector3 worldPos, Vector2 pixelPos)
	{
		WorldPosition = worldPos;
		PixelPosition = pixelPos;
	}
}
