using System;

namespace HytaleClient.Application.Services;

public class ServicesEndpoint
{
	public static readonly ServicesEndpoint Development = new ServicesEndpoint("ash-dev.services.hytale.com", 443, secure: true);

	public static readonly ServicesEndpoint Production = new ServicesEndpoint("services.hytale.com", 443, secure: true);

	public static readonly ServicesEndpoint Default = Production;

	public readonly string Host;

	public readonly int Port;

	public readonly bool Secure;

	public ServicesEndpoint(string host, int port, bool secure)
	{
		Host = host;
		Port = port;
		Secure = secure;
	}

	public override string ToString()
	{
		return string.Format("{0}: {1}, {2}: {3}, {4}: {5}", "Host", Host, "Port", Port, "Secure", Secure);
	}

	public static ServicesEndpoint Parse(string input)
	{
		if (!(input == "Development"))
		{
			if (input == "Production")
			{
				return Production;
			}
			throw new Exception("Failed to parse services endpoint '" + input + "'");
		}
		return Development;
	}
}
