using Coherent.UI.Binding;

namespace HytaleClient.Data.Items;

[CoherentType]
internal class ClientIcon
{
	public const int MaxSize = 64;

	[CoherentProperty("x")]
	public readonly int X;

	[CoherentProperty("y")]
	public readonly int Y;

	[CoherentProperty("size")]
	public readonly int Size;

	public ClientIcon(int x, int y, int size)
	{
		X = x;
		Y = y;
		Size = size;
	}
}
