using HytaleClient.Protocol;

namespace HytaleClient.Data.Entities.Initializers;

public class ClientRepulsionConfigInitializer
{
	public static void Initialize(RepulsionConfig repulsionConfig, ref ClientRepulsionConfig clientRepulsionConfig)
	{
		clientRepulsionConfig.Radius = repulsionConfig.Radius;
		clientRepulsionConfig.MaxForce = repulsionConfig.MaxForce;
		clientRepulsionConfig.MinForce = repulsionConfig.MinForce;
	}
}
