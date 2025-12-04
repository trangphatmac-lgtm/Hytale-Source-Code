using HytaleClient.Graphics;
using HytaleClient.Graphics.Trails;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.FX;

internal class TrailProtocolInitializer
{
	public static void Initialize(Trail networkTrail, ref TrailSettings trailSettings)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected I4, but got Unknown
		trailSettings.Id = networkTrail.Id;
		trailSettings.Texture = networkTrail.Texture;
		FXRenderMode renderMode = networkTrail.RenderMode;
		FXRenderMode val = renderMode;
		switch ((int)val)
		{
		case 0:
			trailSettings.RenderMode = FXSystem.RenderMode.BlendLinear;
			break;
		case 1:
			trailSettings.RenderMode = FXSystem.RenderMode.BlendAdd;
			break;
		case 3:
			trailSettings.RenderMode = FXSystem.RenderMode.Distortion;
			break;
		case 2:
			trailSettings.RenderMode = FXSystem.RenderMode.Erosion;
			break;
		}
		if (networkTrail.IntersectionHighlight_ != null && networkTrail.IntersectionHighlight_.HighlightColor != null)
		{
			trailSettings.IntersectionHighlightColor = new Vector3((float)(int)(byte)networkTrail.IntersectionHighlight_.HighlightColor.Red / 255f, (float)(int)(byte)networkTrail.IntersectionHighlight_.HighlightColor.Green / 255f, (float)(int)(byte)networkTrail.IntersectionHighlight_.HighlightColor.Blue / 255f);
			trailSettings.IntersectionHighlightThreshold = networkTrail.IntersectionHighlight_.HighlightThreshold;
		}
		trailSettings.LifeSpan = networkTrail.LifeSpan;
		trailSettings.Roll = networkTrail.Roll;
		if (networkTrail.Start != null)
		{
			if (networkTrail.Start.Color != null)
			{
				trailSettings.Start.Color = new Vector4((int)(byte)networkTrail.Start.Color.Red, (int)(byte)networkTrail.Start.Color.Green, (int)(byte)networkTrail.Start.Color.Blue, (int)(byte)networkTrail.Start.Color.Alpha) / 255f;
			}
			else
			{
				trailSettings.Start.Color = new Vector4(1f);
			}
			trailSettings.Start.Width = networkTrail.Start.Width;
		}
		else
		{
			trailSettings.Start.Width = 1f;
		}
		if (networkTrail.End != null)
		{
			if (networkTrail.End.Color != null)
			{
				trailSettings.End.Color = new Vector4((int)(byte)networkTrail.End.Color.Red, (int)(byte)networkTrail.End.Color.Green, (int)(byte)networkTrail.End.Color.Blue, (int)(byte)networkTrail.End.Color.Alpha) / 255f;
			}
			else
			{
				trailSettings.End.Color = new Vector4(1f);
			}
			trailSettings.End.Width = networkTrail.End.Width;
		}
		else
		{
			trailSettings.End.Width = 1f;
		}
		trailSettings.LightInfluence = networkTrail.LightInfluence;
		trailSettings.Smooth = networkTrail.Smooth;
		if (networkTrail.FrameSize != null)
		{
			trailSettings.FrameSize = new Point(networkTrail.FrameSize.X, networkTrail.FrameSize.Y);
		}
		if (networkTrail.FrameRange != null)
		{
			trailSettings.FrameRange = new Point(networkTrail.FrameRange.Min, networkTrail.FrameRange.Max);
		}
		trailSettings.FrameLifeSpan = networkTrail.FrameLifeSpan;
	}
}
