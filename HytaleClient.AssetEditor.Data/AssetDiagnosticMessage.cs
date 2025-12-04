using HytaleClient.AssetEditor.Interface.Config;

namespace HytaleClient.AssetEditor.Data;

internal struct AssetDiagnosticMessage
{
	public readonly string Message;

	public readonly PropertyPath Property;

	public AssetDiagnosticMessage(PropertyPath property, string message)
	{
		Property = property;
		Message = message;
	}

	public AssetDiagnosticMessage(string property, string message)
		: this(PropertyPath.FromString(property), message)
	{
	}
}
