using HytaleClient.Protocol;

namespace HytaleClient.Data.Entities;

public class ClientRepulsionConfig
{
	public const int NoRepulsionConfigIndex = -1;

	public float Radius;

	public float MinForce;

	public float MaxForce;

	public ClientRepulsionConfig()
	{
	}

	public ClientRepulsionConfig(RepulsionConfig repulsionConfig)
	{
		Radius = repulsionConfig.Radius;
		MinForce = repulsionConfig.MinForce;
		MaxForce = repulsionConfig.MaxForce;
	}

	public ClientRepulsionConfig Clone()
	{
		ClientRepulsionConfig clientRepulsionConfig = new ClientRepulsionConfig();
		clientRepulsionConfig.Radius = Radius;
		clientRepulsionConfig.MinForce = MinForce;
		clientRepulsionConfig.MaxForce = MaxForce;
		return clientRepulsionConfig;
	}
}
