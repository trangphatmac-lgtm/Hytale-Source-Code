using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Data.UserSettings;

internal class BuilderToolsSettings
{
	public Dictionary<string, JObject> ToolFavorites = new Dictionary<string, JObject>();

	public List<string> BlockFavorites = new List<string>();

	public bool useToolReachDistance = false;

	public int ToolReachDistance = 128;

	public bool ToolReachLock;

	public int ToolDelayMin = 100;

	public bool EnableBrushShapeRendering = true;

	public int BrushOpacity = 30;

	public int SelectionOpacity = 12;

	public bool DisplayLegend = true;

	public bool ShowBuilderToolsNotifications = false;

	public BuilderToolsSettings Clone()
	{
		BuilderToolsSettings builderToolsSettings = new BuilderToolsSettings();
		builderToolsSettings.ToolFavorites = new Dictionary<string, JObject>(ToolFavorites);
		builderToolsSettings.BlockFavorites = new List<string>(BlockFavorites);
		builderToolsSettings.useToolReachDistance = useToolReachDistance;
		builderToolsSettings.ToolReachDistance = ToolReachDistance;
		builderToolsSettings.ToolReachLock = ToolReachLock;
		builderToolsSettings.ToolDelayMin = ToolDelayMin;
		builderToolsSettings.EnableBrushShapeRendering = EnableBrushShapeRendering;
		builderToolsSettings.BrushOpacity = BrushOpacity;
		builderToolsSettings.SelectionOpacity = SelectionOpacity;
		builderToolsSettings.DisplayLegend = DisplayLegend;
		builderToolsSettings.ShowBuilderToolsNotifications = ShowBuilderToolsNotifications;
		return builderToolsSettings;
	}
}
