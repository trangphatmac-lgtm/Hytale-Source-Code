using HytaleClient.Interface.Messages;

namespace HytaleClient.AssetEditor.Data;

public class TrackedAsset
{
	public readonly AssetReference Reference;

	public object Data;

	public bool IsLoading;

	public FormattedMessage FetchError;

	public bool IsAvailable => !IsLoading && FetchError == null && Data != null;

	public TrackedAsset(AssetReference assetReference, object data)
	{
		Reference = assetReference;
		Data = data;
	}
}
