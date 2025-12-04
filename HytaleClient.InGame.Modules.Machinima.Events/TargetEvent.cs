using HytaleClient.InGame.Modules.Machinima.Actors;
using HytaleClient.InGame.Modules.Machinima.Track;
using Newtonsoft.Json;

namespace HytaleClient.InGame.Modules.Machinima.Events;

internal class TargetEvent : KeyframeEvent
{
	private SceneActor _targetActor;

	[JsonProperty("TargetName")]
	public string TargetName;

	public TargetEvent(SceneActor targetActor)
	{
		_targetActor = targetActor;
		TargetName = ((targetActor == null) ? "" : targetActor.Name);
		base.AllowDuplicates = false;
		base.Initialized = true;
	}

	public TargetEvent(string actorName)
	{
		TargetName = actorName;
		base.AllowDuplicates = false;
	}

	public override void Initialize(MachinimaScene scene)
	{
		if (!base.Initialized)
		{
			if (TargetName == "")
			{
				_targetActor = null;
			}
			else
			{
				_targetActor = scene.GetActor(TargetName);
			}
			base.Initialized = true;
		}
	}

	public SceneActor GetTarget()
	{
		return _targetActor;
	}

	public override void Execute(GameInstance gameInstance, SceneTrack track)
	{
		if (track.Parent != null && track.Parent != null)
		{
			track.Parent.SetLookTarget(_targetActor);
		}
	}

	public override string ToString()
	{
		return $"#{Id} - TargetEvent [Target: '{TargetName}']";
	}
}
