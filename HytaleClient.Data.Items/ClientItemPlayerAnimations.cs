using System.Collections.Generic;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Items;

internal class ClientItemPlayerAnimations
{
	public const string DefaultId = "Default";

	public string Id;

	public readonly Dictionary<string, EntityAnimation> Animations = new Dictionary<string, EntityAnimation>();

	public readonly WiggleWeights WiggleWeights;

	public readonly CameraSettings Camera;

	public ClientItemPlayerAnimations(ItemPlayerAnimations networkAnimations)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		Id = networkAnimations.Id;
		WiggleWeights = networkAnimations.WiggleWeights_;
		if (networkAnimations.Camera != null)
		{
			Camera = new CameraSettings(networkAnimations.Camera);
			if (Camera.Yaw != null && Camera.Yaw.AngleRange != null)
			{
				Camera.Yaw.AngleRange.Min = MathHelper.ToRadians(Camera.Yaw.AngleRange.Min);
				Camera.Yaw.AngleRange.Max = MathHelper.ToRadians(Camera.Yaw.AngleRange.Max);
			}
			if (Camera.Pitch != null && Camera.Pitch.AngleRange != null)
			{
				Camera.Pitch.AngleRange.Min = MathHelper.ToRadians(Camera.Pitch.AngleRange.Min);
				Camera.Pitch.AngleRange.Max = MathHelper.ToRadians(Camera.Pitch.AngleRange.Max);
			}
		}
	}
}
