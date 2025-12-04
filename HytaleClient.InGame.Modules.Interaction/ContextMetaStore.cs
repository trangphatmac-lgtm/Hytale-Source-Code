using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.Interaction;

internal class ContextMetaStore
{
	public Entity TargetEntity;

	public Vector4? HitLocation;

	public string HitDetail;

	public BlockPosition TargetBlock;

	public BlockPosition TargetBlockRaw;

	public int? TargetSlot;

	public bool DisableSlotFork;

	public bool? PlaceBlockPrediction;

	public InteractionMetaStore SelectMetaStore;

	public void CopyFrom(ContextMetaStore other)
	{
		TargetEntity = other.TargetEntity;
		HitLocation = other.HitLocation;
		HitDetail = other.HitDetail;
		TargetBlock = other.TargetBlock;
		TargetBlockRaw = other.TargetBlockRaw;
		TargetSlot = other.TargetSlot;
		DisableSlotFork = other.DisableSlotFork;
		PlaceBlockPrediction = other.PlaceBlockPrediction;
	}
}
