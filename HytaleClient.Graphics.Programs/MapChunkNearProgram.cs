namespace HytaleClient.Graphics.Programs;

internal class MapChunkNearProgram : MapChunkBaseProgram
{
	public Uniform FoliageInteractionPositions;

	public Uniform FoliageInteractionParams;

	public MapChunkNearProgram(bool alphaTest, bool useDeferred, bool useLOD, string variationName = null)
		: base(alphaTest, alphaBlend: false, near: true, useDeferred, useLOD, variationName)
	{
	}
}
