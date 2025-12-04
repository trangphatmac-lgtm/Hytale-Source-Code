namespace HytaleClient.Graphics.Programs;

internal class DoFNearCoCBlurProgram : DoFNearCoCBaseProgram
{
	public DoFNearCoCBlurProgram()
		: base("ScreenVS.glsl", "DoFNearCoCBlurFS.glsl")
	{
	}
}
