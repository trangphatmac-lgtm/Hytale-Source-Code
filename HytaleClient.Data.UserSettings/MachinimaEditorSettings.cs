namespace HytaleClient.Data.UserSettings;

public class MachinimaEditorSettings
{
	public int NewKeyframeFrameOffset = 30;

	public int AutosaveDelay = 30;

	public bool CompressSaveFiles = false;

	public MachinimaEditorSettings Clone()
	{
		return new MachinimaEditorSettings
		{
			NewKeyframeFrameOffset = NewKeyframeFrameOffset,
			AutosaveDelay = AutosaveDelay,
			CompressSaveFiles = CompressSaveFiles
		};
	}
}
