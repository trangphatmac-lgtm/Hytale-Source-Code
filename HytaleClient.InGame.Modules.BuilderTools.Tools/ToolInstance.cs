using HytaleClient.Data.Items;
using HytaleClient.InGame.Modules.BuilderTools.Tools.Brush;
using HytaleClient.InGame.Modules.BuilderTools.Tools.Client;

namespace HytaleClient.InGame.Modules.BuilderTools.Tools;

internal class ToolInstance
{
	public readonly ClientItemStack ItemStack;

	public readonly BuilderTool BuilderTool;

	public readonly ClientTool ClientTool;

	public readonly BrushData BrushData;

	public ToolInstance(ClientItemStack itemStack, BuilderTool builderTool, ClientTool clientTool, BrushData brushData)
	{
		ItemStack = itemStack;
		BuilderTool = builderTool;
		ClientTool = clientTool;
		BrushData = brushData;
	}
}
