using System.Collections.Generic;
using HytaleClient.InGame.Modules.Machinima.Track;
using HytaleClient.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HytaleClient.InGame.Modules.Machinima.Events;

internal abstract class KeyframeEvent
{
	private static int NextId;

	[JsonIgnore]
	public readonly int Id;

	[JsonIgnore]
	public string Name => GetType().Name;

	[JsonIgnore]
	public bool AllowDuplicates { get; protected set; }

	[JsonIgnore]
	public bool Initialized { get; protected set; }

	public abstract void Execute(GameInstance gameInstance, SceneTrack track);

	public virtual void Initialize(MachinimaScene scene)
	{
		Initialized = true;
	}

	public KeyframeEvent()
	{
		Id = NextId++;
	}

	public JObject ToJsonObject()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		JObject val = new JObject();
		val.Add(Name, JToken.FromObject((object)this));
		return val;
	}

	public string ToCoherentJson()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		JObject val = ToJsonObject();
		val = (JObject)val[((JProperty)((JToken)val).First).Name];
		val["Id"] = JToken.op_Implicit(Id);
		val["Name"] = JToken.op_Implicit(Name);
		val["AllowDuplicates"] = JToken.op_Implicit(AllowDuplicates);
		return ((object)val).ToString();
	}

	public static KeyframeEvent ConvertJsonObject(JObject jsonData)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		switch (((JProperty)((JToken)jsonData).First).Name)
		{
		case "AnimationEvent":
		{
			string animationId = Extensions.Value<string>((IEnumerable<JToken>)jsonData["AnimationEvent"][(object)"AnimationId"]);
			AnimationSlot slot = (AnimationSlot)Extensions.Value<int>((IEnumerable<JToken>)jsonData["AnimationEvent"][(object)"AnimationSlot"]);
			return new AnimationEvent(animationId, slot);
		}
		case "CommandEvent":
		{
			string command = Extensions.Value<string>((IEnumerable<JToken>)jsonData["CommandEvent"][(object)"Command"]);
			return new CommandEvent(command);
		}
		case "ParticleEvent":
		{
			string particleSystemId = Extensions.Value<string>((IEnumerable<JToken>)jsonData["ParticleEvent"][(object)"ParticleId"]);
			return new ParticleEvent(particleSystemId);
		}
		case "TargetEvent":
		{
			string actorName = Extensions.Value<string>((IEnumerable<JToken>)jsonData["TargetEvent"][(object)"TargetName"]);
			return new TargetEvent(actorName);
		}
		case "CameraEvent":
		{
			bool cameraState = Extensions.Value<bool>((IEnumerable<JToken>)jsonData["CameraEvent"][(object)"CameraState"]);
			return new CameraEvent(cameraState);
		}
		default:
			return null;
		}
	}

	public virtual KeyframeEvent Clone()
	{
		return ConvertJsonObject(ToJsonObject());
	}
}
