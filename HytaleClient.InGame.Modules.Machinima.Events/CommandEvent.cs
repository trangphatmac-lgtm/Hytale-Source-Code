using Hypixel.ProtoPlus;
using HytaleClient.InGame.Modules.Machinima.Track;
using HytaleClient.Protocol;
using Newtonsoft.Json;

namespace HytaleClient.InGame.Modules.Machinima.Events;

internal class CommandEvent : KeyframeEvent
{
	[JsonProperty("Command")]
	public readonly string Command;

	public CommandEvent(string command)
	{
		Command = command;
		base.AllowDuplicates = true;
		base.Initialized = true;
	}

	public override void Execute(GameInstance gameInstance, SceneTrack track)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		if (Command.StartsWith("."))
		{
			gameInstance.ExecuteCommand(Command);
		}
		else
		{
			gameInstance.Connection.SendPacket((ProtoPacket)new ChatMessage(Command));
		}
	}

	public override string ToString()
	{
		return $"#{Id} - CommandEvent [Command: '{Command}']";
	}
}
