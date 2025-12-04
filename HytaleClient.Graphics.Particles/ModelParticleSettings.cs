using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Graphics.Particles;

internal class ModelParticleSettings
{
	public string SystemId;

	public UInt32Color Color;

	public float Scale;

	public int TargetNodeNameId;

	public EntityPart TargetEntityPart;

	public int TargetNodeIndex;

	public Vector3 PositionOffset;

	public Quaternion RotationOffset;

	public bool DetachedFromModel;

	public ModelParticleSettings(string systemId = "")
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		SystemId = systemId;
		Color = UInt32Color.Transparent;
		Scale = 1f;
		TargetNodeNameId = -1;
		TargetEntityPart = (EntityPart)0;
		TargetNodeIndex = 0;
		PositionOffset = Vector3.Zero;
		RotationOffset = Quaternion.Identity;
		DetachedFromModel = false;
	}
}
