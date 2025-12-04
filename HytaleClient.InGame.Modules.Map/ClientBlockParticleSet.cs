using System.Collections.Generic;
using HytaleClient.Graphics;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Map;

internal class ClientBlockParticleSet
{
	public UInt32Color Color;

	public float Scale;

	public Vector3 PositionOffset;

	public Quaternion RotationOffset;

	public Dictionary<ClientBlockParticleEvent, string> ParticleSystemIds;

	public ClientBlockParticleSet Clone()
	{
		ClientBlockParticleSet clientBlockParticleSet = new ClientBlockParticleSet();
		clientBlockParticleSet.Color = Color;
		clientBlockParticleSet.Scale = Scale;
		clientBlockParticleSet.PositionOffset = PositionOffset;
		clientBlockParticleSet.RotationOffset = RotationOffset;
		clientBlockParticleSet.ParticleSystemIds = new Dictionary<ClientBlockParticleEvent, string>(ParticleSystemIds);
		return clientBlockParticleSet;
	}
}
