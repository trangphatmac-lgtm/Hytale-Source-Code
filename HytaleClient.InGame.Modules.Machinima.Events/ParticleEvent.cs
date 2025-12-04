using HytaleClient.Graphics.Particles;
using HytaleClient.InGame.Modules.Machinima.Track;
using HytaleClient.Math;
using Newtonsoft.Json;

namespace HytaleClient.InGame.Modules.Machinima.Events;

internal class ParticleEvent : KeyframeEvent
{
	[JsonProperty("ParticleId")]
	public readonly string ParticleSystemId;

	private ParticleSystemProxy _particleSystemProxy;

	public ParticleEvent(string particleSystemId)
	{
		ParticleSystemId = particleSystemId;
		base.AllowDuplicates = true;
		base.Initialized = true;
	}

	public override void Execute(GameInstance gameInstance, SceneTrack track)
	{
		if (_particleSystemProxy != null)
		{
			_particleSystemProxy.Expire();
			_particleSystemProxy = null;
		}
		if (_particleSystemProxy != null || gameInstance.ParticleSystemStoreModule.TrySpawnSystem(ParticleSystemId, out _particleSystemProxy, isLocalPlayer: false, isTracked: true))
		{
			_particleSystemProxy.Position = track.Parent.Position;
			_particleSystemProxy.Rotation = Quaternion.CreateFromYawPitchRoll(track.Parent.Rotation.Yaw, track.Parent.Rotation.Pitch, track.Parent.Rotation.Roll);
			track.AddParticleSystem(_particleSystemProxy);
		}
	}

	public override string ToString()
	{
		return $"#{Id} - ParticleEvent [ParticleId: '{ParticleSystemId}']";
	}
}
