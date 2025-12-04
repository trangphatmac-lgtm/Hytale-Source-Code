namespace HytaleClient.AssetEditor.Data;

internal struct AssetDiagnostics
{
	public static readonly AssetDiagnostics None = new AssetDiagnostics(null, null);

	public readonly AssetDiagnosticMessage[] Errors;

	public readonly AssetDiagnosticMessage[] Warnings;

	public AssetDiagnostics(AssetDiagnosticMessage[] errors, AssetDiagnosticMessage[] warnings)
	{
		Errors = errors;
		Warnings = warnings;
	}
}
