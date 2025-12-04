using System.Collections.Generic;
using System.IO;
using HytaleClient.Application;
using Newtonsoft.Json.Linq;
using SDL2;

namespace HytaleClient.Utils;

internal class ScreenResolutions
{
	public static ScreenResolution DefaultScreenResolution = new ScreenResolution(1280, 720, fullscreen: false);

	public static ScreenResolution CustomScreenResolution = new ScreenResolution(1025, 769, fullscreen: false);

	private static List<ScreenResolution> AvailableScreenResolutions = new List<ScreenResolution>();

	public static List<KeyValuePair<string, string>> GetAvailableResolutionOptions(App app)
	{
		if (AvailableScreenResolutions.Count == 0)
		{
			LoadResolutionsFromFile(app);
		}
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		foreach (ScreenResolution availableScreenResolution in AvailableScreenResolutions)
		{
			list.Add(new KeyValuePair<string, string>(availableScreenResolution.GetOptionName(availableScreenResolution.Fullscreen), availableScreenResolution.ToString()));
		}
		return list;
	}

	public static List<ScreenResolution> GetAvailableResolutions(App app)
	{
		if (AvailableScreenResolutions.Count == 0)
		{
			LoadResolutionsFromFile(app);
		}
		return AvailableScreenResolutions;
	}

	private static void LoadResolutionsFromFile(App app)
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Expected O, but got Unknown
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		int num = SDL.SDL_GetWindowDisplayIndex(app.Engine.Window.Handle);
		SDL_Rect val = default(SDL_Rect);
		SDL.SDL_GetDisplayBounds(num, ref val);
		string path = Path.Combine(Paths.GameData, "ScreenResolutions.json");
		if (!File.Exists(path))
		{
			return;
		}
		JArray val2 = JArray.Parse(File.ReadAllText(path));
		if (!((JToken)val2).HasValues)
		{
			return;
		}
		bool fullscreen = false;
		AvailableScreenResolutions.Clear();
		JToken val4 = default(JToken);
		foreach (JToken item2 in val2)
		{
			JObject val3 = (JObject)item2;
			if (val3 == null)
			{
				continue;
			}
			ScreenResolution item = default(ScreenResolution);
			if (!val3.TryGetValue("Width", ref val4))
			{
				continue;
			}
			item.Width = (int)val4;
			if (!val3.TryGetValue("Height", ref val4))
			{
				continue;
			}
			item.Height = (int)val4;
			if (item.Width <= val.w && item.Height <= val.h)
			{
				if (item.Width == val.w && item.Height == val.h)
				{
					fullscreen = true;
				}
				item.Fullscreen = fullscreen;
				AvailableScreenResolutions.Add(item);
			}
		}
	}
}
