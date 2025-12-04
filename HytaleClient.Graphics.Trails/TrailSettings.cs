using HytaleClient.Math;

namespace HytaleClient.Graphics.Trails;

internal class TrailSettings
{
	public string Id;

	public string Texture;

	public Rectangle ImageLocation;

	public int LifeSpan;

	public float Roll;

	public Edge Start;

	public Edge End;

	public float LightInfluence;

	public FXSystem.RenderMode RenderMode;

	public bool Smooth;

	public Point FrameSize;

	public Point FrameRange;

	public int FrameLifeSpan;

	public Vector3 IntersectionHighlightColor;

	public float IntersectionHighlightThreshold;
}
