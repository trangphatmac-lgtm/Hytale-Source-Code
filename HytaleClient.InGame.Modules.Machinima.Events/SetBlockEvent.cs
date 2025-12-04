using HytaleClient.InGame.Modules.Machinima.Track;
using Newtonsoft.Json;

namespace HytaleClient.InGame.Modules.Machinima.Events;

internal class SetBlockEvent : KeyframeEvent
{
	[JsonProperty("SetBlock")]
	public readonly int X;

	public readonly int Y;

	public readonly int Z;

	public readonly ushort BlockId;

	public SetBlockEvent(int x, int y, int z, ushort blockid)
	{
		X = x;
		Y = y;
		Z = z;
		BlockId = blockid;
		base.AllowDuplicates = true;
		base.Initialized = true;
	}

	public override void Execute(GameInstance gameInstance, SceneTrack track)
	{
		gameInstance.MapModule.SetClientBlock(X, Y, Z, BlockId);
	}

	public override string ToString()
	{
		return $"#{Id} - SetBlockEvent: Block {BlockId} @ [{X}, {Y}, {Z}]";
	}
}
