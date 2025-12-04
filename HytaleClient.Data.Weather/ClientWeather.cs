using System;
using System.Collections.Generic;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Weather;

internal class ClientWeather
{
	public const string SunTexture = "Sky/Sun.png";

	public const float DefaultHour = 0f;

	public static readonly byte[] DefaultWhiteColor = new byte[4] { 255, 255, 255, 255 };

	public static readonly byte[] DefaultTransparentColor = new byte[4] { 255, 255, 255, 0 };

	public string Id { get; private set; }

	public WeatherParticle Particle { get; private set; }

	public string Stars { get; private set; }

	public Tuple<float, Color>[] SunlightColors { get; private set; }

	public Tuple<float, ColorAlpha>[] SkyTopColors { get; private set; }

	public Tuple<float, ColorAlpha>[] SkyBottomColors { get; private set; }

	public Tuple<float, ColorAlpha>[] SkySunsetColors { get; private set; }

	public Tuple<float, Color>[] WaterTints { get; private set; }

	public Tuple<float, float>[] SunlightDampingMultipliers { get; private set; }

	public Tuple<float, Color>[] SunColors { get; private set; }

	public Tuple<float, float>[] SunScales { get; private set; }

	public Tuple<float, ColorAlpha>[] SunGlowColors { get; private set; }

	public Dictionary<int, string> Moons { get; private set; }

	public Tuple<float, ColorAlpha>[] MoonColors { get; private set; }

	public Tuple<float, float>[] MoonScales { get; private set; }

	public Tuple<float, ColorAlpha>[] MoonGlowColors { get; private set; }

	public string ScreenEffect { get; private set; }

	public Tuple<float, ColorAlpha>[] ScreenEffectColors { get; private set; }

	public NearFar Fog { get; private set; }

	public Tuple<float, Color>[] FogColors { get; private set; }

	public Tuple<float, float>[] FogHeightFalloffs { get; private set; }

	public Tuple<float, float>[] FogDensities { get; private set; }

	public Tuple<float, Color>[] ColorFilters { get; private set; }

	public ClientCloud[] Clouds { get; private set; }

	public ClientWeather()
	{
	}

	public ClientWeather(Weather weather)
	{
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Expected O, but got Unknown
		Id = weather.Id;
		if (weather.Particle != null)
		{
			Particle = weather.Particle;
		}
		Stars = weather.Stars;
		SunlightColors = ParseTimeDictionary(weather.SunlightColors, DefaultWhiteColor);
		SkyTopColors = ParseTimeDictionary(weather.SkyTopColors, DefaultWhiteColor);
		SkyBottomColors = ParseTimeDictionary(weather.SkyBottomColors, DefaultWhiteColor);
		SkySunsetColors = ParseTimeDictionary(weather.SkySunsetColors, DefaultWhiteColor);
		if (weather.WaterTints != null)
		{
			WaterTints = ParseTimeDictionary(weather.WaterTints, DefaultWhiteColor);
		}
		else
		{
			WaterTints = CloneDownColorAlphaDictionary(SkyTopColors);
		}
		SunlightDampingMultipliers = ParseTimeDictionary(weather.SunlightDampingMultiplier, 1f);
		SunColors = ParseTimeDictionary(weather.SunColors, DefaultWhiteColor);
		SunScales = ParseTimeDictionary(weather.SunScales, 1f);
		SunGlowColors = ParseTimeDictionary(weather.SunGlowColors, DefaultTransparentColor);
		Moons = ((weather.Moons != null) ? weather.Moons : new Dictionary<int, string>());
		MoonColors = ParseTimeDictionary(weather.MoonColors, DefaultWhiteColor);
		MoonScales = ParseTimeDictionary(weather.MoonScales, 1f);
		MoonGlowColors = ParseTimeDictionary(weather.MoonGlowColors, DefaultTransparentColor);
		ScreenEffect = weather.ScreenEffect;
		ScreenEffectColors = ParseTimeDictionary(weather.ScreenEffectColors, DefaultWhiteColor);
		Fog = weather.Fog;
		FogColors = ParseTimeDictionary(weather.FogColors, DefaultWhiteColor);
		ColorFilters = ParseTimeDictionary(weather.ColorFilters, DefaultWhiteColor);
		FogHeightFalloffs = ParseTimeDictionary(weather.FogHeightFalloffs, 10f);
		FogDensities = ParseTimeDictionary(weather.FogDensities, 0f);
		Clouds = new ClientCloud[4];
		for (int i = 0; i < 4; i++)
		{
			Cloud cloud = (Cloud)((weather.Clouds != null && i < weather.Clouds.Length) ? ((object)weather.Clouds[i]) : ((object)new Cloud()));
			Clouds[i] = new ClientCloud(cloud);
		}
	}

	public ClientWeather Clone()
	{
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Expected O, but got Unknown
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		ClientWeather clientWeather = new ClientWeather();
		clientWeather.Id = Id;
		if (Particle != null)
		{
			clientWeather.Particle = new WeatherParticle(Particle);
		}
		clientWeather.Stars = Stars;
		clientWeather.SunlightDampingMultipliers = SunlightDampingMultipliers;
		clientWeather.SunlightColors = CloneColorDictionary(SunlightColors);
		clientWeather.SkyTopColors = CloneColorAlphaDictionary(SkyTopColors);
		clientWeather.SkyBottomColors = CloneColorAlphaDictionary(SkyBottomColors);
		clientWeather.SkySunsetColors = CloneColorAlphaDictionary(SkySunsetColors);
		clientWeather.WaterTints = CloneColorDictionary(WaterTints);
		clientWeather.SunColors = CloneColorDictionary(SunColors);
		clientWeather.SunScales = SunScales;
		clientWeather.SunGlowColors = CloneColorAlphaDictionary(SunGlowColors);
		clientWeather.Moons = new Dictionary<int, string>(Moons);
		clientWeather.MoonColors = CloneColorAlphaDictionary(MoonColors);
		clientWeather.MoonScales = MoonScales;
		clientWeather.MoonGlowColors = CloneColorAlphaDictionary(MoonGlowColors);
		clientWeather.ScreenEffect = ScreenEffect;
		clientWeather.ScreenEffectColors = CloneColorAlphaDictionary(ScreenEffectColors);
		clientWeather.Fog = new NearFar(Fog.Near, Fog.Far);
		clientWeather.FogColors = CloneColorDictionary(FogColors);
		clientWeather.FogHeightFalloffs = FogHeightFalloffs;
		clientWeather.FogDensities = FogDensities;
		clientWeather.ColorFilters = CloneColorDictionary(ColorFilters);
		clientWeather.Clouds = new ClientCloud[4];
		for (int i = 0; i < clientWeather.Clouds.Length; i++)
		{
			clientWeather.Clouds[i] = Clouds[i].Clone();
		}
		return clientWeather;
	}

	public static Tuple<float, Color>[] CloneColorDictionary(Tuple<float, Color>[] list)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		Tuple<float, Color>[] array = new Tuple<float, Color>[list.Length];
		for (int i = 0; i < list.Length; i++)
		{
			Tuple<float, Color> tuple = list[i];
			array[i] = new Tuple<float, Color>(tuple.Item1, new Color(tuple.Item2));
		}
		return array;
	}

	public static Tuple<float, Color>[] CloneDownColorAlphaDictionary(Tuple<float, ColorAlpha>[] list)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		Tuple<float, Color>[] array = new Tuple<float, Color>[list.Length];
		for (int i = 0; i < list.Length; i++)
		{
			Tuple<float, ColorAlpha> tuple = list[i];
			array[i] = new Tuple<float, Color>(tuple.Item1, new Color(tuple.Item2.Red, tuple.Item2.Green, tuple.Item2.Blue));
		}
		return array;
	}

	public static Tuple<float, ColorAlpha>[] CloneColorAlphaDictionary(Tuple<float, ColorAlpha>[] list)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		Tuple<float, ColorAlpha>[] array = new Tuple<float, ColorAlpha>[list.Length];
		for (int i = 0; i < list.Length; i++)
		{
			Tuple<float, ColorAlpha> tuple = list[i];
			array[i] = new Tuple<float, ColorAlpha>(tuple.Item1, new ColorAlpha(tuple.Item2));
		}
		return array;
	}

	public static Tuple<float, float>[] ParseTimeDictionary(Dictionary<float, float> dictionary, float defaultValue)
	{
		if (dictionary == null || dictionary.Count == 0)
		{
			return new Tuple<float, float>[1]
			{
				new Tuple<float, float>(0f, defaultValue)
			};
		}
		Tuple<float, float>[] array = new Tuple<float, float>[dictionary.Count];
		int num = 0;
		foreach (KeyValuePair<float, float> item in dictionary)
		{
			array[num++] = new Tuple<float, float>(item.Key, item.Value);
		}
		Array.Sort(array, (Tuple<float, float> a, Tuple<float, float> b) => a.Item1.CompareTo(b.Item1));
		return array;
	}

	public static Tuple<float, Color>[] ParseTimeDictionary(Dictionary<float, Color> dictionary, byte[] defaultValue)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		if (dictionary == null || dictionary.Count == 0)
		{
			return new Tuple<float, Color>[1]
			{
				new Tuple<float, Color>(0f, new Color((sbyte)defaultValue[0], (sbyte)defaultValue[1], (sbyte)defaultValue[2]))
			};
		}
		Tuple<float, Color>[] array = new Tuple<float, Color>[dictionary.Count];
		int num = 0;
		foreach (KeyValuePair<float, Color> item in dictionary)
		{
			array[num++] = new Tuple<float, Color>(item.Key, item.Value);
		}
		Array.Sort(array, (Tuple<float, Color> a, Tuple<float, Color> b) => a.Item1.CompareTo(b.Item1));
		return array;
	}

	public static Tuple<float, ColorAlpha>[] ParseTimeDictionary(Dictionary<float, ColorAlpha> dictionary, byte[] defaultValue)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		if (dictionary == null || dictionary.Count == 0)
		{
			return new Tuple<float, ColorAlpha>[1]
			{
				new Tuple<float, ColorAlpha>(0f, new ColorAlpha((sbyte)defaultValue[3], (sbyte)defaultValue[0], (sbyte)defaultValue[1], (sbyte)defaultValue[2]))
			};
		}
		Tuple<float, ColorAlpha>[] array = new Tuple<float, ColorAlpha>[dictionary.Count];
		int num = 0;
		foreach (KeyValuePair<float, ColorAlpha> item in dictionary)
		{
			array[num++] = new Tuple<float, ColorAlpha>(item.Key, item.Value);
		}
		Array.Sort(array, (Tuple<float, ColorAlpha> a, Tuple<float, ColorAlpha> b) => a.Item1.CompareTo(b.Item1));
		return array;
	}
}
