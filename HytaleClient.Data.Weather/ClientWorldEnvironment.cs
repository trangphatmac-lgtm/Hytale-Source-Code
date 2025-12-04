using System.Collections.Generic;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Weather;

internal class ClientWorldEnvironment
{
	public string Id;

	public int WaterTint = -1;

	public Dictionary<int, FluidParticle> FluidParticles { get; private set; } = new Dictionary<int, FluidParticle>();


	public ClientWorldEnvironment()
	{
	}

	public ClientWorldEnvironment(WorldEnvironment worldEnvironment)
	{
		Id = worldEnvironment.Id;
		if (worldEnvironment.WaterTint != null)
		{
			WaterTint = ((byte)worldEnvironment.WaterTint.Red << 16) | ((byte)worldEnvironment.WaterTint.Green << 8) | (byte)worldEnvironment.WaterTint.Blue;
		}
		if (worldEnvironment.FluidParticles != null)
		{
			FluidParticles = new Dictionary<int, FluidParticle>(worldEnvironment.FluidParticles);
		}
	}

	public ClientWorldEnvironment Clone()
	{
		ClientWorldEnvironment clientWorldEnvironment = new ClientWorldEnvironment();
		clientWorldEnvironment.Id = Id;
		clientWorldEnvironment.WaterTint = WaterTint;
		clientWorldEnvironment.FluidParticles = new Dictionary<int, FluidParticle>(FluidParticles);
		return clientWorldEnvironment;
	}
}
