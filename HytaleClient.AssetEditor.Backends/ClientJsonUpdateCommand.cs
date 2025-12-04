using HytaleClient.AssetEditor.Interface.Config;
using HytaleClient.Protocol;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Backends;

public class ClientJsonUpdateCommand
{
	public JsonUpdateType Type;

	public PropertyPath Path;

	public JToken Value;

	public JToken PreviousValue;

	public PropertyPath? FirstCreatedProperty;

	public AssetEditorRebuildCaches RebuildCaches;

	public override string ToString()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		return string.Format("{0}: {1}, {2}: {3}, {4}: {5}", "Type", Type, "Path", Path, "Value", Value);
	}
}
