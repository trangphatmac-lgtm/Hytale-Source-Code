namespace HytaleClient.Graphics.Programs;

public struct Attrib
{
	public readonly uint Index;

	public readonly string Name;

	public Attrib(uint index, string name)
	{
		Index = index;
		Name = name;
	}
}
