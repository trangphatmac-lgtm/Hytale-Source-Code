namespace HytaleClient.Graphics.Programs;

internal class MapChunkFarProgram : MapChunkBaseProgram
{
	public MapChunkFarProgram(bool alphaTest, bool useDeferred, bool useLOD, string variationName = null)
		: base(alphaTest, alphaBlend: false, near: false, useDeferred, useLOD, variationName)
	{
	}
}
