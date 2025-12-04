using HytaleClient.InGame.Modules.Machinima.Track;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.Machinima;

internal struct SerializedSceneObject
{
	public string Name;

	public int Type;

	public SceneTrack Track;

	public Model Model;

	public string ModelId;

	public string ItemId;

	public float Scale;
}
