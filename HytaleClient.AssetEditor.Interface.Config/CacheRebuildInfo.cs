using HytaleClient.Protocol;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class CacheRebuildInfo
{
	public readonly AssetEditorRebuildCaches Caches;

	public readonly bool AppliesToChildProperties;

	public CacheRebuildInfo(AssetEditorRebuildCaches caches, bool appliesToChildProperties)
	{
		Caches = caches;
		AppliesToChildProperties = appliesToChildProperties;
	}
}
