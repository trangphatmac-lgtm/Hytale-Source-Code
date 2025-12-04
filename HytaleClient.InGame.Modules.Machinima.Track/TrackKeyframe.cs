using System;
using System.Collections.Generic;
using System.Linq;
using Coherent.UI.Binding;
using HytaleClient.InGame.Modules.Machinima.Events;
using HytaleClient.InGame.Modules.Machinima.Settings;
using HytaleClient.Math;
using Newtonsoft.Json;

namespace HytaleClient.InGame.Modules.Machinima.Track;

[CoherentType]
internal class TrackKeyframe
{
	public class DuplicateKeyframeEvent : Exception
	{
	}

	private static int NextId;

	public static readonly BoundingBox KeyframeBox = new BoundingBox(new Vector3(-0.125f, -0.125f, -0.125f), new Vector3(0.125f, 0.125f, 0.125f));

	public static readonly BoundingBox PathBox = new BoundingBox(new Vector3(-0.0625f, -0.0625f, -0.0625f), new Vector3(0.0625f, 0.0625f, 0.0625f));

	[JsonIgnore]
	[CoherentProperty("id")]
	public readonly int Id;

	[CoherentProperty("frame")]
	public float Frame = 0f;

	[CoherentProperty("settings")]
	public Dictionary<string, IKeyframeSetting> Settings { get; private set; }

	public List<KeyframeEvent> Events { get; private set; }

	[JsonIgnore]
	[CoherentProperty("events")]
	public string[] CoherentEvents => Events.Select((KeyframeEvent evt) => evt.ToCoherentJson()).ToArray();

	public TrackKeyframe(float frame)
	{
		Settings = new Dictionary<string, IKeyframeSetting>();
		Events = new List<KeyframeEvent>();
		Frame = frame;
		Id = NextId++;
	}

	public void AddSetting(IKeyframeSetting setting)
	{
		Settings[setting.Name] = setting;
	}

	public void RemoveSetting(string name)
	{
		Settings.Remove(name);
	}

	public KeyframeSetting<T> GetSetting<T>(string name)
	{
		if (Settings.ContainsKey(name))
		{
			return (KeyframeSetting<T>)Settings[name];
		}
		return null;
	}

	public KeyframeSetting<T> GetSetting<T>(KeyframeSettingType settingType)
	{
		if (Settings.ContainsKey(settingType.ToString()))
		{
			return (KeyframeSetting<T>)Settings[settingType.ToString()];
		}
		return null;
	}

	public void AddEvent(KeyframeEvent keyframeEvent)
	{
		if (!keyframeEvent.AllowDuplicates)
		{
			for (int i = 0; i < Events.Count; i++)
			{
				if (Events[i].Name == keyframeEvent.Name)
				{
					throw new DuplicateKeyframeEvent();
				}
			}
		}
		Events.Add(keyframeEvent);
	}

	public void RemoveEvent(int eventId)
	{
		int num = -1;
		for (int i = 0; i < Events.Count; i++)
		{
			if (Events[i].Id == eventId)
			{
				num = i;
				break;
			}
		}
		if (num != -1)
		{
			Events.RemoveAt(num);
		}
	}

	public KeyframeEvent GetEvent(int eventId)
	{
		for (int i = 0; i < Events.Count; i++)
		{
			if (Events[i].Id == eventId)
			{
				return Events[i];
			}
		}
		return null;
	}

	public bool HasEvent(int eventId)
	{
		return GetEvent(eventId) != null;
	}

	public void TriggerEvents(GameInstance gameInstance, SceneTrack track)
	{
		if (Events.Count != 0)
		{
			for (int i = 0; i < Events.Count; i++)
			{
				Events[i].Execute(gameInstance, track);
			}
		}
	}

	public TrackKeyframe Clone()
	{
		TrackKeyframe trackKeyframe = new TrackKeyframe(Frame);
		foreach (KeyValuePair<string, IKeyframeSetting> setting in Settings)
		{
			trackKeyframe.AddSetting(setting.Value.Clone());
		}
		foreach (KeyframeEvent @event in Events)
		{
			trackKeyframe.AddEvent(@event.Clone());
		}
		return trackKeyframe;
	}
}
