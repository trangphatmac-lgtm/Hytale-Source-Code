using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.BlockyModels;

internal class ModelTransformHelper
{
	public static void Decompose(ModelTransform modelTransform, ref Vector3 position, ref Vector3 bodyOrientation, ref Vector3 lookOrientation)
	{
		Position position_ = modelTransform.Position_;
		if (position_ != null)
		{
			position = new Vector3((!double.IsNaN(position_.X) && !double.IsInfinity(position_.X)) ? ((float)position_.X) : position.X, (!double.IsNaN(position_.Y) && !double.IsInfinity(position_.Y)) ? ((float)position_.Y) : position.Y, (!double.IsNaN(position_.Z) && !double.IsInfinity(position_.Z)) ? ((float)position_.Z) : position.Z);
		}
		Direction bodyOrientation2 = modelTransform.BodyOrientation;
		if (bodyOrientation2 != null)
		{
			bodyOrientation = new Vector3((!float.IsNaN(bodyOrientation2.Pitch) && !float.IsInfinity(bodyOrientation2.Pitch)) ? MathHelper.WrapAngle(bodyOrientation2.Pitch) : bodyOrientation.Pitch, (!float.IsNaN(bodyOrientation2.Yaw) && !float.IsInfinity(bodyOrientation2.Yaw)) ? MathHelper.WrapAngle(bodyOrientation2.Yaw) : bodyOrientation.Yaw, (!float.IsNaN(bodyOrientation2.Roll) && !float.IsInfinity(bodyOrientation2.Roll)) ? MathHelper.WrapAngle(bodyOrientation2.Roll) : bodyOrientation.Roll);
		}
		Direction lookOrientation2 = modelTransform.LookOrientation;
		if (lookOrientation2 != null)
		{
			lookOrientation = new Vector3((!float.IsNaN(lookOrientation2.Pitch) && !float.IsInfinity(lookOrientation2.Pitch)) ? MathHelper.WrapAngle(lookOrientation2.Pitch) : lookOrientation.Pitch, (!float.IsNaN(lookOrientation2.Yaw) && !float.IsInfinity(lookOrientation2.Yaw)) ? MathHelper.WrapAngle(lookOrientation2.Yaw) : lookOrientation.Yaw, (!float.IsNaN(lookOrientation2.Roll) && !float.IsInfinity(lookOrientation2.Roll)) ? MathHelper.WrapAngle(lookOrientation2.Roll) : lookOrientation.Roll);
		}
	}
}
