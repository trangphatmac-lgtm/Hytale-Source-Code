using Hypixel.ProtoPlus;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Machinima.Actors;
using HytaleClient.InGame.Modules.Machinima.Track;
using HytaleClient.Protocol;
using Newtonsoft.Json;

namespace HytaleClient.InGame.Modules.Machinima.Events;

internal class AnimationEvent : KeyframeEvent
{
	[JsonProperty("AnimationId")]
	public readonly string AnimationId;

	[JsonProperty("AnimationSlot")]
	public readonly AnimationSlot Slot = (AnimationSlot)0;

	public AnimationEvent(string animationId, AnimationSlot slot = 0)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		AnimationId = animationId;
		Slot = slot;
		base.AllowDuplicates = true;
		base.Initialized = true;
	}

	public override void Execute(GameInstance gameInstance, SceneTrack track)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		if (track.Parent is EntityActor)
		{
			Entity entity = ((EntityActor)track.Parent).GetEntity();
			if (entity != null)
			{
				string text = ((string.IsNullOrWhiteSpace(AnimationId) || AnimationId.ToLower() == "off") ? null : AnimationId);
				gameInstance.InjectPacket((ProtoPacket)new PlayAnimation(entity.NetworkId, (string)null, text, Slot));
			}
		}
	}

	public override string ToString()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		return $"#{Id} AnimationEvent [Id: '{AnimationId}', Slot: '{Slot}']";
	}
}
