#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using HytaleClient.Utils;

namespace HytaleClient.Data.Audio;

internal class WwiseHeaderParser
{
	private const string NamespaceToken = "namespace";

	private const string StaticToken = "static";

	private const string RightCurlyBracketToken = "}";

	private const string EventNamespace = "EVENTS";

	private const string GameParametersNamespace = "GAME_PARAMETERS";

	public static void Parse(string wwiseHeaderPath, out Dictionary<string, WwiseResource> upcomingWwiseIds)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		upcomingWwiseIds = new Dictionary<string, WwiseResource>();
		FileStream stream = File.OpenRead(wwiseHeaderPath);
		using StreamReader streamReader = new StreamReader(stream);
		char[] separator = new char[1] { ' ' };
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		while (!flag2 || !flag4)
		{
			string text = streamReader.ReadLine();
			if (text == null)
			{
				break;
			}
			string[] array = text.Split(separator, 6, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length == 0)
			{
				continue;
			}
			string text2 = array[0];
			if (text2 == "namespace" && array.Length > 1)
			{
				if (array[1] == "EVENTS")
				{
					flag = true;
				}
				if (array[1] == "GAME_PARAMETERS")
				{
					flag3 = true;
				}
			}
			else if (flag)
			{
				if (text2 == "static" && array.Length > 5)
				{
					string key = array[3];
					string text3 = array[5];
					uint id = uint.Parse(text3.Remove(text3.Length - 2));
					upcomingWwiseIds[key] = new WwiseResource(WwiseResource.WwiseResourceType.Event, id);
				}
				else if (text2 == "}")
				{
					flag = false;
					flag2 = true;
				}
			}
			else if (flag3)
			{
				if (text2 == "static" && array.Length > 5)
				{
					string key2 = array[3];
					string text4 = array[5];
					uint id2 = uint.Parse(text4.Remove(text4.Length - 2));
					upcomingWwiseIds[key2] = new WwiseResource(WwiseResource.WwiseResourceType.GameParameter, id2);
				}
				else if (text2 == "}")
				{
					flag3 = false;
					flag4 = true;
				}
			}
		}
	}
}
