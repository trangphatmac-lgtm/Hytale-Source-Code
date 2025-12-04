using System.Collections.Generic;
using HytaleClient.Data.Items;
using HytaleClient.Protocol;
using Newtonsoft.Json.Linq;

namespace HytaleClient.InGame.Modules.BuilderTools.Tools;

internal class BuilderTool
{
	public const string BaseToolNamePrefix = "EditorTool_";

	public const string ToolDataKey = "ToolData";

	public const string BrushDataKey = "BrushData";

	public readonly ItemBuilderToolData BuilderToolData;

	public readonly BuilderToolItem ToolItem;

	public readonly string Id;

	public readonly bool IsBrushTool;

	public BuilderTool(ItemBuilderToolData builderToolData)
	{
		BuilderToolData = builderToolData;
		ToolItem = builderToolData.Tools[0];
		Id = ToolItem.Id;
		IsBrushTool = ToolItem.IsBrush;
	}

	public bool TryGetArg(string argId, out BuilderToolArg toolArg)
	{
		if (ToolItem.Args.ContainsKey(argId))
		{
			toolArg = ToolItem.Args[argId];
			return true;
		}
		toolArg = null;
		return false;
	}

	public string GetDefaultArgValue(string argId)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected I4, but got Unknown
		if (!TryGetArg(argId, out var toolArg))
		{
			return null;
		}
		BuilderToolArgType argType = toolArg.ArgType;
		BuilderToolArgType val = argType;
		return (int)val switch
		{
			4 => toolArg.BlockArg.Default_, 
			0 => toolArg.BoolArg.Default_.ToString(), 
			7 => ((object)(BrushOrigin)(ref toolArg.BrushOriginArg.Default_)).ToString(), 
			6 => ((object)(BrushShape)(ref toolArg.BrushShapeArg.Default_)).ToString(), 
			1 => toolArg.FloatArg.Default_.ToString(), 
			2 => toolArg.IntArg.Default_.ToString(), 
			5 => toolArg.MaskArg.Default_, 
			10 => toolArg.OptionArg.Default_, 
			3 => toolArg.StringArg.Default_, 
			_ => null, 
		};
	}

	public JObject GetDefaultArgData()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		JObject val = new JObject();
		Dictionary<string, BuilderToolArg> args = ToolItem.Args;
		if (args != null && args.Count > 0)
		{
			foreach (KeyValuePair<string, BuilderToolArg> arg in ToolItem.Args)
			{
				val[arg.Key] = JToken.op_Implicit(GetDefaultArgValue(arg.Key));
			}
		}
		return val;
	}

	public bool TryGetItemArgValue(ref ClientItemStack item, string argId, out string argValue)
	{
		argValue = string.Empty;
		if (item.Metadata == null)
		{
			return false;
		}
		JToken val = item.Metadata["ToolData"];
		if (val == null || !TryGetArg(argId, out var _))
		{
			return false;
		}
		argValue = (string)val[(object)argId];
		return true;
	}

	public string GetItemArgValueOrDefault(ref ClientItemStack item, string argId)
	{
		BuilderToolArg toolArg;
		string argValue;
		return (!TryGetArg(argId, out toolArg)) ? null : (TryGetItemArgValue(ref item, argId, out argValue) ? argValue : GetDefaultArgValue(argId));
	}

	public Dictionary<string, string> GetItemToolArgs(ClientItemStack item)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		Dictionary<string, BuilderToolArg> args = ToolItem.Args;
		if (args != null && args.Count > 0)
		{
			foreach (KeyValuePair<string, BuilderToolArg> arg in ToolItem.Args)
			{
				string key = arg.Key;
				string argValue;
				string value = (TryGetItemArgValue(ref item, key, out argValue) ? argValue : GetDefaultArgValue(key));
				dictionary.Add(key, value);
			}
		}
		return dictionary;
	}

	public string GetToolArgsLogText(ToolInstance toolInstance)
	{
		ClientItemStack item = toolInstance.ItemStack;
		string text = (IsBrushTool ? toolInstance.BrushData.ToString() : "");
		Dictionary<string, BuilderToolArg> args = ToolItem.Args;
		if (args != null && args.Count > 0)
		{
			text += "Tool Args\n";
			foreach (KeyValuePair<string, BuilderToolArg> arg in ToolItem.Args)
			{
				string key = arg.Key;
				string argValue;
				string text2 = (TryGetItemArgValue(ref item, key, out argValue) ? argValue : GetDefaultArgValue(key));
				text = text + "  " + key + ": " + text2 + "\n";
			}
		}
		string text3 = Id + " " + (IsBrushTool ? "Brush" : "Tool") + "\n";
		return text3 + ((text.Length == 0) ? "No Args\n" : text);
	}

	public string GetFirstBlockArgId()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Invalid comparison between Unknown and I4
		foreach (KeyValuePair<string, BuilderToolArg> arg in ToolItem.Args)
		{
			if ((int)arg.Value.ArgType == 4)
			{
				return arg.Key;
			}
		}
		return null;
	}

	public static BuilderTool GetToolFromItemStack(GameInstance gameInstance, ClientItemStack itemStack)
	{
		if (itemStack == null || gameInstance == null)
		{
			return null;
		}
		return gameInstance.ItemLibraryModule.GetItem(itemStack.Id)?.BuilderTool;
	}

	public static ClientItemBase[] GetBuilderToolItems(GameInstance gameInstance)
	{
		if (gameInstance == null)
		{
			return null;
		}
		List<ClientItemBase> list = new List<ClientItemBase>();
		Dictionary<string, ClientItemBase> items = gameInstance.ItemLibraryModule.GetItems();
		foreach (ClientItemBase value in items.Values)
		{
			if (value.BuilderTool != null)
			{
				list.Add(value);
			}
		}
		list.Sort((ClientItemBase a, ClientItemBase b) => a.BuilderTool.Id.CompareTo(b.BuilderTool.Id));
		return list.ToArray();
	}

	public override string ToString()
	{
		return Id + " " + (IsBrushTool ? "Brush" : "Tool");
	}
}
