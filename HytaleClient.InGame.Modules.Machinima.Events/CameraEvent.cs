using HytaleClient.InGame.Modules.Machinima.Actors;
using HytaleClient.InGame.Modules.Machinima.Track;
using Newtonsoft.Json;

namespace HytaleClient.InGame.Modules.Machinima.Events;

internal class CameraEvent : KeyframeEvent
{
	[JsonProperty("CameraState")]
	public readonly bool CameraState;

	public CameraEvent(bool cameraState)
	{
		CameraState = cameraState;
		base.AllowDuplicates = false;
		base.Initialized = true;
	}

	public override void Execute(GameInstance gameInstance, SceneTrack track)
	{
		if (track.Parent is CameraActor)
		{
			(track.Parent as CameraActor).SetState(CameraState);
		}
	}

	public override string ToString()
	{
		return $"#{Id} - CameraEvent [CameraState: {CameraState}]";
	}
}
