using System;
using HytaleClient.Math;
using NLog;

namespace HytaleClient.InGame.Modules.Camera.Controllers;

internal class FreeRotateCameraController : ThirdPersonCameraController
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public override bool DisplayReticle => false;

	public override bool ApplyHeadRotation => false;

	public override bool InteractFromEntity => false;

	public override bool IsFirstPerson => false;

	public override Vector3 MovementForceRotation => base.AttachedTo.LookOrientation;

	public FreeRotateCameraController(GameInstance gameInstance)
		: base(gameInstance)
	{
	}

	public override void Reset(GameInstance gameInstance, ICameraController previousCameraController)
	{
		base.Reset(gameInstance, previousCameraController);
		base.PositionOffset = CalcualtePositionOffsetForHitbox(gameInstance.ActiveFieldOfView);
		_horizontalCollisionDistanceOffset = base.PositionOffset.X;
		_verticalCollisionDistanceOffset = base.PositionOffset.Z;
		if (!(previousCameraController is FreeRotateCameraController))
		{
			_rotation = new Vector3(-(float)System.Math.PI / 10f, base.AttachedTo.LookOrientation.Y + (float)System.Math.PI, 0f);
			_inTransition = false;
		}
	}

	public override void ApplyLook(float deltaTime, Vector2 look)
	{
		_rotation = new Vector3(MathHelper.Clamp(base.Rotation.X + look.X, -(float)System.Math.PI / 2f, (float)System.Math.PI / 2f), MathHelper.WrapAngle(base.Rotation.Y + look.Y), base.Rotation.Roll);
	}

	private Vector3 CalcualtePositionOffsetForHitbox(float fov)
	{
		float eyeOffset = GetEyeOffset();
		Vector3 hitboxSize = GetHitboxSize();
		float num = MathHelper.Max(eyeOffset, hitboxSize.Y - eyeOffset);
		float num2 = MathHelper.Max(hitboxSize.X, hitboxSize.Z) / 2f;
		float num3 = (float)System.Math.Sqrt(2f * num2 * num2);
		float num4 = (float)System.Math.Sqrt(num * num + num3 * num3);
		float z = num4 * 1.3f / (float)System.Math.Tan(MathHelper.ToRadians(fov * 0.5f));
		return new Vector3(0f, 0f, z);
	}
}
