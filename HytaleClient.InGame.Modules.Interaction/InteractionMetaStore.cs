using System.Collections.Generic;
using HytaleClient.Audio;
using HytaleClient.Data.ClientInteraction.Selector;
using HytaleClient.Graphics.Particles;
using HytaleClient.InGame.Modules.Entities.Projectile;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.Interaction;

internal class InteractionMetaStore
{
	public float? TimeShift;

	public AudioDevice.SoundEventReference SoundEventReference = AudioDevice.SoundEventReference.None;

	public float LastFirstPersonCameraTime = 0f;

	public float LastThirdPersonCameraTime = 0f;

	public int OldBlockId = int.MaxValue;

	public int ExpectedBlockId = int.MaxValue;

	public int PrimaryChargingLastProgress;

	public bool ChargingVisible = false;

	public float TotalDelay = 0f;

	public Selector EntitySelector;

	public HashSet<int> HitEntities;

	public List<SelectedHitEntity> RecordedHits;

	public InteractionChain ForkedChain;

	public int RemainingRepeats;

	public int? OriginalSlot;

	public EntityEffectUpdate PredictedEffect;

	public int Sequence;

	public EntityStatUpdate[] PredictedStats;

	public AudioDevice.SoundEventReference DamageSoundEventReference = AudioDevice.SoundEventReference.None;

	public ParticleSystemProxy[] DamageParticles;

	public PredictedProjectile Projectile;
}
