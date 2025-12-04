using System;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Weather;

internal class ClientCloud
{
	public const int LayerCount = 4;

	public string Texture;

	public Tuple<float, ColorAlpha>[] Colors { get; private set; }

	public Tuple<float, float>[] Speeds { get; private set; }

	public ClientCloud()
	{
	}

	public ClientCloud(Cloud cloud)
	{
		Texture = cloud.Texture;
		Colors = ClientWeather.ParseTimeDictionary(cloud.Colors, ClientWeather.DefaultWhiteColor);
		Speeds = ClientWeather.ParseTimeDictionary(cloud.Speeds, 1f);
	}

	public ClientCloud Clone()
	{
		ClientCloud clientCloud = new ClientCloud();
		clientCloud.Texture = Texture;
		clientCloud.Colors = ClientWeather.CloneColorAlphaDictionary(Colors);
		clientCloud.Speeds = Speeds;
		return clientCloud;
	}
}
